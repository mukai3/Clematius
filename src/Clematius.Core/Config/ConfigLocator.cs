using System.IO;

namespace Clematius.Core.Config;

/// <summary>設定ファイルの配置先と、ポータブルモードかどうか。</summary>
public readonly record struct ConfigLocation(string Dir, string Path, bool Portable);

/// <summary>
/// 設定ファイル(config.json)の配置先を決める。Win32 非依存（パス文字列の組み立てのみ）。
/// exe と同じフォルダに config.json があれば「ポータブルモード」としてそのフォルダを使う。
/// 無ければ通常モードとして %APPDATA%\Clematius を使う。
/// </summary>
public static class ConfigLocator
{
    public const string FileName = "config.json";
    public const string AppFolder = "Clematius";

    /// <param name="exeDir">実行ファイルのあるフォルダ。</param>
    /// <param name="appDataDir">%APPDATA%（ApplicationData）のフォルダ。</param>
    /// <param name="portableConfigExists">exe フォルダに config.json が存在するか。</param>
    public static ConfigLocation Resolve(string exeDir, string appDataDir, bool portableConfigExists)
    {
        if (portableConfigExists)
            return new ConfigLocation(exeDir, Path.Combine(exeDir, FileName), Portable: true);

        string dir = Path.Combine(appDataDir, AppFolder);
        return new ConfigLocation(dir, Path.Combine(dir, FileName), Portable: false);
    }
}
