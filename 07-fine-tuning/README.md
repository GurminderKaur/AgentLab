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