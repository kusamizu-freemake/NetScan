using System;
using System.Threading;
using System.Windows.Forms;

namespace NetScan
{
    internal static class NetScanStartup
    {
        private const string MUTEX_NAME = "NetScanAppMutex";          // 二重起動防止のためのミューテックス名
        private const string MSG_ALREADY_RUNNING = "すでにアプリが起動しています。";
        private const string TITLE_DUPLICATE_LAUNCH = "二重起動防止";

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 二重起動防止
            using (Mutex Mutex = new Mutex(true, MUTEX_NAME, out bool CreatedNew))
            {
                if (!CreatedNew)
                {
                    MessageBox.Show(
                        MSG_ALREADY_RUNNING,
                        TITLE_DUPLICATE_LAUNCH,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            // アプリケーション実行
            System.Diagnostics.Debug.WriteLine("Startup: NetScan起動");
            Application.Run(new NetScanForm());
        }
    }
}