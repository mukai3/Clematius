using System.Collections.Concurrent;
using System.Diagnostics;
using Clemotius.Core.Windowing;
using Clemotius.Interop;

namespace Clemotius.Windowing;

/// <summary>
/// タイトルバーアクションのウィンドウ操作を実行する副作用層。
/// トグル系（最前面・シェード・半透明）は現在状態を見て反転する。
/// </summary>
internal sealed class WindowActionExecutor
{
    // ウィンドウシェードの巻き上げ前の高さ（hwnd → 元の高さ）
    private readonly ConcurrentDictionary<nint, int> _shadeHeights = new();

    // Clemotius が半透明化（WS_EX_LAYERED 付与）したウィンドウ。元から layered な
    // 他アプリの状態を壊さないよう、解除対象を自分が付けたものに限定するために記録する。
    private readonly ConcurrentDictionary<nint, byte> _translucentWindows = new();

    /// <summary>半透明化の不透明度（%）。設定リロードで更新される。</summary>
    public volatile int OpacityPercent = 50;

    public void Execute(WindowAction action, nint hwnd)
    {
        if (hwnd == 0)
            return;
        switch (action)
        {
            case WindowAction.AlwaysOnTop:
                ToggleAlwaysOnTop(hwnd);
                break;
            case WindowAction.WindowShade:
                ToggleWindowShade(hwnd);
                break;
            case WindowAction.OpenExeFolder:
                OpenExeFolder(hwnd);
                break;
            case WindowAction.Translucent:
                ToggleTranslucent(hwnd);
                break;
        }
    }

    private static void ToggleAlwaysOnTop(nint hwnd)
    {
        bool topmost = ((nint)InputNative.GetWindowLongPtrW(hwnd, InputNative.GWL_EXSTYLE)
                        & InputNative.WS_EX_TOPMOST) != 0;
        InputNative.SetWindowPos(
            hwnd,
            topmost ? InputNative.HWND_NOTOPMOST : InputNative.HWND_TOPMOST,
            0, 0, 0, 0,
            InputNative.SWP_NOMOVE | InputNative.SWP_NOSIZE | InputNative.SWP_NOACTIVATE);
    }

    private void ToggleWindowShade(nint hwnd)
    {
        if (!InputNative.GetWindowRect(hwnd, out var rect))
            return;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (_shadeHeights.TryRemove(hwnd, out int original))
        {
            // 巻き戻し
            InputNative.SetWindowPos(hwnd, 0, rect.Left, rect.Top, width, original,
                InputNative.SWP_NOACTIVATE);
            return;
        }

        // タイトルバーだけ残して巻き上げる
        int captionHeight = InputNative.GetSystemMetrics(InputNative.SM_CYCAPTION)
                          + InputNative.GetSystemMetrics(InputNative.SM_CYSIZEFRAME) * 2;
        if (height <= captionHeight)
            return;
        _shadeHeights[hwnd] = height;
        InputNative.SetWindowPos(hwnd, 0, rect.Left, rect.Top, width, captionHeight,
            InputNative.SWP_NOACTIVATE);
    }

    private static void OpenExeFolder(nint hwnd)
    {
        InputNative.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0)
            return;
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            string? path = proc.MainModule?.FileName;
            if (string.IsNullOrEmpty(path))
                return;
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            // アクセス不可（保護プロセス等）は黙って無視
        }
    }

    private void ToggleTranslucent(nint hwnd)
    {
        nint ex = InputNative.GetWindowLongPtrW(hwnd, InputNative.GWL_EXSTYLE);

        if (_translucentWindows.ContainsKey(hwnd))
        {
            // 自分が付けた半透明だけ解除する（WS_EX_LAYERED を外す）
            InputNative.SetWindowLongPtrW(hwnd, InputNative.GWL_EXSTYLE,
                ex & ~(nint)InputNative.WS_EX_LAYERED);
            _translucentWindows.TryRemove(hwnd, out _);
            return;
        }

        // 元から layered なアプリ（per-pixel alpha やクリック透過などを使う）は触らない。
        // WS_EX_LAYERED を勝手に外すと相手の描画・透明度の前提を壊すため。
        if ((ex & InputNative.WS_EX_LAYERED) != 0)
            return;

        InputNative.SetWindowLongPtrW(hwnd, InputNative.GWL_EXSTYLE,
            ex | InputNative.WS_EX_LAYERED);
        byte alpha = (byte)Math.Clamp(OpacityPercent * 255 / 100, 16, 255);
        InputNative.SetLayeredWindowAttributes(hwnd, 0, alpha, InputNative.LWA_ALPHA);
        _translucentWindows[hwnd] = 1;
    }
}
