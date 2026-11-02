# Step 1, Part 3: an assistant for Sloth Sort

Ask in plain words: **"Find my September phone bill and file it."** A small local model uses tools to search, read and check, then **suggests** a move. Nothing moves until you type `y`.

## The problem from Part 1, and the fix

Part 1's guardrail was "read every file before answering". That works for 3 files, not a real Downloads folder. This practice folder has **40 files**: two phone bills a month apart, a September power bill, and lots of saved web pages.

The fix is **better tools**, not more reading:

| Tool | What the model gets | Why |
|---|---|---|
| `search_files(words)` | Matching file names **plus the line that matched** | Plain C# searches inside all 40 files in milliseconds; the model only reads the promising ones |
| `read_file(name)` | The file's text | To check details (company, date) before deciding |
| `suggest_move(name, folder, reason)` | "Suggestion recorded" | **Can't move anything.** Refuses a file the model hasn't read, and any folder not on the list |

Then the program shows the suggestions and asks **you**. Only `y` moves files, and a move never overwrites (it adds `(2)`).

The trap in the test folder: `document(3).txt` (Chinook Mobile, Sep 14) and `document(7).txt` (Chinook Mobile, Oct 14) look the same until you read the statement date, and `enmax_statement.txt` is a September bill but for power, not a phone.

## First real run: what went wrong, and the fixes

With `qwen3:4b`, the first version **gave up after one search**:

```
🔎 search_files("phone bill September")
Assistant: The search results show no files containing both "phone bill" and "September"…
I cannot suggest moving any file.
```

Two causes, and only one was the model's fault:

1. **My search didn't speak the user's language.** The bill says "Chinook **Mobile**… Statement date: **Sep** 14", not "phone" or "September". So every bill matched just 1 of 3 words, tied with ten saved web pages about "hikes near Canmore this September". The results were mostly noise.
2. **The model gave up without reading anything**, the same laziness as in Step 1.1.

Fixes:
- **Search with aliases and whole words:** `phone` also matches mobile/wireless/cell, `bill` matches statement/invoice/amount due, `September` matches Sep/Sept (but not "separately"). Results are ranked by how many of the user's ideas they match, with up to 3 matching lines shown. Now the September phone bill ranks first at 3 of 3, the October phone bill and the September power bill are next at 2 of 3, and the hiking pages sink to the bottom.
- **A guardrail:** "nothing matches" is only accepted after reading some search results; otherwise the program pushes back once.

**Lesson:** for an agent, tool design matters as much as the model. A tool that returns noise makes a small model look dumber than it is.

## Run it

Needs the **.NET 10 SDK** and Ollama with a tool-capable model.

```powershell
cd 03-sloth-assistant
dotnet run -- selftest                 # checks the tools; no model needed
dotnet run -- qwen3:4b "Find my September phone bill and file it."
```

The practice folder (`practice-downloads`) is created next to the program on first run; delete it to start fresh.
