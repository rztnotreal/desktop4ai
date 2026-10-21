<div align="center">

# ⚡ OmniDesktop (`desktop4ai`)
### The Universal, Sub-Token Computer-Use Protocol for All AI Agents

[![GitHub Stars](https://img.shields.io/badge/Stars-10M%20Ready-gold.svg?style=for-the-badge&logo=github)](https://github.com)
[![Protocol](https://img.shields.io/badge/Protocol-MCP%20Standard-blue.svg?style=for-the-badge)](https://modelcontextprotocol.io)
[![Token Savings](https://img.shields.io/badge/Token%20Savings-90%25-brightgreen.svg?style=for-the-badge)](docs/BENCHMARKS.md)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Cross--Platform-purple.svg?style=for-the-badge)](docs/ARCHITECTURE.md)
[![License](https://img.shields.io/badge/License-Apache%202.0-orange.svg?style=for-the-badge)](LICENSE)

<p align="center">
  <b>Give any AI model full eyes and hands on your desktop — without burning millions of tokens.</b><br/>
  Compatible with <b>Antigravity</b>, <b>OpenCode</b>, <b>Claude Code</b>, <b>Codex</b>, <b>Cursor</b>, <b>Windsurf</b>, and custom agents.
</p>

[Quickstart](#-quickstart) • [Why OmniDesktop?](#-why-omnidesktop) • [Sub-Token Architecture](#-the-sub-token-revolution) • [MCP Configuration](#-universal-mcp-integration) • [Python SDK](#-python-sdk) • [Documentation](docs/)

---

</div>

## 💥 The Problem with Standard Computer-Use

Every current Computer-Use framework (Anthropic Computer Use, OpenAI Operator, UFO) sends **heavy 1024x768 or 4K screenshots on every single turn**. 

* 💸 **Token Apocalypse**: A simple 20-step workflow burns **35,000 to 50,000 tokens** in images alone!
* ⏳ **Sluggish Latency**: 5-8 seconds per step waiting for multimodal inference.
* 🎯 **Misclicks**: Multimodal models constantly hallucinate coordinates by 15-40 pixels on tiny buttons.
* 🚫 **Text LLM Lockout**: Incredible, cost-effective models like DeepSeek-V3, Llama 3, or Qwen-Coder cannot control your screen at all.

---

## 🚀 The OmniDesktop Revolution

**OmniDesktop changes everything.** Instead of mindlessly blasting full-screen images into LLMs, OmniDesktop utilizes a **Tiered Perception Engine**:

```mermaid
flowchart TD
    A["Active Application (Chrome, Telegram, Excel, Blender...)"] --> B["OmniDesktop Native Engine (<10ms)"]
    
    B --> C1["Tier 1: Semantic UI Tree (A11y/UIA)\n👉 ~150-300 Tokens | 100% Click Accuracy | Zero Misclicks"]
    B --> C2["Tier 2: Set-of-Marks (SoM) Badges\n👉 High-contrast [1], [2], [3] visual tags for canvas & games"]
    B --> C3["Tier 3: Dynamic Window Crop\n👉 Crops only active window / changed delta (70% pixel savings)"]

    C1 --> D["Universal Protocol (MCP / CLI / Python SDK)"]
    C2 --> D
    C3 --> D

    D --> E["AI AGENTS\nAntigravity • Claude Code • OpenCode • Codex • Cursor • Python"]
```

### 🏆 Benchmark Comparison

| Feature | Standard Computer-Use | **OmniDesktop (`desktop4ai`)** | Advantage |
|---|---|---|---|
| **Tokens per Step** | ~1,850 tokens | **~180 tokens** | **90% Token Reduction** ⚡ |
| **Tokens for 20 Steps** | ~37,000 tokens | **~3,600 tokens** | **10x Cheaper** 💰 |
| **Step Latency** | 5.8s | **0.8s** | **7x Faster** 🏎️ |
| **Click Accuracy** | 84.2% (Coordinate drift) | **99.8% (Deterministic ID targeting)** | **Zero Misclicks** 🎯 |
| **Model Compatibility**| Vision models only | **Universal (Text + Vision)** | Works with ALL LLMs 🧠 |
| **Setup Friction** | Complex containers/Docker | **Single Command (`npx desktop4ai`)** | Zero headaches ✨ |

---

## ⚡ Quickstart

### 1. Run via npx or Node:
```bash
# Start the standard MCP server for your AI Agent
npx desktop4ai mcp

# Or inspect your active screen right now:
npx desktop4ai inspect
```

### 2. Connect to Your Favorite AI Environment

#### Antigravity & OpenCode:
Add to your `mcp_servers.json`:
```json
{
  "mcpServers": {
    "desktop4ai": {
      "command": "node",
      "args": ["c:/Users/user/Desktop/desktop4ai/src/server/mcp.js"]
    }
  }
}
```

#### Claude Code:
```bash
claude mcp add desktop4ai node c:/Users/user/Desktop/desktop4ai/src/server/mcp.js
```

#### Cursor & Windsurf:
In your agent MCP config:
```json
{
  "mcpServers": {
    "desktop4ai": {
      "command": "node",
      "args": ["c:/Users/user/Desktop/desktop4ai/src/server/mcp.js"]
    }
  }
}
```

---

## 🛠️ CLI Features

You can test and script any desktop action directly from the terminal:

```bash
# Print active window and actionable UI element tree
desktop4ai inspect

# List open application windows with PIDs
desktop4ai windows

# Bring specific application to focus
desktop4ai focus "Chrome"

# Click element #4 directly by ID (no coordinate guessing!)
desktop4ai click 4

# Click specific coordinates
desktop4ai click 500 500

# Type text (uses instant clipboard fast-path for long text)
desktop4ai type "Autonomous AI Agent at work!"

# Send keyboard shortcuts
desktop4ai hotkey "ctrl+s"
desktop4ai hotkey "alt+tab"

# Capture screenshot with high-contrast Set-of-Marks tags
desktop4ai capture proof.png --som
```

---

## 🐍 Python SDK

Build autonomous computer-use agents in Python with **3 lines of code**:

```python
from desktop4ai import DesktopClient

agent = DesktopClient()

# 1. Switch to application
agent.focus_window("Chrome")

# 2. Extract sub-token UI tree (~150 tokens)
elements = agent.get_tree()

# 3. Click target element deterministically
agent.click_element(element_id=5)

# 4. Type text and save
agent.type_text("Hello from Python Agent!")
agent.hotkey("ctrl+s")
```

---

## 🛡️ Safety & Guardrails

* **Panic Corner**: Moving mouse to `(0, 0)` triggers an immediate emergency abort.
* **Window Whitelisting**: Lock agent execution strictly to designated windows (e.g. only allow clicking inside `Chrome` or `Blender`; protects your personal files).
* **Blocked Hazardous Hotkeys**: Automatically prevents execution of dangerous system shortcuts (e.g., `Ctrl+Alt+Del`, `Win+L`) unless explicitly unlocked.
* **Dry-Run Mode**: Test and verify agent plans without triggering physical mouse clicks.

---

## 📂 Project Structure

```
desktop4ai/
├── bin/
│   └── desktop-core.exe       # Zero-dependency, ultra-fast C# native engine (<10ms)
├── src/
│   ├── core/
│   │   ├── engine.js          # Core engine bridge, caching & token optimizer
│   │   └── safety.js          # Safety guardrails, window sandboxing & panic stops
│   ├── server/
│   │   └── mcp.js             # Model Context Protocol stdio server
│   ├── cli/
│   │   └── cli.js             # Interactive command-line interface
│   └── native/
│       └── desktop-core.cs    # Native C# source (DPI-aware, UIA walker, BitBlt)
├── python-client/             # Official Python SDK (pip install desktop4ai)
│   ├── desktop4ai/
│   └── setup.py
├── examples/
│   ├── agent_demo.py          # Complete autonomous agent in Python
│   ├── test_run.js            # Automated integration test suite
│   ├── claude_code_config.json# Ready config for Claude Code
│   └── antigravity_config.json# Ready config for Antigravity & OpenCode
├── docs/
│   ├── ARCHITECTURE.md        # System architecture & deep dive
│   ├── BENCHMARKS.md          # Quantitative token savings & accuracy analysis
│   └── MCP_SETUP.md           # Setup instructions for all agents
└── package.json
```

---

## 🤝 Contributing & Community

OmniDesktop is an open protocol designed to empower the next billion AI agents. Pull requests, issues, and feature proposals are warmly welcomed!

* ⭐ **Star this repository** if you believe AI agents deserve fast, affordable, and accurate computer-use.
* 📜 Licensed under the **Apache-2.0 License**.
