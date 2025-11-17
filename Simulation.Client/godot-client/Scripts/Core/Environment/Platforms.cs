using System;
using Godot;

namespace GodotClient.Scripts.Core.Environment;


public static class EnvironmentSettings
{
    public static EnvironmentPlatforms CurrentPlatform => GetCurrentPlatform();
    
    private static EnvironmentPlatforms GetCurrentPlatform()
    {
        return OS.GetName() switch
        {
            "Windows" => EnvironmentPlatforms.Windows,
            "X11" => EnvironmentPlatforms.Linux,
            "OSX" => EnvironmentPlatforms.macOS,
            "iOS" => EnvironmentPlatforms.iOS,
            "Android" => EnvironmentPlatforms.Android,
            "HTML5" => EnvironmentPlatforms.Web,
            "FreeBSD" => EnvironmentPlatforms.FreeBSD,
            "NetBSD" => EnvironmentPlatforms.NetBSD,
            "OpenBSD" => EnvironmentPlatforms.OpenBSD,
            _ => EnvironmentPlatforms.None
        };
    }
}

[Flags]
public enum EnvironmentPlatforms
{
    None,
    Windows,
    macOS,
    Linux,
    FreeBSD,
    NetBSD,
    OpenBSD,
    BSD,
    iOS,
    Android,
    Web,
}