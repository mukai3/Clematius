using System.IO;
using Clematius.Core.Config;

namespace Clematius.Tests;

public class ConfigLocatorTests
{
    private const string ExeDir = @"C:\Apps\Clematius";
    private const string AppData = @"C:\Users\u\AppData\Roaming";

    [Fact]
    public void Portable_UsesExeFolder()
    {
        var loc = ConfigLocator.Resolve(ExeDir, AppData, portableConfigExists: true);
        Assert.True(loc.Portable);
        Assert.Equal(ExeDir, loc.Dir);
        Assert.Equal(Path.Combine(ExeDir, "config.json"), loc.Path);
    }

    [Fact]
    public void NonPortable_UsesAppDataClematiusFolder()
    {
        var loc = ConfigLocator.Resolve(ExeDir, AppData, portableConfigExists: false);
        Assert.False(loc.Portable);
        Assert.Equal(Path.Combine(AppData, "Clematius"), loc.Dir);
        Assert.Equal(Path.Combine(AppData, "Clematius", "config.json"), loc.Path);
    }
}
