"""
Download PBR texture sets from ambientCG (CC0 license) for LowPoly materials.
Uses 1K-JPG resolution to keep size manageable.

Each set includes: Color, NormalGL, Roughness, Displacement, AmbientOcclusion (when available).

Usage: py -3 download_textures.py
Output: Import/Textures/LowPoly/{Category}/{TextureName}_*.jpg
"""

import os
import sys
import zipfile
import io
import urllib.request
import json
import time

# ------ Configuration ------
OUTPUT_BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Textures", "LowPoly")
RESOLUTION = "1K-JPG"

# Texture sets to download from ambientCG
# Format: (ambientCG_ID, local_category_folder)
TEXTURE_SETS = [
    # Wood
    ("Wood051",      "Wood"),       # Fine grain - light furniture
    ("Wood049",      "Wood"),       # Plank floor - dark furniture
    ("Wood066",      "Wood"),       # Medium veneer - chairs
    ("Wood058",      "Wood"),       # Rough wood - outdoor/fence

    # Metal
    ("Metal032",     "Metal"),      # Brushed stainless steel
    ("Metal049A",    "Metal"),      # Dark iron/cast
    ("Metal048A",    "Metal"),      # Painted industrial metal

    # Fabric
    ("Fabric030",    "Fabric"),     # Woven plain fabric
    ("Fabric061",    "Fabric"),     # Soft cushion/pillow
    ("Fabric066",    "Fabric"),     # Office upholstery

    # Concrete & Stone
    ("Concrete034",  "Stone"),      # Smooth concrete
    ("Concrete042A", "Stone"),      # Rough stone

    # Ceramic & Tiles
    ("Tiles107",     "Ceramic"),    # White ceramic tile
    ("Tiles074",     "Ceramic"),    # Granite-like tile

    # Plastic
    ("Plastic006",   "Plastic"),    # Smooth plastic
    ("Plastic010",   "Plastic"),    # Matte plastic (appliances)

    # Ground / Nature
    ("Ground037",    "Nature"),     # Grass
    ("Bark012",      "Nature"),     # Tree bark / leaves
]


def download_and_extract(asset_id, category):
    """Download a 1K-JPG zip from ambientCG and extract to the target folder."""
    url = f"https://ambientcg.com/get?file={asset_id}_{RESOLUTION}.zip"
    out_dir = os.path.join(OUTPUT_BASE, category)
    os.makedirs(out_dir, exist_ok=True)

    # Check if already downloaded
    marker = os.path.join(out_dir, f"{asset_id}_1K_Color.jpg")
    if os.path.exists(marker):
        print(f"  [SKIP] {asset_id} already exists")
        return True

    print(f"  [GET]  {asset_id} ({category}) ... ", end="", flush=True)
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "ProceduralCities-TextureFetcher/1.0"})
        with urllib.request.urlopen(req, timeout=60) as response:
            data = response.read()
            size_mb = len(data) / (1024 * 1024)
            print(f"{size_mb:.1f} MB ... ", end="", flush=True)

            with zipfile.ZipFile(io.BytesIO(data)) as zf:
                extracted = 0
                for member in zf.namelist():
                    # Only extract image files, skip directories
                    if member.endswith(('.jpg', '.png', '.exr')):
                        # Flatten: strip any subdirectory from the zip
                        filename = os.path.basename(member)
                        target = os.path.join(out_dir, filename)
                        with zf.open(member) as src, open(target, 'wb') as dst:
                            dst.write(src.read())
                        extracted += 1

                print(f"{extracted} files extracted")
                return True

    except Exception as e:
        print(f"FAILED: {e}")
        return False


def main():
    print(f"=== ambientCG Texture Downloader (CC0 License) ===")
    print(f"Resolution: {RESOLUTION}")
    print(f"Output: {OUTPUT_BASE}")
    print(f"Sets to download: {len(TEXTURE_SETS)}")
    print()

    success = 0
    failed = 0

    for asset_id, category in TEXTURE_SETS:
        if download_and_extract(asset_id, category):
            success += 1
        else:
            failed += 1
        # Be polite to the server
        time.sleep(0.5)

    print()
    print(f"Done: {success} downloaded, {failed} failed")

    # List all downloaded textures
    total_files = 0
    total_size = 0
    for root, dirs, files in os.walk(OUTPUT_BASE):
        for f in files:
            fp = os.path.join(root, f)
            total_files += 1
            total_size += os.path.getsize(fp)

    print(f"Total: {total_files} texture files, {total_size / (1024*1024):.1f} MB")


if __name__ == "__main__":
    main()
