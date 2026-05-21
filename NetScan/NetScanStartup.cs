using System;
using System.Threading;
using System.Windows.Forms;

namespace NetScan
{
    internal static class NetScanStartup
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 二重起動防止
            using (Mutex Mutex = new Mutex(true, AppConstants.StartupConfig.MUTEX_NAME, out bool CreatedNew))
            {
                if (!CreatedNew)
                {
                    MessageBox.Show(
                        AppConstants.StartupMsg.MSG_ALREADY_RUNNING,
                        AppConstants.StartupTitle.TITLE_DUPLICATE_LAUNCH,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            // アプリケーション実行
            System.Diagnostics.Debug.WriteLine(AppConstants.StartupMsg.MSG_NetScan_START);
            Application.Run(new NetScanForm());
        }
    }
}