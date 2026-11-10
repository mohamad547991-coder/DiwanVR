"""Regression checks for stale/mismatched release evidence. No Unity required."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from release_verification import SUITES, build_record, code_digest, digest
from verify_apk import check_identity, parse_badging


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value), encoding='utf-8')


class ReleaseEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'UnityProject'
        source = self.project / 'Assets/Diwan/Runtime/Test.cs'
        source.parent.mkdir(parents=True)
        source.write_text('class Test {}', encoding='utf-8')
        self.scene = self.project / 'Assets/Diwan/Scenes/Diwan.unity'
        self.scene.parent.mkdir()
        self.scene.write_text('scene', encoding='utf-8')
        self.run_path = self.project / 'Validation/run-current/result.json'
        self.run = dict(status='passed', sourceVersion='0.4.4', runId='current', finishedUtc='2026-11-10T10:00:00Z',
                        codeSha256=code_digest(self.project), sceneSha256=digest(self.scene), errors=[],
                        checks=[dict(name='preflight', passed=True)] + [dict(name=s, passed=True) for s in SUITES])
        write(self.run_path, self.run)
        for name in SUITES:
            write(self.run_path.parent / name, dict(status='passed', tests=[dict(name='check', passed=True)]))
        self.apk = self.root / 'Install/DiwanVR_0.4.4_Quest.apk'
        self.apk.parent.mkdir()
        self.apk.write_bytes(b'fixture bytes; not a real APK')
        self.receipt = dict(result='Succeeded', version='0.4.4', versionCode=8, runId='build',
                            codeSha256=self.run['codeSha256'], sceneSha256=self.run['sceneSha256'],
                            bytes=self.apk.stat().st_size, sha256=digest(self.apk), finishedUtc='2026-11-10T10:01:00Z')
        write(self.root / 'Install/DiwanVR_0.4.4_receipt.json', self.receipt)
        write(self.project / 'Builds/Quest/run-build/receipt.json', self.receipt)
        self.report = dict(signatureVerified=True, arm64=True, packageName='org.diwan.training.records',
                           version='0.4.4', versionCode=8, apkBytes=self.apk.stat().st_size, apkSha256=digest(self.apk))
        self.report_path = self.project / 'Validation/apk044_verification.json'
        write(self.report_path, self.report)

    def reject(self, pattern):
        with self.assertRaisesRegex(ValueError, pattern):
            build_record(self.root)

    def test_matching_evidence_and_apk_sha256_field(self):
        record = build_record(self.root)
        self.assertEqual(record['apkSha256'], self.report['apkSha256'])
        self.assertEqual(record['totalChecks'], 5)
        self.assertFalse(record['headsetTested'])

    def test_changed_source(self):
        (self.project / 'Assets/Diwan/Runtime/Test.cs').write_text('class Changed {}')
        self.reject('Source changed')

    def test_changed_scene(self):
        self.scene.write_text('different scene')
        self.reject('Scene changed')

    def test_changed_apk_same_size(self):
        self.apk.write_bytes(b'X' * self.apk.stat().st_size)
        self.reject('APK bytes')

    def test_product_errors_cannot_pass(self):
        self.run['errors'] = ['Missing script']
        write(self.run_path, self.run)
        self.reject('product errors')

    def test_stale_global_suite_cannot_override_failed_snapshot(self):
        name = SUITES[0]
        write(self.run_path.parent / name, dict(status='failed', tests=[dict(passed=False)]))
        write(self.project / 'Validation' / name, dict(status='passed', tests=[dict(passed=True)]))
        self.reject('Suite did not pass')

    def test_suite_status_cannot_hide_failed_check(self):
        write(self.run_path.parent / SUITES[0], dict(status='passed', tests=[dict(passed=False)]))
        self.reject('failed checks')

    def test_changed_verification_hash(self):
        self.report['apkSha256'] = '0' * 64
        write(self.report_path, self.report)
        self.reject('APK changed after verification')

    def test_bad_signature(self):
        self.report['signatureVerified'] = False
        write(self.report_path, self.report)
        self.reject('signature or architecture')

    def test_replaced_receipt(self):
        self.receipt['runId'] = 'other'
        write(self.project / 'Builds/Quest/run-build/receipt.json', self.receipt)
        self.reject('differs from the build receipt')


class ApkIdentityTests(unittest.TestCase):
    def setUp(self):
        self.fields = parse_badging("package: name='org.diwan.training.records' versionCode='8' versionName='0.4.4'\nnative-code: 'arm64-v8a'\n")

    def test_expected_package(self):
        check_identity(self.fields, '0.4.4')

    def test_wrong_package(self):
        self.fields['name'] = 'different.app'
        with self.assertRaises(ValueError): check_identity(self.fields, '0.4.4')

    def test_wrong_version_or_architecture(self):
        for key, value in [('versionCode', '7'), ('versionName', '0.4.3'), ('native-code', "'armeabi-v7a'")]:
            with self.subTest(key=key), self.assertRaises(ValueError):
                check_identity(dict(self.fields, **{key: value}), '0.4.4')


if __name__ == '__main__':
    unittest.main()
