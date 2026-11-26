// Bills Agent (Step 2c): the skills agent from Step 2b, now for REAL file types.
// PDFs and Word files are read as text; photos are looked at ONCE by a vision model (Gemma 3), and that description
// is what search and read_file use. Two local models: gemma3:4b looks, qwen3:4b decides.
//   dotnet run -- selftest
//   dotnet run -- qwen3:4b "File my coffee receipt from October."
//   dotnet run -- qwen3:4b "File my September phone bill."

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using BillsAgent;
using SkillAgent;
using SlothAssistant;

var baseDir = AppContext.BaseDirectory;
var photo = File.ReadAllBytes(Path.Combine(baseDir, "samples", "coffee-receipt-photo.jpg"));
if (args.Length > 0 && args[0] == "selftest") return Library.SelfTest(photo) + SlothAssistant.Downloads.SelfTest();

string Opt(string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
var visionModel = Opt("--vision", "gemma3:4b");
var rest = args.Where((a, i) => a != "--vision" && (i == 0 || args[i - 1] != "--vision")).ToArray();
var model = rest.Length > 0 ? rest[0] : "qwen3:4b";
var request = rest.Length > 1 ? rest[1] : "File my coffee receipt from October.";

var root = Path.Combine(baseDir, "practice-downloads");
if (Directory.Exists(root)) Directory.Delete(root, recursive: true);   // fresh folder every run
Library.MakePractice(root, photo);
var library = new Library(Path.Combine(baseDir, "photo-notes.json")); // what the vision model saw, kept between runs
var d = new Downloads(root) { ReadText = library.Read };
var skills = Skills.LoadAll(Path.Combine(baseDir, "skills"));
var loaded = new List<string>();
Console.WriteLine($"Practice Downloads: {Directory.GetFiles(root).Length} files (PDF, Word, photo, text) · skills: {string.Join(", ", skills.Select(s => s.Name))}");

// Small "thinking" models on a laptop CPU can take minutes per turn; OllamaSharp's default wait is only 100 s.
HttpClient Http() => new() { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(15) };

// ── 1. Look at new photos once, with the vision model ───────────────────────
IChatClient eyes = new OllamaApiClient(Http(), visionModel);
async Task<string> Describe(byte[] image, string mediaType)
{
    var msg = new ChatMessage(ChatRole.User, [new TextContent(Library.PhotoPrompt), new DataContent(image, mediaType)]);
    var r = await eyes.GetResponseAsync([msg], new ChatOptions { Temperature = 0 });
    return r.Text;
}
try { await library.LookAtPhotosAsync(root, Describe); }
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    Console.WriteLine($"\nCouldn't reach the vision model ({ex.GetType().Name}). Is Ollama running, and is '{visionModel}' pulled?");
    return 1;
}

// ── 2. The agent, with tools and skills (same as Step 2b) ───────────────────
Console.WriteLine($"\nYou: {request}\n");

[Description("Search inside all files in the Downloads folder (PDFs, Word files, photo descriptions, text) for words, e.g. 'power bill September'.")]
string search_files([Description("A few words to look for")] string query)
{ Console.WriteLine($"  🔎 search_files(\"{query}\")"); return d.Search(query); }

[Description("Read one file's text (for a photo: what the vision model saw). Use the exact file name from search results.")]
string read_file([Description("Exact file name")] string name)
{ Console.WriteLine($"  📄 read_file(\"{name}\")"); return d.ReadFile(name); }

[Description("Suggest moving a file you have READ into a folder. Nothing moves until the user approves.")]
string suggest_move([Description("Exact file name")] string name,
                    [Description("One of: Bills & Receipts, Banking & Taxes, Work, School, Travel, Medical, Home & Car, IDs & Personal, Photos, Other")] string folder,
                    [Description("Short reason")] string reason)
{ Console.WriteLine($"  📦 suggest_move(\"{name}\" → \"{folder}\")"); return d.Suggest(name, folder, reason); }

[Description("Load the full instructions of one skill from the skill list, by its exact name.")]
string load_skill([Description("Exact skill name, e.g. filing-bills")] string name)
{ Console.WriteLine($"  📚 load_skill(\"{name}\")"); loaded.Add(name); return Skills.Load(skills, name); }

IChatClient client = new ChatClientBuilder(new OllamaApiClient(Http(), model))
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 12)
    .Build();
var options = new ChatOptions
{
    Temperature = 0,
    Tools = [AIFunctionFactory.Create(search_files, "search_files"), AIFunctionFactory.Create(read_file, "read_file"),
             AIFunctionFactory.Create(suggest_move, "suggest_move"), AIFunctionFactory.Create(load_skill, "load_skill")],
};
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

var started = DateTime.Now;
ChatResponse response;
try
{
    response = await client.GetResponseAsync(messages, options);
    messages.AddRange(response.Messages);
    // GUARDRAIL (from Step 2b): describing a choice in words isn't a suggestion; only suggest_move records one.
    if (d.Suggestions.Count == 0 && d.Read.Count > 0)
    {
        Console.WriteLine("  model described a choice without calling suggest_move → pushed back");
        messages.Add(new(ChatRole.User, "If a file matches, call the suggest_move tool now to record it (don't write the call as text). If nothing matches, say so."));
        response = await client.GetResponseAsync(messages, options);
    }
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    Console.WriteLine($"\nCouldn't get an answer from Ollama ({ex.GetType().Name}). Is Ollama running, and is '{model}' pulled?");
    return 1;
}
Console.WriteLine($"\nAssistant: {response.Text}\n");

Console.WriteLine("── Result ───────────────────────────────");
Console.WriteLine($"Skills loaded: {(loaded.Count == 0 ? "none" : string.Join(", ", loaded))} · agent took {(DateTime.Now - started).TotalSeconds:F0} s");
if (d.Suggestions.Count == 0) Console.WriteLine("No moves suggested.");
foreach (var (file, folder, reason) in d.Suggestions) Console.WriteLine($"Suggested: {file} → {folder}   ({reason})");
Console.WriteLine("(Practice run: nothing is moved.)");
return 0;
