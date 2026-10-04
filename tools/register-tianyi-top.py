"""Read the original swing atlas and register head size, seat and head landmarks.

Only manifest metadata is written by the caller; source PNGs are never edited.
"""
from pathlib import Path
from copy import deepcopy
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
NAME = 'top-sequence-v5.png'
CANVAS = (256, 352)
HEAD_HEIGHT = 153
SEAT_Y = 242
BANDS = (0, 384, 760, 1135, 1536)
# Two tilted faces have slightly different skin boundaries after WPF bilinear
# resampling. Calibrated against the rendered top-registered-*.png measurements,
# while retaining fixed seat placement (no runtime scale animation).
RENDER_SCALE_CORRECTION = {12:153/150,13:153/156}


def groups(points):
    points = set(points)
    result = []
    while points:
        seed = points.pop(); todo = [seed]; group = [seed]
        while todo:
            x, y = todo.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                pt = x+dx, y+dy
                if pt in points:
                    points.remove(pt); todo.append(pt); group.append(pt)
        result.append(group)
    return result


def register_top(actions):
    image = Image.open(ROOT / NAME).convert('RGBA'); pixels = image.load()
    assert image.size == (1024, 1536)
    frames = []
    for index in range(16):
        col = index % 4 * 256
        start, end = BANDS[index//4:index//4+2]
        solid = {(x, y) for y in range(start,end) for x in range(col+8, col+248) if pixels[x,y][3]>=200}
        body = max(groups(solid),key=len)
        ymin,ymax = min(y for x,y in body),max(y for x,y in body)
        solid = {(x,y) for x,y in solid if ymin<=y<=ymax}
        x0,y0 = min(x for x,y in solid),min(y for x,y in solid)
        x1,y1 = max(x for x,y in solid)+1,max(y for x,y in solid)+1
        face = max(groups((x,y) for x,y in solid if (lambda p:p[0]>180 and p[0]>p[1]*1.04 and p[1]>p[2]*1.02)(pixels[x,y])),key=len)
        chin = max(y for x,y in face)
        scale = HEAD_HEIGHT/(chin-y0)*RENDER_SCALE_CORRECTION.get(index,1)
        # The jade forehead flower is visible during both open- and closed-eye poses.
        flower = max(groups((x,y) for x,y in solid if col+96<x<col+166 and y<y0+65
                            and (lambda p:p[1]>p[0]*1.08 and p[1]>p[2]*1.04)(pixels[x,y])),key=len)
        hx = sum(x for x,y in flower)/len(flower); hy = sum(y for x,y in flower)/len(flower)
        contact = [y for y in range(y0+225,y0+285)
                   if any((x,y) in solid for x in range(col+25,col+43))
                   and any((x,y) in solid for x in range(col+213,col+231))]
        seat = min(contact)+8
        ox = (CANVAS[0]-(x1-x0)*scale)/2
        oy = SEAT_Y-(seat-y0)*scale
        assert ox>=1 and oy>=1 and ox+(x1-x0)*scale<CANVAS[0]-1 and oy+(y1-y0)*scale<CANVAS[1]-1,(index,ox,oy,scale,x1-x0,y1-y0)
        frames.append({'image':NAME,'durationMs':40,
                       'region':{'x':x0,'y':y0,'width':x1-x0,'height':y1-y0},
                       'canvas':{'width':CANVAS[0],'height':CANVAS[1],'scale':scale,'offsetX':ox,'offsetY':oy},
                       'edgeAnchorY':0,'swingSeatAnchorY':SEAT_Y/CANVAS[1],
                       'headAnchorX':(ox+(hx-x0)*scale)/CANVAS[0],
                       'headAnchorY':(oy+(hy-y0)*scale)/CANVAS[1]})
    sequences = {
        'edge-top-peek':[0,2,3]+[4]*4+[3,2,0],
        'edge-top-look':[0,5,6]+[7]*5+[6,5,0,8,9]+[10]*5+[9,8,0],
        # The two closed-eye tilts point in opposite directions in the actual
        # sheet. Route each through the centered closed pose, never 14 <-> 15.
        'edge-top-smile':[0,1,0,11,12,13,12,11,0,2,3,4]+[14]*4
                         +[4,3,2,0,2,3,4]+[15]*4+[4,3,2,0],
    }
    actions['edge-top-idle']={'loop':True,'frames':[dict(deepcopy(frames[0]),durationMs=1000)]}
    for name, indices in sequences.items():
        actions[name]={'loop':False,'smoothFrames':True,'frames':[deepcopy(frames[i]) for i in indices]}
    return actions
