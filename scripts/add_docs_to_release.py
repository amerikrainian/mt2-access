from pathlib import Path
from zipfile import ZipFile


# Packaging pattern adapted from SayTheSpire2:
# https://github.com/bradjrenshaw/say-the-spire2
ROOT = Path(__file__).resolve().parents[1]
RELEASE_ZIP = ROOT / "MonsterTrainAccessibility.zip"
BOOK_DIR = ROOT / "docs_src" / "book"


def main() -> None:
    if not RELEASE_ZIP.exists():
        raise FileNotFoundError(f"Release zip not found: {RELEASE_ZIP}")
    if not BOOK_DIR.is_dir():
        raise FileNotFoundError(f"Docs build output not found: {BOOK_DIR}")

    with ZipFile(RELEASE_ZIP, "a") as archive:
        for src in sorted(BOOK_DIR.rglob("*")):
            if src.is_file():
                arc_name = Path("MT2AccessibilityDocs") / src.relative_to(BOOK_DIR)
                archive.write(src, arc_name.as_posix())


if __name__ == "__main__":
    main()
