using Clemoutis.Core.Config;

namespace Clemoutis.Core.Scroll;

/// <summary>
/// 修飾キーの押下状態とルール一覧から、適用すべきホイール変換を決める。Win32 非依存。
/// 複数の修飾キーが該当する場合は最初に一致したルールを採用する。
/// </summary>
public sealed class ModifierScrollResolver
{
    private readonly IReadOnlyList<ModifierScrollRule> _rules;

    public ModifierScrollResolver(IReadOnlyList<ModifierScrollRule> rules)
    {
        _rules = rules;
    }

    public WheelConversion Resolve(IModifierState modifiers)
    {
        foreach (var rule in _rules)
        {
            if (IsActive(rule.Modifier, modifiers))
                return ScrollBehaviorParser.Parse(rule.Behavior);
        }
        return WheelConversion.None;
    }

    private static bool IsActive(string modifier, IModifierState m) =>
        modifier.Trim().ToLowerInvariant() switch
        {
            "shift" => m.Shift,
            "ctrl" or "control" => m.Ctrl,
            "alt" => m.Alt,
            "win" or "windows" => m.Win,
            _ => false,
        };
}
