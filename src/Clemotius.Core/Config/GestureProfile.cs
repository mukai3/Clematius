using Clemotius.Core.Actions;
using Clemotius.Core.Gestures;

namespace Clemotius.Core.Config;

/// <summary>
/// アプリ単位のプロファイル。前面プロセス名のパターンで適用先を決める。
/// パターンに一致するアプリでのみジェスチャーが有効になる（グローバル既定は持たない）。
/// </summary>
public sealed record GestureProfile
{
    public string Name { get; init; } = "Default";

    /// <summary>
    /// 適用対象のプロセス名（拡張子なし可、カンマ区切りで複数可）。
    /// 空ならどのアプリにも一致しない（プロファイルは事実上無効）。
    /// </summary>
    public string ProcessPattern { get; init; } = "";

    public bool GesturesEnabled { get; init; } = true;

    public IReadOnlyList<GestureBinding> Gestures { get; init; } = Array.Empty<GestureBinding>();

    /// <summary>右ボタン押下中にホイールを上回転したときのアクション。null なら無し。</summary>
    public GestureAction? WheelUp { get; init; }

    /// <summary>右ボタン押下中にホイールを下回転したときのアクション。null なら無し。</summary>
    public GestureAction? WheelDown { get; init; }
}
