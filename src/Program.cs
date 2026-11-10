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
            var app = new App();
            _ = app;
        });
    }
}
