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
            public const string MacAddress = "MACアドレス";
            public const string Status = "状態";
        }

        // ListViewのレイアウト設定
        public static class ListViewLayout
        {
            public const int IPAddressWidth = 150;
            public const int HostNameWidth = 200;
            public const int MacAddressWidth = 150;
            public const int StatusWidth = 80;
        }

        // スキャン設定
        public static class ScanConfig
        {
            public const int PingTimeout = 1000; // Pingのタイムアウト（ミリ秒）
        }

        // スキャン結果の表示文字列
        public static class ScanStatus
        {
            public const string Online = "オンライン";
            public const string HostNameUnknown = "取得不可";
            public const string ScanComplete = "スキャン完了";
            public const string MacAddressUnknown = "取得不可";
            public const string IPRangeInvalid = "開始IPと終了IPは異なる必要があります。";
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