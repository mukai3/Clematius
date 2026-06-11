using Clemoutis.Core;
using Clemoutis.Core.Config;
using Clemoutis.Core.Scroll;

namespace Clemoutis.Tests;

public class ModifierScrollResolverTests
{
    private sealed record Mods(bool Shift = false, bool Ctrl = false, bool Alt = false, bool Win = false)
        : IModifierState;

    private static ModifierScrollResolver Resolver(
        string ctrl = "none", string shift = "none", string ctrlShift = "none") =>
        new(new ModifierScrollSettings { Ctrl = ctrl, Shift = shift, CtrlShift = ctrlShift });

    [Fact]
    public void CtrlPressed_UsesCtrlBehavior()
    {
        Assert.Equal(WheelConversion.Horizontal,
            Resolver(ctrl: "horizontal").Resolve(new Mods(Ctrl: true)));
    }

    [Fact]
    public void ShiftPressed_UsesShiftBehavior()
    {
        Assert.Equal(WheelConversion.Horizontal,
            Resolver(shift: "horizontal").Resolve(new Mods(Shift: true)));
    }

    [Fact]
    public void CtrlShiftPressed_UsesCtrlShiftBehavior_NotCtrl()
    {
        // Ctrl+Shift は単独 Ctrl とは別スロット。Ctrl=horizontal でも CtrlShift が優先される。
        var r = Resolver(ctrl: "horizontal", ctrlShift: "none");
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Ctrl: true, Shift: true)));
    }

    [Fact]
    public void CtrlShiftPressed_ResolvesCtrlShiftHorizontal()
    {
        Assert.Equal(WheelConversion.Horizontal,
            Resolver(ctrlShift: "horizontal").Resolve(new Mods(Ctrl: true, Shift: true)));
    }

    [Fact]
    public void NoModifier_ReturnsNone()
    {
        Assert.Equal(WheelConversion.None, Resolver(ctrl: "horizontal").Resolve(new Mods()));
    }

    [Fact]
    public void AltIsIgnored()
    {
        // Alt はオリジナルに無いので対象外
        Assert.Equal(WheelConversion.None, Resolver(ctrl: "horizontal").Resolve(new Mods(Alt: true)));
    }

    [Fact]
    public void DefaultSettings_AllNone()
    {
        var r = new ModifierScrollResolver(new ModifierScrollSettings());
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Ctrl: true)));
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Shift: true)));
        Assert.Equal(WheelConversion.None, r.Resolve(new Mods(Ctrl: true, Shift: true)));
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
