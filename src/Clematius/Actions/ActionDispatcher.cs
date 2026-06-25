using System.Collections.Concurrent;
using Clematius.Core.Actions;

namespace Clematius.Actions;

/// <summary>
/// アクション実行を単一の専用ワーカースレッドへ集約するディスパッチャ。
///
/// 以前はフックスレッドから 1 入力ごとに <c>Task.Run</c> でアクションを実行していた。
/// 右ボタン+ホイールを高速回転すると 1 ノッチごとにタスクが起動し、キー送信は
/// 前面化待ち＋直列化で 1 件あたり最大 ~100ms かかるため、ユーザーが止めた後も
/// 大量のタスクが ThreadPool に積み上がって後追い実行され、ThreadPool 自体も膨らむ。
///
/// 本クラスは実行を 1 本の専用スレッドへ直列化し、キューに上限を設ける。上限到達時は
/// 最古の要求を捨てて backlog を抑える（入力補助では「全入力を忠実に処理」より
/// 「遅延した操作を溜めない」方が安全という方針）。フックスレッドからの <see cref="Enqueue"/>
/// は待たずに即座に返る。
/// </summary>
internal sealed class ActionDispatcher : IDisposable
{
    // backlog の上限。通常のジェスチャー（1 件）や常識的なホイール連打を吸収しつつ、
    // 暴走した高速ホイールでも遅延実行が溜まり続けないだけの小さめの値。
    private const int Capacity = 16;

    private readonly ActionExecutor _executor;
    private readonly BlockingCollection<(GestureAction action, nint target)> _queue =
        new(new ConcurrentQueue<(GestureAction, nint)>(), Capacity);
    private readonly Thread _worker;

    public ActionDispatcher(ActionExecutor executor)
    {
        _executor = executor;
        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = nameof(ActionDispatcher),
        };
        _worker.Start();
    }

    /// <summary>
    /// アクションを実行キューへ積む。フックスレッドから呼ばれるため決してブロックしない。
    /// 上限到達時は最古の 1 件を捨ててから積み直す（遅延した操作を溜めない）。
    /// </summary>
    public void Enqueue(GestureAction action, nint target)
    {
        // Enqueue はフックコールバック（単一のフックスレッド）からのみ呼ばれる前提のため、
        // 生産者は 1 つ。TryAdd は満杯時に即 false を返す（ブロックしない）。
        while (!_queue.TryAdd((action, target)))
        {
            if (!_queue.TryTake(out _))
                break; // 取り出せない（消費側が処理中で空になった）なら次の TryAdd で入る
        }
    }

    private void Run()
    {
        foreach (var (action, target) in _queue.GetConsumingEnumerable())
        {
            try
            {
                _executor.Execute(action, target);
            }
            catch
            {
                // 1 件の失敗で常駐ワーカーを落とさない（次の要求を処理し続ける）。
            }
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _worker.Join(1000);
        _queue.Dispose();
    }
}
