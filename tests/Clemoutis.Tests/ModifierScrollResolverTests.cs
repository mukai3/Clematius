using Clemoutis.Core;
using Clemoutis.Core.Config;
using Clemoutis.Core.Scroll;

namespace Clemoutis.Tests;

public class ModifierScrollResolverTests
{
    private sealed record Mods(bool Shift = false, bool Ctrl = false, bool Alt = false, bool Win = false)
        : IModifierState;

    [Fact]
    public void AltPressed_WithHorizontalRule_ReturnsHorizontal()
    {
        var r = new ModifierScrollResolver(new[] { new ModifierScrollRule("Alt", "horizontal") });
        Assert.Equal(WheelConversion.Horizontal, r.Resolve(new Mods(Alt: true)));
    }

    [Fact]
    public void AltNotPressed_ReturnsNone()
    {
        var r = new ModifierScrollResolver(new[] { new ModifierScrollRule("Alt", "horizontal") });
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods()));
    }

    [Fact]
    public void UnknownCodeBehavior_FallsBackToNone()
    {
        // ユーザー ini 由来の "code:55"（意味未確定）は素通し
        var r = new ModifierScrollResolver(new[] { new ModifierScrollRule("Alt", "code:55") });
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Alt: true)));
    }

    [Fact]
    public void FirstMatchingRuleWins()
    {
        var r = new ModifierScrollResolver(new[]
        {
            new ModifierScrollRule("Shift", "horizontal"),
            new ModifierScrollRule("Ctrl", "none"),
        });
        Assert.Equal(WheelConversion.Horizontal, r.Resolve(new Mods(Shift: true, Ctrl: true)));
    }

    [Fact]
    public void NoRules_ReturnsNone()
    {
        var r = new ModifierScrollResolver(Array.Empty<ModifierScrollRule>());
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Alt: true)));
    }

    [Theory]
    [InlineData("shift")]
    [InlineData("SHIFT")]
    [InlineData("Shift")]
    public void ModifierName_IsCaseInsensitive(string name)
    {
        var r = new ModifierScrollResolver(new[] { new ModifierScrollRule(name, "horizontal") });
        Assert.Equal(WheelConversion.Horizontal, r.Resolve(new Mods(Shift: true)));
    }
}

public class ScrollBehaviorParserTests
{
    [Theory]
    [InlineData("horizontal", WheelConversion.Horizontal)]
    [InlineData("Horizontal", WheelConversion.Horizontal)]
    [InlineData("none", WheelConversion.None)]
    [InlineData("passthrough", WheelConversion.None)]
    [InlineData("code:55", WheelConversion.None)]
    [InlineData("", WheelConversion.None)]
    [InlineData(null, WheelConversion.None)]
    public void Parse_MapsBehaviors(string? behavior, WheelConversion expected)
    {
        Assert.Equal(expected, ScrollBehaviorParser.Parse(behavior));
    }
}
