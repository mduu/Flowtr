"""Traces assets/logo/flowtr_logo_original.png into flat-colour SVGs.

Usage: python trace_logo.py <original.png> <out_dir>
Requires: pillow, numpy, vtracer. PNG/ICO favicons are rendered from favicon.svg.
"""
import sys, re
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
import vtracer

SRC, OUT = sys.argv[1], Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)
TMP = OUT / "_trace"
TMP.mkdir(exist_ok=True)
UP = 6
DARK, LIGHT = "#3F51B5", "#818CCF"

im = Image.open(SRC).convert("RGB")
big = im.resize((im.width * UP, im.height * UP), Image.LANCZOS)
a = np.asarray(big).astype(float)
white = np.array([255, 255, 255.]); dark = np.array([66, 80, 180.]); light = np.array([129, 140, 207.])

split = 238 * UP  # symbol left of this x, wordmark right
xs = np.arange(a.shape[1])[None, :]

# text: luminance threshold, right side only
lum = a @ np.array([0.299, 0.587, 0.114])
text = (lum < 128) & (xs >= split)
txt_px = a[text]
navy = np.median(txt_px[lum[text] < 60], axis=0)

def proj(c):
    v = white - c
    return ((white - a) @ v) / (v @ v)

# light bars: nearest of {white, light, dark}, then opening removes halos around dark shapes
d = np.stack([np.linalg.norm(a - c, axis=2) for c in (white, light, dark)])
lightm = (d.argmin(0) == 1) & (xs < split)
k = 4 * UP + 1
img = Image.fromarray((lightm * 255).astype(np.uint8))
img = img.filter(ImageFilter.MinFilter(k)).filter(ImageFilter.MaxFilter(k))
lightm = np.asarray(img) > 127
darkm = (proj(dark) > 0.5) & (xs < split) & ~np.asarray(Image.fromarray((lightm * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))).astype(bool)

def trace(mask, color, name):
    # mask -> black on white png -> vtracer binary
    Image.fromarray(np.where(mask, 0, 255).astype(np.uint8)).save(str(TMP / f"{name}.png"))
    vtracer.convert_image_to_svg_py(str(TMP / f"{name}.png"), str(TMP / f"{name}.svg"), colormode="binary", mode="spline",
                                    filter_speckle=40, corner_threshold=70, length_threshold=6.0,
                                    splice_threshold=45, path_precision=2)
    s = open(str(TMP / f"{name}.svg")).read()
    paths = re.findall(r"<path [^>]*/>", s)
    return "".join(p.replace('fill="#000000"', f'fill="{color}"') for p in paths)

navy_hex = "#%02X%02X%02X" % tuple(int(round(x)) for x in navy)
layers = {"light": trace(lightm, LIGHT, "m_light"), "dark": trace(darkm, DARK, "m_dark"),
          "text": trace(text, navy_hex, "m_text")}

def svg(box, body, pad, square=False, bg=None):
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    if square:
        side = max(w, h)
        x0, y0, w, h = x0 - (side - w) / 2, y0 - (side - h) / 2, side, side
    x0, y0, w, h = x0 - pad, y0 - pad, w + 2 * pad, h + 2 * pad
    rect = f'<rect x="{x0:.0f}" y="{y0:.0f}" width="{w:.0f}" height="{h:.0f}" fill="{bg}"/>' if bg else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{x0:.0f} {y0:.0f} {w:.0f} {h:.0f}" '
            f'width="{w / UP:.0f}" height="{h / UP:.0f}" role="img" aria-label="Flowtr"><title>Flowtr</title>'
            f'{rect}{body}</svg>\n')

def bbox(m):
    ys, xs_ = np.nonzero(m)
    return [int(xs_.min()), int(ys.min()), int(xs_.max()) + 1, int(ys.max()) + 1]

symbol = layers["light"] + layers["dark"]
white_symbol = (layers["light"].replace(f'fill="{LIGHT}"', 'fill="#FFFFFF" fill-opacity="0.6"')
                + layers["dark"].replace(f'fill="{DARK}"', 'fill="#FFFFFF"'))
full, sym = bbox(lightm | darkm | text), bbox(lightm | darkm)

(OUT / "flowtr_logo.svg").write_text(svg(full, symbol + layers["text"], 4 * UP))
(OUT / "flowtr_logo_white.svg").write_text(
    svg(full, white_symbol + layers["text"].replace(f'fill="{navy_hex}"', 'fill="#FFFFFF"'), 4 * UP))
(OUT / "flowtr_icon.svg").write_text(svg(sym, symbol, 2 * UP))
(OUT / "favicon.svg").write_text(svg(sym, symbol, 2 * UP, square=True))
(OUT / "app-icon.svg").write_text(svg(sym, symbol, 28 * UP, square=True, bg="#FFFFFF"))
print("navy", navy_hex)
