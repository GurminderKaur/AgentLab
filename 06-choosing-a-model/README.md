# Step 3: choosing a model

Which small open model should make the decisions in my agent? Instead of guessing from benchmarks on the internet, I test candidates **on my own task, on my own laptop**.

## The test

The [Bills Agent](../05-real-bills/BillsAgent) from Step 2c gets a `bench` command that runs **3 tasks with known right answers** on each model, with the same practice files every time:

| Task | Right answer | What it tests |
|---|---|---|
| "File my September phone bill." | `document(3).pdf` → Bills & Receipts | Search inside PDFs; check the statement date (the October bill is a trap) |
| "File my September power bill." | `enmax_statement.pdf` → **Home & Car** | Load the `filing-bills` skill and follow my personal rule |
| "File my coffee receipt from October." | `IMG_2207.jpg` → Bills & Receipts | Find a **photo** by its content |

Scoring ([`Bench.cs`](../05-real-bills/BillsAgent/Bench.cs)): 2 points for exactly the right file and folder, 1 for the right file in the wrong folder (or with extra suggestions), 0 otherwise. The photo is described once by `gemma3:4b` and shared, so only the decision-making model changes.

It also records time, the RAM Ollama reports for the model, how often a guardrail had to push back, whether the right skill was loaded, and the **licence** (read from Ollama with `/api/show`).

## Candidates

All have Ollama's `tools` capability and fit in 16 GB of RAM:

| Model | Size | Why it's here | Licence (check before use) |
|---|---|---|---|
| `qwen3:4b` | 4B | My baseline from Steps 1–2c ("thinking" model) | Apache 2.0 |
| `gemma4:e4b` | ~4B effective | Newer; **tools and vision in one model**, so it could replace the two-model setup | Apache 2.0 (earlier Gemmas used the Gemma Terms of Use) |
| `granite4:3b` | 3B | IBM; no "thinking", so possibly much faster | Apache 2.0 |
| `llama3.2:3b` | 3B | Very common; a different licence to compare | Llama 3.2 Community License (custom terms, not Apache) |

A licence trap I found on the way: **`qwen2.5:3b`** (used in Step 1.1) is under the **Qwen Research License**, for research and evaluation only, while other Qwen sizes and generations use other licences. Licences can differ by size within one family, so check the exact model, not the family.

## Run it

```powershell
ollama pull qwen3:4b
ollama pull gemma4:e4b
ollama pull granite4:3b
ollama pull llama3.2:3b
cd 05-real-bills/BillsAgent
dotnet run -- bench qwen3:4b gemma4:e4b granite4:3b llama3.2:3b
```

12 agent runs; expect roughly an hour on a laptop CPU. Keep the laptop awake. The table is saved to `bench-results.md` next to the program.

## Results (Nov 26, 2026, my laptop: 16 GB RAM, CPU only)

Photo described once by `gemma3:4b`; each model below only makes the decisions.

| Model | Right (of 3) | Score | Avg time | RAM in Ollama | Push-backs | Skill loaded when needed | Licence |
|---|---|---|---|---|---|---|---|
| `qwen3:4b` | 3/3 | 6/6 | 267 s | 12.8 GB | 0 | 3/3 | Apache License |
| `gemma4:e4b` | 3/3 | 6/6 | **67 s** | **2.6 GB** | 0 | 3/3 | Apache License |
| `granite4:3b` | 3/3 | 6/6 | **35 s** | 8.0 GB | 0 | 3/3 | Apache License |
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

### What I read from it

- **Three models were equally accurate**, so the choice comes down to speed, memory, licence and abilities. Choosing a model is a trade-off, not a contest.
- **`gemma4:e4b` is the best fit for this agent:** perfect score, 4× faster than `qwen3:4b`, the least RAM, Apache 2.0, and it can **see images too**, so it could do both jobs (read the photo and decide) and replace the two-model setup.
- **`granite4:3b` is the fastest** (35 s, about 8× faster than `qwen3:4b`) if vision isn't needed. In the photo task it loaded the skill *after* reading the file, but still followed it.
- **`qwen3:4b` is accurate but slow**: it "thinks" before every step. Once (coffee task) it tried to suggest before reading and the tool refused, as in Step 2c.
- **`llama3.2:3b` failed every task, quickly.** It barely used the tools, and on the *phone* bill task it said it would move "Prairie Power Co., power bill, statement Sep 20, 2026": that's the **example text from my skill**, not a file. A weak model can **copy a skill's example** instead of doing the work.
- **A gap in my guardrail:** the "record it, don't just describe it" push-back only fired after the model had read a file, so `llama3.2:3b`, which never read anything, slipped past. Fixed after this run: it now also pushes back when the model answers without reading anything.

### Limits of this test
- 3 tasks, one run each: a small sample. A model can be lucky or unlucky once.
- "Thinking" models vary a little from run to run, even at temperature 0.
- Times and RAM are for **my laptop and my Ollama settings** (RAM depends on the context length and each model's design); with a GPU, results and even the ranking could change.
- Easy tasks for three of the four models: harder tasks (more files, vaguer requests) would separate them better.
