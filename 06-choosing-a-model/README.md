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

## Results

*Not run yet.*
