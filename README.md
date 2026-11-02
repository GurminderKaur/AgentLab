# AgentLab

[![Build](https://github.com/GurminderKaur/AgentLab/actions/workflows/build.yml/badge.svg)](https://github.com/GurminderKaur/AgentLab/actions/workflows/build.yml)

**My hands-on notebook for learning AI agents and fine-tuning with small open-weight models, in C#/.NET, on a laptop.**

In October 2026 I built four local-first apps with open models ([portfolio](https://gurminderkaur.github.io)). They each call a model for one job. This repo is about the next level: models that use tools to finish multi-step tasks, reusable instructions for agents, choosing models, and fine-tuning. Everything runs locally through [Ollama](https://ollama.com), with no cloud and no credit card.

| Step | What I'm learning | Folder | Status |
|---|---|---|---|
| 1 | **Agents with tools** | | |
| 1.1 | An agent loop by hand, with guardrails | [`01-mini-agent`](01-mini-agent) | ✅ Works with `qwen3:4b` and `qwen2.5:3b` |
| 1.2 | The same agent with an open-source .NET AI framework (Microsoft.Extensions.AI) | [`02-framework-agent`](02-framework-agent) | ✅ Works with `qwen3:4b`; guardrail still needed |
| 1.3 | A real task: an assistant for my Sloth Sort app | | Next |
| 2 | **Skills:** reusable instructions for a coding agent (OpenCode + a local model) | | |
| 3 | **Choosing a model:** 3 small models on the same test: accuracy, speed, licence | | |
| 4 | **Fine-tuning** a 1–3B model on fictional receipts (free Colab GPU), measured against the baseline | | |

Each step's folder has a README with what I tried, what went wrong, and real results.

All sample data is fictional. Code is MIT licensed; each model has its own licence (noted per step).
