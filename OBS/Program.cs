using System;
using System.Windows;
using Velopack;

namespace OBS
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Velopack'in install/uninstall/update hook'larını işlemesi için
            // uygulama başlamadan önce çağrılmalıdır.
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
