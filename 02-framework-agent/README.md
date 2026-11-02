# Step 1, Part 2: the same agent with an open-source framework

[Step 1.1](../01-mini-agent) wrote the agent loop by hand. Here, **[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)** (Microsoft's open-source, MIT-licensed AI building blocks for .NET) runs the loop, and **[OllamaSharp](https://github.com/awaescher/OllamaSharp)** (MIT) connects it to Ollama.

## What changed

| | Step 1.1 (by hand) | Step 1.2 (framework) |
|---|---|---|
| Describing tools | Hand-written JSON schema | Plain C# methods with `[Description]` attributes; `AIFunctionFactory.Create` builds the schema |
| The tool loop | My `for` loop: send, check `tool_calls`, run tools, send again | `UseFunctionInvocation()` does it inside one `GetResponseAsync` call |
| Step limit | My loop counter | `MaximumIterationsPerRequest = 8` |
| Swapping Ollama for another provider | Rewrite the HTTP code | Change one line: any `IChatClient` works |
| **"Read every file first" guardrail** | My code | **Still my code.** The framework doesn't know my rule, so I check after each response and push back. |

The framework removes plumbing; it doesn't make a small model reliable. That's still the job of the code around it.

## Run it

```powershell
ollama pull qwen3:4b
cd 02-framework-agent
dotnet run -- qwen3:4b "Which file is my phone bill, and how much is due?"
```

## Result (real run, my laptop, `qwen3:4b`)

```
You: Which file is my phone bill, and how much is due?

  model asks for list_files()
  model asks for read_file("notes.txt")
  model asks for read_file("document(7).txt")
  model tried to answer before reading recipe.txt → pushed back
  model asks for read_file("recipe.txt")

Agent: The phone bill is in `document(7).txt` with an amount due of **$85.12**.
```

- The framework ran the whole tool loop: four tool calls inside two `GetResponseAsync` calls, and no loop code of mine.
- **The guardrail still mattered.** The model found the bill but tried to answer before reading `recipe.txt`, and my check pushed back. The framework has no idea what "read everything first" means for my task.
- Compared with Step 1.1, the model read more files on its own this time (2 of 3 before answering, vs 1 of 3), but it still wasn't thorough without the push-back.

**Build note:** this first run was on the .NET 8 SDK and showed warning `CS9057` (OllamaSharp's source generator was built for a newer C# compiler). The project now targets **.NET 10**, whose compiler is new enough, so the warning is gone. (.NET 8 and 9 stop getting security fixes on November 10, 2026.)
