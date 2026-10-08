import re,glob,sys
for f in glob.glob(sys.argv[1]+'/**/*.cs',recursive=True):
    if 'Shim' in f: continue
    s=open(f,encoding='utf-8').read(); o=s
    s=re.sub(r'^(\s*)\(\(MonoBehaviour\)this\)\._002Ector\(\);\n','',s,flags=re.M)
    s=re.sub(r'^(\s*)(.+?)\.emissionRate = (.+);$',r'\1{ var __em = \2.emission; __em.rateOverTime = \3; }',s,flags=re.M)
    s=s.replace('Screen.showCursor','Cursor.visible')
    s=re.sub(r'Screen\.lockCursor = (true|false);',lambda m:'Cursor.lockState = CursorLockMode.%s;'%('Locked' if m.group(1)=='true' else 'None'),s)
    s=re.sub(r'\.particleSystem\b',r'.GetComponent<ParticleSystem>()',s)
    s=re.sub(r'\binteract_obj\.collider\b','interact_obj.GetComponent<Collider>()',s)
    if 'using UnityEngine;' in s and 'using Random =' not in s:
        s=s.replace('using UnityEngine;','using UnityEngine;\nusing Random = UnityEngine.Random;',1)
    if s!=o: open(f,'w',encoding='utf-8').write(s)
