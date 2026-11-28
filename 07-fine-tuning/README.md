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

Results: *not run yet.*
