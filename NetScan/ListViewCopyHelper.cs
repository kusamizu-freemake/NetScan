#nullable enable
using System;
using System.Linq;
using System.Windows.Forms;

namespace NetScan
{
    // ListViewの右クリックメニュー（コピー機能）をまとめて管理するクラス
    // コンストラクタでListViewとメニュー部品を受け取り、必要なイベントをこのクラスの中で組み立てる
    internal class ListViewCopyHelper
    {
        private readonly ListView listView;
        private readonly ToolStripMenuItem menuItemCopy;
        private readonly ToolStripMenuItem menuItemCopyCell;

        // 右クリックした列（セル）の番号を覚えておく（-1は「列を特定できなかった」という意味）
        private int RightClickedColumnIndex = -1;

        public ListViewCopyHelper(
            ListView listView,
            ContextMenuStrip contextMenuStrip,
            ToolStripMenuItem menuItemCopy,
            ToolStripMenuItem menuItemCopyCell)
        {
            this.listView = listView;
            this.menuItemCopy = menuItemCopy;
            this.menuItemCopyCell = menuItemCopyCell;

            // 必要なイベントをここでまとめて登録する
            listView.MouseUp += ListView_MouseUp;
            contextMenuStrip.Opening += ContextMenuStrip_Opening;
            menuItemCopy.Click += MenuItemCopy_Click;
            menuItemCopyCell.Click += MenuItemCopyCell_Click;
        }

        // ListViewを右クリックした時、その位置の行を選択状態にし、クリックした列も記録する
        // （右クリックだけだと行が選択されない場合があるための対応）
        private void ListView_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            // クリックした位置から「行」と「列（セル）」を取得する
            var hit = listView.HitTest(e.Location);
            var item = hit.Item;

            // 行とセルの両方が見つかった場合は、そのセルが何番目の列かを調べる
            if (item != null && hit.SubItem != null)
            {
                // 例：IPアドレスの列なら0、ホスト名の列なら1、という番号が入る
                RightClickedColumnIndex = item.SubItems.IndexOf(hit.SubItem);
            }
            else
            {
                // 列が特定できなかった場合は-1にしておく
                RightClickedColumnIndex = -1;
            }

            if (item == null)
            {
                // 何もない場所を右クリックした場合は選択を解除する
                listView.SelectedItems.Clear();
                return;
            }

            // すでに選択されている行（複数選択中）はそのままにする
            if (!item.Selected)
            {
                listView.SelectedItems.Clear();
                item.Selected = true;
                item.Focused = true;
            }
        }

        // 右クリックメニューを開く直前の処理：選択行や列の状態に応じて各項目の有効/無効を切り替える
        private void ContextMenuStrip_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 行が1つ以上選択されているかどうか
            bool hasSelection = listView.SelectedItems.Count > 0;

            // 右クリックした列が分かっているかどうか
            bool hasColumn = RightClickedColumnIndex >= 0;

            // 「行をコピー」：行が選択されていれば押せるようにする
            menuItemCopy.Enabled = hasSelection;

            // 「セルをコピー」：行が選択されていて、かつ列も分かっている時だけ押せるようにする
            menuItemCopyCell.Enabled = hasSelection && hasColumn;
        }

        // 「セルをコピー」メニューをクリックした時の処理
        // 右クリックした列と同じ列の値だけをコピーする（複数行選択時は改行区切り）
        private void MenuItemCopyCell_Click(object? sender, EventArgs e)
        {
            // 選択されている行がない場合は何もしない
            if (listView.SelectedItems.Count == 0)
                return;

            // 右クリックした列が分からない場合も何もしない
            if (RightClickedColumnIndex < 0)
                return;

            // コピーする文字列を組み立てるための入れ物
            var sb = new System.Text.StringBuilder();

            // 選択されている行を1つずつ確認する
            foreach (ListViewItem item in listView.SelectedItems)
            {
                // 指定した列がその行に存在するか確認する（念のため）
                if (RightClickedColumnIndex < item.SubItems.Count)
                {
                    // その列の値を1行分追加する
                    sb.AppendLine(item.SubItems[RightClickedColumnIndex].Text);
                }
            }

            // 末尾の改行は不要なので取り除く（1件だけコピーした時に余計な改行が入らないように）
            string copyText = sb.ToString().TrimEnd('\r', '\n');
            Clipboard.SetText(copyText);
        }

        // 「行をコピー（全列）」メニューをクリックした時の処理
        private void MenuItemCopy_Click(object? sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;

            var sb = new System.Text.StringBuilder();

            foreach (ListViewItem item in listView.SelectedItems)
            {
                // 各列の値をタブ区切りでつなぐ（Excelなどに貼り付けやすい形式）
                var values = item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text);
                sb.AppendLine(string.Join("\t", values));
            }

            Clipboard.SetText(sb.ToString());
        }
    }
}
