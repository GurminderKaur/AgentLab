# Step 2c: real bills (PDF, Word, photo): run log

Raw output from my own runs on my laptop (Dell Inspiron 16 Plus, 16 GB RAM, CPU only, Ollama), copied as it came out. Folder paths are shortened to `…`. All data in these runs is fictional.

## Coffee receipt photo: `qwen3:4b` decides, Gemma 3 looks at the photo

```
PS …\AgentLab\05-real-bills\BillsAgent> dotnet run -- qwen3:4b "File my coffee receipt from October."
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

A: Your coffee receipt from Chinook Coffee House (Oct 6, 2026) has been suggested for the Bills & Receipts folder. Please approve to move it.

── Result ───────────────────────────────
Skills loaded: filing-receipts · agent took 299 s
Suggested: IMG_2207.jpg → Bills & Receipts   (Chinook Coffee House, receipt, Oct 6, 2026, $16.96)
(Practice run: nothing is moved.)
```

## Phone bill PDF: `qwen3:4b` (suggested before reading once; the tool refused it)

```
PS …\AgentLab\05-real-bills\BillsAgent> dotnet run -- qwen3:4b "File my September phone bill."
Practice Downloads: 39 files (PDF, Word, photo, text) · skills: filing-bills, filing-receipts

You: File my September phone bill.

  📚 load_skill("filing-bills")
  🔎 search_files("phone bill September")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")
  📄 read_file("document(3).pdf")
  📦 suggest_move("document(3).pdf" → "Bills & Receipts")

A: The file `document(3).pdf` is a September phone bill from Chinook Mobile with statement date Sep 14, 2026. I suggest moving it to **Bills & Receipts**.


The September phone bill (Chinook Mobile, statement Sep 14, 2026) has been identified and is ready to move to **Bills & Receipts**. Please approve to complete filing.

── Result ───────────────────────────────
Skills loaded: filing-bills · agent took 376 s
Suggested: document(3).pdf → Bills & Receipts   (Chinook Mobile, phone bill, statement Sep 14, 2026)
(Practice run: nothing is moved.)
```

## After Step 3: `gemma4:e4b` does both jobs (sees the photo and decides)

```
PS …\AgentLab\05-real-bills\BillsAgent> dotnet run -- gemma4:e4b "File my coffee receipt from October." --vision gemma4:e4b
Practice Downloads (PDF, Word, photo, text) · skills: filing-bills, filing-receipts
  🖼  looking at IMG_2207.jpg… 51 s
     Chinook Coffee House
     Receipt
     Oct 6, 2026
     Total: $16.96

You: File my coffee receipt from October.   [gemma4:e4b]

  📚 load_skill("filing-receipts")
  🔎 search_files("coffee receipt October")
  📄 read_file("IMG_2207.jpg")
  📦 suggest_move("IMG_2207.jpg" → "Bills & Receipts")

A: I found the coffee receipt from Chinook Coffee House and suggested moving it to the Bills & Receipts folder. Please let me know if you approve this move.

── Result ───────────────────────────────
Skills loaded: filing-receipts · agent took 58 s
Suggested: IMG_2207.jpg → Bills & Receipts   (Chinook Coffee House, receipt, Oct 6, 2026, $16.96)
(Practice run: nothing is moved.)
```
