using Clemoutis.Core.Config;

namespace Clemoutis.Core.Scroll;

/// <summary>
/// 修飾キーの押下状態から、適用すべきホイール変換を決める。Win32 非依存。
/// オリジナルに合わせ Ctrl / Shift / Ctrl+Shift の3通りのみを扱う（Alt は対象外）。
/// Ctrl+Shift は単独 Ctrl・単独 Shift とは別物として優先的に判定する。
/// </summary>
public sealed class ModifierScrollResolver
{
    private readonly ModifierScrollSettings _settings;

    public ModifierScrollResolver(ModifierScrollSettings settings)
    {
        _settings = settings;
    }

    public WheelConversion Resolve(IModifierState m)
    {
        if (m.Ctrl && m.Shift)
            return ScrollBehaviorParser.Parse(_settings.CtrlShift);
        if (m.Ctrl)
            return ScrollBehaviorParser.Parse(_settings.Ctrl);
        if (m.Shift)
            return ScrollBehaviorParser.Parse(_settings.Shift);
        return WheelConversion.None;
    }
}
