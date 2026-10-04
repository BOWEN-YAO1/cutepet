"""Read original alpha/contact pixels and register insertion sprites; never edits PNGs."""
from pathlib import Path
from collections import Counter
from copy import deepcopy
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / 'src/CutePet.Desktop/Characters/Packs/tianyi'
# Bands follow the generated layout, which is not an exact 384-pixel grid.
BANDS = {
    'standing': [0, 420, 775, 1140, 1536],
    'throne': [0, 423, 762, 1110, 1536],
    'cloud': [0, 381, 751, 1135, 1536],
    'side': [0, 384, 772, 1155, 1536],
    'bottom': [65, 439, 790, 1126, 1536],
    'top': [0, 390, 760, 1133, 1536],
}
# Subpixel resampling shifts thin rings; measured against actual WPF alpha at the
# two attachment ranges (not the unscaled PNG's antialiased outline).
TOP_CONTACT_OFFSET = {0:-1,1:-1,2:-4,3:-1,4:0,5:-2,6:-3,7:0,
                      8:0,9:-2,10:-7,11:-2,12:0,13:-1,14:-6,15:0}
PAIR_INDICES = {
    'standing': {(4,5):0, (0,5):0, (5,6):2, (6,7):3, (0,8):4, (8,9):5, (10,11):7,
                 (0,12):8, (12,13):9, (0,14):10, (14,15):11, (0,1):12, (1,2):13},
    'throne': {(0,1):0, (1,2):1, (2,3):2, (3,4):3, (4,5):4, (5,6):5, (6,7):6, (7,8):7,
               (8,9):8, (9,10):9, (8,12):12, (13,14):14},
    'cloud': {(0,1):0, (1,2):1, (4,5):4, (5,6):7, (6,7):6, (8,9):9, (9,10):10,
              (0,12):12, (12,13):13, (13,14):14, (14,15):15, (0,8):8},
    'bottom': {(0,1):0, (1,2):1, (2,3):2, (3,4):3, (4,5):4, (5,6):5, (6,7):6, (0,4):7,
               (2,8):8, (8,9):9, (2,10):10, (10,11):11, (4,12):12, (12,13):13,
               (12,14):14, (14,15):15},
    'top': {(0,1):0, (1,2):1, (2,3):3, (3,4):2, (4,5):4, (5,6):5, (6,7):6, (3,0):7,
            (1,8):8, (8,9):9, (2,10):10, (10,11):11, (1,12):12, (12,13):13,
            (13,14):14, (14,15):15},
}

def insert_inbetweens(actions, old_frame):
    old_locations = {}
    for key in BANDS:
        for index in range(16):
            f = old_frame(key,index)
            old_locations[(f['image'],tuple(f['region'].values()))] = (key,index)
    originals = {key:Image.open(ROOT/f'{key}-inbetweens-v1.png').convert('RGBA') for key in BANDS}
    registered = {}

    def middle(key,index):
        if (key,index) in registered: return deepcopy(registered[key,index])
        image = originals[key]; pixels = image.load()
        col = index%4*256; start,end = BANDS[key][index//4:index//4+2]
        # Opaque bounds are measurements only; preserve the unmodified source PNG.
        points=[(x,y) for y in range(start,end) for x in range(col+8,col+248) if pixels[x,y][3]>=200]
        x0=min(x for x,y in points);x1=max(x for x,y in points)+1
        y0=min(y for x,y in points);y1=max(y for x,y in points)+1
        w,h=x1-x0,y1-y0
        normal=key in ('standing','throne','cloud');cw,ch=(240,360) if normal else (256,352)
        target_height={'standing':301,'throne':320,'cloud':301,'side':324,'bottom':273,'top':326}[key]
        scale=min(target_height/h,(cw-24)/w)
        ox=(cw-w*scale)/2;oy=344-h*scale if normal else 14
        if key=='bottom':oy=332-h*scale
        anchor={}
        if key=='side':
            starts=[next((x for x in range(col+20,col+120) if pixels[x,y][3]>=200),col+119)
                    for y in range(y0+int(h*.25),y1)]
            grip=Counter(x for x in starts if x<col+110).most_common(1)[0][0]
            skin=[y for y in range(y0+int(h*.5),y0+int(h*.9))
                  for x in range(max(x0,grip-13),min(x1,grip+6))
                  if pixels[x,y][3]>=200 and pixels[x,y][0]>pixels[x,y][1]*1.1
                  and pixels[x,y][1]>pixels[x,y][2]*1.05]
            anchor={'edgeAnchorX':(ox+(grip-x0)*scale)/cw,
                    'edgeAnchorY':(oy+((sum(skin)/len(skin) if skin else y0+h*.65)-y0)*scale)/ch}
        elif key=='bottom':
            contact=[y for y in range(y0+int(h*.8),y1)
                     if any(pixels[x,y][3]>=200 for x in range(col+46,col+115))
                     and any(pixels[x,y][3]>=200 for x in range(col+141,col+210))]
            anchor={'edgeAnchorY':(oy+(max(contact)-1-y0)*scale)/ch}
        elif key=='top':
            # Keep ring spacing consistent with the existing seat (±8 source pixels)
            # and register the suspension-to-seat distance exactly, independently of head tilt.
            scale=min(.94,(cw-8)/w,326/h)
            ox=(cw-w*scale)/2
            contact=[y for y in range(y0+int(h*.53),y0+int(h*.8))
                     if any(pixels[x,y][3]>=200 for x in range(col+24,col+47))
                     and any(pixels[x,y][3]>=200 for x in range(col+209,col+233))]
            seat=min(contact,key=lambda y:abs((y-y0)-h*.66))
            oy=242-(seat-y0)*scale
            if oy<1 or oy+h*scale>ch-4:
                scale=min(scale,241/(seat-y0),(ch-246)/(h-(seat-y0)))
                ox=(cw-w*scale)/2;oy=242-(seat-y0)*scale
            oy+=TOP_CONTACT_OFFSET[index]
            anchor={'swingSeatAnchorY':242/ch,'edgeAnchorY':0}
        frame={'image':f'{key}-inbetweens-v1.png','durationMs':40,
               'region':{'x':x0,'y':y0,'width':w,'height':h},
               'canvas':{'width':cw,'height':ch,'scale':scale,'offsetX':ox,'offsetY':oy},**anchor}
        assert ox>=0 and oy>=0 and ox+w*scale<=cw+.00001 and oy+h*scale<=ch+.00001,(key,index,frame)
        registered[key,index]=frame
        return deepcopy(frame)

    def locate(f):return old_locations.get((f['image'],tuple(f['region'].values())))
    def between(a,b):
        aa,bb=locate(a),locate(b)
        if aa is None or bb is None or aa[0]!=bb[0]:return []
        key=aa[0];pair=(aa[1],bb[1])
        if key=='side':
            indices=[0,4,6,8,9,10,12,14]
            if pair==(2,3):return [middle(key,i) for i in indices]
            if pair==(3,2):return [middle(key,i) for i in reversed(indices)]
            return []
        choices=PAIR_INDICES[key]
        i=choices.get(pair,choices.get(tuple(reversed(pair))))
        return [] if i is None else [middle(key,i)]

    # Base-pose entry/return is explicit, so responses do not start at a raised hand.
    idle=deepcopy(actions['idle']['frames'][0]);idle['durationMs']=40
    actions['greeting']['frames'][0]=deepcopy(idle)
    actions['greeting']['frames'][0]['durationMs']=80
    actions['happy']['frames'].insert(0,deepcopy(idle))
    for name,base in [('blink','idle'),('sit-blink','sit'),('cloud-blink','cloud-idle')]:
        first=deepcopy(actions[base]['frames'][0]);first['durationMs']=40
        actions[name]['frames'].insert(0,first)
    for name,clip in actions.items():
        if name=='stand':continue
        frames=clip['frames'];result=[]
        for pos,original in enumerate(frames):
            current=deepcopy(original)
            following=frames[pos+1] if pos+1<len(frames) else frames[0] if clip['loop'] else None
            additions=between(current,following) if following else []
            if additions:
                current['durationMs']=max(40,current['durationMs']-40*len(additions))
            result.extend([current]+additions)
        clip['frames']=result
    # Exact same cached poses in reverse retain interrupted lowering at its current drawing.
    rising=[deepcopy(f) for f in reversed(actions['conjure']['frames'])]
    duration=1000//len(rising)//10*10
    assert duration>=40
    for frame in rising:frame['durationMs']=duration
    rising[0]['durationMs']+=1000-duration*len(rising)
    actions['stand']={'loop':False,'frames':rising}
    return actions
