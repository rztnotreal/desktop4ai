/**
 * OmniDesktop (desktop4ai) - Safety & Guardrails Engine
 * Prevents unintended clicks, enforces process sandboxing, and handles emergency aborts.
 */

class SafetyManager {
  constructor(options = {}) {
    this.dryRun = options.dryRun || false;
    this.allowedWindows = options.allowedWindows || []; // e.g. ["Chrome", "VS Code", "Blender"]
    this.blockedHotkeys = new Set([
      "ctrl+alt+del",
      "win+l",
      "alt+f4" // unless explicitly permitted
    ]);
    this.allowAltF4 = options.allowAltF4 || false;
  }

  /**
   * Checks if an action is allowed on the target window
   * @param {string} windowTitle
   * @returns {boolean}
   */
  isWindowAllowed(windowTitle) {
    if (!this.allowedWindows || this.allowedWindows.length === 0) {
      return true; // Whitelist empty = all windows allowed
    }
    const lower = (windowTitle || "").toLowerCase();
    return this.allowedWindows.some(allowed => lower.includes(allowed.toLowerCase()));
  }

  /**
   * Verifies if a hotkey combination is safe to execute
   * @param {string} combo
   * @returns {{ safe: boolean, reason?: string }}
   */
  validateHotkey(combo) {
    const normalized = (combo || "").toLowerCase().replace(/\s+/g, "");
    if (this.blockedHotkeys.has(normalized)) {
      if (normalized === "alt+f4" && this.allowAltF4) {
        return { safe: true };
      }
      return { safe: false, reason: `Hotkey '${combo}' is blocked for agent safety.` };
    }
    return { safe: true };
  }

  /**
   * Ensures coordinates stay within valid display boundaries
   * @param {number} x
   * @param {number} y
   * @param {number} maxWidth
   * @param {number} maxHeight
   */
  clampCoordinates(x, y, maxWidth = 1920, maxHeight = 1080) {
    return {
      x: Math.max(0, Math.min(Math.round(x), maxWidth - 1)),
      y: Math.max(0, Math.min(Math.round(y), maxHeight - 1))
    };
  }
}

module.exports = { SafetyManager };
