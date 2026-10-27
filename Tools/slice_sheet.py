import sys, os
from PIL import Image
import numpy as np
from scipy import ndimage as ndi
src,outdir,prefix,n_expect=sys.argv[1],sys.argv[2],sys.argv[3],int(sys.argv[4])
rows=int(sys.argv[5]); thr=float(sys.argv[6]) if len(sys.argv)>6 else 22
os.makedirs(outdir,exist_ok=True)
im=Image.open(src).convert('RGB'); a=np.array(im).astype(float); H,W=a.shape[:2]
bg=np.median(np.concatenate([a[0],a[-1],a[:,0],a[:,-1]]),axis=0)
d=np.sqrt(((a-bg)**2).sum(axis=2))
alpha=np.clip((d-12)/26,0,1)
mask=d>thr
grown=ndi.binary_dilation(mask,iterations=10)
lab,n=ndi.label(grown)
objs=ndi.find_objects(lab)
areas=[(lab[o]==i+1).sum() for i,o in enumerate(objs)]
keep=[i for i in np.argsort(areas)[::-1][:n_expect]]
items=[]
for i in keep:
    o=objs[i]; cy=(o[0].start+o[0].stop)/2; cx=(o[1].start+o[1].stop)/2; items.append((i,cy,cx))
# order: rows by y, then x
items.sort(key=lambda t:t[1]); per=n_expect//rows; ordered=[]
for r in range(rows):
    row=sorted(items[r*per:(r+1)*per],key=lambda t:t[2]); ordered+=row
for k,(i,cy,cx) in enumerate(ordered,1):
    o=objs[i]; own=(lab==i+1)
    sub=(slice(max(0,o[0].start-4),o[0].stop+4),slice(max(0,o[1].start-4),o[1].stop+4))
    al=(alpha*ndi.binary_dilation(own,iterations=3))[sub]
    rgba=np.dstack([a[sub],al*255]).astype(np.uint8); ic=Image.fromarray(rgba,'RGBA')
    s=max(ic.size); sq=Image.new('RGBA',(s,s),(0,0,0,0)); sq.paste(ic,((s-ic.width)//2,(s-ic.height)//2))
    sq.resize((256,256),Image.LANCZOS).save(f'{outdir}/{prefix}_{k:02d}.png')
print('components',n,'saved',len(ordered))
