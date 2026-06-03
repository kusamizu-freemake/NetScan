using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetScan
{
    public partial class NetScanForm : Form
    {
        private CancellationTokenSource _cts; // スキャン停止のためのCancellationTokenSource

        public NetScanForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化
        }

        // ListViewの列を設定
        private void NetScanForm_Load(object sender, EventArgs e)
        {
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.IPAddress, AppConstants.ListViewLayout.IPAddressWidth);
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.HostName, AppConstants.ListViewLayout.HostNameWidth);
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.MacAddress, AppConstants.ListViewLayout.MacAddressWidth); // MACアドレス
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.Status, AppConstants.ListViewLayout.StatusWidth);

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
                MessageBox.Show(AppConstants.ScanStatus.IPRangeInvalid);
                return;
            }

            // 初期化
            ListViewResult.Items.Clear(); // スキャン結果をクリア
            BtnScan.Enabled = false; // スキャン開始ボタンを無効化
            BtnStop.Enabled = true; // スキャン停止ボタンを有効化


            // CancellationTokenSourceを新規作成
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            // IP範囲リストの作成
            List<string> IPRange = GetIPRange(StartIP, EndIP);
            System.Diagnostics.Debug.WriteLine($"スキャン対象: {IPRange.Count} 件");

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
                                var reply = ping.Send(ip, AppConstants.ScanConfig.PingTimeout);

                                // Pingの結果が成功ならホスト名とMACアドレスを取得してListViewに追加
                                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    // ホスト名を取得（取得できない場合は「取得不可」と表示）
                                    string hostName = "";
                                    try
                                    {
                                        hostName = System.Net.Dns.GetHostEntry(ip).HostName;
                                    }
                                    catch
                                    {
                                        hostName = AppConstants.ScanStatus.HostNameUnknown;
                                    }

                                    // MACアドレスの取得
                                    string macAddress = GetMacAddress(ip);

                                    // UIスレッドへの反映はInvokeで行う
                                    this.Invoke((Action)(() =>
                                    {
                                        // ListViewに追加（オンラインのみ）
                                        ListViewResult.Items.Add(new ListViewItem(new[]
                                        {
                                            ip,
                                            hostName,
                                            macAddress,
                                            AppConstants.ScanStatus.Online
                                        }));
                                    }));

                                    System.Diagnostics.Debug.WriteLine($"{ip}  →  OK  ホスト名:{hostName} MACアドレス:{macAddress}");
                                }
                                else
                                {
                                    // オフラインはデバッグ出力のみ（ListViewには追加しない）
                                    System.Diagnostics.Debug.WriteLine($"{ip}  →  NG");
                                }
                            }
                            // スキャン停止ボタンが押された場合は、ここでは処理せず外側の catch で中止処理を行う
                            catch (OperationCanceledException)
                            {
                                throw;
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
                System.Diagnostics.Debug.WriteLine(AppConstants.ScanStatus.ScanComplete);
                MessageBox.Show(AppConstants.ScanStatus.ScanCompleteMsg);
            }
            // 中止ボタンによるキャンセル → 結果は出力しない
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine(AppConstants.ScanStatus.ScanErrorMsg);
                ListViewResult.Items.Clear(); // 途中結果も消す
                MessageBox.Show(AppConstants.ScanStatus.ScanErrorMsg);
            }
            finally
            {
                // 完了・中止どちらでもボタン状態を復元
                BtnScan.Enabled = true;
                BtnStop.Enabled = false;

                // CancellationTokenSourceは使い終わったら破棄
                _cts.Dispose();
                _cts = null;
            }
        }



        // スキャン停止ボタンのクリックイベントハンドラー
        private void BtnStop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel(); // キャンセル要求を送る

        }

        // MACアドレスを取得する
        private string GetMacAddress(string ipAddress)
        {
            try
            {
                // arp -a コマンドを実行してMACアドレス取得
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "arp",
                        Arguments = $"-a {ipAddress}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
              
                    }
                };
                process.Start(); // コマンド実行開始
                string output = process.StandardOutput.ReadToEnd(); // 実行結果を文字列として受け取る
                process.WaitForExit(); // コマンドが終わるまで待つ

                // MACアドレスを正規表現で抽出（出力例：xx-xx-xx-xx-xx-xx）
                var match = Regex.Match(
                    output, @"([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}");

                return match.Success ? match.Value : AppConstants.ScanStatus.MacAddressUnknown;    
            }
            catch
            {
                return AppConstants.ScanStatus.MacAddressUnknown;
            }
        }



        // IP範囲をリスト化するメソッド
        private List<string> GetIPRange(string StartIP, string EndIP)
        {
            List<string> IPRange = new List<string>();
            
            // IPアドレスを数値に変換(uint型を採用,int型では範囲が足りない)
            uint StartIPNum = IPToUInt(StartIP);
            uint EndIPNum = IPToUInt(EndIP);
            
            // 開始IPから終了IPまでループしてリストに追加
            for (uint i = StartIPNum; i <= EndIPNum; i++)
            {
                IPRange.Add(UIntToIP(i));
            }
            return IPRange;
        }

        // IPアドレス→uintに変換（IPアドレスを数値化して範囲計算を容易にするため）
        private uint IPToUInt(string ipAddress)
        {
            string[] octets = ipAddress.Split('.');
            if (octets.Length != 4)
            {
                throw new ArgumentException("Invalid IP address format.");
            }

            uint ipNum = 0;
            for (int i = 0; i < 4; i++)
            {
                ipNum |= (uint.Parse(octets[i]) << (24 - (8 * i)));
            }
            return ipNum;
        }

        // uint→IPアドレスに変換（数値化したIPアドレスを元の形式に戻すため）
        private string UIntToIP(uint ipNum)
        {
            return string.Join(".", new[]
            {
                (ipNum >> 24) & 0xFF,
                (ipNum >> 16) & 0xFF,
                (ipNum >> 8) & 0xFF,
                ipNum & 0xFF
            });
        }
    }
}