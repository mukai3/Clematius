using Clematius.Core.Actions;
using Clematius.Core.Config;
using Clematius.Core.Gestures;

namespace Clematius.Tests;

public class ProfileResolverTests
{
    private static GestureProfile P(string name, string pattern) =>
        new() { Name = name, ProcessPattern = pattern };

    [Fact]
    public void Resolve_MatchingProcess_ReturnsProfile()
    {
        var r = new ProfileResolver(new[] { P("Chrome", "chrome") });
        Assert.Equal("Chrome", r.Resolve("chrome")!.Name);
    }

    [Fact]
    public void Resolve_IgnoresExeExtension()
    {
        var r = new ProfileResolver(new[] { P("Chrome", "chrome.exe") });
        Assert.Equal("Chrome", r.Resolve("chrome")!.Name);
        Assert.Equal("Chrome", r.Resolve("chrome.exe")!.Name);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var r = new ProfileResolver(new[] { P("Chrome", "Chrome") });
        Assert.Equal("Chrome", r.Resolve("CHROME")!.Name);
    }

    [Fact]
    public void Resolve_NoMatch_ReturnsNull()
    {
        var r = new ProfileResolver(new[] { P("Chrome", "chrome") });
        Assert.Null(r.Resolve("notepad"));
    }

    [Fact]
    public void Resolve_FirstMatchWins()
    {
        var r = new ProfileResolver(new[] { P("A", "chrome"), P("B", "chrome") });
        Assert.Equal("A", r.Resolve("chrome")!.Name);
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("edge")]
    [InlineData("brave.exe")]
    public void Resolve_CommaSeparatedPatterns_MatchAny(string process)
    {
        var r = new ProfileResolver(new[] { P("Browsers", "chrome.exe, edge.exe, brave.exe") });
        Assert.Equal("Browsers", r.Resolve(process)!.Name);
    }

    [Fact]
    public void Resolve_CommaSeparated_NoMatch_ReturnsNull()
    {
        var r = new ProfileResolver(new[] { P("Browsers", "chrome, edge, brave") });
        Assert.Null(r.Resolve("notepad"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyProcess_ReturnsNull(string? process)
    {
        var r = new ProfileResolver(new[] { P("Chrome", "chrome") });
        Assert.Null(r.Resolve(process));
    }

    [Fact]
    public void Resolve_WildcardPattern_DoesNotMatch()
    {
        // "*" は廃止。リテラル "*" はどのプロセス名にも一致しない。
        var r = new ProfileResolver(new[] { P("Legacy", "*") });
        Assert.Null(r.Resolve("notepad"));
        Assert.Null(r.Resolve("chrome"));
    }

    [Fact]
    public void Resolve_EmptyPattern_DoesNotMatch()
    {
        var r = new ProfileResolver(new[] { P("Unassigned", "") });
        Assert.Null(r.Resolve("chrome"));
    }
}
