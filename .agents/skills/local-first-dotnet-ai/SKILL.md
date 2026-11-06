---
name: local-first-dotnet-ai
description: Use when adding or changing a feature, tool or agent in Gurminder's local-first C#/.NET apps that call a local model through Ollama (AgentLab, Sloth Sort, Shoebox, Marigold, TrailBirder). Covers project rules, model calls, guardrails, sample data and how to verify the change.
---

# Local-first .NET + Ollama: how these projects are built

## Project rules
- .NET 10 (`net10.0`). Console apps or ASP.NET Core minimal APIs.
- No new NuGet packages unless the task asks for one. Prefer the base class library.
- The model runs locally through Ollama (`http://localhost:11434`). Nothing the user owns leaves the machine. Web servers bind to `127.0.0.1` only.
- Keep logic that can be tested without a model (tools, parsing, checks) in its own file, separate from model calls.

## Calling the model
- Short structured answers (a folder, a date, a few fields): JSON schema in the request's `format`, temperature 0.
- Long prose (a paragraph or more): plain text, not JSON. Forced JSON made a 4B model run past its length limit.
- Timeouts of at least 10 minutes. Small "thinking" models on a laptop CPU can take minutes per turn.
- Catch Ollama errors and show a short, friendly message, never a stack trace.

## Never trust the model alone
- Check its output in code: arithmetic on receipts, the ghost check for invented names and dates, "read before you answer" guardrails.
- The model may only suggest risky actions. A person approves moves and writes. Never delete. Never overwrite: add " (2)".
- Agent tools take plain file names only. Refuse paths, "..", and anything outside the app's folder.
- Tools should do the heavy work in code (search inside files, match synonyms and month abbreviations) and return why something matched, so a small model isn't drowning in noise.

## Data
- Sample data is fictional and starts with `SAMPLE - FICTIONAL`. Never put real personal documents, photos or names in the repo.

## Definition of done
1. Every new rule or tool has a check in the project's self-test (`dotnet run -- selftest`).
2. `dotnet build` succeeds with no new warnings, and the self-test passes.
3. The README says what changed and shows real results if a model was run (including failures).
4. Report what you changed, what you verified, and anything you could not verify.
