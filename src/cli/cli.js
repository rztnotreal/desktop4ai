#!/usr/bin/env node

/**
 * desktop4ai - Command Line Interface
 */

const { DesktopEngine } = require("../core/engine");
const path = require("path");

const engine = new DesktopEngine();

async function main() {
  const args = process.argv.slice(2);
  const cmd = args[0]?.toLowerCase();

  if (!cmd || cmd === "--help" || cmd === "-h") {
    printHelp();
    return;
  }

  try {
    switch (cmd) {
      case "mcp":
      case "serve": {
        // Run MCP Server
        require("../server/mcp");
        break;
      }

      case "inspect":
      case "tree": {
        const compact = await engine.getCompactContext();
        console.log("\n=======================================================");
        console.log(`Active Window : ${compact.activeWindow}`);
        console.log(`Cursor Pos    : ${compact.cursorPosition}`);
        console.log(`Elements      : ${compact.elementsCount} actionable controls`);
        console.log("=======================================================\n");
        for (const el of compact.elements) {
          console.log(`  ${el}`);
        }
        console.log("");
        break;
      }

      case "windows": {
        const wins = await engine.getWindows();
        console.log(`\nOpen Windows (${wins.length}):`);
        for (const w of wins) {
          console.log(`  - [PID: ${w.pid.toString().padEnd(6)}] "${w.title}" (${w.width}x${w.height})`);
        }
        console.log("");
        break;
      }

      case "focus": {
        const target = args[1];
        if (!target) {
          console.error("Usage: desktop4ai focus <window_title | pid>");
          process.exit(1);
        }
        const res = await engine.focusWindow(target);
        console.log(res?.success ? `[OK] Focused "${target}"` : `[ERROR] Could not find window "${target}"`);
        break;
      }

      case "cursor": {
        const pos = await engine.getCursor();
        console.log(`Cursor coordinates: X=${pos.x}, Y=${pos.y}`);
        break;
      }

      case "click": {
        if (!args[1]) {
          console.error("Usage: desktop4ai click <element_id> OR desktop4ai click <x> <y> [button]");
          process.exit(1);
        }
        if (args.length === 2) {
          // Click by element ID
          const id = parseInt(args[1], 10);
          const res = await engine.clickElement(id);
          console.log(`[OK] Clicked element #${id} at (${res.x}, ${res.y})`);
        } else {
          // Click by coordinates
          const x = parseInt(args[1], 10);
          const y = parseInt(args[2], 10);
          const btn = args[3] || "left";
          const res = await engine.clickCoordinate(x, y, btn);
          console.log(`[OK] Clicked at (${res.x}, ${res.y}) with ${btn}`);
        }
        break;
      }

      case "type": {
        const text = args.slice(1).join(" ");
        if (!text) {
          console.error("Usage: desktop4ai type <text>");
          process.exit(1);
        }
        await engine.type(text);
        console.log(`[OK] Typed ${text.length} characters.`);
        break;
      }

      case "hotkey": {
        const combo = args[1];
        if (!combo) {
          console.error("Usage: desktop4ai hotkey <combo> (e.g. ctrl+s, alt+tab)");
          process.exit(1);
        }
        await engine.hotkey(combo);
        console.log(`[OK] Sent hotkey: ${combo}`);
        break;
      }

      case "scroll": {
        const amount = parseInt(args[1] || "-300", 10);
        await engine.scroll(amount);
        console.log(`[OK] Scrolled: ${amount}`);
        break;
      }

      case "capture": {
        const outPath = args[1] && !args[1].startsWith("--") ? args[1] : `screenshot_${Date.now()}.png`;
        const som = args.includes("--som");
        const res = await engine.capture({ outPath, som });
        console.log(`[OK] Captured screenshot to ${res.path} (${res.width}x${res.height})`);
        if (som) {
          console.log(`Set-of-Marks annotated elements: ${res.somElements}`);
        }
        break;
      }

      default:
        console.error(`Unknown command: ${cmd}`);
        printHelp();
        process.exit(1);
    }
  } catch (err) {
    console.error(`[ERROR] Error: ${err.message}`);
    process.exit(1);
  }
}

function printHelp() {
  console.log(`
desktop4ai - Universal Sub-Token Computer Use Engine

Usage:
  desktop4ai <command> [options]

Commands:
  mcp / serve           Start Model Context Protocol (MCP) server for AI agents
  inspect / tree        Inspect active window & print actionable element tree
  windows               List all open application windows
  focus <target>        Bring window to foreground by title or PID
  cursor                Get current cursor coordinates
  click <id>            Click an element by its numeric ID
  click <x> <y> [btn]   Click at specific screen pixel coordinates
  type <text>           Type text (uses clipboard for instant unicode)
  hotkey <combo>        Send keyboard shortcut (e.g. 'ctrl+s', 'alt+tab')
  scroll <amount>       Scroll mouse wheel (e.g. -300 down, 300 up)
  capture [file] [--som] Take screenshot with optional Set-of-Marks tags

Examples:
  npx desktop4ai mcp
  desktop4ai inspect
  desktop4ai click 3
  desktop4ai type "Hello, world!"
  desktop4ai capture screen.png --som
`);
}

main();
