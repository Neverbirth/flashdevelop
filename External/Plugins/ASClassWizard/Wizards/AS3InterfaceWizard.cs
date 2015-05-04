/* NOTE: FD doesn't correctly handle that interfaces may extend multiple interfaces, so conflicts resolution is no perfect. IMHO, extended interfaces should appear 
 * inside the model Implements field.
 */

/* POSSIBLE IMPROVEMENTS
 *  - A button to extract public members from other classes, it would clean all current selected data.
 *  - A check to extract methods and/or interfaces from superclasses.
 *  - Move up and down members and/or different types of sorting methods.
 *  - Better type autocompletion. Use AirProperties/Controls/CheckedComboBox as a base
 */

//TODO: Type autocompletion
//TODO: Better error display

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ASCompletion.Context;
using ASCompletion.Model;
using PluginCore;
using PluginCore.Localization;
using PluginCore.Managers;
using ProjectManager.Projects;

namespace ASClassWizard.Wizards
{
    public partial class AS3InterfaceWizard : Form
    {

        private Dictionary<string, MemberOwnerPair> _interfaceMethodsMapping;
        private Dictionary<string, List<MemberOwnerPair>> _memberConflicts;

        private bool _addingMethod;

        public String Package
        {
            get { return packageBox.Text.Trim(); }
            set { packageBox.Text = value; }
        }

        public string Directory { get; set; }

        public string InterfaceName
        {
            get { return nameBox.Text.Trim(); }
            set { nameBox.Text = value; }
        }

        private Project project;
        public Project Project
        {
            get { return project; }
            set
            {
                project = value;
                internalRadio.Text = "internal";
                if (project.Language == "as3")
                {
                    this.titleLabel.Text = TextHelper.GetString("Wizard.Label.NewAs3Interface");
                    this.Text = TextHelper.GetString("Wizard.Label.NewAs3Interface");
                }
            }
        }

        private ClassModel _classModel;
        public ClassModel ClassModel
        {
            get
            {
                return _classModel;
            }
            set
            {
                memberList.Items.Clear();
                implementList.Items.Clear();
                _interfaceMethodsMapping.Clear();
                _memberConflicts.Clear();

                if (value != null)
                {
                    _addingMethod = true;
                    FlagType flags = FlagType.Function | FlagType.Getter | FlagType.Setter;
                    FlagType exclude = FlagType.Constructor | FlagType.Static;
                    foreach (var m in value.Members.Items)
                    {
                        if (m.Access == Visibility.Public && (m.Flags & flags) > 0 && (m.Flags & exclude) == 0)
                        {
                            var mc = (MemberModel)m.Clone();
                            if (mc.Parameters != null)
                            {
                                foreach (var p in mc.Parameters)
                                {
                                    if (p.Type != null && p.Type != "*")
                                        p.Type = ASContext.Context.ResolveType(p.Type, value.InFile).QualifiedName;
                                }
                            }

                            if (mc.Type != null) mc.Type = ASContext.Context.ResolveType(mc.Type, value.InFile).QualifiedName;

                            var item = memberList.Items.Add(MemberToItemString(mc));
                            item.Tag = mc;
                            item.Checked = true;
                        }
                    }

                    if (value.Implements != null)
                        foreach (var i in value.Implements)
                        {
                            var iModel = ASContext.Context.ResolveType(i, value.InFile);
                            implementList.Items.Add(new InterfaceListItem {Model = iModel});

                            iModel.ResolveExtends();
                            CheckInheritedMethods(iModel);
                        }

                    if (implementList.Items.Count > 0)
                        implementList.SelectedIndex = implementList.Items.Count - 1;

                    CheckForCollisioningMethods();
                    memberList.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    memberList.Columns[0].Width += SystemInformation.VerticalScrollBarWidth;

                    _addingMethod = false;
                }
            }
        }

        public bool IsPublic
        {
            get { return publicRadio.Checked; }
        }

        public AS3InterfaceWizard()
        {
            InitializeComponent();
            LocalizeText();
            InitializeControls();

            _classModel = new ClassModel {InFile = new FileModel(string.Empty)};
            _classModel.Flags = FlagType.Interface | FlagType.Class;
            _interfaceMethodsMapping = new Dictionary<string, MemberOwnerPair>();
            _memberConflicts = new Dictionary<string, List<MemberOwnerPair>>();

            this.errorIcon.Image = PluginMain.MainForm.FindImage("197");
        }

        #region EventHandlers

        private void MemberList_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_addingMethod) return;

            MemberModel m = (MemberModel) e.Item.Tag;
            List<MemberOwnerPair> conflicting;

            if (e.Item.Checked)
            {
                MemberOwnerPair inherited;
                if (_interfaceMethodsMapping.TryGetValue(m.Name, out inherited))
                {
                    if (CompareMembers(inherited.Owner, inherited.Member, inherited.Properties, null, m) ==
                        MemberComparisonResult.DifferentSignature)
                    {
                        if (!_memberConflicts.TryGetValue(m.Name, out conflicting))
                        {
                            conflicting = new List<MemberOwnerPair>();
                            _memberConflicts[m.Name] = conflicting;
                        }
                        if (conflicting.Find(c => c.Owner.QualifiedName == inherited.Owner.QualifiedName) == null)
                            conflicting.Add(inherited);
                        if (conflicting.Find(c => c.Owner == null) == null)
                            conflicting.Add(new MemberOwnerPair { Owner = null, Member = m });
                    }
                }
            }
            else  if (_memberConflicts.TryGetValue(m.Name, out conflicting))
            {
                int index = conflicting.FindIndex(p => p.Owner == null);
                if (index > -1) conflicting.RemoveAt(index);
                if (conflicting.Count == 1)
                    _memberConflicts.Remove(m.Name);
            }

            ValidateInterface();
        }

        private void ImplementList_SelectedIndexChanged(object sender, EventArgs e)
        {
            implementRemove.Enabled = implementList.SelectedItems.Count > 0;
        }

        /// <summary>
        /// Browse project packages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackageBrowse_Click(object sender, EventArgs e)
        {

            PackageBrowser browser = new PackageBrowser();
            browser.Project = this.Project;

            foreach (string item in Project.AbsoluteClasspaths)
                browser.AddClassPath(item);

            if (browser.ShowDialog(this) == DialogResult.OK)
            {
                if (browser.Package != null)
                {
                    string classpath = this.Project.AbsoluteClasspaths.GetClosestParent(browser.Package);
                    string package = Path.GetDirectoryName(ProjectPaths.GetRelativePath(classpath, Path.Combine(browser.Package, "foo")));
                    if (package != null)
                    {
                        Directory = browser.Package;
                        package = package.Replace(Path.DirectorySeparatorChar, '.');
                        this.packageBox.Text = package;
                    }
                }
                else
                {
                    Directory = browser.Project.Directory;
                    this.packageBox.Text = string.Empty;
                }
            }
        }

        private void AS3InterfaceWizard_Load(object sender, EventArgs e)
        {
            Font = PluginBase.Settings.DefaultFont;
            errorLabel.MaximumSize = new Size(okButton.Left - errorLabel.Left - errorLabel.Margin.Right - okButton.Margin.Left,
                TextRenderer.MeasureText("A" + Environment.NewLine + "A", errorLabel.Font).Height);
            memberButton.PerformClick();
            nameBox.Select();
            ValidateInterface();
        }

        private void AS3InterfaceWizard_Resize(object sender, EventArgs e)
        {
            errorLabel.MaximumSize = new Size(okButton.Left - errorLabel.Left - errorLabel.Margin.Right - okButton.Margin.Left,
                errorLabel.MaximumSize.Height);
        }

        private void ErrorLabel_Resize(object sender, EventArgs e)
        {
            errorLabel.Top = (int)(okButton.Top + okButton.Height * .5 - errorLabel.Height * .5);
        }

        /// <summary>
        /// Added interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImplementBrowse_Click(object sender, EventArgs e)
        {
            ClassBrowser browser = new ClassBrowser();
            MemberList known = null;
            browser.IncludeFlag = FlagType.Interface;
            IASContext context = ASContext.GetLanguageContext(PluginBase.CurrentProject.Language);
            try
            {
                known = context.GetAllProjectClasses();
            }
            catch (Exception error)
            {
                Debug.WriteLine(error.StackTrace);
            }
            browser.ClassList = known;
            if (browser.ShowDialog(this) == DialogResult.OK)
            {
                if (browser.SelectedClass != null)
                {
                    ClassModel iModel = browser.SelectedClassModel as ClassModel;
                    if (iModel == null && (iModel = context.ResolveType(browser.SelectedClassModel.Type, null)) == null) 
                        return;

                    foreach (InterfaceListItem item in this.implementList.Items)
                    {
                        if (item.Model == iModel) return;
                    }

                    iModel.ResolveExtends();
                    CheckInheritedMethods(iModel);

                    this.implementList.Items.Add(new InterfaceListItem {Model = iModel});
                    CheckForCollisioningMethods();
                }
            }
            this.implementList.SelectedIndex = this.implementList.Items.Count - 1;
            ValidateInterface();
        }

        /// <summary>
        /// Remove interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterfaceRemove_Click(object sender, EventArgs e)
        {
            var model = ((InterfaceListItem) implementList.SelectedItem).Model;
            this.implementList.Items.Remove(this.implementList.SelectedItem);
            if (this.implementList.Items.Count > 0)
            {
                this.implementList.SelectedIndex = this.implementList.Items.Count - 1;
            }

            RemoveCachedInheritedMethods(model);
            _memberConflicts.Clear();
            CheckForCollisioningMethods();

            ValidateInterface();
        }

        private void PackageBox_TextChanged(object sender, EventArgs e)
        {
            ValidateInterface();
        }

        private void NameBox_TextChanged(object sender, EventArgs e)
        {
            ValidateInterface();
        }

        private void MemberButton_Click(object sender, EventArgs e)
        {
            if (newMemberGroup.Visible)
            {
                newMemberGroup.Visible = false;
                Width = groupBox2.Width + groupBox2.Left * 3;
                memberButton.Text = TextHelper.GetString("Wizard.Button.AddMember") + " >>";
            }
            else
            {
                newMemberGroup.Visible = true;
                Width = newMemberGroup.Left + newMemberGroup.Width + groupBox2.Left * 2;
                memberButton.Text = "<< " + TextHelper.GetString("Wizard.Button.AddMember");
            }

        }

        private void ArgRemoveButton_Click(object sender, EventArgs e)
        {
            int index = argsList.SelectedIndex;
            argsList.Items.RemoveAt(index);
            if (argsList.Items.Count > 0) argsList.SelectedIndex = index - 1;
        }

        private void ArgAddButton_Click(object sender, EventArgs e)
        {
            var name = argNameBox.Text.Trim();
            if (name == string.Empty) name = "arg" + argsList.Items.Count;

            foreach (ArgumentListItem item in argsList.Items)
            {
                if (item.Name == name)
                {
                    ErrorManager.ShowInfo(TextHelper.GetString("Wizard.Error.ExistingArg"));
                    return;
                }
            }

            var type = argTypeBox.Text.Trim();
            if (type == string.Empty) type = ASContext.Context.Features.objectKey;
            var value = argValueBox.Text.Trim();
            argsList.Items.Add(new ArgumentListItem { Name = name, Type = type, Value = (value == string.Empty ? null : value) });
            argsList.SelectedIndex = argsList.Items.Count - 1;
            argNameBox.Clear();
            argTypeBox.Clear();
            argValueBox.Clear();
        }

        private void ArgsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            argRemoveButton.Enabled = argsList.SelectedItems.Count > 0;
        }

        private void CreateNewMemberButton_Click(object sender, EventArgs e)
        {
            string name = memberNameBox.Text.Trim();
            string type;

            if (string.IsNullOrEmpty(name))
            {
                ErrorManager.ShowInfo(TextHelper.GetString("Wizard.Error.EmptyName"));
                return;
            }

            var newMembers = new List<MemberModel>();
            var newMember = new MemberModel();
            newMember.Name = name;
            switch (memberTypeCombo.SelectedIndex)
            {
                case 0:
                    type = propertyTypeBox.Text.Trim();
                    if (type == string.Empty) type = ASContext.Context.Features.objectKey;
                    newMembers.Add(newMember);

                    if (propertyAccessorsCombo.SelectedIndex == 0)
                    {
                        var newMemberCopy = (MemberModel)newMember.Clone();
                        newMemberCopy.Parameters = new List<MemberModel> {new MemberModel("value", type, 0, 0)};
                        newMemberCopy.Flags = FlagType.Setter;
                        newMember.Type = type;
                        newMember.Flags = FlagType.Getter;
                        newMembers.Add(newMemberCopy);
                    } 
                    else if (propertyAccessorsCombo.SelectedIndex == 1) 
                    {
                        newMember.Type = type;
                        newMember.Flags = FlagType.Getter;
                    }
                    else if (propertyAccessorsCombo.SelectedIndex == 2) 
                    {
                        newMember.Parameters = new List<MemberModel> { new MemberModel("value", type, 0, 0) };
                        newMember.Flags = FlagType.Setter;
                    }

                    break;
                case 1:
                    newMember.Flags = FlagType.Function;
                    type = returnTypeBox.Text.Trim();
                    if (type == string.Empty) type = ASContext.Context.Features.voidKey;
                    newMember.Type = type;

                    if (argsList.Items.Count > 0)
                    {
                        newMember.Parameters = new List<MemberModel>();
                        foreach (ArgumentListItem item in argsList.Items)
                        {
                            newMember.Parameters.Add(new MemberModel {Name = item.Name, Type = item.Type, Value = item.Value});
                        }
                    }

                    newMembers.Add(newMember);

                    break;
            }

            foreach (var m in newMembers)
            {
                bool existing = false;
                MemberOwnerPair inheritedMember;
                if (_interfaceMethodsMapping.TryGetValue(m.Name, out inheritedMember))
                {
                    var compare = CompareMembers(inheritedMember.Owner, inheritedMember.Member, inheritedMember.Properties, null, m);
                    if (compare == MemberComparisonResult.DifferentSignature)
                    {
                        ErrorManager.ShowInfo(string.Format(TextHelper.GetString("Wizard.Warning.ConflictingMember"),
                                                            inheritedMember.Owner.QualifiedName));
                        return;
                    } else if (compare == MemberComparisonResult.Identical)
                        existing = true;
                }

                bool addMember = true;
                foreach (ListViewItem item in memberList.Items)
                {
                    if (item.ForeColor == SystemColors.GrayText && !existing) continue;
                    var member = (MemberModel)item.Tag;
                    // We'll care about making a full comparison only if we're a property, to check against missing accessor
                    if (member.Name == m.Name && (memberTypeCombo.SelectedIndex != 0 || 
                        CompareMembers(null, member, member.Flags, null, m) != MemberComparisonResult.DifferentAccessors))
                    {
                        addMember = false;
                        string displayName = string.Empty;
                        if ((m.Flags & FlagType.Getter) > 0)
                            displayName = "get_";
                        else if ((m.Flags & FlagType.Setter) > 0)
                            displayName = "set_";
                        displayName += m.Name;

                        string title = TextHelper.GetString("FlashDevelop.Title.InfoDialog");

                        if (item.ForeColor == SystemColors.GrayText)
                        {
                            MessageBox.Show(PluginBase.MainForm,
                                            string.Format(TextHelper.GetString("Wizard.Warning.ExistingInheritedMember"),
                                                displayName), " " + title, MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);

                            break;
                        }

                        if (
                            MessageBox.Show(PluginBase.MainForm,
                                            string.Format(TextHelper.GetString("Wizard.Warning.ExistingMember"), displayName), " " + title,
                                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                            DialogResult.Yes)
                        {
                            item.Text = MemberToItemString(m);
                            item.Tag = m;
                            item.Selected = true;
                            item.EnsureVisible();
                        }

                        break;
                    }
                }
                if (!addMember) continue;
                _addingMethod = true;
                var newItem = memberList.Items.Add(MemberToItemString(m));
                newItem.Tag = m;
                newItem.Checked = true;
                _addingMethod = false;
                if (existing) newItem.ForeColor = SystemColors.GrayText;
                newItem.Selected = true;
                newItem.EnsureVisible();
            }
            memberList.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            memberList.Columns[0].Width += SystemInformation.VerticalScrollBarWidth;
        }

        private void MemberTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (memberTypeCombo.SelectedIndex)
            {
                case 0:
                    propertyPanel.Visible = true;
                    functionPanel.Visible = false;
                    break;
                case 1:
                    functionPanel.Visible = true;
                    propertyPanel.Visible = false;
                    break;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            _classModel.InFile.Package = Package;
            _classModel.Name = InterfaceName;
            _classModel.Type = !string.IsNullOrEmpty(_classModel.InFile.Package) ? _classModel.InFile.Package + "." + _classModel.Name : _classModel.Name;
            if (IsPublic) _classModel.Access = Visibility.Public;

            _classModel.Members.Clear();
            foreach (ListViewItem item in memberList.CheckedItems)
            {
                if (item.ForeColor == SystemColors.GrayText) continue;
                _classModel.Members.Add((MemberModel)item.Tag);
            }
            List<string> interfaces = new List<string>(this.implementList.Items.Count);
            foreach (InterfaceListItem item in this.implementList.Items)
            {
                interfaces.Add(item.Model.QualifiedName);
            }
            _classModel.Implements = interfaces;
        }

        #endregion

        private void LocalizeText()
        {
            this.classLabel.Text = TextHelper.GetString("Wizard.Label.Name");
            this.accessLabel.Text = TextHelper.GetString("Wizard.Label.Modifiers");
            this.implementLabel.Text = TextHelper.GetString("Wizard.Label.Interfaces");
            this.implementBrowse.Text = TextHelper.GetString("Wizard.Button.Add");
            this.implementRemove.Text = TextHelper.GetString("Wizard.Button.Remove");
            this.packageLabel.Text = TextHelper.GetString("Wizard.Label.Package");
            this.packageBrowse.Text = TextHelper.GetString("Wizard.Button.Browse");
            this.okButton.Text = TextHelper.GetString("Wizard.Button.Ok");
            this.cancelButton.Text = TextHelper.GetString("Wizard.Button.Cancel");
            this.membersLabel.Text = TextHelper.GetString("Wizard.Label.Members");
            this.memberTypeLabel.Text = TextHelper.GetString("Wizard.Label.Member");
            this.memberNameLabel.Text = this.argNameBox.Prompt = TextHelper.GetString("Wizard.Label.MemberName");
            this.propertyAccessorsLabel.Text = TextHelper.GetString("Wizard.Label.Accessors");
            this.functionArgsLabel.Text = TextHelper.GetString("Wizard.Label.Arguments");
            this.argValueBox.Prompt = TextHelper.GetString("Wizard.Label.Value");
            this.returnTypeBox.Prompt = this.argTypeBox.Prompt = this.propertyTypeLabel.Text =
                TextHelper.GetString("Wizard.Label.Type");
            this.functionReturnLabel.Text = TextHelper.GetString("Wizard.Label.Return");
            this.createNewMemberButton.Text = TextHelper.GetString("Wizard.Button.Create");
        }

        private void InitializeControls()
        {
            memberTypeCombo.Items.Add("Property");
            memberTypeCombo.Items.Add("Function");

            propertyAccessorsCombo.Items.Add("Get/Set");
            propertyAccessorsCombo.Items.Add("Get");
            propertyAccessorsCombo.Items.Add("Set");

            memberTypeCombo.SelectedIndex = 0;
            propertyAccessorsCombo.SelectedIndex = 0;
        }

        private void ValidateInterface()
        {
            string errorMessage = string.Empty;
            if (nameBox.Text == string.Empty)
            {
                errorMessage = TextHelper.GetString("Wizard.Error.EmptyName");
            }
            if (!Regex.Match(nameBox.Text, AS3ClassWizard.REG_IDENTIFIER_AS, RegexOptions.Singleline).Success)
            {
                errorMessage = TextHelper.GetString("Wizard.Error.InvalidInterfaceName");
            }
            if (_memberConflicts.Count > 0)
            {
                var conflicting = new StringBuilder();
                foreach (var c in _memberConflicts)
                {
                    conflicting.Append(c.Key).Append(", ");
                }
                conflicting.Remove(conflicting.Length - 2, 2);
                errorMessage += string.Format(TextHelper.GetString("Wizard.Error.ConflictingMembers"), conflicting);
            }
            if (errorMessage != string.Empty)
            {
                okButton.Enabled = false;
                errorIcon.Visible = true;
            }
            else
            {
                okButton.Enabled = true;
                errorIcon.Visible = false;
            }
            this.errorLabel.Text = errorMessage;
        }

        private void CheckInheritedMethods(ClassModel model)
        {
            if (model.Members != null)
                foreach (var m in model.Members.Items)
                {
                    MemberOwnerPair prevModel;
                    if (_interfaceMethodsMapping.TryGetValue(m.Name, out prevModel))
                    {
                        bool conflicting = CompareMembers(prevModel.Owner, prevModel.Member, prevModel.Properties, model, m) ==
                                           MemberComparisonResult.DifferentSignature;
                        const FlagType property = FlagType.Getter | FlagType.Setter;

                        if (!conflicting && (m.Flags & property) > 0) prevModel.Properties |= m.Flags;
                    }
                    else
                        _interfaceMethodsMapping[m.Name] = new MemberOwnerPair {Owner = model, Member = m};
                }

            if (!model.Extends.IsVoid())
                CheckInheritedMethods(model.Extends);
        }

        private void RemoveCachedInheritedMethods(ClassModel model)
        {
            if (model.Members != null)
                foreach (var m in model.Members.Items)
                {
                    MemberOwnerPair prevModel;
                    if (_interfaceMethodsMapping.TryGetValue(m.Name, out prevModel) && prevModel.Owner == model)
                        _interfaceMethodsMapping.Remove(m.Name);
                }

            if (!model.Extends.IsVoid())
                RemoveCachedInheritedMethods(model.Extends);
        }

        private void CheckForCollisioningMethods()
        {
            foreach (ListViewItem item in memberList.Items)
            {
                MemberModel itemModel = (MemberModel) item.Tag;
                MemberOwnerPair existing;

                if (_interfaceMethodsMapping.TryGetValue(itemModel.Name, out existing))
                {
                    var compare = CompareMembers(existing.Owner, existing.Member, existing.Properties, null, itemModel);
                    if (compare == MemberComparisonResult.Identical)
                    {
                        item.ForeColor = SystemColors.GrayText;
                    }
                    else if (compare == MemberComparisonResult.DifferentSignature)
                    {
                        if (!item.Checked) continue;

                        List<MemberOwnerPair> modelList;
                        if (!_memberConflicts.TryGetValue(itemModel.Name, out modelList))
                        {
                            modelList = new List<MemberOwnerPair>();
                            _memberConflicts[itemModel.Name] = modelList;
                        }
                        if (modelList.Find(c => c.Owner.QualifiedName == existing.Owner.QualifiedName) == null)
                            modelList.Add(existing);
                        if (modelList.Find(c => c.Owner == null) == null)
                            modelList.Add(new MemberOwnerPair { Owner = null, Member = itemModel });
                    }
                }
                else
                    item.ForeColor = SystemColors.WindowText;
            }
        }

        private string MemberToItemString(MemberModel m)
        {
            if (m == null) return ASContext.Context.Features.voidKey;

            var retVal = new StringBuilder();

            var isFunc = (m.Flags & (FlagType.Getter | FlagType.Setter | FlagType.Function)) > 0;

            if ((m.Flags & FlagType.Getter) > 0)
                retVal.Append("get_");
            else if ((m.Flags & FlagType.Setter) > 0)
                retVal.Append("set_");

            retVal.Append(m.Name);

            if (isFunc)
            {
                retVal.Append("(");
                if (m.Parameters != null)
                {
                    foreach (var p in m.Parameters)
                    {
                        var type = p.Type ?? ASContext.Context.Features.objectKey;
                        retVal.Append(type.Substring(type.LastIndexOf('.') + 1)).Append(", ");
                    }
                    retVal.Remove(retVal.Length - 2, 2);
                }
                retVal.Append(")");
            }

            retVal.Append(":");
            var rType = m.Type ?? ASContext.Context.Features.voidKey;
            retVal.Append(rType.Substring(rType.LastIndexOf('.') + 1));
            
            return retVal.ToString();
        }

        #region Members Comparison

        private enum MemberComparisonResult
        {
            Identical, DifferentAccessors, DifferentSignature
        }

        private MemberComparisonResult CompareMembers(ClassModel ownerA, MemberModel memberA, FlagType extraFlagsMemberA, ClassModel ownerB,
                                                      MemberModel memberB)
        {
            // We don't check the name here
            const FlagType property = FlagType.Getter | FlagType.Setter;
            
            if ((memberB.Flags & property) > 0)
            {
                if ((memberA.Flags & property) > 0)
                {
                    string typeA, typeB;
                    if ((memberA.Flags & FlagType.Getter) > 0)
                        typeA = ownerA != null ?
                            ASContext.Context.ResolveType(memberA.Type, ownerA.InFile).QualifiedName : memberA.Type;
                    else
                        typeA = ownerA != null ?
                            ASContext.Context.ResolveType(memberA.Parameters[0].Type, ownerA.InFile).QualifiedName : memberA.Parameters[0].Type;

                    if ((memberB.Flags & FlagType.Getter) > 0)
                        typeB = ownerB != null ?
                            ASContext.Context.ResolveType(memberB.Type, ownerB.InFile).QualifiedName : memberB.Type;
                    else
                        typeB = ownerB != null ?
                            ASContext.Context.ResolveType(memberB.Parameters[0].Type, ownerB.InFile).QualifiedName : 
                            memberB.Parameters[0].Type;

                    if (typeA != typeB) return MemberComparisonResult.DifferentSignature;

                    if ((extraFlagsMemberA & (memberB.Flags & property)) > 0) return MemberComparisonResult.Identical;
                    return MemberComparisonResult.DifferentAccessors;
                }
                else
                    return MemberComparisonResult.DifferentSignature;
            }
            else if ((memberA.Flags & property) > 0)
                return MemberComparisonResult.DifferentSignature;
            else if (!ParametersAreEqual(ownerA, memberA, ownerB, memberB))
                return MemberComparisonResult.DifferentSignature;
            
            return MemberComparisonResult.Identical;
        }

        private bool ParametersAreEqual(ClassModel ownerA, MemberModel memberA, ClassModel ownerB, MemberModel memberB)
        {
            var context = ASContext.Context;
            if (memberA.Parameters != null && memberB.Parameters == null ||
                memberA.Parameters == null && memberB.Parameters != null)
                return false;

            if (memberB.Parameters != null)
            {
                if (memberB.Parameters.Count != memberA.Parameters.Count)
                    return false;

                string typeA = ownerA != null && memberA.Type != null && memberA.Type != "*"
                                    ? context.ResolveType(memberA.Type, ownerA.InFile).QualifiedName : memberA.Type;
                string typeB = ownerB != null && memberB.Type != null && memberB.Type != "*"
                                    ? context.ResolveType(memberB.Type, ownerB.InFile).QualifiedName : memberB.Type;

                if (typeA != typeB) return false;

                for (int i = 0, count = memberB.Parameters.Count; i < count; i++)
                {
                    var aParam = memberA.Parameters[i];
                    var bParam = memberB.Parameters[i];
                    // We're using owner=null for members from the listview
                    typeA = ownerA != null && aParam.Type != null && aParam.Type != "*"
                                    ? context.ResolveType(aParam.Type, ownerA.InFile).QualifiedName : aParam.Type;
                    typeB = ownerB != null && bParam.Type != null && bParam.Type != "*"
                                    ? context.ResolveType(bParam.Type, ownerB.InFile).QualifiedName : bParam.Type;
                    if (typeA != typeB ||
                        aParam.Value != bParam.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        private class InterfaceListItem
        {
            public ClassModel Model;

            public override string ToString()
            {
                return Model.QualifiedName;
            }
        }

        private class ArgumentListItem
        {
            public string Name;
            public string Type;
            public string Value;

            public override string ToString()
            {
                var retVal = Name + ":" + Type;
                if (!string.IsNullOrEmpty(Value)) retVal += "=" + Value;

                return retVal;
            }
        }

        private class MemberOwnerPair
        {
            public ClassModel Owner;
            private MemberModel _member;
            public MemberModel Member
            {
                get { return _member; }
                set
                {
                    if (_member == value) return;
                    _member = value;
                    Properties = _member.Flags;
                }
            }

            // Used to discern which property accessors are already defined since we are using just the name to store the members
            public FlagType Properties;
        }

    }
}
