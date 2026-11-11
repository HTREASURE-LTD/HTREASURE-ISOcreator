namespace HTISOCreator.Forms
{
    partial class CompatibilityForm
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
            this.summaryLabel = new System.Windows.Forms.Label();
            this.rulesLabel = new System.Windows.Forms.Label();
            this.findingsListView = new System.Windows.Forms.ListView();
            this.severityColumn = new System.Windows.Forms.ColumnHeader();
            this.titleColumn = new System.Windows.Forms.ColumnHeader();
            this.detailColumn = new System.Windows.Forms.ColumnHeader();
            this.itemsLabel = new System.Windows.Forms.Label();
            this.itemsListBox = new System.Windows.Forms.ListBox();
            this.renamesLabel = new System.Windows.Forms.Label();
            this.renameFilterLabel = new System.Windows.Forms.Label();
            this.renameFilterTextBox = new System.Windows.Forms.TextBox();
            this.renamesListView = new System.Windows.Forms.ListView();
            this.sourceNameColumn = new System.Windows.Forms.ColumnHeader();
            this.imageNameColumn = new System.Windows.Forms.ColumnHeader();
            this.reasonColumn = new System.Windows.Forms.ColumnHeader();
            this.kindColumn = new System.Windows.Forms.ColumnHeader();
            this.folderColumn = new System.Windows.Forms.ColumnHeader();
            this.confirmCheckBox = new System.Windows.Forms.CheckBox();
            this.exportButton = new System.Windows.Forms.Button();
            this.createButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // summaryLabel
            //
            this.summaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.summaryLabel.AutoEllipsis = true;
            this.summaryLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.summaryLabel.Location = new System.Drawing.Point(12, 12);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(796, 20);
            this.summaryLabel.TabIndex = 0;
            //
            // rulesLabel
            //
            this.rulesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rulesLabel.Location = new System.Drawing.Point(12, 36);
            this.rulesLabel.Name = "rulesLabel";
            this.rulesLabel.Size = new System.Drawing.Size(796, 50);
            this.rulesLabel.TabIndex = 1;
            this.rulesLabel.Tag = "dim";
            //
            // findingsListView
            //
            this.findingsListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.findingsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.severityColumn,
            this.titleColumn,
            this.detailColumn});
            this.findingsListView.FullRowSelect = true;
            this.findingsListView.HideSelection = false;
            this.findingsListView.Location = new System.Drawing.Point(12, 90);
            this.findingsListView.MultiSelect = false;
            this.findingsListView.Name = "findingsListView";
            this.findingsListView.ShowItemToolTips = true;
            this.findingsListView.Size = new System.Drawing.Size(796, 124);
            this.findingsListView.TabIndex = 2;
            this.findingsListView.UseCompatibleStateImageBehavior = false;
            this.findingsListView.View = System.Windows.Forms.View.Details;
            this.findingsListView.SelectedIndexChanged += new System.EventHandler(this.FindingsListView_SelectedIndexChanged);
            //
            // severityColumn
            //
            this.severityColumn.Text = "Severity";
            this.severityColumn.Width = 70;
            //
            // titleColumn
            //
            this.titleColumn.Text = "Finding";
            this.titleColumn.Width = 220;
            //
            // detailColumn
            //
            this.detailColumn.Text = "Details";
            this.detailColumn.Width = 480;
            //
            // itemsLabel
            //
            this.itemsLabel.AutoSize = true;
            this.itemsLabel.Location = new System.Drawing.Point(12, 220);
            this.itemsLabel.Name = "itemsLabel";
            this.itemsLabel.Size = new System.Drawing.Size(85, 15);
            this.itemsLabel.TabIndex = 3;
            this.itemsLabel.Tag = "accent";
            this.itemsLabel.Text = "Affected items";
            //
            // itemsListBox
            //
            this.itemsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.itemsListBox.FormattingEnabled = true;
            this.itemsListBox.HorizontalScrollbar = true;
            this.itemsListBox.IntegralHeight = false;
            this.itemsListBox.ItemHeight = 15;
            this.itemsListBox.Location = new System.Drawing.Point(12, 238);
            this.itemsListBox.Name = "itemsListBox";
            this.itemsListBox.Size = new System.Drawing.Size(796, 74);
            this.itemsListBox.TabIndex = 4;
            //
            // renamesLabel
            //
            this.renamesLabel.AutoSize = true;
            this.renamesLabel.Location = new System.Drawing.Point(12, 324);
            this.renamesLabel.Name = "renamesLabel";
            this.renamesLabel.Size = new System.Drawing.Size(150, 15);
            this.renamesLabel.TabIndex = 5;
            this.renamesLabel.Tag = "accent";
            this.renamesLabel.Text = "Name conversion preview";
            //
            // renameFilterLabel
            //
            this.renameFilterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.renameFilterLabel.AutoSize = true;
            this.renameFilterLabel.Location = new System.Drawing.Point(560, 324);
            this.renameFilterLabel.Name = "renameFilterLabel";
            this.renameFilterLabel.Size = new System.Drawing.Size(36, 15);
            this.renameFilterLabel.TabIndex = 6;
            this.renameFilterLabel.Text = "&Filter:";
            //
            // renameFilterTextBox
            //
            this.renameFilterTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.renameFilterTextBox.Location = new System.Drawing.Point(604, 320);
            this.renameFilterTextBox.Name = "renameFilterTextBox";
            this.renameFilterTextBox.Size = new System.Drawing.Size(204, 23);
            this.renameFilterTextBox.TabIndex = 7;
            this.renameFilterTextBox.TextChanged += new System.EventHandler(this.RenameFilterTextBox_TextChanged);
            //
            // renamesListView
            //
            this.renamesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.renamesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.sourceNameColumn,
            this.imageNameColumn,
            this.reasonColumn,
            this.kindColumn,
            this.folderColumn});
            this.renamesListView.FullRowSelect = true;
            this.renamesListView.HideSelection = false;
            this.renamesListView.Location = new System.Drawing.Point(12, 348);
            this.renamesListView.Name = "renamesListView";
            this.renamesListView.ShowItemToolTips = true;
            this.renamesListView.Size = new System.Drawing.Size(796, 134);
            this.renamesListView.TabIndex = 8;
            this.renamesListView.UseCompatibleStateImageBehavior = false;
            this.renamesListView.View = System.Windows.Forms.View.Details;
            //
            // sourceNameColumn
            //
            this.sourceNameColumn.Text = "Original name";
            this.sourceNameColumn.Width = 230;
            //
            // imageNameColumn
            //
            this.imageNameColumn.Text = "Name in image";
            this.imageNameColumn.Width = 230;
            //
            // reasonColumn
            //
            this.reasonColumn.Text = "Reason";
            this.reasonColumn.Width = 170;
            //
            // kindColumn
            //
            this.kindColumn.Text = "Type";
            this.kindColumn.Width = 50;
            //
            // folderColumn
            //
            this.folderColumn.Text = "Folder";
            this.folderColumn.Width = 200;
            //
            // confirmCheckBox
            //
            this.confirmCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.confirmCheckBox.Location = new System.Drawing.Point(12, 488);
            this.confirmCheckBox.Name = "confirmCheckBox";
            this.confirmCheckBox.Size = new System.Drawing.Size(796, 24);
            this.confirmCheckBox.TabIndex = 9;
            this.confirmCheckBox.Text = "I have reviewed these name conversions";
            this.confirmCheckBox.UseVisualStyleBackColor = true;
            this.confirmCheckBox.Visible = false;
            this.confirmCheckBox.CheckedChanged += new System.EventHandler(this.ConfirmCheckBox_CheckedChanged);
            //
            // exportButton
            //
            this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.exportButton.Location = new System.Drawing.Point(12, 518);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(120, 30);
            this.exportButton.TabIndex = 9;
            this.exportButton.Text = "&Save Report...";
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);
            //
            // createButton
            //
            this.createButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.createButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.createButton.Location = new System.Drawing.Point(602, 518);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(110, 30);
            this.createButton.TabIndex = 10;
            this.createButton.Tag = "primary";
            this.createButton.Text = "&Create ISO";
            this.createButton.UseVisualStyleBackColor = false;
            this.createButton.Click += new System.EventHandler(this.CreateButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Location = new System.Drawing.Point(718, 518);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 30);
            this.closeButton.TabIndex = 11;
            this.closeButton.Text = "Cancel";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // CompatibilityForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(820, 560);
            this.Controls.Add(this.summaryLabel);
            this.Controls.Add(this.rulesLabel);
            this.Controls.Add(this.findingsListView);
            this.Controls.Add(this.itemsLabel);
            this.Controls.Add(this.itemsListBox);
            this.Controls.Add(this.renamesLabel);
            this.Controls.Add(this.renameFilterLabel);
            this.Controls.Add(this.renameFilterTextBox);
            this.Controls.Add(this.renamesListView);
            this.Controls.Add(this.confirmCheckBox);
            this.Controls.Add(this.exportButton);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.closeButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(680, 500);
            this.Name = "CompatibilityForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Filename Compatibility";
            this.Load += new System.EventHandler(this.CompatibilityForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.Label rulesLabel;
        private System.Windows.Forms.ListView findingsListView;
        private System.Windows.Forms.ColumnHeader severityColumn;
        private System.Windows.Forms.ColumnHeader titleColumn;
        private System.Windows.Forms.ColumnHeader detailColumn;
        private System.Windows.Forms.Label itemsLabel;
        private System.Windows.Forms.ListBox itemsListBox;
        private System.Windows.Forms.Label renamesLabel;
        private System.Windows.Forms.Label renameFilterLabel;
        private System.Windows.Forms.TextBox renameFilterTextBox;
        private System.Windows.Forms.ListView renamesListView;
        private System.Windows.Forms.ColumnHeader sourceNameColumn;
        private System.Windows.Forms.ColumnHeader imageNameColumn;
        private System.Windows.Forms.ColumnHeader reasonColumn;
        private System.Windows.Forms.ColumnHeader kindColumn;
        private System.Windows.Forms.ColumnHeader folderColumn;
        private System.Windows.Forms.CheckBox confirmCheckBox;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button closeButton;
    }
}
