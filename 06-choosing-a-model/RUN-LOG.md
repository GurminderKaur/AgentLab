# Step 3: choosing a model: run log

Raw output from my own runs on my laptop (Dell Inspiron 16 Plus, 16 GB RAM, CPU only, Ollama), copied as it came out. Folder paths are shortened to `…`. All data in these runs is fictional.

## Full benchmark console output

`dotnet run -- bench qwen3:4b gemma4:e4b granite4:3b llama3.2:3b` in `05-real-bills/BillsAgent`.

```
PS …\AgentLab\05-real-bills\BillsAgent> dotnet run -- bench qwen3:4b gemma4:e4b granite4:3b llama3.2:3b
Benchmark: 4 model(s) × 3 tasks, same files for every run. Leave the laptop awake (it takes a while).

You: File my September phone bill.   [qwen3:4b]

  📚 load_skill("filing-bills")
  🔎 search_files("phone bill September")
  📄 read_file("document(3).pdf")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")

Assistant: The suggestion to move `document(3).pdf` to **Bills & Receipts** has been recorded. The user will need to approve or reject this move before the file is actually relocated.

  → ✅ right in 282 s

You: File my September power bill.   [qwen3:4b]

  📚 load_skill("filing-bills")
  🔎 search_files("power bill September")
  📄 read_file("enmax_statement.pdf")
  📦 suggest_move("enmax_statement.pdf" → "Home & Car")

Assistant: The file `enmax_statement.pdf` has been suggested for moving to **Home & Car** with the reason: *Prairie Power Co., power bill, statement Sep 20, 2026*. The user will need to approve this move.

  → ✅ right in 198 s

You: File my coffee receipt from October.   [qwen3:4b]

  📚 load_skill("filing-receipts")
  🔎 search_files("coffee October")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")
  📄 read_file("IMG_2207.jpg")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")

Assistant: Your coffee receipt from Chinook Coffee House on Oct 6, 2026 ($16.96) is ready to move to Bills & Receipts. Please approve this suggestion.

  → ✅ right in 322 s

You: File my September phone bill.   [gemma4:e4b]

  📚 load_skill("filing-bills")
  🔎 search_files("phone bill September")
  📄 read_file("document(3).pdf")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")

Assistant: I found and reviewed `document(3).pdf`, which is your September phone bill from Chinook Mobile. I have suggested moving it to the Bills & Receipts folder. Please let me know if you approve this move.

  → ✅ right in 80 s

You: File my September power bill.   [gemma4:e4b]

  📚 load_skill("filing-bills")
  🔎 search_files("power bill September")
  📄 read_file("enmax_statement.pdf")
  📦 suggest_move("enmax_statement.pdf" → "Home & Car")

Assistant: I have found and reviewed your September power bill, `enmax_statement.pdf`. I suggest moving it to the Home & Car folder, as per your filing rules for utility bills.

  → ✅ right in 70 s

You: File my coffee receipt from October.   [gemma4:e4b]

  📚 load_skill("filing-receipts")
  🔎 search_files("coffee receipt October")
  📄 read_file("IMG_2207.jpg")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")

Assistant: I found the coffee receipt from Chinook Coffee House and suggested moving it to the Bills & Receipts folder. Please confirm if you would like to approve this move.

  → ✅ right in 51 s

You: File my September phone bill.   [granite4:3b]

  📚 load_skill("filing-bills")
  🔎 search_files("phone bill September")
  📄 read_file("document(3).pdf")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")

Assistant: The September phone bill from **Chinook Mobile** has been identified and is ready to be moved. It matches the bill statement date of **Sep 14 2026**, so it should be filed in the **Bills & Receipts** folder. The move has been recorded and awaits your approval.

  → ✅ right in 55 s

You: File my September power bill.   [granite4:3b]

  📚 load_skill("filing-bills")
  🔎 search_files("power bill September")
  📄 read_file("enmax_statement.pdf")
  📦 suggest_move("enmax_statement.pdf" → "Home & Car")

Assistant: The September power bill from **Prairie Power Co.** has been identified and is ready to be moved. It matches the rule for **power** bills, which go into the **Home & Car** folder. The file **enmax_statement.pdf** has been suggested for this folder with the reason: *Prairie Power Co., power bill, statement Sep 20, 2026*. Please approve the move, or let me know if you’d like to adjust anything.

  → ✅ right in 27 s

You: File my coffee receipt from October.   [granite4:3b]

  🔎 search_files("coffee receipt October")
  📄 read_file("IMG_2207.jpg")
  📚 load_skill("filing-receipts")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")

Assistant: Your coffee receipt from October has been identified and is ready to be filed. It matches the description **“Chinook Coffee House | Receipt | Oct 6 2026 | $16.96.”**
I’ve suggested moving the photo **IMG_2207.jpg** to the **Bills & Receipts** folder. The move hasn’t been executed yet—please approve or let me know if you’d like to change anything.

  → ✅ right in 23 s

You: File my September phone bill.   [llama3.2:3b]

  📚 load_skill("filing-bills")

Assistant: I will suggest moving the file "Prairie Power Co., power bill, statement Sep 20, 2026" to the "Bills & Receipts" folder.

  → ❌ no suggestion in 32 s

You: File my September power bill.   [llama3.2:3b]

  📚 load_skill("filing-bills")

Assistant: I suggest moving the September power bill to the **Home & Car** folder. Prairie Power Co., power bill, statement Sep 20, 2026.

  → ❌ no suggestion in 7 s

You: File my coffee receipt from October.   [llama3.2:3b]

  🔎 search_files("coffee receipt October")

Assistant: I found a photo of your coffee receipt from October. It's stored in the IMG_2207.jpg file. I'll move it to the "Receipts" folder. Would you like me to suggest a folder for it?

  → ❌ no suggestion in 7 s
```

## The report it saved

### Step 3 benchmark (Nov 26, 2026)

Photo described once by `gemma3:4b`; each model below only makes the decisions.

| Model | Right (of 3) | Score | Avg time | RAM in Ollama | Push-backs | Skill loaded when needed | Licence |
|---|---|---|---|---|---|---|---|
| `qwen3:4b` | 3/3 | 6/6 | 267 s | 12.8 GB | 0 | 3/3 | Apache License |
| `gemma4:e4b` | 3/3 | 6/6 | 67 s | 2.6 GB | 0 | 3/3 | Apache License |
| `granite4:3b` | 3/3 | 6/6 | 35 s | 8.0 GB | 0 | 3/3 | Apache License |
| `llama3.2:3b` | 0/3 | 0/6 | 16 s | 10.1 GB | 0 | 2/3 | LLAMA 3.2 COMMUNITY LICENSE AGREEMENT |

| Model | Task | Result | Suggested | Skills loaded | Tool calls | Time |
|---|---|---|---|---|---|---|
| `qwen3:4b` | phone-bill | ✅ right | document(3).pdf → Bills & Receipts | filing-bills | 4 | 282 s |
| `qwen3:4b` | power-bill | ✅ right | enmax_statement.pdf → Home & Car | filing-bills | 4 | 198 s |
| `qwen3:4b` | coffee-photo | ✅ right | IMG_2207.jpg → Bills & Receipts | filing-receipts | 5 | 322 s |
| `gemma4:e4b` | phone-bill | ✅ right | document(3).pdf → Bills & Receipts | filing-bills | 4 | 80 s |
| `gemma4:e4b` | power-bill | ✅ right | enmax_statement.pdf → Home & Car | filing-bills | 4 | 70 s |
| `gemma4:e4b` | coffee-photo | ✅ right | IMG_2207.jpg → Bills & Receipts | filing-receipts | 4 | 51 s |
| `granite4:3b` | phone-bill | ✅ right | document(3).pdf → Bills & Receipts | filing-bills | 4 | 55 s |
| `granite4:3b` | power-bill | ✅ right | enmax_statement.pdf → Home & Car | filing-bills | 4 | 27 s |
| `granite4:3b` | coffee-photo | ✅ right | IMG_2207.jpg → Bills & Receipts | filing-receipts | 4 | 23 s |
| `llama3.2:3b` | phone-bill | ❌ no suggestion | – | filing-bills | 1 | 32 s |
| `llama3.2:3b` | power-bill | ❌ no suggestion | – | filing-bills | 1 | 7 s |
| `llama3.2:3b` | coffee-photo | ❌ no suggestion | – | none | 1 | 7 s |
