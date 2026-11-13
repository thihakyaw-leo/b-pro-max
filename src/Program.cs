using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace BProMax;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
            _ = app;
        });
    }
}
