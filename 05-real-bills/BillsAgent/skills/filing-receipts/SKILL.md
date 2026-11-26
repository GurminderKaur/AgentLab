---
name: filing-receipts
description: "How I file receipts (coffee, groceries, fuel, office supplies), including photos of receipts. Use whenever filing a receipt."
---

# Filing receipts: my rules

1. **Search** with the store or kind of purchase and the month, e.g. `coffee receipt October`. Photos of receipts are searchable too.
2. **Read** the file. For a photo, the text is a description written by a vision model: check that the store and the date match the request.
3. **Folder:** always **Bills & Receipts**.
4. **Reason** for `suggest_move`: `<Store>, receipt, <date>, <total>`, e.g. `Chinook Coffee House, receipt, Oct 6, 2026, $16.96`.
5. If the photo's description is unclear or the date doesn't match, don't suggest it; say what you found instead.
