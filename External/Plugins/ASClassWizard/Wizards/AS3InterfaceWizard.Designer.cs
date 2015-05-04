namespace ASClassWizard.Wizards
{
    partial class AS3InterfaceWizard
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
            this.nameBox = new System.Windows.Forms.TextBox();
            this.packageBox = new System.Windows.Forms.TextBox();
            this.classLabel = new System.Windows.Forms.Label();
            this.accessLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.publicRadio = new System.Windows.Forms.RadioButton();
            this.internalRadio = new System.Windows.Forms.RadioButton();
            this.membersLabel = new System.Windows.Forms.Label();
            this.packageLabel = new System.Windows.Forms.Label();
            this.packageBrowse = new System.Windows.Forms.Button();
            this.flowLayoutPanel9 = new System.Windows.Forms.FlowLayoutPanel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.memberButton = new System.Windows.Forms.Button();
            this.implementLabel = new System.Windows.Forms.Label();
            this.implementList = new System.Windows.Forms.ListBox();
            this.memberList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.newMemberGroup = new System.Windows.Forms.GroupBox();
            this.createNewMemberButton = new System.Windows.Forms.Button();
            this.memberNameBox = new System.Windows.Forms.TextBox();
            this.memberNameLabel = new System.Windows.Forms.Label();
            this.memberTypeLabel = new System.Windows.Forms.Label();
            this.memberTypeCombo = new System.Windows.Forms.ComboBox();
            this.functionPanel = new System.Windows.Forms.Panel();
            this.argValueBox = new ASClassWizard.Wizards.PromptTextBox();
            this.argNameBox = new ASClassWizard.Wizards.PromptTextBox();
            this.returnTypeBox = new ASClassWizard.Wizards.PromptTextBox();
            this.functionReturnLabel = new System.Windows.Forms.Label();
            this.argRemoveButton = new System.Windows.Forms.Button();
            this.argAddButton = new System.Windows.Forms.Button();
            this.argsList = new System.Windows.Forms.ListBox();
            this.argTypeBox = new ASClassWizard.Wizards.PromptTextBox();
            this.functionArgsLabel = new System.Windows.Forms.Label();
            this.propertyPanel = new System.Windows.Forms.Panel();
            this.propertyAccessorsLabel = new System.Windows.Forms.Label();
            this.propertyAccessorsCombo = new System.Windows.Forms.ComboBox();
            this.propertyTypeBox = new ASClassWizard.Wizards.PromptTextBox();
            this.propertyTypeLabel = new System.Windows.Forms.Label();
            this.errorIcon = new System.Windows.Forms.PictureBox();
            this.errorLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel9.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.newMemberGroup.SuspendLayout();
            this.functionPanel.SuspendLayout();
            this.propertyPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.implementBrowse);
            this.flowLayoutPanel4.Controls.Add(this.implementRemove);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(362, 192);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(98, 71);
            this.flowLayoutPanel4.TabIndex = 12;
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
            // nameBox
            // 
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(101, 62);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(255, 20);
            this.nameBox.TabIndex = 6;
            this.nameBox.Text = "INewInterface";
            this.nameBox.TextChanged += new System.EventHandler(this.NameBox_TextChanged);
            // 
            // packageBox
            // 
            this.packageBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.packageBox.Location = new System.Drawing.Point(101, 5);
            this.packageBox.Name = "packageBox";
            this.packageBox.Size = new System.Drawing.Size(255, 20);
            this.packageBox.TabIndex = 1;
            this.packageBox.Click += new System.EventHandler(this.PackageBox_TextChanged);
            // 
            // classLabel
            // 
            this.classLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.classLabel.AutoSize = true;
            this.classLabel.Location = new System.Drawing.Point(3, 65);
            this.classLabel.Name = "classLabel";
            this.classLabel.Size = new System.Drawing.Size(35, 13);
            this.classLabel.TabIndex = 5;
            this.classLabel.Text = "Name";
            // 
            // accessLabel
            // 
            this.accessLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.accessLabel.AutoSize = true;
            this.accessLabel.Location = new System.Drawing.Point(3, 37);
            this.accessLabel.Name = "accessLabel";
            this.accessLabel.Size = new System.Drawing.Size(42, 13);
            this.accessLabel.TabIndex = 3;
            this.accessLabel.Text = "Access";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.publicRadio);
            this.flowLayoutPanel2.Controls.Add(this.internalRadio);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(101, 33);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(255, 22);
            this.flowLayoutPanel2.TabIndex = 4;
            // 
            // publicRadio
            // 
            this.publicRadio.AutoSize = true;
            this.publicRadio.Checked = true;
            this.publicRadio.Location = new System.Drawing.Point(3, 3);
            this.publicRadio.Name = "publicRadio";
            this.publicRadio.Size = new System.Drawing.Size(53, 17);
            this.publicRadio.TabIndex = 0;
            this.publicRadio.TabStop = true;
            this.publicRadio.Text = "public";
            this.publicRadio.UseVisualStyleBackColor = true;
            // 
            // internalRadio
            // 
            this.internalRadio.AutoSize = true;
            this.internalRadio.Location = new System.Drawing.Point(62, 3);
            this.internalRadio.Name = "internalRadio";
            this.internalRadio.Size = new System.Drawing.Size(59, 17);
            this.internalRadio.TabIndex = 1;
            this.internalRadio.Text = "internal";
            this.internalRadio.UseVisualStyleBackColor = true;
            // 
            // membersLabel
            // 
            this.membersLabel.AutoSize = true;
            this.membersLabel.Location = new System.Drawing.Point(3, 86);
            this.membersLabel.Name = "membersLabel";
            this.membersLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.membersLabel.Size = new System.Drawing.Size(50, 19);
            this.membersLabel.TabIndex = 7;
            this.membersLabel.Text = "Members";
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
            this.packageBrowse.Location = new System.Drawing.Point(372, 3);
            this.packageBrowse.Name = "packageBrowse";
            this.packageBrowse.Size = new System.Drawing.Size(74, 23);
            this.packageBrowse.TabIndex = 2;
            this.packageBrowse.Text = "Browse...";
            this.packageBrowse.UseVisualStyleBackColor = true;
            this.packageBrowse.Click += new System.EventHandler(this.PackageBrowse_Click);
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
            this.flowLayoutPanel9.Size = new System.Drawing.Size(659, 35);
            this.flowLayoutPanel9.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(8, 5);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(169, 13);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "New Actionscript 3 Interface";
            // 
            // groupBox2
            // 
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
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.Controls.Add(this.memberButton, 2, 3);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel4, 2, 4);
            this.tableLayoutPanel2.Controls.Add(this.packageBrowse, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.packageLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.implementLabel, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.packageBox, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.membersLabel, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.accessLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.classLabel, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel2, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.nameBox, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.implementList, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.memberList, 1, 3);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 12);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(460, 263);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // memberButton
            // 
            this.memberButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.memberButton.Location = new System.Drawing.Point(372, 89);
            this.memberButton.Name = "memberButton";
            this.memberButton.Size = new System.Drawing.Size(74, 23);
            this.memberButton.TabIndex = 9;
            this.memberButton.Text = "<< A&dd";
            this.memberButton.UseVisualStyleBackColor = true;
            this.memberButton.Click += new System.EventHandler(this.MemberButton_Click);
            // 
            // implementLabel
            // 
            this.implementLabel.AutoSize = true;
            this.implementLabel.Location = new System.Drawing.Point(3, 192);
            this.implementLabel.Name = "implementLabel";
            this.implementLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.implementLabel.Size = new System.Drawing.Size(55, 19);
            this.implementLabel.TabIndex = 10;
            this.implementLabel.Text = "Implement";
            // 
            // implementList
            // 
            this.implementList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.implementList.FormattingEnabled = true;
            this.implementList.IntegralHeight = false;
            this.implementList.Location = new System.Drawing.Point(101, 195);
            this.implementList.Name = "implementList";
            this.implementList.Size = new System.Drawing.Size(255, 65);
            this.implementList.TabIndex = 11;
            this.implementList.SelectedIndexChanged += new System.EventHandler(this.ImplementList_SelectedIndexChanged);
            // 
            // memberList
            // 
            this.memberList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.memberList.CheckBoxes = true;
            this.memberList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.memberList.FullRowSelect = true;
            this.memberList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.memberList.Location = new System.Drawing.Point(101, 89);
            this.memberList.Name = "memberList";
            this.memberList.Size = new System.Drawing.Size(255, 100);
            this.memberList.TabIndex = 8;
            this.memberList.UseCompatibleStateImageBehavior = false;
            this.memberList.View = System.Windows.Forms.View.Details;
            this.memberList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.MemberList_ItemChecked);
            // 
            // newMemberGroup
            // 
            this.newMemberGroup.Controls.Add(this.createNewMemberButton);
            this.newMemberGroup.Controls.Add(this.memberNameBox);
            this.newMemberGroup.Controls.Add(this.memberNameLabel);
            this.newMemberGroup.Controls.Add(this.memberTypeLabel);
            this.newMemberGroup.Controls.Add(this.memberTypeCombo);
            this.newMemberGroup.Controls.Add(this.functionPanel);
            this.newMemberGroup.Controls.Add(this.propertyPanel);
            this.newMemberGroup.Location = new System.Drawing.Point(484, 53);
            this.newMemberGroup.Name = "newMemberGroup";
            this.newMemberGroup.Size = new System.Drawing.Size(187, 279);
            this.newMemberGroup.TabIndex = 2;
            this.newMemberGroup.TabStop = false;
            // 
            // createNewMemberButton
            // 
            this.createNewMemberButton.Location = new System.Drawing.Point(99, 252);
            this.createNewMemberButton.Name = "createNewMemberButton";
            this.createNewMemberButton.Size = new System.Drawing.Size(75, 23);
            this.createNewMemberButton.TabIndex = 6;
            this.createNewMemberButton.Text = "Crea&te";
            this.createNewMemberButton.UseVisualStyleBackColor = true;
            this.createNewMemberButton.Click += new System.EventHandler(this.CreateNewMemberButton_Click);
            // 
            // memberNameBox
            // 
            this.memberNameBox.Location = new System.Drawing.Point(74, 44);
            this.memberNameBox.Name = "memberNameBox";
            this.memberNameBox.Size = new System.Drawing.Size(100, 20);
            this.memberNameBox.TabIndex = 3;
            // 
            // memberNameLabel
            // 
            this.memberNameLabel.AutoSize = true;
            this.memberNameLabel.Location = new System.Drawing.Point(9, 47);
            this.memberNameLabel.Name = "memberNameLabel";
            this.memberNameLabel.Size = new System.Drawing.Size(35, 13);
            this.memberNameLabel.TabIndex = 2;
            this.memberNameLabel.Text = "Name";
            // 
            // memberTypeLabel
            // 
            this.memberTypeLabel.AutoSize = true;
            this.memberTypeLabel.Location = new System.Drawing.Point(6, 20);
            this.memberTypeLabel.Name = "memberTypeLabel";
            this.memberTypeLabel.Size = new System.Drawing.Size(45, 13);
            this.memberTypeLabel.TabIndex = 0;
            this.memberTypeLabel.Text = "Member";
            // 
            // memberTypeCombo
            // 
            this.memberTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.memberTypeCombo.FormattingEnabled = true;
            this.memberTypeCombo.Location = new System.Drawing.Point(74, 17);
            this.memberTypeCombo.Name = "memberTypeCombo";
            this.memberTypeCombo.Size = new System.Drawing.Size(100, 21);
            this.memberTypeCombo.TabIndex = 1;
            this.memberTypeCombo.SelectedIndexChanged += new System.EventHandler(this.MemberTypeCombo_SelectedIndexChanged);
            // 
            // functionPanel
            // 
            this.functionPanel.Controls.Add(this.argValueBox);
            this.functionPanel.Controls.Add(this.argNameBox);
            this.functionPanel.Controls.Add(this.returnTypeBox);
            this.functionPanel.Controls.Add(this.functionReturnLabel);
            this.functionPanel.Controls.Add(this.argRemoveButton);
            this.functionPanel.Controls.Add(this.argAddButton);
            this.functionPanel.Controls.Add(this.argsList);
            this.functionPanel.Controls.Add(this.argTypeBox);
            this.functionPanel.Controls.Add(this.functionArgsLabel);
            this.functionPanel.Location = new System.Drawing.Point(6, 70);
            this.functionPanel.Name = "functionPanel";
            this.functionPanel.Size = new System.Drawing.Size(175, 182);
            this.functionPanel.TabIndex = 4;
            // 
            // argValueBox
            // 
            this.argValueBox.Location = new System.Drawing.Point(119, 3);
            this.argValueBox.Name = "argValueBox";
            this.argValueBox.Prompt = "Value";
            this.argValueBox.Size = new System.Drawing.Size(49, 20);
            this.argValueBox.TabIndex = 2;
            // 
            // argNameBox
            // 
            this.argNameBox.Location = new System.Drawing.Point(68, 3);
            this.argNameBox.Name = "argNameBox";
            this.argNameBox.Prompt = "Name";
            this.argNameBox.Size = new System.Drawing.Size(49, 20);
            this.argNameBox.TabIndex = 1;
            // 
            // returnTypeBox
            // 
            this.returnTypeBox.Location = new System.Drawing.Point(68, 159);
            this.returnTypeBox.Name = "returnTypeBox";
            this.returnTypeBox.Prompt = "Type";
            this.returnTypeBox.Size = new System.Drawing.Size(100, 20);
            this.returnTypeBox.TabIndex = 8;
            // 
            // functionReturnLabel
            // 
            this.functionReturnLabel.AutoSize = true;
            this.functionReturnLabel.Location = new System.Drawing.Point(3, 162);
            this.functionReturnLabel.Name = "functionReturnLabel";
            this.functionReturnLabel.Size = new System.Drawing.Size(39, 13);
            this.functionReturnLabel.TabIndex = 7;
            this.functionReturnLabel.Text = "Return";
            // 
            // argRemoveButton
            // 
            this.argRemoveButton.Enabled = false;
            this.argRemoveButton.ForeColor = System.Drawing.Color.DarkRed;
            this.argRemoveButton.Location = new System.Drawing.Point(127, 55);
            this.argRemoveButton.Name = "argRemoveButton";
            this.argRemoveButton.Size = new System.Drawing.Size(23, 23);
            this.argRemoveButton.TabIndex = 5;
            this.argRemoveButton.Text = "✖";
            this.argRemoveButton.UseVisualStyleBackColor = true;
            this.argRemoveButton.Click += new System.EventHandler(this.ArgRemoveButton_Click);
            // 
            // argAddButton
            // 
            this.argAddButton.Location = new System.Drawing.Point(94, 55);
            this.argAddButton.Name = "argAddButton";
            this.argAddButton.Size = new System.Drawing.Size(23, 23);
            this.argAddButton.TabIndex = 4;
            this.argAddButton.Text = "▼";
            this.argAddButton.UseVisualStyleBackColor = true;
            this.argAddButton.Click += new System.EventHandler(this.ArgAddButton_Click);
            // 
            // argsList
            // 
            this.argsList.FormattingEnabled = true;
            this.argsList.Location = new System.Drawing.Point(68, 84);
            this.argsList.Name = "argsList";
            this.argsList.Size = new System.Drawing.Size(100, 69);
            this.argsList.TabIndex = 6;
            this.argsList.SelectedIndexChanged += new System.EventHandler(this.ArgsList_SelectedIndexChanged);
            // 
            // argTypeBox
            // 
            this.argTypeBox.ForeColor = System.Drawing.SystemColors.WindowText;
            this.argTypeBox.Location = new System.Drawing.Point(68, 29);
            this.argTypeBox.Name = "argTypeBox";
            this.argTypeBox.Prompt = "Type";
            this.argTypeBox.Size = new System.Drawing.Size(100, 20);
            this.argTypeBox.TabIndex = 3;
            // 
            // functionArgsLabel
            // 
            this.functionArgsLabel.AutoSize = true;
            this.functionArgsLabel.Location = new System.Drawing.Point(3, 6);
            this.functionArgsLabel.Name = "functionArgsLabel";
            this.functionArgsLabel.Size = new System.Drawing.Size(57, 13);
            this.functionArgsLabel.TabIndex = 0;
            this.functionArgsLabel.Text = "Arguments";
            // 
            // propertyPanel
            // 
            this.propertyPanel.Controls.Add(this.propertyAccessorsLabel);
            this.propertyPanel.Controls.Add(this.propertyAccessorsCombo);
            this.propertyPanel.Controls.Add(this.propertyTypeBox);
            this.propertyPanel.Controls.Add(this.propertyTypeLabel);
            this.propertyPanel.Location = new System.Drawing.Point(6, 70);
            this.propertyPanel.Name = "propertyPanel";
            this.propertyPanel.Size = new System.Drawing.Size(175, 179);
            this.propertyPanel.TabIndex = 5;
            // 
            // propertyAccessorsLabel
            // 
            this.propertyAccessorsLabel.AutoSize = true;
            this.propertyAccessorsLabel.Location = new System.Drawing.Point(3, 7);
            this.propertyAccessorsLabel.Name = "propertyAccessorsLabel";
            this.propertyAccessorsLabel.Size = new System.Drawing.Size(56, 13);
            this.propertyAccessorsLabel.TabIndex = 0;
            this.propertyAccessorsLabel.Text = "Accessors";
            // 
            // propertyAccessorsCombo
            // 
            this.propertyAccessorsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.propertyAccessorsCombo.FormattingEnabled = true;
            this.propertyAccessorsCombo.Location = new System.Drawing.Point(68, 4);
            this.propertyAccessorsCombo.Name = "propertyAccessorsCombo";
            this.propertyAccessorsCombo.Size = new System.Drawing.Size(100, 21);
            this.propertyAccessorsCombo.TabIndex = 1;
            // 
            // propertyTypeBox
            // 
            this.propertyTypeBox.Location = new System.Drawing.Point(68, 31);
            this.propertyTypeBox.Name = "propertyTypeBox";
            this.propertyTypeBox.Size = new System.Drawing.Size(100, 20);
            this.propertyTypeBox.TabIndex = 3;
            // 
            // propertyTypeLabel
            // 
            this.propertyTypeLabel.AutoSize = true;
            this.propertyTypeLabel.Location = new System.Drawing.Point(3, 34);
            this.propertyTypeLabel.Name = "propertyTypeLabel";
            this.propertyTypeLabel.Size = new System.Drawing.Size(31, 13);
            this.propertyTypeLabel.TabIndex = 2;
            this.propertyTypeLabel.Text = "Type";
            // 
            // errorIcon
            // 
            this.errorIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.errorIcon.Location = new System.Drawing.Point(12, 348);
            this.errorIcon.Name = "errorIcon";
            this.errorIcon.Size = new System.Drawing.Size(16, 16);
            this.errorIcon.TabIndex = 5;
            this.errorIcon.TabStop = false;
            // 
            // errorLabel
            // 
            this.errorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.errorLabel.AutoEllipsis = true;
            this.errorLabel.AutoSize = true;
            this.errorLabel.ForeColor = System.Drawing.Color.Black;
            this.errorLabel.Location = new System.Drawing.Point(34, 349);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(29, 13);
            this.errorLabel.TabIndex = 4;
            this.errorLabel.Text = "Error";
            this.errorLabel.Resize += ErrorLabel_Resize;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(596, 344);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 7;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(515, 344);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 6;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // AS3InterfaceWizard
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(683, 379);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.errorIcon);
            this.Controls.Add(this.errorLabel);
            this.Controls.Add(this.newMemberGroup);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.flowLayoutPanel9);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AS3InterfaceWizard";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New ActionScript Interface";
            this.Load += new System.EventHandler(this.AS3InterfaceWizard_Load);
            this.Resize += new System.EventHandler(this.AS3InterfaceWizard_Resize);
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel9.ResumeLayout(false);
            this.flowLayoutPanel9.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.newMemberGroup.ResumeLayout(false);
            this.newMemberGroup.PerformLayout();
            this.functionPanel.ResumeLayout(false);
            this.functionPanel.PerformLayout();
            this.propertyPanel.ResumeLayout(false);
            this.propertyPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label packageLabel;
        private System.Windows.Forms.TextBox packageBox;
        private System.Windows.Forms.Button packageBrowse;
        private System.Windows.Forms.Label classLabel;
        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.Label accessLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.RadioButton publicRadio;
        private System.Windows.Forms.RadioButton internalRadio;
        private System.Windows.Forms.Label membersLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Button implementBrowse;
        private System.Windows.Forms.Button implementRemove;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel9;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label implementLabel;
        private System.Windows.Forms.ListBox implementList;
        private System.Windows.Forms.ListView memberList;
        private System.Windows.Forms.Button memberButton;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.GroupBox newMemberGroup;
        private System.Windows.Forms.Label memberTypeLabel;
        private System.Windows.Forms.ComboBox memberTypeCombo;
        private System.Windows.Forms.Panel functionPanel;
        private System.Windows.Forms.ListBox argsList;
        private PromptTextBox argTypeBox;
        private System.Windows.Forms.Label functionArgsLabel;
        private System.Windows.Forms.Button argRemoveButton;
        private System.Windows.Forms.Button argAddButton;
        private PromptTextBox returnTypeBox;
        private System.Windows.Forms.Label functionReturnLabel;
        private System.Windows.Forms.TextBox memberNameBox;
        private System.Windows.Forms.Label memberNameLabel;
        private System.Windows.Forms.Panel propertyPanel;
        private PromptTextBox propertyTypeBox;
        private System.Windows.Forms.Label propertyTypeLabel;
        private PromptTextBox argNameBox;
        private System.Windows.Forms.Button createNewMemberButton;
        private System.Windows.Forms.Label propertyAccessorsLabel;
        private System.Windows.Forms.ComboBox propertyAccessorsCombo;
        private System.Windows.Forms.PictureBox errorIcon;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private PromptTextBox argValueBox;
    }
}