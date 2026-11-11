namespace HTISOCreator.Forms
{
    partial class VerificationForm
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
            this.components = new System.ComponentModel.Container();
            this.isoLabel = new System.Windows.Forms.Label();
            this.isoTextBox = new System.Windows.Forms.TextBox();
            this.isoBrowseButton = new System.Windows.Forms.Button();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.sourceTextBox = new System.Windows.Forms.TextBox();
            this.sourceBrowseButton = new System.Windows.Forms.Button();
            this.levelHintLabel = new System.Windows.Forms.Label();
            this.hashCheckBox = new System.Windows.Forms.CheckBox();
            this.mountCheckBox = new System.Windows.Forms.CheckBox();
            this.namesLabel = new System.Windows.Forms.Label();
            this.namesComboBox = new System.Windows.Forms.ComboBox();
            this.verifyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressLabel = new System.Windows.Forms.Label();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.checksListView = new System.Windows.Forms.ListView();
            this.checkColumn = new System.Windows.Forms.ColumnHeader();
            this.resultColumn = new System.Windows.Forms.ColumnHeader();
            this.detailColumn = new System.Windows.Forms.ColumnHeader();
            this.differencesLabel = new System.Windows.Forms.Label();
            this.differencesListView = new System.Windows.Forms.ListView();
            this.typeColumn = new System.Windows.Forms.ColumnHeader();
            this.pathColumn = new System.Windows.Forms.ColumnHeader();
            this.saveReportButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // isoLabel
            //
            this.isoLabel.AutoSize = true;
            this.isoLabel.Location = new System.Drawing.Point(12, 17);
            this.isoLabel.Name = "isoLabel";
            this.isoLabel.Size = new System.Drawing.Size(50, 15);
            this.isoLabel.TabIndex = 0;
            this.isoLabel.Text = "ISO file:";
            //
            // isoTextBox
            //
            this.isoTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.isoTextBox.Location = new System.Drawing.Point(170, 14);
            this.isoTextBox.Name = "isoTextBox";
            this.isoTextBox.Size = new System.Drawing.Size(480, 23);
            this.isoTextBox.TabIndex = 1;
            this.isoTextBox.TextChanged += new System.EventHandler(this.Input_Changed);
            //
            // isoBrowseButton
            //
            this.isoBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.isoBrowseButton.Location = new System.Drawing.Point(656, 12);
            this.isoBrowseButton.Name = "isoBrowseButton";
            this.isoBrowseButton.Size = new System.Drawing.Size(92, 28);
            this.isoBrowseButton.TabIndex = 2;
            this.isoBrowseButton.Text = "&Browse...";
            this.isoBrowseButton.UseVisualStyleBackColor = true;
            this.isoBrowseButton.Click += new System.EventHandler(this.IsoBrowseButton_Click);
            //
            // sourceLabel
            //
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Location = new System.Drawing.Point(12, 51);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(145, 15);
            this.sourceLabel.TabIndex = 3;
            this.sourceLabel.Text = "Source folder (optional):";
            //
            // sourceTextBox
            //
            this.sourceTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceTextBox.Location = new System.Drawing.Point(170, 48);
            this.sourceTextBox.Name = "sourceTextBox";
            this.sourceTextBox.Size = new System.Drawing.Size(480, 23);
            this.sourceTextBox.TabIndex = 4;
            this.toolTip.SetToolTip(this.sourceTextBox, "The folder the image was created from. Leave empty to check the image structure only.");
            this.sourceTextBox.TextChanged += new System.EventHandler(this.Input_Changed);
            //
            // sourceBrowseButton
            //
            this.sourceBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceBrowseButton.Location = new System.Drawing.Point(656, 46);
            this.sourceBrowseButton.Name = "sourceBrowseButton";
            this.sourceBrowseButton.Size = new System.Drawing.Size(92, 28);
            this.sourceBrowseButton.TabIndex = 5;
            this.sourceBrowseButton.Text = "B&rowse...";
            this.sourceBrowseButton.UseVisualStyleBackColor = true;
            this.sourceBrowseButton.Click += new System.EventHandler(this.SourceBrowseButton_Click);
            //
            // levelHintLabel
            //
            this.levelHintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.levelHintLabel.AutoEllipsis = true;
            this.levelHintLabel.Location = new System.Drawing.Point(170, 76);
            this.levelHintLabel.Name = "levelHintLabel";
            this.levelHintLabel.Size = new System.Drawing.Size(578, 18);
            this.levelHintLabel.TabIndex = 6;
            this.levelHintLabel.Tag = "dim";
            //
            // hashCheckBox
            //
            this.hashCheckBox.AutoSize = true;
            this.hashCheckBox.Location = new System.Drawing.Point(170, 98);
            this.hashCheckBox.Name = "hashCheckBox";
            this.hashCheckBox.Size = new System.Drawing.Size(349, 19);
            this.hashCheckBox.TabIndex = 7;
            this.hashCheckBox.Text = "Compare file &contents with SHA-256 (reads every file again)";
            this.hashCheckBox.UseVisualStyleBackColor = true;
            this.hashCheckBox.CheckedChanged += new System.EventHandler(this.Input_Changed);
            //
            // mountCheckBox
            //
            this.mountCheckBox.AutoSize = true;
            this.mountCheckBox.Location = new System.Drawing.Point(170, 122);
            this.mountCheckBox.Name = "mountCheckBox";
            this.mountCheckBox.Size = new System.Drawing.Size(390, 19);
            this.mountCheckBox.TabIndex = 8;
            this.mountCheckBox.Text = "Also &mount with Windows and compare (briefly assigns a drive letter)";
            this.toolTip.SetToolTip(this.mountCheckBox, "Uses Windows\' own ISO driver as a second, independent reader. The image is dismounted afterwards.");
            this.mountCheckBox.UseVisualStyleBackColor = true;
            //
            // namesLabel
            //
            this.namesLabel.AutoSize = true;
            this.namesLabel.Location = new System.Drawing.Point(532, 100);
            this.namesLabel.Name = "namesLabel";
            this.namesLabel.Size = new System.Drawing.Size(67, 15);
            this.namesLabel.TabIndex = 19;
            this.namesLabel.Text = "File &names:";
            //
            // namesComboBox
            //
            this.namesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.namesComboBox.Items.AddRange(new object[] {
            "Must match originals (Strict)",
            "Approved conversions (Compatibility)",
            "Report differences only"});
            this.namesComboBox.Location = new System.Drawing.Point(604, 96);
            this.namesComboBox.Name = "namesComboBox";
            this.namesComboBox.Size = new System.Drawing.Size(144, 23);
            this.namesComboBox.TabIndex = 20;
            this.toolTip.SetToolTip(this.namesComboBox, "How names in the image are judged when a source folder is compared. Strict: every name must equal the original. " +
                "Compatibility: names must match HT ISO Creator's conversion plan exactly.");
            //
            // verifyButton
            //
            this.verifyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.verifyButton.Location = new System.Drawing.Point(170, 150);
            this.verifyButton.Name = "verifyButton";
            this.verifyButton.Size = new System.Drawing.Size(110, 30);
            this.verifyButton.TabIndex = 9;
            this.verifyButton.Tag = "primary";
            this.verifyButton.Text = "&Verify";
            this.verifyButton.UseVisualStyleBackColor = false;
            this.verifyButton.Click += new System.EventHandler(this.VerifyButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Enabled = false;
            this.cancelButton.Location = new System.Drawing.Point(286, 150);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(90, 30);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "Cance&l";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            //
            // progressBar
            //
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 190);
            this.progressBar.Maximum = 1000;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(736, 16);
            this.progressBar.TabIndex = 11;
            //
            // progressLabel
            //
            this.progressLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLabel.AutoEllipsis = true;
            this.progressLabel.Location = new System.Drawing.Point(12, 210);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(736, 18);
            this.progressLabel.TabIndex = 12;
            this.progressLabel.Tag = "dim";
            //
            // summaryLabel
            //
            this.summaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.summaryLabel.AutoEllipsis = true;
            this.summaryLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.summaryLabel.Location = new System.Drawing.Point(12, 234);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(736, 20);
            this.summaryLabel.TabIndex = 13;
            //
            // checksListView
            //
            this.checksListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checksListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.checkColumn,
            this.resultColumn,
            this.detailColumn});
            this.checksListView.FullRowSelect = true;
            this.checksListView.HideSelection = false;
            this.checksListView.Location = new System.Drawing.Point(12, 258);
            this.checksListView.Name = "checksListView";
            this.checksListView.ShowItemToolTips = true;
            this.checksListView.Size = new System.Drawing.Size(736, 170);
            this.checksListView.TabIndex = 14;
            this.checksListView.UseCompatibleStateImageBehavior = false;
            this.checksListView.View = System.Windows.Forms.View.Details;
            //
            // checkColumn
            //
            this.checkColumn.Text = "Check";
            this.checkColumn.Width = 160;
            //
            // resultColumn
            //
            this.resultColumn.Text = "Result";
            this.resultColumn.Width = 70;
            //
            // detailColumn
            //
            this.detailColumn.Text = "Details";
            this.detailColumn.Width = 480;
            //
            // differencesLabel
            //
            this.differencesLabel.AutoSize = true;
            this.differencesLabel.Location = new System.Drawing.Point(12, 436);
            this.differencesLabel.Name = "differencesLabel";
            this.differencesLabel.Size = new System.Drawing.Size(67, 15);
            this.differencesLabel.TabIndex = 15;
            this.differencesLabel.Tag = "accent";
            this.differencesLabel.Text = "Differences";
            //
            // differencesListView
            //
            this.differencesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.differencesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.typeColumn,
            this.pathColumn});
            this.differencesListView.FullRowSelect = true;
            this.differencesListView.HideSelection = false;
            this.differencesListView.Location = new System.Drawing.Point(12, 456);
            this.differencesListView.Name = "differencesListView";
            this.differencesListView.ShowItemToolTips = true;
            this.differencesListView.Size = new System.Drawing.Size(736, 88);
            this.differencesListView.TabIndex = 16;
            this.differencesListView.UseCompatibleStateImageBehavior = false;
            this.differencesListView.View = System.Windows.Forms.View.Details;
            //
            // typeColumn
            //
            this.typeColumn.Text = "Type";
            this.typeColumn.Width = 140;
            //
            // pathColumn
            //
            this.pathColumn.Text = "Path in image";
            this.pathColumn.Width = 570;
            //
            // saveReportButton
            //
            this.saveReportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.saveReportButton.Enabled = false;
            this.saveReportButton.Location = new System.Drawing.Point(12, 554);
            this.saveReportButton.Name = "saveReportButton";
            this.saveReportButton.Size = new System.Drawing.Size(120, 30);
            this.saveReportButton.TabIndex = 17;
            this.saveReportButton.Text = "&Save Report...";
            this.saveReportButton.UseVisualStyleBackColor = true;
            this.saveReportButton.Click += new System.EventHandler(this.SaveReportButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Location = new System.Drawing.Point(658, 554);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 30);
            this.closeButton.TabIndex = 18;
            this.closeButton.Text = "Cl&ose";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // VerificationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 596);
            this.Controls.Add(this.isoLabel);
            this.Controls.Add(this.isoTextBox);
            this.Controls.Add(this.isoBrowseButton);
            this.Controls.Add(this.sourceLabel);
            this.Controls.Add(this.sourceTextBox);
            this.Controls.Add(this.sourceBrowseButton);
            this.Controls.Add(this.levelHintLabel);
            this.Controls.Add(this.hashCheckBox);
            this.Controls.Add(this.mountCheckBox);
            this.Controls.Add(this.namesLabel);
            this.Controls.Add(this.namesComboBox);
            this.Controls.Add(this.verifyButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.summaryLabel);
            this.Controls.Add(this.checksListView);
            this.Controls.Add(this.differencesLabel);
            this.Controls.Add(this.differencesListView);
            this.Controls.Add(this.saveReportButton);
            this.Controls.Add(this.closeButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 560);
            this.Name = "VerificationForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Verify ISO";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VerificationForm_FormClosing);
            this.Load += new System.EventHandler(this.VerificationForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label isoLabel;
        private System.Windows.Forms.TextBox isoTextBox;
        private System.Windows.Forms.Button isoBrowseButton;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.TextBox sourceTextBox;
        private System.Windows.Forms.Button sourceBrowseButton;
        private System.Windows.Forms.Label levelHintLabel;
        private System.Windows.Forms.CheckBox hashCheckBox;
        private System.Windows.Forms.CheckBox mountCheckBox;
        private System.Windows.Forms.Label namesLabel;
        private System.Windows.Forms.ComboBox namesComboBox;
        private System.Windows.Forms.Button verifyButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressLabel;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.ListView checksListView;
        private System.Windows.Forms.ColumnHeader checkColumn;
        private System.Windows.Forms.ColumnHeader resultColumn;
        private System.Windows.Forms.ColumnHeader detailColumn;
        private System.Windows.Forms.Label differencesLabel;
        private System.Windows.Forms.ListView differencesListView;
        private System.Windows.Forms.ColumnHeader typeColumn;
        private System.Windows.Forms.ColumnHeader pathColumn;
        private System.Windows.Forms.Button saveReportButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
