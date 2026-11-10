from pathlib import Path
import subprocess,json
root=Path(__file__).resolve().parent
sdk=Path('C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk')
output=root/'PinchRegression.dll'
args=['-target:exe','-nostdlib','-out:"'+str(output)+'"']
args += ['-r:"'+str(p)+'"' for p in (sdk/'packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0').glob('*.dll')]
args += ['"'+str(p)+'"' for p in [root/'PinchRegression.cs',root.parent/'UnityProject/Assets/Diwan/Runtime/TrackedPinch.cs']]
rsp=root/'pinch-test.rsp';rsp.write_text('\n'.join(args),encoding='utf-8')
subprocess.run([str(sdk/'dotnet.exe'),str(sdk/'sdk/8.0.318/Roslyn/bincore/csc.dll'),'@'+str(rsp)],check=True)
output.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
r=subprocess.run([str(sdk/'dotnet.exe'),str(output)],capture_output=True,text=True)
(root/'pinch-results.txt').write_text(r.stdout+r.stderr,encoding='utf-8')
print(r.stdout+r.stderr)
raise SystemExit(r.returncode)
