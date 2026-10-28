"""Drive the running game through the mod's dev server (http://127.0.0.1:8772).

    uv run python tools/python/dev.py <command> [args]

  health                      is the dev server up
  launch                      start the game through Steam and wait for the dev server
  kill                        kill MonsterTrain2.exe (a host DLL change needs this before a build)
  reload                      hot-reload Core + Module (after `dotnet build src/MonsterTrainAccessibility.Module/...`)
  module                      generation, DLL write times, Harmony patches
  nav                         our navigator: screen stack, focused node, every node in the render
  input VERB [VERB ...]       dispatch inputs (ui.down, ui.next, ui.activate, ui.back, ui.tooltip, mod.menu...)
  speech [--since N] [--tail K]   what the mod spoke
  log [--grep S] [--tail K]   the mod's log lines
  gui [--grep REGEX] [--context N]   the raw uGUI hierarchy, optionally around lines matching a regex
  eval FILE|-                 run C# (Roslyn) against the live game, from a file or stdin
  wait EXPR [--timeout MS]    poll a C# bool expression on the main thread until true
  typeinfo NAME               a type's members (fields, properties, methods)
  actions                     the registered input actions and their keys
  screenshot [OUT.png]        capture the game view (copies the temp file when OUT is given)
  click [X Y]                 an OS-level click at Unity screen coordinates (default: window centre);
                              for prompts the mod does not cover yet

Environment: MTA_DEV_URL or MTA_DEV_PORT override the server address.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gamelib  # noqa: E402


def call(route: str, body: str | None = None, method: str | None = None, timeout: float = 60, query: dict | None = None) -> str:
    url = gamelib.DEV_URL + route
    if query:
        url += "?" + urllib.parse.urlencode(query)
    data = body.encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method=method or ("POST" if data is not None else "GET"))
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return resp.read().decode("utf-8", errors="replace")
    except urllib.error.URLError as e:
        sys.exit(f"dev server not reachable at {gamelib.DEV_URL}: {e}")


def cmd_launch(_: argparse.Namespace) -> int:
    steam = Path(r"C:\Program Files (x86)\Steam\steam.exe")
    subprocess.Popen([str(steam), "-applaunch", gamelib.STEAM_APP_ID])
    print("launched; waiting for the dev server", end="", flush=True)
    for _ in range(90):
        time.sleep(2)
        try:
            with urllib.request.urlopen(gamelib.DEV_URL + "/health", timeout=2) as resp:
                if resp.status == 200:
                    print(" up")
                    return 0
        except (urllib.error.URLError, OSError):
            print(".", end="", flush=True)
    print(" timed out")
    return 1


def cmd_kill(_: argparse.Namespace) -> int:
    return subprocess.call(["taskkill.exe", "/F", "/IM", gamelib.GAME_EXE])


def cmd_click(args: argparse.Namespace) -> int:
    import ctypes
    from ctypes import wintypes

    user32 = ctypes.windll.user32
    hwnd = user32.FindWindowW(None, gamelib.WINDOW_TITLE)
    if not hwnd:
        sys.exit("no " + gamelib.WINDOW_TITLE + " window")
    rect = wintypes.RECT()
    user32.GetClientRect(hwnd, ctypes.byref(rect))
    cw, ch = rect.right - rect.left, rect.bottom - rect.top
    sw, sh = args.screen
    ux, uy = (args.x, args.y) if args.x is not None else (sw / 2, sh / 2)
    pt = wintypes.POINT(int(ux * cw / sw), int((sh - uy) * ch / sh))
    user32.ClientToScreen(hwnd, ctypes.byref(pt))
    user32.SetForegroundWindow(hwnd)
    time.sleep(0.3)
    user32.SetCursorPos(pt.x, pt.y)
    time.sleep(0.1)
    user32.mouse_event(0x0002, 0, 0, 0, 0)
    time.sleep(0.06)
    user32.mouse_event(0x0004, 0, 0, 0, 0)
    print(f"clicked at client {pt.x},{pt.y}")
    return 0


def main() -> int:
    # Game text is any script (leaderboard names, localized strings): a Windows console or a pipe
    # in the ANSI codepage must not crash a print.
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("health", "reload", "module", "nav", "actions", "launch", "kill"):
        sub.add_parser(name)
    p = sub.add_parser("input"); p.add_argument("verbs", nargs="+")
    p = sub.add_parser("speech"); p.add_argument("--since", type=int, default=0); p.add_argument("--tail", type=int, default=20)
    p = sub.add_parser("log"); p.add_argument("--grep"); p.add_argument("--tail", type=int, default=30)
    p = sub.add_parser("gui"); p.add_argument("--grep"); p.add_argument("--context", type=int, default=0)
    p = sub.add_parser("eval"); p.add_argument("file")
    p = sub.add_parser("wait"); p.add_argument("expr"); p.add_argument("--timeout", type=int, default=30000)
    p = sub.add_parser("typeinfo"); p.add_argument("name")
    p = sub.add_parser("screenshot"); p.add_argument("out", nargs="?")
    p = sub.add_parser("click"); p.add_argument("x", type=float, nargs="?"); p.add_argument("y", type=float, nargs="?")
    p.add_argument("--screen", type=int, nargs=2, default=(1920, 1200), metavar=("W", "H"), help="the game's Screen.width/height")
    args = ap.parse_args()

    if args.cmd == "launch":
        return cmd_launch(args)
    if args.cmd == "kill":
        return cmd_kill(args)
    if args.cmd == "click":
        return cmd_click(args)
    if args.cmd in ("health", "module", "nav", "actions"):
        print(call("/" + args.cmd).rstrip())
        return 0
    if args.cmd == "reload":
        print(call("/reload", body="", timeout=90).rstrip())
        return 0
    if args.cmd == "input":
        for verb in args.verbs:
            print(call("/input", body=verb).rstrip())
        return 0
    if args.cmd == "speech":
        lines = call("/speech", query={"since": args.since}).rstrip().splitlines()
        print("\n".join(lines[-args.tail:]))
        return 0
    if args.cmd == "log":
        query = {"grep": args.grep} if args.grep else None
        lines = call("/log", query=query).rstrip().splitlines()
        print("\n".join(lines[-args.tail:]))
        return 0
    if args.cmd == "gui":
        text = call("/gui", timeout=90)
        if not args.grep:
            print(text.rstrip())
            return 0
        import re
        lines = text.splitlines()
        rx = re.compile(args.grep)
        for i, line in enumerate(lines):
            if rx.search(line):
                lo, hi = max(0, i - args.context), min(len(lines), i + args.context + 1)
                for j in range(lo, hi):
                    print(f"{j + 1}: {lines[j]}")
                if args.context:
                    print("--")
        return 0
    if args.cmd == "eval":
        code = sys.stdin.read() if args.file == "-" else Path(args.file).read_text(encoding="utf-8")
        print(call("/eval", body=code, timeout=90).rstrip())
        return 0
    if args.cmd == "wait":
        print(call("/wait", body=args.expr, timeout=args.timeout / 1000 + 10, query={"timeout": args.timeout}).rstrip())
        return 0
    if args.cmd == "typeinfo":
        print(call("/typeinfo", query={"name": args.name}).rstrip())
        return 0
    if args.cmd == "screenshot":
        path = call("/screenshot").strip()
        if args.out:
            shutil.copyfile(path, args.out)
            print(args.out)
        else:
            print(path)
        return 0
    return 2


if __name__ == "__main__":
    sys.exit(main())
