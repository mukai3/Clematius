using Clemotius.Core.Actions;
using Clemotius.Core.Gestures;

namespace Clemotius.Core.Config;

/// <summary>
/// アプリ全体の設定ルート。既定値はユーザーの Kazaguru.ini からデコードした
/// ジェスチャー割り当てと設定値で構成する（使用感の踏襲）。
/// </summary>
public sealed record ClemotiusConfig
{
    public GestureSettings Gesture { get; init; } = new();
    public ScrollSettings Scroll { get; init; } = new();
    public TitlebarSettings Titlebar { get; init; } = new();
    public TraySettings Tray { get; init; } = new();
    public IReadOnlyList<GestureProfile> Profiles { get; init; } = new[] { DefaultProfile(), DefaultBrowserProfile() };

    /// <summary>
    /// 全アプリ共通のグローバルプロファイル。あらゆるアプリで使える「戻る/進む」のみを持つ。
    /// ブラウザ専用の操作は <see cref="DefaultBrowserProfile"/> 側に分離する。
    /// </summary>
    public static GestureProfile DefaultProfile() => new()
    {
        Name = "Default",
        ProcessPattern = "*",
        GesturesEnabled = true,
        Gestures = new[]
        {
            new GestureBinding("L", new AppCommandAction(AppCommand.BrowserBackward)),  // 戻る
            new GestureBinding("R", new AppCommandAction(AppCommand.BrowserForward)),   // 進む
        },
    };

    /// <summary>
    /// 既定の Web ブラウザ用プロファイル（chrome / msedge）。タブ閉じ・再読込・先頭/末尾移動・
    /// 右ボタン+ホイールでのタブ切替を持つ。戻る/進むはグローバルから継承される。
    /// </summary>
    public static GestureProfile DefaultBrowserProfile() => new()
    {
        Name = "ブラウザ",
        ProcessPattern = "chrome, msedge",
        GesturesEnabled = true,
        Gestures = new[]
        {
            new GestureBinding("DR", new KeyAction(KeyStrokeParser.Parse("Ctrl+W"))),
            new GestureBinding("UDU", new KeyAction(KeyStrokeParser.Parse("Ctrl+F5"))),
            new GestureBinding("DUD", new KeyAction(KeyStrokeParser.Parse("Ctrl+F5"))),
            new GestureBinding("LU", new KeyAction(KeyStrokeParser.Parse("Ctrl+Home"))),
            new GestureBinding("RU", new KeyAction(KeyStrokeParser.Parse("Ctrl+Home"))),
            new GestureBinding("LD", new KeyAction(KeyStrokeParser.Parse("Ctrl+End"))),
            new GestureBinding("RD", new KeyAction(KeyStrokeParser.Parse("Ctrl+End"))),
        },
        // ユーザー ini の R+WU / R+WD（右ボタン+ホイール）由来
        WheelUp = new KeyAction(KeyStrokeParser.Parse("Ctrl+Shift+Tab")),   // 前のタブ
        WheelDown = new KeyAction(KeyStrokeParser.Parse("Ctrl+Tab")),       // 次のタブ
    };

    public static ClemotiusConfig CreateDefault() => new();
}
