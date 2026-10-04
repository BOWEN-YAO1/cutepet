"""Read the four dense swing atlases and register head size, seat and landmarks.

Only manifest metadata is written by the caller; source PNGs are never edited.
"""
from pathlib import Path
from copy import deepcopy
import math
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
ATLASES = {'blink':'top-blink-v6.png','left':'top-left-v6.png',
           'right':'top-right-v6.png','smile':'top-smile-v6.png'}
CANVAS = (256, 352)
BANDS = (0, 384, 768, 1152, 1536)
REGISTRATION = {}
# WPF resampling joins narrow jaw/neck skin at these three source regions.
# Calibrate against the measured rendered head, not only source thresholds.
RENDERED_HEAD_CORRECTION = {('left',4):153/156,('left',6):153/158,('left',7):153/158}
# The generator's row order has small reversals in eyelid openness. Use actual
# registered iris measurements to put all sixteen drawings in closing order.
BLINK_ORDER = (0,2,1,3,4,5,9,7,8,6,10,11,13,14,12,15)


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


def measure_atlas(name):
    image = Image.open(ROOT/name).convert('RGBA'); pixels = image.load()
    assert image.size == (1024,1536)
    result = []
    for index in range(16):
        col=index%4*256;start,end=BANDS[index//4:index//4+2]
        solid={(x,y) for y in range(start,end) for x in range(col+8,col+248) if pixels[x,y][3]>=200}
        body=max(groups(solid),key=len);ymin,ymax=min(y for x,y in body),max(y for x,y in body)
        solid={(x,y) for x,y in solid if ymin<=y<=ymax}
        x0,y0=min(x for x,y in solid),min(y for x,y in solid)
        x1,y1=max(x for x,y in solid)+1,max(y for x,y in solid)+1
        face=max(groups((x,y) for x,y in solid if y<y0+210
                        and (lambda p:p[0]>180 and p[0]>p[1]*1.04 and p[1]>p[2]*1.02)(pixels[x,y])),key=len)
        chin=max(y for x,y in face)
        flower=max(groups((x,y) for x,y in solid if col+90<x<col+175 and y<y0+70
                          and (lambda p:p[1]>p[0]*1.08 and p[1]>p[2]*1.04)(pixels[x,y])),key=len)
        hx=sum(x for x,y in flower)/len(flower);hy=sum(y for x,y in flower)/len(flower)
        def ring(x,y):
            p=pixels[x,y]
            return p[3]>=200 and (p[0]>100 and p[1]>100 and p[2]<p[1]*.8
                                 or p[1]>p[0]*1.08 and p[1]>p[2]*1.04)
        contact=[y for y in range(y0+210,min(y0+290,end))
                 if any(ring(x,y) for x in range(col+22,col+48))
                 and any(ring(x,y) for x in range(col+208,col+234))]
        seat=min(contact)+6
        result.append((name,index,x0,y0,x1,y1,chin-y0,seat-y0,hx,hy))
    return result


def register_top(actions):
    measured={key:measure_atlas(name) for key,name in ATLASES.items()}
    all_frames=[m for frames in measured.values() for m in frames]
    up=max(m[7]/m[6] for m in all_frames)
    down=max((m[5]-m[3]-m[7])/m[6] for m in all_frames)
    wide=max((m[4]-m[2])/m[6] for m in all_frames)
    # One shared scale target and seat for all 64 drawings; fit complete shoes
    # and rings without cropping or changing targets between the four clips.
    head=min(153,math.floor((CANVAS[1]-6)/(up+down)),math.floor((CANVAS[0]-4)/wide))
    seat=round((up*head+3+CANVAS[1]-3-down*head)/2,3)
    REGISTRATION.update(headHeight=head,seatY=seat)
    registered={}
    for key,items in measured.items():
        registered[key]=[]
        for name,index,x0,y0,x1,y1,hh,drop,hx,hy in items:
            scale=head/hh*RENDERED_HEAD_CORRECTION.get((key,index),1)
            ox=(CANVAS[0]-(x1-x0)*scale)/2;oy=seat-drop*scale
            assert ox>=1 and oy>=1 and ox+(x1-x0)*scale<CANVAS[0]-1 and oy+(y1-y0)*scale<CANVAS[1]-1,(key,index,ox,oy,scale)
            registered[key].append({'image':name,'durationMs':40,
                'region':{'x':x0,'y':y0,'width':x1-x0,'height':y1-y0},
                'canvas':{'width':CANVAS[0],'height':CANVAS[1],'scale':round(scale,6),'offsetX':round(ox,3),'offsetY':round(oy,3)},
                'edgeAnchorY':0,'swingSeatAnchorY':round(seat/CANVAS[1],6),
                'headAnchorX':round((ox+(hx-x0)*scale)/CANVAS[0],6),'headAnchorY':round((oy+(hy-y0)*scale)/CANVAS[1],6)})
    registered['blink']=[registered['blink'][i] for i in BLINK_ORDER]
    base=registered['blink'][0]
    def trip(key):
        frames=registered[key]
        return frames+list(reversed(frames[:-1]))
    sequences={'edge-top-peek':trip('blink'),
               'edge-top-look':[base]+trip('left')+[base]+trip('right')+[base],
               'edge-top-smile':[base]+trip('smile')+[base]}
    actions['edge-top-idle']={'loop':True,'frames':[dict(deepcopy(base),durationMs=1000)]}
    for name,frames in sequences.items():
        actions[name]={'loop':False,'smoothFrames':True,'frames':deepcopy(frames)}
    return actions
