from pathlib import Path
import subprocess
root = Path(__file__).resolve().parent
unity = Path('C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data')
csc = unity / 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
dotnet = unity / 'DotNetSdk/dotnet.exe'
# Resolve the moved project's references locally; old response files pointed
# into another Desktop folder and silently checked the wrong package versions.
assemblies = list((unity/'Managed/UnityEngine').glob('*.dll'))
assemblies += [unity/'NetStandard/ref/2.1.0/netstandard.dll']
assemblies += list((unity/'NetStandard/compat/2.1.0/shims/netfx').glob('*.dll'))
assemblies += [p for p in (root/'UnityProject/Library/ScriptAssemblies').glob('*.dll')
               if not p.name.startswith('Assembly-CSharp')]
refs = ['-r:"'+str(p)+'"' for p in dict.fromkeys(assemblies) if p.exists()]
lines=['-target:library','-nostdlib','-define:UNITY_EDITOR,UNITY_ANDROID,UNITY_6000_0_OR_NEWER,UNITY_6000_1_OR_NEWER,UNITY_6000_2_OR_NEWER','-out:"'+str(root/'all-source-check.dll')+'"']+refs
lines += ['"'+str(p)+'"' for p in (root/'UnityProject/Assets/Diwan').rglob('*.cs')]
rsp=root/'all-source-check.rsp'
rsp.write_text('\n'.join(lines),encoding='utf-8')
r=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],capture_output=True,text=True)
(root/'source-check.log').write_text(r.stdout+r.stderr,encoding='utf-8')
print(r.stdout+r.stderr)
raise SystemExit(r.returncode)
