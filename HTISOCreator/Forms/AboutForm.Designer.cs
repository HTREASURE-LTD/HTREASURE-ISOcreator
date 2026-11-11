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
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.licenceLabel = new System.Windows.Forms.Label();
            this.noticesLinkLabel = new System.Windows.Forms.LinkLabel();
            this.okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(58)))), ((int)(((byte)(110)))));
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
            this.developerLabel.Location = new System.Drawing.Point(20, 54);
            this.developerLabel.Name = "developerLabel";
            this.developerLabel.Size = new System.Drawing.Size(63, 15);
            this.developerLabel.TabIndex = 1;
            this.developerLabel.Text = "Publisher:";
            //
            // developerValueLabel
            //
            this.developerValueLabel.AutoSize = true;
            this.developerValueLabel.Location = new System.Drawing.Point(100, 54);
            this.developerValueLabel.Name = "developerValueLabel";
            this.developerValueLabel.Size = new System.Drawing.Size(73, 15);
            this.developerValueLabel.TabIndex = 2;
            this.developerValueLabel.Text = "HTREASURE LTD";
            //
            // versionLabel
            //
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(20, 76);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(48, 15);
            this.versionLabel.TabIndex = 3;
            this.versionLabel.Text = "Version:";
            //
            // versionValueLabel
            //
            this.versionValueLabel.AutoSize = true;
            this.versionValueLabel.Location = new System.Drawing.Point(100, 76);
            this.versionValueLabel.Name = "versionValueLabel";
            this.versionValueLabel.Size = new System.Drawing.Size(37, 15);
            this.versionValueLabel.TabIndex = 4;
            this.versionValueLabel.Text = "1.0.0";
            //
            // descriptionLabel
            //
            this.descriptionLabel.Location = new System.Drawing.Point(20, 104);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(420, 48);
            this.descriptionLabel.TabIndex = 5;
            this.descriptionLabel.Text = "HT ISO Creator creates and verifies standard data ISO images (ISO 9660, Joliet and UDF) from folders. It does not create bootable images.";
            //
            // copyrightLabel
            //
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.Location = new System.Drawing.Point(20, 160);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size(232, 15);
            this.copyrightLabel.TabIndex = 6;
            this.copyrightLabel.Text = "Copyright © 2026 HTREASURE LTD. All rights reserved.";
            //
            // licenceLabel
            //
            this.licenceLabel.ForeColor = System.Drawing.Color.DimGray;
            this.licenceLabel.Location = new System.Drawing.Point(20, 184);
            this.licenceLabel.Name = "licenceLabel";
            this.licenceLabel.Size = new System.Drawing.Size(420, 32);
            this.licenceLabel.TabIndex = 7;
            this.licenceLabel.Tag = "dim";
            this.licenceLabel.Text = "ISO 9660 and Joliet images are written with DiscUtils 0.16.13 (MIT License); UDF images with the Windows IMAPI2 component.";
            //
            // noticesLinkLabel
            //
            this.noticesLinkLabel.AutoSize = true;
            this.noticesLinkLabel.Location = new System.Drawing.Point(20, 216);
            this.noticesLinkLabel.Name = "noticesLinkLabel";
            this.noticesLinkLabel.Size = new System.Drawing.Size(135, 15);
            this.noticesLinkLabel.TabIndex = 8;
            this.noticesLinkLabel.TabStop = true;
            this.noticesLinkLabel.Text = "View third-party notices";
            this.noticesLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.NoticesLinkLabel_LinkClicked);
            //
            // okButton
            //
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(350, 244);
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
            this.ClientSize = new System.Drawing.Size(460, 290);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.developerLabel);
            this.Controls.Add(this.developerValueLabel);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.versionValueLabel);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.copyrightLabel);
            this.Controls.Add(this.licenceLabel);
            this.Controls.Add(this.noticesLinkLabel);
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
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.Windows.Forms.Label licenceLabel;
        private System.Windows.Forms.LinkLabel noticesLinkLabel;
        private System.Windows.Forms.Button okButton;
    }
}
