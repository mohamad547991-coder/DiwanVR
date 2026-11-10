"""Create a release record only from matching, successful build/test artifacts."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json

SUITES = ('scenario_tests.json', 'play_mode_tests.json', 'office040_play_tests.json', 'fix044_tests.json')
PACKAGE = 'org.diwan.training.records'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def code_digest(project):
    files = sorted((project / 'Assets/Diwan').rglob('*.cs'), key=lambda p: p.relative_to(project).as_posix())
    data = ''.join(f'{p.relative_to(project).as_posix()}\n{digest(p)}\n' for p in files)
    return hashlib.sha256(data.encode()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def build_record(root, version='0.4.4'):
    project = root / 'UnityProject'
    validation = project / 'Validation'
    candidates = list(validation.glob('run-*/result.json'))
    require(bool(candidates), 'No validation run found.')
    run_path = max(candidates, key=lambda p: p.stat().st_mtime_ns)
    run = read(run_path)
    require(run.get('status') == 'passed' and run.get('sourceVersion') == version, 'The latest validation is not a passing run for this release.')
    require(run.get('checks') and all(c.get('passed') is True for c in run['checks']), 'Validation contains failed or missing checks.')
    require(not run.get('errors'), 'Validation recorded product errors.')
    require(run.get('finishedUtc'), 'Validation did not finish.')
    require(run.get('codeSha256') == code_digest(project), 'Source changed since validation.')
    require(run.get('sceneSha256') == digest(project / 'Assets/Diwan/Scenes/Diwan.unity'), 'Scene changed since validation.')

    install = root / 'Install'
    receipt = read(install / f'DiwanVR_{version}_receipt.json')
    require(receipt.get('result') == 'Succeeded' and receipt.get('version') == version, 'Build receipt is not successful for this release.')
    require(receipt.get('codeSha256') == run['codeSha256'] and receipt.get('sceneSha256') == run['sceneSha256'], 'Build and validation describe different sources.')
    original = project / 'Builds/Quest' / ('run-' + receipt['runId']) / 'receipt.json'
    require(read(original) == receipt, 'Installed receipt differs from the build receipt.')
    apk = install / f'DiwanVR_{version}_Quest.apk'
    apk_sha = digest(apk)
    require(apk.stat().st_size > 0 and apk.stat().st_size == receipt.get('bytes') and apk_sha == receipt.get('sha256'), 'APK bytes do not match the build receipt.')
    report = read(validation / f'apk{version.replace(".", "")}_verification.json')
    require(report.get('signatureVerified') is True and report.get('arm64') is True, 'APK signature or architecture verification failed.')
    require(report.get('packageName') == PACKAGE and report.get('version') == version and report.get('versionCode') == receipt.get('versionCode'), 'APK identity does not match this release.')
    require(report.get('apkSha256') == apk_sha and report.get('apkBytes') == apk.stat().st_size, 'APK changed after verification.')

    counts = {}
    for name in SUITES:
        # Immutable copies collected by this run, not later global reports.
        suite = read(run_path.parent / name)
        require(suite.get('status') == 'passed' and suite.get('tests'), f'Suite did not pass: {name}')
        require(all(t.get('passed') is True for t in suite['tests']), f'Suite has failed checks: {name}')
        require(any(c.get('name') == name and c.get('passed') is True for c in run['checks']), f'Suite was not collected by this run: {name}')
        counts[name] = len(suite['tests'])
    preflight = len(run['checks']) - len(SUITES)
    return dict(sourceVersion=version, apkVersion=report['version'], androidVersionCode=report['versionCode'],
                unityVersion='6000.6.4f1', compiledInUnity=True, validationPassed=True,
                validationRunId=run['runId'], validationFinishedUtc=run['finishedUtc'],
                playModeTested=True, headsetTested=False, newApkBuilt=True,
                buildRunId=receipt['runId'], builtUtc=receipt['finishedUtc'],
                testCounts=counts, preflightChecks=preflight, totalChecks=sum(counts.values()) + preflight,
                productErrorsInValidation=[], ignoredEditorErrors=len(run.get('ignoredErrors', [])),
                apkSignatureVerified=True, apkBytes=report['apkBytes'], apkSha256=apk_sha,
                apkPackageName=PACKAGE, arm64=True, codeSha256=run['codeSha256'], sceneSha256=run['sceneSha256'],
                validationKind='Editor simulation and rendered image smoke tests; headset untested',
                recordedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--version', default='0.4.4')
    args = parser.parse_args()
    root = Path(__file__).resolve().parent
    target = root / 'UnityProject/Validation' / f'release{args.version.replace(".", "")}_status.json'
    try:
        record = build_record(root, args.version)
    except (ValueError, KeyError, OSError) as error:
        raise SystemExit(f'Release not verified: {error}')
    target.write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(json.dumps(record, indent=2))
    print(target)
