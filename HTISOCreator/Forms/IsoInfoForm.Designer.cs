namespace HTISOCreator.Forms
{
    partial class IsoInfoForm
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
            this.pathLabel = new System.Windows.Forms.Label();
            this.pathTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.openButton = new System.Windows.Forms.Button();
            this.fileNameCaption = new System.Windows.Forms.Label();
            this.fileNameValue = new System.Windows.Forms.Label();
            this.fileSizeCaption = new System.Windows.Forms.Label();
            this.fileSizeValue = new System.Windows.Forms.Label();
            this.declaredSizeCaption = new System.Windows.Forms.Label();
            this.declaredSizeValue = new System.Windows.Forms.Label();
            this.filesCaption = new System.Windows.Forms.Label();
            this.filesValue = new System.Windows.Forms.Label();
            this.foldersCaption = new System.Windows.Forms.Label();
            this.foldersValue = new System.Windows.Forms.Label();
            this.bootCaption = new System.Windows.Forms.Label();
            this.bootValue = new System.Windows.Forms.Label();
            this.labelCaption = new System.Windows.Forms.Label();
            this.labelValue = new System.Windows.Forms.Label();
            this.jolietLabelCaption = new System.Windows.Forms.Label();
            this.jolietLabelValue = new System.Windows.Forms.Label();
            this.fileSystemCaption = new System.Windows.Forms.Label();
            this.fileSystemValue = new System.Windows.Forms.Label();
            this.createdCaption = new System.Windows.Forms.Label();
            this.createdValue = new System.Windows.Forms.Label();
            this.modifiedCaption = new System.Windows.Forms.Label();
            this.modifiedValue = new System.Windows.Forms.Label();
            this.applicationCaption = new System.Windows.Forms.Label();
            this.applicationValue = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.treeLabel = new System.Windows.Forms.Label();
            this.treeView = new System.Windows.Forms.TreeView();
            this.copyButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // pathLabel
            //
            this.pathLabel.AutoSize = true;
            this.pathLabel.Location = new System.Drawing.Point(12, 17);
            this.pathLabel.Name = "pathLabel";
            this.pathLabel.Size = new System.Drawing.Size(50, 15);
            this.pathLabel.TabIndex = 0;
            this.pathLabel.Text = "ISO file:";
            //
            // pathTextBox
            //
            this.pathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pathTextBox.Location = new System.Drawing.Point(80, 14);
            this.pathTextBox.Name = "pathTextBox";
            this.pathTextBox.Size = new System.Drawing.Size(472, 23);
            this.pathTextBox.TabIndex = 1;
            //
            // browseButton
            //
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.Location = new System.Drawing.Point(558, 12);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(92, 28);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "&Browse...";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // openButton
            //
            this.openButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openButton.Location = new System.Drawing.Point(656, 12);
            this.openButton.Name = "openButton";
            this.openButton.Size = new System.Drawing.Size(92, 28);
            this.openButton.TabIndex = 3;
            this.openButton.Text = "&Read";
            this.toolTip.SetToolTip(this.openButton, "Read the image named in the box");
            this.openButton.UseVisualStyleBackColor = true;
            this.openButton.Click += new System.EventHandler(this.OpenButton_Click);
            //
            // fileNameCaption
            //
            this.fileNameCaption.AutoSize = true;
            this.fileNameCaption.Location = new System.Drawing.Point(12, 54);
            this.fileNameCaption.Name = "fileNameCaption";
            this.fileNameCaption.Size = new System.Drawing.Size(61, 15);
            this.fileNameCaption.TabIndex = 4;
            this.fileNameCaption.Tag = "dim";
            this.fileNameCaption.Text = "File name:";
            //
            // fileNameValue
            //
            this.fileNameValue.AutoEllipsis = true;
            this.fileNameValue.Location = new System.Drawing.Point(130, 54);
            this.fileNameValue.Name = "fileNameValue";
            this.fileNameValue.Size = new System.Drawing.Size(240, 18);
            this.fileNameValue.TabIndex = 5;
            //
            // fileSizeCaption
            //
            this.fileSizeCaption.AutoSize = true;
            this.fileSizeCaption.Location = new System.Drawing.Point(12, 76);
            this.fileSizeCaption.Name = "fileSizeCaption";
            this.fileSizeCaption.Size = new System.Drawing.Size(51, 15);
            this.fileSizeCaption.TabIndex = 6;
            this.fileSizeCaption.Tag = "dim";
            this.fileSizeCaption.Text = "File size:";
            //
            // fileSizeValue
            //
            this.fileSizeValue.AutoEllipsis = true;
            this.fileSizeValue.Location = new System.Drawing.Point(130, 76);
            this.fileSizeValue.Name = "fileSizeValue";
            this.fileSizeValue.Size = new System.Drawing.Size(240, 18);
            this.fileSizeValue.TabIndex = 7;
            //
            // declaredSizeCaption
            //
            this.declaredSizeCaption.AutoSize = true;
            this.declaredSizeCaption.Location = new System.Drawing.Point(12, 98);
            this.declaredSizeCaption.Name = "declaredSizeCaption";
            this.declaredSizeCaption.Size = new System.Drawing.Size(104, 15);
            this.declaredSizeCaption.TabIndex = 8;
            this.declaredSizeCaption.Tag = "dim";
            this.declaredSizeCaption.Text = "Declared vol. size:";
            this.toolTip.SetToolTip(this.declaredSizeCaption, "The volume size recorded in the primary volume descriptor.");
            //
            // declaredSizeValue
            //
            this.declaredSizeValue.AutoEllipsis = true;
            this.declaredSizeValue.Location = new System.Drawing.Point(130, 98);
            this.declaredSizeValue.Name = "declaredSizeValue";
            this.declaredSizeValue.Size = new System.Drawing.Size(240, 18);
            this.declaredSizeValue.TabIndex = 9;
            //
            // filesCaption
            //
            this.filesCaption.AutoSize = true;
            this.filesCaption.Location = new System.Drawing.Point(12, 120);
            this.filesCaption.Name = "filesCaption";
            this.filesCaption.Size = new System.Drawing.Size(33, 15);
            this.filesCaption.TabIndex = 10;
            this.filesCaption.Tag = "dim";
            this.filesCaption.Text = "Files:";
            //
            // filesValue
            //
            this.filesValue.AutoEllipsis = true;
            this.filesValue.Location = new System.Drawing.Point(130, 120);
            this.filesValue.Name = "filesValue";
            this.filesValue.Size = new System.Drawing.Size(240, 18);
            this.filesValue.TabIndex = 11;
            //
            // foldersCaption
            //
            this.foldersCaption.AutoSize = true;
            this.foldersCaption.Location = new System.Drawing.Point(12, 142);
            this.foldersCaption.Name = "foldersCaption";
            this.foldersCaption.Size = new System.Drawing.Size(48, 15);
            this.foldersCaption.TabIndex = 12;
            this.foldersCaption.Tag = "dim";
            this.foldersCaption.Text = "Folders:";
            //
            // foldersValue
            //
            this.foldersValue.AutoEllipsis = true;
            this.foldersValue.Location = new System.Drawing.Point(130, 142);
            this.foldersValue.Name = "foldersValue";
            this.foldersValue.Size = new System.Drawing.Size(240, 18);
            this.foldersValue.TabIndex = 13;
            //
            // bootCaption
            //
            this.bootCaption.AutoSize = true;
            this.bootCaption.Location = new System.Drawing.Point(12, 164);
            this.bootCaption.Name = "bootCaption";
            this.bootCaption.Size = new System.Drawing.Size(34, 15);
            this.bootCaption.TabIndex = 14;
            this.bootCaption.Tag = "dim";
            this.bootCaption.Text = "Boot:";
            //
            // bootValue
            //
            this.bootValue.AutoEllipsis = true;
            this.bootValue.Location = new System.Drawing.Point(130, 164);
            this.bootValue.Name = "bootValue";
            this.bootValue.Size = new System.Drawing.Size(240, 18);
            this.bootValue.TabIndex = 15;
            //
            // labelCaption
            //
            this.labelCaption.AutoSize = true;
            this.labelCaption.Location = new System.Drawing.Point(384, 54);
            this.labelCaption.Name = "labelCaption";
            this.labelCaption.Size = new System.Drawing.Size(84, 15);
            this.labelCaption.TabIndex = 16;
            this.labelCaption.Tag = "dim";
            this.labelCaption.Text = "Label (ISO):";
            //
            // labelValue
            //
            this.labelValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelValue.AutoEllipsis = true;
            this.labelValue.Location = new System.Drawing.Point(500, 54);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(248, 18);
            this.labelValue.TabIndex = 17;
            //
            // jolietLabelCaption
            //
            this.jolietLabelCaption.AutoSize = true;
            this.jolietLabelCaption.Location = new System.Drawing.Point(384, 76);
            this.jolietLabelCaption.Name = "jolietLabelCaption";
            this.jolietLabelCaption.Size = new System.Drawing.Size(77, 15);
            this.jolietLabelCaption.TabIndex = 18;
            this.jolietLabelCaption.Tag = "dim";
            this.jolietLabelCaption.Text = "Label (Joliet):";
            //
            // jolietLabelValue
            //
            this.jolietLabelValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.jolietLabelValue.AutoEllipsis = true;
            this.jolietLabelValue.Location = new System.Drawing.Point(500, 76);
            this.jolietLabelValue.Name = "jolietLabelValue";
            this.jolietLabelValue.Size = new System.Drawing.Size(248, 18);
            this.jolietLabelValue.TabIndex = 19;
            //
            // fileSystemCaption
            //
            this.fileSystemCaption.AutoSize = true;
            this.fileSystemCaption.Location = new System.Drawing.Point(384, 98);
            this.fileSystemCaption.Name = "fileSystemCaption";
            this.fileSystemCaption.Size = new System.Drawing.Size(71, 15);
            this.fileSystemCaption.TabIndex = 20;
            this.fileSystemCaption.Tag = "dim";
            this.fileSystemCaption.Text = "File system:";
            //
            // fileSystemValue
            //
            this.fileSystemValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileSystemValue.AutoEllipsis = true;
            this.fileSystemValue.Location = new System.Drawing.Point(500, 98);
            this.fileSystemValue.Name = "fileSystemValue";
            this.fileSystemValue.Size = new System.Drawing.Size(248, 18);
            this.fileSystemValue.TabIndex = 21;
            //
            // createdCaption
            //
            this.createdCaption.AutoSize = true;
            this.createdCaption.Location = new System.Drawing.Point(384, 120);
            this.createdCaption.Name = "createdCaption";
            this.createdCaption.Size = new System.Drawing.Size(51, 15);
            this.createdCaption.TabIndex = 22;
            this.createdCaption.Tag = "dim";
            this.createdCaption.Text = "Created:";
            //
            // createdValue
            //
            this.createdValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.createdValue.AutoEllipsis = true;
            this.createdValue.Location = new System.Drawing.Point(500, 120);
            this.createdValue.Name = "createdValue";
            this.createdValue.Size = new System.Drawing.Size(248, 18);
            this.createdValue.TabIndex = 23;
            //
            // modifiedCaption
            //
            this.modifiedCaption.AutoSize = true;
            this.modifiedCaption.Location = new System.Drawing.Point(384, 142);
            this.modifiedCaption.Name = "modifiedCaption";
            this.modifiedCaption.Size = new System.Drawing.Size(58, 15);
            this.modifiedCaption.TabIndex = 24;
            this.modifiedCaption.Tag = "dim";
            this.modifiedCaption.Text = "Modified:";
            //
            // modifiedValue
            //
            this.modifiedValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.modifiedValue.AutoEllipsis = true;
            this.modifiedValue.Location = new System.Drawing.Point(500, 142);
            this.modifiedValue.Name = "modifiedValue";
            this.modifiedValue.Size = new System.Drawing.Size(248, 18);
            this.modifiedValue.TabIndex = 25;
            //
            // applicationCaption
            //
            this.applicationCaption.AutoSize = true;
            this.applicationCaption.Location = new System.Drawing.Point(384, 164);
            this.applicationCaption.Name = "applicationCaption";
            this.applicationCaption.Size = new System.Drawing.Size(71, 15);
            this.applicationCaption.TabIndex = 26;
            this.applicationCaption.Tag = "dim";
            this.applicationCaption.Text = "Application:";
            //
            // applicationValue
            //
            this.applicationValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.applicationValue.AutoEllipsis = true;
            this.applicationValue.Location = new System.Drawing.Point(500, 164);
            this.applicationValue.Name = "applicationValue";
            this.applicationValue.Size = new System.Drawing.Size(248, 18);
            this.applicationValue.TabIndex = 27;
            //
            // statusLabel
            //
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 192);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(736, 18);
            this.statusLabel.TabIndex = 28;
            this.statusLabel.Tag = "dim";
            //
            // treeLabel
            //
            this.treeLabel.AutoSize = true;
            this.treeLabel.Location = new System.Drawing.Point(12, 218);
            this.treeLabel.Name = "treeLabel";
            this.treeLabel.Size = new System.Drawing.Size(111, 15);
            this.treeLabel.TabIndex = 29;
            this.treeLabel.Tag = "accent";
            this.treeLabel.Text = "Directory structure";
            //
            // treeView
            //
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.HideSelection = false;
            this.treeView.Location = new System.Drawing.Point(12, 238);
            this.treeView.Name = "treeView";
            this.treeView.ShowNodeToolTips = true;
            this.treeView.Size = new System.Drawing.Size(736, 268);
            this.treeView.TabIndex = 30;
            //
            // copyButton
            //
            this.copyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.copyButton.Enabled = false;
            this.copyButton.Location = new System.Drawing.Point(12, 516);
            this.copyButton.Name = "copyButton";
            this.copyButton.Size = new System.Drawing.Size(110, 30);
            this.copyButton.TabIndex = 31;
            this.copyButton.Text = "&Copy Details";
            this.copyButton.UseVisualStyleBackColor = true;
            this.copyButton.Click += new System.EventHandler(this.CopyButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(658, 516);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 30);
            this.closeButton.TabIndex = 32;
            this.closeButton.Text = "Cl&ose";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // IsoInfoForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(760, 558);
            this.Controls.Add(this.pathLabel);
            this.Controls.Add(this.pathTextBox);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.fileNameCaption);
            this.Controls.Add(this.fileNameValue);
            this.Controls.Add(this.fileSizeCaption);
            this.Controls.Add(this.fileSizeValue);
            this.Controls.Add(this.declaredSizeCaption);
            this.Controls.Add(this.declaredSizeValue);
            this.Controls.Add(this.filesCaption);
            this.Controls.Add(this.filesValue);
            this.Controls.Add(this.foldersCaption);
            this.Controls.Add(this.foldersValue);
            this.Controls.Add(this.bootCaption);
            this.Controls.Add(this.bootValue);
            this.Controls.Add(this.labelCaption);
            this.Controls.Add(this.labelValue);
            this.Controls.Add(this.jolietLabelCaption);
            this.Controls.Add(this.jolietLabelValue);
            this.Controls.Add(this.fileSystemCaption);
            this.Controls.Add(this.fileSystemValue);
            this.Controls.Add(this.createdCaption);
            this.Controls.Add(this.createdValue);
            this.Controls.Add(this.modifiedCaption);
            this.Controls.Add(this.modifiedValue);
            this.Controls.Add(this.applicationCaption);
            this.Controls.Add(this.applicationValue);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.treeLabel);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.copyButton);
            this.Controls.Add(this.closeButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(680, 480);
            this.Name = "IsoInfoForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ISO Information";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IsoInfoForm_FormClosing);
            this.Load += new System.EventHandler(this.IsoInfoForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label pathLabel;
        private System.Windows.Forms.TextBox pathTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Button openButton;
        private System.Windows.Forms.Label fileNameCaption;
        private System.Windows.Forms.Label fileNameValue;
        private System.Windows.Forms.Label fileSizeCaption;
        private System.Windows.Forms.Label fileSizeValue;
        private System.Windows.Forms.Label declaredSizeCaption;
        private System.Windows.Forms.Label declaredSizeValue;
        private System.Windows.Forms.Label filesCaption;
        private System.Windows.Forms.Label filesValue;
        private System.Windows.Forms.Label foldersCaption;
        private System.Windows.Forms.Label foldersValue;
        private System.Windows.Forms.Label bootCaption;
        private System.Windows.Forms.Label bootValue;
        private System.Windows.Forms.Label labelCaption;
        private System.Windows.Forms.Label labelValue;
        private System.Windows.Forms.Label jolietLabelCaption;
        private System.Windows.Forms.Label jolietLabelValue;
        private System.Windows.Forms.Label fileSystemCaption;
        private System.Windows.Forms.Label fileSystemValue;
        private System.Windows.Forms.Label createdCaption;
        private System.Windows.Forms.Label createdValue;
        private System.Windows.Forms.Label modifiedCaption;
        private System.Windows.Forms.Label modifiedValue;
        private System.Windows.Forms.Label applicationCaption;
        private System.Windows.Forms.Label applicationValue;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label treeLabel;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Button copyButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
