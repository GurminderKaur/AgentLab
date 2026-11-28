# receipt-reader v1: model card

**receipt-reader**: Gemma 3 270M fine-tuned (LoRA, Unsloth, free Colab T4) to turn a receipt's **text** into JSON fields: vendor, date, subtotal, GST/HST, other tax, tip, total. Step 4 of AgentLab.

Trained only on 2,000 **fictional** receipts made by [`make_training_data.py`](https://github.com/GurminderKaur/AgentLab/blob/main/07-fine-tuning/data/make_training_data.py). No real receipts or personal data.

| | |
|---|---|
| File | `receipt-reader-Q8_0.gguf` · 291.5 MB · Q8_0 |
| SHA-256 | `f284987ff481254efade476a890d699a559c90cc6557cf2e47ac402db9c2a79d` |
| Base model | `unsloth/gemma-3-270m-it` (Google Gemma 3 270M, 268M parameters) |
| Result | 42/42 fields on 6 fictional test receipts, ~1 s each on a laptop CPU (before training: 11/42). Details and limits in [`07-fine-tuning/README.md`](https://github.com/GurminderKaur/AgentLab/blob/main/07-fine-tuning/README.md) |

**Use it:** put the file next to [`07-fine-tuning/Modelfile`](https://github.com/GurminderKaur/AgentLab/blob/main/07-fine-tuning/Modelfile) and run `ollama create receipt-reader -f Modelfile`. Give it the prompt in [`prompt.txt`](https://github.com/GurminderKaur/AgentLab/blob/main/07-fine-tuning/prompt.txt) followed by the receipt text.

**Limits:** a learning project, not a product. Tested on only 6 receipts in styles similar to its training data; it reads text, not photos; it only does this one job. Check its numbers before relying on them.

**Licence:** Gemma is provided under and subject to the Gemma Terms of Use found at https://ai.google.dev/gemma/terms. This is a modified version of Gemma 3 270M (fine-tuned by Gurminder Kaur). Use is subject to the Gemma Terms of Use and the Gemma Prohibited Use Policy (https://ai.google.dev/gemma/prohibited_use_policy). The AgentLab code is MIT; this model file is not.
