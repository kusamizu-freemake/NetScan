using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetScan
{
    public partial class NetScanForm : Form
    {

        public NetScanForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化
        }

        // ListViewの列を設定
        private void NetScanForm_Load(object sender, EventArgs e)
        {
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.IPAddress, AppConstants.ListViewLayout.IPAddressWidth);
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.HostName, AppConstants.ListViewLayout.HostNameWidth);
            ListViewResult.Columns.Add(AppConstants.ListViewColumns.Status, AppConstants.ListViewLayout.StatusWidth);
        }
    }
}