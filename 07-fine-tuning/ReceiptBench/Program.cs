// Receipt test for Step 4: how well does a model read Shoebox's 6 fictional sample receipts, field by field?
//   dotnet run -- selftest
//   dotnet run -- transcribe [--vision gemma4:e4b]   photo → plain text, once (saved to samples/transcripts.json)
//   dotnet run -- direct gemma4:e4b                  photo → fields in one step (how Shoebox works)
//   dotnet run -- extract gemma3:270m                text → fields (the job the fine-tuned model will learn)
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ReceiptBench;

if (args.Length > 0 && args[0] == "selftest") return Score.SelfTest();

// Find this project's folder (samples and the shared prompt live there, not in bin/).
var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ReceiptBench.csproj"))) dir = dir.Parent;
var project = dir?.FullName ?? throw new InvalidOperationException("Run this from inside the ReceiptBench folder.");
var samples = Path.Combine(project, "samples");
var transcriptsFile = Path.Combine(samples, "transcripts.json");
// The SAME prompt is used to test and (in Step 4.3) to train, so the fine-tuned model sees exactly what it learned with.
var prompt = File.ReadAllText(Path.Combine(project, "..", "prompt.txt")).Trim();
var expected = Score.LoadExpected(Path.Combine(samples, "expected.json"));

string Opt(string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
IChatClient Client(string model) => new OllamaApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(15) }, model);
var mode = args.Length > 0 ? args[0] : "";

// ── transcribe: let a vision model write out each receipt's text, once ──────
if (mode == "transcribe")
{
    var vision = Opt("--vision", "gemma4:e4b");
    var eyes = Client(vision);
    var transcripts = new Dictionary<string, string>();
    foreach (var e in expected)
    {
        var started = DateTime.Now;
        var msg = new ChatMessage(ChatRole.User, [
            new TextContent("Transcribe every line of text on this receipt exactly as printed, top to bottom. Plain text only, no commentary."),
            new DataContent(File.ReadAllBytes(Path.Combine(samples, e.File)), "image/jpeg")]);
        var text = (await eyes.GetResponseAsync([msg], new ChatOptions { Temperature = 0 })).Text.Trim();
        transcripts[e.File] = text;
        Console.WriteLine($"── {e.File} ({(DateTime.Now - started).TotalSeconds:F0} s, {vision})\n{text}\n");
    }
    File.WriteAllText(transcriptsFile, JsonSerializer.Serialize(transcripts, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Saved to {transcriptsFile}");
    return 0;
}

// ── direct / extract: read the fields and score them ────────────────────────
if (mode is "direct" or "extract")
{
    var model = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "gemma4:e4b";
    Dictionary<string, string>? transcripts = null;
    if (mode == "extract")
    {
        if (!File.Exists(transcriptsFile)) { Console.WriteLine("No transcripts yet. Run: dotnet run -- transcribe"); return 1; }
        transcripts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(transcriptsFile))!;
    }
    var client = Client(model);
    var hits = Score.Fields.ToDictionary(f => f, _ => 0);
    var table = new StringBuilder("| Receipt | " + string.Join(" | ", Score.Fields) + " | Time |\n|---|" + string.Concat(Score.Fields.Select(_ => "---|")) + "---|\n");
    double totalSecs = 0;
    foreach (var e in expected)
    {
        var started = DateTime.Now;
        ChatMessage msg = mode == "direct"
            ? new(ChatRole.User, [new TextContent(prompt), new DataContent(File.ReadAllBytes(Path.Combine(samples, e.File)), "image/jpeg")])
            : new(ChatRole.User, prompt + "\n\nReceipt:\n" + transcripts![e.File]);
        string reply;
        try { reply = (await client.GetResponseAsync([msg], new ChatOptions { Temperature = 0, ResponseFormat = ChatResponseFormat.Json })).Text; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { Console.WriteLine($"Couldn't get an answer from Ollama ({ex.GetType().Name}). Is '{model}' pulled?"); return 1; }
        var secs = (DateTime.Now - started).TotalSeconds; totalSecs += secs;
        var r = Score.Parse(reply);
        var ok = Score.Check(e, r);
        foreach (var f in Score.Fields) if (ok[f]) hits[f]++;
        object Val(string f) => f switch { "vendor" => r.Vendor, "date" => r.Date, "subtotal" => r.Subtotal, "gst" => r.Gst, "other_tax" => r.OtherTax, "tip" => r.Tip, _ => r.Total };
        table.Append($"| {e.File} | " + string.Join(" | ", Score.Fields.Select(f => ok[f] ? "✅" : $"❌ `{Val(f)}`")) + $" | {secs:F0} s |\n");
        Console.WriteLine($"{e.File,-20} {(r.Parsed ? "" : "(no JSON) ")}" + string.Join(" ", Score.Fields.Select(f => (ok[f] ? "✓" : "✗") + f)) + $"  {secs:F0} s");
    }
    int n = expected.Count;
    var summary = $"**{model}** ({(mode == "direct" ? "photo → fields in one step" : "transcript → fields")}): " +
                  string.Join(", ", Score.Fields.Select(f => $"{f} {hits[f]}/{n}")) + $" · **all fields right on {hits.Values.Sum()}/{n * Score.Fields.Length}** · {totalSecs / n:F0} s per receipt";
    var report = $"{summary}\n\n{table}";
    var outFile = Path.Combine(AppContext.BaseDirectory, $"result-{mode}-{model.Replace(':', '-').Replace('/', '-')}.md");
    File.WriteAllText(outFile, report);
    Console.WriteLine($"\n{report}\nSaved to {outFile}");
    return 0;
}

Console.WriteLine("Usage: dotnet run -- selftest | transcribe [--vision MODEL] | direct MODEL | extract MODEL");
return 1;
