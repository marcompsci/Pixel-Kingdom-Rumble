"""
October 2026 cast additions in the chibi style (all original): five town folk who stand around the levels and chat,
and three new foes drawn with the same rig as the heroes.
"""
from chars import C, P, GOLD, METAL, anim_poses
from props import rig_crop, st

NONE = dict(type='none')


def _spec(**kw):
    s = dict(build='normal', head='hair', extra=None, skin=C(240, 200, 170), hair=C(80, 50, 40), top=C(200, 200, 200),
             accent=C(200, 80, 80), pants=C(70, 70, 90), shoes=C(60, 50, 50), belt=C(70, 60, 50), weapon=NONE)
    s.update(kw)
    return s


# ---- town folk ---------------------------------------------------------------------------------------------------
NPCS = {
    'mayor_opal': dict(name='Mayor Opal', spec=_spec(build='normal', head='tophat', skin=C(150, 100, 70), hair=C(110, 100, 120),
                                                     top=C(120, 60, 140), accent=C(50, 40, 60), accent2=C(255, 200, 80),
                                                     pants=C(50, 40, 60), emblem=GOLD, extra='collar')),
    'rosie_crumb': dict(name='Rosie Crumb', spec=_spec(build='bulky', head='bob', skin=C(250, 210, 180), hair=C(200, 90, 70),
                                                       top=C(255, 190, 200), pants=C(250, 250, 245), overalls=True,
                                                       accent=C(255, 140, 160), shoes=C(140, 90, 70), belt=C(255, 140, 160))),
    'fitz_postwell': dict(name='Fitz Postwell', spec=_spec(build='slim', head='cap', skin=C(196, 140, 104), hair=C(40, 30, 30),
                                                           top=C(60, 110, 190), accent=C(255, 210, 80), accent2=C(40, 70, 140),
                                                           pants=C(40, 50, 90), extra='satchel', emblem=C(255, 210, 80))),
    'lila_sprout': dict(name='Lila Sprout', spec=_spec(build='slim', head='ponytail', skin=C(240, 196, 160), hair=C(90, 160, 90),
                                                       top=C(250, 240, 200), accent=C(255, 120, 150), pants=C(90, 150, 90),
                                                       overalls=True, shoes=C(250, 200, 80), eye=C(80, 60, 50))),
    'benji_bolts': dict(name='Benji Bolts', spec=_spec(build='small', head='curls', skin=C(120, 80, 56), hair=C(30, 24, 24),
                                                       top=C(250, 140, 60), accent=C(60, 200, 220), pants=C(60, 80, 130),
                                                       shoes=C(240, 240, 240), glasses=C(60, 200, 220), emblem=C(60, 200, 220))),
}


def npc_frames(npc_id):
    """Four idle frames (breathing, a wave on frame 2, a blink on frame 3), 32x40, feet at the bottom."""
    spec = NPCS[npc_id]['spec']
    out = []
    for i in range(4):
        hands = ((-2, -7), (3, -7)) if i != 2 else ((-2, -7), (5, 4))  # frame 2: a friendly wave
        pose = P(by=-(1 if i in (1, 2) else 0), hands=hands, wang=0, flutter=i, eyes='blink' if i == 3 else 'open')
        out.append(rig_crop(spec, pose, 32, 40))
    return out


# ---- new foes ----------------------------------------------------------------------------------------------------
FOES = {
    # Tickworks maintenance crew turned troublemakers: hard hat, overalls, a big wrench.
    'gear_goon': (_spec(build='bulky', head='helmet', helm=C(250, 200, 60), skin=C(230, 180, 140), hair=C(60, 40, 30),
                        top=C(240, 120, 50), accent=C(60, 60, 70), pants=C(70, 90, 130), overalls=True, shoes=C(60, 50, 40),
                        eye=C(60, 40, 30),
                        weapon=dict(type='wrench', color=C(170, 176, 190), accent=C(240, 120, 50), fx=C(255, 220, 140))), 44),
    # A masked mischief-maker on spring shoes.
    'prank_bandit': (_spec(build='small', head='bandana', skin=C(200, 150, 110), hair=C(40, 30, 30), top=C(250, 250, 250),
                           accent=C(60, 60, 80), pants=C(60, 60, 80), shoes=C(255, 90, 90), emblem=C(60, 60, 80), skates=False,
                           eye=C(40, 30, 30)), 40),
    # A stage magician who throws cards.
    'hex_hatter': (_spec(build='slim', head='tophat', skin=C(240, 210, 200), hair=C(150, 60, 180), top=C(80, 50, 110),
                         accent=C(60, 40, 80), accent2=C(255, 220, 100), pants=C(50, 40, 70), shoes=C(30, 30, 40),
                         extra='cape', eye=C(150, 60, 180)), 46),
}


def foe(name, a=None):
    spec, h = FOES[name]
    kind, ph, i = st(a)
    poses = anim_poses(spec['build'])
    if kind == 'walk':
        pose = poses['run'][i % 6]
    elif kind == 'air':
        pose = poses['jump'][0] if i % 2 == 0 else poses['fall'][0]
    elif kind == 'hurt':
        pose = poses['hurt'][0]
    else:
        pose = poses['idle'][i % 4]
    pose = dict(pose, eyes='angry' if kind not in ('hurt',) and pose['eyes'] == 'open' else pose['eyes'])
    return rig_crop(spec, pose, 32, h)


FOE_CLIPS = {
    'gear_goon': (lambda a=None: foe('gear_goon', a), dict(idle=2, walk=6, hurt=1)),
    'prank_bandit': (lambda a=None: foe('prank_bandit', a), dict(idle=2, air=2, hurt=1)),
    'hex_hatter': (lambda a=None: foe('hex_hatter', a), dict(idle=2, walk=6, hurt=1)),
}
