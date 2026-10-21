#!/usr/bin/env node

/**
 * OmniDesktop (desktop4ai) - Universal Model Context Protocol (MCP) Server
 * Compatible with Antigravity, OpenCode, Claude Code, Codex, Cursor, Windsurf, Zed.
 */

const { Server } = require("@modelcontextprotocol/sdk/server/index.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
const {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} = require("@modelcontextprotocol/sdk/types.js");
const { DesktopEngine } = require("../core/engine");
const path = require("path");
const fs = require("fs");

const engine = new DesktopEngine();

const server = new Server(
  {
    name: "omni-desktop",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Define tool specifications
const TOOLS = [
  {
    name: "get_desktop_state",
    description:
      "Get the current state of the active desktop window, cursor coordinates, and a token-optimized semantic UI tree of all clickable/interactive controls with IDs. Extremely token-efficient (~150 tokens vs ~2500 for images).",
    inputSchema: {
      type: "object",
      properties: {
        include_screenshot: {
          type: "boolean",
          description: "Whether to also capture a screenshot (increases token usage). Default false.",
        },
        include_som: {
          type: "boolean",
          description: "If screenshot is requested, whether to draw Set-of-Marks numeric tags on elements.",
        },
        limit: {
          type: "number",
          description: "Max number of interactive UI elements to return (default: 50).",
        },
      },
    },
  },
  {
    name: "click_element",
    description:
      "Click directly on an interactive UI element by its numeric ID from get_desktop_state (100% accurate, eliminates coordinate guessing errors).",
    inputSchema: {
      type: "object",
      properties: {
        id: {
          type: "number",
          description: "The numeric ID of the element (e.g. 1, 2, 5).",
        },
        button: {
          type: "string",
          enum: ["left", "right", "middle", "double"],
          description: "Mouse button to click (default: 'left').",
        },
      },
      required: ["id"],
    },
  },
  {
    name: "click_coordinate",
    description: "Click at specific screen pixel coordinates (X, Y).",
    inputSchema: {
      type: "object",
      properties: {
        x: { type: "number", description: "X coordinate (0 to screen width)." },
        y: { type: "number", description: "Y coordinate (0 to screen height)." },
        button: {
          type: "string",
          enum: ["left", "right", "middle", "double"],
          description: "Mouse button to click (default: 'left').",
        },
      },
      required: ["x", "y"],
    },
  },
  {
    name: "move_cursor",
    description: "Move the mouse cursor smoothly to target coordinates.",
    inputSchema: {
      type: "object",
      properties: {
        x: { type: "number", description: "Target X coordinate." },
        y: { type: "number", description: "Target Y coordinate." },
        smooth: { type: "boolean", description: "Use human-like smooth Bezier movement (default: true)." },
      },
      required: ["x", "y"],
    },
  },
  {
    name: "type_text",
    description: "Type text into the currently focused input field. Uses instant clipboard paste for long text.",
    inputSchema: {
      type: "object",
      properties: {
        text: { type: "string", description: "The text string to type or paste." },
      },
      required: ["text"],
    },
  },
  {
    name: "press_hotkey",
    description: "Press a keyboard shortcut (e.g. 'ctrl+c', 'ctrl+v', 'alt+tab', 'enter', 'esc', 'ctrl+s').",
    inputSchema: {
      type: "object",
      properties: {
        combo: {
          type: "string",
          description: "Hotkey combination separated by plus, e.g. 'ctrl+s', 'enter', 'alt+f4', 'win+r'.",
        },
      },
      required: ["combo"],
    },
  },
  {
    name: "scroll_page",
    description: "Scroll the mouse wheel up or down.",
    inputSchema: {
      type: "object",
      properties: {
        amount: {
          type: "number",
          description: "Scroll amount (positive = scroll up, negative = scroll down, e.g. -300 to scroll down).",
        },
        x: { type: "number", description: "Optional X position to hover before scrolling." },
        y: { type: "number", description: "Optional Y position to hover before scrolling." },
      },
      required: ["amount"],
    },
  },
  {
    name: "drag_and_drop",
    description: "Drag from start coordinates and drop at destination coordinates.",
    inputSchema: {
      type: "object",
      properties: {
        from_x: { type: "number", description: "Start X." },
        from_y: { type: "number", description: "Start Y." },
        to_x: { type: "number", description: "Destination X." },
        to_y: { type: "number", description: "Destination Y." },
      },
      required: ["from_x", "from_y", "to_x", "to_y"],
    },
  },
  {
    name: "focus_window",
    description: "Switch to and activate a specific application window by title or process ID.",
    inputSchema: {
      type: "object",
      properties: {
        target: {
          type: "string",
          description: "Window title substring (e.g. 'Chrome', 'Telegram', 'Blender') or PID.",
        },
      },
      required: ["target"],
    },
  },
  {
    name: "list_windows",
    description: "List all visible windows on the desktop with process IDs and coordinates.",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "capture_screen",
    description: "Capture a full screenshot with optional Set-of-Marks (SoM) annotations.",
    inputSchema: {
      type: "object",
      properties: {
        som: {
          type: "boolean",
          description: "Draw Set-of-Marks tags over detected elements (default: true).",
        },
      },
    },
  },
];

// Register list tools handler
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools: TOOLS };
});

// Register call tool handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case "get_desktop_state": {
        const compact = await engine.getCompactContext();
        let screenshotInfo = null;

        if (args?.include_screenshot) {
          const cap = await engine.capture({ som: !!args.include_som });
          screenshotInfo = {
            savedPath: cap.path,
            resolution: `${cap.width}x${cap.height}`,
            somBadgesCount: cap.somElements,
          };
        }

        const formattedText = [
          `🖥️ Active Window: ${compact.activeWindow}`,
          `📍 Mouse Cursor: ${compact.cursorPosition}`,
          `🎯 Interactive Elements Count: ${compact.elementsCount}`,
          "",
          "--- Actionable UI Elements (Call click_element with ID) ---",
          ...compact.elements.slice(0, args?.limit || 50),
        ];

        if (screenshotInfo) {
          formattedText.push("", `📸 Screenshot saved at: ${screenshotInfo.savedPath} (${screenshotInfo.resolution})`);
        }

        return {
          content: [
            {
              type: "text",
              text: formattedText.join("\n"),
            },
          ],
        };
      }

      case "click_element": {
        const res = await engine.clickElement(args.id, args.button || "left");
        return {
          content: [
            {
              type: "text",
              text: `✅ Clicked element #${args.id} at (${res.x}, ${res.y}) with ${args.button || "left"} click.`,
            },
          ],
        };
      }

      case "click_coordinate": {
        const res = await engine.clickCoordinate(args.x, args.y, args.button || "left");
        return {
          content: [
            {
              type: "text",
              text: `✅ Clicked coordinates (${res.x}, ${res.y}) with ${args.button || "left"} click.`,
            },
          ],
        };
      }

      case "move_cursor": {
        const res = await engine.move(args.x, args.y, args.smooth !== false);
        return {
          content: [
            {
              type: "text",
              text: `✅ Moved cursor to (${res.x}, ${res.y}).`,
            },
          ],
        };
      }

      case "type_text": {
        const res = await engine.type(args.text);
        return {
          content: [
            {
              type: "text",
              text: `✅ Successfully typed text (${args.text.length} characters).`,
            },
          ],
        };
      }

      case "press_hotkey": {
        const res = await engine.hotkey(args.combo);
        return {
          content: [
            {
              type: "text",
              text: `✅ Pressed hotkey '${args.combo}'.`,
            },
          ],
        };
      }

      case "scroll_page": {
        const res = await engine.scroll(args.amount, args.x, args.y);
        const direction = args.amount > 0 ? "up" : "down";
        return {
          content: [
            {
              type: "text",
              text: `✅ Scrolled ${direction} (amount: ${args.amount}).`,
            },
          ],
        };
      }

      case "drag_and_drop": {
        const res = await engine.drag(args.from_x, args.from_y, args.to_x, args.to_y);
        return {
          content: [
            {
              type: "text",
              text: `✅ Dragged from (${args.from_x}, ${args.from_y}) to (${args.to_x}, ${args.to_y}).`,
            },
          ],
        };
      }

      case "focus_window": {
        const res = await engine.focusWindow(args.target);
        return {
          content: [
            {
              type: "text",
              text: res?.success
                ? `✅ Brought window '${args.target}' to foreground.`
                : `⚠️ Window matching '${args.target}' was not found. Call list_windows to see active windows.`,
            },
          ],
        };
      }

      case "list_windows": {
        const wins = await engine.getWindows();
        const list = (wins || []).map(
          (w) => `- [PID: ${w.pid}] "${w.title}" (${w.width}x${w.height} at ${w.x},${w.y})`
        );
        return {
          content: [
            {
              type: "text",
              text: `Desktop Windows (${list.length}):\n` + list.join("\n"),
            },
          ],
        };
      }

      case "capture_screen": {
        const som = args?.som !== false;
        const res = await engine.capture({ som });
        return {
          content: [
            {
              type: "text",
              text: `✅ Screenshot captured: ${res.path} (${res.width}x${res.height}). Annotated elements: ${res.somElements || 0}`,
            },
          ],
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    return {
      content: [
        {
          type: "text",
          text: `❌ Error executing ${name}: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});

// Start standard stdio MCP transport
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("[OmniDesktop] MCP Server running on stdio.");
}

main().catch((err) => {
  console.error("[OmniDesktop] Fatal startup error:", err);
  process.exit(1);
});
