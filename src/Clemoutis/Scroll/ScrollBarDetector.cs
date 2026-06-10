using Clemoutis.Interop;

namespace Clemoutis.Scroll;

/// <summary>
/// カーソル直下が水平スクロールバーかを判定する。
///
/// v1 は Win32 のクラス名/スタイル判定のみ。これは独立した "ScrollBar" コントロール
/// （SBS_HORZ）を検出できるが、多くのアプリのスクロールバーはウィンドウの非クライアント
/// 領域（WS_HSCROLL）で別 HWND を持たないため検出できない。オリジナルが使う MSAA による
/// 厳密判定は後続（設計書の RE 方針）で追加する。
/// </summary>
internal static class ScrollBarDetector
{
    private const int GWL_STYLE = -16;
    private const int SBS_VERT = 0x0001; // 立っていれば垂直スクロールバー

    public static bool IsHorizontalScrollBar(int x, int y)
    {
        var pt = new NativeMethods.POINT { X = x, Y = y };
        nint hwnd = InputNative.WindowFromPoint(pt);
        if (hwnd == 0)
            return false;

        if (!GetClassName(hwnd).Equals("ScrollBar", StringComparison.OrdinalIgnoreCase))
            return false;

        int style = InputNative.GetWindowLongW(hwnd, GWL_STYLE);
        // SBS_VERT(0x1) が立っていれば垂直、立っていなければ水平
        return (style & SBS_VERT) == 0;
    }

    private static string GetClassName(nint hwnd)
    {
        var buffer = new char[64];
        int len = InputNative.GetClassNameW(hwnd, buffer, buffer.Length);
        return len > 0 ? new string(buffer, 0, len) : "";
    }
}
