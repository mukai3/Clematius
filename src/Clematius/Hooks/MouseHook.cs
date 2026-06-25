using Clematius.Interop;

namespace Clematius.Hooks;

internal sealed class MouseHook : LowLevelHook
{
    public MouseHook() : base(NativeMethods.WH_MOUSE_LL) { }

    /// <summary>(message, data) を受け取り、飲み込むなら true を返す。</summary>
    public Func<int, NativeMethods.MSLLHOOKSTRUCT, bool>? Handler { get; set; }

    protected override unsafe bool Handle(nint wParam, nint lParam)
    {
        ref readonly var data = ref *(NativeMethods.MSLLHOOKSTRUCT*)lParam;
        // 自分が注入したイベントだけを判定対象から除外する（無限ループ防止／生存プローブ）。
        // 以前は LLMHF_INJECTED で「あらゆる注入入力」を無視していたが、それだと AutoHotkey や
        // PowerToys 等の支援ツール由来の入力までジェスチャー対象外になり相互運用性を損なう。
        // 自前注入は必ず ClematiusSignature を dwExtraInfo に載せているため、これで識別する。
        if (data.dwExtraInfo == InputNative.ClematiusSignature)
            return false;
        NoteRealEvent();
        return Handler?.Invoke((int)wParam, data) ?? false;
    }
}
