using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace CutePet.Desktop;

public readonly record struct StartupState(bool Enabled, string? Error = null);

public interface IStartupRegistration
{
    StartupState Read();
    StartupState SetEnabled(bool enabled);
}

internal interface IStartupCommandStore
{
    string? Read();
    void Write(string command);
    void Remove();
}

internal sealed class StartupRegistration(IStartupCommandStore store, string executable,
    Func<string, bool>? executableExists = null) : IStartupRegistration
{
    public static string CommandFor(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || executable.IndexOfAny(new[] { '"', '\r', '\n', '\0' }) >= 0
            || !Path.IsPathFullyQualified(executable) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("An absolute executable path is required", nameof(executable));
        var command = "\"" + Path.GetFullPath(executable) + "\" --autostart";
        if (command.Length > 260) throw new ArgumentException("Startup command is too long", nameof(executable));
        return command;
    }

    public StartupState Read()
    {
        try { return new(!string.IsNullOrWhiteSpace(store.Read())); }
        catch (Exception ex) when (IsExpected(ex)) { return new(false, "无法读取开机启动项"); }
    }

    public StartupState SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                if (!(executableExists ?? File.Exists)(executable))
                    return new(Read().Enabled, "找不到 CutePet.exe，无法设置开机启动");
                store.Write(CommandFor(executable));
            }
            else store.Remove();
            var state = Read();
            return state.Error is not null || state.Enabled == enabled ? state : new(state.Enabled, "开机启动项未保存");
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            var state = Read();
            return new(state.Enabled, "无法修改开机启动项");
        }
    }

    private static bool IsExpected(Exception ex) => ex is IOException or UnauthorizedAccessException or SecurityException or ArgumentException;

    public static IStartupRegistration ForCurrentApp() => new StartupRegistration(
        new RegistryStartupStore(), Path.Combine(AppContext.BaseDirectory, "CutePet.exe"));
}

internal sealed class RegistryStartupStore : IStartupCommandStore
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CutePet";
    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(ValueName) as string;
    }
    public void Write(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }
    public void Remove()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

// Verification never accesses the user's registry. It also covers failed writes.
internal sealed class MemoryStartupStore : IStartupCommandStore
{
    public string? Command { get; set; }
    public int Writes { get; private set; }
    public bool RejectWrites { get; set; }
    public string? Read() => Command;
    public void Write(string command) { Guard(); Command = command; Writes++; }
    public void Remove() { Guard(); Command = null; Writes++; }
    private void Guard() { if (RejectWrites) throw new UnauthorizedAccessException(); }
}
