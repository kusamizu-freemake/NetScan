#nullable enable
using System.Text.RegularExpressions;

namespace NetScan
{
    // ホスト名を取得するための機能をまとめたクラス（DNS → NetBIOS の順で試みる）
    // 状態（フィールド）を持たないので、static classにしている
    internal static class HostNameResolver
    {
        // NetBIOSホスト名の取得タイムアウト（ミリ秒）
        private const int NETBIOS_HOSTNAME_TIMEOUT = 1000;

        // NetBIOS名前テーブルからコンピューター名を抽出する正規表現
        // <00> はワークステーションサービス（コンピューター名）を示す
        // 例: MYPC            <00>  UNIQUE  Registered
        private const string NETBIOS_HOSTNAME_PATTERN = @"^\s*(\S+)\s+<00>\s+UNIQUE";

        // ホスト名が取得できなかった場合の表示文字列
        private const string STATUS_HOSTNAME_UNKNOWN = "取得不可";

        // ホスト名を取得する（DNS → NetBIOS の順で試みる）
        public static string GetHostName(string ipAddress)
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
        private static string GetHostNameByNetBios(string ipAddress)
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

                // 先に出力を全部読んでから終了を待つ（逆順だと出力が途中で切れる可能性あり）
                string output = process.StandardOutput.ReadToEnd();

                // タイムアウト1秒（応答しない機器で長時間ブロックされないように）
                bool finished = process.WaitForExit(NETBIOS_HOSTNAME_TIMEOUT);

                // タイムアウトした場合はプロセスを強制終了
                if (!finished)
                    process.Kill();

                System.Diagnostics.Debug.WriteLine($"[NetBIOS] {ipAddress} の出力:\n{output}");

                // コンピューター名の行を探す（例: MYPC            <00>  UNIQUE  Registered）
                var match = Regex.Match(output, NETBIOS_HOSTNAME_PATTERN, RegexOptions.Multiline);

                if (match.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[NetBIOS] マッチ成功: {match.Groups[1].Value}");
                    return match.Groups[1].Value;
                }

                System.Diagnostics.Debug.WriteLine("[NetBIOS] マッチ失敗（ホスト名が見つからなかった）");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetBIOS] 例外発生: {ex.Message}");
            }

            // DNS・NetBIOSどちらも失敗
            return STATUS_HOSTNAME_UNKNOWN;
        }
    }
}
