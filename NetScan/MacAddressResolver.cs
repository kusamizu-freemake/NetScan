#nullable enable
using System.Linq;
using System.Text.RegularExpressions;

namespace NetScan
{
    // MACアドレスを取得するための機能をまとめたクラス（自NIC → ARP の順で試みる）
    // 状態（フィールド）を持たないので、static classにしている
    internal static class MacAddressResolver
    {
        // MACアドレスを抽出する正規表現（出力例：xx-xx-xx-xx-xx-xx）
        private const string MAC_ADDRESS_PATTERN = @"([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}";

        // MACアドレスが取得できなかった場合の表示文字列
        private const string STATUS_MAC_UNKNOWN = "取得不可";

        // MACアドレスを取得する（自NIC → ARP の順で試みる）
        public static string GetMacAddress(string ipAddress)
        {
            // ① 自PCのNICから取得を試みる（自PCはARPキャッシュに載らないため）
            string? localMac = GetMacAddressFromLocalNic(ipAddress);
            if (localMac != null)
                return localMac;

            // ② 自PC以外 → arp -a で取得を試みる
            return GetMacAddressFromArp(ipAddress);
        }

        // 自PCのNICからMACアドレスを取得する（対象IPが自PCでない場合はnullを返す）
        private static string? GetMacAddressFromLocalNic(string ipAddress)
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
        private static string GetMacAddressFromArp(string ipAddress)
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
            catch (System.Exception ex)
            {
                // 例外の内容を表示
                System.Diagnostics.Debug.WriteLine($"[ARP] 例外発生: {ex.Message}");
                return STATUS_MAC_UNKNOWN;
            }
        }
    }
}
