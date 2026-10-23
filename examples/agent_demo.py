"""
Example: Autonomous Computer-Use Agent using desktop4ai and OpenAI / Anthropic / Local LLM.
Demonstrates Sub-Token GUI Control (~150 tokens per step vs ~2500 tokens).
"""

import sys
import os

# Add local package to path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "python-client")))

from desktop4ai import DesktopClient

def main():
    desktop = DesktopClient()

    print("[INFO] Initializing desktop4ai Autonomous Agent...")

    # 1. Inspect Windows
    windows = desktop.list_windows()
    print(f"\nFound {len(windows)} active windows:")
    for w in windows[:5]:
        print(f"   - {w.title} (PID: {w.pid})")

    # 2. Get Sub-Token Context
    print("\nExtracting Sub-Token UI Tree (Zero Vision Tokens Needed!)...")
    elements = desktop.get_tree(limit=15)
    print(f"   Extracted {len(elements)} interactive controls:")
    for el in elements:
        print(f"   [#{el.id}] {el.type}: '{el.label}' at center ({el.center_x}, {el.center_y})")

    # 3. Formatted prompt for LLM
    print("\nLLM Agent State Representation (Under 200 tokens!):")
    print("-------------------------------------------------------")
    print(desktop.get_agent_prompt(limit=10))
    print("-------------------------------------------------------")

    print("\nAgent is ready to execute deterministic actions:")
    print("   - desktop.click_element(id=3)")
    print("   - desktop.type_text('Hello AI!')")
    print("   - desktop.hotkey('ctrl+s')")

if __name__ == "__main__":
    main()
