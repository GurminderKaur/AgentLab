# Step 4: fine-tuning

**Question:** can a *tiny* model, trained on examples of exactly one job, beat a bigger general model at that job?

**The job:** Shoebox's weak spot. Reading 6 fictional sample receipts, Gemma 3 got GST right on only 4. Shoebox's README records why, and neither error was about *seeing*:
- **Courier receipt:** it **copied the subtotal** ($25.00) into GST instead of $1.25.
- **Grocery receipt:** the receipt shows **$0.00 GST** (groceries are zero-rated), but it wrote **$1.87**, exactly 5%: it **assumed** Alberta GST.

Shoebox's prompt already said "0 if no GST/HST is shown", so better prompting had been tried. These are *habits*, and changing habits with many examples is what fine-tuning is for.

## The plan

| Part | What | Where |
|---|---|---|
| **4.1** | **Measure first.** [`ReceiptBench`](ReceiptBench) turns each receipt photo into text once (a vision model transcribes it), then asks a model for the fields and scores every one against the known answers. | My laptop |
| 4.2 | **Training data:** ~2,000 fictional receipt texts with correct answers (zero-rated groceries, tips, out-of-province tax, many layouts). Never the 6 test receipts: training on the test would be cheating. | Script |
| 4.3 | **Fine-tune** Gemma 3 270M (a tiny model Google made for task-specific fine-tuning) with LoRA on a free Google Colab GPU, and export it for Ollama. | Browser |
| 4.4 | **Measure again** with the same test and the same prompt. | My laptop |

The prompt is in one shared file, [`prompt.txt`](prompt.txt), used for testing *and* training: a fine-tuned model must be tested with the instructions it learned with.

## Part 4.1: the "before" numbers

```powershell
cd 07-fine-tuning/ReceiptBench
dotnet run -- selftest
dotnet run -- transcribe                # photo → text with gemma4:e4b, once (≈1 min per receipt)
dotnet run -- direct gemma4:e4b         # photo → fields in one step, like Shoebox
dotnet run -- extract gemma4:e4b        # text → fields, big general model
ollama pull gemma3:270m
dotnet run -- extract gemma3:270m       # text → fields, the tiny model BEFORE training
```

### Results (my laptop, CPU only)

| Setup | All fields right | GST | Time per receipt | Size on disk |
|---|---|---|---|---|
| `gemma4:e4b`, photo → fields in one step (Shoebox's way) | **42/42** | **6/6** | 39 s | ~3 GB |
| `gemma4:e4b`, transcript → fields | **42/42** | **6/6** | 31 s | ~3 GB |
| `gemma3:270m`, transcript → fields, **before training** | 11/42 | 1/6 | **1 s** | **~0.3 GB** |

**Finding 1: Shoebox's GST problem is already solved, by a newer model.** Gemma 4 read all six receipts perfectly, including the courier receipt and the zero-rated groceries that Gemma 3 got wrong. Lesson: before training anything, check whether a better model (Step 3) fixes the problem. It's cheaper.

**Finding 2: the tiny model's 11/42 is really zero.** It copied the example shape from the prompt: `"..."` as the vendor, `"YYYY-MM-DD"` as the date, `0` for every amount. Its "right" fields are zeros that happened to be correct (no tip, no other tax, zero GST on groceries).

**So the question for fine-tuning becomes:** can a model **about 10× smaller and 30× faster** be trained to match the big model's 42/42? That's the usual reason teams fine-tune: big-model quality at small-model cost and speed.

The transcripts Gemma 4 wrote are saved in [`samples/transcripts.json`](ReceiptBench/samples/transcripts.json), so every text-only test uses exactly the same input.

## Part 4.2: training data

[`data/make_training_data.py`](data/make_training_data.py) writes **2,000 training** and **200 held-out** fictional receipt texts, in the style of Gemma 4's real transcripts, each with the correct answer:

- 7 provinces: Alberta GST 5%, BC GST + PST, Saskatchewan and Manitoba GST + PST/RST, **Ontario and Nova Scotia HST** (goes in `gst`), **Quebec GST + QST**.
- 8 kinds of business. **Groceries are zero-rated**: GST 0.00 printed, or no tax line at all (378 zero-GST receipts in total).
- Tips (357), several labels for each line (`GST 5%`, `G.S.T.`, `GST/HST`, `SUB-TOTAL`, `AMOUNT DUE`…), and the six date styles seen in the real transcripts (`10/03/2026`, `Oct 6, 2026`, `29 SEP 2026`…).

Checks the script makes before saving:
- every answer adds up to the cent (subtotal + GST/HST + other tax + tip = total);
- **no test receipt text and none of the 6 test businesses appear** (no leakage);
- every example starts with exactly the text of `prompt.txt`, the same way ReceiptBench builds its message.

The 200 held-out examples are never trained on: they show whether the model learned the rules or memorised the examples.
## Part 4.3: training on a free Colab GPU

[![Open In Colab](https://colab.research.google.com/assets/colab-badge.svg)](https://colab.research.google.com/github/GurminderKaur/AgentLab/blob/main/07-fine-tuning/colab/fine_tune_receipt_reader.ipynb)

The notebook [`colab/fine_tune_receipt_reader.ipynb`](colab/fine_tune_receipt_reader.ipynb) follows Unsloth's official Gemma 3 270M example:

1. installs Unsloth;
2. downloads `train.jsonl` and `heldout.jsonl` from this repo (fictional data only, nothing from my laptop);
3. loads `unsloth/gemma-3-270m-it` and adds a LoRA adapter (r = 128): the original model stays frozen;
4. scores 50 held-out receipts **before** training;
5. trains for 2 passes over the 2,000 receipts, scoring only the answers (`train_on_responses_only`);
6. scores all 200 held-out receipts **after** training;
7. exports one `receipt-reader-Q8_0.gguf` (~0.3 GB) and downloads it.

Runtime: **T4 GPU**. About 15–25 minutes in total.

### What happened in my run (Nov 28, 2026, free Colab T4)

The full notebook with every output is saved as [`colab/run-2026-11-28.ipynb`](colab/run-2026-11-28.ipynb).

| | Held-out receipts | All fields right |
|---|---|---|
| Before training | 50 | **96/350** (27%): it answered `"..."`, `"YYYY-MM-DD"` and zeros, the same template-copying as on my laptop |
| **After training** | **200** | **1400/1400** (100%) |

- **Trained:** 30.4 M of 298 M numbers (10%, the LoRA adapter), 680 steps, **10.5 minutes**. Unsloth packed the 2,000 receipts into 340 rows ("packing", on by default).
- **Loss:** 0.084 at step 25, 0.004 at step 50, and **0.000 from step 150 on**. It learned the job in the first quarter of training; the rest added nothing visible. One epoch would very likely have been enough.
- **Export:** merged into the base model and converted to one 292 MB GGUF (Q8_0).

**Why 100% isn't as impressive as it looks:** the 200 held-out receipts come from the **same generator** as the training data: same layouts, same kinds of businesses, same tax rules. A perfect score shows it learned *my generator's* rules, not that it can read any receipt. The six test receipts on my laptop (Part 4.4) are a slightly harder check, because they were made separately; a real-world check would need receipts in styles the generator never produced.

The [`Modelfile`](Modelfile) turns the downloaded file into an Ollama model with Gemma 3's chat format, `temperature 0`. GGUF files are git-ignored (too big for the repo).

## Part 4.4: the trained model on my laptop

**Download:** [`receipt-reader-Q8_0.gguf`](https://github.com/GurminderKaur/AgentLab/releases/tag/receipt-reader-v1) (292 MB, Gemma Terms of Use; see the [model card](MODEL-CARD.md)).

Trained on Colab on Nov 28, 2026 ([the run, with all its outputs](colab/run-2026-11-28.ipynb)), downloaded as `receipt-reader-Q8_0.gguf`, loaded with `ollama create receipt-reader -f Modelfile`, then the same test as Part 4.1:

| Model | Size | All fields right (of 42) | Time per receipt (transcript → fields) |
|---|---|---|---|
| Gemma 3 270M, before training | ~0.3 GB | 11 (only fields whose answer was 0) | 1 s |
| **Gemma 3 270M, after training (`receipt-reader`)** | **~0.3 GB** | **42** | **~1 s** (6 s for the first, while it loads) |
| Gemma 4 E4B (no training) | ~3 GB | 42 | 31 s |

**On this job, the fine-tuned 270M model matched Gemma 4, about 30 times faster.** It learned the job from fictional receipts only: none of the 6 test receipts or their businesses were in the training data.

### What this does and doesn't show
- **Only 6 test receipts**, one run, all from the same fictional sample set. 42/42 here is not proof it's perfect; the 200 held-out receipts in the Colab notebook are the bigger check.
- **I wrote the training data after seeing these receipts' styles** (date formats, `GST 5%` lines, zero-rated groceries, BC PST). No receipt text was copied, but the training data was shaped to look like this test. A receipt in a style it never saw (a long restaurant bill, another language, a scanned crooked photo) could fail.
- **It reads text, not photos.** The 270M model only does the second half: turning a receipt's text into fields. Something still has to read the photo first (here, Gemma 4's transcripts). The win is a fast, tiny, specialised second step, not a replacement for the vision model.
- **It does one thing.** Ask it anything else and it will still try to answer with receipt JSON. That's the trade-off of fine-tuning a tiny model.
