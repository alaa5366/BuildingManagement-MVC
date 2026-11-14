#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
translate_resx_02.py — Fill [TODO:<lang>] placeholders using DeepL API.

Usage:
    # 1. Set your API key
    #    Windows PowerShell:
    #        $env:DEEPL_API_KEY = "0a621f7d-ec8f-4a85-9153-57968b59e64c:fx"
    #                              0a621f7d-ec8f-4a85-9153-57968b59e64c:fx 
    #    Or create a .env file with: DEEPL_API_KEY=your-key-here
    #
    # 2. Dry-run (shows what would be translated):
    #    python translate_resx_02.py --lang en
    #
    # 3. Apply:
    #    python translate_resx_02.py --lang en --apply
    #    python translate_resx_02.py --lang fr --apply
    #    python translate_resx_02.py --lang de --apply
    #
    # 4. All languages:
    #    python translate_resx_02.py --lang en de fr --apply

    - Reads Shared.<lang>.resx.
    - Finds every value that starts with [TODO:<lang>] .
    - Extracts the Arabic text after the prefix.
    - Calls DeepL to translate ar -> target lang.
    - Writes translation back into <value>, removing the [TODO] prefix.
    - Preserves xml:space="preserve" and existing (non-TODO) values.
    - Creates .bak-YYYYMMDD-HHMMSS before writing.
"""

import argparse
import os
import re
import shutil
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime
from pathlib import Path
from xml.etree import ElementTree as ET

DEEPL_FREE_URL = "https://api-free.deepl.com/v2/translate"
DEEPL_PRO_URL  = "https://api.deepl.com/v2/translate"

# DeepL target language codes
DEEPL_TARGET = {
    "en": "EN",
    "de": "DE",
    "fr": "FR",
    "es": "ES",
    "it": "IT",
    "pt": "PT-PT",
    "ru": "RU",
    "tr": "TR",
    "nl": "NL",
    "pl": "PL",
}

TODO_RE = re.compile(r"^\[TODO:(?P<lang>[a-zA-Z-]+)\]\s*(?P<text>.*)$", re.DOTALL)


def parse_resx(path: Path):
    if not path.exists():
        return {}, []
    tree = ET.parse(path)
    root = tree.getroot()
    entries, order = {}, []
    for data in root.findall("data"):
        name = data.get("name")
        if name is None:
            continue
        v = data.find("value")
        value = v.text if (v is not None and v.text is not None) else ""
        entries[name] = value
        order.append(name)
    return entries, order


def _escape_xml_value(val: str) -> str:
    if not val:
        return ""
    out = val
    if "&" in out:
        out = re.sub(r"&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[0-9a-fA-F]+;)", "&amp;", out)
    out = out.replace("<", "&lt;").replace(">", "&gt;")
    return out


def write_resx(path: Path, entries: dict, order: list):
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<root>"]
    for name, value in [
        ("resmimetype", "text/microsoft-resx"),
        ("version", "2.0"),
        ("reader", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
        ("writer", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
    ]:
        lines.append(f'  <resheader name="{name}"><value>{value}</value></resheader>')
    lines.append("")
    for name in order:
        val = _escape_xml_value(entries.get(name) or "")
        lines.append(f'  <data name="{name}" xml:space="preserve"><value>{val}</value></data>')
    lines.append("</root>")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def translate_batch(texts: list, target_lang: str, api_key: str) -> list:
    """Translate a batch (DeepL max 50 texts per request)."""
    if not texts:
        return []
    data = [("text", t) for t in texts]
    data.append(("target_lang", DEEPL_TARGET.get(target_lang, target_lang.upper())))
    data.append(("source_lang", "AR"))
    data.append(("preserve_formatting", "1"))

    body = urllib.parse.urlencode(data, doseq=True).encode("utf-8")

    # Free vs Pro endpoint by key suffix
    url = DEEPL_FREE_URL if api_key.endswith(":fx") else DEEPL_PRO_URL

    req = urllib.request.Request(
        url,
        data=body,
        headers={
            "Authorization": f"DeepL-Auth-Key {api_key}",
            "Content-Type": "application/x-www-form-urlencoded",
        },
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        import json
        payload = json.loads(resp.read().decode("utf-8"))

    return [t["text"] for t in payload["translations"]]


def process_language(path: Path, lang: str, api_key: str, apply: bool):
    entries, order = parse_resx(path)
    if not entries:
        print(f"❌ No entries in {path}")
        return 0

    # Collect TODO keys
    to_translate = []  # (key, arabic_text)
    for name in order:
        val = entries[name] or ""
        m = TODO_RE.match(val)
        if m and m.group("lang").lower() == lang.lower():
            arabic = m.group("text").strip()
            if arabic:
                to_translate.append((name, arabic))

    print(f"\n📄 {path}")
    print(f"   Total keys     : {len(entries)}")
    print(f"   TODO to translate: {len(to_translate)}")

    if not to_translate:
        print("   ℹ️  Nothing to translate.")
        return 0

    if not apply:
        print("   🔍 DRY-RUN — first 5 would be translated:")
        for k, t in to_translate[:5]:
            print(f"      {k}: {t[:60]}{'...' if len(t) > 60 else ''}")
        return len(to_translate)

    # Translate in batches of 50
    BATCH = 50
    done = 0
    for i in range(0, len(to_translate), BATCH):
        chunk = to_translate[i:i + BATCH]
        keys = [k for k, _ in chunk]
        texts = [t for _, t in chunk]
        try:
            translations = translate_batch(texts, lang, api_key)
        except Exception as e:
            print(f"   ❌ Batch {i // BATCH + 1} failed: {e}")
            continue

        for k, tr in zip(keys, translations):
            entries[k] = tr
            done += 1

        print(f"   ✅ Batch {i // BATCH + 1}/{(len(to_translate) + BATCH - 1) // BATCH} "
              f"({done}/{len(to_translate)})")
        time.sleep(0.5)  # be nice to the API

    # Backup + write
    bak = path.with_suffix(path.suffix + f".bak-{datetime.now():%Y%m%d-%H%M%S}")
    shutil.copy2(path, bak)
    write_resx(path, entries, order)
    print(f"   💾 Backup: {bak}")
    print(f"   ✅ Wrote  : {path}  ({done} keys translated)")

    return done


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--lang", nargs="+", default=["en", "de", "fr"])
    p.add_argument("--resources-dir", default="Resources")
    p.add_argument("--apply", action="store_true")
    p.add_argument("--api-key", default=os.environ.get("DEEPL_API_KEY", ""))
    args = p.parse_args()

    if not args.api_key:
        print("❌ DEEPL_API_KEY not set.")
        print("   PowerShell: $env:DEEPL_API_KEY = 'your-key'")
        sys.exit(1)

    total = 0
    for lang in args.lang:
        path = Path(args.resources_dir) / f"Shared.{lang}.resx"
        total += process_language(path, lang, args.api_key, args.apply)

    print(f"\n📊 Total translated: {total}")
    if not args.apply:
        print("   ⚠️  DRY-RUN — nothing written. Add --apply to write.")


if __name__ == "__main__":
    main()