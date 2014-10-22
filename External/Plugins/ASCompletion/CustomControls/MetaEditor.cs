//TODO: Ensure new fields have name
//TODO: Ensure field and meta names start with letters

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ASCompletion.Completion;

namespace ASCompletion.Forms
{
    public partial class MetaEditorForm : Form
    {

        private MetaEntry _selectedMeta;

        private List<MetaEntry> _values;
        public IEnumerable<MetaEntry> Values
        {
            get { return _values; }
            set {
                if (_values == value) return;
                
                metaList.Items.Clear();

                _values.Clear();

                if (value == null) return;

                foreach (var meta in value)
                {
                    var copy = (MetaEntry)meta.Clone();
                    var item = metaList.Items.Add(copy.Label);
                    item.Tag = copy;

                    _values.Add(copy);
                }
            }
        }

        public MetaEditorForm()
        {
            InitializeComponent();
            _values = new List<MetaEntry>();
            metaFieldsGrid.AutoGenerateColumns = false;
            MetaFieldsGrid_EnabledChanged(metaFieldsGrid, EventArgs.Empty);
        }

        private void MetaList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (metaList.SelectedItems.Count > 0)
            {
                _selectedMeta = (MetaEntry)metaList.SelectedItems[0].Tag;

                appearsInHelpCheck.Enabled = classesCheck.Enabled = attributesCheck.Enabled = functionsCheck.Enabled = true;

                appearsInHelpCheck.Checked = _selectedMeta.AppearsInFieldHelp;
                classesCheck.Checked = (_selectedMeta.DecoratableFields & MetaEntry.DecoratableField.Class) > 0;
                attributesCheck.Checked = (_selectedMeta.DecoratableFields & MetaEntry.DecoratableField.Attribute) > 0;
                functionsCheck.Checked = (_selectedMeta.DecoratableFields & MetaEntry.DecoratableField.Function) > 0;

                addDescLangButton.Enabled = true;
                removeMetaButton.Enabled = true;

                if (_selectedMeta.Description != null)
                {
                    foreach (var entry in _selectedMeta.Description)
                        descriptionKeyCombo.Items.Add(entry.Key);
                    if (descriptionKeyCombo.Items.Count > 0)
                    {
                        descriptionKeyCombo.SelectedIndex = 0;
                        descriptionTextBox.Enabled = true;
                        descriptionKeyCombo.Enabled = true;
                    }
                }

                if (_selectedMeta.Fields == null) _selectedMeta.Fields = new List<MetaField>();
                metaFieldsGrid.DataSource = new BindingList<MetaField>(_selectedMeta.Fields);
                metaFieldsGrid.Enabled = true;
            }
            else
            {
                _selectedMeta = null;

                appearsInHelpCheck.Checked = appearsInHelpCheck.Enabled = false;
                classesCheck.Checked = classesCheck.Enabled = false;
                attributesCheck.Checked = attributesCheck.Enabled = false;
                functionsCheck.Checked = functionsCheck.Enabled = false;
                descriptionKeyCombo.Items.Clear();
                descriptionKeyCombo.Enabled = false;
                descriptionTextBox.Clear();
                descriptionTextBox.Enabled = false;
                removeMetaButton.Enabled = false;
                
                metaFieldsGrid.Enabled = false;
                metaFieldsGrid.DataSource = null;
            }
        }

        private void AddMetaButton_Click(object sender, EventArgs e)
        {
            _selectedMeta = new MetaEntry {Label = "Meta"};
            _values.Add(_selectedMeta);
            var newItem = metaList.Items.Add("Meta");
            newItem.Tag = _selectedMeta;
            newItem.BeginEdit();
        }

        private void RemoveMetaButton_Click(object sender, EventArgs e)
        {
            int i = metaList.SelectedIndices[0];
            _values.RemoveAt(i);
            metaList.Items.RemoveAt(i);
        }

        private void DescriptionKeyCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (descriptionKeyCombo.SelectedIndex == -1)
            {
                descriptionTextBox.Clear();
                removeDescLangButton.Enabled = false;

                return;
            }

            string description;

            if (_selectedMeta.Description != null &&
                _selectedMeta.Description.TryGetValue(descriptionKeyCombo.Text, out description))
                descriptionTextBox.Text = description;

            descriptionTextBox.Enabled = true;
            removeDescLangButton.Enabled = true;
        }

        private void MetaList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null) return;

            if (!IsValidIdentifierName(e.Label))
            {
                MessageBox.Show("Not valid identifier", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.CancelEdit = true;
                return;
            }

            foreach (var meta in _values)
            {
                if (meta.Label == e.Label && meta != _selectedMeta && (meta.DecoratableFields & _selectedMeta.DecoratableFields) > 0)
                {
                    MessageBox.Show("There is already a meta tag with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.CancelEdit = true;
                    return;
                }
            }

            _selectedMeta.Label = e.Label;
        }

        private void AppearsInHelpCheck_Click(object sender, EventArgs e)
        {
            _selectedMeta.AppearsInFieldHelp = appearsInHelpCheck.Checked;
        }

        private void FieldsCheck_Click(object sender, EventArgs e)
        {
            MetaEntry.DecoratableField value = 0;
            if (classesCheck.Checked) value = MetaEntry.DecoratableField.Class;
            if (attributesCheck.Checked) value |= MetaEntry.DecoratableField.Attribute;
            if (functionsCheck.Checked) value |= MetaEntry.DecoratableField.Function;

            foreach (var meta in _values)
            {
                if (meta.Label == _selectedMeta.Label && meta != _selectedMeta && (meta.DecoratableFields & value) > 0)
                {
                    var checkBox = (CheckBox)sender;

                    MessageBox.Show("There is already a meta tag with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    checkBox.Checked = !checkBox.Checked;
                    return;
                }
            }
            _selectedMeta.DecoratableFields = value;
        }

        private void MetaFieldsGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 0 && metaFieldsGrid.Enabled)
            {
                string fieldName = (string) e.FormattedValue;
                if (!IsValidIdentifierName(fieldName))
                {
                    MessageBox.Show("Not valid identifier", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
                else
                {
                    foreach (var field in _selectedMeta.Fields)
                    {
                        if (field.Name == fieldName)
                        {
                            MessageBox.Show("There is already a field with the same name", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            e.Cancel = true;
                            break;
                        }
                    }
                }
            }
        }

        private void MetaFieldsGrid_EnabledChanged(object sender, EventArgs e)
        {
            if (!metaFieldsGrid.Enabled)
            {
                metaFieldsGrid.DefaultCellStyle.BackColor = SystemColors.Control;
                metaFieldsGrid.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                metaFieldsGrid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                metaFieldsGrid.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.GrayText;
                metaFieldsGrid.CurrentCell = null;
                metaFieldsGrid.ReadOnly = true;
            }
            else
            {
                metaFieldsGrid.DefaultCellStyle.BackColor = SystemColors.Window;
                metaFieldsGrid.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                metaFieldsGrid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Window;
                metaFieldsGrid.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
                metaFieldsGrid.ReadOnly = false;
            }
        }

        private void AddDescLangButton_Click(object sender, EventArgs e)
        {
            using (var descriptionIsoDialog = new DescriptionIsoInputDialog("New description language",
                                                                         "Select or write description language ISO", _selectedMeta.Description))
            {
                if (descriptionIsoDialog.ShowDialog(this) == DialogResult.OK)
                {
                    descriptionKeyCombo.Items.Add(descriptionIsoDialog.Value);
                    descriptionKeyCombo.SelectedIndex = descriptionKeyCombo.Items.Count - 1;

                    descriptionKeyCombo.Enabled = true;
                }
            }
        }

        private void RemoveDescLangButton_Click(object sender, EventArgs e)
        {
            _selectedMeta.Description.Remove(descriptionKeyCombo.Text);
            descriptionKeyCombo.Items.RemoveAt(descriptionKeyCombo.SelectedIndex);

            if (descriptionKeyCombo.Items.Count == 0)
            {
                descriptionTextBox.Clear();
                removeDescLangButton.Enabled = descriptionKeyCombo.Enabled = false;
            }
        }

        private void DescriptionTextBox_Leave(object sender, EventArgs e)
        {
            if (_selectedMeta.Description == null) _selectedMeta.Description = new Dictionary<string, string>();
            _selectedMeta.Description[descriptionKeyCombo.Text] = descriptionTextBox.Text;
        }

        private bool IsValidIdentifierName(string value)
        {
            return !string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "\\s") && Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private class DescriptionIsoInputDialog : Form
        {

            private ComboBox inputCombo = new ComboBox();

            private Dictionary<string, string> descriptions;

            private string _value;
            public string Value {
                get { return _value; }
            }

            public DescriptionIsoInputDialog(string title, string promptText, Dictionary<string, string> descriptions)
            {
                Label label = new Label();
                Button okButton = new Button();
                Button cancelButton = new Button();

                Text = title;
                label.Text = promptText;

                okButton.Text = "&OK";
                cancelButton.Text = "&Cancel";
                okButton.DialogResult = DialogResult.OK;
                cancelButton.DialogResult = DialogResult.Cancel;

                inputCombo.Items.AddRange(new[] {"zh", "zh-Hant", "cs", "nl", "en", "fr", "de", "it", "ja", "ko", "pl", "pt", "pt-BR", "ru", "es", "sv", "tr"});

                label.SetBounds(9, 20, 372, 13);
                inputCombo.SetBounds(12, 36, 372, 20);
                okButton.SetBounds(228, 72, 75, 23);
                cancelButton.SetBounds(309, 72, 75, 23);

                label.AutoSize = true;
                inputCombo.Anchor = inputCombo.Anchor | AnchorStyles.Right;
                okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                inputCombo.Validating += InputCombo_Validating;

                ClientSize = new Size(396, 107);
                Controls.AddRange(new Control[] { label, inputCombo, okButton, cancelButton });
                ClientSize = new Size(Math.Max(300, label.Right + 10), ClientSize.Height);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterScreen;
                MinimizeBox = false;
                MaximizeBox = false;
                AcceptButton = okButton;
                CancelButton = cancelButton;

                this.descriptions = descriptions;
            }

            private void InputCombo_Validating(Object sender, CancelEventArgs e)
            {
                string iso = inputCombo.Text.Trim();
                if (iso == string.Empty || (descriptions != null && descriptions.ContainsKey(iso)))
                {
                    e.Cancel = true;
                }
                else _value = iso;
            }

        }

    }

    public class MetaEditor : UITypeEditor
    {

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (svc != null)
            {
                using (Forms.MetaEditorForm form = new Forms.MetaEditorForm())
                {
                    form.Values = (IEnumerable<MetaEntry>)value;
                    if (svc.ShowDialog(form) == DialogResult.OK)
                    {
                        value = form.Values;
                    }
                }
            }

            value = value ?? new MetaEntry[]{};

            return value;
        }

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

    }

}
