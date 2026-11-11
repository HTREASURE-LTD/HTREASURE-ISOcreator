namespace HTISOCreator.Forms
{
    partial class SettingsForm
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
            this.outputGroupBox = new System.Windows.Forms.GroupBox();
            this.outputFolderLabel = new System.Windows.Forms.Label();
            this.outputFolderTextBox = new System.Windows.Forms.TextBox();
            this.browseOutputButton = new System.Windows.Forms.Button();
            this.outputFolderHintLabel = new System.Windows.Forms.Label();
            this.rememberOutputCheckBox = new System.Windows.Forms.CheckBox();
            this.isoGroupBox = new System.Windows.Forms.GroupBox();
            this.formatLabel = new System.Windows.Forms.Label();
            this.formatComboBox = new System.Windows.Forms.ComboBox();
            this.volumeLabel = new System.Windows.Forms.Label();
            this.volumeTextBox = new System.Windows.Forms.TextBox();
            this.volumeHintLabel = new System.Windows.Forms.Label();
            this.capacityLabel = new System.Windows.Forms.Label();
            this.capacityComboBox = new System.Windows.Forms.ComboBox();
            this.customCapacityUpDown = new System.Windows.Forms.NumericUpDown();
            this.customCapacityUnitLabel = new System.Windows.Forms.Label();
            this.verificationGroupBox = new System.Windows.Forms.GroupBox();
            this.autoVerifyCheckBox = new System.Windows.Forms.CheckBox();
            this.verifyHashesCheckBox = new System.Windows.Forms.CheckBox();
            this.verifyMountCheckBox = new System.Windows.Forms.CheckBox();
            this.generalGroupBox = new System.Windows.Forms.GroupBox();
            this.rememberSourceCheckBox = new System.Windows.Forms.CheckBox();
            this.completionCheckBox = new System.Windows.Forms.CheckBox();
            this.themeLabel = new System.Windows.Forms.Label();
            this.themeComboBox = new System.Windows.Forms.ComboBox();
            this.restoreDefaultsButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.outputGroupBox.SuspendLayout();
            this.isoGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.customCapacityUpDown)).BeginInit();
            this.verificationGroupBox.SuspendLayout();
            this.generalGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            //
            // outputGroupBox
            //
            this.outputGroupBox.Controls.Add(this.outputFolderLabel);
            this.outputGroupBox.Controls.Add(this.outputFolderTextBox);
            this.outputGroupBox.Controls.Add(this.browseOutputButton);
            this.outputGroupBox.Controls.Add(this.outputFolderHintLabel);
            this.outputGroupBox.Controls.Add(this.rememberOutputCheckBox);
            this.outputGroupBox.Location = new System.Drawing.Point(16, 12);
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.Size = new System.Drawing.Size(532, 112);
            this.outputGroupBox.TabIndex = 0;
            this.outputGroupBox.TabStop = false;
            this.outputGroupBox.Text = "Output";
            //
            // outputFolderLabel
            //
            this.outputFolderLabel.AutoSize = true;
            this.outputFolderLabel.Location = new System.Drawing.Point(14, 29);
            this.outputFolderLabel.Name = "outputFolderLabel";
            this.outputFolderLabel.Size = new System.Drawing.Size(122, 15);
            this.outputFolderLabel.TabIndex = 0;
            this.outputFolderLabel.Text = "Default output folder:";
            //
            // outputFolderTextBox
            //
            this.outputFolderTextBox.Location = new System.Drawing.Point(150, 26);
            this.outputFolderTextBox.Name = "outputFolderTextBox";
            this.outputFolderTextBox.Size = new System.Drawing.Size(270, 23);
            this.outputFolderTextBox.TabIndex = 1;
            this.toolTip.SetToolTip(this.outputFolderTextBox, "Folder suggested for new ISO files.");
            this.outputFolderTextBox.TextChanged += new System.EventHandler(this.Input_Changed);
            //
            // browseOutputButton
            //
            this.browseOutputButton.Location = new System.Drawing.Point(430, 24);
            this.browseOutputButton.Name = "browseOutputButton";
            this.browseOutputButton.Size = new System.Drawing.Size(88, 28);
            this.browseOutputButton.TabIndex = 2;
            this.browseOutputButton.Text = "&Browse...";
            this.browseOutputButton.UseVisualStyleBackColor = true;
            this.browseOutputButton.Click += new System.EventHandler(this.BrowseOutputButton_Click);
            //
            // outputFolderHintLabel
            //
            this.outputFolderHintLabel.AutoEllipsis = true;
            this.outputFolderHintLabel.Location = new System.Drawing.Point(150, 54);
            this.outputFolderHintLabel.Name = "outputFolderHintLabel";
            this.outputFolderHintLabel.Size = new System.Drawing.Size(368, 18);
            this.outputFolderHintLabel.TabIndex = 3;
            this.outputFolderHintLabel.Tag = "dim";
            this.outputFolderHintLabel.Text = "Leave empty to save next to the source folder.";
            //
            // rememberOutputCheckBox
            //
            this.rememberOutputCheckBox.AutoSize = true;
            this.rememberOutputCheckBox.Location = new System.Drawing.Point(17, 80);
            this.rememberOutputCheckBox.Name = "rememberOutputCheckBox";
            this.rememberOutputCheckBox.Size = new System.Drawing.Size(305, 19);
            this.rememberOutputCheckBox.TabIndex = 4;
            this.rememberOutputCheckBox.Text = "Remember the last &output location (used before the default)";
            this.rememberOutputCheckBox.UseVisualStyleBackColor = true;
            //
            // isoGroupBox
            //
            this.isoGroupBox.Controls.Add(this.formatLabel);
            this.isoGroupBox.Controls.Add(this.formatComboBox);
            this.isoGroupBox.Controls.Add(this.volumeLabel);
            this.isoGroupBox.Controls.Add(this.volumeTextBox);
            this.isoGroupBox.Controls.Add(this.volumeHintLabel);
            this.isoGroupBox.Controls.Add(this.capacityLabel);
            this.isoGroupBox.Controls.Add(this.capacityComboBox);
            this.isoGroupBox.Controls.Add(this.customCapacityUpDown);
            this.isoGroupBox.Controls.Add(this.customCapacityUnitLabel);
            this.isoGroupBox.Location = new System.Drawing.Point(16, 132);
            this.isoGroupBox.Name = "isoGroupBox";
            this.isoGroupBox.Size = new System.Drawing.Size(532, 144);
            this.isoGroupBox.TabIndex = 1;
            this.isoGroupBox.TabStop = false;
            this.isoGroupBox.Text = "ISO Image";
            //
            // formatLabel
            //
            this.formatLabel.AutoSize = true;
            this.formatLabel.Location = new System.Drawing.Point(14, 29);
            this.formatLabel.Name = "formatLabel";
            this.formatLabel.Size = new System.Drawing.Size(113, 15);
            this.formatLabel.TabIndex = 0;
            this.formatLabel.Text = "Default file system:";
            //
            // formatComboBox
            //
            this.formatComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.formatComboBox.FormattingEnabled = true;
            this.formatComboBox.Items.AddRange(new object[] {
            "ISO 9660 + Joliet (64-character names)",
            "UDF (127-character names, files over 4 GB)",
            "ISO 9660 + Joliet + UDF (hybrid)",
            "ISO 9660 only (8.3-style names)"});
            this.formatComboBox.Location = new System.Drawing.Point(150, 26);
            this.formatComboBox.Name = "formatComboBox";
            this.formatComboBox.Size = new System.Drawing.Size(270, 23);
            this.formatComboBox.TabIndex = 1;
            this.formatComboBox.SelectedIndexChanged += new System.EventHandler(this.Input_Changed);
            //
            // volumeLabel
            //
            this.volumeLabel.AutoSize = true;
            this.volumeLabel.Location = new System.Drawing.Point(14, 63);
            this.volumeLabel.Name = "volumeLabel";
            this.volumeLabel.Size = new System.Drawing.Size(125, 15);
            this.volumeLabel.TabIndex = 2;
            this.volumeLabel.Text = "Default volume name:";
            //
            // volumeTextBox
            //
            this.volumeTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.volumeTextBox.Location = new System.Drawing.Point(150, 60);
            this.volumeTextBox.MaxLength = 32;
            this.volumeTextBox.Name = "volumeTextBox";
            this.volumeTextBox.Size = new System.Drawing.Size(180, 23);
            this.volumeTextBox.TabIndex = 3;
            this.volumeTextBox.TextChanged += new System.EventHandler(this.Input_Changed);
            //
            // volumeHintLabel
            //
            this.volumeHintLabel.AutoEllipsis = true;
            this.volumeHintLabel.Location = new System.Drawing.Point(150, 86);
            this.volumeHintLabel.Name = "volumeHintLabel";
            this.volumeHintLabel.Size = new System.Drawing.Size(368, 18);
            this.volumeHintLabel.TabIndex = 4;
            this.volumeHintLabel.Tag = "dim";
            this.volumeHintLabel.Text = "Leave empty to use the source folder name.";
            //
            // capacityLabel
            //
            this.capacityLabel.AutoSize = true;
            this.capacityLabel.Location = new System.Drawing.Point(14, 113);
            this.capacityLabel.Name = "capacityLabel";
            this.capacityLabel.Size = new System.Drawing.Size(103, 15);
            this.capacityLabel.TabIndex = 5;
            this.capacityLabel.Text = "Target disc:";
            //
            // capacityComboBox
            //
            this.capacityComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.capacityComboBox.FormattingEnabled = true;
            this.capacityComboBox.Location = new System.Drawing.Point(150, 110);
            this.capacityComboBox.Name = "capacityComboBox";
            this.capacityComboBox.Size = new System.Drawing.Size(180, 23);
            this.capacityComboBox.TabIndex = 6;
            this.toolTip.SetToolTip(this.capacityComboBox, "Used only to warn when an image will not fit. It does not change the image.");
            this.capacityComboBox.SelectedIndexChanged += new System.EventHandler(this.CapacityComboBox_SelectedIndexChanged);
            //
            // customCapacityUpDown
            //
            this.customCapacityUpDown.Location = new System.Drawing.Point(338, 110);
            this.customCapacityUpDown.Maximum = new decimal(new int[] {
            1048576,
            0,
            0,
            0});
            this.customCapacityUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.customCapacityUpDown.Name = "customCapacityUpDown";
            this.customCapacityUpDown.Size = new System.Drawing.Size(90, 23);
            this.customCapacityUpDown.TabIndex = 7;
            this.customCapacityUpDown.ThousandsSeparator = true;
            this.customCapacityUpDown.Value = new decimal(new int[] {
            4400,
            0,
            0,
            0});
            //
            // customCapacityUnitLabel
            //
            this.customCapacityUnitLabel.AutoSize = true;
            this.customCapacityUnitLabel.Location = new System.Drawing.Point(434, 113);
            this.customCapacityUnitLabel.Name = "customCapacityUnitLabel";
            this.customCapacityUnitLabel.Size = new System.Drawing.Size(25, 15);
            this.customCapacityUnitLabel.TabIndex = 8;
            this.customCapacityUnitLabel.Text = "MB";
            //
            // verificationGroupBox
            //
            this.verificationGroupBox.Controls.Add(this.autoVerifyCheckBox);
            this.verificationGroupBox.Controls.Add(this.verifyHashesCheckBox);
            this.verificationGroupBox.Controls.Add(this.verifyMountCheckBox);
            this.verificationGroupBox.Location = new System.Drawing.Point(16, 404);
            this.verificationGroupBox.Name = "verificationGroupBox";
            this.verificationGroupBox.Size = new System.Drawing.Size(532, 100);
            this.verificationGroupBox.TabIndex = 3;
            this.verificationGroupBox.TabStop = false;
            this.verificationGroupBox.Text = "Verification";
            //
            // autoVerifyCheckBox
            //
            this.autoVerifyCheckBox.AutoSize = true;
            this.autoVerifyCheckBox.Location = new System.Drawing.Point(17, 24);
            this.autoVerifyCheckBox.Name = "autoVerifyCheckBox";
            this.autoVerifyCheckBox.Size = new System.Drawing.Size(316, 19);
            this.autoVerifyCheckBox.TabIndex = 0;
            this.autoVerifyCheckBox.Text = "&Verify each new image automatically after creating it";
            this.autoVerifyCheckBox.UseVisualStyleBackColor = true;
            this.autoVerifyCheckBox.CheckedChanged += new System.EventHandler(this.Input_Changed);
            //
            // verifyHashesCheckBox
            //
            this.verifyHashesCheckBox.AutoSize = true;
            this.verifyHashesCheckBox.Location = new System.Drawing.Point(17, 48);
            this.verifyHashesCheckBox.Name = "verifyHashesCheckBox";
            this.verifyHashesCheckBox.Size = new System.Drawing.Size(358, 19);
            this.verifyHashesCheckBox.TabIndex = 1;
            this.verifyHashesCheckBox.Text = "Include a SHA-&256 comparison of every file (reads all data again)";
            this.verifyHashesCheckBox.UseVisualStyleBackColor = true;
            //
            // verifyMountCheckBox
            //
            this.verifyMountCheckBox.AutoSize = true;
            this.verifyMountCheckBox.Location = new System.Drawing.Point(17, 72);
            this.verifyMountCheckBox.Name = "verifyMountCheckBox";
            this.verifyMountCheckBox.Size = new System.Drawing.Size(395, 19);
            this.verifyMountCheckBox.TabIndex = 2;
            this.verifyMountCheckBox.Text = "Also &mount with Windows when verifying (briefly assigns a drive letter)";
            this.verifyMountCheckBox.UseVisualStyleBackColor = true;
            //
            // generalGroupBox
            //
            this.generalGroupBox.Controls.Add(this.rememberSourceCheckBox);
            this.generalGroupBox.Controls.Add(this.completionCheckBox);
            this.generalGroupBox.Controls.Add(this.themeLabel);
            this.generalGroupBox.Controls.Add(this.themeComboBox);
            this.generalGroupBox.Location = new System.Drawing.Point(16, 284);
            this.generalGroupBox.Name = "generalGroupBox";
            this.generalGroupBox.Size = new System.Drawing.Size(532, 112);
            this.generalGroupBox.TabIndex = 2;
            this.generalGroupBox.TabStop = false;
            this.generalGroupBox.Text = "General";
            //
            // rememberSourceCheckBox
            //
            this.rememberSourceCheckBox.AutoSize = true;
            this.rememberSourceCheckBox.Location = new System.Drawing.Point(17, 26);
            this.rememberSourceCheckBox.Name = "rememberSourceCheckBox";
            this.rememberSourceCheckBox.Size = new System.Drawing.Size(287, 19);
            this.rememberSourceCheckBox.TabIndex = 0;
            this.rememberSourceCheckBox.Text = "Remember the last &source folder between sessions";
            this.rememberSourceCheckBox.UseVisualStyleBackColor = true;
            //
            // completionCheckBox
            //
            this.completionCheckBox.AutoSize = true;
            this.completionCheckBox.Location = new System.Drawing.Point(17, 50);
            this.completionCheckBox.Name = "completionCheckBox";
            this.completionCheckBox.Size = new System.Drawing.Size(303, 19);
            this.completionCheckBox.TabIndex = 1;
            this.completionCheckBox.Text = "Show a &notification when an ISO image is completed";
            this.toolTip.SetToolTip(this.completionCheckBox, "When off, completion is shown in the status bar with a sound.");
            this.completionCheckBox.UseVisualStyleBackColor = true;
            //
            // themeLabel
            //
            this.themeLabel.AutoSize = true;
            this.themeLabel.Location = new System.Drawing.Point(14, 80);
            this.themeLabel.Name = "themeLabel";
            this.themeLabel.Size = new System.Drawing.Size(47, 15);
            this.themeLabel.TabIndex = 2;
            this.themeLabel.Text = "&Theme:";
            //
            // themeComboBox
            //
            this.themeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.themeComboBox.FormattingEnabled = true;
            this.themeComboBox.Items.AddRange(new object[] {
            "Use Windows setting",
            "Light",
            "Dark"});
            this.themeComboBox.Location = new System.Drawing.Point(150, 77);
            this.themeComboBox.Name = "themeComboBox";
            this.themeComboBox.Size = new System.Drawing.Size(180, 23);
            this.themeComboBox.TabIndex = 3;
            //
            // restoreDefaultsButton
            //
            this.restoreDefaultsButton.Location = new System.Drawing.Point(16, 518);
            this.restoreDefaultsButton.Name = "restoreDefaultsButton";
            this.restoreDefaultsButton.Size = new System.Drawing.Size(130, 30);
            this.restoreDefaultsButton.TabIndex = 4;
            this.restoreDefaultsButton.Text = "&Restore Defaults";
            this.restoreDefaultsButton.UseVisualStyleBackColor = true;
            this.restoreDefaultsButton.Click += new System.EventHandler(this.RestoreDefaultsButton_Click);
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(362, 518);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(90, 30);
            this.okButton.TabIndex = 5;
            this.okButton.Tag = "primary";
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(458, 518);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(90, 30);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // errorProvider
            //
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errorProvider.ContainerControl = this;
            //
            // SettingsForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(564, 562);
            this.Controls.Add(this.outputGroupBox);
            this.Controls.Add(this.isoGroupBox);
            this.Controls.Add(this.generalGroupBox);
            this.Controls.Add(this.verificationGroupBox);
            this.Controls.Add(this.restoreDefaultsButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.outputGroupBox.ResumeLayout(false);
            this.outputGroupBox.PerformLayout();
            this.isoGroupBox.ResumeLayout(false);
            this.isoGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.customCapacityUpDown)).EndInit();
            this.verificationGroupBox.ResumeLayout(false);
            this.verificationGroupBox.PerformLayout();
            this.generalGroupBox.ResumeLayout(false);
            this.generalGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.Label outputFolderLabel;
        private System.Windows.Forms.TextBox outputFolderTextBox;
        private System.Windows.Forms.Button browseOutputButton;
        private System.Windows.Forms.Label outputFolderHintLabel;
        private System.Windows.Forms.CheckBox rememberOutputCheckBox;
        private System.Windows.Forms.GroupBox isoGroupBox;
        private System.Windows.Forms.Label formatLabel;
        private System.Windows.Forms.ComboBox formatComboBox;
        private System.Windows.Forms.Label volumeLabel;
        private System.Windows.Forms.TextBox volumeTextBox;
        private System.Windows.Forms.Label volumeHintLabel;
        private System.Windows.Forms.GroupBox generalGroupBox;
        private System.Windows.Forms.CheckBox rememberSourceCheckBox;
        private System.Windows.Forms.CheckBox completionCheckBox;
        private System.Windows.Forms.Label themeLabel;
        private System.Windows.Forms.ComboBox themeComboBox;
        private System.Windows.Forms.Button restoreDefaultsButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label capacityLabel;
        private System.Windows.Forms.ComboBox capacityComboBox;
        private System.Windows.Forms.NumericUpDown customCapacityUpDown;
        private System.Windows.Forms.Label customCapacityUnitLabel;
        private System.Windows.Forms.GroupBox verificationGroupBox;
        private System.Windows.Forms.CheckBox autoVerifyCheckBox;
        private System.Windows.Forms.CheckBox verifyHashesCheckBox;
        private System.Windows.Forms.CheckBox verifyMountCheckBox;
    }
}
