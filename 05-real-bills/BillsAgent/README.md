# Step 2c: real bills (PDFs, Word files and photos)

Real bills arrive as **PDFs**, and receipts are often **photos**. Until now the practice files were plain `.txt`, so the agent's `search_files` and `read_file` would have seen gibberish (a PDF) or nothing (a photo). This step teaches the same agent to read real file types, locally.

| File type | How the agent "reads" it | AI involved? |
|---|---|---|
| Digital PDF (e-bills, statements) | Text pulled out of the PDF by plain C# ([`TextExtract.cs`](TextExtract.cs), adapted from my [Sloth Sort](https://github.com/GurminderKaur/SlothSort) app, no packages) | No |
| Word / Excel / PowerPoint | Same: they're zip files of XML | No |
| Photo (JPG/PNG) of a receipt | **Gemma 3 4B vision** looks at it **once** and writes 3–6 lines (store, kind, dates, total). That description is what search and read use, and it's remembered (`photo-notes.json`) so the photo is never sent twice | Yes: vision model |
| Scanned PDF (an image inside a PDF) | Honestly reported as "no readable text" | Not yet (see below) |

In this step, two local models worked together: **`gemma3:4b` looked** (it can see images but can't call tools in Ollama) and **`qwen3:4b` decided** (it can call tools but can't see images). **Since Step 3, the default is one model for both, `gemma4:e4b`** ([why](../../06-choosing-a-model)); the commands below pass the models explicitly to reproduce the original runs.

The practice folder ([`Library.cs`](Library.cs)) is fictional and in real formats: compressed PDF bills (two Chinook Mobile phone bills a month apart, a power bill, a bank statement), a lease as a `.docx`, a "scanned" PDF with no text, a **photo of a coffee receipt** (`IMG_2207.jpg`, a fictional sample from my [Shoebox](https://github.com/GurminderKaur/ShoeBox) app), and 30 saved web pages as noise.

Skills: `filing-bills` (reused from Step 2b) and a new [`filing-receipts`](skills/filing-receipts/SKILL.md).

## Run it

```powershell
ollama pull gemma3:4b     # if not already there (it is, from Shoebox and Marigold)
cd 05-real-bills/BillsAgent
dotnet run -- selftest
dotnet run -- qwen3:4b "File my coffee receipt from October." --vision gemma3:4b
dotnet run -- qwen3:4b "File my September phone bill." --vision gemma3:4b
dotnet run -- gemma4:e4b "File my coffee receipt from October."   # since Step 3: gemma4:e4b does both jobs
```

The first run looks at the photo with Gemma (about a minute on a laptop CPU); later runs reuse what it saw.

## Results (my laptop, 16 GB RAM, CPU only)

**"File my coffee receipt from October."** The only match is a photo, `IMG_2207.jpg`.

```
Practice Downloads: 39 files (PDF, Word, photo, text) · skills: filing-bills, filing-receipts
  🖼  looking at IMG_2207.jpg… 38 s
     Chinook Coffee House
     Receipt
     Oct 6, 2026
     $16.96

You: File my coffee receipt from October.

  📚 load_skill("filing-receipts")
  🔎 search_files("coffee October")
  📄 read_file("IMG_2207.jpg")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")

── Result ───────────────────────────────
Skills loaded: filing-receipts · agent took 299 s
Suggested: IMG_2207.jpg → Bills & Receipts   (Chinook Coffee House, receipt, Oct 6, 2026, $16.96)
```

- ✅ **Gemma 3 read the photo correctly**: store, kind of document, date and total all match the receipt, with nothing invented. 38 s, once (later runs reuse `photo-notes.json`).
- ✅ The agent loaded the **right** skill (`filing-receipts`, not `filing-bills`).
- ✅ It found the photo by **what's in it** ("coffee October"), not by its meaningless name.
- ✅ Read before suggesting; right folder; reason in my skill's exact format. No push-back needed.
- ⏱ 38 s to look + 299 s for the agent, on a laptop CPU.

**"File my September phone bill."** The answer is a PDF, `document(3).pdf`.

```
  📚 load_skill("filing-bills")
  🔎 search_files("phone bill September")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")     ← refused: not read yet
  📄 read_file("document(3).pdf")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")     ← accepted
Suggested: document(3).pdf → Bills & Receipts   (Chinook Mobile, phone bill, statement Sep 14, 2026)
agent took 376 s
```

- ✅ The PDF was read as text, with no AI involved in the reading.
- ✅ No second look at the photo: the description came from `photo-notes.json`.
- ✅ **The tool guardrail from Step 1.3 caught a real mistake:** the model tried to suggest before reading, `suggest_move` refused ("read it first"), and the model then read the PDF, checked the date and suggested again.

**Two models, each doing one job:** Gemma 3 (vision, can't use tools) turned the photo into four lines of text; Qwen3 (tools, can't see) worked only from that text.

## Not covered yet: scanned PDFs

A scanned PDF is a picture inside a PDF. A model can't open a PDF, so each page would first have to be rendered to an image, and then sent to Gemma like a photo. Rendering PDF pages needs a PDF engine (for example PDFium through a NuGet package): the first third-party package this project would need for reading files.
