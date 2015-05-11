using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using PluginCore;
using PluginCore.Localization;
using ProjectManager.Projects;

using ASCompletion.Context;
using ASCompletion.Model;

using AS3Context;
using ProjectManager.Projects.AS3;

namespace ASClassWizard.Wizards
{
    public partial class MxmlClassWizard : Form
    {
        private string directoryPath;
        private Project project;
        public const string REG_IDENTIFIER_AS = "^[a-zA-Z_$][a-zA-Z0-9_$]*$";

        private int flexVersion;

        public MxmlClassWizard()
        {
            InitializeComponent();
            LocalizeText();
            CenterToParent();
            this.Font = PluginBase.Settings.DefaultFont;
            this.errorIcon.Image = PluginMain.MainForm.FindImage("197");

            _classModel = new ClassModel { Flags = FlagType.Class, InFile = new FileModel(string.Empty) };
            _namespaces = new Dictionary<string, string>();
        }

        private void LocalizeText()
        {
            this.titleLabel.Text = TextHelper.GetString("Wizard.Label.NewMxmlClass");
            this.Text = TextHelper.GetString("Wizard.Label.NewMxmlClass");
            this.classLabel.Text = TextHelper.GetString("Wizard.Label.Name");
            this.baseLabel.Text = TextHelper.GetString("Wizard.Label.SuperClass");
            this.implementLabel.Text = TextHelper.GetString("Wizard.Label.Interfaces");
            this.implementBrowse.Text = TextHelper.GetString("Wizard.Button.Add");
            this.implementRemove.Text = TextHelper.GetString("Wizard.Button.Remove");
            this.packageLabel.Text = TextHelper.GetString("Wizard.Label.Package");
            this.packageBrowse.Text = TextHelper.GetString("Wizard.Button.Browse");
            this.baseBrowse.Text = TextHelper.GetString("Wizard.Button.Browse");
            this.okButton.Text = TextHelper.GetString("Wizard.Button.Ok");
            this.cancelButton.Text = TextHelper.GetString("Wizard.Button.Cancel");
            this.generationLabel.Text = TextHelper.GetString("Wizard.Label.CodeGeneration");
            this.interfaceCheck.Text = TextHelper.GetString("Wizard.Label.GenerateInherited");
            this.namespacesLabel.Text = TextHelper.GetString("Wizard.Label.Namespaces");
            this.namespacesAdd.Text = TextHelper.GetString("Wizard.Button.Add");
            this.namespacesRemove.Text = TextHelper.GetString("Wizard.Button.Remove");
        }

        public String Package
        {
            get { return packageBox.Text.Trim(); }
            set { packageBox.Text = value; }
        }

        public String StartupClassName
        {
            set { classBox.Text = value; }
        }

        public string ClassName
        {
            get { return classBox.Text.Trim(); }
            set { classBox.Text = value; }
        }

        public string Directory
        {
            get { return this.directoryPath; }
            set { this.directoryPath = value; }
        }

        public Project Project
        {
            get { return project; }
            set 
            { 
                this.project = value;
                string cp = String.Join(";", ((MxmlcOptions) project.CompilerOptions).IntrinsicPaths ?? new string[] {}).Replace('\\', '/');
                if (cp.Contains("Library/AS3/frameworks/Flex3"))
                {
                    _namespaces["mx"] = MxmlFilter.OLD_MX;
                    namespacesList.Items.Add("mx: " + MxmlFilter.OLD_MX);
                    flexVersion = 3;
                }
                else
                {
                    _namespaces["fx"] = "http://ns.adobe.com/mxml/2009";
                    namespacesList.Items.Add("fx: http://ns.adobe.com/mxml/2009");
                    flexVersion = 4;
                }
            }
        }

        private ClassModel _classModel;
        public ClassModel ClassModel
        {
            get { return _classModel; }
            set
            {
                if (_classModel == value) return;
                _classModel = value;
            }
        }

        public bool ImplementInterfaces
        {
            get { return interfaceCheck.Checked; }
            set { interfaceCheck.Checked = value; }
        }

        private string baseClassPrefix;
        public string BaseClassPrefix
        {
            get { return baseClassPrefix; }
        }

        private Dictionary<string, string> _namespaces;
        public Dictionary<string, string> Namespaces
        {
            get { return _namespaces; }
            set { _namespaces = value; }
        }

        private void ValidateClass()
        {
            string errorMessage = "";
            string regex = REG_IDENTIFIER_AS;
            if (classBox.Text == "")
                errorMessage = TextHelper.GetString("Wizard.Error.EmptyClassName");
            else if (!Regex.Match(classBox.Text, regex, RegexOptions.Singleline).Success)
                errorMessage = TextHelper.GetString("Wizard.Error.InvalidClassName");
            else if (baseBox.Text == "")
                errorMessage = TextHelper.GetString("Wizard.Error.MissingMxmlBaseClass");

            if (errorMessage != "")
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

        #region EventHandlers

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
                        directoryPath = browser.Package;
                        package = package.Replace(Path.DirectorySeparatorChar, '.');
                        this.packageBox.Text = package;
                    }
                }
                else
                {
                    this.directoryPath = browser.Project.Directory;
                    this.packageBox.Text = "";
                }
            }
        }

        private void AS3ClassWizard_Load(object sender, EventArgs e)
        {
            this.classBox.Select();
            this.ValidateClass();
        }

        private void BaseBrowse_Click(object sender, EventArgs e)
        {
            using (ClassBrowser browser = new ClassBrowser())
            {
                IASContext context = ASContext.GetLanguageContext(PluginBase.CurrentProject.Language);
                browser.ClassList = context.GetAllProjectClasses();
                browser.ExcludeFlag = FlagType.Interface;
                browser.IncludeFlag = FlagType.Class;
                if (browser.ShowDialog(this) == DialogResult.OK)
                {
                    string baseClass = browser.SelectedClass;

                    if (baseClass == null) return;

                    // Check components defined in available manifests
                    var existingNames = new List<string>();
                    foreach (var cat in MxmlFilter.GetCatalogs().Values)
                    {
                        if (cat.ContainsValue(baseClass))
                        {
                            // Is this namespace already imported?
                            foreach (var nsentry in _namespaces)
                            {
                                if (nsentry.Value == cat.URI)
                                {
                                    baseBox.Text = baseClass;
                                    baseClassPrefix = nsentry.Key;
                                    return;
                                }
                            }

                            existingNames.Add(cat.URI);
                        }
                    }

                    // No manifest with this class, import the classpath, we may do this check even if we have manifests
                    if (existingNames.Count == 0)
                    {
                        int dot = baseClass.LastIndexOf('.');
                        string name = (dot > -1 ? baseClass.Substring(0, dot + 1) : "") + "*";
                        // Is this namespace already imported?
                        foreach (var nsentry in _namespaces)
                        {
                            if (nsentry.Value == name)
                            {
                                baseBox.Text = baseClass;
                                baseClassPrefix = nsentry.Key;
                                return;
                            }
                        }
                        existingNames.Add(name);
                    }

                    using (var nsDialog = new NamespaceDialog())
                    {
                        nsDialog.StartPosition = FormStartPosition.CenterParent;
                        nsDialog.KnownNames = existingNames;
                        nsDialog.ExistingNamespaces = Namespaces;
                        nsDialog.AllowCustomNames = false;

                        if (nsDialog.ShowDialog(this) != DialogResult.OK)
                            return;
                        
                        namespacesList.Items.Add(nsDialog.NamespacePrefix + ": " + nsDialog.NamespaceName);
                        Namespaces[nsDialog.NamespacePrefix] = nsDialog.NamespaceName;

                        baseClassPrefix = nsDialog.NamespacePrefix;
                    }

                    baseBox.Text = baseClass;
                }
                this.okButton.Focus();
            }
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
                    foreach (string item in this.implementList.Items)
                    {
                        if (item == browser.SelectedClass) return;
                    }
                    this.implementList.Items.Add(browser.SelectedClass);
                }
            }
            this.implementRemove.Enabled = this.implementList.Items.Count > 0;
            this.implementList.SelectedIndex = this.implementList.Items.Count - 1;
            this.interfaceCheck.Enabled = this.implementList.Items.Count > 0;
            ValidateClass();
        }

        /// <summary>
        /// Remove interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterfaceRemove_Click(object sender, EventArgs e)
        {
            if (this.implementList.SelectedItem != null)
            {
                this.implementList.Items.Remove(this.implementList.SelectedItem);
            }
            if (this.implementList.Items.Count > 0)
            {
                this.implementList.SelectedIndex = this.implementList.Items.Count - 1;
            }
            this.implementRemove.Enabled = this.implementList.Items.Count > 0;
            this.interfaceCheck.Enabled = this.implementList.Items.Count > 0;
            ValidateClass();
        }

        private void PackageBox_TextChanged(object sender, EventArgs e)
        {
            ValidateClass();
        }

        private void ClassBox_TextChanged(object sender, EventArgs e)
        {
            ValidateClass();
        }

        private void BaseBox_TextChanged(object sender, EventArgs e)
        {
            ValidateClass();
        }

        #endregion

        public static Image GetResource( string resourceID )
        {
            resourceID = "ASClassWizard." + resourceID;
            Assembly assembly = Assembly.GetExecutingAssembly();
            Image image = new Bitmap(assembly.GetManifestResourceStream(resourceID));
            return image;
        }

        private void NamespacesAdd_Click(object sender, EventArgs e)
        {
            using (var nsDialog = new NamespaceDialog())
            {
                nsDialog.StartPosition = FormStartPosition.CenterParent;
                nsDialog.KnownNames = MxmlFilter.GetCatalogs().Keys;
                nsDialog.ExistingNamespaces = Namespaces;

                if (nsDialog.ShowDialog(this) == DialogResult.OK)
                {
                    namespacesList.Items.Add(nsDialog.NamespacePrefix + ": " + nsDialog.NamespaceName);
                    Namespaces[nsDialog.NamespacePrefix] = nsDialog.NamespaceName;
                }
            }
        }

        private void NamespacesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            namespacesRemove.Enabled = namespacesList.SelectedItems.Count > 0 &&
                                       !namespacesList.SelectedItem.ToString().StartsWith(baseClassPrefix + ": ") &&
                                       namespacesList.SelectedIndex > 0;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            _classModel.Name = classBox.Text.Trim();
            _classModel.Type = baseBox.Text.Substring(baseBox.Text.LastIndexOf('.') + 1);
            List<string> interfaces = new List<string>(this.implementList.Items.Count);
            foreach (var item in this.implementList.Items)
            {
                interfaces.Add(item.ToString());
            }
            _classModel.Implements = interfaces;
        }

        private void NamespacesRemove_Click(object sender, EventArgs e)
        {
            _namespaces.Remove(namespacesList.SelectedItem.ToString().Substring(0, namespacesList.SelectedItem.ToString().IndexOf(": ")));
            this.namespacesList.Items.RemoveAt(this.namespacesList.SelectedIndex);
            if (this.namespacesList.Items.Count > 0)
            {
                this.namespacesList.SelectedIndex = this.namespacesList.Items.Count - 1;
            }
        }


    }
}
