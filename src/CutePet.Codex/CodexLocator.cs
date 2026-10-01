namespace CutePet.Codex;

public static class CodexLocator
{
    public static string Find(string? explicitPath = null)
    {
        if (explicitPath is not null)
        {
            var full = Path.GetFullPath(explicitPath);
            if (File.Exists(full)) return Validate(full);
            throw Missing();
        }
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var path = Path.Combine(directory.Trim('"'), OperatingSystem.IsWindows() ? "codex.exe" : "codex");
            if (File.Exists(path)) return Validate(path);
        }
        // The observed Windows desktop installation may not be on the user's PATH.
        if (OperatingSystem.IsWindows())
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenAI", "Codex", "bin");
            try
            {
                if (Directory.Exists(root))
                {
                    var installed = Directory.EnumerateDirectories(root)
                        .Select(d => new FileInfo(Path.Combine(d, "codex.exe")))
                        .Where(f => f.Exists).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
                    if (installed is not null) return installed.FullName;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        throw Missing();
    }

    private static string Validate(string path)
    {
        // Direct executable only: never route a user-controlled path through cmd.exe.
        if (OperatingSystem.IsWindows() && !Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            throw new QuotaException(QuotaFailure.MissingDependency,
                "此原型需要原生 codex.exe；请用 --codex 指定路径，暂不支持 .cmd/.ps1 启动器。");
        return path;
    }

    private static QuotaException Missing() => new(QuotaFailure.MissingDependency,
        "未找到 Codex 可执行文件。请安装兼容 CLI，或使用 --codex 指定完整路径。");
}
