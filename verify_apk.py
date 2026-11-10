"""Verify APK identity, ARM64 and signing before writing a successful report."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import subprocess

PACKAGE = 'org.diwan.training.records'


def parse_badging(text):
    import re
    fields = {}
    for line in text.splitlines():
        if line.startswith('package:'):
            fields.update(re.findall(r"(\w+)='([^']*)'", line))
        elif line.startswith('native-code:'):
            fields['native-code'] = line.split(':', 1)[1].strip()
    return fields


def check_identity(fields, version):
    if fields.get('name') != PACKAGE:
        raise ValueError('Unexpected Android package identity.')
    if fields.get('versionName') != version:
        raise ValueError('APK version differs from the requested release.')
    if 'arm64-v8a' not in fields.get('native-code', '').split("'"):
        raise ValueError('APK does not contain arm64-v8a native code.')
    if version == '0.4.4' and fields.get('versionCode') != '8':
        raise ValueError('Release 0.4.4 requires Android versionCode 8.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('version', nargs='?', default='0.4.4')
    parser.add_argument('--unity-data', type=Path, default=Path(r'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data'))
    args = parser.parse_args()
    root = Path(__file__).resolve().parent
    apk = root / 'Install' / f'DiwanVR_{args.version}_Quest.apk'
    android = args.unity_data / 'PlaybackEngines/AndroidPlayer'
    sdk = android / 'SDK/build-tools/36.0.0'
    target = root / 'UnityProject/Validation' / f'apk{args.version.replace(".", "")}_verification.json'
    # A failed attempt must not leave an older successful report in its place.
    target.unlink(missing_ok=True)
    try:
        def run(command):
            completed = subprocess.run([str(x) for x in command], capture_output=True, text=True, timeout=120)
            output = completed.stdout + completed.stderr
            if completed.returncode != 0:
                raise ValueError(output.strip() or f'Tool failed: {command[0]}')
            return output
        badging = run([sdk / 'aapt.exe', 'dump', 'badging', apk])
        fields = parse_badging(badging)
        check_identity(fields, args.version)
        signing = run([android / 'OpenJDK/bin/java.exe', '-jar', sdk / 'lib/apksigner.jar', 'verify', '--verbose', '--print-certs', apk])
        if 'Verifies' not in signing:
            raise ValueError('APK signature did not verify.')
        with apk.open('rb') as stream:
            digest = hashlib.file_digest(stream, 'sha256').hexdigest()
        report = dict(version=fields['versionName'], versionCode=int(fields['versionCode']),
                      packageName=fields['name'], arm64=True, nativeCode=fields['native-code'],
                      signatureVerified=True, signingOutput=signing.strip(),
                      apkBytes=apk.stat().st_size, apkSha256=digest,
                      verifiedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), headsetTested=False)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(report, indent=2), encoding='utf-8')
        print(json.dumps(report, indent=2))
        print(target)
    except (ValueError, OSError, subprocess.TimeoutExpired) as error:
        raise SystemExit(f'APK verification failed: {error}')


if __name__ == '__main__':
    main()
