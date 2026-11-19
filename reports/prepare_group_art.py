"""Copy original group assets and derive geometry from technical source layers.
No scene rendering or gameplay is involved. Export coordinates use bottom-left / 64 PPU.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np
import shutil,json
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/_Project/Art'

def components(mask):
    mask=mask.copy(); result=[]
    for yy in range(mask.shape[0]):
        for xx in np.flatnonzero(mask[yy]):
            if not mask[yy,xx]: continue
            stack=[(int(xx),yy)];mask[yy,xx]=False;x0=x1=int(xx);y0=y1=yy;count=0
            while stack:
                x,y=stack.pop();count+=1;x0=min(x0,x);x1=max(x1,x);y0=min(y0,y);y1=max(y1,y)
                for a,b in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                    if 0<=a<mask.shape[1] and 0<=b<mask.shape[0] and mask[b,a]:
                        mask[b,a]=False;stack.append((a,b))
            result.append((x0,y0,x1+1,y1+1,count))
    return result

def rectangles(mask):
    active={};out=[]
    for y,row in enumerate(mask):
        edges=np.diff(np.r_[False,row,False].astype(int));runs=list(zip(np.flatnonzero(edges==1),np.flatnonzero(edges==-1)))
        current={}
        for a,b in runs:
            key=(int(a),int(b));current[key]=active.pop(key,[int(a),y,int(b),y])
            current[key][3]=y+1
        out.extend(active.values());active=current
    return out+list(active.values())

def box(b,H):
    x,y,x1,y1=b[:4];return dict(x=int(x),y=int(H-y1),w=int(x1-x),h=int(y1-y))

def copy_tree(source,dest):
    dest.mkdir(parents=True,exist_ok=True)
    for p in source.rglob('*'):
        if p.is_file() and p.suffix!='.meta':
            q=dest/p.relative_to(source);q.parent.mkdir(parents=True,exist_ok=True)
            if not q.exists() or p.read_bytes()!=q.read_bytes(): shutil.copy2(p,q)

for src,dest in [('props','Environment/Props'),('tiles','Environment/Tiles'),('decals','Environment/Decals'),('items','Items'),('luces','Lighting')]:
    copy_tree(ROOT/'Packages'/src,ART/dest)
for p in (ROOT/'Packages/personajes').glob('*'):
    if p.suffix=='.meta' or not p.stem.startswith('sombra'):continue
    dest=ART/'Characters/Enemies/Shadow'
    dest.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest/p.name)

for source in sorted((ROOT/'Packages/niveles').glob('*_capas')):
    name=source.name.removesuffix('_capas');dest=ART/'Levels'/name
    copy_tree(source,dest/'Layers')
    shutil.copy2(ROOT/'Packages/niveles'/f'{name}.aseprite',dest/f'{name}.aseprite')
    ims={p.stem:Image.open(p).convert('RGBA') for p in source.glob('*.png')};W,H=ims['Suelo'].size
    mask=np.array(ims['Colisiones'])[:,:,3]>0
    rects=rectangles(mask)
    # Verify exact mask reconstruction before exporting geometry.
    rebuilt=np.zeros_like(mask)
    for a,b,c,d in rects: rebuilt[b:d,a:c]=True
    assert np.array_equal(mask,rebuilt)
    data=dict(level=name,width=W,height=H,ppu=64,collisions=[box(r,H) for r in rects],objects=[],triggers=[],characters=[],items=[],lights=[])
    dilated=np.array(ims['Objetos'].getchannel('A').filter(ImageFilter.MaxFilter(5)))>0
    for r in components(dilated):
        # Restrict bounds to actual art (dilation is only for connecting a prop's pieces).
        x,y,x1,y1,_=r;region=np.array(ims['Objetos'])[y:y1,x:x1,3];ys,xs=np.nonzero(region)
        if len(xs): data['objects'].append(box((x+xs.min(),y+ys.min(),x+xs.max()+1,y+ys.max()+1),H))
    tr=np.array(ims['Triggers'])
    for rgb in np.unique(tr[tr[:,:,3]>0,:3],axis=0):
        for r in components((tr[:,:,3]>0)&np.all(tr[:,:,:3]==rgb,axis=2)):
            if r[4]>100:
                t=box(r,H);t['color']=','.join(map(str,rgb));data['triggers'].append(t)
    for r in components(np.array(ims['Personajes'].getchannel('A').filter(ImageFilter.MaxFilter(9)))>0):
        if r[4]>100:data['characters'].append(box(r,H))
    # Match actual item silhouettes, rather than making the guide layer visible.
    templates={p.stem:Image.open(p).convert('RGBA') for p in (ROOT/'Packages/items').glob('*.png')}
    for r in components(np.array(ims['Items_guia'].getchannel('A').filter(ImageFilter.MaxFilter(7)))>0):
        x,y,x1,y1,_=r;im=ims['Items_guia'].crop((x,y,x1,y1));bb=im.getbbox()
        if not bb:continue
        crop=im.crop(bb);a=np.array(crop);scores=[]
        for label,temp in templates.items():
            temp=temp.crop(temp.getbbox())
            if temp.size==crop.size:
                b=np.array(temp);opaque=(a[:,:,3]>160)&(b[:,:,3]>160)
                score=np.mean(np.abs(a[opaque,:3].astype(float)-b[opaque,:3])) if opaque.any() else 999
                scores.append((score,label))
        if scores and min(scores)[0]<35:
            item=box((x+bb[0],y+bb[1],x+bb[2],y+bb[3]),H);item['asset']=min(scores)[1];data['items'].append(item)
    # The brightest authored regions identify lamps; only one gets flicker.
    darkness=np.array(ims['Luz_Oscuridad'])[:,:,3]
    for r in sorted(components(darkness==40),key=lambda z:-z[4])[:4]:
        if r[4]>800:data['lights'].append(box(r,H))
    (dest/'LevelGeometry.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
    print(name,'colliders',len(rects),'objects',len(data['objects']),'items',data['items'],'triggers',data['triggers'],'characters',data['characters'])
