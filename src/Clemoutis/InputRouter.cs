using Clemoutis.Gestures;
using Clemoutis.Hooks;
using Clemoutis.Interop;
using Clemoutis.Scroll;

namespace Clemoutis;

/// <summary>
/// フックから受けたイベントを各機能へ振り分け、飲み込むか素通しかを最終判断する。
/// マウスはジェスチャー優先、未処理ならスクロール拡張へ渡す。
/// </summary>
internal sealed class InputRouter
{
    private readonly ModifierStateTracker _modifiers;
    private readonly GestureEngine _gesture;
    private readonly ScrollEnhancer _scroll;

    public InputRouter(ModifierStateTracker modifiers, GestureEngine gesture, ScrollEnhancer scroll)
    {
        _modifiers = modifiers;
        _gesture = gesture;
        _scroll = scroll;
    }

    /// <returns>true ならイベントを飲み込む</returns>
    public bool OnMouse(int message, NativeMethods.MSLLHOOKSTRUCT data)
    {
        if (_gesture.OnMouse(message, data))
            return true;

        if (message == NativeMethods.WM_MOUSEWHEEL)
            return _scroll.OnMouseWheel(data);

        return false;
    }

    /// <returns>true ならイベントを飲み込む</returns>
    public bool OnKeyboard(int message, NativeMethods.KBDLLHOOKSTRUCT data)
    {
        _modifiers.OnKey(message, data.vkCode);
        return false; // 素通し
    }
}
