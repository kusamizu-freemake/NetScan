using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetScan
{
    /// <summary>
    /// アプリケーション全体で使用する定数を管理するクラス
    /// </summary>
    public static class AppConstants
    {
        // NetScanForm.cs

        // ListViewの列名
        public static class ListViewColumns
        {
            public const string IPAddress = "IPアドレス";
            public const string HostName = "ホスト名";
            public const string Status = "状態";
        }

        // ListViewのレイアウト設定
        public static class ListViewLayout
        {
            public const int IPAddressWidth = 150;
            public const int HostNameWidth = 200;
            public const int StatusWidth = 80;
        }

        // NetScanStartup.cs
        public static class StartupMsg
        {
            public const string MSG_ALREADY_RUNNING = "すでにアプリが起動しています。";
            public const string MSG_NetScan_START = "Startup: NetScan起動";
        }

        public static class StartupTitle
        {
            public const string TITLE_DUPLICATE_LAUNCH = "二重起動防止";
        }

        public static class StartupConfig
        {
            public const string MUTEX_NAME = "NetScanAppMutex"; // 二重起動防止のためのミューテックス名
        }
    }
}
