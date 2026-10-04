"""Measure actual WPF diagnostic renders and update scale metadata; never edit PNGs.

Run against a fresh verification directory generated from the current manifest.
Rebuild/register afterwards. Corrections are cumulative and kept with the source.
"""
import argparse
import json
import importlib.util
from pathlib import Path
from PIL import Image

spec=importlib.util.spec_from_file_location('registration',Path(__file__).with_name('register-tianyi-side.py'))
r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--verification-dir',type=Path,required=True)
args=parser.parse_args()
manifest=json.loads((r.ROOT/'character.json').read_text(encoding='utf-8-sig'))
frames={}
for action in ('edge-peek','edge-shy','edge-sway','edge-nod'):
    for frame in manifest['actions'][action]['frames']:
        frames.setdefault((frame['image'],tuple(frame['region'].values())),frame)
heights=[]
for i,frame in enumerate(frames.values()):
    image=Image.open(args.verification_dir/f'side-registered-{i:02}.png').convert('RGBA')
    px=image.load()
    solid={(x,y)for y in range(image.height)for x in range(image.width)if px[x,y][3]>=200}
    skin={(x,y)for x,y in solid if (lambda p:p[0]>180 and p[0]>p[1]*1.04 and p[1]>p[2]*1.02)(px[x,y])}
    face=max(r.groups(skin,diagonal=True),key=len)
    height=max(y for x,y in face)-min(y for x,y in solid)
    heights.append(height)
    region=frame['region'];index=region['y']//384*4+region['x']//256
    key=f"{frame['image']}:{index}"
    r.CORRECTIONS[key]=round(r.CORRECTIONS.get(key,1)*r.HEAD_HEIGHT/height,8)
(r.ROOT/'side-registration-v9.json').write_text(json.dumps(r.CORRECTIONS,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'measured':len(heights),'headMin':min(heights),'headMax':max(heights)}))
