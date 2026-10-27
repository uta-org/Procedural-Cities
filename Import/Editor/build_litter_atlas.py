"""
Build Import/Textures/Litter/LitterAtlas.png, the one texture of every litter prop (P23).

1024 x 1024, a grid of 128 px cells (column, row; row 0 at the top). Each prop's faces map inside its
own cell(s), and a prop's colour variants sit side by side, so the game picks a variant by shifting U
(LitterCatalog in the runtime and GenerateLitterProps in the editor mirror this layout):

    (0,0)-(1,1) newspaper, Il Secolo       (2,0)-(3,1) newspaper, The Times Dispatch
    (4..7,0)    can: red, blue, green, silver (label above, metal rim strip below)
    (3,1) (4,1) paper ball: white, newsprint   (5,1) (6,1) cardboard: clean (taped), worn
    (0..2,2)    cup: red, green, brown band    (3..5,2) plastic bottle: blue, green, clear
    (6,2) (7,2) glass bottle: green, brown     (0..2,3) plastic bag: white, blue, black
    (3..5,3)    drink carton: orange, milk, apple   (6,3) cigarette butts   (7,3) traffic cone
    (0,4)-(1,4) tyre: tread | sidewall         (3,4) (4,4) chair: bare wood, green paint
    (0,5)-(3,6) mattress, a little stained     (4,5)-(7,6) mattress, dirtier
                (top 192 px: the quilted cover; bottom 64 px: the ticking of its sides)

Sources: download_litter_textures.py (ambientCG CC0, Commons public domain) and the low-poly Wood051
(ambientCG CC0). Usage: py -3 build_litter_atlas.py
"""

import os
import random

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
TEXTURES = os.path.join(HERE, "..", "Textures")
SOURCES = os.path.join(TEXTURES, "Litter", "Sources~")
OUTPUT = os.path.join(TEXTURES, "Litter", "LitterAtlas.png")
CELL = 128
SIZE = 1024

random.seed(23)
atlas = Image.new("RGB", (SIZE, SIZE), (128, 128, 128))


def src(name):
    path = os.path.join(SOURCES, name)
    if not os.path.exists(path):
        path = os.path.join(TEXTURES, "LowPoly", "Wood", name)
    return Image.open(path).convert("RGB")


def crop(img, w, h, scale=1.0, x=0.5, y=0.5):
    """A w x h piece of img: a region `scale` times the target size (in source pixels per target
    pixel) centred at (x, y) of the source, resized."""
    cw, ch = min(img.width, int(w * scale)), min(img.height, int(h * scale))
    left = int((img.width - cw) * x)
    top = int((img.height - ch) * y)
    return img.crop((left, top, left + cw, top + ch)).resize((w, h), Image.LANCZOS)


def tint(img, rgb, keep=0.0, strength=1.0):
    """Multiply img's luminance by rgb; keep = share of the original colour kept; strength = how much
    of the texture shows over the flat colour (1 all of it, 0 a flat rgb)."""
    grey = ImageOps.grayscale(img)
    grey = ImageOps.autocontrast(grey, cutoff=2)
    lum = ImageEnhance.Brightness(grey.convert("RGB")).enhance(1.15)
    colour = Image.new("RGB", img.size, rgb)
    tinted = ImageChops.multiply(lum, colour)
    tinted = Image.blend(tinted, img, keep) if keep > 0 else tinted
    return Image.blend(colour, tinted, strength) if strength < 1 else tinted


def solid(w, h, rgb, noise=6):
    img = Image.new("RGB", (w, h), rgb)
    px = img.load()
    for yy in range(h):
        for xx in range(w):
            n = random.randint(-noise, noise)
            r, g, b = rgb
            px[xx, yy] = (max(0, min(255, r + n)), max(0, min(255, g + n)), max(0, min(255, b + n)))
    return img


def put(img, col, row):
    atlas.paste(img, (col * CELL, row * CELL))


def text_lines(draw, box, rgb, rows, seed):
    """Short grey bars that read as printed text from a distance."""
    rnd = random.Random(seed)
    x0, y0, x1, y1 = box
    step = (y1 - y0) / rows
    for i in range(rows):
        y = int(y0 + i * step)
        x = x0
        while x < x1 - 4:
            w = rnd.randint(4, 14)
            draw.line((x, y, min(x1, x + w), y), fill=rgb, width=1)
            x += w + rnd.randint(2, 4)


# ── Newspapers: the top of two 1912 front pages, aged ──
for col, name in ((0, "news_secolo.jpg"), (2, "news_dispatch.jpg")):
    page = src(name)
    square = page.crop((0, 0, page.width, page.width)).resize((256, 256), Image.LANCZOS)
    aged = ImageChops.multiply(square, Image.new("RGB", square.size, (236, 229, 210)))
    aged = ImageEnhance.Contrast(aged).enhance(0.85)
    d = ImageDraw.Draw(aged)
    d.line((128, 0, 128, 255), fill=(190, 182, 165), width=2)  # the fold
    put(aged, col, 0)

# ── Cans: label (96 px) over a metal strip (32 px) for the rims and ends ──
foil = src("Foil003_1K-JPG_Color.jpg")
metal = tint(crop(foil, CELL, 32, 3.0), (214, 216, 222), strength=0.45)
for i, rgb in enumerate(((196, 28, 36), (28, 72, 172), (36, 136, 64), (186, 190, 198))):
    cell = solid(CELL, CELL, rgb, 5)
    d = ImageDraw.Draw(cell)
    accent = (245, 245, 245) if i < 3 else (40, 60, 150)
    for k in range(6):  # a white wave across the label
        y = 40 + int(10 * ((k % 2) * 2 - 1))
        d.line((k * 26, y, (k + 1) * 26, 40 - (y - 40)), fill=accent, width=7)
    d.ellipse((50, 54, 78, 82), fill=accent)
    text_lines(d, (10, 86, 118, 95), tuple(max(0, c - 60) for c in rgb), 2, 10 + i)
    cell.paste(metal, (0, 96))
    put(cell, 4 + i, 0)

# ── Paper balls: white creased paper, newsprint ──
paper = src("Paper003_1K-JPG_Color.jpg")
put(ImageEnhance.Brightness(crop(paper, CELL, CELL, 3.0)).enhance(0.92), 3, 1)
news = src("news_secolo.jpg")
put(ImageChops.multiply(crop(news, CELL, CELL, 2.0, 0.3, 0.6), Image.new("RGB", (CELL, CELL), (232, 226, 208))), 4, 1)

# ── Cardboard: clean with a strip of tape, worn ──
clean = crop(src("Cardboard004_1K-JPG_Color.jpg"), CELL, CELL, 4.0)
d = ImageDraw.Draw(clean, "RGBA")
d.rectangle((0, 57, 127, 71), fill=(214, 186, 128, 170))
put(clean, 5, 1)
put(crop(src("Cardboard001_1K-JPG_Color.jpg"), CELL, CELL, 4.0, 0.3, 0.4), 6, 1)

# ── Cups: white paper with a coloured band; the inside below ──
cup_paper = tint(crop(paper, CELL, CELL, 2.0), (246, 245, 240), strength=0.2)
for i, rgb in enumerate(((186, 32, 40), (30, 108, 70), (108, 68, 40))):
    cell = cup_paper.copy()
    d = ImageDraw.Draw(cell)
    d.rectangle((0, 0, 127, 7), fill=(226, 226, 222))
    d.rectangle((0, 38, 127, 84), fill=rgb)
    d.ellipse((50, 46, 78, 76), outline=(245, 245, 245), width=3)
    d.rectangle((0, 100, 127, 127), fill=(236, 232, 224))
    put(cell, i, 2)

# ── Plastic bottles: tinted body, a label band, the cap on top ──
for i, (body, cap) in enumerate((((168, 204, 226), (36, 80, 196)), ((120, 188, 138), (30, 120, 50)), ((214, 224, 230), (210, 36, 36)))):
    cell = solid(CELL, CELL, body, 4)
    d = ImageDraw.Draw(cell, "RGBA")
    for x in (18, 22, 70):  # highlights
        d.line((x, 16, x, 127), fill=(255, 255, 255, 110), width=3)
    d.rectangle((0, 0, 127, 14), fill=cap)
    d.rectangle((0, 50, 127, 90), fill=(245, 245, 245))
    d.rectangle((0, 62, 127, 76), fill=cap)
    text_lines(d, (8, 80, 120, 88), (120, 120, 120), 2, 20 + i)
    put(cell, 3 + i, 2)

# ── Glass bottles: green and brown with a paper label and a foil neck ──
for i, (glass, neck) in enumerate((((38, 92, 44), (196, 168, 60)), ((92, 50, 20), (150, 30, 30)))):
    cell = solid(CELL, CELL, glass, 4)
    d = ImageDraw.Draw(cell, "RGBA")
    d.line((24, 14, 24, 127), fill=(255, 255, 255, 90), width=5)
    d.line((32, 14, 32, 127), fill=(255, 255, 255, 50), width=2)
    d.rectangle((0, 0, 127, 12), fill=neck)
    d.rectangle((0, 56, 127, 96), fill=(232, 222, 192))
    d.ellipse((44, 62, 84, 90), outline=(170, 30, 30), width=3)
    put(cell, 6 + i, 2)

# ── Plastic bags: white, blue, a black bin bag ──
bag = src("Plastic013A_1K-JPG_Color.jpg")
put(tint(crop(bag, CELL, CELL, 3.0), (242, 242, 240), strength=0.4), 0, 3)
put(tint(crop(bag, CELL, CELL, 3.0, 0.2, 0.7), (118, 160, 222), strength=0.45), 1, 3)
put(tint(crop(src("Plastic012A_1K-JPG_Color.jpg"), CELL, CELL, 3.0), (52, 52, 56), strength=0.6), 2, 3)

# ── Drink cartons: orange juice, milk, apple ──
for i, (main, band, mark) in enumerate((((242, 140, 22), (250, 250, 245), (60, 150, 50)),
                                         ((248, 248, 244), (36, 88, 188), (36, 88, 188)),
                                         ((92, 170, 58), (250, 250, 245), (200, 30, 30)))):
    cell = solid(CELL, CELL, main, 4)
    d = ImageDraw.Draw(cell)
    d.rectangle((0, 0, 127, 22), fill=band)
    d.rectangle((0, 104, 127, 127), fill=band)
    d.ellipse((44, 44, 84, 84), fill=mark)
    d.line((64, 22, 64, 104), fill=tuple(max(0, c - 25) for c in main), width=1)  # the fold
    text_lines(d, (10, 108, 118, 124), (110, 110, 110), 3, 30 + i)
    put(cell, 3 + i, 3)

# ── Cigarette butts: cork filter above, paper and ash below ──
cell = solid(CELL, CELL, (206, 132, 60), 10)
d = ImageDraw.Draw(cell)
for _ in range(220):
    x, y = random.randint(0, 127), random.randint(0, 63)
    d.point((x, y), fill=(160, 96, 40))
d.rectangle((0, 64, 127, 127), fill=(236, 234, 226))
d.rectangle((0, 108, 127, 127), fill=(96, 92, 88))
d.line((0, 64, 127, 64), fill=(200, 196, 180), width=2)
put(cell, 6, 3)

# ── Traffic cone: orange, two white bands, the black base ──
cell = solid(CELL, CELL, (240, 92, 22), 5)
d = ImageDraw.Draw(cell)
d.rectangle((0, 30, 127, 48), fill=(244, 244, 244))
d.rectangle((0, 70, 127, 86), fill=(244, 244, 244))
d.rectangle((0, 110, 127, 127), fill=(28, 28, 30))
put(cell, 7, 3)

# ── Tyre: tread (left), sidewall (right) ──
rubber = tint(crop(src("Rubber004_1K-JPG_Color.jpg"), 256, CELL, 2.0), (54, 54, 58))
d = ImageDraw.Draw(rubber)
for y in range(0, CELL, 16):  # zigzag grooves across the tread
    d.line((10, y, 64, y + 8), fill=(18, 18, 20), width=3)
    d.line((64, y + 8, 118, y), fill=(18, 18, 20), width=3)
d.line((128, 30, 255, 30), fill=(78, 78, 82), width=2)
text_lines(d, (150, 56, 240, 72), (84, 84, 88), 2, 40)
put(rubber, 0, 4)

# ── Chair wood: bare, green paint worn through ──
wood = crop(src("Wood051_1K-JPG_Color.jpg"), CELL, CELL, 3.0)
put(wood, 3, 4)
paint = solid(CELL, CELL, (52, 104, 70), 6)
mask = Image.new("L", (CELL, CELL), 255)
md = ImageDraw.Draw(mask)
for _ in range(14):
    x, y = random.randint(0, 127), random.randint(0, 127)
    md.ellipse((x - 6, y - 3, x + 6, y + 3), fill=0)
put(Image.composite(paint, wood, mask.filter(ImageFilter.GaussianBlur(1.2))), 4, 4)

# ── Mattresses: quilted cover (192 px) over the ticking of the sides (64 px) ──
fabric = src("Fabric036_1K-JPG_Color.jpg")
for col, stains in ((0, 1), (4, 3)):
    region = Image.new("RGB", (512, 256))
    cover = tint(crop(fabric, 512, 192, 1.5), (232, 226, 206), strength=0.3)
    d = ImageDraw.Draw(cover, "RGBA")
    for k in range(-192, 512, 48):  # quilting
        d.line((k, 0, k + 192, 192), fill=(196, 188, 166, 255), width=2)
        d.line((k + 192, 0, k, 192), fill=(196, 188, 166, 255), width=2)
    for x in range(24, 512, 48):
        for y in range(24, 192, 48):
            d.ellipse((x - 2, y - 2, x + 2, y + 2), fill=(170, 160, 140, 255))
    rnd = random.Random(col)
    for _ in range(stains):
        x, y, r = rnd.randint(60, 450), rnd.randint(40, 150), rnd.randint(18, 34)
        d.ellipse((x - r, y - int(r * 0.7), x + r, y + int(r * 0.7)), fill=(150, 118, 70, 90))
        d.ellipse((x - r // 2, y - r // 3, x + r // 2, y + r // 3), fill=(130, 100, 60, 70))
    if stains > 1:
        grime = Image.new("RGB", cover.size, (170, 160, 140))
        cover = Image.blend(cover, grime, 0.18)
    region.paste(cover, (0, 0))
    ticking = Image.new("RGB", (512, 64), (236, 236, 232))
    td = ImageDraw.Draw(ticking)
    for x in range(0, 512, 12):
        td.rectangle((x, 0, x + 4, 63), fill=(70, 98, 160))
    region.paste(tint(ticking, (240, 238, 232), 0.85) if stains == 1 else ImageChops.multiply(ticking, Image.new("RGB", ticking.size, (214, 208, 196))), (0, 192))
    atlas.paste(region, (col * CELL, 5 * CELL))

atlas.save(OUTPUT, optimize=True)
print("wrote", os.path.normpath(OUTPUT))
