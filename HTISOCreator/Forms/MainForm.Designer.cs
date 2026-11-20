namespace HTISOCreator.Forms
{
    partial class MainForm
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
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (_cancellation != null)
                {
                    _cancellation.Dispose();
                }
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
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.selectSourceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chooseOutputMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createIsoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifyIsoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.isoInfoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compatibilityMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.historyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.openOutputFolderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogFolderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewLogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.capacityLabel = new System.Windows.Forms.Label();
            this.capacityComboBox = new System.Windows.Forms.ComboBox();
            this.customCapacityUpDown = new System.Windows.Forms.NumericUpDown();
            this.customCapacityUnitLabel = new System.Windows.Forms.Label();
            this.capacityStatusLabel = new System.Windows.Forms.Label();
            this.compatibilityLinkLabel = new System.Windows.Forms.LinkLabel();
            this.verifyCheckBox = new System.Windows.Forms.CheckBox();
            this.hashCheckBox = new System.Windows.Forms.CheckBox();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.shortcutsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.userGuideMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSystemGuideMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.troubleshootingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.releaseNotesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noticesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.licenceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpDocsSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutAppMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.securityHistoryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.securityAdvisoriesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.headerIcon = new System.Windows.Forms.PictureBox();
            this.headerLabel = new System.Windows.Forms.Label();
            this.websiteLinkLabel = new System.Windows.Forms.LinkLabel();
            this.subHeaderLabel = new System.Windows.Forms.Label();
            this.sourceGroupBox = new System.Windows.Forms.GroupBox();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.sourceTextBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.filesCaption = new System.Windows.Forms.Label();
            this.filesValue = new System.Windows.Forms.Label();
            this.foldersCaption = new System.Windows.Forms.Label();
            this.foldersValue = new System.Windows.Forms.Label();
            this.sourceSizeCaption = new System.Windows.Forms.Label();
            this.sourceSizeValue = new System.Windows.Forms.Label();
            this.isoSizeCaption = new System.Windows.Forms.Label();
            this.isoSizeValue = new System.Windows.Forms.Label();
            this.formatCaption = new System.Windows.Forms.Label();
            this.formatValue = new System.Windows.Forms.Label();
            this.freeSpaceCaption = new System.Windows.Forms.Label();
            this.freeSpaceValue = new System.Windows.Forms.Label();
            this.sourceSummaryLabel = new System.Windows.Forms.Label();
            this.detailsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.isoGroupBox = new System.Windows.Forms.GroupBox();
            this.volumeLabel = new System.Windows.Forms.Label();
            this.volumeTextBox = new System.Windows.Forms.TextBox();
            this.fileSystemLabel = new System.Windows.Forms.Label();
            this.fileSystemComboBox = new System.Windows.Forms.ComboBox();
            this.volumeHintLabel = new System.Windows.Forms.Label();
            this.filenameGroupBox = new System.Windows.Forms.GroupBox();
            this.strictModeRadioButton = new System.Windows.Forms.RadioButton();
            this.compatibilityModeRadioButton = new System.Windows.Forms.RadioButton();
            this.filenameStatusLabel = new System.Windows.Forms.Label();
            this.exportNamesButton = new System.Windows.Forms.Button();
            this.outputGroupBox = new System.Windows.Forms.GroupBox();
            this.outputLabel = new System.Windows.Forms.Label();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.progressGroupBox = new System.Windows.Forms.GroupBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.percentLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.currentFileLabel = new System.Windows.Forms.Label();
            this.bytesLabel = new System.Windows.Forms.Label();
            this.filesLabel = new System.Windows.Forms.Label();
            this.elapsedLabel = new System.Windows.Forms.Label();
            this.remainingLabel = new System.Windows.Forms.Label();
            this.speedLabel = new System.Windows.Forms.Label();
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.openFolderButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.formatStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.brandStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.elapsedTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip.SuspendLayout();
            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.headerIcon)).BeginInit();
            this.sourceGroupBox.SuspendLayout();
            this.isoGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.customCapacityUpDown)).BeginInit();
            this.filenameGroupBox.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.progressGroupBox.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            //
            // menuStrip
            //
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.toolsMenu,
            this.helpMenu});
            this.menuStrip.Location = new System.Drawing.Point(0, 44);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(784, 24);
            this.menuStrip.TabIndex = 0;
            //
            // fileMenu
            //
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectSourceMenuItem,
            this.chooseOutputMenuItem,
            this.createIsoMenuItem,
            this.fileMenuSeparator,
            this.exitMenuItem});
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(37, 20);
            this.fileMenu.Text = "&File";
            //
            // selectSourceMenuItem
            //
            this.selectSourceMenuItem.Name = "selectSourceMenuItem";
            this.selectSourceMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.selectSourceMenuItem.Size = new System.Drawing.Size(240, 22);
            this.selectSourceMenuItem.Text = "Select &Source Folder...";
            this.selectSourceMenuItem.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // chooseOutputMenuItem
            //
            this.chooseOutputMenuItem.Name = "chooseOutputMenuItem";
            this.chooseOutputMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.chooseOutputMenuItem.Size = new System.Drawing.Size(240, 22);
            this.chooseOutputMenuItem.Text = "Choose &Output File...";
            this.chooseOutputMenuItem.Click += new System.EventHandler(this.SaveAsButton_Click);
            //
            // createIsoMenuItem
            //
            this.createIsoMenuItem.Name = "createIsoMenuItem";
            this.createIsoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.createIsoMenuItem.Size = new System.Drawing.Size(240, 22);
            this.createIsoMenuItem.Text = "&Create ISO";
            this.createIsoMenuItem.Click += new System.EventHandler(this.CreateButton_Click);
            //
            // fileMenuSeparator
            //
            this.fileMenuSeparator.Name = "fileMenuSeparator";
            this.fileMenuSeparator.Size = new System.Drawing.Size(237, 6);
            //
            // exitMenuItem
            //
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitMenuItem.Size = new System.Drawing.Size(240, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // toolsMenu
            //
            this.toolsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.verifyIsoMenuItem,
            this.isoInfoMenuItem,
            this.compatibilityMenuItem,
            this.historyMenuItem,
            this.refreshMenuItem,
            this.toolsMenuSeparator2,
            this.openOutputFolderMenuItem,
            this.openLogFolderMenuItem,
            this.viewLogMenuItem,
            this.toolsMenuSeparator,
            this.settingsMenuItem});
            this.toolsMenu.Name = "toolsMenu";
            this.toolsMenu.Size = new System.Drawing.Size(46, 20);
            this.toolsMenu.Text = "&Tools";
            //
            // refreshMenuItem
            //
            this.refreshMenuItem.Name = "refreshMenuItem";
            this.refreshMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.refreshMenuItem.Size = new System.Drawing.Size(240, 22);
            this.refreshMenuItem.Text = "&Refresh Source Analysis";
            this.refreshMenuItem.Click += new System.EventHandler(this.RefreshButton_Click);
            //
            // verifyIsoMenuItem
            //
            this.verifyIsoMenuItem.Name = "verifyIsoMenuItem";
            this.verifyIsoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.V)));
            this.verifyIsoMenuItem.Size = new System.Drawing.Size(270, 22);
            this.verifyIsoMenuItem.Text = "&Verify ISO...";
            this.verifyIsoMenuItem.Click += new System.EventHandler(this.VerifyIsoMenuItem_Click);
            //
            // isoInfoMenuItem
            //
            this.isoInfoMenuItem.Name = "isoInfoMenuItem";
            this.isoInfoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.isoInfoMenuItem.Size = new System.Drawing.Size(270, 22);
            this.isoInfoMenuItem.Text = "ISO &Information...";
            this.isoInfoMenuItem.Click += new System.EventHandler(this.IsoInfoMenuItem_Click);
            //
            // compatibilityMenuItem
            //
            this.compatibilityMenuItem.Name = "compatibilityMenuItem";
            this.compatibilityMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.K)));
            this.compatibilityMenuItem.Size = new System.Drawing.Size(270, 22);
            this.compatibilityMenuItem.Text = "Filename &Compatibility...";
            this.compatibilityMenuItem.Click += new System.EventHandler(this.CompatibilityMenuItem_Click);
            //
            // historyMenuItem
            //
            this.historyMenuItem.Name = "historyMenuItem";
            this.historyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.historyMenuItem.Size = new System.Drawing.Size(270, 22);
            this.historyMenuItem.Text = "Creation &History...";
            this.historyMenuItem.Click += new System.EventHandler(this.HistoryMenuItem_Click);
            //
            // toolsMenuSeparator2
            //
            this.toolsMenuSeparator2.Name = "toolsMenuSeparator2";
            this.toolsMenuSeparator2.Size = new System.Drawing.Size(267, 6);
            //
            // viewLogMenuItem
            //
            this.viewLogMenuItem.Name = "viewLogMenuItem";
            this.viewLogMenuItem.Size = new System.Drawing.Size(270, 22);
            this.viewLogMenuItem.Text = "View &Today\'s Log";
            this.viewLogMenuItem.Click += new System.EventHandler(this.ViewLogMenuItem_Click);
            //
            // openOutputFolderMenuItem
            //
            this.openOutputFolderMenuItem.Name = "openOutputFolderMenuItem";
            this.openOutputFolderMenuItem.Size = new System.Drawing.Size(240, 22);
            this.openOutputFolderMenuItem.Text = "Open Output &Folder";
            this.openOutputFolderMenuItem.Click += new System.EventHandler(this.OpenFolderButton_Click);
            //
            // openLogFolderMenuItem
            //
            this.openLogFolderMenuItem.Name = "openLogFolderMenuItem";
            this.openLogFolderMenuItem.Size = new System.Drawing.Size(240, 22);
            this.openLogFolderMenuItem.Text = "Open &Log Folder";
            this.openLogFolderMenuItem.Click += new System.EventHandler(this.OpenLogFolderMenuItem_Click);
            //
            // toolsMenuSeparator
            //
            this.toolsMenuSeparator.Name = "toolsMenuSeparator";
            this.toolsMenuSeparator.Size = new System.Drawing.Size(237, 6);
            //
            // settingsMenuItem
            //
            this.settingsMenuItem.Name = "settingsMenuItem";
            this.settingsMenuItem.ShortcutKeyDisplayString = "Ctrl+,";
            this.settingsMenuItem.Size = new System.Drawing.Size(240, 22);
            this.settingsMenuItem.Text = "&Settings...";
            this.settingsMenuItem.Click += new System.EventHandler(this.SettingsMenuItem_Click);
            //
            // helpMenu
            //
            this.helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.userGuideMenuItem,
            this.fileSystemGuideMenuItem,
            this.troubleshootingMenuItem,
            this.releaseNotesMenuItem,
            this.helpDocsSeparator,
            this.shortcutsMenuItem,
            this.licenceMenuItem,
            this.noticesMenuItem,
            this.securityAdvisoriesMenuItem,
            this.helpMenuSeparator,
            this.aboutMenuItem});
            this.helpMenu.Name = "helpMenu";
            this.helpMenu.Size = new System.Drawing.Size(44, 20);
            this.helpMenu.Text = "&Help";
            //
            // userGuideMenuItem
            //
            this.userGuideMenuItem.Name = "userGuideMenuItem";
            this.userGuideMenuItem.Size = new System.Drawing.Size(220, 22);
            this.userGuideMenuItem.Tag = "USER_GUIDE.md";
            this.userGuideMenuItem.Text = "&User Guide";
            this.userGuideMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // fileSystemGuideMenuItem
            //
            this.fileSystemGuideMenuItem.Name = "fileSystemGuideMenuItem";
            this.fileSystemGuideMenuItem.Size = new System.Drawing.Size(220, 22);
            this.fileSystemGuideMenuItem.Tag = "FILESYSTEM_GUIDE.md";
            this.fileSystemGuideMenuItem.Text = "&File System Guide";
            this.fileSystemGuideMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // troubleshootingMenuItem
            //
            this.troubleshootingMenuItem.Name = "troubleshootingMenuItem";
            this.troubleshootingMenuItem.Size = new System.Drawing.Size(220, 22);
            this.troubleshootingMenuItem.Tag = "TROUBLESHOOTING.md";
            this.troubleshootingMenuItem.Text = "&Troubleshooting";
            this.troubleshootingMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // releaseNotesMenuItem
            //
            this.releaseNotesMenuItem.Name = "releaseNotesMenuItem";
            this.releaseNotesMenuItem.Size = new System.Drawing.Size(220, 22);
            this.releaseNotesMenuItem.Tag = "RELEASE_NOTES.md";
            this.releaseNotesMenuItem.Text = "&Release Notes";
            this.releaseNotesMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // licenceMenuItem
            //
            this.licenceMenuItem.Name = "licenceMenuItem";
            this.licenceMenuItem.Size = new System.Drawing.Size(220, 22);
            this.licenceMenuItem.Tag = "LICENSE.txt";
            this.licenceMenuItem.Text = "&Licence Agreement";
            this.licenceMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // noticesMenuItem
            //
            this.noticesMenuItem.Name = "noticesMenuItem";
            this.noticesMenuItem.Size = new System.Drawing.Size(220, 22);
            this.noticesMenuItem.Tag = "THIRD_PARTY_NOTICES.md";
            this.noticesMenuItem.Text = "Third-Party &Notices";
            this.noticesMenuItem.Click += new System.EventHandler(this.DocumentMenuItem_Click);
            //
            // helpDocsSeparator
            //
            this.helpDocsSeparator.Name = "helpDocsSeparator";
            this.helpDocsSeparator.Size = new System.Drawing.Size(217, 6);
            //
            // shortcutsMenuItem
            //
            this.shortcutsMenuItem.Name = "shortcutsMenuItem";
            this.shortcutsMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.shortcutsMenuItem.Size = new System.Drawing.Size(220, 22);
            this.shortcutsMenuItem.Text = "&Keyboard Shortcuts";
            this.shortcutsMenuItem.Click += new System.EventHandler(this.ShortcutsMenuItem_Click);
            //
            // helpMenuSeparator
            //
            this.helpMenuSeparator.Name = "helpMenuSeparator";
            this.helpMenuSeparator.Size = new System.Drawing.Size(217, 6);
            //
            // aboutMenuItem
            //
            // Help > About is a submenu (HTREASURE software standards): this application, and its Security Advisory History.
            this.aboutMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutAppMenuItem,
            this.aboutSeparator,
            this.securityHistoryMenuItem});
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(220, 22);
            this.aboutMenuItem.Text = "&About";
            //
            // aboutAppMenuItem
            //
            this.aboutAppMenuItem.Name = "aboutAppMenuItem";
            this.aboutAppMenuItem.Size = new System.Drawing.Size(230, 22);
            this.aboutAppMenuItem.Text = "&About HT ISO Creator";
            this.aboutAppMenuItem.Click += new System.EventHandler(this.AboutMenuItem_Click);
            //
            // aboutSeparator
            //
            this.aboutSeparator.Name = "aboutSeparator";
            this.aboutSeparator.Size = new System.Drawing.Size(227, 6);
            //
            // securityHistoryMenuItem
            //
            this.securityHistoryMenuItem.Name = "securityHistoryMenuItem";
            this.securityHistoryMenuItem.Size = new System.Drawing.Size(230, 22);
            this.securityHistoryMenuItem.Text = "Security Advisory &History";
            this.securityHistoryMenuItem.Click += new System.EventHandler(this.SecurityAdvisoriesMenuItem_Click);
            //
            // securityAdvisoriesMenuItem
            //
            this.securityAdvisoriesMenuItem.Name = "securityAdvisoriesMenuItem";
            this.securityAdvisoriesMenuItem.Size = new System.Drawing.Size(220, 22);
            this.securityAdvisoriesMenuItem.Text = "Security &Advisories";
            this.securityAdvisoriesMenuItem.Click += new System.EventHandler(this.SecurityAdvisoriesMenuItem_Click);
            //
            // headerPanel
            //
            this.headerPanel.Controls.Add(this.headerIcon);
            this.headerPanel.Controls.Add(this.headerLabel);
            this.headerPanel.Controls.Add(this.subHeaderLabel);
            this.headerPanel.Controls.Add(this.websiteLinkLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(784, 44);
            this.headerPanel.TabIndex = 1;
            this.headerPanel.Tag = "header";
            //
            // headerIcon
            //
            this.headerIcon.Location = new System.Drawing.Point(16, 5);
            this.headerIcon.Name = "headerIcon";
            this.headerIcon.Size = new System.Drawing.Size(36, 36);
            this.headerIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.headerIcon.TabIndex = 0;
            this.headerIcon.TabStop = false;
            //
            // headerLabel
            //
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerLabel.Location = new System.Drawing.Point(60, 1);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(140, 25);
            this.headerLabel.TabIndex = 1;
            this.headerLabel.Text = "HT ISO Creator";
            //
            // subHeaderLabel
            //
            this.subHeaderLabel.AutoSize = true;
            this.subHeaderLabel.Location = new System.Drawing.Point(62, 27);
            this.subHeaderLabel.Name = "subHeaderLabel";
            this.subHeaderLabel.Size = new System.Drawing.Size(408, 15);
            this.subHeaderLabel.TabIndex = 2;
            this.subHeaderLabel.Tag = "dim";
            this.subHeaderLabel.Text = "Create a standard ISO image from any folder. Tip: drag a folder onto this window.";
            //
            // websiteLinkLabel
            //
            this.websiteLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.websiteLinkLabel.Location = new System.Drawing.Point(608, 13);
            this.websiteLinkLabel.Name = "websiteLinkLabel";
            this.websiteLinkLabel.Size = new System.Drawing.Size(160, 18);
            this.websiteLinkLabel.TabIndex = 3;
            this.websiteLinkLabel.TabStop = true;
            this.websiteLinkLabel.Text = "www.htreasure.com";
            this.websiteLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip.SetToolTip(this.websiteLinkLabel, "Open the HTREASURE LTD website (https://www.htreasure.com)");
            this.websiteLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebsiteLinkLabel_LinkClicked);
            //
            // sourceGroupBox
            //
            this.sourceGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceGroupBox.Controls.Add(this.sourceLabel);
            this.sourceGroupBox.Controls.Add(this.sourceTextBox);
            this.sourceGroupBox.Controls.Add(this.browseButton);
            this.sourceGroupBox.Controls.Add(this.refreshButton);
            this.sourceGroupBox.Controls.Add(this.filesCaption);
            this.sourceGroupBox.Controls.Add(this.filesValue);
            this.sourceGroupBox.Controls.Add(this.foldersCaption);
            this.sourceGroupBox.Controls.Add(this.foldersValue);
            this.sourceGroupBox.Controls.Add(this.sourceSizeCaption);
            this.sourceGroupBox.Controls.Add(this.sourceSizeValue);
            this.sourceGroupBox.Controls.Add(this.isoSizeCaption);
            this.sourceGroupBox.Controls.Add(this.isoSizeValue);
            this.sourceGroupBox.Controls.Add(this.formatCaption);
            this.sourceGroupBox.Controls.Add(this.formatValue);
            this.sourceGroupBox.Controls.Add(this.freeSpaceCaption);
            this.sourceGroupBox.Controls.Add(this.freeSpaceValue);
            this.sourceGroupBox.Controls.Add(this.sourceSummaryLabel);
            this.sourceGroupBox.Controls.Add(this.detailsLinkLabel);
            this.sourceGroupBox.Location = new System.Drawing.Point(16, 74);
            this.sourceGroupBox.Name = "sourceGroupBox";
            this.sourceGroupBox.Size = new System.Drawing.Size(752, 130);
            this.sourceGroupBox.TabIndex = 2;
            this.sourceGroupBox.TabStop = false;
            this.sourceGroupBox.Text = "Source";
            //
            // sourceLabel
            //
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Location = new System.Drawing.Point(14, 26);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(83, 15);
            this.sourceLabel.TabIndex = 0;
            this.sourceLabel.Text = "Source Folder:";
            //
            // sourceTextBox
            //
            this.sourceTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceTextBox.Location = new System.Drawing.Point(124, 23);
            this.sourceTextBox.Name = "sourceTextBox";
            this.sourceTextBox.AccessibleName = "Source folder";
            this.sourceTextBox.Size = new System.Drawing.Size(418, 23);
            this.sourceTextBox.TabIndex = 1;
            this.toolTip.SetToolTip(this.sourceTextBox, "The folder whose contents will be placed in the ISO image. You can also drag a folder from File Explorer.");
            this.sourceTextBox.TextChanged += new System.EventHandler(this.SourceTextBox_TextChanged);
            this.sourceTextBox.Leave += new System.EventHandler(this.SourceTextBox_Leave);
            //
            // browseButton
            //
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.Location = new System.Drawing.Point(550, 21);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(92, 28);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "&Browse...";
            this.toolTip.SetToolTip(this.browseButton, "Choose the source folder (Ctrl+O)");
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // refreshButton
            //
            this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshButton.Location = new System.Drawing.Point(648, 21);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(90, 28);
            this.refreshButton.TabIndex = 3;
            this.refreshButton.Text = "Re&fresh";
            this.toolTip.SetToolTip(this.refreshButton, "Scan the source folder again (Ctrl+R)");
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            //
            // filesCaption
            //
            this.filesCaption.AutoSize = true;
            this.filesCaption.Location = new System.Drawing.Point(14, 50);
            this.filesCaption.Name = "filesCaption";
            this.filesCaption.Size = new System.Drawing.Size(33, 15);
            this.filesCaption.TabIndex = 4;
            this.filesCaption.Tag = "dim";
            this.filesCaption.Text = "Files:";
            //
            // filesValue
            //
            this.filesValue.AutoEllipsis = true;
            this.filesValue.Location = new System.Drawing.Point(124, 50);
            this.filesValue.Name = "filesValue";
            this.filesValue.Size = new System.Drawing.Size(230, 18);
            this.filesValue.TabIndex = 5;
            this.filesValue.Text = "--";
            //
            // foldersCaption
            //
            this.foldersCaption.AutoSize = true;
            this.foldersCaption.Location = new System.Drawing.Point(14, 68);
            this.foldersCaption.Name = "foldersCaption";
            this.foldersCaption.Size = new System.Drawing.Size(70, 15);
            this.foldersCaption.TabIndex = 6;
            this.foldersCaption.Tag = "dim";
            this.foldersCaption.Text = "Subfolders:";
            //
            // foldersValue
            //
            this.foldersValue.AutoEllipsis = true;
            this.foldersValue.Location = new System.Drawing.Point(124, 68);
            this.foldersValue.Name = "foldersValue";
            this.foldersValue.Size = new System.Drawing.Size(230, 18);
            this.foldersValue.TabIndex = 7;
            this.foldersValue.Text = "--";
            //
            // sourceSizeCaption
            //
            this.sourceSizeCaption.AutoSize = true;
            this.sourceSizeCaption.Location = new System.Drawing.Point(14, 86);
            this.sourceSizeCaption.Name = "sourceSizeCaption";
            this.sourceSizeCaption.Size = new System.Drawing.Size(70, 15);
            this.sourceSizeCaption.TabIndex = 8;
            this.sourceSizeCaption.Tag = "dim";
            this.sourceSizeCaption.Text = "Source size:";
            //
            // sourceSizeValue
            //
            this.sourceSizeValue.AutoEllipsis = true;
            this.sourceSizeValue.Location = new System.Drawing.Point(124, 86);
            this.sourceSizeValue.Name = "sourceSizeValue";
            this.sourceSizeValue.Size = new System.Drawing.Size(230, 18);
            this.sourceSizeValue.TabIndex = 9;
            this.sourceSizeValue.Text = "--";
            //
            // isoSizeCaption
            //
            this.isoSizeCaption.AutoSize = true;
            this.isoSizeCaption.Location = new System.Drawing.Point(380, 50);
            this.isoSizeCaption.Name = "isoSizeCaption";
            this.isoSizeCaption.Size = new System.Drawing.Size(51, 15);
            this.isoSizeCaption.TabIndex = 10;
            this.isoSizeCaption.Tag = "dim";
            this.isoSizeCaption.Text = "ISO size:";
            this.toolTip.SetToolTip(this.isoSizeCaption, "Calculated from the image layout, including file system overhead and 2 KB sector alignment.");
            //
            // isoSizeValue
            //
            this.isoSizeValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.isoSizeValue.AutoEllipsis = true;
            this.isoSizeValue.Location = new System.Drawing.Point(500, 50);
            this.isoSizeValue.Name = "isoSizeValue";
            this.isoSizeValue.Size = new System.Drawing.Size(238, 18);
            this.isoSizeValue.TabIndex = 11;
            this.isoSizeValue.Text = "--";
            //
            // formatCaption
            //
            this.formatCaption.AutoSize = true;
            this.formatCaption.Location = new System.Drawing.Point(380, 68);
            this.formatCaption.Name = "formatCaption";
            this.formatCaption.Size = new System.Drawing.Size(71, 15);
            this.formatCaption.TabIndex = 12;
            this.formatCaption.Tag = "dim";
            this.formatCaption.Text = "File system:";
            //
            // formatValue
            //
            this.formatValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.formatValue.AutoEllipsis = true;
            this.formatValue.Location = new System.Drawing.Point(500, 68);
            this.formatValue.Name = "formatValue";
            this.formatValue.Size = new System.Drawing.Size(238, 18);
            this.formatValue.TabIndex = 13;
            this.formatValue.Text = "--";
            //
            // freeSpaceCaption
            //
            this.freeSpaceCaption.AutoSize = true;
            this.freeSpaceCaption.Location = new System.Drawing.Point(380, 86);
            this.freeSpaceCaption.Name = "freeSpaceCaption";
            this.freeSpaceCaption.Size = new System.Drawing.Size(66, 15);
            this.freeSpaceCaption.TabIndex = 14;
            this.freeSpaceCaption.Tag = "dim";
            this.freeSpaceCaption.Text = "Free space:";
            this.toolTip.SetToolTip(this.freeSpaceCaption, "Space available on the drive that will hold the ISO file.");
            //
            // freeSpaceValue
            //
            this.freeSpaceValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.freeSpaceValue.AutoEllipsis = true;
            this.freeSpaceValue.Location = new System.Drawing.Point(500, 86);
            this.freeSpaceValue.Name = "freeSpaceValue";
            this.freeSpaceValue.Size = new System.Drawing.Size(238, 18);
            this.freeSpaceValue.TabIndex = 15;
            this.freeSpaceValue.Text = "--";
            //
            // sourceSummaryLabel
            //
            this.sourceSummaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceSummaryLabel.AutoEllipsis = true;
            this.sourceSummaryLabel.Location = new System.Drawing.Point(14, 105);
            this.sourceSummaryLabel.Name = "sourceSummaryLabel";
            this.sourceSummaryLabel.Size = new System.Drawing.Size(600, 18);
            this.sourceSummaryLabel.TabIndex = 16;
            this.sourceSummaryLabel.Tag = "dim";
            this.sourceSummaryLabel.Text = "No folder selected.";
            //
            // detailsLinkLabel
            //
            this.detailsLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.detailsLinkLabel.Location = new System.Drawing.Point(620, 105);
            this.detailsLinkLabel.Name = "detailsLinkLabel";
            this.detailsLinkLabel.Size = new System.Drawing.Size(118, 18);
            this.detailsLinkLabel.TabIndex = 17;
            this.detailsLinkLabel.TabStop = true;
            this.detailsLinkLabel.Text = "Show details";
            this.detailsLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.detailsLinkLabel.Visible = false;
            this.detailsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DetailsLinkLabel_LinkClicked);
            //
            // isoGroupBox
            //
            this.isoGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.isoGroupBox.Controls.Add(this.volumeLabel);
            this.isoGroupBox.Controls.Add(this.volumeTextBox);
            this.isoGroupBox.Controls.Add(this.fileSystemLabel);
            this.isoGroupBox.Controls.Add(this.fileSystemComboBox);
            this.isoGroupBox.Controls.Add(this.volumeHintLabel);
            this.isoGroupBox.Controls.Add(this.capacityLabel);
            this.isoGroupBox.Controls.Add(this.capacityComboBox);
            this.isoGroupBox.Controls.Add(this.customCapacityUpDown);
            this.isoGroupBox.Controls.Add(this.customCapacityUnitLabel);
            this.isoGroupBox.Controls.Add(this.capacityStatusLabel);
            this.isoGroupBox.Location = new System.Drawing.Point(16, 210);
            this.isoGroupBox.Name = "isoGroupBox";
            this.isoGroupBox.Size = new System.Drawing.Size(752, 100);
            this.isoGroupBox.TabIndex = 3;
            this.isoGroupBox.TabStop = false;
            this.isoGroupBox.Text = "ISO Information";
            //
            // volumeLabel
            //
            this.volumeLabel.AutoSize = true;
            this.volumeLabel.Location = new System.Drawing.Point(14, 24);
            this.volumeLabel.Name = "volumeLabel";
            this.volumeLabel.Size = new System.Drawing.Size(81, 15);
            this.volumeLabel.TabIndex = 0;
            this.volumeLabel.Text = "Volume Name:";
            //
            // volumeTextBox
            //
            this.volumeTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.volumeTextBox.Location = new System.Drawing.Point(124, 21);
            this.volumeTextBox.MaxLength = 32;
            this.volumeTextBox.Name = "volumeTextBox";
            this.volumeTextBox.AccessibleName = "Volume name";
            this.volumeTextBox.Size = new System.Drawing.Size(220, 23);
            this.volumeTextBox.TabIndex = 1;
            this.toolTip.SetToolTip(this.volumeTextBox, "The disc label shown when the image is mounted or burned.");
            this.volumeTextBox.TextChanged += new System.EventHandler(this.VolumeTextBox_TextChanged);
            //
            // fileSystemLabel
            //
            this.fileSystemLabel.AutoSize = true;
            this.fileSystemLabel.Location = new System.Drawing.Point(380, 24);
            this.fileSystemLabel.Name = "fileSystemLabel";
            this.fileSystemLabel.Size = new System.Drawing.Size(71, 15);
            this.fileSystemLabel.TabIndex = 2;
            this.fileSystemLabel.Text = "File s&ystem:";
            //
            // fileSystemComboBox
            //
            this.fileSystemComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileSystemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fileSystemComboBox.DropDownWidth = 320;
            this.fileSystemComboBox.Location = new System.Drawing.Point(462, 21);
            this.fileSystemComboBox.Name = "fileSystemComboBox";
            this.fileSystemComboBox.AccessibleName = "File system";
            this.fileSystemComboBox.Size = new System.Drawing.Size(276, 23);
            this.fileSystemComboBox.TabIndex = 3;
            this.toolTip.SetToolTip(this.fileSystemComboBox, "ISO 9660 + Joliet suits most data discs. UDF keeps names up to 127 characters and files over 4 GB. " +
                "The hybrid adds Joliet and ISO 9660 names for older readers. ISO 9660 only uses short upper-case names.");
            this.fileSystemComboBox.SelectedIndexChanged += new System.EventHandler(this.FileSystemComboBox_SelectedIndexChanged);
            //
            // volumeHintLabel
            //
            this.volumeHintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.volumeHintLabel.AutoEllipsis = true;
            this.volumeHintLabel.Location = new System.Drawing.Point(124, 48);
            this.volumeHintLabel.Name = "volumeHintLabel";
            this.volumeHintLabel.Size = new System.Drawing.Size(450, 18);
            this.volumeHintLabel.TabIndex = 3;
            this.volumeHintLabel.Tag = "dim";
            this.volumeHintLabel.Text = "Up to 16 characters: A-Z, 0-9 and underscore.";
            //
            // compatibilityLinkLabel
            //
            this.compatibilityLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.compatibilityLinkLabel.Location = new System.Drawing.Point(460, 38);
            this.compatibilityLinkLabel.Name = "compatibilityLinkLabel";
            this.compatibilityLinkLabel.Size = new System.Drawing.Size(148, 18);
            this.compatibilityLinkLabel.TabIndex = 3;
            this.compatibilityLinkLabel.TabStop = true;
            this.compatibilityLinkLabel.Text = "Preview conversions...";
            this.compatibilityLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.toolTip.SetToolTip(this.compatibilityLinkLabel, "Preview name conversions and compatibility findings without creating the image (Ctrl+K)");
            this.compatibilityLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CompatibilityLinkLabel_LinkClicked);
            //
            // capacityLabel
            //
            this.capacityLabel.AutoSize = true;
            this.capacityLabel.Location = new System.Drawing.Point(14, 74);
            this.capacityLabel.Name = "capacityLabel";
            this.capacityLabel.Size = new System.Drawing.Size(69, 15);
            this.capacityLabel.TabIndex = 5;
            this.capacityLabel.Text = "Target disc:";
            //
            // capacityComboBox
            //
            this.capacityComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.capacityComboBox.FormattingEnabled = true;
            this.capacityComboBox.Location = new System.Drawing.Point(124, 71);
            this.capacityComboBox.Name = "capacityComboBox";
            this.capacityComboBox.AccessibleName = "Target disc";
            this.capacityComboBox.Size = new System.Drawing.Size(220, 23);
            this.capacityComboBox.TabIndex = 6;
            this.toolTip.SetToolTip(this.capacityComboBox, "Warns when the image will not fit. It does not change the image or its file system, and fitting does not guarantee a burner or disc will accept it.");
            this.capacityComboBox.SelectedIndexChanged += new System.EventHandler(this.Capacity_Changed);
            //
            // customCapacityUpDown
            //
            this.customCapacityUpDown.Location = new System.Drawing.Point(350, 71);
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
            this.customCapacityUpDown.AccessibleName = "Custom disc capacity in megabytes";
            this.customCapacityUpDown.Size = new System.Drawing.Size(80, 23);
            this.customCapacityUpDown.TabIndex = 7;
            this.customCapacityUpDown.ThousandsSeparator = true;
            this.toolTip.SetToolTip(this.customCapacityUpDown, "Custom capacity in MB (1 MB = 1,048,576 bytes)");
            this.customCapacityUpDown.Value = new decimal(new int[] {
            4400,
            0,
            0,
            0});
            this.customCapacityUpDown.ValueChanged += new System.EventHandler(this.Capacity_Changed);
            //
            // customCapacityUnitLabel
            //
            this.customCapacityUnitLabel.AutoSize = true;
            this.customCapacityUnitLabel.Location = new System.Drawing.Point(434, 74);
            this.customCapacityUnitLabel.Name = "customCapacityUnitLabel";
            this.customCapacityUnitLabel.Size = new System.Drawing.Size(25, 15);
            this.customCapacityUnitLabel.TabIndex = 8;
            this.customCapacityUnitLabel.Text = "MB";
            //
            // capacityStatusLabel
            //
            this.capacityStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.capacityStatusLabel.AutoEllipsis = true;
            this.capacityStatusLabel.Location = new System.Drawing.Point(466, 74);
            this.capacityStatusLabel.Name = "capacityStatusLabel";
            this.capacityStatusLabel.Size = new System.Drawing.Size(272, 18);
            this.capacityStatusLabel.TabIndex = 9;
            this.capacityStatusLabel.Tag = "dim";
            this.capacityStatusLabel.Text = "No capacity limit selected.";
            //
            // filenameGroupBox
            //
            this.filenameGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filenameGroupBox.Controls.Add(this.strictModeRadioButton);
            this.filenameGroupBox.Controls.Add(this.compatibilityModeRadioButton);
            this.filenameGroupBox.Controls.Add(this.filenameStatusLabel);
            this.filenameGroupBox.Controls.Add(this.compatibilityLinkLabel);
            this.filenameGroupBox.Controls.Add(this.exportNamesButton);
            this.filenameGroupBox.Location = new System.Drawing.Point(16, 316);
            this.filenameGroupBox.Name = "filenameGroupBox";
            this.filenameGroupBox.Size = new System.Drawing.Size(752, 58);
            this.filenameGroupBox.TabIndex = 4;
            this.filenameGroupBox.TabStop = false;
            this.filenameGroupBox.Text = "Filename Compatibility";
            //
            // strictModeRadioButton
            //
            this.strictModeRadioButton.AutoSize = true;
            this.strictModeRadioButton.Checked = true;
            this.strictModeRadioButton.Location = new System.Drawing.Point(14, 18);
            this.strictModeRadioButton.Name = "strictModeRadioButton";
            this.strictModeRadioButton.Size = new System.Drawing.Size(262, 19);
            this.strictModeRadioButton.TabIndex = 0;
            this.strictModeRadioButton.TabStop = true;
            this.strictModeRadioButton.Text = "&Strict Mode \u2014 Preserve Original Filenames";
            this.toolTip.SetToolTip(this.strictModeRadioButton, "Refuse to create the image if any file or folder name would change. Recommended for software, drivers and DLLs.");
            this.strictModeRadioButton.UseVisualStyleBackColor = true;
            this.strictModeRadioButton.CheckedChanged += new System.EventHandler(this.FilenameMode_CheckedChanged);
            //
            // compatibilityModeRadioButton
            //
            this.compatibilityModeRadioButton.AutoSize = true;
            this.compatibilityModeRadioButton.Location = new System.Drawing.Point(296, 18);
            this.compatibilityModeRadioButton.Name = "compatibilityModeRadioButton";
            this.compatibilityModeRadioButton.Size = new System.Drawing.Size(296, 19);
            this.compatibilityModeRadioButton.TabIndex = 1;
            this.compatibilityModeRadioButton.Text = "Co&mpatibility Mode \u2014 Allow Filename Conversion";
            this.toolTip.SetToolTip(this.compatibilityModeRadioButton, "Convert names the file system cannot store, after you have reviewed and confirmed every conversion.");
            this.compatibilityModeRadioButton.UseVisualStyleBackColor = true;
            this.compatibilityModeRadioButton.CheckedChanged += new System.EventHandler(this.FilenameMode_CheckedChanged);
            //
            // filenameStatusLabel
            //
            this.filenameStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filenameStatusLabel.AutoEllipsis = true;
            this.filenameStatusLabel.Location = new System.Drawing.Point(14, 38);
            this.filenameStatusLabel.Name = "filenameStatusLabel";
            this.filenameStatusLabel.AccessibleName = "Filename preservation status";
            this.filenameStatusLabel.Size = new System.Drawing.Size(440, 18);
            this.filenameStatusLabel.TabIndex = 2;
            this.filenameStatusLabel.Text = "Select a source folder to check its file names.";
            //
            // exportNamesButton
            //
            this.exportNamesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.exportNamesButton.Location = new System.Drawing.Point(618, 20);
            this.exportNamesButton.Name = "exportNamesButton";
            this.exportNamesButton.Size = new System.Drawing.Size(120, 28);
            this.exportNamesButton.TabIndex = 4;
            this.exportNamesButton.Text = "E&xport Report...";
            this.toolTip.SetToolTip(this.exportNamesButton, "Save the filename conversion report as TXT or CSV");
            this.exportNamesButton.UseVisualStyleBackColor = true;
            this.exportNamesButton.Click += new System.EventHandler(this.ExportNamesButton_Click);
            //
            // outputGroupBox
            //
            this.outputGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputGroupBox.Controls.Add(this.outputLabel);
            this.outputGroupBox.Controls.Add(this.outputTextBox);
            this.outputGroupBox.Controls.Add(this.saveAsButton);
            this.outputGroupBox.Controls.Add(this.verifyCheckBox);
            this.outputGroupBox.Controls.Add(this.hashCheckBox);
            this.outputGroupBox.Location = new System.Drawing.Point(16, 380);
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.Size = new System.Drawing.Size(752, 80);
            this.outputGroupBox.TabIndex = 5;
            this.outputGroupBox.TabStop = false;
            this.outputGroupBox.Text = "Output Destination";
            //
            // outputLabel
            //
            this.outputLabel.AutoSize = true;
            this.outputLabel.Location = new System.Drawing.Point(14, 27);
            this.outputLabel.Name = "outputLabel";
            this.outputLabel.Size = new System.Drawing.Size(50, 15);
            this.outputLabel.TabIndex = 0;
            this.outputLabel.Text = "ISO File:";
            //
            // outputTextBox
            //
            this.outputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputTextBox.Location = new System.Drawing.Point(124, 24);
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.AccessibleName = "ISO file";
            this.outputTextBox.Size = new System.Drawing.Size(516, 23);
            this.outputTextBox.TabIndex = 1;
            this.toolTip.SetToolTip(this.outputTextBox, "Where the ISO image file will be saved.");
            this.outputTextBox.TextChanged += new System.EventHandler(this.OutputTextBox_TextChanged);
            this.outputTextBox.Leave += new System.EventHandler(this.OutputTextBox_Leave);
            //
            // saveAsButton
            //
            this.saveAsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveAsButton.Location = new System.Drawing.Point(648, 22);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(90, 28);
            this.saveAsButton.TabIndex = 2;
            this.saveAsButton.Text = "&Save As...";
            this.toolTip.SetToolTip(this.saveAsButton, "Choose where to save the ISO file (Ctrl+S)");
            this.saveAsButton.UseVisualStyleBackColor = true;
            this.saveAsButton.Click += new System.EventHandler(this.SaveAsButton_Click);
            //
            // verifyCheckBox
            //
            this.verifyCheckBox.AutoSize = true;
            this.verifyCheckBox.Location = new System.Drawing.Point(124, 54);
            this.verifyCheckBox.Name = "verifyCheckBox";
            this.verifyCheckBox.Size = new System.Drawing.Size(218, 19);
            this.verifyCheckBox.TabIndex = 3;
            this.verifyCheckBox.Text = "&Verify the image after creating it";
            this.toolTip.SetToolTip(this.verifyCheckBox, "Reads the new image back with an independent reader and compares every path and size with the source.");
            this.verifyCheckBox.UseVisualStyleBackColor = true;
            this.verifyCheckBox.CheckedChanged += new System.EventHandler(this.VerifyOptions_Changed);
            //
            // hashCheckBox
            //
            this.hashCheckBox.AutoSize = true;
            this.hashCheckBox.Location = new System.Drawing.Point(380, 54);
            this.hashCheckBox.Name = "hashCheckBox";
            this.hashCheckBox.Size = new System.Drawing.Size(249, 19);
            this.hashCheckBox.TabIndex = 4;
            this.hashCheckBox.Text = "Include SHA-&256 content check (slower)";
            this.toolTip.SetToolTip(this.hashCheckBox, "Also compares the SHA-256 of every file with the source. Reads all data again.");
            this.hashCheckBox.UseVisualStyleBackColor = true;
            this.hashCheckBox.CheckedChanged += new System.EventHandler(this.VerifyOptions_Changed);
            //
            // progressGroupBox
            //
            this.progressGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressGroupBox.Controls.Add(this.progressBar);
            this.progressGroupBox.Controls.Add(this.percentLabel);
            this.progressGroupBox.Controls.Add(this.statusLabel);
            this.progressGroupBox.Controls.Add(this.currentFileLabel);
            this.progressGroupBox.Controls.Add(this.bytesLabel);
            this.progressGroupBox.Controls.Add(this.filesLabel);
            this.progressGroupBox.Controls.Add(this.elapsedLabel);
            this.progressGroupBox.Controls.Add(this.remainingLabel);
            this.progressGroupBox.Controls.Add(this.speedLabel);
            this.progressGroupBox.Location = new System.Drawing.Point(16, 466);
            this.progressGroupBox.Name = "progressGroupBox";
            this.progressGroupBox.Size = new System.Drawing.Size(752, 118);
            this.progressGroupBox.TabIndex = 6;
            this.progressGroupBox.TabStop = false;
            this.progressGroupBox.Text = "Progress";
            //
            // progressBar
            //
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(14, 18);
            this.progressBar.Name = "progressBar";
            this.progressBar.AccessibleName = "Progress";
            this.progressBar.AccessibleDescription = "Progress of the current operation";
            this.progressBar.Size = new System.Drawing.Size(650, 22);
            this.progressBar.TabIndex = 0;
            //
            // percentLabel
            //
            this.percentLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.percentLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.percentLabel.Location = new System.Drawing.Point(668, 19);
            this.percentLabel.Name = "percentLabel";
            this.percentLabel.Size = new System.Drawing.Size(70, 20);
            this.percentLabel.TabIndex = 1;
            this.percentLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // statusLabel
            //
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Location = new System.Drawing.Point(14, 42);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.AccessibleName = "Status";
            this.statusLabel.Size = new System.Drawing.Size(724, 18);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Ready.";
            //
            // currentFileLabel
            //
            this.currentFileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.currentFileLabel.AutoEllipsis = true;
            this.currentFileLabel.Location = new System.Drawing.Point(14, 60);
            this.currentFileLabel.Name = "currentFileLabel";
            this.currentFileLabel.Size = new System.Drawing.Size(724, 18);
            this.currentFileLabel.TabIndex = 3;
            this.currentFileLabel.Tag = "dim";
            //
            // bytesLabel
            //
            this.bytesLabel.AutoEllipsis = true;
            this.bytesLabel.Location = new System.Drawing.Point(14, 78);
            this.bytesLabel.Name = "bytesLabel";
            this.bytesLabel.Size = new System.Drawing.Size(340, 18);
            this.bytesLabel.TabIndex = 4;
            this.bytesLabel.Text = "Written: --";
            //
            // filesLabel
            //
            this.filesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filesLabel.AutoEllipsis = true;
            this.filesLabel.Location = new System.Drawing.Point(380, 78);
            this.filesLabel.Name = "filesLabel";
            this.filesLabel.Size = new System.Drawing.Size(358, 18);
            this.filesLabel.TabIndex = 5;
            this.filesLabel.Text = "Files: --";
            this.filesLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            //
            // elapsedLabel
            //
            this.elapsedLabel.AutoEllipsis = true;
            this.elapsedLabel.Location = new System.Drawing.Point(14, 96);
            this.elapsedLabel.Name = "elapsedLabel";
            this.elapsedLabel.Size = new System.Drawing.Size(160, 18);
            this.elapsedLabel.TabIndex = 6;
            this.elapsedLabel.Text = "Elapsed: --";
            //
            // remainingLabel
            //
            this.remainingLabel.AutoEllipsis = true;
            this.remainingLabel.Location = new System.Drawing.Point(180, 96);
            this.remainingLabel.Name = "remainingLabel";
            this.remainingLabel.Size = new System.Drawing.Size(194, 18);
            this.remainingLabel.TabIndex = 7;
            this.remainingLabel.Text = "Remaining: --";
            //
            // speedLabel
            //
            this.speedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.speedLabel.AutoEllipsis = true;
            this.speedLabel.Location = new System.Drawing.Point(380, 96);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(358, 18);
            this.speedLabel.TabIndex = 8;
            this.speedLabel.Text = "Speed: --";
            this.speedLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            //
            // createButton
            //
            this.createButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.createButton.FlatAppearance.BorderSize = 0;
            this.createButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.createButton.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.createButton.Location = new System.Drawing.Point(314, 590);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(116, 32);
            this.createButton.TabIndex = 7;
            this.createButton.Tag = "primary";
            this.createButton.Text = "&Create ISO";
            this.toolTip.SetToolTip(this.createButton, "Create the ISO image (F5)");
            this.createButton.UseVisualStyleBackColor = false;
            this.createButton.EnabledChanged += new System.EventHandler(this.CreateButton_EnabledChanged);
            this.createButton.Click += new System.EventHandler(this.CreateButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Enabled = false;
            this.cancelButton.Location = new System.Drawing.Point(436, 590);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(90, 32);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "Cance&l";
            this.toolTip.SetToolTip(this.cancelButton, "Stop creating the image (Esc)");
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            //
            // openFolderButton
            //
            this.openFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.openFolderButton.Location = new System.Drawing.Point(532, 590);
            this.openFolderButton.Name = "openFolderButton";
            this.openFolderButton.Size = new System.Drawing.Size(140, 32);
            this.openFolderButton.TabIndex = 9;
            this.openFolderButton.Text = "&Open Output Folder";
            this.toolTip.SetToolTip(this.openFolderButton, "Show the output folder in File Explorer");
            this.openFolderButton.UseVisualStyleBackColor = true;
            this.openFolderButton.Click += new System.EventHandler(this.OpenFolderButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Location = new System.Drawing.Point(678, 590);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(90, 32);
            this.closeButton.TabIndex = 10;
            this.closeButton.Text = "Clos&e";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusStripLabel,
            this.brandStripLabel,
            this.formatStripLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 628);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.SizingGrip = false;
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 11;
            //
            // statusStripLabel
            //
            this.statusStripLabel.Name = "statusStripLabel";
            this.statusStripLabel.Size = new System.Drawing.Size(655, 17);
            this.statusStripLabel.Spring = true;
            this.statusStripLabel.Text = "Ready";
            this.statusStripLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // brandStripLabel
            //
            this.brandStripLabel.Name = "brandStripLabel";
            this.brandStripLabel.Size = new System.Drawing.Size(260, 17);
            this.brandStripLabel.Text = "© HTREASURE LTD · An HTREASURE LTD product";
            //
            // formatStripLabel
            //
            this.formatStripLabel.Name = "formatStripLabel";
            this.formatStripLabel.Size = new System.Drawing.Size(114, 17);
            this.formatStripLabel.Text = "ISO 9660 + Joliet";
            //
            // errorProvider
            //
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errorProvider.ContainerControl = this;
            //
            // elapsedTimer
            //
            this.elapsedTimer.Interval = 500;
            this.elapsedTimer.Tick += new System.EventHandler(this.ElapsedTimer_Tick);
            //
            // MainForm
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 650);
            this.Controls.Add(this.sourceGroupBox);
            this.Controls.Add(this.isoGroupBox);
            this.Controls.Add(this.filenameGroupBox);
            this.Controls.Add(this.outputGroupBox);
            this.Controls.Add(this.progressGroupBox);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.openFolderButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.headerPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HT ISO Creator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.DragLeave += new System.EventHandler(this.MainForm_DragLeave);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.headerIcon)).EndInit();
            this.sourceGroupBox.ResumeLayout(false);
            this.sourceGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.customCapacityUpDown)).EndInit();
            this.isoGroupBox.ResumeLayout(false);
            this.isoGroupBox.PerformLayout();
            this.filenameGroupBox.ResumeLayout(false);
            this.filenameGroupBox.PerformLayout();
            this.outputGroupBox.ResumeLayout(false);
            this.outputGroupBox.PerformLayout();
            this.progressGroupBox.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem selectSourceMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chooseOutputMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createIsoMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsMenu;
        private System.Windows.Forms.ToolStripMenuItem refreshMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openOutputFolderMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogFolderMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolsMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenu;
        private System.Windows.Forms.ToolStripMenuItem shortcutsMenuItem;
        private System.Windows.Forms.ToolStripSeparator helpMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem userGuideMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileSystemGuideMenuItem;
        private System.Windows.Forms.ToolStripMenuItem troubleshootingMenuItem;
        private System.Windows.Forms.ToolStripMenuItem releaseNotesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noticesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem licenceMenuItem;
        private System.Windows.Forms.ToolStripSeparator helpDocsSeparator;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutAppMenuItem;
        private System.Windows.Forms.ToolStripSeparator aboutSeparator;
        private System.Windows.Forms.ToolStripMenuItem securityHistoryMenuItem;
        private System.Windows.Forms.ToolStripMenuItem securityAdvisoriesMenuItem;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.PictureBox headerIcon;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.LinkLabel websiteLinkLabel;
        private System.Windows.Forms.Label subHeaderLabel;
        private System.Windows.Forms.GroupBox sourceGroupBox;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.TextBox sourceTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Label filesCaption;
        private System.Windows.Forms.Label filesValue;
        private System.Windows.Forms.Label foldersCaption;
        private System.Windows.Forms.Label foldersValue;
        private System.Windows.Forms.Label sourceSizeCaption;
        private System.Windows.Forms.Label sourceSizeValue;
        private System.Windows.Forms.Label isoSizeCaption;
        private System.Windows.Forms.Label isoSizeValue;
        private System.Windows.Forms.Label formatCaption;
        private System.Windows.Forms.Label formatValue;
        private System.Windows.Forms.Label freeSpaceCaption;
        private System.Windows.Forms.Label freeSpaceValue;
        private System.Windows.Forms.Label sourceSummaryLabel;
        private System.Windows.Forms.LinkLabel detailsLinkLabel;
        private System.Windows.Forms.GroupBox isoGroupBox;
        private System.Windows.Forms.Label volumeLabel;
        private System.Windows.Forms.TextBox volumeTextBox;
        private System.Windows.Forms.Label fileSystemLabel;
        private System.Windows.Forms.ComboBox fileSystemComboBox;
        private System.Windows.Forms.Label volumeHintLabel;
        private System.Windows.Forms.GroupBox filenameGroupBox;
        private System.Windows.Forms.RadioButton strictModeRadioButton;
        private System.Windows.Forms.RadioButton compatibilityModeRadioButton;
        private System.Windows.Forms.Label filenameStatusLabel;
        private System.Windows.Forms.Button exportNamesButton;
        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.Label outputLabel;
        private System.Windows.Forms.TextBox outputTextBox;
        private System.Windows.Forms.Button saveAsButton;
        private System.Windows.Forms.GroupBox progressGroupBox;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label percentLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label currentFileLabel;
        private System.Windows.Forms.Label bytesLabel;
        private System.Windows.Forms.Label filesLabel;
        private System.Windows.Forms.Label elapsedLabel;
        private System.Windows.Forms.Label remainingLabel;
        private System.Windows.Forms.Label speedLabel;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button openFolderButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusStripLabel;
        private System.Windows.Forms.ToolStripStatusLabel formatStripLabel;
        private System.Windows.Forms.ToolStripStatusLabel brandStripLabel;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Timer elapsedTimer;
        private System.Windows.Forms.ToolStripMenuItem verifyIsoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem isoInfoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compatibilityMenuItem;
        private System.Windows.Forms.ToolStripMenuItem historyMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolsMenuSeparator2;
        private System.Windows.Forms.ToolStripMenuItem viewLogMenuItem;
        private System.Windows.Forms.Label capacityLabel;
        private System.Windows.Forms.ComboBox capacityComboBox;
        private System.Windows.Forms.NumericUpDown customCapacityUpDown;
        private System.Windows.Forms.Label customCapacityUnitLabel;
        private System.Windows.Forms.Label capacityStatusLabel;
        private System.Windows.Forms.LinkLabel compatibilityLinkLabel;
        private System.Windows.Forms.CheckBox verifyCheckBox;
        private System.Windows.Forms.CheckBox hashCheckBox;
    }
}
