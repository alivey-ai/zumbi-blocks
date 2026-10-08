import glob,sys
for f in glob.glob(sys.argv[1]+'/**/*.cs',recursive=True):
    s=open(f,encoding='utf-8').read()
    key=sys.argv[2]+'.op_Implicit('
    if key not in s: continue
    while True:
        i=s.find(key)
        if i<0: break
        j=i+len(key); d=1
        while d:
            c=s[j]
            if c=='(':d+=1
            elif c==')':d-=1
            j+=1
        inner=s[i+len(key):j-1]
        s=s[:i]+'('+inner+(' != null' if sys.argv[2]=='Object' else '')+')'+s[j:]
    open(f,'w',encoding='utf-8').write(s)
