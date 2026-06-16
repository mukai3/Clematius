namespace Clemotius.Core.Config;

/// <summary>
/// 前面アプリのプロセス名から適用プロファイルを決定する。Win32 非依存。
/// マッチ規則: ProcessPattern（拡張子 .exe を無視）にプロセス名が一致する先頭プロファイルを返す。
/// ProcessPattern はカンマ区切りで複数のプロセス名を指定でき、いずれかに一致すれば該当
/// （例 "chrome, edge, brave"）。一致するプロファイルが無ければ null（＝ジェスチャー無効）。
/// グローバル既定（旧 "*" プロファイル）は廃止し、明示プロファイルに一致したアプリだけで有効。
/// </summary>
public sealed class ProfileResolver
{
    private readonly IReadOnlyList<GestureProfile> _profiles;

    public ProfileResolver(IReadOnlyList<GestureProfile> profiles)
    {
        _profiles = profiles;
    }

    public GestureProfile? Resolve(string? processName)
    {
        string name = NormalizeProcess(processName);
        if (name.Length == 0)
            return null;

        foreach (var p in _profiles)
        {
            if (Matches(p.ProcessPattern, name))
                return p;
        }
        return null;
    }

    private static bool Matches(string pattern, string processName)
    {
        // カンマ区切りの各プロセス名のいずれかに一致すれば真
        foreach (var part in pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(NormalizeProcess(part), processName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string NormalizeProcess(string? value) => ProcessName.Normalize(value);
}
