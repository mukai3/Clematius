using System.Runtime.InteropServices;
using Accessibility;

namespace Clemotius.Gestures;

/// <summary>
/// 右DOWN位置がファイル/フォルダ等の「ドラッグ可能な項目」の上かを MSAA で判定する。
/// 項目上なら右ボタンをアプリへ透過し、アプリ独自の右ドラッグ（例: エクスプローラの
/// ファイル/フォルダの右ドラッグ）を成立させる。項目の無い背景上ならジェスチャーを扱う。
///
/// 設計原則（フックスレッドで無制限の同期クロスプロセス呼び出しをしない）に従い、MSAA 呼び出しは
/// 別スレッドで実行して最大 ~30ms だけ待つ。時間内に判定できなければ「項目上」とみなして透過する
/// （壊れた右ドラッグは致命的なため、不確定時はドラッグ保護を優先する安全側）。
/// 連続右クリックに備えて近傍・短時間の結果はキャッシュする。
/// </summary>
internal static class RightDragItemDetector
{
    // MSAA ロール（oleacc）
    private const int ROLE_SYSTEM_LISTITEM = 34;   // リスト項目（ファイル/フォルダ等）
    private const int ROLE_SYSTEM_OUTLINEITEM = 35; // ツリー項目（フォルダツリー等）

    private const int ProbeTimeoutMs = 30;

    private sealed record CacheEntry(int X, int Y, bool IsItem, uint Tick);
    private static volatile CacheEntry? _cache;

    /// <returns>項目（ファイル/フォルダ等）の上なら true。背景上なら false。</returns>
    public static bool IsOverDraggableItem(int x, int y)
    {
        var c = _cache;
        uint now = (uint)Environment.TickCount;
        if (c is not null && now - c.Tick < 250 && Math.Abs(x - c.X) < 8 && Math.Abs(y - c.Y) < 8)
            return c.IsItem;

        bool? result = null;
        try
        {
            var task = Task.Run(() => ProbeIsItem(x, y));
            if (task.Wait(ProbeTimeoutMs))
                result = task.Result;
        }
        catch (AggregateException)
        {
            // ProbeIsItem 内で握り切れなかった例外。判定不能として扱う。
        }

        if (result is bool r)
        {
            _cache = new CacheEntry(x, y, r, now);
            return r;
        }
        // 判定不能（相手ビジー等で 30ms 以内に応答せず）: ドラッグ保護を優先して項目扱い（透過）。
        return true;
    }

    private static bool ProbeIsItem(int x, int y)
    {
        try
        {
            if (AccessibleObjectFromPoint(new POINTSTRUCT { x = x, y = y },
                    out IAccessible? acc, out object child) != 0 || acc is null)
            {
                return false;
            }

            // ヒット要素が項目の子（アイコン/テキスト等）のこともあるため親を少し遡って項目を探す
            object childId = child ?? 0;
            for (int depth = 0; depth < 3 && acc is not null; depth++)
            {
                int role = RoleOf(acc, childId);
                if (role is ROLE_SYSTEM_LISTITEM or ROLE_SYSTEM_OUTLINEITEM)
                    return true;
                acc = acc.accParent as IAccessible;
                childId = 0; // 親へ遡ったら自身を指す
            }
        }
        catch (COMException) { }
        catch (InvalidCastException) { }
        catch (ArgumentException) { }
        return false;
    }

    private static int RoleOf(IAccessible acc, object childId)
    {
        try
        {
            return acc.get_accRole(childId) is int role ? role : 0;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTSTRUCT { public int x, y; }

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromPoint(
        POINTSTRUCT pt, out IAccessible? ppacc, out object pvarChild);
}
