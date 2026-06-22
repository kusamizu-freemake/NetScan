#nullable enable
using System;
using System.Collections.Generic;

namespace NetScan
{
    // 開始IP〜終了IPの範囲をリスト化するための機能をまとめたクラス
    // 状態（フィールド）を持たないので、static classにしている
    internal static class IpRangeHelper
    {
        // IP範囲をリスト化するメソッド
        public static List<string> GetIPRange(string startIp, string endIp)
        {
            List<string> ipRange = new List<string>();

            // IPアドレスを数値に変換(uint型を採用,int型では範囲が足りない)
            uint startIpNum = IPToUInt(startIp);
            uint endIpNum = IPToUInt(endIp);

            // 開始IPから終了IPまでループしてリストに追加
            for (uint i = startIpNum; i <= endIpNum; i++)
            {
                ipRange.Add(UIntToIP(i));
            }
            return ipRange;
        }

        // IPアドレス→uintに変換（IPアドレスを数値化して範囲計算を容易にするため）
        private static uint IPToUInt(string ipAddress)
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
        private static string UIntToIP(uint ipNum)
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
