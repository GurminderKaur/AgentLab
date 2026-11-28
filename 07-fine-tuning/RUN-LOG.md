# Step 4: fine-tuning: run log

Raw output from my own runs on my laptop (Dell Inspiron 16 Plus, 16 GB RAM, CPU only, Ollama), copied as it came out. Folder paths are shortened to `…`. All data in these runs is fictional. Colab training hasn't run yet: no free GPU was available on Nov 28, 2026.

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
