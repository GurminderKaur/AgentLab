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

## Run it

Needs the **.NET 10 SDK** and Ollama with a tool-capable model.

```powershell
cd 03-sloth-assistant
dotnet run -- selftest                 # checks the tools; no model needed
dotnet run -- qwen3:4b "Find my September phone bill and file it."
```

The practice folder (`practice-downloads`) is created next to the program on first run; delete it to start fresh.
