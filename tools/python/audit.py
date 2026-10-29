"""Run the mod's content audits against the live game (Module/Dev/GlossaryAudit, through the dev server's REPL).

    uv run python tools/python/audit.py <check> [options]

  glossary [--all-languages]   the glossary: every tooltip title the game draws is a definition or a content
                               name; bold mentions resolve; what each card and artifact's buffer gains; the
                               definitions' links. --all-languages repeats the last three in every language.
  titles                       the tooltip lines as speech reads them: none says its name twice ("Spikes Spikes");
                               titles still reading an icon are listed
  compare [--all-languages]    what each item gains now against the earlier rule (bold matched by spelling)
  screen                       the lines the glossary adds to each node of the screen open now

  --top N      how many of each list to print (default 40)
  --out FILE   also write the report to FILE

Cards are read through a card's own tooltip container, so a screen with a card must be open (the Logbook will
do); artifacts need nothing. The language checks switch through the game's own language setting and switch
back. A full run takes seconds per language.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dev  # noqa: E402

AUDIT = "MonsterTrainAccessibility.Module.Dev.GlossaryAudit"


def expression(args: argparse.Namespace) -> str:
    if args.check == "glossary":
        return f"{AUDIT}.RunAllLanguages({args.top})" if args.all_languages else f"{AUDIT}.Run({args.top})"
    if args.check == "titles":
        return f"{AUDIT}.Titles({args.top})"
    if args.check == "compare":
        return f"{AUDIT}.Compare({'true' if args.all_languages else 'false'}, {args.top})"
    return f"{AUDIT}.Screen()"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("check", choices=["glossary", "titles", "compare", "screen"])
    parser.add_argument("--all-languages", action="store_true", help="glossary and compare: every game language")
    parser.add_argument("--top", type=int, default=40, help="how many of each list to print")
    parser.add_argument("--out", type=Path, help="also write the report here")
    args = parser.parse_args()
    # Game text is Unicode (curly quotes, every language's script); the Windows console's code page is not.
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    result = dev.call("/eval", expression(args), timeout=900)
    if not result.startswith("=> "):
        print(result, file=sys.stderr)  # a compile error or an exception, as the REPL gave it
        return 1
    report = result[3:]
    print(report)
    if args.out:
        args.out.write_text(report, encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
