"""
Python Client for OmniDesktop (desktop4ai) Native Engine.
"""

import os
import sys
import json
import subprocess
from dataclasses import dataclass
from typing import List, Optional, Tuple, Dict, Any


@dataclass
class ElementNode:
    id: int
    type: str
    label: str
    x: int
    y: int
    width: int
    height: int
    center_x: int
    center_y: int


@dataclass
class WindowInfo:
    pid: int
    title: str
    x: int
    y: int
    width: int
    height: int


class DesktopClient:
    """
    Sub-token Python interface for controlling Windows/Desktop environment.
    """

    def __init__(self, bin_path: Optional[str] = None):
        if bin_path:
            self.bin_path = bin_path
        else:
            # Default lookup relative to repo root
            repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
            default_bin = os.path.join(repo_root, "bin", "desktop-core.exe")
            if os.path.exists(default_bin):
                self.bin_path = default_bin
            else:
                self.bin_path = "desktop-core.exe"

        self._cached_elements: Dict[int, ElementNode] = {}

    def _run_native(self, args: List[str]) -> Any:
        full_cmd = [self.bin_path] + args
        try:
            res = subprocess.run(
                full_cmd,
                capture_output=True,
                text=True,
                check=True,
                encoding="utf-8"
            )
            raw = res.stdout.strip()
            if not raw:
                return None
            try:
                return json.loads(raw)
            except json.JSONDecodeError:
                return raw
        except subprocess.CalledProcessError as e:
            raise RuntimeError(f"Desktop4AI native command failed: {e.stderr or e}")

    def get_cursor(self) -> Tuple[int, int]:
        """Returns current mouse cursor coordinates (x, y)."""
        data = self._run_native(["cursor"])
        return data["x"], data["y"]

    def list_windows(self) -> List[WindowInfo]:
        """Returns all open top-level application windows."""
        data = self._run_native(["windows"]) or []
        return [
            WindowInfo(
                pid=w["pid"],
                title=w["title"],
                x=w["x"],
                y=w["y"],
                width=w["width"],
                height=w["height"],
            )
            for w in data
        ]

    def focus_window(self, target: str) -> bool:
        """Brings application window to the foreground by title or PID."""
        res = self._run_native(["focus", str(target)])
        return bool(res and res.get("success"))

    def get_tree(self, limit: int = 50, window: Optional[str] = None) -> List[ElementNode]:
        """
        Extracts semantic interactive UI tree of the active window.
        Uses only ~150-300 tokens of LLM context!
        """
        args = ["tree", "--limit", str(limit)]
        if window:
            args.extend(["--window", window])

        data = self._run_native(args) or {}
        elements = []
        self._cached_elements.clear()

        for el in data.get("elements", []):
            node = ElementNode(
                id=el["id"],
                type=el["type"],
                label=el["label"],
                x=el["x"],
                y=el["y"],
                width=el["width"],
                height=el["height"],
                center_x=el["centerX"],
                center_y=el["centerY"],
            )
            elements.append(node)
            self._cached_elements[node.id] = node

        return elements

    def click_element(self, element_id: int, button: str = "left") -> Dict[str, Any]:
        """
        Clicks directly on a UI element by its integer ID.
        100% deterministic, zero coordinate guessing.
        """
        if element_id not in self._cached_elements:
            self.get_tree()

        if element_id not in self._cached_elements:
            raise ValueError(f"Element ID {element_id} not found in current UI state.")

        el = self._cached_elements[element_id]
        return self.click(el.center_x, el.center_y, button=button)

    def click(self, x: int, y: int, button: str = "left") -> Dict[str, Any]:
        """Clicks at exact screen coordinates."""
        return self._run_native(["click", str(x), str(y), button])

    def move(self, x: int, y: int, smooth: bool = True) -> Dict[str, Any]:
        """Moves cursor to target coordinates with natural human-like Bezier curve."""
        args = ["move", str(x), str(y)]
        if smooth:
            args.append("--smooth")
        return self._run_native(args)

    def drag(self, x1: int, y1: int, x2: int, y2: int) -> Dict[str, Any]:
        """Drags mouse from (x1, y1) to (x2, y2)."""
        return self._run_native(["drag", str(x1), str(y1), str(x2), str(y2)])

    def scroll(self, amount: int, x: Optional[int] = None, y: Optional[int] = None) -> Dict[str, Any]:
        """Scrolls mouse wheel. Positive = up, negative = down."""
        args = ["scroll", str(amount)]
        if x is not None and y is not None:
            args.extend([str(x), str(y)])
        return self._run_native(args)

    def type_text(self, text: str) -> Dict[str, Any]:
        """Types text or pastes unicode text instantaneously."""
        return self._run_native(["type", text])

    def hotkey(self, combo: str) -> Dict[str, Any]:
        """Presses keyboard shortcut (e.g. 'ctrl+c', 'alt+tab', 'enter')."""
        return self._run_native(["hotkey", combo])

    def capture_screen(self, out_path: str = "screenshot.png", som: bool = True) -> Dict[str, Any]:
        """Captures screenshot with optional Set-of-Marks tags."""
        args = ["capture", out_path]
        if som:
            args.append("--som")
        return self._run_native(args)

    def get_agent_prompt(self, limit: int = 40) -> str:
        """
        Formats a hyper-compact context string ready to inject into any LLM's system prompt or tool response.
        Consumes ~150-250 tokens!
        """
        elements = self.get_tree(limit=limit)
        cur_x, cur_y = self.get_cursor()

        lines = [
            f"Cursor: ({cur_x}, {cur_y})",
            f"Interactive Elements ({len(elements)}):",
        ]
        for el in elements:
            label_str = f' "{el.label}"' if el.label else ""
            lines.append(f"  [#{el.id}] {el.type}{label_str} at ({el.center_x}, {el.center_y})")

        return "\n".join(lines)
