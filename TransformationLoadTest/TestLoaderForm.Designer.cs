namespace TransformationLoadTest
{
    partial class LoadTestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadTestForm));
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.configTextbox = new System.Windows.Forms.TextBox();
            this.statusListBox = new System.Windows.Forms.ListBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.openBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.runBtn = new System.Windows.Forms.ToolStripButton();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.filenameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(12, 59);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.configTextbox);
            this.splitContainer.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.statusListBox);
            this.splitContainer.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer.Size = new System.Drawing.Size(993, 649);
            this.splitContainer.SplitterDistance = 466;
            this.splitContainer.TabIndex = 2;
            // 
            // configTextbox
            // 
            this.configTextbox.AcceptsReturn = true;
            this.configTextbox.AcceptsTab = true;
            this.configTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.configTextbox.Location = new System.Drawing.Point(3, 3);
            this.configTextbox.Multiline = true;
            this.configTextbox.Name = "configTextbox";
            this.configTextbox.Size = new System.Drawing.Size(987, 460);
            this.configTextbox.TabIndex = 2;
            this.configTextbox.Text = resources.GetString("configTextbox.Text");
            // 
            // statusListBox
            // 
            this.statusListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusListBox.BackColor = System.Drawing.SystemColors.Info;
            this.statusListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.statusListBox.FormattingEnabled = true;
            this.statusListBox.Location = new System.Drawing.Point(0, 3);
            this.statusListBox.Name = "statusListBox";
            this.statusListBox.Size = new System.Drawing.Size(993, 169);
            this.statusListBox.TabIndex = 1;
            this.statusListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.statusListBox_DrawItem);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openBtn,
            this.toolStripSeparator1,
            this.runBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1017, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // openBtn
            // 
            this.openBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openBtn.Image = ((System.Drawing.Image)(resources.GetObject("openBtn.Image")));
            this.openBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openBtn.Name = "openBtn";
            this.openBtn.Size = new System.Drawing.Size(23, 22);
            this.openBtn.Text = "Load File";
            this.openBtn.Click += new System.EventHandler(this.openBtn_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // runBtn
            // 
            this.runBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.runBtn.Image = ((System.Drawing.Image)(resources.GetObject("runBtn.Image")));
            this.runBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.runBtn.Name = "runBtn";
            this.runBtn.Size = new System.Drawing.Size(23, 22);
            this.runBtn.Text = "Run";
            this.runBtn.Click += new System.EventHandler(this.runBtn_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "csv";
            this.openFileDialog.FileName = "*.*";
            // 
            // filenameLabel
            // 
            this.filenameLabel.AutoSize = true;
            this.filenameLabel.Location = new System.Drawing.Point(12, 32);
            this.filenameLabel.Name = "filenameLabel";
            this.filenameLabel.Size = new System.Drawing.Size(55, 13);
            this.filenameLabel.TabIndex = 4;
            this.filenameLabel.Text = "Filename: ";
            // 
            // LoadTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1017, 720);
            this.Controls.Add(this.filenameLabel);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.splitContainer);
            this.Name = "LoadTestForm";
            this.Text = "Transformation Tester";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TextBox configTextbox;
        private System.Windows.Forms.ListBox statusListBox;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton openBtn;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton runBtn;
        private System.Windows.Forms.Label filenameLabel;
    }
}

