#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetScan
{
    public partial class NetScanForm : Form
    {
        private CancellationTokenSource? cts; // スキャン停止のためのCancellationTokenSource

        // ListViewの列名
        private const string COL_IP_ADDRESS = "IPアドレス";
        private const string COL_HOST_NAME = "ホスト名";
        private const string COL_MAC_ADDRESS = "MACアドレス";
        private const string COL_STATUS = "状態";

        // ListViewのレイアウト設定
        private const int COL_IP_ADDRESS_WIDTH = 150;
        private const int COL_HOST_NAME_WIDTH = 200;
        private const int COL_MAC_ADDRESS_WIDTH = 150;
        private const int COL_STATUS_WIDTH = 80;

        // スキャン設定
        private const int PING_TIMEOUT = 1000;              // Pingのタイムアウト（ミリ秒）
        private const int ARP_CACHE_WAIT = 100;             // ARPキャッシュの更新待ち時間（ミリ秒）
        private const int NETBIOS_HOSTNAME_TIMEOUT = 1000;  // NetBIOSホスト名の取得タイムアウト（ミリ秒）

        // 正規表現パターン
        // NetBIOS名前テーブルからコンピューター名を抽出する正規表現
        // <00> はワークステーションサービス（コンピューター名）を示す
        // 例: MYPC            <00>  UNIQUE  Registered
        private const string NETBIOS_HOSTNAME_PATTERN = @"^\s*(\S+)\s+<00>\s+UNIQUE";
        // MACアドレスを抽出する正規表現（出力例：xx-xx-xx-xx-xx-xx）
        private const string MAC_ADDRESS_PATTERN = @"([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}";

        // スキャン結果の表示文字列
        private const string STATUS_ONLINE = "オンライン";
        private const string STATUS_HOSTNAME_UNKNOWN = "取得不可";
        private const string STATUS_MAC_UNKNOWN = "取得不可";
        private const string STATUS_IP_RANGE_INVALID = "開始IPと終了IPは異なる必要があります。";
        private const string MSG_SCAN_COMPLETE = "スキャン完了しました。";
        private const string MSG_SCAN_CANCEL = "スキャンを中止しました。";

        public NetScanForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化
        }

        // ListViewの列を設定
        private void NetScanForm_Load(object sender, EventArgs e)
        {
            ListViewResult.Columns.Add(COL_IP_ADDRESS, COL_IP_ADDRESS_WIDTH);
            ListViewResult.Columns.Add(COL_HOST_NAME, COL_HOST_NAME_WIDTH);
            ListViewResult.Columns.Add(COL_MAC_ADDRESS, COL_MAC_ADDRESS_WIDTH);
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

            // 初期化
            ListViewResult.Items.Clear();
            BtnScan.Enabled = false;
            BtnStop.Enabled = true;

            // CancellationTokenSourceを新規作成
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

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
                                var reply = ping.Send(ip, PING_TIMEOUT);

                                // Pingの結果が成功ならホスト名とMACアドレスを取得してListViewに追加
                                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    // ARPキャッシュへの登録を待つ
                                    Thread.Sleep(ARP_CACHE_WAIT);

                                    // ホスト名を取得（DNS → NetBIOS の順で試みる。どちらも失敗なら「取得不可」）
                                    string hostName = GetHostName(ip);

                                    // MACアドレスの取得
                                    string macAddress = GetMacAddress(ip);

                                    // Task.Run内はバックグラウンドスレッドのため、UI操作はInvokeを経由してUIスレッドで行う
                                    this.Invoke((Action)(() =>
                                    {
                                        // ListViewに追加（オンラインのみ）
                                        ListViewResult.Items.Add(new ListViewItem(new[]
                                        {
                                            ip,
                                            hostName,
                                            macAddress,
                                            STATUS_ONLINE
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
                // 完了・中止どちらでもボタン状態を復元
                BtnScan.Enabled = true;
                BtnStop.Enabled = false;

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

        // ホスト名を取得する（DNS → NetBIOS の順で試みる）
        private string GetHostName(string ipAddress)
        {
            try
            {
                // ①DNS逆引きで取得を試みる
                return System.Net.Dns.GetHostEntry(ipAddress).HostName;
            }
            catch
            {
                // ②DNS逆引き失敗 → NetBIOSで取得を試みる
                return GetHostNameByNetBios(ipAddress);
            }
        }

        // NetBIOS（nbtstat -A）でホスト名を取得する
        private string GetHostNameByNetBios(string ipAddress)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = @"C:\Windows\System32\nbtstat.exe",
                        Arguments = $"-A {ipAddress}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                // タイムアウト1秒（応答しない機器で長時間ブロックされないように）

                bool finished = process.WaitForExit(NETBIOS_HOSTNAME_TIMEOUT);
                string output = finished ? process.StandardOutput.ReadToEnd() : "";

                // タイムアウトした場合はプロセスを強制終了
                if (!finished)
                    process.Kill();

                System.Diagnostics.Debug.WriteLine($"[NetBIOS] {ipAddress} の出力:\n{output}");

                // コンピューター名の行を探す（例: MYPC            <00>  UNIQUE  Registered）
                // <00> はワークステーションサービス（コンピューター名）を示す
                var match = Regex.Match(output, NETBIOS_HOSTNAME_PATTERN, RegexOptions.Multiline);

                if (match.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[NetBIOS] マッチ成功: {match.Groups[1].Value}");
                    return match.Groups[1].Value;
                }

                System.Diagnostics.Debug.WriteLine("[NetBIOS] マッチ失敗（ホスト名が見つからなかった）");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetBIOS] 例外発生: {ex.Message}");
            }

            // DNS・NetBIOSどちらも失敗
            return STATUS_HOSTNAME_UNKNOWN;
        }

        // MACアドレスを取得する（自NIC → ARP の順で試みる）
        private string GetMacAddress(string ipAddress)
        {
            // ① 自PCのNICから取得を試みる（自PCはARPキャッシュに載らないため）
            string? localMac = GetMacAddressFromLocalNic(ipAddress);
            if (localMac != null)
                return localMac;

            // ② 自PC以外 → arp -a で取得を試みる
            return GetMacAddressFromArp(ipAddress);
        }

        // 自PCのNICからMACアドレスを取得する（対象IPが自PCでない場合はnullを返す）
        private string? GetMacAddressFromLocalNic(string ipAddress)
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                // 無効なインターフェースはスキップ
                if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    // IPv4のみ対象
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;

                    if (addr.Address.ToString() == ipAddress)
                    {
                        // 自PCのIPと一致 → MACアドレスを直接取得
                        string localMac = nic.GetPhysicalAddress().ToString();

                        // 取得できなかった場合（仮想NICなど）
                        if (string.IsNullOrEmpty(localMac))
                            return STATUS_MAC_UNKNOWN;

                        // 形式を xx-xx-xx-xx-xx-xx に整える
                        return string.Join("-", Enumerable.Range(0, 6)
                            .Select(i => localMac.Substring(i * 2, 2)));
                    }
                }
            }

            // 自PCのIPと一致するNICが見つからなかった
            return null;
        }

        // arp -a コマンドでMACアドレスを取得する
        private string GetMacAddressFromArp(string ipAddress)
        {
            try
            {
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
                process.Start();
                string output = process.StandardOutput.ReadToEnd(); // 実行結果を文字列として受け取る
                process.WaitForExit();

                // ARPの生の出力をデバッグコンソールに表示
                System.Diagnostics.Debug.WriteLine($"[ARP] {ipAddress} の出力:\n{output}");

                // MACアドレスを正規表現で抽出（出力例：xx-xx-xx-xx-xx-xx）
                var match = Regex.Match(output, MAC_ADDRESS_PATTERN);

                // 正規表現のマッチ結果を表示
                System.Diagnostics.Debug.WriteLine(
                    match.Success ? $"[ARP] マッチ成功: {match.Value}" : "[ARP] マッチ失敗（MACアドレスが見つからなかった）");

                return match.Success ? match.Value : STATUS_MAC_UNKNOWN;
            }
            catch (Exception ex)
            {
                // 例外の内容を表示
                System.Diagnostics.Debug.WriteLine($"[ARP] 例外発生: {ex.Message}");
                return STATUS_MAC_UNKNOWN;
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