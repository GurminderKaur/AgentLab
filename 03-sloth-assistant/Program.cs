// Sloth Assistant: ask in plain words ("find my September phone bill and file it").
// The model searches and reads with tools, then SUGGESTS moves. Nothing moves until you type y.

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using SlothAssistant;

if (args.Length > 0 && args[0] == "selftest") return Downloads.SelfTest();

var model = args.Length > 0 ? args[0] : "qwen3:4b";
var request = args.Length > 1 ? args[1] : "Find my September phone bill and file it.";

var root = Path.Combine(AppContext.BaseDirectory, "practice-downloads");
if (!Directory.Exists(root)) Downloads.MakePractice(root);
var d = new Downloads(root);
Console.WriteLine($"Practice Downloads folder: {Directory.GetFiles(root).Length} files\nYou: {request}\n");

// ── Tools: thin wrappers that print what the model asked for, then call the tested code in Tools.cs ──
[Description("Search inside all files in the Downloads folder (and their names) for words, e.g. 'Chinook statement' or 'bill September'. Returns matching file names with the line that matched.")]
string search_files([Description("A few words to look for")] string query)
{ Console.WriteLine($"  🔎 search_files(\"{query}\")"); return d.Search(query); }

[Description("Read one file's text. Use the exact file name from search results.")]
string read_file([Description("Exact file name")] string name)
{ Console.WriteLine($"  📄 read_file(\"{name}\")"); return d.ReadFile(name); }

[Description("Suggest moving a file you have READ into a folder. This does not move anything; the user approves later.")]
string suggest_move([Description("Exact file name")] string name,
                    [Description("One of: Bills & Receipts, Banking & Taxes, Work, School, Travel, Medical, Home & Car, IDs & Personal, Photos, Other")] string folder,
                    [Description("Short reason, e.g. 'Chinook Mobile bill, statement date Sep 14, 2026'")] string reason)
{ Console.WriteLine($"  📦 suggest_move(\"{name}\" → \"{folder}\")"); return d.Suggest(name, folder, reason); }

// Small "thinking" models on a laptop CPU can take minutes per turn; OllamaSharp's default wait is only 100 s.
IChatClient client = new ChatClientBuilder(new OllamaApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) }, model))
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 10)
    .Build();

var options = new ChatOptions
{
    Temperature = 0,
    Tools = [AIFunctionFactory.Create(search_files), AIFunctionFactory.Create(read_file), AIFunctionFactory.Create(suggest_move)],
};
List<ChatMessage> messages =
[
    new(ChatRole.System, """
        You help tidy the user's Downloads folder. File names are often meaningless (document(3).txt), so:
        1. search_files with a few useful words, 2. read_file on the promising results, 3. check the details
        (company, dates) match the request, 4. suggest_move only for files that really match.
        Never guess from a file name. If nothing matches, say so. Finish with one or two sentences on what you suggested and why.
        """),
    new(ChatRole.User, request),
];

var started = DateTime.Now;
ChatResponse response;
try { response = await client.GetResponseAsync(messages, options); }
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    Console.WriteLine($"\nCouldn't get an answer from Ollama ({ex.GetType().Name}). Is the Ollama app running, and is '{model}' pulled?");
    return 1;
}
messages.AddRange(response.Messages);

// GUARDRAIL: "nothing matches" is only allowed after actually reading some search results.
if (d.Suggestions.Count == 0 && d.Read.Count == 0 && d.Searched)
{
    Console.WriteLine("  model gave up without reading any search results → pushed back");
    messages.Add(new(ChatRole.User, "You haven't read any of the files your search found. Read the top 3 results (exact names), check company and dates, then decide."));
    response = await client.GetResponseAsync(messages, options);
}
Console.WriteLine($"\nAssistant: {response.Text}\n\n(took {(DateTime.Now - started).TotalSeconds:F0} s on this computer)\n");

// ── Human in the loop: the only place anything moves ────────────────────────
if (d.Suggestions.Count == 0) { Console.WriteLine("No moves suggested."); return 0; }
Console.WriteLine("Suggested moves:");
foreach (var (file, folder, reason) in d.Suggestions) Console.WriteLine($"  {file} → {folder}   ({reason})");
Console.Write("\nMove these files? (y/n) ");
if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
    foreach (var line in d.Apply()) Console.WriteLine($"  moved: {line}");
else
    Console.WriteLine("Nothing moved.");
return 0;
