namespace Clemoutis.Core.Scroll;

/// <summary>
/// 設定 behavior 文字列を <see cref="WheelConversion"/> に変換する。
/// 既知のキーワードのみ解釈し、未確定のコード値（"code:55" 等）は None に倒す。
/// それらコードの意味は動的解析（設計書 D.4）で確定後にここへ追加する。
/// </summary>
public static class ScrollBehaviorParser
{
    public static WheelConversion Parse(string? behavior)
    {
        if (string.IsNullOrWhiteSpace(behavior))
            return WheelConversion.None;

        return behavior.Trim().ToLowerInvariant() switch
        {
            "horizontal" => WheelConversion.Horizontal,
            "none" or "passthrough" => WheelConversion.None,
            // "code:NN" や未知の文字列は、意味が確定するまで素通し
            _ => WheelConversion.None,
        };
    }
}
