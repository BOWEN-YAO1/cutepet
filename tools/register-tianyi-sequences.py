"""Register unmodified AI atlases in the manifest; reads pixels, never edits images.

Pillow is only needed to reproduce the contact-point measurements. Runtime and
packaging use the saved manifest and have no Python/Pillow dependency.
"""
from pathlib import Path
from collections import Counter
import json
import argparse
import importlib.util
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
ATLASES = {
    'standing': ('standing-sequence-v4.png', [40, 417, 793, 1145]),
    'throne': ('throne-sequence-v4.png', [56, 410, 755, 1109]),
    'cloud': ('cloud-sequence-v5.png', [49, 416, 796, 1158]),
    'side': ('edge-sequence-v5.png', [47, 429, 804, 1169]),
    'bottom': ('bottom-sequence-v6.png', [65, 422, 776, 1119]),
    'top': ('top-sequence-v4.png', [70, 434, 808, 1161]),
}
images = {key: Image.open(ROOT/name).convert('RGBA') for key, (name, _) in ATLASES.items()}
anchors = {}
for key in ('side', 'bottom', 'top'):
    pixels = images[key].load()
    for index in range(16):
        ox, oy = index % 4 * 256, ATLASES[key][1][index // 4]
        def solid(x, y): return pixels[ox+x, oy+y][3] >= 200
        if key == 'side':
            starts = [next((x for x in range(20, 120) if solid(x, y)), 119) for y in range(80, 320)]
            grip = Counter(x for x in starts if x < 110).most_common(1)[0][0]
            skin = [y for y in range(180, 300) for x in range(max(0, grip-13), grip+6)
                    if solid(x, y) and (lambda c: c[0] > c[1]*1.1 and c[1] > c[2]*1.05)(pixels[ox+x, oy+y])]
            anchors[key, index] = {'edgeAnchorX': grip/256,
                                   'edgeAnchorY': (sum(skin)/len(skin) if skin else 240)/352}
        elif key == 'bottom':
            # Pick the last row actually contacting BOTH sleeves, excluding antialiasing.
            contact = [y for y in range(270, 346)
                       if any(solid(x, y) for x in range(46, 115))
                       and any(solid(x, y) for x in range(141, 210))]
            anchors[key, index] = {'edgeAnchorY': (max(contact)-1)/352}
        else:
            # Actual jade ring pixels at both endpoints; vertical registration shares
            # the same suspension-to-seat distance across all sixteen poses.
            contact = [y for y in range(200, 282)
                       if any(solid(x, y) for x in range(27, 45))
                       and any(solid(x, y) for x in range(211, 229))]
            seat = min(contact, key=lambda y: abs(y-242))
            anchors[key, index] = {'swingSeatAnchorY': seat/352,
                                   'edgeAnchorY': seat/352 - 242/352}

def frame(key, index, duration=80):
    name, rows = ATLASES[key]
    edge = key in ('side','bottom','top')
    x = index % 4 * 256 + (0 if edge else 16 if key=='cloud' else 8)
    result = {'image': name, 'durationMs': duration,
            'region': {'x': x, 'y': rows[index // 4], 'width': 256 if edge else 240, 'height': 352 if edge else 360},
            **anchors.get((key, index), {})}
    if key=='cloud':
        region=result['region']
        bounds=images[key].getchannel('A').crop((x,region['y'],x+240,region['y']+360)).point(lambda a:255 if a>=200 else 0).getbbox()
        scale=301/(bounds[3]-bounds[1])
        result['canvas']={'width':240,'height':360,'scale':scale,
                          'offsetX':120*(1-scale),'offsetY':min(344-bounds[3]*scale,360*(1-scale))}
    return result

def clip(key, stages, loop=False):
    frames=[]
    for item in stages:
        index, duration = item if isinstance(item, tuple) else (item, 80)
        frames.append(frame(key, index, duration))
    return {'loop': loop, 'frames': frames}

manifest = json.loads((ROOT/'character.json').read_text(encoding='utf-8-sig'))
actions = {
    'idle': clip('standing', [(0, 800), (3, 800)], True),
    'blink': clip('standing', [(1, 40), (2, 80), (1, 40), (0, 40)]),
    'greeting': clip('standing', [4, 5, 6, (7, 160), 6, 7, 6, 5, 4, 0]),
    'low': clip('cloud', [(0, 800), (1, 100), (2, 100), (3, 100), (0, 1000)], True),
    'look': clip('standing', [0, 12, (13, 280), 12, 0, 14, (15, 280), 14, 0]),
    'happy': clip('standing', [8, (9, 120), (10, 160), (11, 120), 9, 8, (0, 160)]),
    'conjure': {'loop': False, 'frames': [frame('standing', 0, 100)] +
                [frame('throne', i, ms) for i, ms in [(0,200),(1,300),(2,100),(3,100),(4,100),(5,100),(6,100),(7,100),(8,300)]]},
    'sit': clip('throne', [(8, 1000)], True),
    'summon-cloud': {'loop': False, 'frames': [frame('standing',0,80)] +
                     [frame('cloud',i,ms) for i,ms in [(12,100),(13,140),(14,140),(15,120)]] + [frame('standing',0,120)]},
    'cloud-idle': clip('cloud', [(i,180) for i in [4,5,6,7,6,5,4]], True),
    'cloud-blink': clip('cloud', [(8,40),(9,40),(10,80),(11,40),(8,40)]),
    'sit-blink': clip('throne', [(9,60),(10,80),(9,60),(8,100)]),
    'sit-greeting': clip('throne', [8,12,13,14,(15,160),14,15,14,13,12,8]),
    'sit-happy': clip('throne', [(8,100),(9,100),(10,250),(11,200),(8,150)]),
    'edge-idle': clip('side', [(0,1000)], True),
    'edge-peek': clip('side', list(range(8))+list(range(6,-1,-1))),
    'edge-shy': clip('side', [0,1,2,3,4,8,9,10,9,8,4,3,2,1,0]),
    'edge-nod': clip('side', [0,1,2,3,4,8,9,10,11,8,4,3,2,1,0]),
    'edge-sway': clip('side', [0,1,2,3,4,12,13,14,15,14,13,12,4,3,2,1,0]),
    'edge-bottom-idle': clip('bottom', [(0,1000)], True),
    'edge-bottom-peek': clip('bottom', [0,1,2,3,4,5,6,7,4,3,2,1,0]),
    'edge-bottom-look': clip('bottom', [0,1,2,8,9,8,2,10,11,10,2,1,0]),
    'edge-bottom-smile': clip('bottom', [0,4,12,13,12,14,15,14,12,4,0]),
    'edge-top-idle': clip('top', [(i,800) for i in [0,1,2,3]], True),
    'edge-top-peek': clip('top', [0,1,2,3,4,5,6,7,6,5,4,3,2,1,0]),
    'edge-top-look': clip('top', [0,1,8,9,8,1,2,10,11,10,2,1,0]),
    'edge-top-smile': clip('top', [0,1,12,13,14,15,14,13,12,1,0]),
}
actions['stand'] = {'loop': False, 'frames': [dict(f, durationMs=100) for f in reversed(actions['conjure']['frames'])]}
spec=importlib.util.spec_from_file_location('inbetweens',Path(__file__).with_name('register-tianyi-inbetweens.py'))
inbetweens=importlib.util.module_from_spec(spec);spec.loader.exec_module(inbetweens)
actions=inbetweens.insert_inbetweens(actions,frame)
spec=importlib.util.spec_from_file_location('side_registration',Path(__file__).with_name('register-tianyi-side.py'))
side_registration=importlib.util.module_from_spec(spec);spec.loader.exec_module(side_registration)
actions=side_registration.register_side(actions)
spec=importlib.util.spec_from_file_location('top_registration',Path(__file__).with_name('register-tianyi-top.py'))
top_registration=importlib.util.module_from_spec(spec);spec.loader.exec_module(top_registration)
actions=top_registration.register_top(actions)
manifest['actions'] = actions
manifest['edgeAnchorX'] = actions['edge-idle']['frames'][0]['edgeAnchorX']
manifest['edgeTopAnchorY'] = actions['edge-top-idle']['frames'][0]['edgeAnchorY']
manifest['edgeBottomAnchorY'] = anchors['bottom',0]['edgeAnchorY']
manifest['topSwing']['seatAnchorY'] = actions['edge-top-idle']['frames'][0]['swingSeatAnchorY']
manifest['topSwing']['seatHalfWidth'] = .365
content = json.dumps(manifest, ensure_ascii=False, indent=2)+'\n'
assert len(content.encode()) <= 512*1024
assert sum(len(c['frames']) for c in actions.values()) <= 768
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check', action='store_true', help='Check saved metadata without writing files')
if parser.parse_args().check:
    assert json.loads(content) == json.loads((ROOT/'character.json').read_text(encoding='utf-8-sig'))
else:
    (ROOT/'character.json').write_text(content, encoding='utf-8')
print(json.dumps({'actions':len(actions), 'references':sum(len(c['frames']) for c in actions.values()),
                  'uniquePoses':len({(f['image'],tuple(f['region'].values())) for c in actions.values() for f in c['frames']}),
                  'manifestBytes':len(content.encode())}))
