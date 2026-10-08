import re,glob,sys
files=[f for f in glob.glob(sys.argv[1]+'/**/*.cs',recursive=True) if 'LegacyNetworkShim' not in f]
for f in files:
    s=open(f,encoding='utf-8-sig').read(); o=s
    s=re.sub(r'\(\([A-Za-z0-9]+\)\(ref ([A-Za-z_0-9]+)\)\)',r'\1',s)
    s=s.replace('.networkView','.NetView()')
    for n,t in [('animation','Animation'),('rigidbody','Rigidbody'),('renderer','Renderer'),('light','Light'),('camera','Camera'),('audio','AudioSource')]:
        s=re.sub(r'\.'+n+r'\b(?!\s*\()',f'.GetComponent<{t}>()',s)
    s=re.sub(r'(\(Component\)[A-Za-z_0-9\[\]]+\)|gameObject)\.collider\b',r'\1.GetComponent<Collider>()',s)
    if 'using UnityEngine;' in s and 'using Object =' not in s:
        s=s.replace('using UnityEngine;','using UnityEngine;\nusing Object = UnityEngine.Object;',1)
    s=re.sub(r'public override void (OnPlayerConnected|OnDisconnectedFromServer|OnConnectedToServer|OnFailedToConnect|OnPlayerDisconnected)',r'public void \1',s)
    if s!=o: open(f,'w',encoding='utf-8').write(s)
