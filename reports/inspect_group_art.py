from pathlib import Path
from PIL import Image, ImageDraw
import struct, json
root=Path(__file__).resolve().parents[1]
levels=[]
sheet=Image.new('RGB',(1400,1100),(28,28,32)); draw=ImageDraw.Draw(sheet)
for i,folder in enumerate(sorted((root/'Packages/niveles').glob('*_capas'))):
    imgs={p.stem:Image.open(p).convert('RGBA') for p in folder.glob('*.png')}
    base=Image.new('RGBA',imgs['Suelo'].size)
    for name in ['Suelo','Paredes','Objetos','Decals']: base.alpha_composite(imgs[name])
    x=(i%2)*700;y=(i//2)*550
    draw.text((x+10,y+5),folder.name,fill='white')
    base.thumbnail((680,320),Image.Resampling.NEAREST);sheet.paste(base,(x+10,y+28),base)
    for j,name in enumerate(['Colisiones','Triggers','Items_guia','Personajes','Luz_Oscuridad']):
        im=imgs[name]; colors=im.getdata(); counts={}
        for c in colors:
            if c[3]: counts[str(c)]=counts.get(str(c),0)+1
        print(folder.name,name,im.size,im.getbbox(), sorted(counts.items(),key=lambda z:-z[1])[:12])
        im=im.copy();im.thumbnail((132,150),Image.Resampling.NEAREST)
        xx=x+8+j*138;draw.text((xx,y+368),name,fill='white');sheet.paste(im,(xx,y+390),im)
    levels.append({'level':folder.name,'size':imgs['Suelo'].size})
sheet.save(root/'reports/source_maps.png')
for p in (root/'Assets/_Project/Art/Characters/Player/Official').glob('*.aseprite'):
    data=p.read_bytes();print(p.name,'frames,width,height,depth',struct.unpack_from('<4H',data,6))
