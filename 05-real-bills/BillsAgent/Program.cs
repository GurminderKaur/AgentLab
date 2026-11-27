// Bills Agent (Steps 2c and 3): the skills agent from Step 2b, for REAL file types.
// PDFs and Word files are read as text; photos are looked at ONCE by a vision model (Gemma 3), and that description
// is what search and read_file use. One model looks (gemma3:4b), another decides (qwen3:4b by default).
//   dotnet run -- selftest
//   dotnet run -- qwen3:4b "File my coffee receipt from October."
//   dotnet run -- bench qwen3:4b gemma4:e4b granite4:3b llama3.2:3b      (Step 3: compare models)

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OllamaSharp;
using BillsAgent;
using SkillAgent;
using SlothAssistant;

var baseDir = AppContext.BaseDirectory;
var photo = File.ReadAllBytes(Path.Combine(baseDir, "samples", "coffee-receipt-photo.jpg"));
if (args.Length > 0 && args[0] == "selftest") return Library.SelfTest(photo) + Bench.SelfTest() + SlothAssistant.Downloads.SelfTest();

string Opt(string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
var visionModel = Opt("--vision", "gemma3:4b");
var rest = args.Where((a, i) => a != "--vision" && (i == 0 || args[i - 1] != "--vision")).ToArray();
var root = Path.Combine(baseDir, "practice-downloads");
var library = new Library(Path.Combine(baseDir, "photo-notes.json")); // what the vision model saw, kept between runs
var skills = Skills.LoadAll(Path.Combine(baseDir, "skills"));

// Small "thinking" models on a laptop CPU can take minutes per turn; OllamaSharp's default wait is only 100 s.
HttpClient Http() => new() { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(15) };

// ── Look at new photos once, with the vision model ──────────────────────────
async Task<bool> LookAtPhotos()
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);   // fresh folder every run
    Library.MakePractice(root, photo);
    IChatClient eyes = new OllamaApiClient(Http(), visionModel);
    try
    {
        await library.LookAtPhotosAsync(root, async (image, mediaType) =>
        {
            var msg = new ChatMessage(ChatRole.User, [new TextContent(Library.PhotoPrompt), new DataContent(image, mediaType)]);
            return (await eyes.GetResponseAsync([msg], new ChatOptions { Temperature = 0 })).Text;
        });
        return true;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        Console.WriteLine($"\nCouldn't reach the vision model ({ex.GetType().Name}). Is Ollama running, and is '{visionModel}' pulled?");
        return false;
    }
}

// ── One agent run: tools + skills + guardrails (as in Step 2b) ──────────────
async Task<RunResult> RunAgent(string model, string request, string taskId = "")
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);   // same starting files for every run
    Library.MakePractice(root, photo);
    var d = new Downloads(root) { ReadText = library.Read };
    var result = new RunResult { Model = model, TaskId = taskId };

    // Each tool counts its calls and prints what the model asked for; the real work is in the tested Tools.cs.
    string Search(string query) { result.ToolCalls++; Console.WriteLine($"  🔎 search_files(\"{query}\")"); return d.Search(query); }
    string Read(string name) { result.ToolCalls++; Console.WriteLine($"  📄 read_file(\"{name}\")"); return d.ReadFile(name); }
    string Suggest(string name, string folder, string reason) { result.ToolCalls++; Console.WriteLine($"  📦 suggest_move(\"{name}\" → \"{folder}\")"); return d.Suggest(name, folder, reason); }
    string LoadSkill(string name) { result.ToolCalls++; Console.WriteLine($"  📚 load_skill(\"{name}\")"); result.Loaded.Add(name); return Skills.Load(skills, name); }

    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(([Description("A few words to look for")] string query) => Search(query), "search_files",
            "Search inside all files in the Downloads folder (PDFs, Word files, photo descriptions, text) for words, e.g. 'power bill September'."),
        AIFunctionFactory.Create(([Description("Exact file name")] string name) => Read(name), "read_file",
            "Read one file's text (for a photo: what the vision model saw). Use the exact file name from search results."),
        AIFunctionFactory.Create(([Description("Exact file name")] string name,
                                  [Description("One of: Bills & Receipts, Banking & Taxes, Work, School, Travel, Medical, Home & Car, IDs & Personal, Photos, Other")] string folder,
                                  [Description("Short reason")] string reason) => Suggest(name, folder, reason), "suggest_move",
            "Suggest moving a file you have READ into a folder. Nothing moves until the user approves."),
        AIFunctionFactory.Create(([Description("Exact skill name, e.g. filing-bills")] string name) => LoadSkill(name), "load_skill",
            "Load the full instructions of one skill from the skill list, by its exact name."),
    };

    IChatClient client = new ChatClientBuilder(new OllamaApiClient(Http(), model))
        .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 12)
        .Build();
    var options = new ChatOptions { Temperature = 0, Tools = tools };
    List<ChatMessage> messages =
    [
        new(ChatRole.System, $"""
            You help tidy the user's Downloads folder. It holds PDFs, Word files, photos and text files, and file names are
            often meaningless (document(3).pdf, IMG_2207.jpg), so: search_files, read_file the promising results, check the
            details match the request, then suggest_move. Never guess from a file name. Finish with one or two sentences.

            The user has written SKILLS: their own rules for certain jobs. Before doing a job that matches a skill below,
            call load_skill with its name and follow its instructions; they override your own assumptions.
            Available skills (name: when to use it):
            {Skills.Index(skills)}
            """),
        new(ChatRole.User, request),
    ];

    Console.WriteLine($"\nYou: {request}   [{model}]\n");
    var started = DateTime.Now;
    try
    {
        var response = await client.GetResponseAsync(messages, options);
        messages.AddRange(response.Messages);
        // GUARDRAIL (Step 2b): describing a choice in words isn't a suggestion; only suggest_move records one.
        if (d.Suggestions.Count == 0 && d.Read.Count > 0)
        {
            result.Pushbacks++;
            Console.WriteLine("  model described a choice without calling suggest_move → pushed back");
            messages.Add(new(ChatRole.User, "If a file matches, call the suggest_move tool now to record it (don't write the call as text). If nothing matches, say so."));
            response = await client.GetResponseAsync(messages, options);
        }
        Console.WriteLine($"\nAssistant: {response.Text.Trim()}\n");
    }
    catch (Exception ex)
    {
        result.Error = ex is HttpRequestException or TaskCanceledException ? $"no answer from Ollama ({ex.GetType().Name})" : ex.Message.Split('\n')[0];
        Console.WriteLine($"\n  ✗ {result.Error}\n");
    }
    result.Seconds = (DateTime.Now - started).TotalSeconds;
    result.Suggestions.AddRange(d.Suggestions);
    return result;
}

// ── What Ollama knows about a model: is it pulled, its licence, and how much RAM it took ──
async Task<string?> LicenceOf(string model)
{
    using var http = Http();
    var resp = await http.PostAsync("/api/show", new StringContent(JsonSerializer.Serialize(new { model }), Encoding.UTF8, "application/json"));
    if (!resp.IsSuccessStatusCode) return null;   // not pulled
    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    return Bench.LicenceName(doc.RootElement.TryGetProperty("license", out var l) ? l.GetString() : null);
}
async Task<double> RamGbOf(string model)
{
    try
    {
        using var http = Http();
        using var doc = JsonDocument.Parse(await http.GetStringAsync("/api/ps"));
        foreach (var m in doc.RootElement.GetProperty("models").EnumerateArray())
            if (m.GetProperty("name").GetString() is { } n && (n == model || n == model + ":latest"))
                return m.GetProperty("size").GetInt64() / 1e9;
    }
    catch { }
    return 0;
}

// ── Step 3: the benchmark ───────────────────────────────────────────────────
if (rest.Length > 0 && rest[0] == "bench")
{
    var models = rest.Skip(1).DefaultIfEmpty("qwen3:4b").ToList();
    Console.WriteLine($"Benchmark: {models.Count} model(s) × {Bench.Tasks.Length} tasks, same files for every run. Leave the laptop awake (it takes a while).");
    if (!await LookAtPhotos()) return 1;   // one photo description (by the vision model) shared by every model, so only the decision-maker differs
    var runs = new List<RunResult>();
    var info = new Dictionary<string, (double, string)>();
    foreach (var m in models)
    {
        var lic = await LicenceOf(m);
        if (lic == null) { Console.WriteLine($"\n⚠ {m} isn't pulled. Run: ollama pull {m}   (skipping it)"); continue; }
        double ram = 0;
        foreach (var t in Bench.Tasks)
        {
            var r = await RunAgent(m, t.Request, t.Id);
            ram = Math.Max(ram, await RamGbOf(m));
            runs.Add(r);
            Console.WriteLine($"  → {Bench.Score(t, r).Label} in {r.Seconds:F0} s");
        }
        info[m] = (ram, lic);
    }
    var report = $"# Step 3 benchmark ({DateTime.Now:MMM d, yyyy})\n\nPhoto described once by `{visionModel}`; each model below only makes the decisions.\n\n" + Bench.Report(runs, info);
    var file = Path.Combine(baseDir, "bench-results.md");
    File.WriteAllText(file, report);
    Console.WriteLine("\n" + report + $"\nSaved to {file}");
    return 0;
}

// ── A single task (Step 2c) ─────────────────────────────────────────────────
var model = rest.Length > 0 ? rest[0] : "qwen3:4b";
var request = rest.Length > 1 ? rest[1] : "File my coffee receipt from October.";
Console.WriteLine($"Practice Downloads (PDF, Word, photo, text) · skills: {string.Join(", ", skills.Select(s => s.Name))}");
if (!await LookAtPhotos()) return 1;
var run = await RunAgent(model, request);
Console.WriteLine("── Result ───────────────────────────────");
Console.WriteLine($"Skills loaded: {(run.Loaded.Count == 0 ? "none" : string.Join(", ", run.Loaded))} · agent took {run.Seconds:F0} s{(run.Error != null ? " · " + run.Error : "")}");
if (run.Suggestions.Count == 0) Console.WriteLine("No moves suggested.");
foreach (var (file, folder, reason) in run.Suggestions) Console.WriteLine($"Suggested: {file} → {folder}   ({reason})");
Console.WriteLine("(Practice run: nothing is moved.)");
return run.Error == null ? 0 : 1;
