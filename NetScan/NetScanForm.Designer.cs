namespace NetScan
{
    partial class NetScanForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.TxtEndIP = new System.Windows.Forms.TextBox();
            this.TxtStartIP = new System.Windows.Forms.MaskedTextBox();
            this.BtnScan = new System.Windows.Forms.Button();
            this.BtnStop = new System.Windows.Forms.Button();
            this.ListViewResult = new System.Windows.Forms.ListView();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TxtEndIP
            // 
            this.TxtEndIP.Location = new System.Drawing.Point(66, 75);
            this.TxtEndIP.Name = "TxtEndIP";
            this.TxtEndIP.Size = new System.Drawing.Size(139, 22);
            this.TxtEndIP.TabIndex = 0;
            this.TxtEndIP.Text = " 192.168.1.254";
            // 
            // TxtStartIP
            // 
            this.TxtStartIP.Location = new System.Drawing.Point(66, 35);
            this.TxtStartIP.Name = "TxtStartIP";
            this.TxtStartIP.Size = new System.Drawing.Size(139, 22);
            this.TxtStartIP.TabIndex = 1;
            this.TxtStartIP.Text = " 192.168.1.1";
            // 
            // BtnScan
            // 
            this.BtnScan.Location = new System.Drawing.Point(15, 121);
            this.BtnScan.Name = "BtnScan";
            this.BtnScan.Size = new System.Drawing.Size(100, 30);
            this.BtnScan.TabIndex = 3;
            this.BtnScan.Text = "スキャン開始";
            this.BtnScan.UseVisualStyleBackColor = true;
            // 
            // BtnStop
            // 
            this.BtnStop.Location = new System.Drawing.Point(15, 162);
            this.BtnStop.Name = "BtnStop";
            this.BtnStop.Size = new System.Drawing.Size(100, 30);
            this.BtnStop.TabIndex = 4;
            this.BtnStop.Text = "中止";
            this.BtnStop.UseVisualStyleBackColor = true;
            // 
            // ListViewResult
            // 
            this.ListViewResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ListViewResult.FullRowSelect = true;
            this.ListViewResult.GridLines = true;
            this.ListViewResult.HideSelection = false;
            this.ListViewResult.Location = new System.Drawing.Point(0, 322);
            this.ListViewResult.Name = "ListViewResult";
            this.ListViewResult.Size = new System.Drawing.Size(682, 131);
            this.ListViewResult.TabIndex = 6;
            this.ListViewResult.UseCompatibleStateImageBehavior = false;
            this.ListViewResult.View = System.Windows.Forms.View.Details;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(15, 40);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(50, 15);
            this.Label1.TabIndex = 7;
            this.Label1.Text = "開始IP";
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(15, 78);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(50, 15);
            this.Label2.TabIndex = 8;
            this.Label2.Text = "終了IP";
            // 
            // NetScanForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 453);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.ListViewResult);
            this.Controls.Add(this.BtnStop);
            this.Controls.Add(this.BtnScan);
            this.Controls.Add(this.TxtStartIP);
            this.Controls.Add(this.TxtEndIP);
            this.MinimumSize = new System.Drawing.Size(700, 500);
            this.Name = "NetScanForm";
            this.Text = "NetScan";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.NetScanForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TxtEndIP;
        private System.Windows.Forms.MaskedTextBox TxtStartIP;
        private System.Windows.Forms.Button BtnScan;
        private System.Windows.Forms.Button BtnStop;
        private System.Windows.Forms.ListView ListViewResult;
        private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.Label Label2;
    }
}

