namespace Clemoutis.Core.Config;

/// <summary>スクロール拡張の設定。既定値はユーザーの Kazaguru.ini 由来。</summary>
public sealed record ScrollSettings
{
    public int Sensitivity { get; init; } = 3;
    public int Acceleration { get; init; } = 3;
    public bool AcceleratedScroll { get; init; }
    public bool ScrollAlways { get; init; }
    public bool HorizontalOnScrollbar { get; init; } = true;
    public int MergeWheelDelta { get; init; } = 2;
    public int WheelResolution { get; init; } = 1;
    public int AutoWheelResolution { get; init; } = 3;

    /// <summary>
    /// 修飾キー押下中のホイール挙動。オリジナルと同じく Ctrl / Shift / Ctrl+Shift の
    /// 3スロットを持つ（Alt は対象外）。値は behavior 文字列（"none" / "horizontal" 等）。
    /// 既定はユーザー ini の ScrollExCtrl/Shift/CtrlShift がいずれも 0 のため "none"。
    /// </summary>
    public ModifierScrollSettings ModifierScroll { get; init; } = new();
}

/// <summary>Ctrl / Shift / Ctrl+Shift それぞれのホイール挙動。</summary>
public sealed record ModifierScrollSettings
{
    public string Ctrl { get; init; } = "none";
    public string Shift { get; init; } = "none";
    public string CtrlShift { get; init; } = "none";
}
