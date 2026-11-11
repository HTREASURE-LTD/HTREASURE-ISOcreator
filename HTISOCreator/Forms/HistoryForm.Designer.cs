namespace HTISOCreator.Forms
{
    partial class HistoryForm
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
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.filterLabel = new System.Windows.Forms.Label();
            this.filterComboBox = new System.Windows.Forms.ComboBox();
            this.countLabel = new System.Windows.Forms.Label();
            this.historyListView = new System.Windows.Forms.ListView();
            this.dateColumn = new System.Windows.Forms.ColumnHeader();
            this.resultColumn = new System.Windows.Forms.ColumnHeader();
            this.fileColumn = new System.Windows.Forms.ColumnHeader();
            this.labelColumn = new System.Windows.Forms.ColumnHeader();
            this.formatColumn = new System.Windows.Forms.ColumnHeader();
            this.sizeColumn = new System.Windows.Forms.ColumnHeader();
            this.filesColumn = new System.Windows.Forms.ColumnHeader();
            this.durationColumn = new System.Windows.Forms.ColumnHeader();
            this.verificationColumn = new System.Windows.Forms.ColumnHeader();
            this.folderColumn = new System.Windows.Forms.ColumnHeader();
            this.detailsTextBox = new System.Windows.Forms.TextBox();
            this.openFolderButton = new System.Windows.Forms.Button();
            this.verifyButton = new System.Windows.Forms.Button();
            this.copyPathButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // searchLabel
            //
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(12, 17);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(45, 15);
            this.searchLabel.TabIndex = 0;
            this.searchLabel.Text = "&Search:";
            //
            // searchTextBox
            //
            this.searchTextBox.Location = new System.Drawing.Point(66, 14);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(260, 23);
            this.searchTextBox.TabIndex = 1;
            this.toolTip.SetToolTip(this.searchTextBox, "Searches ISO file, source folder, volume name and messages");
            this.searchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
            //
            // filterLabel
            //
            this.filterLabel.AutoSize = true;
            this.filterLabel.Location = new System.Drawing.Point(344, 17);
            this.filterLabel.Name = "filterLabel";
            this.filterLabel.Size = new System.Drawing.Size(39, 15);
            this.filterLabel.TabIndex = 2;
            this.filterLabel.Text = "S&how:";
            //
            // filterComboBox
            //
            this.filterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterComboBox.FormattingEnabled = true;
            this.filterComboBox.Location = new System.Drawing.Point(390, 14);
            this.filterComboBox.Name = "filterComboBox";
            this.filterComboBox.Size = new System.Drawing.Size(180, 23);
            this.filterComboBox.TabIndex = 3;
            this.filterComboBox.SelectedIndexChanged += new System.EventHandler(this.FilterComboBox_SelectedIndexChanged);
            //
            // countLabel
            //
            this.countLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.countLabel.AutoEllipsis = true;
            this.countLabel.Location = new System.Drawing.Point(590, 17);
            this.countLabel.Name = "countLabel";
            this.countLabel.Size = new System.Drawing.Size(258, 18);
            this.countLabel.TabIndex = 4;
            this.countLabel.Tag = "dim";
            this.countLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            //
            // historyListView
            //
            this.historyListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.historyListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dateColumn,
            this.resultColumn,
            this.fileColumn,
            this.labelColumn,
            this.formatColumn,
            this.sizeColumn,
            this.filesColumn,
            this.durationColumn,
            this.verificationColumn,
            this.folderColumn});
            this.historyListView.FullRowSelect = true;
            this.historyListView.HideSelection = false;
            this.historyListView.Location = new System.Drawing.Point(12, 46);
            this.historyListView.Name = "historyListView";
            this.historyListView.ShowItemToolTips = true;
            this.historyListView.Size = new System.Drawing.Size(836, 330);
            this.historyListView.TabIndex = 5;
            this.historyListView.UseCompatibleStateImageBehavior = false;
            this.historyListView.View = System.Windows.Forms.View.Details;
            this.historyListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.HistoryListView_ColumnClick);
            this.historyListView.SelectedIndexChanged += new System.EventHandler(this.HistoryListView_SelectedIndexChanged);
            this.historyListView.DoubleClick += new System.EventHandler(this.HistoryListView_DoubleClick);
            //
            // dateColumn
            //
            this.dateColumn.Text = "Date";
            this.dateColumn.Width = 110;
            //
            // resultColumn
            //
            this.resultColumn.Text = "Result";
            this.resultColumn.Width = 70;
            //
            // fileColumn
            //
            this.fileColumn.Text = "ISO file";
            this.fileColumn.Width = 150;
            //
            // labelColumn
            //
            this.labelColumn.Text = "Volume";
            this.labelColumn.Width = 90;
            //
            // formatColumn
            //
            this.formatColumn.Text = "Format";
            this.formatColumn.Width = 100;
            //
            // sizeColumn
            //
            this.sizeColumn.Text = "Size";
            this.sizeColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeColumn.Width = 70;
            //
            // filesColumn
            //
            this.filesColumn.Text = "Files";
            this.filesColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.filesColumn.Width = 50;
            //
            // durationColumn
            //
            this.durationColumn.Text = "Time";
            this.durationColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.durationColumn.Width = 50;
            //
            // verificationColumn
            //
            this.verificationColumn.Text = "Verification";
            this.verificationColumn.Width = 80;
            //
            // folderColumn
            //
            this.folderColumn.Text = "Folder";
            this.folderColumn.Width = 200;
            //
            // detailsTextBox
            //
            this.detailsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detailsTextBox.Location = new System.Drawing.Point(12, 384);
            this.detailsTextBox.Multiline = true;
            this.detailsTextBox.Name = "detailsTextBox";
            this.detailsTextBox.ReadOnly = true;
            this.detailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.detailsTextBox.Size = new System.Drawing.Size(836, 92);
            this.detailsTextBox.TabIndex = 6;
            //
            // openFolderButton
            //
            this.openFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.openFolderButton.Location = new System.Drawing.Point(12, 486);
            this.openFolderButton.Name = "openFolderButton";
            this.openFolderButton.Size = new System.Drawing.Size(100, 30);
            this.openFolderButton.TabIndex = 7;
            this.openFolderButton.Text = "&Open Folder";
            this.openFolderButton.UseVisualStyleBackColor = true;
            this.openFolderButton.Click += new System.EventHandler(this.OpenFolderButton_Click);
            //
            // verifyButton
            //
            this.verifyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.verifyButton.Location = new System.Drawing.Point(118, 486);
            this.verifyButton.Name = "verifyButton";
            this.verifyButton.Size = new System.Drawing.Size(90, 30);
            this.verifyButton.TabIndex = 8;
            this.verifyButton.Text = "&Verify...";
            this.verifyButton.UseVisualStyleBackColor = true;
            this.verifyButton.Click += new System.EventHandler(this.VerifyButton_Click);
            //
            // copyPathButton
            //
            this.copyPathButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.copyPathButton.Location = new System.Drawing.Point(214, 486);
            this.copyPathButton.Name = "copyPathButton";
            this.copyPathButton.Size = new System.Drawing.Size(90, 30);
            this.copyPathButton.TabIndex = 9;
            this.copyPathButton.Text = "Copy &Path";
            this.copyPathButton.UseVisualStyleBackColor = true;
            this.copyPathButton.Click += new System.EventHandler(this.CopyPathButton_Click);
            //
            // exportButton
            //
            this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.exportButton.Location = new System.Drawing.Point(310, 486);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(100, 30);
            this.exportButton.TabIndex = 10;
            this.exportButton.Text = "&Export CSV...";
            this.toolTip.SetToolTip(this.exportButton, "Export the entries currently shown");
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);
            //
            // deleteButton
            //
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteButton.Location = new System.Drawing.Point(428, 486);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(90, 30);
            this.deleteButton.TabIndex = 11;
            this.deleteButton.Text = "&Delete";
            this.toolTip.SetToolTip(this.deleteButton, "Remove the selected entries from the history (the ISO files are kept)");
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            //
            // clearButton
            //
            this.clearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearButton.Location = new System.Drawing.Point(524, 486);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(90, 30);
            this.clearButton.TabIndex = 12;
            this.clearButton.Text = "Clear &All";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(758, 486);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 30);
            this.closeButton.TabIndex = 13;
            this.closeButton.Text = "Cl&ose";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // HistoryForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(860, 528);
            this.Controls.Add(this.searchLabel);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.filterLabel);
            this.Controls.Add(this.filterComboBox);
            this.Controls.Add(this.countLabel);
            this.Controls.Add(this.historyListView);
            this.Controls.Add(this.detailsTextBox);
            this.Controls.Add(this.openFolderButton);
            this.Controls.Add(this.verifyButton);
            this.Controls.Add(this.copyPathButton);
            this.Controls.Add(this.exportButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.closeButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 420);
            this.Name = "HistoryForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Creation History";
            this.Load += new System.EventHandler(this.HistoryForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Label filterLabel;
        private System.Windows.Forms.ComboBox filterComboBox;
        private System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.ListView historyListView;
        private System.Windows.Forms.ColumnHeader dateColumn;
        private System.Windows.Forms.ColumnHeader resultColumn;
        private System.Windows.Forms.ColumnHeader fileColumn;
        private System.Windows.Forms.ColumnHeader labelColumn;
        private System.Windows.Forms.ColumnHeader formatColumn;
        private System.Windows.Forms.ColumnHeader sizeColumn;
        private System.Windows.Forms.ColumnHeader filesColumn;
        private System.Windows.Forms.ColumnHeader durationColumn;
        private System.Windows.Forms.ColumnHeader verificationColumn;
        private System.Windows.Forms.ColumnHeader folderColumn;
        private System.Windows.Forms.TextBox detailsTextBox;
        private System.Windows.Forms.Button openFolderButton;
        private System.Windows.Forms.Button verifyButton;
        private System.Windows.Forms.Button copyPathButton;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
