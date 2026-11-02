# Step 1, Part 1: MiniAgent

**An AI agent in about 100 lines of C#, with no packages.** A small local model (through Ollama) answers a question about files by deciding which tools to call. My code runs the tools and decides what's allowed.

## What an agent actually is

An app that *calls* a model asks one question and gets one answer. An **agent** lets the model choose the next step, in a loop:

```
You:   "Which file is my phone bill, and how much is due?"
Model: I'll call list_files            ← the model chooses a tool
Code:  runs it → "document(7).txt, notes.txt, recipe.txt"
Model: I'll call read_file("document(7).txt")
Code:  runs it → "Chinook Mobile … Amount due: $85.12"
Model: "It's document(7).txt: $85.12, due Nov 4."   ← no more tools: done
```

The model never touches files. It only **asks**; the C# code checks each request (plain file names only, practice folder only, at most 8 steps).

## What went wrong, and the guardrails that fixed it

I ran it on my Windows laptop with two small open-weight models. The first version failed in two different ways:

| Model | What it did | Problem |
|---|---|---|
| `qwen2.5:3b` | Answered "none of the files mention an assignment" **without calling any tool** | Claimed something it never checked |
| `qwen3:4b` | Listed the files, read **only** `notes.txt`, then said "none of the files contain a phone bill" | Made a claim about files it hadn't read |

A better prompt alone wasn't enough, so the code now enforces two rules:

1. **Look before you answer:** an answer from a model that hasn't read anything is rejected, and the model is told to use the tools.
2. **Read everything before saying "it's not there":** until every file in the (small) folder has been read, the code pushes back and names the files still unread.

## Results after the guardrails (real runs, my laptop)

| | `qwen3:4b` | `qwen2.5:3b` |
|---|---|---|
| Used tools without being told | ✅ | ✅ |
| Tried to answer too early | after reading 1 of 3 files | after reading 0 of 3 files |
| Guardrail pushed back | ✅ | ✅ |
| Final answer | ✅ `document(7).txt`, $85.12, due Nov 4 | ✅ `document(7).txt`, $85.12, due Nov 4 |

After the push-back, both models asked for **several tools in one step** (parallel tool calls); the loop handles that by running every call in the list.

**The lesson:** with small models, reliability comes from the code around the model (limits, guardrails, checks), not from trusting the model.

**The limit:** "read every file" only works for a small folder. Step 1 Part 3 needs a better tool design for a real Downloads folder.

## Run it

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download) and [Ollama](https://ollama.com).

```powershell
ollama pull qwen3:4b          # or qwen2.5:3b; the model must support tool calling (Gemma 3 doesn't in Ollama)
cd 01-mini-agent
dotnet run -- qwen3:4b "Which file is my phone bill, and how much is due?"
dotnet run -- qwen3:4b "When is my assignment due?"
```

The first run creates `practice-folder/` with three fictional files.
