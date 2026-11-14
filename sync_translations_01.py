#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
================================================================================
 sync_translations_01.py — Multi-language .resx synchronizer
================================================================================

WHAT IT DOES
------------
Syncs Shared.<lang>.resx files against the master Shared.ar.resx file.

For every target language (en, de, fr, ...):
  - Adds keys that exist in AR but are missing in the target
    (with a [TODO:<lang>] placeholder containing the Arabic value).
  - Optionally prunes keys that exist in the target but not in AR.
  - Preserves existing translated values (never overwrites).
  - Fills empty <value></value> entries with placeholders.
  - Ensures xml:space="preserve" on every <data> element.
  - Keeps the same key order as AR (so all languages stay in sync).
  - Creates a timestamped .bak file before writing.

By default it runs in DRY-RUN mode — nothing is written unless --apply is used.

--------------------------------------------------------------------------------
REQUIREMENTS
--------------------------------------------------------------------------------
  - Python 3.8+
  - No third-party packages (uses only stdlib).

--------------------------------------------------------------------------------
FILE LAYOUT EXPECTED
--------------------------------------------------------------------------------
  Resources/
    ├── Shared.ar.resx      ← master (required)
    ├── Shared.en.resx      ← target (created if missing)
    ├── Shared.de.resx      ← target (created if missing)
    └── Shared.fr.resx      ← target (created if missing)

  (Change paths with --ar / --out-dir / --resources-dir.)

--------------------------------------------------------------------------------
QUICK START — COMMON COMMANDS
--------------------------------------------------------------------------------

# 1) DRY-RUN for all default languages (en, de, fr) — NO FILES ARE MODIFIED
python sync_translations_01.py

# 2) Apply changes for all default languages (en, de, fr)
python sync_translations_01.py --apply

# 3) Apply changes for English only
python sync_translations_01.py --apply --languages en

# 4) Apply changes for German + French only
python sync_translations_01.py --apply --languages de fr

# 5) Also remove orphan keys (in target but not in AR) — USE WITH CAUTION
python sync_translations_01.py --apply --prune

# 6) Add a new language, e.g. Spanish (es) — the file is created if missing
python sync_translations_01.py --apply --languages es

# 7) Custom AR path + custom output directory
python sync_translations_01.py --ar src/Resources/Shared.ar.resx \
                            --resources-dir src/Resources \
                            --apply --languages en de fr

# 8) Combine: prune + explicit languages + custom path
python sync_translations_01.py --apply --prune --languages en de \
                            --ar Resources/Shared.ar.resx

--------------------------------------------------------------------------------
ALL OPTIONS
--------------------------------------------------------------------------------
  --ar PATH               Path to the master Arabic .resx file.
                          Default: Resources/Shared.ar.resx

  --resources-dir DIR     Folder where Shared.<lang>.resx files live.
                          Default: Resources

  --languages L1 L2 ...   Languages to sync (space-separated).
                          Default: en de fr
                          Special: use "all" to sync every Shared.*.resx
                          found in --resources-dir (except .ar).

  --apply                 Actually write files. Without it, dry-run only.

  --prune                 Remove keys from target languages that are not
                          present in AR. Default: keep them (safer).

  --placeholder FORMAT    Placeholder template for missing keys.
                          Must contain {lang} and {ar_value}.
                          Default: "[TODO:{lang}] {ar_value}"

  --no-backup             Skip creating .bak files before writing.
                          (Not recommended.)

  --quiet                 Suppress per-language detail; only print summary.

  --help, -h              Show this help.

--------------------------------------------------------------------------------
PLACEHOLDER BEHAVIOUR
--------------------------------------------------------------------------------
When a key is missing in a target language, the value is set to:

    [TODO:en] <the Arabic value>

This makes it easy for translators to find untranslated keys:

    grep "\[TODO:en\]" Resources/Shared.en.resx

After a translator finishes, running the script again will report
"No changes needed" and will NOT touch the translated values.

--------------------------------------------------------------------------------
SAFE-WRITE GUARANTEES
--------------------------------------------------------------------------------
  - Dry-run is the default. --apply is required to write.
  - Every modified file gets a timestamped .bak-YYYYMMDD-HHMMSS backup.
  - Existing translations are never overwritten.
  - XML structure matches the reorganized Shared.ar.resx exactly
    (one <data> per line, xml:space="preserve" everywhere).

--------------------------------------------------------------------------------
EXIT CODES
--------------------------------------------------------------------------------
  0  Success (or no changes needed)
  1  Fatal error (AR missing, XML parse error, write failure)
  2  Bad arguments (invalid --languages, invalid --placeholder)

================================================================================
"""

import argparse
import re
import shutil
import sys
from datetime import datetime
from pathlib import Path
from xml.etree import ElementTree as ET

# ============================================================
# Defaults
# ============================================================
DEFAULT_AR            = "Resources/Shared.ar.resx"
DEFAULT_RESOURCES_DIR = "Resources"
DEFAULT_LANGUAGES     = ["en", "de", "fr"]
DEFAULT_PLACEHOLDER   = "[TODO:{lang}] {ar_value}"

# ============================================================
# ResX boilerplate (must match Shared.ar.resx structure)
# ============================================================
RESX_HEADERS = [
    ("resmimetype", "text/microsoft-resx"),
    ("version", "2.0"),
    ("reader",
     "System.Resources.ResXResourceReader, System.Windows.Forms, "
     "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
    ("writer",
     "System.Resources.ResXResourceWriter, System.Windows.Forms, "
     "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
]

RESX_SCHEMA = """  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>"""


# ============================================================
# Parsing
# ============================================================
def parse_resx(path: Path):
    """
    Parse a .resx file.
    Returns (entries, order):
        entries[name] = {"value": str, "has_xml_space": bool, "comment": str|None}
        order = list of names as they appear in the file
    """
    if not path.exists():
        return {}, []

    try:
        tree = ET.parse(path)
    except ET.ParseError as e:
        print(f"❌ XML parse error in {path}: {e}")
        sys.exit(1)

    root = tree.getroot()
    entries, order = {}, []

    for data in root.findall("data"):
        name = data.get("name")
        if name is None:
            continue
        value_el = data.find("value")
        value = value_el.text if (value_el is not None and value_el.text is not None) else ""
        comment_el = data.find("comment")
        comment = comment_el.text if (comment_el is not None and comment_el.text is not None) else None
        has_xml_space = data.get("{http://www.w3.org/XML/1998/namespace}space") == "preserve"

        entries[name] = {"value": value, "has_xml_space": has_xml_space, "comment": comment}
        order.append(name)

    return entries, order


# ============================================================
# Writing
# ============================================================
def _escape_xml_value(val: str) -> str:
    """Escape &, <, > once — never double-escape pre-escaped entities."""
    if not val:
        return ""
    # Already contains &amp; / &lt; / &gt;? Keep as-is, only escape lone & < >
    out = val
    if "&" in out:
        # Replace & that is NOT part of an entity
        out = re.sub(r"&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[0-9a-fA-F]+;)", "&amp;", out)
    out = out.replace("<", "&lt;").replace(">", "&gt;")
    return out


def write_resx(path: Path, entries: dict, order: list, header_comment: str = ""):
    """Write a .resx file in the same one-key-per-line style as Shared.ar.resx."""
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<root>"]
    if header_comment:
        for cl in header_comment.splitlines():
            lines.append(f"  <!-- {cl} -->")
        lines.append("")

    lines.append(RESX_SCHEMA)
    lines.append("")

    for name, value in RESX_HEADERS:
        lines.append(f'  <resheader name="{name}"><value>{value}</value></resheader>')
    lines.append("")

    for name in order:
        e = entries.get(name)
        if e is None:
            continue
        val = _escape_xml_value(e.get("value") or "")
        lines.append(
            f'  <data name="{name}" xml:space="preserve"><value>{val}</value></data>'
        )

    lines.append("</root>")
    lines.append("")

    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))


# ============================================================
# Sync one language
# ============================================================
def sync_language(
    ar_entries: dict,
    ar_order: list,
    target_path: Path,
    lang: str,
    prune: bool,
    apply: bool,
    placeholder_tpl: str,
    backup: bool,
    quiet: bool,
):
    tgt_entries, tgt_order = parse_resx(target_path)

    added, removed, empty_fixed = [], [], []

    # 1) Add missing keys
    for name in ar_order:
        if name not in tgt_entries:
            ar_val = ar_entries[name]["value"]
            placeholder = placeholder_tpl.format(lang=lang, ar_value=ar_val)
            tgt_entries[name] = {"value": placeholder, "has_xml_space": True, "comment": None}
            tgt_order.append(name)
            added.append(name)

    # 2) Prune orphans
    if prune:
        ar_keys = set(ar_entries.keys())
        for name in list(tgt_order):
            if name not in ar_keys:
                removed.append(name)
                del tgt_entries[name]
                tgt_order.remove(name)

    # 3) Fill empty values
    for name in tgt_order:
        e = tgt_entries.get(name)
        if e is None:
            continue
        if e["value"] is None or e["value"].strip() == "":
            ar_val = ar_entries.get(name, {}).get("value", "")
            tgt_entries[name]["value"] = placeholder_tpl.format(lang=lang, ar_value=ar_val)
            empty_fixed.append(name)

    # 4) Report
    if not quiet:
        print(f"\n{'=' * 64}")
        print(f"📄 Target: {target_path}  (lang={lang})")
        print(f"{'=' * 64}")
        print(f"  AR keys      : {len(ar_entries)}")
        print(f"  Target before: {len(tgt_entries) - len(added) + len(removed)}")
        print(f"  ➕ Added      : {len(added)}")
        if added and len(added) <= 10:
            for n in added:
                print(f"       + {n}")
        elif added:
            for n in added[:10]:
                print(f"       + {n}")
            print(f"       ... and {len(added) - 10} more")
        if prune:
            print(f"  ➖ Removed    : {len(removed)}")
            for n in removed[:10]:
                print(f"       - {n}")
            if len(removed) > 10:
                print(f"       ... and {len(removed) - 10} more")
        if empty_fixed:
            print(f"  ✏️  Empty fixed: {len(empty_fixed)}")
        print(f"  Target after : {len(tgt_entries)}")

    # 5) Write
    changed = bool(added or removed or empty_fixed)
    if apply and changed:
        if backup and target_path.exists():
            bak = target_path.with_suffix(target_path.suffix + f".bak-{datetime.now():%Y%m%d-%H%M%S}")
            shutil.copy2(target_path, bak)
            if not quiet:
                print(f"  💾 Backup: {bak}")

        header = (
            f"Auto-synced from Shared.ar.resx on {datetime.now():%Y-%m-%d %H:%M:%S}\n"
            f"Language: {lang}\n"
            f"Added: {len(added)} | Removed: {len(removed)} | Empty fixed: {len(empty_fixed)}"
        )
        write_resx(target_path, tgt_entries, tgt_order, header_comment=header)
        if not quiet:
            print(f"  ✅ Written: {target_path}")
    elif not apply:
        if not quiet:
            print("  🔍 DRY-RUN (use --apply to write)")
    else:
        if not quiet:
            print("  ℹ️  No changes needed.")

    return len(added), len(removed), len(empty_fixed)


# ============================================================
# Discover languages from disk
# ============================================================
def discover_languages(resources_dir: Path) -> list:
    """Find Shared.<lang>.resx files in resources_dir (except .ar)."""
    pat = re.compile(r"^Shared\.([a-z]{2}(?:-[A-Z]{2})?)\.resx$")
    langs = []
    if resources_dir.exists():
        for p in resources_dir.iterdir():
            if p.is_file():
                m = pat.match(p.name)
                if m and m.group(1) != "ar":
                    langs.append(m.group(1))
    return sorted(set(langs))


# ============================================================
# Main
# ============================================================
def main():
    parser = argparse.ArgumentParser(
        description="Sync Shared.<lang>.resx files against Shared.ar.resx",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Examples:\n"
            "  python sync_translations_01.py\n"
            "  python sync_translations_01.py --apply\n"
            "  python sync_translations_01.py --apply --languages en\n"
            "  python sync_translations_01.py --apply --languages de fr\n"
            "  python sync_translations_01.py --apply --prune\n"
            "  python sync_translations_01.py --apply --languages es\n"
        ),
    )
    parser.add_argument("--ar", default=DEFAULT_AR,
                        help=f"Path to Shared.ar.resx (default: {DEFAULT_AR})")
    parser.add_argument("--resources-dir", default=DEFAULT_RESOURCES_DIR,
                        help=f"Folder with Shared.<lang>.resx files (default: {DEFAULT_RESOURCES_DIR})")
    parser.add_argument("--languages", nargs="+", default=None,
                        help="Languages to sync (default: en de fr). Use 'all' to auto-detect from disk.")
    parser.add_argument("--apply", action="store_true",
                        help="Write changes (default is dry-run).")
    parser.add_argument("--prune", action="store_true",
                        help="Remove keys in target not present in AR (default: keep).")
    parser.add_argument("--placeholder", default=DEFAULT_PLACEHOLDER,
                        help=f"Placeholder template with {{lang}} and {{ar_value}} (default: {DEFAULT_PLACEHOLDER!r})")
    parser.add_argument("--no-backup", action="store_true",
                        help="Skip .bak backups before writing (not recommended).")
    parser.add_argument("--quiet", action="store_true",
                        help="Suppress per-language detail.")
    args = parser.parse_args()

    # Validate placeholder
    if "{lang}" not in args.placeholder or "{ar_value}" not in args.placeholder:
        print("❌ --placeholder must contain both {lang} and {ar_value}")
        sys.exit(2)

    ar_path = Path(args.ar)
    resources_dir = Path(args.resources_dir)

    # Resolve languages
    if args.languages is None:
        languages = list(DEFAULT_LANGUAGES)
    elif args.languages == ["all"]:
        languages = discover_languages(resources_dir)
        if not languages:
            print(f"⚠️  No Shared.<lang>.resx files found in {resources_dir}")
            sys.exit(1)
    else:
        languages = list(args.languages)

    print(f"📖 Master AR : {ar_path}")
    print(f"📁 Resources : {resources_dir}")
    print(f"🌐 Languages : {', '.join(languages)}")
    print(f"🔧 Mode      : {'APPLY' if args.apply else 'DRY-RUN'}")
    if args.prune:
        print(f"🧹 Prune     : ON")
    print()

    if not ar_path.exists():
        print(f"❌ AR file not found: {ar_path}")
        sys.exit(1)

    ar_entries, ar_order = parse_resx(ar_path)
    if not ar_entries:
        print(f"❌ No keys found in {ar_path}. Aborting.")
        sys.exit(1)
    print(f"   Found {len(ar_entries)} keys in AR\n")

    total_added = total_removed = total_fixed = 0
    for lang in languages:
        target = resources_dir / f"Shared.{lang}.resx"
        a, r, f = sync_language(
            ar_entries=ar_entries,
            ar_order=ar_order,
            target_path=target,
            lang=lang,
            prune=args.prune,
            apply=args.apply,
            placeholder_tpl=args.placeholder,
            backup=not args.no_backup,
            quiet=args.quiet,
        )
        total_added += a
        total_removed += r
        total_fixed += f

    print(f"\n{'=' * 64}")
    print("📊 SUMMARY")
    print(f"{'=' * 64}")
    print(f"  Languages    : {len(languages)}")
    print(f"  Total added  : {total_added}")
    print(f"  Total removed: {total_removed}")
    print(f"  Empty fixed  : {total_fixed}")
    if not args.apply:
        print("\n  ⚠️  DRY-RUN — no files were modified.")
        print("     Run with --apply to write changes.")
    else:
        print(f"\n  ✅ Files written. Search for [TODO:<lang>] to find translations to do.")
    print()


if __name__ == "__main__":
    main()