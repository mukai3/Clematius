namespace Clemotius.Core.Scroll;

/// <summary>スクロールバー帯判定の結果。</summary>
public enum BandHit
{
    None,
    Horizontal,
    Vertical,
}

/// <summary>
/// 「スクロール可能要素の端の帯（スクロールバー相当領域）にカーソルがあるか」の
/// ピュアロジック。Chromium 等のカスタム描画スクロールバーは UIA に ScrollBar
/// 要素として公開されないため、ScrollPattern を持つ要素の右端/下端の帯で代用する。
/// </summary>
public static class ScrollBarBand
{
    /// <summary>帯幅に足す余裕(px)。オーバーレイ型の細いスクロールバーを拾いやすくする。</summary>
    public const int Margin = 4;

    /// <param name="x">カーソルX（スクリーン座標）</param>
    /// <param name="y">カーソルY（スクリーン座標）</param>
    /// <param name="left">要素の左端</param>
    /// <param name="top">要素の上端</param>
    /// <param name="width">要素の幅</param>
    /// <param name="height">要素の高さ</param>
    /// <param name="verticallyScrollable">垂直スクロール可能か</param>
    /// <param name="horizontallyScrollable">水平スクロール可能か</param>
    /// <param name="vBarWidth">垂直スクロールバー幅（SM_CXVSCROLL）</param>
    /// <param name="hBarHeight">水平スクロールバー高（SM_CYHSCROLL）</param>
    public static BandHit Hit(
        int x, int y,
        int left, int top, int width, int height,
        bool verticallyScrollable, bool horizontallyScrollable,
        int vBarWidth, int hBarHeight)
    {
        if (width <= 0 || height <= 0)
            return BandHit.None;
        if (x < left || x >= left + width || y < top || y >= top + height)
            return BandHit.None;

        // 垂直を優先（右下隅は垂直扱い。両方可の場合の慣例に合わせる）
        if (verticallyScrollable && x >= left + width - (vBarWidth + Margin))
            return BandHit.Vertical;
        if (horizontallyScrollable && y >= top + height - (hBarHeight + Margin))
            return BandHit.Horizontal;
        return BandHit.None;
    }

    /// <summary>
    /// 窓矩形の右端/下端の「スクロールバーがありうる帯」にカーソルがあるかを返す（ジオメトリのみ）。
    /// 大窓（Chromium 等、コンテンツとバーが同一 HWND）で先読み（Prime）の対象にするかの判定に使う。
    /// スクロール可能かは判定できないため候補軸のみ返す（右端→Vertical 候補、下端→Horizontal 候補、
    /// 右下隅は <see cref="Hit"/> と同じく Vertical 優先）。実際にスクロールバーかの確定は UIA 検出で行う。
    /// </summary>
    /// <param name="x">カーソルX（スクリーン座標）</param>
    /// <param name="y">カーソルY（スクリーン座標）</param>
    /// <param name="left">窓の左端</param>
    /// <param name="top">窓の上端</param>
    /// <param name="width">窓の幅</param>
    /// <param name="height">窓の高さ</param>
    /// <param name="vBarWidth">垂直スクロールバー幅（SM_CXVSCROLL）</param>
    /// <param name="hBarHeight">水平スクロールバー高（SM_CYHSCROLL）</param>
    public static BandHit EdgeCandidate(
        int x, int y, int left, int top, int width, int height, int vBarWidth, int hBarHeight)
    {
        if (width <= 0 || height <= 0)
            return BandHit.None;
        if (x < left || x >= left + width || y < top || y >= top + height)
            return BandHit.None;

        if (x >= left + width - (vBarWidth + Margin))
            return BandHit.Vertical;
        if (y >= top + height - (hBarHeight + Margin))
            return BandHit.Horizontal;
        return BandHit.None;
    }
}
