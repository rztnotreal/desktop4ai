# 🔌 Universal MCP Setup Guide

OmniDesktop communicates over the official **Model Context Protocol (MCP)** via standard `stdio`. This allows any modern AI assistant or agentic IDE to instantly acquire computer-use capabilities.

---

## 1. Antigravity & OpenCode Configuration

Add the following to your MCP configuration file (`mcp_config.json` or `.gemini/antigravity/mcp_servers.json`):

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

Or via npx once published:
```json
{
  "mcpServers": {
    "desktop4ai": {
      "command": "npx",
      "args": ["-y", "desktop4ai", "mcp"]
    }
  }
}
```

---

## 2. Claude Code

Register OmniDesktop directly with Claude Code using the CLI:

```bash
claude mcp add desktop4ai node c:/Users/user/Desktop/desktop4ai/src/server/mcp.js
```

Or edit `~/.claude.json`:
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

Now you can tell Claude Code:
> *"Open Telegram, click the search bar, search for 'marketing report', and copy the latest message."*

Claude Code will execute this using **Sub-Token Mode** without spending 30,000 tokens on screenshots!

---

## 3. Cursor & Windsurf

In Cursor or Windsurf settings (`cursor_settings.json`):

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

## 4. Standalone CLI & Terminal Use

You can also run OmniDesktop standalone as a regular command-line tool:

```bash
# Inspect the active window and print clickable elements
desktop4ai inspect

# Click an element by ID
desktop4ai click 12

# Type text
desktop4ai type "Automating my desktop with AI"

# Send shortcut
desktop4ai hotkey "ctrl+s"

# Capture Set-of-Marks screenshot
desktop4ai capture proof.png --som
```
