namespace zepp_os_push_to_simulator_cs
{
    partial class MainWindow
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.pick_zpk_Btn = new System.Windows.Forms.Button();
            this.zpk_loc_Label = new System.Windows.Forms.Label();
            this.send_Btn = new System.Windows.Forms.Button();
            this.content_Label = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.json_loc_Label = new System.Windows.Forms.Label();
            this.addressTextbox = new System.Windows.Forms.TextBox();
            this.addressLabel = new System.Windows.Forms.Label();
            this.convertBtn = new System.Windows.Forms.Button();
            this.tokenTextBox = new System.Windows.Forms.TextBox();
            this.tokenLabel = new System.Windows.Forms.Label();
            this.devlistCombo = new System.Windows.Forms.ComboBox();
            this.devListLabel = new System.Windows.Forms.Label();
            this.convertOptGroup = new System.Windows.Forms.GroupBox();
            this.convertOptGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // pick_zpk_Btn
            // 
            this.pick_zpk_Btn.Location = new System.Drawing.Point(28, 21);
            this.pick_zpk_Btn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pick_zpk_Btn.Name = "pick_zpk_Btn";
            this.pick_zpk_Btn.Size = new System.Drawing.Size(230, 66);
            this.pick_zpk_Btn.TabIndex = 0;
            this.pick_zpk_Btn.Text = "Pick ZPK/ZIP With Extracted Folder";
            this.pick_zpk_Btn.UseVisualStyleBackColor = true;
            this.pick_zpk_Btn.Click += new System.EventHandler(this.pick_zpk_Btn_Click);
            // 
            // zpk_loc_Label
            // 
            this.zpk_loc_Label.AutoSize = true;
            this.zpk_loc_Label.Location = new System.Drawing.Point(24, 92);
            this.zpk_loc_Label.MaximumSize = new System.Drawing.Size(466, 0);
            this.zpk_loc_Label.Name = "zpk_loc_Label";
            this.zpk_loc_Label.Size = new System.Drawing.Size(73, 21);
            this.zpk_loc_Label.TabIndex = 1;
            this.zpk_loc_Label.Text = "Package: ";
            // 
            // send_Btn
            // 
            this.send_Btn.Location = new System.Drawing.Point(371, 378);
            this.send_Btn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.send_Btn.Name = "send_Btn";
            this.send_Btn.Size = new System.Drawing.Size(112, 40);
            this.send_Btn.TabIndex = 2;
            this.send_Btn.Text = "Connect";
            this.send_Btn.UseVisualStyleBackColor = true;
            this.send_Btn.Click += new System.EventHandler(this.send_Btn_Click);
            // 
            // content_Label
            // 
            this.content_Label.AutoSize = true;
            this.content_Label.Location = new System.Drawing.Point(24, 423);
            this.content_Label.MaximumSize = new System.Drawing.Size(466, 0);
            this.content_Label.Name = "content_Label";
            this.content_Label.Size = new System.Drawing.Size(135, 21);
            this.content_Label.TabIndex = 3;
            this.content_Label.Text = "Selected Package: ";
            // 
            // json_loc_Label
            // 
            this.json_loc_Label.AutoSize = true;
            this.json_loc_Label.Location = new System.Drawing.Point(24, 178);
            this.json_loc_Label.MaximumSize = new System.Drawing.Size(466, 0);
            this.json_loc_Label.Name = "json_loc_Label";
            this.json_loc_Label.Size = new System.Drawing.Size(56, 21);
            this.json_loc_Label.TabIndex = 5;
            this.json_loc_Label.Text = "JSON: ";
            // 
            // addressTextbox
            // 
            this.addressTextbox.Location = new System.Drawing.Point(107, 378);
            this.addressTextbox.Name = "addressTextbox";
            this.addressTextbox.Size = new System.Drawing.Size(257, 29);
            this.addressTextbox.TabIndex = 6;
            this.addressTextbox.Text = "http://localhost:7650";
            // 
            // addressLabel
            // 
            this.addressLabel.AutoSize = true;
            this.addressLabel.Location = new System.Drawing.Point(24, 381);
            this.addressLabel.Name = "addressLabel";
            this.addressLabel.Size = new System.Drawing.Size(77, 21);
            this.addressLabel.TabIndex = 3;
            this.addressLabel.Text = "Sim URL: ";
            // 
            // convertBtn
            // 
            this.convertBtn.Location = new System.Drawing.Point(268, 21);
            this.convertBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.convertBtn.Name = "convertBtn";
            this.convertBtn.Size = new System.Drawing.Size(230, 66);
            this.convertBtn.TabIndex = 7;
            this.convertBtn.Text = "Pick QR/ZPK/ZIP and Convert";
            this.convertBtn.UseVisualStyleBackColor = true;
            this.convertBtn.Click += new System.EventHandler(this.convertBtn_Click);
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(147, 59);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.Size = new System.Drawing.Size(324, 29);
            this.tokenTextBox.TabIndex = 9;
            this.tokenTextBox.Text = "random";
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(12, 62);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(106, 21);
            this.tokenLabel.TabIndex = 8;
            this.tokenLabel.Text = "Token For QR:";
            // 
            // devlistCombo
            // 
            this.devlistCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.devlistCombo.FormattingEnabled = true;
            this.devlistCombo.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.devlistCombo.Location = new System.Drawing.Point(147, 25);
            this.devlistCombo.Name = "devlistCombo";
            this.devlistCombo.Size = new System.Drawing.Size(324, 29);
            this.devlistCombo.TabIndex = 10;
            this.devlistCombo.SelectedIndexChanged += new System.EventHandler(this.devlistCombo_SelectedIndexChanged);
            // 
            // devListLabel
            // 
            this.devListLabel.AutoSize = true;
            this.devListLabel.Location = new System.Drawing.Point(12, 25);
            this.devListLabel.MaximumSize = new System.Drawing.Size(466, 0);
            this.devListLabel.Name = "devListLabel";
            this.devListLabel.Size = new System.Drawing.Size(129, 21);
            this.devListLabel.TabIndex = 11;
            this.devListLabel.Text = "Device Overwrite";
            // 
            // convertOptGroup
            // 
            this.convertOptGroup.Controls.Add(this.tokenTextBox);
            this.convertOptGroup.Controls.Add(this.devlistCombo);
            this.convertOptGroup.Controls.Add(this.tokenLabel);
            this.convertOptGroup.Controls.Add(this.devListLabel);
            this.convertOptGroup.Location = new System.Drawing.Point(12, 268);
            this.convertOptGroup.Name = "convertOptGroup";
            this.convertOptGroup.Size = new System.Drawing.Size(486, 102);
            this.convertOptGroup.TabIndex = 12;
            this.convertOptGroup.TabStop = false;
            this.convertOptGroup.Text = "Convert Options";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 561);
            this.Controls.Add(this.convertOptGroup);
            this.Controls.Add(this.convertBtn);
            this.Controls.Add(this.addressTextbox);
            this.Controls.Add(this.json_loc_Label);
            this.Controls.Add(this.addressLabel);
            this.Controls.Add(this.content_Label);
            this.Controls.Add(this.send_Btn);
            this.Controls.Add(this.zpk_loc_Label);
            this.Controls.Add(this.pick_zpk_Btn);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainWindow";
            this.Text = "Zepp OS Push to Simulator";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.convertOptGroup.ResumeLayout(false);
            this.convertOptGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button pick_zpk_Btn;
        private System.Windows.Forms.Label zpk_loc_Label;
        private System.Windows.Forms.Button send_Btn;
        private System.Windows.Forms.Label content_Label;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label json_loc_Label;
        private System.Windows.Forms.TextBox addressTextbox;
        private System.Windows.Forms.Label addressLabel;
        private System.Windows.Forms.Button convertBtn;
        private System.Windows.Forms.TextBox tokenTextBox;
        private System.Windows.Forms.Label tokenLabel;
        private System.Windows.Forms.ComboBox devlistCombo;
        private System.Windows.Forms.Label devListLabel;
        private System.Windows.Forms.GroupBox convertOptGroup;
    }
}

