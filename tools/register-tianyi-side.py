"""Measure unmodified dense side atlases; write only registration metadata."""
from pathlib import Path
from collections import Counter
from copy import deepcopy
from PIL import Image
import json

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
ATLASES = {'reveal-a':'side-reveal-a-v7.png', 'reveal-b':'side-reveal-b-v8.png',
           'nod':'side-nod-v7.png', 'sway':'side-sway-v7.png'}
ATLASES.update({f'transition-{i}':f'side-transition-{i}-v9.png' for i in range(1,7)})
ATLASES['transition-deep']='side-transition-deep-v9.png'
HEAD_HEIGHT = 198
CANVAS = (288,384)
GRIP = (48,260)
REGISTRATION = {}
CORRECTIONS = json.loads((ROOT/'side-registration-v9.json').read_text(encoding='utf-8')) if (ROOT/'side-registration-v9.json').exists() else {}

def groups(points,diagonal=False):
    points=set(points);result=[]
    while points:
        seed=points.pop();todo=[seed];group=[seed]
        while todo:
            x,y=todo.pop()
            neighbors=((1,0),(-1,0),(0,1),(0,-1)) if not diagonal else ((dx,dy) for dx in range(-1,2) for dy in range(-1,2) if dx or dy)
            for dx,dy in neighbors:
                pt=x+dx,y+dy
                if pt in points:
                    points.remove(pt);todo.append(pt);group.append(pt)
        result.append(group)
    return result

def measure(key):
    name=ATLASES[key];image=Image.open(ROOT/name).convert('RGBA');pixels=image.load()
    assert image.size==(1024,1536)
    result=[]
    for index in range(16):
        col,row=index%4*256,index//4*384
        solid={(x,y) for y in range(row,row+384) for x in range(col+20,col+250) if pixels[x,y][3]>=200}
        body=max(groups(solid),key=len)
        ymin,ymax=min(y for x,y in body),max(y for x,y in body)
        solid={(x,y) for x,y in solid if ymin<=y<=ymax}
        x0,y0=min(x for x,y in solid),min(y for x,y in solid)
        x1,y1=max(x for x,y in solid)+1,max(y for x,y in solid)+1
        skin={(x,y) for x,y in solid if (lambda p:p[0]>180 and p[0]>p[1]*1.04 and p[1]>p[2]*1.02)(pixels[x,y])}
        # Filtered diagonal skin links must remain connected when measuring the
        # cheek and chin. Do not bridge across costume or hand outlines.
        face=max(groups(skin,diagonal=True),key=len);chin=max(y for x,y in face);face_top=min(y for x,y in face)
        row_starts={}
        for x,y in solid:
            if x<col+120:row_starts[y]=min(row_starts.get(y,119),x-col)
        starts=[row_starts.get(line,119) for line in range(row+120,row+340)]
        boundary=col+Counter(x for x in starts if x<110).most_common(1)[0][0]
        palms=groups((x,y) for x,y in solid if x<boundary+18 and chin+6<y<row+335
                     and (lambda p:p[0]>p[1]*1.1 and p[1]>p[2]*1.05)(pixels[x,y]))
        palms=sorted((g for g in palms if len(g)>=12 and max(y for x,y in g)-min(y for x,y in g)<50),key=len,reverse=True)[:2]
        assert len(palms)==2,(key,index,'missing gripping hands')
        centers=[(sum(x for x,y in g)/len(g),sum(y for x,y in g)/len(g)) for g in palms]
        assert abs(centers[0][1]-centers[1][1])>20,(key,index,centers)
        grip_x,grip_y=(sum(c[i] for c in centers)/2 for i in (0,1))
        flower=max(groups((x,y) for x,y in solid if y<y0+100
                          and (lambda p:p[1]>p[0]*1.08 and p[1]>p[2]*1.04)(pixels[x,y])),key=len)
        hx,hy=(sum(p[i] for p in flower)/len(flower) for i in (0,1))
        eye_groups=groups((x,y) for x,y in solid if face_top<=y<chin-8
                          and (lambda p:p[1]>p[0]*1.1 and p[1]>p[2]*1.05 and p[0]<170)(pixels[x,y]))
        eye=max((g for g in eye_groups if len(g)>=8),key=lambda g:sum(x for x,y in g)/len(g))
        scale=HEAD_HEIGHT/(chin-y0)*CORRECTIONS.get(f'{name}:{index}',1)
        depth=(sum(x for x,y in eye)/len(eye)-grip_x)*scale
        ox=GRIP[0]-(grip_x-x0)*scale;oy=GRIP[1]-(grip_y-y0)*scale
        assert ox>=1 and oy>=1 and ox+(x1-x0)*scale<CANVAS[0]-1 and oy+(y1-y0)*scale<CANVAS[1]-1,(key,index,ox,oy,scale,(x0,y0,x1,y1),centers)
        frame={'image':name,'durationMs':40 if key in ('nod','sway') else 10,
               'region':{'x':x0,'y':y0,'width':x1-x0,'height':y1-y0},
               'canvas':{'width':CANVAS[0],'height':CANVAS[1],'scale':round(scale,6),'offsetX':round(ox,3),'offsetY':round(oy,3)},
               'edgeAnchorX':GRIP[0]/CANVAS[0],'edgeAnchorY':GRIP[1]/CANVAS[1],
               'headAnchorX':round((ox+(hx-x0)*scale)/CANVAS[0],6),
               'headAnchorY':round((oy+(hy-y0)*scale)/CANVAS[1],6)}
        result.append((depth,index,frame))
    return result

def register_side(actions):
    measured={key:measure(key) for key in ATLASES}
    # Generated reveals overlap instead of respecting a 50% split. Interleave
    # by actual visible right-eye distance from the fixed grip, not atlas order.
    ordered=sorted([f for key,frames in measured.items() if key not in ('nod','sway') for f in frames],key=lambda f:f[0])
    assert len(ordered)==144
    assert max(b[0]-a[0] for a,b in zip(ordered,ordered[1:]))<=5
    reveal=[f[2] for f in ordered]
    REGISTRATION.update(revealOrder=[(f[2]['image'],f[1],round(f[0],3)) for f in ordered])
    REGISTRATION['maximumDepthStep']=round(max(b[0]-a[0] for a,b in zip(ordered,ordered[1:])),3)
    leaving=list(reversed(reveal[:-1]))
    def gesture(key):
        frames=[f[2] for f in measured[key]]
        # The new gesture poses lean less deeply than the final reveal. Insert
        # at their closest real depth so the head never jumps back at the join.
        insertion=min(range(len(ordered)),key=lambda i:abs(ordered[i][0]-measured[key][0][0]))
        REGISTRATION[key+'Insertion']=insertion
        return reveal[:insertion+1]+frames+list(reversed(frames[:-1]))+reveal[insertion+1:]+leaving
    sequences={'edge-peek':reveal+leaving,
               'edge-shy':reveal+list(reversed(reveal[71:143]))+reveal[72:144]+leaving,
               'edge-nod':gesture('nod'),'edge-sway':gesture('sway')}
    actions['edge-idle']={'loop':True,'frames':[dict(deepcopy(reveal[0]),durationMs=1000)]}
    for name,frames in sequences.items():
        actions[name]={'loop':False,'smoothFrames':True,'frames':deepcopy(frames)}
    return actions
