namespace ASClassWizard.Wizards
{
    partial class NamespaceDialog
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
            this.prefixLabel = new System.Windows.Forms.Label();
            this.prefixBox = new System.Windows.Forms.TextBox();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameComboBox = new System.Windows.Forms.ComboBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // prefixLabel
            // 
            this.prefixLabel.AutoSize = true;
            this.prefixLabel.Location = new System.Drawing.Point(12, 15);
            this.prefixLabel.Name = "prefixLabel";
            this.prefixLabel.Size = new System.Drawing.Size(36, 13);
            this.prefixLabel.TabIndex = 0;
            this.prefixLabel.Text = "Prefix:";
            // 
            // prefixBox
            // 
            this.prefixBox.Location = new System.Drawing.Point(51, 12);
            this.prefixBox.Name = "prefixBox";
            this.prefixBox.Size = new System.Drawing.Size(61, 20);
            this.prefixBox.TabIndex = 1;
            this.prefixBox.Validating += new System.ComponentModel.CancelEventHandler(this.PrefixBox_Validating);
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.Location = new System.Drawing.Point(118, 15);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(38, 13);
            this.nameLabel.TabIndex = 2;
            this.nameLabel.Text = "Name:";
            // 
            // nameComboBox
            // 
            this.nameComboBox.FormattingEnabled = true;
            this.nameComboBox.Location = new System.Drawing.Point(162, 12);
            this.nameComboBox.Name = "nameComboBox";
            this.nameComboBox.Size = new System.Drawing.Size(167, 21);
            this.nameComboBox.TabIndex = 3;
            this.nameComboBox.SelectedIndexChanged += new System.EventHandler(this.NameComboBox_SelectedIndexChanged);
            this.nameComboBox.Validating += new System.ComponentModel.CancelEventHandler(this.NameComboBox_Validating);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(173, 40);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.CausesValidation = false;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(254, 40);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // NamespaceDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(341, 80);
            this.ControlBox = false;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.nameComboBox);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.prefixBox);
            this.Controls.Add(this.prefixLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "NamespaceDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "NamespaceDialog";
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label prefixLabel;
        private System.Windows.Forms.TextBox prefixBox;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.ComboBox nameComboBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ErrorProvider errorProvider;
    }
}