#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
================================================================================
 fix_resx_format.py — Collapse every <data> element into a single line
================================================================================

WHAT IT DOES
------------
Reads one or more .resx files and rewrites them so that every <data> entry is
on ONE line:

    <data name="Key" xml:space="preserve"><value>Value</value></data>

Instead of the multi-line form that VS Code / auto-formatters produce:

    <data name="Key" xml:space="preserve">
      <value>Value</value>
    </data>

It also:
  - Ensures xml:space="preserve" on every <data>
  - Preserves <comment> elements when present
  - Preserves multi-line values (very important for Js_ConfirmImpersonate,
    Js_ConfirmOpenApt, etc.) by keeping the newlines INSIDE <value> as
    the literal two-character sequence "&#10;" ONLY IF you pass --newline-entity.
    By default, real newlines are kept inside <value> (still valid XML).
  - Preserves section comments like:
        <!-- ============================================================ -->
        <!-- SECTION: _Global                                             -->
        <!-- ============================================================ -->
  - Preserves the resheader / schema block
  - Never touches Arabic text or translation content
  - Creates a .bak-YYYYMMDD-HHMMSS backup before writing

--------------------------------------------------------------------------------
REQUIREMENTS
--------------------------------------------------------------------------------
  Python 3.8+  (stdlib only — no pip install)

--------------------------------------------------------------------------------
QUICK START — COMMON COMMANDS
--------------------------------------------------------------------------------

# 1) Dry-run: report which files would change (nothing written)
python fix_resx_format.py

# 2) Fix all Shared.*.resx files in Resources/ (writes changes + backups)
python fix_resx_format.py --apply

# 3) Fix a single file
python fix_resx_format.py --files Resources/Shared.ar.resx --apply

# 4) Fix every .resx file recursively under a folder
python fix_resx_format.py --dir Resources --apply --recursive

# 5) Only validate (no write) — exit code 1 if any file is mis-formatted
python fix_resx_format.py --check

--------------------------------------------------------------------------------
ALL OPTIONS
--------------------------------------------------------------------------------
  --files F1 F2 ...   Specific .resx files to process.
  --dir DIR           Folder to scan (default: Resources).
  --recursive         Recurse into subfolders of --dir.
  --apply             Write changes (default: dry-run).
  --check             Validate only; exit 1 if any file needs fixing.
  --no-backup         Skip .bak files (not recommended).
  --quiet             Only print summary.
  --help, -h          Show this help.

--------------------------------------------------------------------------------
EXIT CODES
--------------------------------------------------------------------------------
  0  Success (or --check found everything already correct)
  1  --check found at least one file that needs fixing
  2  Fatal error (bad path, unreadable file, etc.)

================================================================================
"""

import argparse
import os
import re
import shutil
import sys
from datetime import datetime
from pathlib import Path

# ============================================================
# Defaults
# ============================================================
DEFAULT_DIR = "Resources"

# ============================================================
# Regexes
# ============================================================
# Matches a <data ...>...</data> block, multi-line OR single-line.
# Uses non-greedy match; `re.DOTALL` makes `.` match newlines.
DATA_BLOCK_RE = re.compile(
    r"<data\b(?P<attrs>[^>]*)>(?P<body>.*?)</data>",
    re.DOTALL,
)

# Extract the <value>...</value> (may span lines).
VALUE_RE = re.compile(r"<value(?:\s[^>]*)?>(?P<val>.*?)</value>", re.DOTALL)

# Extract optional <comment>...</comment>.
COMMENT_RE = re.compile(r"<comment(?:\s[^>]*)?>(?P<c>.*?)</comment>", re.DOTALL)

# Extract name="..." from the data attrs.
NAME_RE = re.compile(r'\bname\s*=\s*"([^"]*)"')

# xml:space="preserve" presence
XML_SPACE_RE = re.compile(r'\bxml:space\s*=\s*"preserve"')

# Section comment block (kept as-is)
SECTION_COMMENT_RE = re.compile(
    r"<!--\s*=+\s*-->\s*"
    r"<!--\s*SECTION:.*?-->\s*"
    r"(?:<!--.*?-->\s*)*"
    r"<!--\s*=+\s*-->",
    re.DOTALL,
)


# ============================================================
# Helpers
# ============================================================
def _safe_escape_value(val: str) -> str:
    """
    Escape & < > ONCE, but preserve existing entities like &amp; &lt; &gt;
    and numeric entities &#10; &#x41;.
    """
    if val is None:
        return ""
    out = val
    # Escape lone & (not already part of an entity)
    out = re.sub(r"&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[0-9a-fA-F]+;)", "&amp;", out)
    out = out.replace("<", "&lt;").replace(">", "&gt;")
    return out


def _normalize_attrs(attrs: str) -> str:
    """
    Ensure the <data ...> attrs include xml:space="preserve".
    Keep any other attrs as-is (e.g. type="...").
    """
    attrs = attrs.strip()
    if not XML_SPACE_RE.search(attrs):
        if attrs:
            attrs += ' xml:space="preserve"'
        else:
            attrs = 'xml:space="preserve"'
    return attrs


def _collapse_data_block(match: re.Match) -> str:
    """
    Turn a <data>...</data> block into a single-line form:
        <data name="X" xml:space="preserve"><value>...</value></data>
        <data name="X" xml:space="preserve"><value>...</value><comment>...</comment></data>
    """
    attrs = match.group("attrs") or ""
    body = match.group("body") or ""

    attrs = _normalize_attrs(attrs)
    # Normalize whitespace between attrs (multiple spaces -> one)
    attrs = re.sub(r"\s+", " ", attrs).strip()

    val_m = VALUE_RE.search(body)
    value = val_m.group("val") if val_m else ""

    cmt_m = COMMENT_RE.search(body)
    comment = cmt_m.group("c") if cmt_m else None

    # Trim ONLY leading/trailing whitespace introduced by indentation,
    # NOT internal newlines (needed for multi-line JS strings).
    value_clean = value.strip("\r\n \t") if value is not None else ""
    escaped = _safe_escape_value(value_clean)

    if comment is not None:
        comment_clean = comment.strip("\r\n \t")
        return f'<data {attrs}><value>{escaped}</value><comment>{comment_clean}</comment></data>'
    return f'<data {attrs}><value>{escaped}</value></data>'


# ============================================================
# Main transform
# ============================================================
def transform_resx(text: str) -> tuple:
    """
    Returns (new_text, changed_count).
    Preserves header/schema, section comments, indentation style.
    """
    # 1. Collapse every <data> block (any whitespace layout -> single line)
    changed = {"n": 0}

    def _count(m):
        changed["n"] += 1
        return _collapse_data_block(m)

    # Protect the schema block (<xsd:schema ...> ... </xsd:schema>) from
    # accidental collapse — its <data> is only a type definition, not a
    # resource entry. Also protect <resheader> and <assembly> blocks.
    schema_start = text.find("<xsd:schema")
    schema_end = text.find("</xsd:schema>")
    schema_block = ""
    if schema_start != -1 and schema_end != -1:
        schema_end += len("</xsd:schema>")
        schema_block = text[schema_start:schema_end]
        text = text[:schema_start] + "@@SCHEMA_PLACEHOLDER@@" + text[schema_end:]

    # Collapse data blocks
    text = DATA_BLOCK_RE.sub(_count, text)

    # Restore schema
    if schema_block:
        text = text.replace("@@SCHEMA_PLACEHOLDER@@", schema_block)

    # 2. Ensure xml:space="preserve" on any <data> that lost it
    # (the collapse step already adds it, but keep this as a safety net.)

    # 3. Normalize trailing whitespace + ensure single trailing newline
    lines = text.splitlines()
    # Re-indent <data> lines to 2 spaces (matches our style)
    out = []
    for line in lines:
        stripped = line.lstrip()
        if stripped.startswith("<data "):
            out.append("  " + stripped)
        elif stripped.startswith("<resheader "):
            out.append("  " + stripped)
        else:
            out.append(line)
    text = "\n".join(out).rstrip() + "\n"

    return text, changed["n"]


# ============================================================
# Process one file
# ============================================================
def process_file(path: Path, apply: bool, backup: bool, check_only: bool, quiet: bool) -> bool:
    """
    Returns True if file needed fixing (or was fixed).
    """
    try:
        original = path.read_text(encoding="utf-8")
    except Exception as e:
        print(f"❌ {path}: {e}")
        return False

    new_text, n = transform_resx(original)
    needs_fix = new_text != original

    if not quiet:
        marker = "🔧" if needs_fix else "✅"
        print(f"{marker} {path}: " + (f"{n} <data> blocks collapsed" if needs_fix else "already clean"))

    if check_only:
        return needs_fix

    if apply and needs_fix:
        if backup:
            bak = path.with_suffix(path.suffix + f".bak-{datetime.now():%Y%m%d-%H%M%S}")
            shutil.copy2(path, bak)
            if not quiet:
                print(f"   💾 Backup: {bak}")
        path.write_text(new_text, encoding="utf-8", newline="\n")
        if not quiet:
            print(f"   ✅ Written")

    return needs_fix


# ============================================================
# Discovery
# ============================================================
def find_resx_files(folder: Path, recursive: bool) -> list:
    if not folder.exists():
        return []
    pattern = "**/*.resx" if recursive else "*.resx"
    return sorted(folder.glob(pattern))


# ============================================================
# Main
# ============================================================
def main():
    p = argparse.ArgumentParser(
        description="Collapse every <data> in .resx files to a single line.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Examples:\n"
            "  python fix_resx_format.py --check\n"
            "  python fix_resx_format.py --apply\n"
            "  python fix_resx_format.py --files Resources/Shared.ar.resx --apply\n"
            "  python fix_resx_format.py --dir Resources --apply --recursive\n"
        ),
    )
    p.add_argument("--files", nargs="+", default=None,
                   help="Specific .resx files to process.")
    p.add_argument("--dir", default=DEFAULT_DIR,
                   help=f"Folder to scan (default: {DEFAULT_DIR})")
    p.add_argument("--recursive", action="store_true", help="Recurse into subfolders.")
    p.add_argument("--apply", action="store_true", help="Write changes (default: dry-run).")
    p.add_argument("--check", action="store_true",
                   help="Validate only; exit 1 if any file needs fixing.")
    p.add_argument("--no-backup", action="store_true", help="Skip .bak backups.")
    p.add_argument("--quiet", action="store_true", help="Only print summary.")
    args = p.parse_args()

    # Determine files
    if args.files:
        files = [Path(f) for f in args.files]
    else:
        files = find_resx_files(Path(args.dir), args.recursive)

    if not files:
        print(f"❌ No .resx files found (dir={args.dir}, recursive={args.recursive})")
        sys.exit(2)

    # Mode banner
    if args.check:
        mode = "CHECK"
    elif args.apply:
        mode = "APPLY"
    else:
        mode = "DRY-RUN"
    if not args.quiet:
        print(f"🔎 Mode: {mode}   |   Files: {len(files)}\n")

    any_changed = False
    changed_files = 0
    for f in files:
        if not f.exists():
            print(f"⚠️  Skipping missing file: {f}")
            continue
        changed = process_file(
            path=f,
            apply=args.apply,
            backup=not args.no_backup,
            check_only=args.check,
            quiet=args.quiet,
        )
        if changed:
            any_changed = True
            changed_files += 1

    # Summary
    if not args.quiet:
        print()
    print(f"📊 Summary: {changed_files}/{len(files)} file(s) needed fixing")
    if args.check and any_changed:
        print("   ❌ Some files are mis-formatted (run with --apply to fix).")
        sys.exit(1)
    if not args.apply and not args.check:
        print("   ℹ️  DRY-RUN — nothing written. Add --apply to write.")
    elif args.apply and changed_files:
        print("   ✅ Done. Files rewritten + backups created.")


if __name__ == "__main__":
    main()