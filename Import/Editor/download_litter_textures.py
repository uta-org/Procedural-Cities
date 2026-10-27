"""
Download the source textures of the litter props (P23, owner 2026-10-27: "look for open-licence
textures, even if CC-BY attribution, to build these low-poly props").

- ambientCG (CC0 1.0, https://docs.ambientcg.com/license/): only each set's 1K Color map.
- Wikimedia Commons (public domain): two 1912 newspaper front pages, as 1024 px wide thumbnails.

The sources go to Import/Textures/Litter/Sources~ (a folder Unity does not import);
build_litter_atlas.py turns them into Import/Textures/Litter/LitterAtlas.png. Every source and its
licence is listed in Import/Textures/Litter/CREDITS.txt.

Usage: py -3 download_litter_textures.py
"""

import io
import json
import os
import time
import urllib.parse
import urllib.request
import zipfile

OUTPUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Textures", "Litter", "Sources~")
USER_AGENT = {"User-Agent": "ProceduralCities-LitterTextures/1.0 (personal game project)"}

# (ambientCG id, what it dresses)
AMBIENTCG = [
    ("Cardboard004", "cardboard box, clean"),
    ("Cardboard001", "cardboard box, worn and torn"),
    ("Paper003", "crumpled paper ball, paper cups"),
    ("Foil003", "crushed can rims and ends"),
    ("Plastic013A", "plastic bags"),
    ("Plastic012A", "black bin bag"),
    ("Rubber004", "old tyre"),
    ("Fabric036", "mattress cover"),
]

# (Commons file, local name)
COMMONS = [
    ("File:Il secolo - 17 aprile 1912.jpg", "news_secolo.jpg"),
    ("File:TheTimesDispatch-Titanic-1912-4-16.jpg", "news_dispatch.jpg"),
]


def get_ambientcg(asset_id):
    """Download one 1K-JPG set and keep only its Color map."""
    target = os.path.join(OUTPUT, f"{asset_id}_1K-JPG_Color.jpg")
    if os.path.exists(target):
        print(f"  [SKIP] {asset_id}")
        return
    url = f"https://ambientcg.com/get?file={asset_id}_1K-JPG.zip"
    data = urllib.request.urlopen(urllib.request.Request(url, headers=USER_AGENT), timeout=60).read()
    with zipfile.ZipFile(io.BytesIO(data)) as zf:
        for member in zf.namelist():
            if member.endswith("_Color.jpg"):
                with zf.open(member) as src, open(target, "wb") as dst:
                    dst.write(src.read())
    print(f"  [GET]  {asset_id}")


def get_commons(titles):
    """Download the Commons files as 1024 px wide thumbnails and write their licence data."""
    api = "https://commons.wikimedia.org/w/api.php?"
    query = {"action": "query", "titles": "|".join(t for t, _ in titles), "prop": "imageinfo",
             "iiprop": "url|size|extmetadata", "iiurlwidth": 1024, "format": "json"}
    pages = json.load(urllib.request.urlopen(urllib.request.Request(api + urllib.parse.urlencode(query), headers=USER_AGENT), timeout=30))
    names = dict(titles)
    meta = []
    for page in pages["query"]["pages"].values():
        info = page["imageinfo"][0]
        md = info["extmetadata"]
        name = names[page["title"]]
        time.sleep(2)  # Commons answers 429 to quick bursts
        data = urllib.request.urlopen(urllib.request.Request(info["thumburl"], headers=USER_AGENT), timeout=60).read()
        with open(os.path.join(OUTPUT, name), "wb") as f:
            f.write(data)
        meta.append({"file": name, "title": page["title"], "page": info["descriptionurl"],
                     "licence": md.get("LicenseShortName", {}).get("value"), "date": md.get("DateTimeOriginal", {}).get("value", "")[:40]})
        print(f"  [GET]  {name} ({meta[-1]['licence']})")
    with io.open(os.path.join(OUTPUT, "commons.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)


def main():
    os.makedirs(OUTPUT, exist_ok=True)
    print("=== Litter textures: ambientCG (CC0) and Wikimedia Commons (public domain) ===")
    for asset_id, _ in AMBIENTCG:
        get_ambientcg(asset_id)
        time.sleep(0.5)
    get_commons(COMMONS)


if __name__ == "__main__":
    main()
