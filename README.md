# AgentLab

[![Build](https://github.com/GurminderKaur/AgentLab/actions/workflows/build.yml/badge.svg)](https://github.com/GurminderKaur/AgentLab/actions/workflows/build.yml)

**My hands-on notebook for learning AI agents and fine-tuning with small open-weight models, in C#/.NET, on a laptop.**

In October 2026 I built four local-first apps with open models ([portfolio](https://gurminderkaur.github.io)). They each call a model for one job. This repo is about the next level: models that use tools to finish multi-step tasks, reusable instructions for agents, choosing models, and fine-tuning. Everything runs locally through [Ollama](https://ollama.com), with no cloud and no credit card. All projects target **.NET 10** (LTS, supported until November 2028).

| Step | What I'm learning | Folder | Status |
|---|---|---|---|
| 1 | **Agents with tools** | | |
| 1.1 | An agent loop by hand, with guardrails | [`01-mini-agent`](01-mini-agent) | ✅ Works with `qwen3:4b` and `qwen2.5:3b` |
| 1.2 | The same agent with an open-source .NET AI framework (Microsoft.Extensions.AI) | [`02-framework-agent`](02-framework-agent) | ✅ Works with `qwen3:4b`; guardrail still needed |
| 1.3 | A real task: an assistant for my Sloth Sort app (search, read, suggest; you approve) | [`03-sloth-assistant`](03-sloth-assistant) | ✅ Found the right bill out of 40 files (185 s, laptop CPU) |
| 2 | **Skills:** reusable instructions for an agent | [`04-skills`](04-skills) | ✅ With a skill, the same model followed my filing rule (Home & Car) and my reason format; without it, it guessed |
| 2c | **Real bills:** PDFs and Word files read as text, photos described by Gemma 3 vision | [`05-real-bills`](05-real-bills/BillsAgent) | ✅ Gemma read a receipt photo correctly; the agent found and filed it by its content |
| 3 | **Choosing a model:** 4 small models on the same 3 tasks: accuracy, speed, RAM, licence | [`06-choosing-a-model`](06-choosing-a-model) | ✅ `gemma4:e4b`: 3/3, 67 s, 2.6 GB, Apache 2.0, and can see. `granite4:3b` 3/3 in 35 s. `llama3.2:3b` 0/3 |
| 4 | **Fine-tuning:** can a tiny model (Gemma 3 270M, ~0.3 GB) be trained to read receipts as well as Gemma 4 (~3 GB)? | [`07-fine-tuning`](07-fine-tuning) | 4.1 ✅ Gemma 4: 42/42 at 31 s per receipt; 270M before training: 11/42 at 1 s. 4.2 ✅ 2,000 fictional training receipts. 4.3 Colab notebook ready, to run |

Each step's folder has a README with what I tried, what went wrong, and real results.

All sample data is fictional. Code is MIT licensed; each model has its own licence (noted per step).
