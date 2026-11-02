// MiniAgent: the smallest useful AI agent, in one file, with no packages.
// A local model (through Ollama) answers a question by deciding which tools to call.
// The tools can only READ files inside one practice folder; the model never touches anything itself.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var model = args.Length > 0 ? args[0] : "qwen2.5:3b";   // a small model that supports tool calling
var question = args.Length > 1 ? args[1] : "Which file is my phone bill, and how much is due?";
var http = new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) };

// ── A practice folder with fictional files (created on first run) ─────────
var sandbox = Path.Combine(AppContext.BaseDirectory, "practice-folder");
if (!Directory.Exists(sandbox))
{
    Directory.CreateDirectory(sandbox);
    File.WriteAllText(Path.Combine(sandbox, "document(7).txt"), "SAMPLE - FICTIONAL\nChinook Mobile\nStatement date: Oct 14, 2026\nAmount due: $85.12\nDue by: Nov 4, 2026");
    File.WriteAllText(Path.Combine(sandbox, "notes.txt"), "COMP 2510 - Assignment 3 due Nov 2. Remember to email the TA.");
    File.WriteAllText(Path.Combine(sandbox, "recipe.txt"), "Pan de muerto: flour, butter, eggs, orange zest, anise.");
}

// ── The tools: what the model may ASK for. Your code decides what actually happens. ──
var tools = JsonNode.Parse("""
[
  { "type": "function", "function": {
      "name": "list_files",
      "description": "List the file names in the user's practice folder.",
      "parameters": { "type": "object", "properties": {}, "required": [] } } },
  { "type": "function", "function": {
      "name": "read_file",
      "description": "Read the text of one file from the practice folder.",
      "parameters": { "type": "object",
        "properties": { "name": { "type": "string", "description": "File name, e.g. notes.txt" } },
        "required": ["name"] } } }
]
""")!;

string RunTool(string name, JsonNode? input)
{
    switch (name)
    {
        case "list_files":
            return string.Join("\n", Directory.GetFiles(sandbox).Select(Path.GetFileName));
        case "read_file":
            var file = input?["name"]?.GetValue<string>() ?? "";
            // Safety: only a plain file name inside the practice folder. No paths, no "..".
            if (file.Length == 0 || file != Path.GetFileName(file)) return "Error: give just a file name.";
            var path = Path.Combine(sandbox, file);
            if (!File.Exists(path)) return $"Error: no file named {file}.";
            var text = File.ReadAllText(path);
            return text.Length > 2000 ? text[..2000] : text;
        default:
            return $"Error: there is no tool called {name}.";
    }
}

// ── The agent loop ────────────────────────────────────────────────────────
var messages = new JsonArray
{
    new JsonObject { ["role"] = "system", ["content"] = "You help the user with files in their practice folder. File names don't tell you what's inside, so first call list_files, then call read_file on every file that might be relevant. Only answer after reading. Answer briefly, and only with what the files say." },
    new JsonObject { ["role"] = "user", ["content"] = question },
};
Console.WriteLine($"You: {question}\n");

var filesRead = new HashSet<string>();    // the guardrails below use this
for (int step = 1; step <= 8; step++)   // a hard limit, so a confused model can't loop forever
{
    var body = new JsonObject { ["model"] = model, ["stream"] = false, ["messages"] = messages.DeepClone(), ["tools"] = tools.DeepClone(),
                                ["options"] = new JsonObject { ["temperature"] = 0 } };
    var resp = await http.PostAsync("/api/chat", new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
    var raw = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode) { Console.WriteLine($"Ollama error: {raw}"); return; }

    var reply = JsonNode.Parse(raw)!["message"]!;
    messages.Add(reply.DeepClone());                       // remember what the model said / asked for

    var calls = reply["tool_calls"]?.AsArray();
    if (calls == null || calls.Count == 0)                 // no tool requested → the model wants to answer
    {
        // GUARDRAIL: no answer until every file has been read. A model that read one file
        // can't honestly say "none of the files…". (Fine for a small folder; Part 3 shows a better way for big ones.)
        var unread = Directory.GetFiles(sandbox).Select(Path.GetFileName).Where(f => !filesRead.Contains(f!)).ToList();
        if (unread.Count > 0)
        {
            Console.WriteLine($"  step {step}: model tried to answer before reading {string.Join(", ", unread)} → pushed back");
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = $"Before answering, read these files you haven't opened yet: {string.Join(", ", unread)}." });
            continue;
        }
        Console.WriteLine($"Agent: {reply["content"]}");
        return;
    }
    foreach (var call in calls)
    {
        var name = call!["function"]!["name"]!.GetValue<string>();
        var toolArgs = call["function"]!["arguments"];
        Console.WriteLine($"  step {step}: model asks for {name}({toolArgs?.ToJsonString() ?? ""})");
        var result = RunTool(name, toolArgs);
        if (name == "read_file" && !result.StartsWith("Error")) filesRead.Add(toolArgs?["name"]?.GetValue<string>() ?? "");
        Console.WriteLine($"          tool returns: {result.Replace("\n", " | ")}");
        messages.Add(new JsonObject { ["role"] = "tool", ["tool_name"] = name, ["content"] = result });
    }
}
Console.WriteLine("Agent: stopped after 8 steps without an answer.");
