// FrameworkAgent: the same agent as 01-mini-agent, but Microsoft.Extensions.AI runs the tool loop.
// You describe C# methods as tools; the framework sends them to the model, runs the ones it asks for,
// and repeats until the model answers. The "read every file first" guardrail is still YOUR code.

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OllamaSharp;

var model = args.Length > 0 ? args[0] : "qwen3:4b";
var question = args.Length > 1 ? args[1] : "Which file is my phone bill, and how much is due?";

// ── Same practice folder and fictional files as Step 1.1 ───────────────────
var sandbox = Path.Combine(AppContext.BaseDirectory, "practice-folder");
if (!Directory.Exists(sandbox))
{
    Directory.CreateDirectory(sandbox);
    File.WriteAllText(Path.Combine(sandbox, "document(7).txt"), "SAMPLE - FICTIONAL\nChinook Mobile\nStatement date: Oct 14, 2026\nAmount due: $85.12\nDue by: Nov 4, 2026");
    File.WriteAllText(Path.Combine(sandbox, "notes.txt"), "COMP 2510 - Assignment 3 due Nov 2. Remember to email the TA.");
    File.WriteAllText(Path.Combine(sandbox, "recipe.txt"), "Pan de muerto: flour, butter, eggs, orange zest, anise.");
}
var filesRead = new HashSet<string>();

// ── Tools are plain C# methods. The framework turns name + description + parameters into the JSON the model sees. ──
[Description("List the file names in the user's practice folder.")]
string ListFiles()
{
    Console.WriteLine("  model asks for list_files()");
    return string.Join("\n", Directory.GetFiles(sandbox).Select(Path.GetFileName));
}

[Description("Read the text of one file from the practice folder.")]
string ReadFile([Description("File name, e.g. notes.txt")] string name)
{
    Console.WriteLine($"  model asks for read_file(\"{name}\")");
    if (name.Length == 0 || name != Path.GetFileName(name)) return "Error: give just a file name.";   // same safety rule as Step 1.1
    var path = Path.Combine(sandbox, name);
    if (!File.Exists(path)) return $"Error: no file named {name}.";
    filesRead.Add(name);
    var text = File.ReadAllText(path);
    return text.Length > 2000 ? text[..2000] : text;
}

// ── The framework: an Ollama chat client, wrapped so it runs tool calls automatically ──
// Small "thinking" models on a laptop CPU can take minutes per turn; OllamaSharp's default wait is only 100 s.
IChatClient client = new ChatClientBuilder(new OllamaApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) }, model))
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 8)   // same hard limit as before
    .Build();

var options = new ChatOptions
{
    Temperature = 0,
    Tools = [AIFunctionFactory.Create(ListFiles, "list_files"), AIFunctionFactory.Create(ReadFile, "read_file")],
};

List<ChatMessage> messages =
[
    new(ChatRole.System, "You help the user with files in their practice folder. File names don't tell you what's inside, so first call list_files, then call read_file on every file that might be relevant. Only answer after reading. Answer briefly, and only with what the files say."),
    new(ChatRole.User, question),
];
Console.WriteLine($"You: {question}\n");

// The framework runs the whole tool loop inside each GetResponseAsync call.
// Our guardrail wraps around it: if the model answers before reading everything, push back and ask again.
for (int round = 1; round <= 3; round++)
{
    var response = await client.GetResponseAsync(messages, options);
    messages.AddRange(response.Messages);

    var unread = Directory.GetFiles(sandbox).Select(Path.GetFileName).Where(f => !filesRead.Contains(f!)).ToList();
    if (unread.Count == 0)
    {
        Console.WriteLine($"\nAgent: {response.Text}");
        return;
    }
    Console.WriteLine($"  model tried to answer before reading {string.Join(", ", unread)} → pushed back");
    messages.Add(new(ChatRole.User, $"Before answering, read these files you haven't opened yet: {string.Join(", ", unread)}."));
}
Console.WriteLine("\nAgent: stopped without reading every file.");
