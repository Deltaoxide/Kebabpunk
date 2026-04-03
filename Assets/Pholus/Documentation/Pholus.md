# Pholus - AI-Powered Performance Protection for Unity

## Table of Contents

1. [Overview](#overview)
2. [Requirements](#requirements)
3. [Installation](#installation)
4. [Setup Wizard](#setup-wizard)
5. [Getting Started](#getting-started)
6. [Analysis Modes](#analysis-modes)
7. [Understanding Results](#understanding-results)
8. [Fixing Issues](#fixing-issues)
9. [AI Providers](#ai-providers)
10. [Consensus Mode](#consensus-mode)
11. [Settings Reference](#settings-reference)
12. [Demo Scripts](#demo-scripts)
13. [Troubleshooting](#troubleshooting)
14. [Contact & Support](#contact--support)

---

## Overview

Pholus is a Unity Editor tool that uses AI to detect performance anti-patterns in your C# scripts and provides one-click fixes with diff preview. It catches common issues like uncached `GetComponent` calls, allocations in Update loops, physics calls without layer masks, and 20+ other performance pitfalls.

### Key Features

- **AI-Powered Analysis** - Uses leading AI models to understand code context, not just pattern matching
- **20+ Detection Rules** - Catches the most impactful Unity performance anti-patterns
- **One-Click Fix** - AI generates fixes with diff preview before applying
- **Multi-Provider Support** - Works with Claude, OpenAI Codex, Google Gemini, Cursor, and OpenRouter
- **Consensus Mode** - Run multiple AI providers in parallel for higher accuracy
- **Platform-Aware** - Adjusts thresholds for Mobile, PC/Console, and VR
- **Backup & Undo** - Automatic backups before every fix, with full undo support
- **Batch Scanning** - Analyze entire folders or your whole project at once

---

## Requirements

- **Unity**: 2021.3 or later
- **Node.js**: v20 or later (for CLI installation)
- **npm**: Included with Node.js
- **AI Provider Account**: At least one supported provider (see [AI Providers](#ai-providers))

---

## Installation

1. Import the Pholus package into your Unity project
2. Open Pholus via the menu: **Tools > Pholus > Open Pholus**
3. The Setup Wizard launches automatically on first use

---

## Setup Wizard

The Setup Wizard walks you through initial configuration in 4 steps. You can reopen it anytime from **Tools > Pholus > Setup Wizard**.

### Step 1: Choose Your AI Provider

Select your preferred AI provider:

| Provider | Subscription | Notes |
|----------|-------------|-------|
| **Claude Code** | Claude Pro/Max | Recommended - best accuracy, prompt caching |
| **OpenAI Codex** | ChatGPT Plus | Solid alternative |
| **Google Gemini** | Free (1000 req/day) or API key | Free tier available |
| **Cursor** | Free (limited) or Pro | Beta - requires Cursor agent |
| **OpenRouter** | Pay-per-use | Alpha - access 100+ models |

### Step 2: Install Prerequisites

Pholus verifies that **Node.js v20+** and **npm** are installed, then installs your chosen provider's CLI tool automatically.

If Node.js is not installed or outdated, the wizard provides download links and instructions for your platform.

### Step 3: Authenticate

Authenticate with your chosen provider:

- **Claude / Codex / Gemini / Cursor**: Opens a terminal to run the login command
- **OpenRouter**: Enter your API key directly in the Pholus UI (get one at [openrouter.ai/keys](https://openrouter.ai/keys))

The wizard verifies your connection works before proceeding.

### Step 4: Done

Setup is complete. You can now start analyzing scripts.

---

## Getting Started

### Quick Start (3 Steps)

1. Open Pholus: **Tools > Pholus > Open Pholus**
2. Drag and drop a C# script into the script list (or click **Add** to browse)
3. Click **Analyze**

Pholus sends your code to the AI, which returns a list of performance issues with severity ratings, explanations, and fix suggestions.

### First Analysis

After analysis completes, you'll see:

- A **score from 0-100** (100 = no issues found)
- A list of **detected issues** grouped by certainty
- **Positive notes** highlighting good practices in your code
- A **Fix** button on each issue for one-click repair

---

## Analysis Modes

### Scripts Mode (Default)

Analyze individual C# scripts:

- **Add scripts** via the Add button or drag-and-drop
- **Remove scripts** with the x button next to each entry
- Click **Analyze** to scan all selected scripts
- Results are cached for quick re-inspection

### Folder Mode

Scan an entire folder recursively:

- Click **Browse** to select a folder
- Click **Scan Folder** to analyze all `.cs` files within it
- Progress shows the current file being analyzed
- Results list all files with a summary score

### Project Mode

Scan your entire project:

- Analyzes all scripts under `Assets/`
- Automatically excludes `Plugins` and `Editor` folders
- Click **Scan Project** to begin
- Best for comprehensive audits before release

---

## Understanding Results

### Issue Certainty Levels

Issues are categorized by how confident the AI is:

| Level | Meaning | Example |
|-------|---------|---------|
| **Definite** | Almost certainly a performance issue | `GetComponent` inside `Update()` |
| **Contextual** | Depends on usage context | `Find()` in a method that might be called rarely |
| **Suggestion** | Optional improvement | Code style optimization |

### Severity Ratings

Each issue has a severity:

| Severity | Color | Score Impact | Description |
|----------|-------|--------------|-------------|
| **Critical** | Red | -15 points | Must fix - significant frame drops |
| **High** | Orange | -10 points | Should fix - noticeable impact |
| **Medium** | Yellow | -5 points | Consider fixing - minor impact |
| **Low** | Green | -2 points | Nice to fix - minimal impact |

### Issue Details

Each issue includes:

- **Title** - Short description of the problem
- **Line Number** - Where in your code the issue occurs
- **Code Snippet** - The specific problematic code
- **Explanation** - Why this is a performance issue
- **Impact** - Estimated performance cost (e.g., "~0.3ms per frame")
- **Why I Might Be Wrong** - Conditions where this might be acceptable (toggle in settings)

### What Pholus Detects

Pholus detects 20+ performance anti-patterns, including:

| Issue | What It Catches |
|-------|----------------|
| Uncached GetComponent | `GetComponent<T>()` in Update/FixedUpdate/LateUpdate |
| Uncached Camera.main | `Camera.main` lookup without caching |
| Uncached Find | `Find()` / `FindObjectOfType()` in hot paths |
| String Concatenation | String `+=` or `Concat` in Update |
| LINQ in Update | `Where()`, `OrderBy()`, `FirstOrDefault()` allocations |
| Allocations in Update | `new List<>()`, `new Array[]` in hot paths |
| Empty Unity Methods | Unused `OnEnable`/`Start` that Unity still calls |
| Physics No Layer Mask | `Raycast` / `SphereCast` without layer mask filtering |
| SendMessage Usage | `SendMessage` / `BroadcastMessage` reflection overhead |
| Tag Comparison | `tag == "string"` instead of `CompareTag()` |
| Debug.Log in Update | `Debug.Log` in performance-critical loops |
| Animator String Hash | `animator.SetTrigger("string")` instead of hash |
| Coroutine Allocations | `new WaitForSeconds()` instead of cached instance |
| Resources.Load | Runtime asset loading in tight loops |

### Confidence Modifiers

The AI adjusts confidence based on context:

**Reduces confidence when:**
- Class name suggests singleton (Manager, Controller)
- Code is inside `#if UNITY_EDITOR`
- Method is event-driven (OnClick, OnTrigger)
- Collections are small and fixed-size

**Increases confidence when:**
- Class name suggests mass instantiation (Enemy, Bullet, Projectile)
- Code is inside a loop
- Multiple issues compound together
- Running in high-frequency methods (Update, FixedUpdate)

---

## Fixing Issues

### Fix Workflow

1. Click the **Fix** button on any issue
2. Pholus sends the issue and your code to the AI
3. The AI generates a complete fix
4. A **diff preview** shows exactly what will change (green = added, red = removed)
5. Choose **Apply** to accept, or **Cancel** to discard

### Backup System

When enabled (default), Pholus creates a backup before every fix:

- **Location**: `.pholus-backup/` folder next to your script
- **Format**: `ScriptName.cs.yyyyMMdd_HHmmss.bak`
- **Limit**: Configurable (1-50, default 10) — oldest backups auto-deleted

### Undo

- Click **Undo Last Fix** in the footer bar after applying a fix
- Restores the file from the most recent backup
- Up to 50 undo actions per session

### Auto-Apply Mode

In Settings, you can enable **Auto-Apply Fixes** to skip the diff preview and apply fixes immediately. Backups and undo still work normally.

---

## AI Providers

Pholus supports five AI providers. You only need one to get started.

### Claude Code (Recommended)

- **Best for**: Highest accuracy analysis and fixes
- **CLI**: `claude`
- **Install**: `npm install -g @anthropic-sdk/claude-code`
- **Auth**: `claude /login` (requires Claude Pro or Max subscription)
- **Prompt Caching**: Supported — saves up to 90% on repeated analyses. Cache valid for 5 minutes.
- **Token Display**: Shows real-time usage with cache statistics in the header

### OpenAI Codex

- **CLI**: `codex`
- **Install**: `npm install -g @openai/codex`
- **Auth**: `codex` (interactive login, requires ChatGPT Plus)

### Google Gemini

- **CLI**: `gemini`
- **Install**: `npm install -g @google/gemini-cli`
- **Auth**: `gemini` (auto-auth with Google account)
- **Free Tier**: 1000 requests/day with a Google account

### Cursor (Beta)

- **CLI**: `cursor-agent`
- **Install**: `curl https://cursor.com/install -fsSL | bash`
- **Auth**: Requires Cursor account (Free limited, or Pro)
- **Windows**: Requires WSL (Windows Subsystem for Linux)

### OpenRouter (Alpha)

- **CLI**: `openrouter`
- **Install**: `npm install -g @letuscode/openrouter-cli`
- **Auth**: API key from [openrouter.ai/keys](https://openrouter.ai/keys)
- **Pay-Per-Use**: Access 100+ models from various providers
- **Model Format**: `provider/model-name` (e.g., `anthropic/claude-3.5-sonnet`)

### Switching Providers

Change your provider anytime in **Settings > AI Provider**. Each provider maintains its own model selection independently.

### Model Selection

- Use **(CLI Default)** to let the provider choose the best model
- Click **Refresh** to discover available models
- Click **Add** to manually add a model name not in the discovered list

---

## Consensus Mode

Consensus Mode runs your analysis through multiple AI providers simultaneously, then uses a "Director" provider to judge the combined results. This reduces false positives and increases confidence.

### How It Works

1. **Analyzers** (2+ providers) each independently analyze your code
2. A **Director** provider reviews all opinions and decides which issues are real
3. Results show **agreement tags** like `[2/3]` indicating how many providers found each issue
4. Dismissed issues are still shown separately with the director's reasoning

### Setup

1. Go to **Settings > Consensus Mode**
2. Enable **Multi-Provider Consensus**
3. Check at least **2 authenticated providers** as Analyzers
4. Select a **Director** provider (Claude recommended for best judgment)

### Provider Status Icons

- **Green check** = Authenticated and ready
- **Orange warning** = Installed but not authenticated
- **Gray X** = Not installed

### When to Use Consensus

- Before major releases — highest confidence results
- When you want to minimize false positives
- When analyzing critical performance paths
- When you want multiple perspectives on contextual issues

---

## Settings Reference

Open Settings via the gear icon in the Pholus header.

### AI Provider

| Setting | Description |
|---------|-------------|
| Active Provider | Which AI provider to use |
| Provider Status | Shows connection/auth status |
| Model Selection | Choose specific model or CLI default |
| Refresh | Re-discover available models |
| Login / Relogin | Open terminal for authentication |

### Analysis Options

| Setting | Description | Default |
|---------|-------------|---------|
| Target Platform | Mobile / PC-Console / VR — adjusts detection thresholds | PC/Console |
| Show "Why I Might Be Wrong" | Display AI reasoning for contextual issues | Off |
| Group by Certainty | Group issues as Definite > Contextual > Suggestions | On |

### Platform Thresholds

| Platform | Frame Budget | Strictness |
|----------|-------------|------------|
| **Mobile** | 16ms (60 FPS) | Strict — every allocation matters |
| **PC/Console** | 16ms (60 FPS) | Standard — more headroom |
| **VR** | 11ms (90 FPS) | Very strict — stutter highly noticeable |

### Fix Options

| Setting | Description | Default |
|---------|-------------|---------|
| Create Backup Before Fix | Save a copy before modifying | On |
| Max Backups to Keep | 1-50, oldest auto-deleted | 10 |

### Preferences

| Setting | Description | Default |
|---------|-------------|---------|
| Auto-Apply Fixes | Skip diff preview, apply immediately | Off |

### Logging

| Setting | Description | Default |
|---------|-------------|---------|
| Log Debug Messages | Show info logs in Unity Console | Off |
| Log Warnings | Show warning logs | On |
| Log Errors | Show error logs | On |

### Cache Management

| Action | What It Does |
|--------|-------------|
| Clear Cache | Removes all cached analysis results |
| Clear Models | Resets discovered and custom models |
| Reset to Defaults | Restores all settings to factory defaults |

---

## Demo Scripts

Pholus includes example scripts in the `Demo/` folder that demonstrate common performance issues. Use these to test your setup and understand what Pholus detects:

| Script | Issue Demonstrated |
|--------|-------------------|
| `UncachedComponents.cs` | GetComponent in Update/FixedUpdate |
| `LinqInUpdate.cs` | LINQ allocations in Update |
| `PerformanceAntiPatterns.cs` | Multiple anti-patterns combined |
| `BadCoroutines.cs` | WaitForSeconds allocations |
| `SendMessageUsage.cs` | SendMessage reflection overhead |
| `UpdateAllocations.cs` | Various allocation patterns |
| `ExpensiveLookups.cs` | Find/GetComponent patterns |
| `PhysicsNoLayerMask.cs` | Raycast without layer mask |
| `TagComparison.cs` | String tag comparison |
| `DebugInUpdate.cs` | Debug.Log in hot paths |
| `AnimatorStrings.cs` | String-based animator calls |

**Try it**: Drag any demo script into Pholus and click Analyze to see the tool in action.

---

## Troubleshooting

### CLI Not Detected

**Symptoms**: Provider shows "Not installed" in settings.

**Solutions**:
1. Verify Node.js v20+ is installed: run `node --version` in your terminal
2. Install the provider CLI manually: `npm install -g <cli-package>`
3. Restart Unity after installing
4. On Windows, ensure npm's global bin directory is in your PATH
5. Re-run the Setup Wizard: **Tools > Pholus > Setup Wizard**

### Authentication Failed

**Symptoms**: Provider shows "Not authenticated" after login attempt.

**Solutions**:
1. Run the login command directly in your terminal (not through Pholus)
2. Verify your subscription is active with the provider
3. Check for credential files:
   - Claude: `~/.claude/.credentials.json`
   - Codex: `~/.codex/auth.json`
   - Gemini: `~/.gemini/settings.json`
4. Try **Relogin** in Pholus settings

### Analysis Returns No Results

**Symptoms**: Analysis completes but shows no issues.

**Solutions**:
1. Your code may genuinely have no issues (score: 100)
2. Try analyzing a demo script to verify the tool works
3. Check Unity Console for error messages
4. Verify the AI provider is responding (check network connection)

### Model Unavailable Error

**Symptoms**: Error mentioning a model that cannot be used.

**Solution**: Pholus handles this automatically by resetting to the CLI default model and retrying. If it persists, click **Refresh** in model settings to discover currently available models.

### Fix Not Applying

**Symptoms**: Fix appears to succeed but code doesn't change.

**Solutions**:
1. Close the script in your IDE before applying the fix
2. Check Unity Console for errors
3. Verify the script file is not read-only
4. Try the fix again — the AI occasionally generates incomplete fixes

### Cursor on Windows

**Symptoms**: Cursor provider doesn't work on Windows.

**Solution**: Cursor agent requires WSL (Windows Subsystem for Linux). Install it with `wsl --install` in an administrator PowerShell, then restart your computer.

---

## Contact & Support

- **Email**: codeturion@gmail.com
- **Discord**: codeturion

For bug reports and feature requests, reach out via email or Discord.
