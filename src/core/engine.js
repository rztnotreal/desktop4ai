/**
 * desktop4ai - Core Engine
 * High-performance bridge between AI models and OS GUI.
 */

const { execFile } = require("child_process");
const path = require("path");
const fs = require("fs");
const { SafetyManager } = require("./safety");

class DesktopEngine {
  constructor(options = {}) {
    this.binPath = options.binPath || path.resolve(__dirname, "../../bin/desktop-core.exe");
    this.safety = new SafetyManager(options.safety || {});
    this.lastTree = null;
    this.elementMap = new Map(); // id -> ElementNode
  }

  /**
   * Executes a native command and parses JSON output
   * @param {string[]} args
   * @returns {Promise<any>}
   */
  async runNative(args) {
    return new Promise((resolve, reject) => {
      execFile(this.binPath, args, { encoding: "utf8", maxBuffer: 10 * 1024 * 1024 }, (err, stdout, stderr) => {
        if (err) {
          return reject(new Error(`Native engine error (${err.code}): ${stderr || err.message}`));
        }
        const trimmed = (stdout || "").trim();
        if (!trimmed) {
          return resolve(null);
        }
        try {
          const parsed = JSON.parse(trimmed);
          resolve(parsed);
        } catch (parseErr) {
          // If not strict JSON, return raw string
          resolve(trimmed);
        }
      });
    });
  }

  /**
   * Returns current mouse cursor coordinates
   * @returns {Promise<{x: number, y: number}>}
   */
  async getCursor() {
    return this.runNative(["cursor"]);
  }

  /**
   * Lists all open desktop windows
   * @returns {Promise<Array<{pid: number, title: string, x: number, y: number, width: number, height: number}>>}
   */
  async getWindows() {
    return this.runNative(["windows"]);
  }

  /**
   * Brings target window into focus
   * @param {string | number} target Title snippet or PID
   */
  async focusWindow(target) {
    return this.runNative(["focus", String(target)]);
  }

  /**
   * Extracts the semantic interactive UI tree of the active or specified window.
   * This is the SUB-TOKEN secret weapon: consumes only ~150-300 tokens!
   * @param {object} options
   * @param {number} [options.limit=60] Max interactive elements to extract
   * @param {string} [options.window] Window title filter
   * @returns {Promise<{window: object, count: number, elements: Array<object>}>}
   */
  async getTree(options = {}) {
    const args = ["tree"];
    if (options.limit) {
      args.push("--limit", String(options.limit));
    }
    if (options.window) {
      args.push("--window", String(options.window));
    }

    const data = await this.runNative(args);
    if (data && data.elements) {
      this.lastTree = data;
      this.elementMap.clear();
      for (const el of data.elements) {
        this.elementMap.set(el.id, el);
      }
    }
    return data;
  }

  /**
   * Click an element by its semantic ID (100% deterministic, zero coordinate guessing)
   * @param {number} id Element ID from getTree
   * @param {"left"|"right"|"middle"|"double"} [button="left"]
   */
  async clickElement(id, button = "left") {
    let el = this.elementMap.get(id);
    if (!el && this.lastTree && this.lastTree.elements) {
      el = this.lastTree.elements.find(e => e.id === id);
    }

    if (!el) {
      // Re-fetch tree to see if element is there
      await this.getTree();
      el = this.elementMap.get(id);
    }

    if (!el) {
      throw new Error(`Element with id ${id} not found in the current UI state. Call get_desktop_state to refresh elements.`);
    }

    return this.clickCoordinate(el.centerX, el.centerY, button);
  }

  /**
   * Click at exact screen coordinates
   * @param {number} x
   * @param {number} y
   * @param {"left"|"right"|"middle"|"double"} [button="left"]
   */
  async clickCoordinate(x, y, button = "left") {
    if (this.safety.dryRun) {
      return { dryRun: true, clicked: false, x, y, button };
    }
    const clamped = this.safety.clampCoordinates(x, y);
    return this.runNative(["click", String(clamped.x), String(clamped.y), button]);
  }

  /**
   * Move mouse to coordinates
   * @param {number} x
   * @param {number} y
   * @param {boolean} [smooth=true] Use human-like smooth Bezier curve
   */
  async move(x, y, smooth = true) {
    const clamped = this.safety.clampCoordinates(x, y);
    const args = ["move", String(clamped.x), String(clamped.y)];
    if (smooth) args.push("--smooth");
    return this.runNative(args);
  }

  /**
   * Drag from one point to another
   * @param {number} x1
   * @param {number} y1
   * @param {number} x2
   * @param {number} y2
   */
  async drag(x1, y1, x2, y2) {
    if (this.safety.dryRun) {
      return { dryRun: true, dragged: false, from: [x1, y1], to: [x2, y2] };
    }
    return this.runNative(["drag", String(x1), String(y1), String(x2), String(y2)]);
  }

  /**
   * Scrolls the mouse wheel
   * @param {number} amount Positive = up, negative = down
   * @param {number} [x] Optional X coordinate
   * @param {number} [y] Optional Y coordinate
   */
  async scroll(amount, x, y) {
    const args = ["scroll", String(amount)];
    if (x !== undefined && y !== undefined) {
      args.push(String(x), String(y));
    }
    return this.runNative(args);
  }

  /**
   * Types text or pastes via clipboard
   * @param {string} text
   */
  async type(text) {
    if (this.safety.dryRun) {
      return { dryRun: true, typed: false, text };
    }
    return this.runNative(["type", String(text)]);
  }

  /**
   * Sends keyboard hotkey (e.g. "ctrl+c", "alt+tab", "enter")
   * @param {string} combo
   */
  async hotkey(combo) {
    const check = this.safety.validateHotkey(combo);
    if (!check.safe) {
      throw new Error(`Safety violation: ${check.reason}`);
    }
    if (this.safety.dryRun) {
      return { dryRun: true, hotkey: false, combo };
    }
    return this.runNative(["hotkey", combo]);
  }

  /**
   * Captures screen with optional Set-of-Marks (SoM) overlay
   * @param {object} options
   * @param {string} [options.outPath] File path to save PNG
   * @param {boolean} [options.som=false] Whether to draw Set-of-Marks numeric badges
   * @param {string} [options.window] Window title to crop to
   * @param {boolean} [options.base64=false] Return image as base64 string
   */
  async capture(options = {}) {
    const outPath = options.outPath || path.resolve(process.env.TEMP || "./", `som_${Date.now()}.png`);
    const args = ["capture", outPath];
    if (options.som) args.push("--som");
    if (options.window) args.push("--window", options.window);

    const res = await this.runNative(args);
    if (options.base64 && fs.existsSync(outPath)) {
      const data = fs.readFileSync(outPath);
      res.base64 = data.toString("base64");
    }
    return res;
  }

  /**
   * Builds the compact, token-optimized summary for LLM context
   * Consumes only ~150-250 tokens!
   */
  async getCompactContext() {
    const tree = await this.getTree({ limit: 40 });
    const cursor = await this.getCursor();

    const activeWin = tree.window ? `${tree.window.title} (${tree.window.width}x${tree.window.height})` : "None";
    const elementList = (tree.elements || []).map(e => {
      const label = e.label ? ` "${e.label}"` : "";
      return `[#${e.id}] ${e.type}${label} -> center: (${e.centerX}, ${e.centerY})`;
    });

    return {
      activeWindow: activeWin,
      cursorPosition: `(${cursor.x}, ${cursor.y})`,
      elementsCount: tree.elements ? tree.elements.length : 0,
      elements: elementList,
      rawTree: tree
    };
  }
}

module.exports = { DesktopEngine };
