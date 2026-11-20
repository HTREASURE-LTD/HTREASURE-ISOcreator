namespace HTISOCreator.Forms
{
    partial class AboutForm
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.developerLabel = new System.Windows.Forms.Label();
            this.developerValueLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.versionValueLabel = new System.Windows.Forms.Label();
            this.websiteLabel = new System.Windows.Forms.Label();
            this.websiteLinkLabel = new System.Windows.Forms.LinkLabel();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.licenceLabel = new System.Windows.Forms.Label();
            this.noticesLinkLabel = new System.Windows.Forms.LinkLabel();
            this.licenceLinkLabel = new System.Windows.Forms.LinkLabel();
            this.attributionLabel = new System.Windows.Forms.Label();
            this.supportLabel = new System.Windows.Forms.Label();
            this.supportValueLabel = new System.Windows.Forms.Label();
            this.companyLabel = new System.Windows.Forms.Label();
            this.securityLinkLabel = new System.Windows.Forms.LinkLabel();
            this.okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(61)))), ((int)(((byte)(145)))));
            this.titleLabel.Location = new System.Drawing.Point(18, 16);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(140, 25);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Tag = "accent";
            this.titleLabel.Text = "HT ISO Creator";
            //
            // developerLabel
            //
            this.developerLabel.AutoSize = true;
            this.developerLabel.Location = new System.Drawing.Point(20, 68);
            this.developerLabel.Name = "developerLabel";
            this.developerLabel.Size = new System.Drawing.Size(63, 15);
            this.developerLabel.TabIndex = 1;
            this.developerLabel.Text = "Publisher:";
            //
            // developerValueLabel
            //
            this.developerValueLabel.AutoSize = true;
            this.developerValueLabel.Location = new System.Drawing.Point(100, 68);
            this.developerValueLabel.Name = "developerValueLabel";
            this.developerValueLabel.Size = new System.Drawing.Size(73, 15);
            this.developerValueLabel.TabIndex = 2;
            this.developerValueLabel.Text = "HTREASURE LTD";
            //
            // versionLabel
            //
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(20, 90);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(48, 15);
            this.versionLabel.TabIndex = 3;
            this.versionLabel.Text = "Version:";
            //
            // versionValueLabel
            //
            this.versionValueLabel.AutoSize = true;
            this.versionValueLabel.Location = new System.Drawing.Point(100, 90);
            this.versionValueLabel.Name = "versionValueLabel";
            this.versionValueLabel.Size = new System.Drawing.Size(37, 15);
            this.versionValueLabel.TabIndex = 4;
            this.versionValueLabel.Text = "1.0.2";
            //
            // websiteLabel
            //
            this.websiteLabel.AutoSize = true;
            this.websiteLabel.Location = new System.Drawing.Point(20, 112);
            this.websiteLabel.Name = "websiteLabel";
            this.websiteLabel.Size = new System.Drawing.Size(52, 15);
            this.websiteLabel.TabIndex = 10;
            this.websiteLabel.Text = "Website:";
            //
            // websiteLinkLabel
            //
            this.websiteLinkLabel.AutoSize = true;
            this.websiteLinkLabel.Location = new System.Drawing.Point(100, 112);
            this.websiteLinkLabel.Name = "websiteLinkLabel";
            this.websiteLinkLabel.Size = new System.Drawing.Size(108, 15);
            this.websiteLinkLabel.TabIndex = 11;
            this.websiteLinkLabel.TabStop = true;
            this.websiteLinkLabel.Text = "www.htreasure.com";
            this.websiteLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebsiteLinkLabel_LinkClicked);
            //
            // descriptionLabel
            //
            this.descriptionLabel.Location = new System.Drawing.Point(20, 182);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(460, 40);
            this.descriptionLabel.TabIndex = 5;
            this.descriptionLabel.Text = "HT ISO Creator creates and verifies standard data ISO images (ISO 9660, Joliet and UDF) from folders. It does not create bootable images.";
            //
            // copyrightLabel
            //
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.Location = new System.Drawing.Point(20, 228);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size(232, 15);
            this.copyrightLabel.TabIndex = 6;
            this.copyrightLabel.Text = "Copyright © 2026 HTREASURE LTD. All rights reserved.";
            //
            // licenceLabel
            //
            this.licenceLabel.ForeColor = System.Drawing.Color.DimGray;
            this.licenceLabel.Location = new System.Drawing.Point(20, 252);
            this.licenceLabel.Name = "licenceLabel";
            this.licenceLabel.Size = new System.Drawing.Size(460, 32);
            this.licenceLabel.TabIndex = 7;
            this.licenceLabel.Tag = "dim";
            this.licenceLabel.Text = "ISO 9660 and Joliet images are written with DiscUtils 0.16.13 (MIT License); UDF images with the Windows IMAPI2 component.";
            //
            // noticesLinkLabel
            //
            this.noticesLinkLabel.AutoSize = true;
            this.noticesLinkLabel.Location = new System.Drawing.Point(20, 288);
            this.noticesLinkLabel.Name = "noticesLinkLabel";
            this.noticesLinkLabel.Size = new System.Drawing.Size(135, 15);
            this.noticesLinkLabel.TabIndex = 8;
            this.noticesLinkLabel.TabStop = true;
            this.noticesLinkLabel.Text = "View third-party notices";
            this.noticesLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.NoticesLinkLabel_LinkClicked);
            //
            // licenceLinkLabel
            //
            this.licenceLinkLabel.AutoSize = true;
            this.licenceLinkLabel.Location = new System.Drawing.Point(178, 288);
            this.licenceLinkLabel.Name = "licenceLinkLabel";
            this.licenceLinkLabel.Size = new System.Drawing.Size(136, 15);
            this.licenceLinkLabel.TabIndex = 12;
            this.licenceLinkLabel.TabStop = true;
            this.licenceLinkLabel.Text = "View licence agreement";
            this.licenceLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LicenceLinkLabel_LinkClicked);
            //
            // attributionLabel
            //
            this.attributionLabel.AutoSize = true;
            this.attributionLabel.ForeColor = System.Drawing.Color.DimGray;
            this.attributionLabel.Location = new System.Drawing.Point(20, 42);
            this.attributionLabel.Name = "attributionLabel";
            this.attributionLabel.Size = new System.Drawing.Size(150, 15);
            this.attributionLabel.TabIndex = 13;
            this.attributionLabel.Tag = "dim";
            this.attributionLabel.Text = "An HTREASURE LTD product";
            //
            // supportLabel
            //
            this.supportLabel.AutoSize = true;
            this.supportLabel.Location = new System.Drawing.Point(20, 134);
            this.supportLabel.Name = "supportLabel";
            this.supportLabel.Size = new System.Drawing.Size(52, 15);
            this.supportLabel.TabIndex = 14;
            this.supportLabel.Text = "Support:";
            //
            // supportValueLabel
            //
            this.supportValueLabel.AutoSize = true;
            this.supportValueLabel.Location = new System.Drawing.Point(100, 134);
            this.supportValueLabel.Name = "supportValueLabel";
            this.supportValueLabel.Size = new System.Drawing.Size(130, 15);
            this.supportValueLabel.TabIndex = 15;
            this.supportValueLabel.Text = "support@htreasure.com";
            //
            // companyLabel
            //
            this.companyLabel.ForeColor = System.Drawing.Color.DimGray;
            this.companyLabel.Location = new System.Drawing.Point(20, 158);
            this.companyLabel.Name = "companyLabel";
            this.companyLabel.Size = new System.Drawing.Size(460, 17);
            this.companyLabel.TabIndex = 16;
            this.companyLabel.Tag = "dim";
            this.companyLabel.Text = "HTREASURE LTD is registered in England and Wales, company no. 17178941.";
            //
            // securityLinkLabel
            //
            this.securityLinkLabel.AutoSize = true;
            this.securityLinkLabel.Location = new System.Drawing.Point(334, 288);
            this.securityLinkLabel.Name = "securityLinkLabel";
            this.securityLinkLabel.Size = new System.Drawing.Size(146, 15);
            this.securityLinkLabel.TabIndex = 17;
            this.securityLinkLabel.TabStop = true;
            this.securityLinkLabel.Text = "Security advisory history";
            this.securityLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SecurityLinkLabel_LinkClicked);
            //
            // okButton
            //
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(390, 322);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(90, 30);
            this.okButton.TabIndex = 9;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            //
            // AboutForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.okButton;
            this.ClientSize = new System.Drawing.Size(500, 368);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.developerLabel);
            this.Controls.Add(this.developerValueLabel);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.versionValueLabel);
            this.Controls.Add(this.websiteLabel);
            this.Controls.Add(this.websiteLinkLabel);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.copyrightLabel);
            this.Controls.Add(this.licenceLabel);
            this.Controls.Add(this.noticesLinkLabel);
            this.Controls.Add(this.licenceLinkLabel);
            this.Controls.Add(this.attributionLabel);
            this.Controls.Add(this.supportLabel);
            this.Controls.Add(this.supportValueLabel);
            this.Controls.Add(this.companyLabel);
            this.Controls.Add(this.securityLinkLabel);
            this.Controls.Add(this.okButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About HT ISO Creator";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label developerLabel;
        private System.Windows.Forms.Label developerValueLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label versionValueLabel;
        private System.Windows.Forms.Label websiteLabel;
        private System.Windows.Forms.LinkLabel websiteLinkLabel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.Windows.Forms.Label licenceLabel;
        private System.Windows.Forms.LinkLabel noticesLinkLabel;
        private System.Windows.Forms.LinkLabel licenceLinkLabel;
        private System.Windows.Forms.Label attributionLabel;
        private System.Windows.Forms.Label supportLabel;
        private System.Windows.Forms.Label supportValueLabel;
        private System.Windows.Forms.Label companyLabel;
        private System.Windows.Forms.LinkLabel securityLinkLabel;
        private System.Windows.Forms.Button okButton;
    }
}
