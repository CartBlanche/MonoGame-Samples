#!/usr/bin/env python3
"""
Extract unique CJK characters from Japanese and Chinese resource files.
This creates a minimal character set for SpriteFont generation.
"""

import xml.etree.ElementTree as ET
import sys
from pathlib import Path

def extract_characters_from_resx(file_path):
    """Extract all text values from a .resx file and return unique characters."""
    tree = ET.parse(file_path)
    root = tree.getroot()

    all_text = []
    # Find all <data> elements and extract their <value> content
    for data in root.findall(".//data"):
        value = data.find("value")
        if value is not None and value.text:
            all_text.append(value.text)

    # Combine all text and get unique characters
    combined_text = ''.join(all_text)
    unique_chars = sorted(set(combined_text))

    return unique_chars

def is_cjk_character(char):
    """Check if a character is CJK (Chinese/Japanese/Korean)."""
    code_point = ord(char)
    # CJK Unified Ideographs
    if 0x4E00 <= code_point <= 0x9FFF:
        return True
    # Hiragana
    if 0x3040 <= code_point <= 0x309F:
        return True
    # Katakana
    if 0x30A0 <= code_point <= 0x30FF:
        return True
    # Katakana Phonetic Extensions
    if 0x31F0 <= code_point <= 0x31FF:
        return True
    # CJK Symbols and Punctuation
    if 0x3000 <= code_point <= 0x303F:
        return True
    return False

def generate_character_region_xml(chars):
    """Generate XML character region entries for MonoGame .spritefont files."""
    regions = []
    current_start = None
    current_end = None

    for char in chars:
        code_point = ord(char)
        if current_start is None:
            current_start = code_point
            current_end = code_point
        elif code_point == current_end + 1:
            current_end = code_point
        else:
            # End current region and start new one
            regions.append(f"    <Start>&#x{current_start:04X};</Start>\n    <End>&#x{current_end:04X};</End>")
            current_start = code_point
            current_end = code_point

    # Add final region
    if current_start is not None:
        regions.append(f"    <Start>&#x{current_start:04X};</Start>\n    <End>&#x{current_end:04X};</End>")

    return '\n'.join(regions)

def main():
    # Get the script directory
    script_dir = Path(__file__).parent
    resources_dir = script_dir.parent / "Core" / "Game"

    # Process Japanese
    ja_file = resources_dir / "Resources.ja.resx"
    if ja_file.exists():
        print(f"Processing {ja_file}...")
        ja_chars = extract_characters_from_resx(ja_file)
        ja_cjk_chars = [c for c in ja_chars if is_cjk_character(c)]

        print(f"\nJapanese Statistics:")
        print(f"  Total unique characters: {len(ja_chars)}")
        print(f"  CJK characters: {len(ja_cjk_chars)}")
        print(f"  ASCII/Latin: {len(ja_chars) - len(ja_cjk_chars)}")

        # Write Japanese character list
        ja_output = script_dir / "japanese_characters.txt"
        with open(ja_output, 'w', encoding='utf-8') as f:
            f.write(''.join(ja_cjk_chars))
        print(f"  Saved to: {ja_output}")

        # Generate XML for .spritefont
        ja_xml_output = script_dir / "japanese_character_regions.xml"
        with open(ja_xml_output, 'w', encoding='utf-8') as f:
            f.write(generate_character_region_xml(ja_cjk_chars))
        print(f"  XML regions saved to: {ja_xml_output}")

    # Process Chinese
    zh_file = resources_dir / "Resources.zh.resx"
    if zh_file.exists():
        print(f"\nProcessing {zh_file}...")
        zh_chars = extract_characters_from_resx(zh_file)
        zh_cjk_chars = [c for c in zh_chars if is_cjk_character(c)]

        print(f"\nChinese Statistics:")
        print(f"  Total unique characters: {len(zh_chars)}")
        print(f"  CJK characters: {len(zh_cjk_chars)}")
        print(f"  ASCII/Latin: {len(zh_chars) - len(zh_cjk_chars)}")

        # Write Chinese character list
        zh_output = script_dir / "chinese_characters.txt"
        with open(zh_output, 'w', encoding='utf-8') as f:
            f.write(''.join(zh_cjk_chars))
        print(f"  Saved to: {zh_output}")

        # Generate XML for .spritefont
        zh_xml_output = script_dir / "chinese_character_regions.xml"
        with open(zh_xml_output, 'w', encoding='utf-8') as f:
            f.write(generate_character_region_xml(zh_cjk_chars))
        print(f"  XML regions saved to: {zh_xml_output}")

    # Combine both for unified CJK font
    if ja_file.exists() and zh_file.exists():
        print(f"\nCombining Japanese + Chinese...")
        combined_cjk = sorted(set(ja_cjk_chars + zh_cjk_chars))

        print(f"\nCombined CJK Statistics:")
        print(f"  Total unique CJK characters: {len(combined_cjk)}")

        # Write combined character list
        combined_output = script_dir / "cjk_characters.txt"
        with open(combined_output, 'w', encoding='utf-8') as f:
            f.write(''.join(combined_cjk))
        print(f"  Saved to: {combined_output}")

        # Generate XML for .spritefont
        combined_xml_output = script_dir / "cjk_character_regions.xml"
        with open(combined_xml_output, 'w', encoding='utf-8') as f:
            f.write(generate_character_region_xml(combined_cjk))
        print(f"  XML regions saved to: {combined_xml_output}")

if __name__ == "__main__":
    main()
