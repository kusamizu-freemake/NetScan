#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetScan
{
    public partial class NetScanForm : Form
    {
        private CancellationTokenSource? cts; // スキャン停止のためのCancellationTokenSource

        // 右クリックメニュー（コピー機能）をまとめて管理するクラス
        private ListViewCopyHelper? listViewCopyHelper;

        // ソート機能をまとめて管理するクラス
        private ListViewSortHelper? listViewSortHelper;

        // ListViewの列名
        private const string COL_IP_ADDRESS = "IPアドレス";
        private const string COL_HOST_NAME = "ホスト名";
        private const string COL_MAC_ADDRESS = "MACアドレス";
        private const string COL_RESPONSE_TIME = "応答時間(ms)";
        private const string COL_STATUS = "状態";

        // ListViewのレイアウト設定
        private const int COL_IP_ADDRESS_WIDTH = 150;
        private const int COL_HOST_NAME_WIDTH = 200;
        private const int COL_MAC_ADDRESS_WIDTH = 150;
        private const int COL_RESPONSE_TIME_WIDTH = 100;
        private const int COL_STATUS_WIDTH = 80;

        // スキャン設定
        private const int PING_TIMEOUT = 1000;              // Pingのタイムアウト（ミリ秒）
        private const int ARP_CACHE_WAIT = 100;             // ARPキャッシュの更新待ち時間（ミリ秒）

        // スキャン結果の表示文字列
        private const string STATUS_ONLINE = "オンライン";
        private const string STATUS_IP_RANGE_INVALID = "開始IPと終了IPは異なる必要があります。";
        private const string MSG_SCAN_COMPLETE = "スキャン完了しました。";
        private const string MSG_SCAN_CANCEL = "スキャンを中止しました。";

        // 進捗バーの初期値
        private const int PROGRESS_INITIAL_VALUE = 0;

        public NetScanForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化

            // 右クリックメニュー（コピー機能）をListViewに割り当てる
            listViewCopyHelper = new ListViewCopyHelper(
                ListViewResult,
                ContextMenuStripResult,
                MenuItemCopy,
                MenuItemCopyCell);

            // ソート機能をListViewに割り当てる
            listViewSortHelper = new ListViewSortHelper(ListViewResult);
        }

        // ListViewの列を設定
        private void NetScanForm_Load(object sender, EventArgs e)
        {
            ListViewResult.Columns.Add(COL_IP_ADDRESS, COL_IP_ADDRESS_WIDTH);
            ListViewResult.Columns.Add(COL_HOST_NAME, COL_HOST_NAME_WIDTH);
            ListViewResult.Columns.Add(COL_MAC_ADDRESS, COL_MAC_ADDRESS_WIDTH);
            ListViewResult.Columns.Add(COL_RESPONSE_TIME, COL_RESPONSE_TIME_WIDTH);
            ListViewResult.Columns.Add(COL_STATUS, COL_STATUS_WIDTH);

            BtnStop.Enabled = false; // スキャン停止ボタンは初期状態で無効化
        }

        // スキャン開始ボタン
        private async void BtnScan_Click(object sender, EventArgs e)
        {
            // 入力されたIPアドレスの範囲を取得
            string StartIP = TxtStartIP.Text.Trim();
            string EndIP = TxtEndIP.Text.Trim();

            if (StartIP == EndIP)
            {
                MessageBox.Show(STATUS_IP_RANGE_INVALID);
                return;
            }

            // IP範囲リストをここで先に作成（ProgressBarのMaximumに使うため）
            List<string> IPRange = IpRangeHelper.GetIPRange(StartIP, EndIP);
            System.Diagnostics.Debug.WriteLine($"スキャン対象: {IPRange.Count} 件");

            // 初期化
            ListViewResult.Items.Clear();
            BtnScan.Enabled = false;
            BtnStop.Enabled = true;
            ProgressBarScan.Maximum = IPRange.Count; // スキャン対象のIP数を最大値にセット
            ProgressBarScan.Value = PROGRESS_INITIAL_VALUE; // 進捗を0にリセット

            // CancellationTokenSourceを新規作成
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                // バックグラウンドタスクでスキャン処理を実行
                await Task.Run(() =>
                {
                    // 各IPにPingを送信
                    foreach (string ip in IPRange)
                    {
                        // キャンセルされたかチェック
                        token.ThrowIfCancellationRequested();

                        // Pingクラスを使用してオンラインかどうかを確認
                        using (var ping = new System.Net.NetworkInformation.Ping())
                        {
                            try
                            {
                                // オンラインのみ処理する
                                var reply = ping.Send(ip, PING_TIMEOUT);

                                // Pingの結果が成功ならホスト名・MACアドレス・応答時間を取得してListViewに追加
                                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    // 応答時間を取得（PingReplyから直接取れる）
                                    string responseTime = reply.RoundtripTime.ToString();

                                    // ARPキャッシュへの登録を待つ
                                    Thread.Sleep(ARP_CACHE_WAIT);

                                    // ホスト名を取得（DNS → NetBIOS の順で試みる。どちらも失敗なら「取得不可」）
                                    string hostName = HostNameResolver.GetHostName(ip);

                                    // MACアドレスの取得
                                    string macAddress = MacAddressResolver.GetMacAddress(ip);

                                    // Task.Run内はバックグラウンドスレッドのため、UI操作はInvokeを経由してUIスレッドで行う
                                    this.Invoke((Action)(() =>
                                    {
                                        // ListViewに追加（オンラインのみ）
                                        ListViewResult.Items.Add(new ListViewItem(new[]
                                        {
                                            ip,
                                            hostName,
                                            macAddress,
                                            responseTime,
                                            STATUS_ONLINE
                                        }));

                                        // 進捗を+1（オンラインIPの追加と同じタイミングで更新）
                                        ProgressBarScan.Value = Math.Min(ProgressBarScan.Value + 1, ProgressBarScan.Maximum);
                                    }));

                                    System.Diagnostics.Debug.WriteLine($"{ip}  →  OK  応答時間:{responseTime}ms  ホスト名:{hostName}  MACアドレス:{macAddress}");
                                }
                                else
                                {
                                    // オフラインのIPも進捗としてカウントする
                                    this.Invoke((Action)(() =>
                                    {
                                        ProgressBarScan.Value = Math.Min(ProgressBarScan.Value + 1, ProgressBarScan.Maximum);
                                    }));

                                    // オフラインはデバッグ出力のみ（ListViewには追加しない）
                                    System.Diagnostics.Debug.WriteLine($"{ip}  →  NG");
                                }
                            }
                            // バックグラウンドスレッド内はUI操作不可のため、外側のcatch（UIスレッド）に処理を任せる
                            catch (OperationCanceledException)
                            {
                                throw; // 外側のcatchに伝えるため再スロー(UI操作できないため)
                            }
                            // その他の例外はログに出力してスキャンを続行
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"{ip}  →  エラー: {ex.Message}");
                            }
                        }
                    }
                });

                // 正常完了時のみ表示
                System.Diagnostics.Debug.WriteLine(MSG_SCAN_COMPLETE);
                MessageBox.Show(MSG_SCAN_COMPLETE);
            }
            // 中止ボタンによるキャンセル → 結果は出力しない、途中結果も消す
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine(MSG_SCAN_CANCEL);
                ListViewResult.Items.Clear();
                MessageBox.Show(MSG_SCAN_CANCEL);
            }
            finally
            {
                // 完了・中止どちらでもボタン状態と進捗バーを復元
                BtnScan.Enabled = true;
                BtnStop.Enabled = false;
                ProgressBarScan.Value = PROGRESS_INITIAL_VALUE; // 進捗バーをリセット

                // CancellationTokenSourceは使い終わったら破棄
                cts.Dispose();
                cts = null;
            }
        }

        // スキャン停止ボタンのクリックイベントハンドラー
        private void BtnStop_Click(object sender, EventArgs e)
        {
            cts?.Cancel(); // キャンセル要求を送る
        }
    }

    // ListViewの列インデックスを1か所にまとめて管理するクラス
    internal static class ColIndex
    {
        public const int IpAddress = 0;
        public const int HostName = 1;
        public const int MacAddress = 2;
        public const int ResponseTime = 3;
        public const int Status = 4;
    }
}