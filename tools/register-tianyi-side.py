"""Measure the unmodified side atlas; normalize head scale and fixed hand grips.

This reads source pixels and writes only manifest metadata, never bitmap files.
"""
from pathlib import Path
from collections import Counter
from copy import deepcopy
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
NAME = 'side-sequence-v6.png'
HEAD_HEIGHT = 200
CANVAS = (288, 384)
GRIP = (48, 250)

def register_side(actions):
    image = Image.open(ROOT / NAME).convert('RGBA')
    pixels = image.load()
    frames = []
    for index in range(15):
        col, row = index % 4 * 256, index // 4 * 384
        solid = {(x, y) for y in range(384) for x in range(256) if pixels[col+x, row+y][3] >= 200}
        starts = [next((x for x in range(20,120) if (x,y) in solid),119) for y in range(120,340)]
        grip = Counter(x for x in starts if x < 110).most_common(1)[0][0]
        # Keep this sprite's fingers, but omit hair spilling from adjacent cells.
        xstart, xend = col + grip - 12, min(image.width, col + grip + 225)
        solid = {(x,y) for y in range(row,row+384) for x in range(xstart,xend) if pixels[x,y][3]>=200}
        skin = {(x,y) for x,y in solid if (lambda p:p[0]>180 and p[0]>p[1]*1.04 and p[1]>p[2]*1.02)(pixels[x,y])}
        groups = []
        while skin:
            seed = skin.pop(); todo = [seed]; group = [seed]
            while todo:
                x,y = todo.pop()
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    point=x+dx,y+dy
                    if point in skin:
                        skin.remove(point); todo.append(point); group.append(point)
            groups.append(group)
        face = max(groups,key=len)
        x0,y0 = min(x for x,y in solid),min(y for x,y in solid)
        x1,y1 = max(x for x,y in solid)+1,max(y for x,y in solid)+1
        chin = max(y for x,y in face)
        scale = HEAD_HEIGHT / (chin-y0)
        hands = [y for x,y in solid if abs(x-col-grip)<13 and row+200<y<row+330
                 and (lambda p:p[0]>p[1]*1.1 and p[1]>p[2]*1.05)(pixels[x,y])]
        hand_y = sum(hands)/len(hands)
        face_top=min(y for x,y in face)
        eye_pixels={(x,y) for x,y in solid if face_top<=y<chin-8
                    and (lambda p:p[1]>p[0]*1.1 and p[1]>p[2]*1.05 and p[0]<170)(pixels[x,y])}
        eyes=[]
        while eye_pixels:
            seed=eye_pixels.pop();todo=[seed];group=[seed]
            while todo:
                x,y=todo.pop()
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    point=x+dx,y+dy
                    if point in eye_pixels:
                        eye_pixels.remove(point);todo.append(point);group.append(point)
            if len(group)>=10:eyes.append(group)
        eye=max(eyes,key=lambda group:sum(x for x,y in group)/len(group))
        eye_x=sum(x for x,y in eye)/len(eye);eye_y=sum(y for x,y in eye)/len(eye)
        ox = GRIP[0] - (col+grip-x0)*scale
        oy = GRIP[1] - (hand_y-y0)*scale
        w,h = x1-x0,y1-y0
        assert ox>=0 and oy>=0 and ox+w*scale<=CANVAS[0] and oy+h*scale<=CANVAS[1], (index,ox,oy,w,h,scale)
        frames.append({'image':NAME,'durationMs':40,
                       'region':{'x':x0,'y':y0,'width':w,'height':h},
                       'canvas':{'width':CANVAS[0],'height':CANVAS[1],'scale':scale,'offsetX':ox,'offsetY':oy},
                       'edgeAnchorX':GRIP[0]/CANVAS[0],'edgeAnchorY':GRIP[1]/CANVAS[1],
                       'headAnchorX':(ox+(eye_x-x0)*scale)/CANVAS[0],
                       'headAnchorY':(oy+(eye_y-y0)*scale)/CANVAS[1]})
    # Sprite 15 releases a hand and changes costume details; do not adopt it.
    entering = list(range(15))
    leaving = list(range(13,-1,-1))
    sequences = {
        'edge-peek':entering+leaving,
        'edge-shy':list(range(9))+list(range(7,2,-1))+list(range(4,15))+leaving,
        'edge-nod':entering+[13,12,13,14]+leaving,
        'edge-sway':entering+[13,12,11,12,13,14,13,12,13,14]+leaving,
    }
    actions['edge-idle']={'loop':True,'frames':[dict(deepcopy(frames[0]),durationMs=1000)]}
    for name,indices in sequences.items():
        assert all(abs(a-b)==1 for a,b in zip(indices,indices[1:])),name
        actions[name]={'loop':False,'smoothFrames':True,'frames':[deepcopy(frames[i]) for i in indices]}
    return actions
