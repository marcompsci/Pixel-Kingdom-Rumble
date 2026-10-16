#!/usr/bin/env python3
"""
Cuts the zombie App Store preview / trailer from Tools/sim_trailer.command footage (Logs/trailer/seg_*.mov).

  python3 Tools/make_trailer.py <trailer folder> <out folder>

Writes:
  zombies_preview_1920x886.mp4  App Store app preview (landscape, 6.9"/6.7" size, 30 fps, H.264, AAC stereo, < 30 s)
  zombies_trailer_1080p.mp4     the same cut at 1920x1080 (letterboxed) for social posts
  screenshots/                   the gameplay shots turned to landscape 2868x1320, the menu shot as portrait 1320x2868
Music: Assets/_Project/Resources/Audio/music_survival.wav (the game's own synthesized track).
Captions use the game's pixel font (PKRPixel.ttf). Needs ffmpeg and Pillow.
"""
import os, subprocess, sys, glob, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'PKR', 'UI', 'PKRPixel.ttf')
MUSIC = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Audio', 'music_survival.wav')
W, H, FPS = 1920, 886, 30

# (segment name part, start second in the clip, length, caption)
# (App Store previews should be gameplay, so the portrait menu clip is left out; its screenshot is still made.)
CUTS = [
    ('LastStand_demo', 3.0, 7.5, 'LAST STAND  -  AI SQUAD OR 4-PLAYER CO-OP'),
    ('LastStand_pierdemo', 3.0, 7.5, 'NEW MAP: SALT PIER  -  NEW WEEKLY CHALLENGE'),
    ('ZombieLand_isle', 2.0, 6.5, 'ZOMBIE LAND  -  CHAPTER 2: BLACKWATER'),
]
TITLE, END = 2.5, 3.0


def run(*args):
    subprocess.run(list(args), check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def card(path, lines, sizes, colors):
    im = Image.new('RGB', (W, H), (16, 12, 24))
    d = ImageDraw.Draw(im)
    total = sum(s + 30 for s in sizes) - 30
    y = (H - total) // 2
    for text, size, col in zip(lines, sizes, colors):
        f = ImageFont.truetype(FONT, size)
        w = d.textlength(text, font=f)
        d.text(((W - w) // 2 + 6, y + 6), text, font=f, fill=(0, 0, 0))
        d.text(((W - w) // 2, y), text, font=f, fill=col)
        y += size + 30
    im.save(path)


def caption(path, text):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(FONT, 46)
    w = d.textlength(text, font=f)
    x, y = (W - w) // 2, H - 120
    d.rectangle([x - 28, y - 18, x + w + 28, y + 64], fill=(0, 0, 0, 170))
    d.text((x, y), text, font=f, fill=(255, 214, 90, 255))
    im.save(path)


def main():
    src, out = sys.argv[1], sys.argv[2]
    tmp = os.path.join(out, '_tmp')
    os.makedirs(tmp, exist_ok=True)
    parts = []
    card(os.path.join(tmp, 'title.png'), ['PIXEL KINGDOM RUMBLE', 'ZOMBIES'], [64, 150], [(255, 255, 255), (255, 80, 60)])
    card(os.path.join(tmp, 'end.png'), ['ZOMBIES, ALL YEAR', 'PIXEL KINGDOM RUMBLE'], [96, 54], [(255, 80, 60), (255, 214, 90)])
    for name, dur in (('title', TITLE), ('end', END)):
        p = os.path.join(tmp, name + '.mp4')
        run('ffmpeg', '-y', '-loop', '1', '-i', os.path.join(tmp, name + '.png'), '-t', str(dur), '-r', str(FPS),
            '-vf', 'fade=in:0:8,fade=out:st=%s:d=0.3' % (dur - 0.3), '-pix_fmt', 'yuv420p', '-c:v', 'libx264', p)
        if name == 'title':
            parts.append(p)
    end_clip = os.path.join(tmp, 'end.mp4')
    for i, (key, start, dur, text) in enumerate(CUTS):
        seg = glob.glob(os.path.join(src, 'seg_*%s*.mov' % key))
        if not seg:
            print('missing segment', key)
            continue
        cap = os.path.join(tmp, 'cap%d.png' % i)
        caption(cap, text)
        p = os.path.join(tmp, 'part%d.mp4' % i)
        portrait = 'MainMenu' in key
        if portrait:
            # the menu is portrait: show it centred on a dark backdrop
            vf = ('[0:v]scale=-2:%d,pad=%d:%d:(ow-iw)/2:0:color=0x100c18,fps=%d[v];[v][1:v]overlay=0:0,'
                  'fade=in:0:6,fade=out:st=%s:d=0.25[o]' % (H, W, H, FPS, dur - 0.25))
        else:
            # simulator recordings come out portrait-rotated: turn them to landscape, fill the frame
            vf = ('[0:v]transpose=2,scale=%d:%d:force_original_aspect_ratio=increase,crop=%d:%d,fps=%d[v];'
                  '[v][1:v]overlay=0:0,fade=in:0:6,fade=out:st=%s:d=0.25[o]' % (W, H, W, H, FPS, dur - 0.25))
        run('ffmpeg', '-y', '-ss', str(start), '-t', str(dur), '-i', seg[0], '-loop', '1', '-t', str(dur), '-i', cap,
            '-filter_complex', vf, '-map', '[o]', '-an', '-pix_fmt', 'yuv420p', '-c:v', 'libx264', '-crf', '18', p)
        parts.append(p)
    parts.append(end_clip)
    lst = os.path.join(tmp, 'list.txt')
    with open(lst, 'w') as f:
        for p in parts:
            f.write("file '%s'\n" % p)
    silent = os.path.join(tmp, 'video.mp4')
    run('ffmpeg', '-y', '-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', silent)
    dur = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', silent]).decode().strip())
    preview = os.path.join(out, 'zombies_preview_1920x886.mp4')
    run('ffmpeg', '-y', '-i', silent, '-stream_loop', '-1', '-i', MUSIC, '-t', '%.2f' % dur,
        '-af', 'afade=in:0:d=0.5,afade=out:st=%.2f:d=1.5,aformat=channel_layouts=stereo,aresample=44100' % (dur - 1.5),
        '-map', '0:v', '-map', '1:a', '-c:v', 'libx264', '-profile:v', 'high', '-level', '4.0', '-pix_fmt', 'yuv420p',
        '-r', str(FPS), '-b:v', '10M', '-c:a', 'aac', '-b:a', '256k', '-movflags', '+faststart', preview)
    run('ffmpeg', '-y', '-i', preview, '-vf', 'scale=1920:-2,pad=1920:1080:0:(oh-ih)/2:color=0x100c18', '-c:v', 'libx264',
        '-crf', '18', '-pix_fmt', 'yuv420p', '-c:a', 'copy', '-movflags', '+faststart', os.path.join(out, 'zombies_trailer_1080p.mp4'))
    # screenshots
    shots = os.path.join(out, 'screenshots')
    os.makedirs(shots, exist_ok=True)
    order = [('pierdemo_b', '1_last_stand_salt_pier'), ('LastStand_demo_b', '2_last_stand_hollow_block'),
             ('isle_a', '3_zombie_land_chapter2'), ('MainMenu_zombies_a', '4_zombies_menu')]
    for key, name in order:
        f = glob.glob(os.path.join(src, 'shot_*%s.png' % key))
        if not f:
            continue
        im = Image.open(f[0])
        if 'MainMenu' not in key:
            im = im.rotate(90, expand=True)  # landscape 2868x1320
        im.convert('RGB').save(os.path.join(shots, name + '.png'))
    shutil.rmtree(tmp)
    print('preview %.1f s ->' % dur, preview)


if __name__ == '__main__':
    main()
