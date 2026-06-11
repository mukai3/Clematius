namespace Clemoutis.Settings;

/// <summary>
/// スクロール挙動の値（"none"/"horizontal"/"code:NN"）と表示名の対応。
/// v1 で実装済みの挙動のみ選択肢に出し、未確定コードは「未確定」として保持・表示する。
/// </summary>
internal static class ScrollBehaviorChoice
{
    public const string None = "none";
    public const string Horizontal = "horizontal";

    public static string Display(string value) => value switch
    {
        None => "なし",
        Horizontal => "水平スクロール",
        _ when value.StartsWith("code:", StringComparison.OrdinalIgnoreCase)
            => $"未確定（{value}）",
        _ => value,
    };

    /// <summary>コンボに出す選択肢。既存値が未知コードならそれも保持して含める。</summary>
    public static string[] ChoicesIncluding(string currentValue)
    {
        var list = new List<string> { None, Horizontal };
        if (!list.Contains(currentValue, StringComparer.OrdinalIgnoreCase))
            list.Add(currentValue); // 未確定コード等を温存
        return list.ToArray();
    }
}
