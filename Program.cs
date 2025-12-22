using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(GlobalThreadExceptionHandler);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalUnhandledExceptionHandler);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Sistem parametrelerini uygulama açılışında bir kez yükle
            SystemParameters.Load();

            try 
            {
                Application.Run(new SplashForm());
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Uygulama çökme hatası", "Program", ex.ToString());
            }
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Uygulama iş parçacığı hatası", "Program.GlobalThreadExceptionHandler", e.Exception.ToString());
            MessageBox.Show("Beklenmedik bir hata oluştu. Hata loglandı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Uygulama işlenmemiş hata", "Program.GlobalUnhandledExceptionHandler", ex.ToString());
            MessageBox.Show("Kritik bir hata oluştu. Uygulama kapatılacak.", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
