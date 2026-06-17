using Clemotius.Interop;

namespace Clemotius.Hooks;

internal sealed class KeyboardHook : LowLevelHook
{
    public KeyboardHook() : base(NativeMethods.WH_KEYBOARD_LL) { }

    /// <summary>(message, data) を受け取り、飲み込むなら true を返す。</summary>
    public Func<int, NativeMethods.KBDLLHOOKSTRUCT, bool>? Handler { get; set; }

    protected override unsafe bool Handle(nint wParam, nint lParam)
    {
        ref readonly var data = ref *(NativeMethods.KBDLLHOOKSTRUCT*)lParam;
        // 自前注入のみ除外する（MouseHook と同方針）。dwExtraInfo の署名で識別し、
        // 他ツール由来の注入キー入力は通常入力として扱って相互運用性を保つ。
        if (data.dwExtraInfo == InputNative.ClemotiusSignature)
            return false;
        NoteRealEvent();
        return Handler?.Invoke((int)wParam, data) ?? false;
    }
}
