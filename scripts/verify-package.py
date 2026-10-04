"""Check published ZIP layout, character contents and its matching source archive."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import io
import json
import zipfile


def check_paths(archive):
    names = archive.namelist()
    assert len(names) == len(set(names)), 'Duplicate ZIP entries'
    for name in names:
        path = PurePosixPath(name)
        assert not path.is_absolute() and '..' not in path.parts and ':' not in name and '\\' not in name, name
    assert archive.testzip() is None, 'Damaged ZIP entry'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('--expected-commit', required=True)
    args = parser.parse_args()
    checks = []
    with zipfile.ZipFile(args.package) as archive:
        check_paths(archive); checks.append('ZIP integrity and safe unique paths')
        names = archive.namelist()
        expected_root = {'CutePet.exe', 'CutePet.dll', 'CutePet.Core.dll', 'CutePet.Codex.dll',
                         'CutePet.deps.json', 'CutePet.runtimeconfig.json', '使用说明.md'}
        assert {name for name in names if '/' not in name} == expected_root
        groups = {'characters', 'previews', 'docs', 'licenses', 'source'}
        assert {name.split('/')[0] for name in names if '/' in name} == groups
        assert not any(PurePosixPath(name).suffix.lower() in {'.png','.gif','.ico','.zip'} for name in expected_root)
        checks.append('Only executable, required runtime files and guide at root; five grouped folders')
        assert {'licenses/LICENSE', 'licenses/THIRD_PARTY_NOTICES.md', 'source/SOURCE.txt'}.issubset(names)
        source_info = archive.read('source/SOURCE.txt').decode('utf-8').replace('\r\n', '\n')
        assert 'Commit: ' + args.expected_commit + '\n' in source_info
        source_name = next(name for name in names if name.startswith('source/') and name.endswith('.zip'))
        source_bytes = archive.read(source_name)
        assert 'SHA256: '+hashlib.sha256(source_bytes).hexdigest()+'\n' in source_info
        with zipfile.ZipFile(io.BytesIO(source_bytes)) as source:
            check_paths(source)
            for role, count in [('tianyi',60), ('cat',32)]:
                name = next(name for name in names if name.startswith('characters/') and name.endswith('.cutepet.zip')
                            and name.split('/')[-1].startswith('天依' if role == 'tianyi' else '小猫'))
                with zipfile.ZipFile(io.BytesIO(archive.read(name))) as pack:
                    check_paths(pack)
                    manifest = json.loads(pack.read('character.json'))
                    built_in = json.loads(source.read(f'src/CutePet.Desktop/Characters/Packs/{role}/character.json'))
                    assert manifest['dialogue'] == built_in['dialogue'] and sum(map(len,manifest['dialogue'].values())) == count
                    assert manifest['actions'] == built_in['actions']
                    active = {frame['image'] for action in manifest['actions'].values() for frame in action['frames']}
                    if manifest.get('cloud'): active.add(manifest['cloud']['image'])
                    for key in ('ornament', 'scenery'):
                        if manifest.get('topSwing', {}).get(key): active.add(manifest['topSwing'][key]['image'])
                    assert active == {name for name in pack.namelist() if name.endswith('.png')}
                    for image in active:
                        assert pack.read(image) == source.read(f'src/CutePet.Desktop/Characters/Packs/{role}/{image}')
                    assert {'LICENSE.txt','SOURCE.md'}.issubset(pack.namelist())
                checks.append(f'{role}: portable dialogue and every active sprite match attached source')
        checks.append('License, exact commit and matching source checksum attached')
        assert {'previews/interface/app-dialogue-settings.png', 'previews/dialogue/dialogue-preview.png'}.issubset(names)
        assert {name for name in names if name.startswith('previews/animations/') and name.endswith('.gif')} == {
            'previews/animations/'+name for name in ('side-edge-actions.gif','bottom-edge-actions.gif','top-edge-actions.gif','cloud-landing.gif')}
        checks.append('Fresh interface, dialogue, three edge animations and soft landing grouped correctly')
    digest = hashlib.sha256(args.package.read_bytes()).hexdigest()
    assert args.package.with_name(args.package.name+'.sha256').read_text().split()[0] == digest
    print(json.dumps({'passed': True, 'checks': checks, 'sha256': digest}, ensure_ascii=False))


if __name__ == '__main__':
    main()
