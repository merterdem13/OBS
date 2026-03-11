using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OBS.DataAccess;

namespace OBS.Helpers
{
    /// <summary>
    /// Pencere açılırken oluşan beyaz parıltıyı (white flash) önler.
    ///
    /// Sorun: Windows, HWND'yi WPF render pipeline'dan önce sınıf arka plan
    ///        fırçasıyla (varsayılan beyaz) boyar. Opacity=0 gibi WPF-düzeyi
    ///        geçici çözümler bu boyamayı engelleyemez.
    ///
    /// Çözüm: OnSourceInitialized anında Win32 SetClassLong ile HWND sınıf
    ///        arka plan rengini mevcut temaya uygun renge ayarla.
    /// </summary>
    public static class WindowFlashFixer
    {
        private const int GCL_HBRBACKGROUND = -10;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetClassLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        /// <summary>
        /// Verilen pencerenin HWND sınıf arka plan fırçasını mevcut uygulama temasına göre ayarlar.
        /// Bu metod pencenin OnSourceInitialized override'ı içinde çağrılmalıdır.
        /// </summary>
        public static void Apply(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                var theme = GetCurrentTheme();
                var gdiColor = GetGdiColorForTheme(theme);
                var hBrush = CreateSolidBrush(gdiColor);
                if (hBrush == IntPtr.Zero) return;

                if (Environment.Is64BitProcess)
                    SetClassLongPtr(hwnd, GCL_HBRBACKGROUND, hBrush);
                else
                    SetClassLong(hwnd, GCL_HBRBACKGROUND, hBrush.ToInt32());

                // Pencereyi yeniden çiz — WPF zaten kendi içeriğini çizecek
                InvalidateRect(hwnd, IntPtr.Zero, true);
            }
            catch
            {
                // Sessiz hata — en kötü senaryoda flash görünür ama çökmez
            }
        }

        private static string GetCurrentTheme()
        {
            try
            {
                return new SettingsRepository().GetSetting("Theme") ?? "Light";
            }
            catch
            {
                return "Light";
            }
        }

        /// <summary>
        /// GDI renk formatı: 0x00BBGGRR (ters byte sırası).
        /// BgBodyBrush değerleriyle eşleştirilmiştir.
        /// </summary>
        private static int GetGdiColorForTheme(string theme)
        {
            return theme switch
            {
                // Dark:  #121212  → R=18 G=18 B=18
                "Dark"        => (18) | (18 << 8) | (18 << 16),
                // Alternative (Nord): #2E3440 → R=46 G=52 B=64
                "Alternative" => (46) | (52 << 8) | (64 << 16),
                // Light: #f1f5f9 → R=241 G=245 B=249
                _             => (241) | (245 << 8) | (249 << 16),
            };
        }
    }
}
