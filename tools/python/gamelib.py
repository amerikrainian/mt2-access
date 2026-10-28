"""Shared constants for the dev tools: where the game is, how to launch it, where the dev server listens."""
from __future__ import annotations

import os
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
STEAM_APP_ID = "2742830"
GAME_EXE = "MonsterTrain2.exe"
WINDOW_TITLE = "Monster Train 2"
DEV_URL = os.environ.get("MTA_DEV_URL", "http://127.0.0.1:" + os.environ.get("MTA_DEV_PORT", "8772"))
