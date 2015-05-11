using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using PluginCore;
using PluginCore.Localization;

namespace ASClassWizard.Wizards
{
    public partial class NamespaceDialog : Form
    {

        public string NamespacePrefix
        {
            get { return prefixBox.Text.Trim(); }
            set { prefixBox.Text = value; }
        }

        public string NamespaceName
        {
            get { return nameComboBox.Text.Trim(); }
            set { nameComboBox.Text = value; }
        }

        public bool AllowCustomNames
        {
            get { return nameComboBox.DropDownStyle == ComboBoxStyle.DropDown; }
            set { nameComboBox.DropDownStyle = value ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList; }
        }

        public Dictionary<string, string> ExistingNamespaces { get; set; }

        private IEnumerable<string> _knownNames;
        public IEnumerable<string> KnownNames
        {
            get { return _knownNames; }
            set
            {
                if (_knownNames == value) return;

                _knownNames = value;
                nameComboBox.Items.Clear();

                int maxWidth = nameComboBox.Width;
                if (value != null)
                {
                    int scrollBarWidth = (nameComboBox.Items.Count > nameComboBox.MaxDropDownItems)
                                             ? SystemInformation.VerticalScrollBarWidth
                                             : 0;

                    foreach (var name in _knownNames)
                    {
                        int width = TextRenderer.MeasureText(name, nameComboBox.Font).Width + scrollBarWidth;
                        if (width > maxWidth)
                            maxWidth = width;
                        nameComboBox.Items.Add(name);
                    }
                }
                nameComboBox.DropDownWidth = maxWidth;
            }
        }

        public NamespaceDialog()
        {
            InitializeComponent();
            this.Font = PluginBase.Settings.DefaultFont;
            LocalizeText();
        }

        private void LocalizeText()
        {
            this.Text = TextHelper.GetString("NamespacesDialog.Title");
            this.prefixLabel.Text = TextHelper.GetString("NamespacesDialog.Label.Prefix");
            this.nameLabel.Text = TextHelper.GetString("NamespacesDialog.Label.Name");
            this.okButton.Text = TextHelper.GetString("Wizard.Button.Ok");
            this.cancelButton.Text = TextHelper.GetString("Wizard.Button.Cancel");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
                e.Cancel = !ValidateChildren();
            base.OnFormClosing(e);
        }

        private void PrefixBox_Validating(object sender, CancelEventArgs e)
        {
            string prefix = NamespacePrefix;
            string error = null;
            if (ExistingNamespaces != null && ExistingNamespaces.ContainsKey(prefix))
            {
                error = TextHelper.GetString("NamespacesDialog.Error.ExistingPrefix");
                e.Cancel = true;
            }
            else if (prefix != "")
            {
                try
                {
                    if (XmlConvert.VerifyNCName(prefix) != prefix)
                    {
                        error = TextHelper.GetString("NamespacesDialog.Error.ExistingPrefix");
                        e.Cancel = true;
                    }
                }
                catch (Exception ex)
                {
                    error = TextHelper.GetString("NamespacesDialog.Error.ExistingPrefix") + ": " + ex.Message;
                    e.Cancel = true;
                }
            }

            errorProvider.SetError(prefixBox, error);
        }

        private void NameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // This would be interesting, although we need to think of something better than just this event
            /*if (prefixBox.Text != "") return;

            string suggestedPrefix;
            if (nameComboBox.Text == "*")
            {
                suggestedPrefix = "locals";
            }
            else if (nameComboBox.Text.EndsWith(".*"))
            {
                int dot = nameComboBox.Text.LastIndexOf('.', nameComboBox.Text.Length - 3) + 1;
                suggestedPrefix = nameComboBox.Text.Substring(dot, nameComboBox.Text.Length - 2 - dot);
            }
            else if (nameComboBox.Text == "library://ns.adobe.com/flex/spark")
            {
                suggestedPrefix = "s";
            }
            else if (nameComboBox.Text == "library://ns.adobe.com/flex/mx" || nameComboBox.Text == "http://www.adobe.com/2006/mxml")
            {
                suggestedPrefix = "mx";
            }
            else if (nameComboBox.Text == "http://ns.adobe.com/mxml/2009")
            {
                suggestedPrefix = "fx";
            }
            else
            {
                suggestedPrefix =
                    System.IO.Path.GetFileNameWithoutExtension(
                        nameComboBox.Text.Substring(nameComboBox.Text.LastIndexOf('/') + 1));
            }

            prefixBox.Text = suggestedPrefix;*/
        }

        private void NameComboBox_Validating(object sender, CancelEventArgs e)
        {
            string error = null;
            if (NamespaceName == "")
            {
                error = TextHelper.GetString("NamespacesDialog.Error.BlankName");
                e.Cancel = true;
            }
            errorProvider.SetError(nameComboBox, error);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            AutoValidate = AutoValidate.Disable;
        }

    }
}
