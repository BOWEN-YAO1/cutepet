"""Package a verified Release build. Standard library only; no bitmap processing."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

RUNTIME_FILES = ('CutePet.exe', 'CutePet.dll', 'CutePet.Core.dll', 'CutePet.Codex.dll',
                 'CutePet.deps.json', 'CutePet.runtimeconfig.json')


def zip_directory(directory, destination):
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(directory.rglob('*')):
            if path.is_file():
                archive.write(path, path.relative_to(directory).as_posix())


def checksum(path):
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_name(path.name + '.sha256').write_text(digest + '  ' + path.name + '\n', encoding='utf-8')
    return digest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish-dir', type=Path, required=True)
    parser.add_argument('--verification-dir', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parent.parent
    version = ET.parse(repo/'src/CutePet.Desktop/CutePet.Desktop.csproj').findtext('.//Version')
    git = ['git', '-c', 'safe.directory=' + repo.as_posix(), '-C', str(repo)]
    if subprocess.check_output(git + ['status', '--porcelain'], text=True).strip():
        raise RuntimeError('Commit the release changes first so the attached GPL source matches this build.')
    sha = subprocess.check_output(git + ['rev-parse', 'HEAD'], text=True).strip()
    report = json.loads((args.verification_dir/'verification.json').read_text(encoding='utf-8-sig'))
    if not report['passed'] or not report['checks']:
        raise RuntimeError('The WPF verification report must pass before packaging.')
    args.output_dir.mkdir(parents=True, exist_ok=True)
    source = args.output_dir/f'CutePet-Source-v{version}.zip'
    subprocess.run(git + ['archive', '--format=zip', '--output=' + str(source.resolve()), 'HEAD'], check=True)

    with tempfile.TemporaryDirectory(prefix=f'cutepet-v{version}-', dir=args.output_dir) as temp:
        stage = Path(temp)/'desktop'; stage.mkdir()
        for name in RUNTIME_FILES:
            shutil.copy2(args.publish_dir/name, stage/name)
        for name in ('characters', 'previews/interface', 'previews/animations', 'previews/dialogue', 'licenses', 'source'):
            (stage/name).mkdir(parents=True, exist_ok=True)
        shutil.copytree(repo/'docs', stage/'docs')
        for name in ('LICENSE', 'THIRD_PARTY_NOTICES.md'):
            shutil.copy2(repo/name, stage/'licenses'/name)
        shutil.copy2(source, stage/'source'/source.name)
        (stage/'source/SOURCE.txt').write_text(
            f'CutePet {version}\nRepository: https://github.com/BOWEN-YAO1/cutepet\nBranch: feat/desktop-shell\n'
            f'Commit: {sha}\nMatching GPL v3 source: {source.name}\nSHA256: {checksum(source)}\n', encoding='utf-8')
        for name in ('app-overview.png', 'app-characters.png', 'app-activity.png', 'app-settings.png', 'app-dialogue-settings.png'):
            shutil.copy2(args.verification_dir/name, stage/'previews/interface'/name)
        for name in ('side-edge-actions.gif', 'side-edge-contact-sheet.png', 'side-144-stages.png', 'bottom-edge-actions.gif',
                     'bottom-edge-contact-sheet.png', 'top-edge-actions.gif', 'top-edge-contact-sheet.png', 'top-drawn-keyframes.gif', 'side-drawn-keyframes.gif', 'cloud-landing.gif',
                     'pose-sequences.gif', 'pose-contact-sheet.png', 'throne-motion.gif', 'seated-motion.gif'):
            shutil.copy2(args.verification_dir/name, stage/'previews/animations'/name)
        shutil.copy2(args.verification_dir/'dialogue-preview.png', stage/'previews/dialogue/dialogue-preview.png')
        zip_directory(repo/'examples/character-pack', stage/'characters/角色包示例.zip')
        for role, title, count in [('tianyi', '天依-台词互动', 60), ('cat', '小猫-台词互动', 32)]:
            pack = repo/'src/CutePet.Desktop/Characters/Packs'/role
            manifest = json.loads((pack/'character.json').read_text(encoding='utf-8-sig'))
            assert sum(map(len, manifest['dialogue'].values())) == count
            files = {frame['image'] for action in manifest['actions'].values() for frame in action['frames']}
            if manifest.get('cloud'): files.add(manifest['cloud']['image'])
            for name in ('ornament', 'scenery'):
                if manifest.get('topSwing', {}).get(name): files.add(manifest['topSwing'][name]['image'])
            variant = Path(temp)/role; variant.mkdir()
            for name in sorted(files):
                destination = variant/name; destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(pack/name, destination)
            for name in ('SOURCE.md', 'LICENSE.txt'): shutil.copy2(pack/name, variant/name)
            manifest['id'] = role + '-dialogue-v' + version.replace('.', '')
            manifest['name'] = ('洛天依' if role == 'tianyi' else '小猫') + '（台词互动）'
            (variant/'character.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
            role_zip = args.output_dir/f'{title}-v{version}.cutepet.zip'
            zip_directory(variant, role_zip); checksum(role_zip)
            shutil.copy2(role_zip, stage/'characters'/role_zip.name)
        guide = f'''# CutePet {version}

先退出旧版，再将此 ZIP 完整解压到一个新目录，双击 CutePet.exe 启动。
需要 Windows x64 和 .NET 8 Desktop Runtime；运行文件请保持在同一个目录。
原有设置和已导入的角色包继续沿用，无需删除设置。以前导入的角色不会被自动覆盖。

这版新增112张侧边过渡绘图：144级探出、16张点头、16张轻摇，侧边共176张不同姿势。
左右共用绘图并镜像，缩回使用原路倒放，扶边点固定。
previews/animations/side-drawn-keyframes.gif直接播放原绘图；side-edge-actions.gif展示左右窗口效果。
上侧继续保留64张绘图：闭眼、向左张望、向右张望、微笑歪头各16张。
天依共354个活跃绘图、28个动作。固定座椅与悬挂点，保留花藤绳饰，动作沿原路倒放回收。
previews/animations/top-drawn-keyframes.gif直接播放原绘图，不经过图片混合；top-edge-actions.gif展示实际窗口效果。
新天依角色包需应用0.44.0。
回应开始、结束和待机共用同一姿势，头部配准后的过渡不会拖动秋千板。
侧边统一头部尺寸，头部短过渡在上方手掌前归零，两只手保持扶边；原始 PNG 不修改。
运动段每帧 40 毫秒，目标每秒 25 个源帧时间段，并随屏幕绘制在相邻帧之间做短过渡。
保留侧边探头、缩回再探出、轻摇和点头。回收共用逆序，停留重复引用原图，不计作新增绘图。
帧间过渡为像素混合，复杂结构仍可能有轻微重影；发丝、衣纹和表情仍有 AI 绘图差异。
请选择内置天依体验新版；此前导入的角色包保留原动作，可另行导入本版 characters 内的天依包。
乘云和自动休息继续统一轮换，坐姿和贴边自动动作减少重复。
点击时立即回应，云朵与浮动姿态柔和收回。拖动、锁定、隐藏、角色切换和低额度仍能立即中止移动。
保留天依 60 句、小猫 32 句情境台词；点击、悬停、拖放、乘云、王座和四边互动分别回应。
气泡约 4–8 秒后自动收起，额度只在状态变化时提醒。设置 → 交流可选择安静 / 普通 / 活泼，或关闭主动台词。
需要体验新台词请选择内置角色，也可导入 characters 内的新角色包。

- characters/：天依、小猫台词互动包与自定义角色示例。
- previews/：主界面图片、站立 / 王座 / 坐姿 / 四边动作 GIF、收云与台词预览，按用途归档。
- docs/：功能、角色包格式、开发和验证文档。
- licenses/：GPL v3 与素材权利说明。
- source/：与此版本对应的源码 ZIP、提交编号与校验值。

已通过 {len(report['checks']):,} 项隔离窗口与功能检查。预览使用演示额度，不连接真实账号。
测试不能代替实际多屏、任务栏和长期使用体验。

主界面关闭后桌宠继续运行，双击托盘图标可再打开；退出请在主界面或托盘中选择退出。
源码项目：https://github.com/BOWEN-YAO1/cutepet
'''
        (stage/'使用说明.md').write_text(guide, encoding='utf-8')
        (args.output_dir/f'CutePet-v{version}-使用说明.md').write_text(guide, encoding='utf-8')
        package = args.output_dir/f'CutePet-Desktop-v{version}-win-x64.zip'
        zip_directory(stage, package)
        digest = checksum(package)
        print(json.dumps({'package': str(package), 'version': version, 'commit': sha, 'checks': len(report['checks']),
                          'bytes': package.stat().st_size, 'sha256': digest}, ensure_ascii=False))


if __name__ == '__main__':
    main()
