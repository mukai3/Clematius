namespace Clemoutis.Core.Scroll;

/// <summary>ホイールイベントに適用する変換の種類。</summary>
public enum WheelConversion
{
    /// <summary>変換なし（素通し）。</summary>
    None,

    /// <summary>縦ホイールを水平スクロールに変換する。</summary>
    Horizontal,
}
