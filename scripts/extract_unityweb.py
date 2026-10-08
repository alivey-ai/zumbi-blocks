import struct,lzma,sys,os
d=open(sys.argv[1],'rb').read()
out=sys.argv[2]
p=0
def cs():
    global p
    e=d.index(b'\0',p); s=d[p:e]; p=e+1; return s
sig=cs(); fmt=struct.unpack('>i',d[p:p+4])[0]; p+=4
v=cs(); e=cs(); print(sig,fmt,v,e)
size,hdr,nlev=struct.unpack('>iii',d[p:p+12]); p+=12
levels=[struct.unpack('>ii',d[p+8*i:p+8*i+8]) for i in range(nlev)]; p+=8*nlev
print(size,hdr,levels)
print(d[p:hdr][:40])
c=d[hdr:hdr+levels[-1][0]]
# last level covers all (cumulative)
raw=lzma.LZMADecompressor(format=lzma.FORMAT_ALONE).decompress(c)
print(len(raw))
n=struct.unpack('>i',raw[:4])[0]; q=4
os.makedirs(out,exist_ok=True)
for i in range(n):
    e=raw.index(b'\0',q); name=raw[q:e].decode(); q=e+1
    off,sz=struct.unpack('>ii',raw[q:q+8]); q+=8
    print(name,off,sz)
    open(os.path.join(out,name.replace('/','_')),'wb').write(raw[off:off+sz])
