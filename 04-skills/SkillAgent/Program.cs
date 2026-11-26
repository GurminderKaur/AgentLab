// Skill Agent: the Sloth Sort assistant (Step 1.3) plus SKILLS.
// The model sees only skill names + descriptions, and calls load_skill to read a recipe when it needs one.
//   dotnet run -- qwen3:4b "File my September power bill."               (with skills)
//   dotnet run -- qwen3:4b "File my September power bill." --no-skills   (without, for comparison)

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using SkillAgent;
using SlothAssistant;

var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");
if (args.Length > 0 && args[0] == "selftest") return Skills.SelfTest(skillsDir);

bool useSkills = !args.Contains("--no-skills");
var rest = args.Where(a => a != "--no-skills").ToArray();
var model = rest.Length > 0 ? rest[0] : "qwen3:4b";
var request = rest.Length > 1 ? rest[1] : "File my September power bill.";

var root = Path.Combine(AppContext.BaseDirectory, "practice-downloads");
if (Directory.Exists(root)) Directory.Delete(root, recursive: true);   // fresh folder every run, so runs are comparable
Downloads.MakePractice(root);
var d = new Downloads(root);
var skills = useSkills ? Skills.LoadAll(skillsDir) : new List<Skill>();
var loaded = new List<string>();
Console.WriteLine($"Practice Downloads: {Directory.GetFiles(root).Length} files · skills: {(useSkills ? string.Join(", ", skills.Select(s => s.Name)) : "OFF")}\nYou: {request}\n");

[Description("Search inside all files in the Downloads folder (and their names) for words, e.g. 'Chinook statement' or 'power bill September'.")]
string search_files([Description("A few words to look for")] string query)
{ Console.WriteLine($"  🔎 search_files(\"{query}\")"); return d.Search(query); }

[Description("Read one file's text. Use the exact file name from search results.")]
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

// Small "thinking" models on a laptop CPU can take minutes per turn; OllamaSharp's default wait is only 100 s.
IChatClient client = new ChatClientBuilder(new OllamaApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(15) }, model))
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 12)
    .Build();

var tools = new List<AITool> { AIFunctionFactory.Create(search_files, "search_files"), AIFunctionFactory.Create(read_file, "read_file"), AIFunctionFactory.Create(suggest_move, "suggest_move") };
var system = """
    You help tidy the user's Downloads folder. File names are often meaningless (document(3).txt), so:
    search_files, read_file the promising results, check the details match the request, then suggest_move.
    Never guess from a file name. Finish with one or two sentences on what you suggested and why.
    """;
if (useSkills)
{
    tools.Add(AIFunctionFactory.Create(load_skill, "load_skill"));
    system += $"""

        The user has written SKILLS: their own rules for certain jobs. Before doing a job that matches a skill below,
        call load_skill with its name and follow its instructions; they override your own assumptions.
        Available skills (name: when to use it):
        {Skills.Index(skills)}
        """;
}

var options = new ChatOptions { Temperature = 0, Tools = tools };
List<ChatMessage> messages = [new(ChatRole.System, system), new(ChatRole.User, request)];

var started = DateTime.Now;
ChatResponse response;
try { response = await client.GetResponseAsync(messages, options); }
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    Console.WriteLine($"\nCouldn't get an answer from Ollama ({ex.GetType().Name}). Is Ollama running, and is '{model}' pulled?");
    return 1;
}
messages.AddRange(response.Messages);

// GUARDRAIL: "I suggest moving X" in words isn't a suggestion; only a suggest_move call records one.
// (In my first runs the model described its choice, or wrote the call as text, without calling the tool.)
if (d.Suggestions.Count == 0 && d.Read.Count > 0)
{
    Console.WriteLine("  model described a choice without calling suggest_move → pushed back");
    messages.Add(new(ChatRole.User, "If a file matches, call the suggest_move tool now to record it (don't write the call as text). If nothing matches, say so."));
    response = await client.GetResponseAsync(messages, options);
}
Console.WriteLine($"\nAssistant: {response.Text}\n");

// ── A short report, so runs with and without skills are easy to compare ──
Console.WriteLine("── Result ───────────────────────────────");
Console.WriteLine($"Skills: {(useSkills ? "on" : "off")} · loaded: {(loaded.Count == 0 ? "none" : string.Join(", ", loaded))} · took {(DateTime.Now - started).TotalSeconds:F0} s");
if (d.Suggestions.Count == 0) Console.WriteLine("No moves suggested.");
foreach (var (file, folder, reason) in d.Suggestions) Console.WriteLine($"Suggested: {file} → {folder}   ({reason})");
Console.WriteLine("(Practice run: nothing is moved.)");
return 0;
