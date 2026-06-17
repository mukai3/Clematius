using System.Runtime.InteropServices;
using Accessibility;
using Clemotius.Interop;

namespace Clemotius.Scroll;

/// <summary>カーソル直下のスクロールバーの向き。</summary>
internal enum ScrollBarHit
{
    None,
    Horizontal,
    Vertical,
}

/// <summary>
/// カーソル直下がスクロールバーかどうか、およびその向きを判定する。4段構え:
///   1. 単独スクロールバー（独立した "ScrollBar" コントロール）: クラス名＋スタイルで判定
///   2. 標準スクロールバー（ウィンドウ非クライアントの WS_HSCROLL/WS_VSCROLL）: WM_NCHITTEST で判定
///   3. カスタム描画スクロールバー: MSAA の ROLE_SYSTEM_SCROLLBAR
///   4. Chromium 系: MSAA は a11y 既定無効でスタブしか返さないため、UIA の
///      ScrollPattern 要素＋端帯ジオメトリ（<see cref="ScrollBarBand"/>）で判定。
///      スクロール自体は WM_VSCROLL/WM_HSCROLL が Chromium にも効くことを実測確認済み。
/// </summary>
internal static class ScrollBarDetector
{
    private const int GWL_STYLE = -16;
    private const int SBS_VERT = 0x0001; // 立っていれば垂直（単独スクロールバー）

    // 検出はクロスプロセス呼び出し（NCHITTEST/MSAA/UIA）を含み、相手がビジーだと
    // 長くブロックしうるため、連続ホイール中は近傍・短時間の結果を再利用する。
    // 参照の差し替えがアトミックになるよう不変レコードで保持する
    // （フックスレッドとバックグラウンド検出スレッドの両方から書くため）。
    // Wheel=true は「対象が WM_VSCROLL を受け付けないカスタムバー（Excel 等の MSAA 検出分）なので、
    // WM_MOUSEWHEEL/WM_MOUSEHWHEEL で送る」ことを示す。標準バー/Chromium(UIA) は false（WM_SCROLL）。
    private sealed record CacheEntry(int X, int Y, ScrollBarHit Hit, nint Target, bool Wheel, uint Tick);

    private static volatile CacheEntry? _cache;

    // MSAA/UIA バックグラウンド検出のリース (0=空き、それ以外=開始tick)。MSAA/UIA が相手プロセス
    // 都合で長時間戻らないと、単純な 0/1 ガードでは解放されず以後の検出が永久に起動しなくなる。
    // ProbeMaxMs を超えたリースは「詰まった」とみなして新しい検出が奪えるようにし、検出が
    // グローバルに固まるのを防ぐ。
    private static uint _probeLease;
    private const uint ProbeMaxMs = 1000;

    // 直近にカスタムバー（MSAA/UIA）と判定した窓を覚えておく。非同期検出が間に合わない
    // 新規/キャッシュ切れのホイールでも、同じ窓なら前回の軸で確定して「素通し（＝誤軸スクロール、
    // 例: 横バー上で縦に動く）」を防ぐ。フックを止めないため同期 MSAA は使わない。
    private sealed record CustomHit(nint Hwnd, ScrollBarHit Hit, bool Wheel, uint Tick);
    private static volatile CustomHit? _lastCustom;
    private const uint CustomMemoryMs = 2000;

    /// <summary>
    /// カーソル直下のスクロールバーの向きと、スクロールメッセージの送出先ウィンドウを返す。
    /// 検出できなければ (None, 0)。
    ///
    /// フックスレッドの停止時間を最大 ~30ms に抑えるため、同期的に行うのは
    /// クラス名判定と NCHITTEST（30ms 打ち切り）まで。MSAA/UIA はバックグラウンドで
    /// 計算してキャッシュへ反映する（その間の数十 ms は素通し扱い）。
    /// タイムアウトなしの同期呼び出しは、高負荷アプリ（画像ビューアの読み込み中等）で
    /// フックごと固まり、入力キュー溢れ＝連続ビープ音の原因になる。
    /// </summary>
    public static (ScrollBarHit hit, nint target, bool wheel) Detect(int x, int y)
    {
        var c = _cache;
        uint now = (uint)Environment.TickCount;
        if (c is not null && now - c.Tick < 250 && Math.Abs(x - c.X) < 8 && Math.Abs(y - c.Y) < 8)
            return (c.Hit, c.Target, c.Wheel);

        var pt = new NativeMethods.POINT { X = x, Y = y };
        nint hwnd = InputNative.WindowFromPoint(pt);
        if (hwnd == 0)
            return Store(x, y, ScrollBarHit.None, 0, false);

        // 1) 単独スクロールバー コントロール → 親ウィンドウへ送る
        if (GetClassName(hwnd).Equals("ScrollBar", StringComparison.OrdinalIgnoreCase))
        {
            int style = InputNative.GetWindowLongW(hwnd, GWL_STYLE);
            var dir = (style & SBS_VERT) != 0 ? ScrollBarHit.Vertical : ScrollBarHit.Horizontal;
            return Store(x, y, dir, hwnd, false);
        }

        // 2) 非クライアントの標準スクロールバー: 30ms 打ち切りでヒットテスト
        nint lParam = unchecked((nint)((y << 16) | (x & 0xFFFF)));
        if (InputNative.SendMessageTimeoutW(
                hwnd, InputNative.WM_NCHITTEST, 0, lParam,
                InputNative.SMTO_ABORTIFHUNG, 30, out nint hit) == 0)
        {
            // 応答しない相手には MSAA/UIA も掛けない
            return Store(x, y, ScrollBarHit.None, 0, false);
        }
        switch ((int)hit)
        {
            case InputNative.HTHSCROLL:
                return Store(x, y, ScrollBarHit.Horizontal, hwnd, false);
            case InputNative.HTVSCROLL:
                return Store(x, y, ScrollBarHit.Vertical, hwnd, false);
        }

        // 3)+4) カスタム描画スクロールバー（MSAA → UIA）: 相手プロセス次第で
        // 数秒ブロックしうるため、フックスレッドでは待たずに別スレッドで計算して
        // キャッシュへ反映する。完了までは暫定で「スクロールバーでない」をキャッシュし、
        // 連続ホイール中の NCHITTEST 再実行（30ms×N）も防ぐ。
        KickProbe(x, y, hwnd);

        // 非同期検出が間に合わない間も、同じ窓を直近カスタムバーと判定済みなら前回の軸で確定する。
        // これにより横スクロールバー上で素通しの縦スクロールが混ざるのを防ぐ。送出先はこの窓でよい
        // （ホイール送出はどの窓でも有効なことを実測確認済み）。
        var lc = _lastCustom;
        if (lc is not null && lc.Hwnd == hwnd && now - lc.Tick < CustomMemoryMs && lc.Hit != ScrollBarHit.None)
            return Store(x, y, lc.Hit, hwnd, lc.Wheel);

        return Store(x, y, ScrollBarHit.None, 0, false); // 暫定値（プローブ完了時に上書きされる）
    }

    // スクロールバー窓（NUIScrollbar 等）はこの太さ以下の細い窓になることが多い。
    private const int ScrollbarWindowThickness = 26;

    /// <summary>
    /// マウス移動中の事前検出（ホバー先読み）。カスタムスクロールバーらしい「細い窓」の上だけ
    /// バックグラウンドで検出してキャッシュを温め、直後のホイールが最初の1ノッチから正しい軸で
    /// 動くようにする（プロセス外フックでは同期 MSAA が使えないための代替）。フックスレッドでは
    /// WindowFromPoint/GetWindowRect（ローカルで安全）しか行わない。
    /// </summary>
    public static void Prime(int x, int y)
    {
        var c = _cache;
        uint now = (uint)Environment.TickCount;
        if (c is not null && now - c.Tick < 250 && Math.Abs(x - c.X) < 8 && Math.Abs(y - c.Y) < 8)
            return; // 近傍に新しい結果あり
        if (ProbeBusy())
            return; // 既に検出中（期限内）

        nint hwnd = InputNative.WindowFromPoint(new NativeMethods.POINT { X = x, Y = y });
        if (hwnd == 0 || !InputNative.GetWindowRect(hwnd, out var r))
            return;
        if (Math.Min(r.Right - r.Left, r.Bottom - r.Top) > ScrollbarWindowThickness)
            return; // 細くない＝カスタムスクロールバー窓ではない（通常窓上では先読みしない）

        KickProbe(x, y, hwnd);
    }

    // カスタム描画スクロールバー（MSAA → UIA）をバックグラウンドで検出してキャッシュへ反映する。
    // フックスレッドを止めないため必ず別スレッドで実行する（単発ガード付き）。
    private static void KickProbe(int x, int y, nint hwnd)
    {
        uint lease = TryAcquireProbe();
        if (lease == 0)
            return;
        Task.Run(() =>
        {
            try
            {
                // 3) MSAA カスタムバー（Excel 等）は WM_VSCROLL を受け付けないため WM_MOUSEWHEEL で送る
                var m = DetectByMsaaCore(x, y);
                if (m.hit != ScrollBarHit.None)
                {
                    uint t = (uint)Environment.TickCount;
                    _cache = new CacheEntry(x, y, m.hit, m.target, true, t);
                    _lastCustom = new CustomHit(hwnd, m.hit, true, t);
                }
                else
                {
                    // 4) UIA（Chromium 等）は WM_VSCROLL/WM_HSCROLL がそのまま有効
                    var u = DetectByUia(x, y, hwnd);
                    uint t = (uint)Environment.TickCount;
                    _cache = new CacheEntry(x, y, u.hit, u.target, false, t);
                    if (u.hit != ScrollBarHit.None)
                        _lastCustom = new CustomHit(hwnd, u.hit, false, t);
                }
            }
            finally
            {
                ReleaseProbe(lease);
            }
        });
    }

    // MSAA/UIA バックグラウンド検出のリースを取得する。空き、または期限超過で詰まったリースを
    // 奪えたらその開始 tick（解放用トークン、0 は使わない）を返す。取れなければ 0。
    private static uint TryAcquireProbe()
    {
        uint now = (uint)Environment.TickCount;
        if (now == 0) now = 1; // 0 は「空き」を表すので避ける
        while (true)
        {
            uint cur = Volatile.Read(ref _probeLease);
            if (cur != 0 && now - cur < ProbeMaxMs)
                return 0; // 実行中かつ期限内
            if (Interlocked.CompareExchange(ref _probeLease, now, cur) == cur)
                return now; // 空き、または期限超過のリースを奪った
        }
    }

    // 自分のリースのままなら解放する。期限超過で横取りされていたら触らない。
    private static void ReleaseProbe(uint myToken)
        => Interlocked.CompareExchange(ref _probeLease, 0, myToken);

    private static bool ProbeBusy()
    {
        uint cur = Volatile.Read(ref _probeLease);
        return cur != 0 && (uint)Environment.TickCount - cur < ProbeMaxMs;
    }

    /// <summary>
    /// 切り分け用: カーソル直下の各検出段（クラス名 / NCHITTEST / MSAA / UIA）の結果を1行にまとめる。
    /// クロスプロセス呼び出しを含むため必ずバックグラウンドで呼ぶこと（フックスレッド禁止）。
    /// </summary>
    public static string Describe(int x, int y)
    {
        var pt = new NativeMethods.POINT { X = x, Y = y };
        nint hwnd = InputNative.WindowFromPoint(pt);
        if (hwnd == 0)
            return $"pos=({x},{y}) window=(none)";

        string cls = GetClassName(hwnd);
        int style = InputNative.GetWindowLongW(hwnd, GWL_STYLE);

        string nchit = "-";
        nint lParam = unchecked((nint)((y << 16) | (x & 0xFFFF)));
        if (InputNative.SendMessageTimeoutW(
                hwnd, InputNative.WM_NCHITTEST, 0, lParam,
                InputNative.SMTO_ABORTIFHUNG, 30, out nint hit) != 0)
        {
            nchit = ((int)hit).ToString();
        }

        var msaa = DetectByMsaaCore(x, y);
        var uia = DetectByUia(x, y, hwnd);

        return $"pos=({x},{y}) class={cls} style=0x{style:X} nchit={nchit} " +
               $"msaa={msaa.hit} uia={uia.hit}";
    }

    private static (ScrollBarHit hit, nint target, bool wheel) Store(
        int x, int y, ScrollBarHit hit, nint target, bool wheel)
    {
        _cache = new CacheEntry(x, y, hit, target, wheel, (uint)Environment.TickCount);
        return (hit, target, wheel);
    }

    private const int ROLE_SYSTEM_SCROLLBAR = 3;

    // ── 4) UIA: ScrollPattern を持つ要素＋端帯ジオメトリ ──
    // Chromium はスクロールバーを UIA の ScrollBar 要素として公開しないため、
    // 「スクロール可能要素の右端/下端のスクロールバー幅の帯」をスクロールバー扱いする。
    private static (ScrollBarHit hit, nint target) DetectByUia(int x, int y, nint hwnd)
    {
        try
        {
            var el = System.Windows.Automation.AutomationElement.FromPoint(
                new System.Windows.Point(x, y));
            for (int depth = 0; el is not null && depth < 8; depth++)
            {
                if (el.TryGetCurrentPattern(
                        System.Windows.Automation.ScrollPattern.Pattern, out object p))
                {
                    var pattern = (System.Windows.Automation.ScrollPattern)p;
                    var rect = el.Current.BoundingRectangle;
                    var hit = Clemotius.Core.Scroll.ScrollBarBand.Hit(
                        x, y,
                        (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                        pattern.Current.VerticallyScrollable,
                        pattern.Current.HorizontallyScrollable,
                        InputNative.GetSystemMetrics(InputNative.SM_CXVSCROLL),
                        InputNative.GetSystemMetrics(InputNative.SM_CYHSCROLL));
                    return hit switch
                    {
                        Clemotius.Core.Scroll.BandHit.Vertical => (ScrollBarHit.Vertical, hwnd),
                        Clemotius.Core.Scroll.BandHit.Horizontal => (ScrollBarHit.Horizontal, hwnd),
                        // 最初に見つかったスクロール要素で判定を終える（外側へは波及させない）
                        _ => (ScrollBarHit.None, 0),
                    };
                }
                el = System.Windows.Automation.TreeWalker.ControlViewWalker.GetParent(el);
            }
        }
        catch (System.Windows.Automation.ElementNotAvailableException) { }
        catch (COMException) { }
        catch (InvalidOperationException) { }
        return (ScrollBarHit.None, 0);
    }

    private static (ScrollBarHit hit, nint target) DetectByMsaaCore(int x, int y)
    {
        try
        {
            if (AccessibleObjectFromPoint(new POINTSTRUCT { x = x, y = y },
                    out IAccessible? acc, out object child) != 0 || acc is null)
            {
                return (ScrollBarHit.None, 0);
            }

            object childId = child ?? 0;
            // ヒットした要素がスクロールバーの子（ボタン/つまみ）のこともあるため親を少し遡る
            for (int depth = 0; depth < 3 && acc is not null; depth++)
            {
                if (RoleOf(acc, childId) == ROLE_SYSTEM_SCROLLBAR)
                {
                    acc.accLocation(out _, out _, out int w, out int h, childId);
                    if (w <= 0 || h <= 0)
                        return (ScrollBarHit.None, 0);
                    if (WindowFromAccessibleObject(acc, out nint hwnd) != 0 || hwnd == 0)
                        return (ScrollBarHit.None, 0);
                    return (w >= h ? ScrollBarHit.Horizontal : ScrollBarHit.Vertical, hwnd);
                }
                acc = acc.accParent as IAccessible;
                childId = 0; // 親へ遡ったら自身を指す
            }
        }
        catch (COMException) { }
        catch (InvalidCastException) { }
        catch (ArgumentException) { }
        return (ScrollBarHit.None, 0);
    }

    private static int RoleOf(IAccessible acc, object childId)
    {
        try
        {
            return acc.get_accRole(childId) is int role ? role : 0;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTSTRUCT { public int x, y; }

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromPoint(
        POINTSTRUCT pt, out IAccessible? ppacc, out object pvarChild);

    [DllImport("oleacc.dll")]
    private static extern int WindowFromAccessibleObject(IAccessible pacc, out nint phwnd);

    private static string GetClassName(nint hwnd)
    {
        var buffer = new char[64];
        int len = InputNative.GetClassNameW(hwnd, buffer, buffer.Length);
        return len > 0 ? new string(buffer, 0, len) : "";
    }
}
