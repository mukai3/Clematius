using Clemoutis.Core.Actions;

namespace Clemoutis.Settings;

/// <summary>設定画面でのアクション表示・編集用のヘルパー。</summary>
internal static class ActionDisplay
{
    public const string TypeKey = "キー送信";
    public const string TypeAppCommand = "コマンド";
    public const string TypeClose = "閉じる";

    public static string[] TypeNames => new[] { TypeKey, TypeAppCommand, TypeClose };

    /// <summary>一覧表示用の説明文。</summary>
    public static string Describe(GestureAction action) => action switch
    {
        KeyAction k => $"{TypeKey}: {k.Stroke}",
        AppCommandAction c => $"{TypeAppCommand}: {c.Command}",
        CloseAction => TypeClose,
        _ => "(不明)",
    };

    public static string TypeNameOf(GestureAction action) => action switch
    {
        KeyAction => TypeKey,
        AppCommandAction => TypeAppCommand,
        CloseAction => TypeClose,
        _ => TypeKey,
    };
}
