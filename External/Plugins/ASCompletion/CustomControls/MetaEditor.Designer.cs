namespace ASCompletion.Forms
{
    partial class MetaEditorForm
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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.metaFieldsGrid = new System.Windows.Forms.DataGridView();
            this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mandatoryColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.metaList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.removeMetaButton = new System.Windows.Forms.Button();
            this.addMetaButton = new System.Windows.Forms.Button();
            this.removeDescLangButton = new System.Windows.Forms.Button();
            this.addDescLangButton = new System.Windows.Forms.Button();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.descriptionKeyCombo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.decorationLabel = new System.Windows.Forms.Label();
            this.functionsCheck = new System.Windows.Forms.CheckBox();
            this.classesCheck = new System.Windows.Forms.CheckBox();
            this.attributesCheck = new System.Windows.Forms.CheckBox();
            this.appearsInHelpCheck = new System.Windows.Forms.CheckBox();
            this.fieldsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.metaFieldsGrid)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(339, 290);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(420, 290);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // metaFieldsGrid
            // 
            this.metaFieldsGrid.AllowUserToResizeRows = false;
            this.metaFieldsGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.metaFieldsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.metaFieldsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.nameColumn,
            this.mandatoryColumn});
            this.metaFieldsGrid.Enabled = false;
            this.metaFieldsGrid.Location = new System.Drawing.Point(-1, 144);
            this.metaFieldsGrid.Margin = new System.Windows.Forms.Padding(0);
            this.metaFieldsGrid.Name = "metaFieldsGrid";
            this.metaFieldsGrid.Size = new System.Drawing.Size(318, 128);
            this.metaFieldsGrid.TabIndex = 3;
            this.metaFieldsGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.MetaFieldsGrid_CellValidating);
            this.metaFieldsGrid.EnabledChanged += new System.EventHandler(this.MetaFieldsGrid_EnabledChanged);
            // 
            // nameColumn
            // 
            this.nameColumn.DataPropertyName = "Name";
            this.nameColumn.HeaderText = "Name";
            this.nameColumn.Name = "nameColumn";
            this.nameColumn.Width = 150;
            // 
            // mandatoryColumn
            // 
            this.mandatoryColumn.DataPropertyName = "Mandatory";
            this.mandatoryColumn.HeaderText = "Mandatory";
            this.mandatoryColumn.Name = "mandatoryColumn";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.metaList);
            this.splitContainer1.Panel1.Controls.Add(this.removeMetaButton);
            this.splitContainer1.Panel1.Controls.Add(this.addMetaButton);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.removeDescLangButton);
            this.splitContainer1.Panel2.Controls.Add(this.addDescLangButton);
            this.splitContainer1.Panel2.Controls.Add(this.descriptionTextBox);
            this.splitContainer1.Panel2.Controls.Add(this.descriptionKeyCombo);
            this.splitContainer1.Panel2.Controls.Add(this.label3);
            this.splitContainer1.Panel2.Controls.Add(this.decorationLabel);
            this.splitContainer1.Panel2.Controls.Add(this.functionsCheck);
            this.splitContainer1.Panel2.Controls.Add(this.classesCheck);
            this.splitContainer1.Panel2.Controls.Add(this.attributesCheck);
            this.splitContainer1.Panel2.Controls.Add(this.appearsInHelpCheck);
            this.splitContainer1.Panel2.Controls.Add(this.fieldsLabel);
            this.splitContainer1.Panel2.Controls.Add(this.metaFieldsGrid);
            this.splitContainer1.Size = new System.Drawing.Size(483, 272);
            this.splitContainer1.SplitterDistance = 161;
            this.splitContainer1.TabIndex = 4;
            // 
            // metaList
            // 
            this.metaList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.metaList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.metaList.FullRowSelect = true;
            this.metaList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.metaList.LabelEdit = true;
            this.metaList.LabelWrap = false;
            this.metaList.Location = new System.Drawing.Point(0, 29);
            this.metaList.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.metaList.MultiSelect = false;
            this.metaList.Name = "metaList";
            this.metaList.Size = new System.Drawing.Size(161, 243);
            this.metaList.TabIndex = 5;
            this.metaList.UseCompatibleStateImageBehavior = false;
            this.metaList.View = System.Windows.Forms.View.Details;
            this.metaList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.MetaList_AfterLabelEdit);
            this.metaList.SelectedIndexChanged += new System.EventHandler(this.MetaList_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Label";
            this.columnHeader1.Width = 157;
            // 
            // removeMetaButton
            // 
            this.removeMetaButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeMetaButton.Enabled = false;
            this.removeMetaButton.Location = new System.Drawing.Point(132, 0);
            this.removeMetaButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
            this.removeMetaButton.Name = "removeMetaButton";
            this.removeMetaButton.Size = new System.Drawing.Size(29, 23);
            this.removeMetaButton.TabIndex = 4;
            this.removeMetaButton.Text = "-";
            this.removeMetaButton.UseVisualStyleBackColor = true;
            this.removeMetaButton.Click += new System.EventHandler(this.RemoveMetaButton_Click);
            // 
            // addMetaButton
            // 
            this.addMetaButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addMetaButton.Location = new System.Drawing.Point(96, 0);
            this.addMetaButton.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.addMetaButton.Name = "addMetaButton";
            this.addMetaButton.Size = new System.Drawing.Size(30, 23);
            this.addMetaButton.TabIndex = 3;
            this.addMetaButton.Text = "+";
            this.addMetaButton.UseVisualStyleBackColor = true;
            this.addMetaButton.Click += new System.EventHandler(this.AddMetaButton_Click);
            // 
            // removeDescLangButton
            // 
            this.removeDescLangButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeDescLangButton.Enabled = false;
            this.removeDescLangButton.Location = new System.Drawing.Point(288, 20);
            this.removeDescLangButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
            this.removeDescLangButton.Name = "removeDescLangButton";
            this.removeDescLangButton.Size = new System.Drawing.Size(29, 23);
            this.removeDescLangButton.TabIndex = 14;
            this.removeDescLangButton.Text = "-";
            this.removeDescLangButton.UseVisualStyleBackColor = true;
            this.removeDescLangButton.Click += new System.EventHandler(this.RemoveDescLangButton_Click);
            // 
            // addDescLangButton
            // 
            this.addDescLangButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addDescLangButton.Enabled = false;
            this.addDescLangButton.Location = new System.Drawing.Point(252, 20);
            this.addDescLangButton.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.addDescLangButton.Name = "addDescLangButton";
            this.addDescLangButton.Size = new System.Drawing.Size(30, 23);
            this.addDescLangButton.TabIndex = 13;
            this.addDescLangButton.Text = "+";
            this.addDescLangButton.UseVisualStyleBackColor = true;
            this.addDescLangButton.Click += new System.EventHandler(this.AddDescLangButton_Click);
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionTextBox.Enabled = false;
            this.descriptionTextBox.Location = new System.Drawing.Point(0, 49);
            this.descriptionTextBox.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.descriptionTextBox.Size = new System.Drawing.Size(318, 56);
            this.descriptionTextBox.TabIndex = 12;
            this.descriptionTextBox.Leave += new System.EventHandler(this.DescriptionTextBox_Leave);
            // 
            // descriptionKeyCombo
            // 
            this.descriptionKeyCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionKeyCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.descriptionKeyCombo.Enabled = false;
            this.descriptionKeyCombo.FormattingEnabled = true;
            this.descriptionKeyCombo.Location = new System.Drawing.Point(146, 22);
            this.descriptionKeyCombo.Name = "descriptionKeyCombo";
            this.descriptionKeyCombo.Size = new System.Drawing.Size(100, 21);
            this.descriptionKeyCombo.TabIndex = 11;
            this.descriptionKeyCombo.SelectedIndexChanged += new System.EventHandler(this.DescriptionKeyCombo_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-3, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Help Description:";
            // 
            // decorationLabel
            // 
            this.decorationLabel.AutoSize = true;
            this.decorationLabel.Location = new System.Drawing.Point(-1, 3);
            this.decorationLabel.Name = "decorationLabel";
            this.decorationLabel.Size = new System.Drawing.Size(58, 13);
            this.decorationLabel.TabIndex = 9;
            this.decorationLabel.Text = "Adornates:";
            // 
            // functionsCheck
            // 
            this.functionsCheck.AutoSize = true;
            this.functionsCheck.Enabled = false;
            this.functionsCheck.Location = new System.Drawing.Point(208, 3);
            this.functionsCheck.Name = "functionsCheck";
            this.functionsCheck.Size = new System.Drawing.Size(72, 17);
            this.functionsCheck.TabIndex = 8;
            this.functionsCheck.Text = "Functions";
            this.functionsCheck.UseVisualStyleBackColor = true;
            this.functionsCheck.Click += new System.EventHandler(this.FieldsCheck_Click);
            // 
            // classesCheck
            // 
            this.classesCheck.AutoSize = true;
            this.classesCheck.Enabled = false;
            this.classesCheck.Location = new System.Drawing.Point(64, 2);
            this.classesCheck.Name = "classesCheck";
            this.classesCheck.Size = new System.Drawing.Size(62, 17);
            this.classesCheck.TabIndex = 7;
            this.classesCheck.Text = "Classes";
            this.classesCheck.UseVisualStyleBackColor = true;
            this.classesCheck.Click += new System.EventHandler(this.FieldsCheck_Click);
            // 
            // attributesCheck
            // 
            this.attributesCheck.AutoSize = true;
            this.attributesCheck.Enabled = false;
            this.attributesCheck.Location = new System.Drawing.Point(132, 2);
            this.attributesCheck.Name = "attributesCheck";
            this.attributesCheck.Size = new System.Drawing.Size(70, 17);
            this.attributesCheck.TabIndex = 6;
            this.attributesCheck.Text = "Attributes";
            this.attributesCheck.UseVisualStyleBackColor = true;
            this.attributesCheck.Click += new System.EventHandler(this.FieldsCheck_Click);
            // 
            // appearsInHelpCheck
            // 
            this.appearsInHelpCheck.AutoSize = true;
            this.appearsInHelpCheck.Enabled = false;
            this.appearsInHelpCheck.Location = new System.Drawing.Point(0, 111);
            this.appearsInHelpCheck.Name = "appearsInHelpCheck";
            this.appearsInHelpCheck.Size = new System.Drawing.Size(167, 17);
            this.appearsInHelpCheck.TabIndex = 5;
            this.appearsInHelpCheck.Text = "Display in adornated field help";
            this.appearsInHelpCheck.UseVisualStyleBackColor = true;
            this.appearsInHelpCheck.Click += new System.EventHandler(this.AppearsInHelpCheck_Click);
            // 
            // fieldsLabel
            // 
            this.fieldsLabel.AutoSize = true;
            this.fieldsLabel.Location = new System.Drawing.Point(-4, 131);
            this.fieldsLabel.Name = "fieldsLabel";
            this.fieldsLabel.Size = new System.Drawing.Size(37, 13);
            this.fieldsLabel.TabIndex = 4;
            this.fieldsLabel.Text = "Fields:";
            // 
            // MetaEditorForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(507, 325);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.splitContainer1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(523, 363);
            this.Name = "MetaEditorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MetaEditor";
            ((System.ComponentModel.ISupportInitialize)(this.metaFieldsGrid)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.DataGridView metaFieldsGrid;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label fieldsLabel;
        private System.Windows.Forms.CheckBox functionsCheck;
        private System.Windows.Forms.CheckBox classesCheck;
        private System.Windows.Forms.CheckBox attributesCheck;
        private System.Windows.Forms.CheckBox appearsInHelpCheck;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label decorationLabel;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.ComboBox descriptionKeyCombo;
        private System.Windows.Forms.Button removeMetaButton;
        private System.Windows.Forms.Button addMetaButton;
        private System.Windows.Forms.Button removeDescLangButton;
        private System.Windows.Forms.Button addDescLangButton;
        private System.Windows.Forms.ListView metaList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn mandatoryColumn;
    }
}