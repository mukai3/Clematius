namespace Clemoutis;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AppContext());
    }
}

/// <summary>
/// メインウィンドウを持たない常駐アプリのルート。
/// フェーズ1でトレイアイコンとフック基盤をここに載せる。
/// </summary>
internal sealed class AppContext : ApplicationContext
{
}
