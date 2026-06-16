using System.IO;

namespace Clemotius.Scroll;

/// <summary>
/// スクロールバー検出の切り分け用ログ。環境変数 <c>CLEMOTIUS_SCROLL_DIAG=1</c> のときだけ有効。
/// 出力先は %APPDATA%\Clemotius\scroll-diag.log。通常運用ではゼロコスト（無効時は即 return）。
/// ファイル I/O はフックスレッドで呼ばないこと（バックグラウンドからのみ使う）。
/// </summary>
internal static class ScrollDiagnostics
{
    public static readonly bool Enabled =
        Environment.GetEnvironmentVariable("CLEMOTIUS_SCROLL_DIAG") == "1";

    private static readonly object Gate = new();
    private static readonly string LogPath = BuildPath();

    private static string BuildPath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clemotius");
        return Path.Combine(dir, "scroll-diag.log");
    }

    public static void Log(string line)
    {
        if (!Enabled)
            return;
        try
        {
            lock (Gate)
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
