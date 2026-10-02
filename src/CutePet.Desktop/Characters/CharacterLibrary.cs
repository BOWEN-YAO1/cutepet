using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace CutePet.Desktop;

internal sealed class CharacterLibrary
{
    public string Root { get; }
    private readonly List<CharacterPack> packs = new();
    public IReadOnlyList<CharacterPack> Packs => packs;
    public string? Warning { get; private set; }
    public CharacterLibrary(string root) { Root = Path.GetFullPath(root); Reload(); }
    public CharacterPack Find(string? id) => packs.FirstOrDefault(pack => pack.Id == id) ?? packs[0];
    public void Reload()
    {
        packs.Clear();
        packs.AddRange(CharacterCatalog.BuiltIns);
        Warning = null;
        try
        {
            if (!Directory.Exists(Root)) return;
            RejectReparse(Root);
            foreach (var directory in Directory.EnumerateDirectories(Root).Where(path => !Path.GetFileName(path).StartsWith('.')).Order())
            {
                if (packs.Count >= 34) { Warning = "只加载前 32 个自定义角色，请移除不需要的角色。"; break; }
                try
                {
                    RejectReparse(directory);
                    var pack = FromDirectory(directory);
                    if (pack.Id != Path.GetFileName(directory) || packs.Any(existing => existing.Id == pack.Id))
                        throw new InvalidDataException("角色编号重复或与目录不一致。");
                    packs.Add(pack);
                }
                catch (Exception ex) when (IsPackageError(ex)) { Warning = "部分自定义角色无法读取，已跳过；内置角色仍可使用。"; }
            }
        }
        catch (Exception ex) when (IsPackageError(ex)) { Warning = "自定义角色目录暂时无法读取，使用内置角色。"; }
    }
    public CharacterPack Import(string path)
    {
        if (packs.Count >= 34) throw new InvalidDataException("最多安装 32 个自定义角色，请先移除不需要的角色。");
        Directory.CreateDirectory(Root);
        RejectReparse(Root);
        var staging = Path.Combine(Root, ".import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            if (Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                using (var source = File.OpenRead(path))
                using (var destination = File.Create(Path.Combine(staging, "idle.png")))
                    CharacterPackLoader.CopyLimited(source, destination, 8 * 1024 * 1024);
                var displayName = Path.GetFileNameWithoutExtension(path);
                displayName = new string(displayName.Where(character => !char.IsControl(character)).Take(60).ToArray());
                var manifest = new CharacterManifest { Id = "custom-" + Guid.NewGuid().ToString("N"),
                    Name = string.IsNullOrWhiteSpace(displayName) ? "自定义角色" : displayName,
                    Actions = new() { ["idle"] = new() { Loop = true, Frames = new() { new("idle.png", 1000) } } } };
                File.WriteAllText(Path.Combine(staging, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
            }
            else Extract(path, staging);
            var pack = FromDirectory(staging);
            if (packs.Any(existing => existing.Id == pack.Id))
                throw new InvalidDataException("这个角色编号已经存在，请修改包中的 id 或先移除原角色。");
            var installed = Path.Combine(Root, pack.Id);
            Directory.Move(staging, installed);
            pack = pack with { Directory = installed };
            packs.Add(pack);
            return pack;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }
    }
    private static void Extract(string path, string directory)
    {
        using var file = File.OpenRead(path);
        if (file.Length > 64 * 1024 * 1024) throw new InvalidDataException("压缩包不能超过 64 MB。");
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        if (archive.Entries.Count > 256) throw new InvalidDataException("角色包最多包含 256 个条目。");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0, actualTotal = 0;
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName;
            var directoryEntry = name.EndsWith('/');
            var candidate = directoryEntry ? name.TrimEnd('/') : name;
            if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, "^[a-zA-Z0-9_-]+(?:/[a-zA-Z0-9_.-]+)*$|^[a-zA-Z0-9_.-]+$")
                || candidate.Any(char.IsControl) || candidate.Split('/').Any(segment => segment is "." or "..") || name.Length > 140
                || !seen.Add(candidate) || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000
                || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("角色包包含不合法或重复的路径。");
            if (directoryEntry) continue;
            var allowed = name == "character.json" || CharacterPackLoader.SafeFile(name)
                || name is "README.md" or "LICENSE.txt" or "LICENSE.md" or "SOURCE.md";
            if (!allowed) throw new InvalidDataException("角色包只支持角色配置、PNG 图片和素材说明文件。");
            var limit = name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? 8 * 1024 * 1024 : 64 * 1024;
            total += entry.Length;
            if (entry.Length > limit || total > 64 * 1024 * 1024) throw new InvalidDataException("解压后的角色包超过大小限制。");
            var target = Path.GetFullPath(Path.Combine(directory, name));
            if (!target.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("角色包路径超出目标目录。");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var source = entry.Open();
            using var destination = File.Create(target);
            CharacterPackLoader.CopyLimited(source, destination, limit);
            actualTotal += destination.Length;
            if (actualTotal > 64 * 1024 * 1024) throw new InvalidDataException("解压后的角色包超过大小限制。");
        }
    }
    public void Remove(CharacterPack pack)
    {
        if (pack.BuiltIn || pack.Directory is null) throw new InvalidDataException("内置角色不能移除。");
        var expected = Path.Combine(Root, pack.Id);
        if (!string.Equals(expected, pack.Directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("角色目录不匹配。");
        RejectReparse(Root);
        RejectReparse(expected);
        var trash = Path.Combine(Root, ".removed");
        Directory.CreateDirectory(trash);
        RejectReparse(trash);
        Directory.Move(expected, Path.Combine(trash, pack.Id + "-" + Guid.NewGuid().ToString("N")));
        packs.RemoveAll(item => item.Id == pack.Id);
    }
    public void Export(CharacterPack pack, string destination)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var config = archive.CreateEntry("character.json");
            using (var stream = config.Open()) JsonSerializer.Serialize(stream, pack.Manifest, CharacterPackLoader.Json);
            var imageNames = pack.Manifest.Actions.Values.SelectMany(action => action.Frames).Select(frame => frame.Image);
            if (pack.Manifest.Cloud is { } cloud) imageNames = imageNames.Append(cloud.Image);
            foreach (var name in imageNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                using var source = pack.BuiltIn ? Application.GetResourceStream(new Uri(
                    "pack://application:,,,/CutePet;component/Characters/Packs/" + pack.Id + "/" + name))!.Stream : OpenSafe(pack.Directory!, name);
                using var stream = archive.CreateEntry(name).Open();
                CharacterPackLoader.CopyLimited(source, stream, 8 * 1024 * 1024);
            }
            var copiedSource = false;
            var notices = pack.BuiltIn ? new[] { "LICENSE.txt", "SOURCE.md" }
                : new[] { "README.md", "LICENSE.txt", "LICENSE.md", "SOURCE.md" };
            foreach (var name in notices)
            {
                if (!pack.BuiltIn && !File.Exists(Path.Combine(pack.Directory!, name))) continue;
                using var source = pack.BuiltIn ? Application.GetResourceStream(new Uri(
                    "pack://application:,,,/CutePet;component/Characters/Packs/" + pack.Id + "/" + name))!.Stream
                    : OpenSafe(pack.Directory!, name);
                using var destinationStream = archive.CreateEntry(name).Open();
                CharacterPackLoader.CopyLimited(source, destinationStream, 64 * 1024);
                copiedSource |= name == "SOURCE.md";
            }
            if (!copiedSource)
            {
                using var writer = new StreamWriter(archive.CreateEntry("SOURCE.md").Open());
                writer.WriteLine($"# {pack.Name}\n\n作者：{pack.Manifest.Author}\n\n许可或权利说明：{pack.Manifest.License}");
            }
        }
        File.WriteAllBytes(destination, output.ToArray());
    }
    private static CharacterPack FromDirectory(string directory)
    {
        using var manifest = OpenSafe(directory, "character.json");
        return CharacterPackLoader.Load(manifest, name => OpenSafe(directory, name), builtIn: false, directory);
    }
    private static Stream OpenSafe(string directory, string name)
    {
        RejectReparse(directory);
        var path = directory;
        foreach (var segment in name.Split('/')) { path = Path.Combine(path, segment); RejectReparse(path); }
        return File.OpenRead(path);
    }
    private static void RejectReparse(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("角色资源不能使用链接或目录联接。");
    }
    internal static bool IsPackageError(Exception ex) => ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException
        or ArgumentException or NotSupportedException or System.Security.SecurityException or FileFormatException;
}
