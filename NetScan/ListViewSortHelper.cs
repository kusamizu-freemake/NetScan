#nullable enable
using System;
using System.Collections;
using System.Windows.Forms;

namespace NetScan
{
    // ListViewの列ヘッダークリックによるソート機能をまとめたクラス
    // コンストラクタでListViewを受け取り、イベントをこのクラスの中で組み立てる
    internal class ListViewSortHelper
    {
        private readonly ListView listView;

        public ListViewSortHelper(ListView listView)
        {
            this.listView = listView;

            // イベントをここで登録する
            listView.ColumnClick += ListView_ColumnClick;
        }

        // 列ヘッダーをクリックした時のソート処理
        private void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            // ソート対象外の列はスキップ
            if (e.Column == ColIndex.Status)
                return;

            ApplySort(e.Column, GetNextSortOrder(e.Column));
        }

        // ソートを実行する（列インデックスとソート順を受け取る）
        private void ApplySort(int columnIndex, SortOrder sortOrder)
        {
            listView.ListViewItemSorter = new ListViewItemComparer(columnIndex, sortOrder);
            listView.Sort();
        }

        // 現在のソート状態を見て、次にクリックした時のソート順を返す
        // （同じ列を再クリックすると逆順になる。別の列なら昇順スタート）
        private SortOrder GetNextSortOrder(int columnIndex)
        {
            // 現在と同じ列をクリックした場合は順番を反転する
            if (listView.ListViewItemSorter is ListViewItemComparer currentSorter
                && currentSorter.ColumnIndex == columnIndex)
            {
                if (currentSorter.SortOrder == SortOrder.Ascending)
                    return SortOrder.Descending; // 現在が昇順なので、次は降順にする
                else
                    return SortOrder.Ascending;  // 現在が降順なので、次は昇順にする
            }

            // 別の列をクリックした場合は昇順スタート
            return SortOrder.Ascending;
        }
    }

    // ListViewの行を比較するクラス
    internal class ListViewItemComparer : IComparer
    {
        public int ColumnIndex { get; }
        public SortOrder SortOrder { get; }

        // 「何列目を・昇順/降順で並び替えるか」をセットするコンストラクタ
        public ListViewItemComparer(int columnIndex, SortOrder sortOrder)
        {
            ColumnIndex = columnIndex;
            SortOrder = sortOrder;
        }

        // 2行のデータ（x・y）を受け取り、どちらを上に表示するかを判定するメソッド
        public int Compare(object? x, object? y)
        {
            // xがListViewItem型かどうかを確認する
            // 型が合っていればitemXに値が入り、合っていなければitemXはnullになる
            ListViewItem? itemX = x as ListViewItem;

            // yも同様にListViewItem型かどうかを確認する
            ListViewItem? itemY = y as ListViewItem;

            // どちらかがListViewItemに変換できなかった場合は、
            // 比較できないので「同じ扱い（0）」として処理を終える
            if (itemX == null || itemY == null)
            {
                return 0;
            }

            // 比較対象の列（ColumnIndex）のセルから、文字列を取り出す
            string valueX = itemX.SubItems[ColumnIndex].Text;
            string valueY = itemY.SubItems[ColumnIndex].Text;

            // 比較結果（大小関係）を入れるための変数
            int result;

            // IPアドレス列なら
            if (ColumnIndex == ColIndex.IpAddress)
            {
                // IPアドレス列は4つの数値に分解して比較する
                // （文字列比較だと "192.168.1.9" > "192.168.1.10" になるため）
                result = CompareIpAddress(valueX, valueY);
            }
            // 応答時間列なら
            else if (ColumnIndex == ColIndex.ResponseTime)
            {
                // 応答時間列は数値として比較する（文字列比較だと "9" > "10" になるため）
                bool parsedX = long.TryParse(valueX, out long numX);
                bool parsedY = long.TryParse(valueY, out long numY);

                // 両方とも数値に変換できた場合だけ、数値として比較する
                if (parsedX && parsedY)
                {
                    // 数値として大小を比較する
                    result = numX.CompareTo(numY);
                }
                else
                {
                    // 数値への変換に失敗した場合は、文字列のまま比較する
                    result = string.Compare(valueX, valueY, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                // ホスト名・MACアドレスは文字列比較
                result = string.Compare(valueX, valueY, StringComparison.OrdinalIgnoreCase);
            }

            // 降順の場合は比較結果を反転する
            if (SortOrder == SortOrder.Descending)
                result = -result;

            // 最終的な比較結果をlistView.Sort()に返す
            return result;
        }

        // IPアドレスを4つの数値に分解して順番に比較するメソッド
        // 例：「192.168.1.9」と「192.168.1.10」を比較する場合
        //   → ドット（.）で分割して ["192","168","1","9"] と ["192","168","1","10"] にする
        private static int CompareIpAddress(string ipX, string ipY)
        {
            // ドット（.）を区切り文字としてIPアドレスを4つに分割する
            string[] partsX = ipX.Split('.'); // 例：「192.168.1.9」→ ["192", "168", "1", "9"]
            string[] partsY = ipY.Split('.'); // 例：「192.168.1.10」→ ["192", "168", "1", "10"]

            // 正常なIPアドレスは必ず4つに分割できる
            // 4つに分割できない場合は、想定外の文字列なので文字列のまま比較する
            if (partsX.Length != 4 || partsY.Length != 4)
            {
                return string.Compare(ipX, ipY, StringComparison.OrdinalIgnoreCase);
            }

            // 第1〜第4オクテットを順番に比較する（前のオクテットが同じなら次のオクテットへ）
            for (int i = 0; i < 4; i++)
            {

                bool parsedX = int.TryParse(partsX[i], out int numX);
                bool parsedY = int.TryParse(partsY[i], out int numY);

                // 正常なIPアドレスの各オクテットは必ず数値に変換できる
                // 変換できない場合は、想定外の文字列なので文字列のまま比較する
                if (parsedX == false || parsedY == false)
                {
                    return string.Compare(ipX, ipY, StringComparison.OrdinalIgnoreCase);
                }

                // 現在のオクテットを数値として比較する
                int cmp = numX.CompareTo(numY);

                // このオクテットで大小が決まったら結果を返し、同じなら次のオクテットを比較する
                if (cmp != 0)
                    return cmp;
            }

            // 全オクテットが同じだった場合（例：192.168.1.10 と 192.168.1.10）
            return 0;
        }
    }
}