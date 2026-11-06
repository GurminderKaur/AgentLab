# AGENTS.md: house rules for coding agents in this repo

This repo is a learning notebook: one folder per step (`01-mini-agent`, `02-framework-agent`, `03-sloth-assistant`, …). Each step is a small C# console app that talks to a **local** model through Ollama.

## Always
- **C# on .NET 10** (`<TargetFramework>net10.0</TargetFramework>`), nullable and implicit usings on.
- **Stay local.** Models run through Ollama at `http://localhost:11434`. Never add a cloud AI API, telemetry, or anything that sends data off the computer.
- **Few packages.** No new NuGet packages unless the step is about that package. Agent steps may use `Microsoft.Extensions.AI` and `OllamaSharp`; nothing else without asking.
- **Fictional data only.** Sample files start with `SAMPLE - FICTIONAL`. Never use real names, accounts, or documents.
- **The model suggests, the person decides.** Code never deletes or overwrites a user's file; moves add ` (2)` instead of overwriting; tools accept plain file names only (no paths, no `..`).
- **Keep it small and readable.** This is for learning: short files, comments that explain *why*, no clever abstractions.

## Before you finish any change
1. Build: `dotnet build <step folder> -c Release` with no new warnings.
2. If the step has a self-test (`dotnet run -- selftest`), run it and make sure it passes. Every change in behaviour gets a new check (see the `add-selftest-check` skill).
3. Update the step's README if what it does changed. **Never invent results**: real results come only from runs on my laptop.

## Don't
- Don't change other steps' folders when working on one step.
- Don't rename public tool names the model sees (`search_files`, `read_file`, `suggest_move`) without updating the README.
