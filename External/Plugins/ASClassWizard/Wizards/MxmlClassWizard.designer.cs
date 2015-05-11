
using ASClassWizard.Resources;

namespace ASClassWizard.Wizards
{
    partial class MxmlClassWizard
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
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.implementBrowse = new System.Windows.Forms.Button();
            this.implementRemove = new System.Windows.Forms.Button();
            this.baseBox = new System.Windows.Forms.TextBox();
            this.classBox = new System.Windows.Forms.TextBox();
            this.packageBox = new System.Windows.Forms.TextBox();
            this.classLabel = new System.Windows.Forms.Label();
            this.baseLabel = new System.Windows.Forms.Label();
            this.packageLabel = new System.Windows.Forms.Label();
            this.packageBrowse = new System.Windows.Forms.Button();
            this.baseBrowse = new System.Windows.Forms.Button();
            this.errorLabel = new System.Windows.Forms.Label();
            this.errorIcon = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel9 = new System.Windows.Forms.FlowLayoutPanel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.generationLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.namespacesAdd = new System.Windows.Forms.Button();
            this.namespacesRemove = new System.Windows.Forms.Button();
            this.namespacesList = new System.Windows.Forms.ListBox();
            this.namespacesLabel = new System.Windows.Forms.Label();
            this.implementLabel = new System.Windows.Forms.Label();
            this.implementList = new System.Windows.Forms.ListBox();
            this.interfaceCheck = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorIcon)).BeginInit();
            this.flowLayoutPanel6.SuspendLayout();
            this.flowLayoutPanel9.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.implementBrowse);
            this.flowLayoutPanel4.Controls.Add(this.implementRemove);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(374, 151);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(86, 63);
            this.flowLayoutPanel4.TabIndex = 13;
            // 
            // implementBrowse
            // 
            this.implementBrowse.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.implementBrowse.Location = new System.Drawing.Point(3, 3);
            this.implementBrowse.Name = "implementBrowse";
            this.implementBrowse.Size = new System.Drawing.Size(74, 23);
            this.implementBrowse.TabIndex = 0;
            this.implementBrowse.Text = "Browse...";
            this.implementBrowse.UseVisualStyleBackColor = true;
            this.implementBrowse.Click += new System.EventHandler(this.ImplementBrowse_Click);
            // 
            // implementRemove
            // 
            this.implementRemove.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.implementRemove.Enabled = false;
            this.implementRemove.Location = new System.Drawing.Point(3, 32);
            this.implementRemove.Name = "implementRemove";
            this.implementRemove.Size = new System.Drawing.Size(74, 23);
            this.implementRemove.TabIndex = 1;
            this.implementRemove.Text = "Remove";
            this.implementRemove.UseVisualStyleBackColor = true;
            this.implementRemove.Click += new System.EventHandler(this.InterfaceRemove_Click);
            // 
            // baseBox
            // 
            this.baseBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.baseBox.Enabled = false;
            this.baseBox.Location = new System.Drawing.Point(105, 63);
            this.baseBox.Name = "baseBox";
            this.baseBox.Size = new System.Drawing.Size(263, 20);
            this.baseBox.TabIndex = 6;
            this.baseBox.TextChanged += new System.EventHandler(this.BaseBox_TextChanged);
            // 
            // classBox
            // 
            this.classBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.classBox.Location = new System.Drawing.Point(105, 34);
            this.classBox.Name = "classBox";
            this.classBox.Size = new System.Drawing.Size(263, 20);
            this.classBox.TabIndex = 4;
            this.classBox.Text = "NewClass";
            this.classBox.TextChanged += new System.EventHandler(this.ClassBox_TextChanged);
            // 
            // packageBox
            // 
            this.packageBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.packageBox.Location = new System.Drawing.Point(105, 5);
            this.packageBox.Name = "packageBox";
            this.packageBox.Size = new System.Drawing.Size(263, 20);
            this.packageBox.TabIndex = 1;
            this.packageBox.TextChanged += new System.EventHandler(this.PackageBox_TextChanged);
            // 
            // classLabel
            // 
            this.classLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.classLabel.AutoSize = true;
            this.classLabel.Location = new System.Drawing.Point(3, 37);
            this.classLabel.Name = "classLabel";
            this.classLabel.Size = new System.Drawing.Size(32, 13);
            this.classLabel.TabIndex = 3;
            this.classLabel.Text = "Class";
            // 
            // baseLabel
            // 
            this.baseLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.baseLabel.AutoSize = true;
            this.baseLabel.Location = new System.Drawing.Point(3, 66);
            this.baseLabel.Name = "baseLabel";
            this.baseLabel.Size = new System.Drawing.Size(58, 13);
            this.baseLabel.TabIndex = 5;
            this.baseLabel.Text = "Base class";
            // 
            // packageLabel
            // 
            this.packageLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.packageLabel.AutoSize = true;
            this.packageLabel.Location = new System.Drawing.Point(3, 8);
            this.packageLabel.Name = "packageLabel";
            this.packageLabel.Size = new System.Drawing.Size(50, 13);
            this.packageLabel.TabIndex = 0;
            this.packageLabel.Text = "Package";
            // 
            // packageBrowse
            // 
            this.packageBrowse.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.packageBrowse.Location = new System.Drawing.Point(378, 3);
            this.packageBrowse.Name = "packageBrowse";
            this.packageBrowse.Size = new System.Drawing.Size(74, 23);
            this.packageBrowse.TabIndex = 2;
            this.packageBrowse.Text = "Browse...";
            this.packageBrowse.UseVisualStyleBackColor = true;
            this.packageBrowse.Click += new System.EventHandler(this.PackageBrowse_Click);
            // 
            // baseBrowse
            // 
            this.baseBrowse.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.baseBrowse.Location = new System.Drawing.Point(378, 61);
            this.baseBrowse.Name = "baseBrowse";
            this.baseBrowse.Size = new System.Drawing.Size(74, 23);
            this.baseBrowse.TabIndex = 7;
            this.baseBrowse.Text = "Browse...";
            this.baseBrowse.UseVisualStyleBackColor = true;
            this.baseBrowse.Click += new System.EventHandler(this.BaseBrowse_Click);
            // 
            // errorLabel
            // 
            this.errorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.errorLabel.AutoSize = true;
            this.errorLabel.ForeColor = System.Drawing.Color.Black;
            this.errorLabel.Location = new System.Drawing.Point(25, 4);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(29, 13);
            this.errorLabel.TabIndex = 0;
            this.errorLabel.Text = "Error";
            // 
            // errorIcon
            // 
            this.errorIcon.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.errorIcon.Location = new System.Drawing.Point(3, 3);
            this.errorIcon.Name = "errorIcon";
            this.errorIcon.Size = new System.Drawing.Size(16, 16);
            this.errorIcon.TabIndex = 0;
            this.errorIcon.TabStop = false;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel6.Controls.Add(this.errorIcon);
            this.flowLayoutPanel6.Controls.Add(this.errorLabel);
            this.flowLayoutPanel6.Location = new System.Drawing.Point(5, 3);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Size = new System.Drawing.Size(296, 23);
            this.flowLayoutPanel6.TabIndex = 0;
            // 
            // flowLayoutPanel9
            // 
            this.flowLayoutPanel9.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel9.BackColor = System.Drawing.SystemColors.Window;
            this.flowLayoutPanel9.Controls.Add(this.titleLabel);
            this.flowLayoutPanel9.Location = new System.Drawing.Point(12, 12);
            this.flowLayoutPanel9.Name = "flowLayoutPanel9";
            this.flowLayoutPanel9.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel9.Size = new System.Drawing.Size(466, 35);
            this.flowLayoutPanel9.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(8, 5);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(148, 13);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "New Actionscript 2 Class";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tableLayoutPanel2);
            this.groupBox2.Location = new System.Drawing.Point(10, 53);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(468, 279);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.42382F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 72.57618F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.tableLayoutPanel2.Controls.Add(this.generationLabel, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel2, 2, 3);
            this.tableLayoutPanel2.Controls.Add(this.namespacesList, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.namespacesLabel, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.baseBrowse, 2, 2);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel4, 2, 4);
            this.tableLayoutPanel2.Controls.Add(this.packageBrowse, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.packageLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.implementLabel, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.packageBox, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.baseBox, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.baseLabel, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.implementList, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.classLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.classBox, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.interfaceCheck, 1, 5);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 12);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 6;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 63F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 63F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(460, 263);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // generationLabel
            // 
            this.generationLabel.AutoSize = true;
            this.generationLabel.Location = new System.Drawing.Point(3, 214);
            this.generationLabel.Name = "generationLabel";
            this.generationLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.generationLabel.Size = new System.Drawing.Size(85, 19);
            this.generationLabel.TabIndex = 14;
            this.generationLabel.Text = "Code generation";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.namespacesAdd);
            this.flowLayoutPanel2.Controls.Add(this.namespacesRemove);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(374, 88);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(86, 63);
            this.flowLayoutPanel2.TabIndex = 10;
            // 
            // namespacesAdd
            // 
            this.namespacesAdd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.namespacesAdd.Location = new System.Drawing.Point(3, 3);
            this.namespacesAdd.Name = "namespacesAdd";
            this.namespacesAdd.Size = new System.Drawing.Size(74, 23);
            this.namespacesAdd.TabIndex = 0;
            this.namespacesAdd.Text = "Add";
            this.namespacesAdd.UseVisualStyleBackColor = true;
            this.namespacesAdd.Click += new System.EventHandler(this.NamespacesAdd_Click);
            // 
            // namespacesRemove
            // 
            this.namespacesRemove.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.namespacesRemove.Enabled = false;
            this.namespacesRemove.Location = new System.Drawing.Point(3, 32);
            this.namespacesRemove.Name = "namespacesRemove";
            this.namespacesRemove.Size = new System.Drawing.Size(74, 23);
            this.namespacesRemove.TabIndex = 1;
            this.namespacesRemove.Text = "Remove";
            this.namespacesRemove.UseVisualStyleBackColor = true;
            this.namespacesRemove.Click += new System.EventHandler(this.NamespacesRemove_Click);
            // 
            // namespacesList
            // 
            this.namespacesList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.namespacesList.FormattingEnabled = true;
            this.namespacesList.Location = new System.Drawing.Point(105, 91);
            this.namespacesList.Name = "namespacesList";
            this.namespacesList.Size = new System.Drawing.Size(263, 56);
            this.namespacesList.TabIndex = 9;
            this.namespacesList.SelectedIndexChanged += new System.EventHandler(this.NamespacesList_SelectedIndexChanged);
            // 
            // namespacesLabel
            // 
            this.namespacesLabel.AutoSize = true;
            this.namespacesLabel.Location = new System.Drawing.Point(3, 88);
            this.namespacesLabel.Name = "namespacesLabel";
            this.namespacesLabel.Size = new System.Drawing.Size(69, 13);
            this.namespacesLabel.TabIndex = 8;
            this.namespacesLabel.Text = "Namespaces";
            // 
            // implementLabel
            // 
            this.implementLabel.AutoSize = true;
            this.implementLabel.Location = new System.Drawing.Point(3, 151);
            this.implementLabel.Name = "implementLabel";
            this.implementLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.implementLabel.Size = new System.Drawing.Size(55, 19);
            this.implementLabel.TabIndex = 11;
            this.implementLabel.Text = "Implement";
            // 
            // implementList
            // 
            this.implementList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.implementList.FormattingEnabled = true;
            this.implementList.Location = new System.Drawing.Point(105, 154);
            this.implementList.Name = "implementList";
            this.implementList.Size = new System.Drawing.Size(263, 56);
            this.implementList.TabIndex = 12;
            // 
            // interfaceCheck
            // 
            this.interfaceCheck.AutoSize = true;
            this.interfaceCheck.Checked = true;
            this.interfaceCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.interfaceCheck.Enabled = false;
            this.interfaceCheck.Location = new System.Drawing.Point(105, 217);
            this.interfaceCheck.Name = "interfaceCheck";
            this.interfaceCheck.Size = new System.Drawing.Size(124, 17);
            this.interfaceCheck.TabIndex = 15;
            this.interfaceCheck.Text = "Implement Interfaces";
            this.interfaceCheck.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Controls.Add(this.okButton);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel6);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 338);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(466, 29);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(388, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(307, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // MxmlClassWizard
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(490, 379);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.flowLayoutPanel9);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MxmlClassWizard";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New ActionScript Class";
            this.Load += new System.EventHandler(this.AS3ClassWizard_Load);
            this.flowLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorIcon)).EndInit();
            this.flowLayoutPanel6.ResumeLayout(false);
            this.flowLayoutPanel6.PerformLayout();
            this.flowLayoutPanel9.ResumeLayout(false);
            this.flowLayoutPanel9.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label packageLabel;
        private System.Windows.Forms.TextBox packageBox;
        private System.Windows.Forms.Button packageBrowse;
        private System.Windows.Forms.Label classLabel;
        private System.Windows.Forms.TextBox classBox;
        private System.Windows.Forms.Label baseLabel;
        private System.Windows.Forms.TextBox baseBox;
        private System.Windows.Forms.Button baseBrowse;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Button implementBrowse;
        private System.Windows.Forms.Button implementRemove;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.PictureBox errorIcon;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel9;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label implementLabel;
        private System.Windows.Forms.ListBox implementList;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button namespacesAdd;
        private System.Windows.Forms.Button namespacesRemove;
        private System.Windows.Forms.ListBox namespacesList;
        private System.Windows.Forms.Label namespacesLabel;
        private System.Windows.Forms.Label generationLabel;
        private System.Windows.Forms.CheckBox interfaceCheck;
    }
}

