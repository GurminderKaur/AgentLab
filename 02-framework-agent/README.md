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
