#!/usr/bin/env python3
"""
Regenerates every piece of Pixel Kingdom Rumble's pixel art (all original) and its Unity import settings.
  python3 Tools/art/build_all.py            (from the repo root)

Writes:
  Art/Placeholder/Sprites/ph_*.png   2x redraws of every legacy sprite (same names, PPU 16 -> 32)
  Art/Pixel/Fighters/<id>/*.png      80x48 animation frames: idle4 run6 jump fall attack3 special2 hurt
  Art/Pixel/Enemies/<name>/*.png      enemy clips: idle, run (walk), jump/fall (air), hurt
  Art/Pixel/Bosses/warden_*.png      boss face states
  Art/Pixel/Tiles/<theme>_*.png      top / fill / stone / plank per world
  Art/Pixel/Backgrounds/<theme>_*.png far / mid / near / clouds parallax layers
  Art/Pixel/FX/*.png                 glow, shadow, projectile sprites
"""
import hashlib, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(ROOT, 'Assets', '_Project', 'Art')
PH = os.path.join(ART, 'Placeholder', 'Sprites')
PIX = os.path.join(ART, 'Pixel')

import props, tiles, bg, fx
import events  # registers the holiday / haunted tile and backdrop themes
import world4  # registers the astral (Starfall Observatory) theme
from chars import draw_fighter, anim_poses, crop_center
from roster import ROSTER, ORDER

ALIGN = {'center': 0, 'bottom': 7, 'bottomleft': 6}


def guid(rel):
    return hashlib.md5(("PKR:" + rel.replace(os.sep, "/")).encode()).hexdigest()


META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: 0
    wrapU: {wrap}
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: {align}
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: {b}, y: {b}, z: {b}, w: {b}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sid}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

written = []


def ensure_dir(path):
    if not os.path.isdir(path):
        os.makedirs(path)
    # folder metas up to ART
    p = path
    stop = os.path.join(ROOT, 'Assets', '_Project')
    while os.path.abspath(p) != os.path.abspath(stop) and p.startswith(stop):
        m = p + '.meta'
        if not os.path.exists(m):
            rel = os.path.relpath(p, ROOT)
            with open(m, 'w', newline='\n') as f:
                f.write(FOLDER_META.format(guid=guid(rel)))
        p = os.path.dirname(p)


def save(img, path, ppu=32, align='center', filt=0, wrap=1, border=0):
    ensure_dir(os.path.dirname(path))
    img.save(path)
    written.append(path)
    meta = path + '.meta'
    if os.path.exists(meta):
        s = open(meta).read()
        s = re.sub(r'spritePixelsToUnits: \d+', f'spritePixelsToUnits: {ppu}', s)
        s = re.sub(r'(textureSettings:\n    serializedVersion: 2\n    filterMode: )\d', rf'\g<1>{filt}', s)
        open(meta, 'w', newline='\n').write(s)
        return
    rel = os.path.relpath(path, ROOT)
    g = guid(rel)
    with open(meta, 'w', newline='\n') as f:
        f.write(META.format(guid=g, filter=filt, wrap=wrap, align=ALIGN[align], ppu=ppu, b=border, sid=hashlib.md5(g.encode()).hexdigest()))


ANIMS = ['idle', 'run', 'jump', 'fall', 'attack', 'special', 'hurt']


def build_fighters():
    for cid in ORDER:
        s = ROSTER[cid]
        poses = anim_poses(s['build'])
        for a in ANIMS:
            for i, p in enumerate(poses[a]):
                img = draw_fighter(s, p).image()
                save(img, os.path.join(PIX, 'Fighters', cid, f'{cid}_{a}_{i}.png'), align='bottom')
        idle = draw_fighter(s, poses['idle'][0]).image()
        save(crop_center(idle, 32), os.path.join(PH, s['file'] + '.png'), align='bottom')


def build_props():
    for name, fn in props.ALL.items():
        save(fn().image(), os.path.join(PH, name + '.png'))
    save(props.warden_body().image(), os.path.join(PH, 'ph_warden_body.png'))


ENEMY_CLIP_FILES = {'idle': ['idle'], 'walk': ['run'], 'air': ['jump', 'fall'], 'hurt': ['hurt']}


def build_enemies():
    for name, (fn, clips) in props.ENEMY_CLIPS.items():
        for kind, n in clips.items():
            for i in range(n):
                img = fn((kind, i, n)).image()
                for clip in ENEMY_CLIP_FILES[kind]:
                    save(img, os.path.join(PIX, 'Enemies', name, f'{name}_{clip}_{i}.png'), align='bottom')
    for i, img in enumerate(props.warden_frames()):
        save(img.image(), os.path.join(PIX, 'Bosses', f'warden_{props.WARDEN_STATES[i]}.png'))


def build_controls():
    import controls
    ui = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'UI')
    for name, fn in controls.ALL.items():
        save(fn(), os.path.join(ui, name + '.png'), ppu=100, filt=1)


FONT_META = """fileFormatVersion: 2
guid: {guid}
TrueTypeFontImporter:
  externalObjects: {{}}
  serializedVersion: 4
  fontSize: 16
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames:
  - PKR Pixel
  fallbackFontReferences: []
  customCharacters:
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def build_ui():
    import ui
    res = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'UI')
    for name, fn in ui.ALL.items():
        save(fn(), os.path.join(res, name + '.png'), ppu=100, border=ui.BORDERS[name])
    font = os.path.join(res, 'PKRPixel.ttf')
    ensure_dir(res)
    ui.build_font(font)
    written.append(font)
    if not os.path.exists(font + '.meta'):
        open(font + '.meta', 'w', newline='\n').write(FONT_META.format(guid=guid(os.path.relpath(font, ROOT))))


def build_obstacles():
    import obstacles
    for name, fn in obstacles.ALL.items():
        save(fn().image(), os.path.join(PIX, 'Obstacles', name + '.png'))


def build_events():
    for name, (fn, clips) in events.EVENT_ENEMIES.items():
        for kind, n in clips.items():
            for i in range(n):
                img = fn((kind, i, n)).image()
                for clip in ENEMY_CLIP_FILES[kind]:
                    save(img, os.path.join(PIX, 'Enemies', name, f'{name}_{clip}_{i}.png'), align='bottom')
    for name, fn in events.COLLECTIBLES.items():
        save(fn().image(), os.path.join(PIX, 'Events', f'collect_{name}.png'))
    for name, fn in events.ZOMBIE_PROPS.items():
        save(fn().image(), os.path.join(PIX, 'Events', f'{name}.png'), align='bottom')
    # Zombie Survival gun shots: near-white so each gun tints its own (loaded at runtime from Resources).
    shots = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'Shots')
    for kind in ('ball', 'shard', 'spark', 'blob', 'comet', 'lance', 'crescent'):
        save(fx.shot(kind, (236, 236, 236)).image(), os.path.join(shots, f'shot_{kind}.png'))


def build_cast():
    import cast
    for name, (fn, clips) in cast.FOE_CLIPS.items():
        for kind, n in clips.items():
            for i in range(n):
                img = fn((kind, i, n)).image()
                for clip in ENEMY_CLIP_FILES[kind]:
                    save(img, os.path.join(PIX, 'Enemies', name, f'{name}_{clip}_{i}.png'), align='bottom')
    for name, (fn, clips) in world4.WORLD4_ENEMIES.items():
        for kind, n in clips.items():
            for i in range(n):
                img = fn((kind, i, n)).image()
                for clip in ENEMY_CLIP_FILES[kind]:
                    save(img, os.path.join(PIX, 'Enemies', name, f'{name}_{clip}_{i}.png'), align='bottom')
    for nid in cast.NPCS:
        for i, c in enumerate(cast.npc_frames(nid)):
            save(c.image(), os.path.join(PIX, 'NPCs', nid, f'{nid}_idle_{i}.png'), align='bottom')


def build_tiles():
    for i, (theme, th) in enumerate(tiles.THEMES.items()):
        base = os.path.join(PIX, 'Tiles', theme)
        save(tiles.make_top(th, i * 31 + 3).image(), base + '_top.png', wrap=0)
        save(tiles.make_fill(th, i * 31 + 7).image(), base + '_fill.png', wrap=0)
        save(tiles.make_stone(th, i * 31 + 9).image(), base + '_stone.png', wrap=0)
        save(tiles.make_plank(th, i).image(), base + '_plank.png', wrap=0)


def build_backgrounds():
    for theme, fn in bg.THEMES.items():
        L = fn()
        for k, c in L.items():
            save(c.image(), os.path.join(PIX, 'Backgrounds', f'{theme}_{k}.png'), align='bottom', wrap=0)


def build_maps():
    import maps
    res = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'Maps')
    for name, fn in maps.ALL.items():
        save(fn().image(), os.path.join(res, name + '.png'), ppu=32, wrap=1)


def build_menu():
    import menubg
    from px import Canvas
    res = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'Menu')
    sky, clouds = menubg.build()
    save(sky, os.path.join(res, 'menu_bg.png'), wrap=0)
    save(clouds, os.path.join(res, 'menu_clouds.png'), wrap=0)


def build_story():
    import story_art
    res = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'Story')
    for key, fn in story_art.ALL.items():
        save(fn(), os.path.join(res, 'story_' + key + '.png'))


def build_fx():
    res = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR')
    save(fx.glow(), os.path.join(res, 'fx_glow.png'), ppu=32, filt=1)
    save(fx.shadow(), os.path.join(res, 'fx_shadow.png'), ppu=32, filt=1)
    for cid, spec in fx.SHOTS.items():
        save(fx.shot(*spec).image(), os.path.join(PIX, 'FX', f'shot_{cid}.png'))


if __name__ == '__main__':
    only = sys.argv[1:] or ['fighters', 'props', 'enemies', 'controls', 'ui', 'obstacles', 'events', 'cast', 'tiles', 'backgrounds', 'fx', 'maps', 'menu', 'story']
    for step in only:
        globals()['build_' + step]()
        print(step, 'done')
    print(len(written), 'images written')
