#!/usr/bin/env python3
"""Draws the zombie achievement badges for App Store Connect (1024x1024 PNG, original art, the game's pixel font).
   python3 Tools/art/achievement_badges.py Docs/AppStore/achievements"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'UI', 'PKRPixel.ttf')
S = 64  # drawn at 64x64 pixels, scaled up 16x with nearest neighbour

BADGES = [
    ('z_round10', (170, 70, 40), '10'),
    ('z_round20', (150, 30, 40), '20'),
    ('z_hollow_king', (110, 80, 170), 'crown'),
    ('z_tide_mother', (40, 110, 150), 'wave'),
    ('z_iron_widow', (120, 80, 50), 'wheel'),
    ('z_chapter1', (90, 60, 140), 'I'),
    ('z_chapter2', (40, 120, 110), 'II'),
    ('z_chapter3', (70, 120, 50), 'III'),
    ('z_weekly', (130, 100, 30), 'W'),
    ('z_streak7', (200, 90, 40), '7'),
    ('z_medic', (60, 130, 80), 'cross'),
]


def badge(kind, col):
    im = Image.new('RGB', (S, S), (16, 12, 24))
    d = ImageDraw.Draw(im)
    dark = tuple(max(0, c - 60) for c in col)
    light = tuple(min(255, c + 70) for c in col)
    # a shield
    pts = [(10, 8), (54, 8), (54, 34), (32, 58), (10, 34)]
    d.polygon(pts, fill=dark)
    d.polygon([(13, 11), (51, 11), (51, 33), (32, 54), (13, 33)], fill=col)
    d.line([(13, 11), (51, 11)], fill=light)
    gold, white = (255, 214, 90), (250, 246, 236)
    if kind == 'crown':
        d.polygon([(20, 36), (20, 22), (26, 29), (32, 18), (38, 29), (44, 22), (44, 36)], fill=gold)
        d.rectangle([20, 36, 44, 39], fill=gold)
    elif kind == 'wave':
        for y in (24, 31, 38):
            for x in range(18, 46):
                yy = y + (2 if (x // 4) % 2 else 0)
                d.point((x, yy), fill=white); d.point((x, yy + 1), fill=white)
    elif kind == 'wheel':
        d.ellipse([20, 18, 44, 42], outline=gold, width=3)
        d.ellipse([29, 27, 35, 33], fill=gold)
        for a, b in (((32, 20), (32, 40)), ((22, 30), (42, 30)), ((25, 23), (39, 37)), ((39, 23), (25, 37))):
            d.line([a, b], fill=gold)
    elif kind == 'cross':
        d.rectangle([28, 18, 36, 42], fill=white)
        d.rectangle([20, 26, 44, 34], fill=white)
    else:
        f = ImageFont.truetype(FONT, 24 if len(kind) <= 2 else 18)
        w = d.textlength(kind, font=f)
        d.text(((S - w) / 2 + 1, 18), kind, font=f, fill=dark)
        d.text(((S - w) / 2, 17), kind, font=f, fill=gold)
    return im.resize((1024, 1024), Image.NEAREST)


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    for name, col, kind in BADGES:
        badge(kind, col).save(os.path.join(out, name + '.png'))
    print(len(BADGES), 'badges ->', out)
