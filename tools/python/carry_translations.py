"""Carry translations of a string from main's JSON files into lang/*.txt.

    uv run python tools/python/carry_translations.py NEWKEY=MAINKEY [NEWKEY=MAINKEY ...]

MAINKEY is main's key without its section ("state.completed"), or "section.key" to pick one section.
For each pair, every translated language file under lang/ gets `NEWKEY=<main's translation>` appended
(replacing an existing NEWKEY line), read from main's
MonsterTrainAccessibility/Localization/Strings/<code>.json in the main worktree (../monster_traina11y2-main
by default; MTA_MAIN_DIR overrides). main's named placeholders ({attack}) become {0}, {1}... in the order
they first appear in main's ENGLISH value, which is the order the new key's English template must use.
Add the key to Core/Strings/Strings.cs first, then regenerate lang/en.txt (MTA_WRITE_LANG_TEMPLATE=1).
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
MAIN = Path(os.environ.get("MTA_MAIN_DIR", REPO.parent / "monster_traina11y2-main"))
MAIN_STRINGS = MAIN / "MonsterTrainAccessibility" / "Localization" / "Strings"
LANG = REPO / "lang"

# Our file (the game's I2 code) -> main's file.
FILES = {
    "de-DE": "de", "es": "es", "fr-FR": "fr", "ja": "ja", "ko": "ko",
    "pt-BR": "pt", "ru": "ru", "zh-CN": "zh-CN", "zh-HK": "zh-TW",
}

NAMED = re.compile(r"\{([A-Za-z_][A-Za-z0-9_]*)\}")


def load(code: str) -> dict:
    """main's file flattened: its keys live under sections ("messages", "ui", ...); a key is found by
    its name alone, or as "section.key" when two sections share it."""
    with open(MAIN_STRINGS / f"{code}.json", encoding="utf-8-sig") as f:
        sections = json.load(f)
    flat = {}
    for section, table in sections.items():
        for key, value in table.items():
            flat.setdefault(key, value)
            flat[section + "." + key] = value
    return flat


def positional(value: str, order: list[str]) -> str:
    return NAMED.sub(lambda m: "{%d}" % order.index(m.group(1)) if m.group(1) in order else m.group(0), value)


def main(argv: list[str]) -> int:
    pairs = [a.split("=", 1) for a in argv]
    if not pairs or any(len(p) != 2 for p in pairs):
        print(__doc__)
        return 2
    english = load("en")
    for ours, theirs in pairs:
        if theirs not in english:
            sys.exit(f"main has no key {theirs}")
    for ours_code, main_code in FILES.items():
        table = load(main_code)
        path = LANG / f"{ours_code}.txt"
        lines = path.read_text(encoding="utf-8").splitlines()
        for ours, theirs in pairs:
            order = []
            for name in NAMED.findall(english[theirs]):
                if name not in order:
                    order.append(name)
            value = table.get(theirs)
            if not value:
                print(f"{ours_code}: main has no {theirs}; left out (English fallback)")
                continue
            lines = [l for l in lines if not l.startswith(ours + "=")]
            lines.append(f"{ours}={positional(value, order)}")
        path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print("carried", ", ".join(o for o, _ in pairs))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
