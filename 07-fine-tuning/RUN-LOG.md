# Step 4: fine-tuning: run log

Raw output from my own runs on my laptop (Dell Inspiron 16 Plus, 16 GB RAM, CPU only, Ollama), copied as it came out. Folder paths are shortened to `…`. All data in these runs is fictional.

## Part 4.1: Gemma 4 straight from the photo (`direct`)

```
PS …\AgentLab\07-fine-tuning\ReceiptBench> dotnet run -- direct gemma4:e4b
01_fuel.jpg          ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  49 s
02_coffee.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  35 s
03_office.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  36 s
04_courier.jpg       ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  37 s
05_bc_print.jpg      ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  39 s
06_groceries.jpg     ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  40 s

gemma4:e4b (photo → fields in one step): vendor 6/6, date 6/6, subtotal 6/6, gst 6/6, other_tax 6/6, tip 6/6, total 6/6 · all fields right on 42/42 · 39 s per receipt
```

## Part 4.1: Gemma 4 from the transcript (`extract`)

```
PS …\AgentLab\07-fine-tuning\ReceiptBench> dotnet run -- extract gemma4:e4b
01_fuel.jpg          ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  47 s
02_coffee.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  30 s
03_office.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  29 s
04_courier.jpg       ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  29 s
05_bc_print.jpg      ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  28 s
06_groceries.jpg     ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  25 s

gemma4:e4b (transcript → fields): vendor 6/6, date 6/6, subtotal 6/6, gst 6/6, other_tax 6/6, tip 6/6, total 6/6 · all fields right on 42/42 · 31 s per receipt
```

## Part 4.1: Gemma 3 270M before any training (`extract`)

```
PS …\AgentLab\07-fine-tuning\ReceiptBench> dotnet run -- extract gemma3:270m
01_fuel.jpg          ✗vendor ✗date ✗subtotal ✗gst ✓other_tax ✓tip ✗total  4 s
02_coffee.jpg        ✗vendor ✗date ✗subtotal ✗gst ✓other_tax ✗tip ✗total  1 s
03_office.jpg        ✗vendor ✗date ✗subtotal ✗gst ✓other_tax ✓tip ✗total  1 s
04_courier.jpg       ✗vendor ✗date ✗subtotal ✗gst ✓other_tax ✓tip ✗total  1 s
05_bc_print.jpg      ✗vendor ✗date ✗subtotal ✗gst ✗other_tax ✓tip ✗total  1 s
06_groceries.jpg     ✗vendor ✗date ✗subtotal ✓gst ✓other_tax ✓tip ✗total  1 s

gemma3:270m (transcript → fields): vendor 0/6, date 0/6, subtotal 0/6, gst 1/6, other_tax 5/6, tip 5/6, total 0/6 · all fields right on 11/42 · 1 s per receipt
```

What the 270M model actually answered, per receipt:

| Receipt | vendor | date | subtotal | gst | other_tax | tip | total | Time |
|---|---|---|---|---|---|---|---|---|
| 01_fuel.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ❌ `0` | ✅ | ✅ | ❌ `0` | 4 s |
| 02_coffee.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ❌ `0` | ✅ | ❌ `0` | ❌ `0` | 1 s |
| 03_office.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ❌ `0` | ✅ | ✅ | ❌ `0` | 1 s |
| 04_courier.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ❌ `0` | ✅ | ✅ | ❌ `0` | 1 s |
| 05_bc_print.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ❌ `0` | ❌ `0` | ✅ | ❌ `0` | 1 s |
| 06_groceries.jpg | ❌ `...` | ❌ `YYYY-MM-DD` | ❌ `0` | ✅ | ✅ | ✅ | ❌ `0` | 1 s |

It copied the example shape from the prompt, so its "right" fields are only the ones whose correct answer happened to be 0.

The photo transcripts Gemma 4 made (input for `extract`) are in [`ReceiptBench/samples/transcripts.json`](ReceiptBench/samples/transcripts.json).

## Part 4.3: training on Colab (free T4 GPU, Nov 28, 2026)

From [`colab/run-2026-11-28.ipynb`](colab/run-2026-11-28.ipynb) (full outputs there).

```
2000 training receipts, 200 held out

==((====))==  Unsloth 2026.11.12: Fast Gemma3 patching. Transformers: 4.57.6.
   Tesla T4. Num GPUs = 1. Max memory: 14.563 GB. Platform: Linux.
Unsloth: Using float16 precision for gemma3 won't work! Using float32.
Unsloth: QLoRA and full finetuning all not selected. Switching to 16bit LoRA.
```

Before training (50 held-out receipts):

~~~~
model said: ```json
{"vendor": "...", "date": "YYYY-MM-DD", "subtotal": 0.00, "gst": 0.00, "other_tax": 0.00, "tip": 0.00, "total": 0.00}
```
right was: {"vendor": "SIGNAL PRINT & COPY", "date": "2026-10-08", "subtotal": 392.12, "gst": 19.61, "other_tax": 23.53, "tip": 0.0, "total": 435.26}

vendor 3/50, date 2/50, subtotal 0/50, gst 8/50, other_tax 35/50, tip 48/50, total 0/50 · all fields right on 96/350
~~~~

Training:

```
Unsloth: Packing is on by default: 2000 documents -> 340 packed rows.
   Num examples = 340 | Num Epochs = 2 | Total steps = 680
   Trainable parameters = 30,375,936 of 298,474,112 (10.18% trained)

 Step  Training loss
   25  0.084400
   50  0.003900
   75  0.001400
  100  0.002900
  125  0.000700
  150  0.000000
  175  0.000000
  200  0.000000
  225  0.000000
  250  0.000000
  275  0.000000
  300  0.000000
  325  0.000000
  350  0.000000
  375  0.000000
  400  0.000000
  425  0.000000
  450  0.000000
  475  0.000000
  500  0.000000
  525  0.000000
  550  0.000000
  575  0.000000
  600  0.000000
  625  0.000000
  650  0.000000
  675  0.000000

Trained in 10.5 minutes
```

After training (all 200 held-out receipts):

```
model said: {"vendor": "SIGNAL PRINT & COPY", "date": "2026-10-08", "subtotal": 392.12, "gst": 19.61, "other_tax": 23.53, "tip": 0.0, "total": 435.26}
right was:  {"vendor": "SIGNAL PRINT & COPY", "date": "2026-10-08", "subtotal": 392.12, "gst": 19.61, "other_tax": 23.53, "tip": 0.0, "total": 435.26}

vendor 200/200, date 200/200, subtotal 200/200, gst 200/200, other_tax 200/200, tip 200/200, total 200/200 · all fields right on 1400/1400
```

Export: `receipt_reader_gguf/gemma-3-270m-it.Q8_0.gguf` → `receipt-reader-Q8_0.gguf`, 292 MB.

## Part 4.4: the fine-tuned model (`receipt-reader`), after training on Colab

```
PS …\AgentLab\07-fine-tuning> ollama create receipt-reader -f Modelfile
gathering model components
parsing GGUF
using existing layer sha256:f284987ff481254efade476a890d699a559c90cc6557cf2e47ac402db9c2a79d
using autodetected template gemma3-instruct
using existing layer sha256:d3a76cb8c4a07d0a6c82ac6e839f98816b5077699d393b2cc77008c16d8078ac
writing manifest
success
PS …\AgentLab\07-fine-tuning> cd ReceiptBench
PS …\AgentLab\07-fine-tuning\ReceiptBench> dotnet run -- extract receipt-reader
01_fuel.jpg          ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  6 s
02_coffee.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  1 s
03_office.jpg        ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  1 s
04_courier.jpg       ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  1 s
05_bc_print.jpg      ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  1 s
06_groceries.jpg     ✓vendor ✓date ✓subtotal ✓gst ✓other_tax ✓tip ✓total  1 s

receipt-reader (transcript → fields): vendor 6/6, date 6/6, subtotal 6/6, gst 6/6, other_tax 6/6, tip 6/6, total 6/6 · all fields right on 42/42 · 2 s per receipt
```
