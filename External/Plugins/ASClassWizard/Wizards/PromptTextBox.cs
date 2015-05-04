// There is a native API to show a prompt in TextBoxes and ComboBoxes, but I'm not using it in order to not have native code...

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ASClassWizard.Wizards
{
    public class PromptTextBox : TextBox
    {

        private bool _inControl;

        private string _prompt = "";
        [DefaultValue("")]
        [Localizable(true)]
        public string Prompt
        {
            get { return _prompt; }
            set
            {
                if (value == null) value = string.Empty;

                _prompt = value;
                if (_text == string.Empty)
                {
                    base.Text = _prompt;
                    base.ForeColor = PromptForeColor;
                }
            }
        }
        
        private Color _promptForeColor = SystemColors.GrayText;
        [Localizable(true)]
        public Color PromptForeColor
        {
            get { return _promptForeColor; }
            set
            {
                if (_text == string.Empty && Prompt != string.Empty)
                    base.ForeColor = _promptForeColor;
            }
        }

        private Color _foreColor = SystemColors.ControlText;
        public override Color ForeColor
        {
            get
            {
                return Text != string.Empty || DesignMode ? _foreColor : PromptForeColor;
            }
            set
            {
                _foreColor = value;
                if (Text != string.Empty)
                    base.ForeColor = value;
            }
        }

        private string _text = "";
        public override string Text
        {
            get
            {
                return _inControl ? base.Text : _text;
            }
            set
            {
                if (value == null) value = string.Empty;
                
                _text = value;
                if (_text == string.Empty && Prompt != string.Empty && !_inControl)
                    base.Text = Prompt;
                else
                    base.Text = value;
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            _inControl = true;
            base.OnEnter(e);
            if (_text == string.Empty)
            {
                base.Text = string.Empty;
                base.ForeColor = ForeColor;
            }
        }

        protected override void OnLeave(EventArgs e)
        {
            _inControl = false;
            _text = base.Text;
            if (_text == string.Empty && Prompt != string.Empty)
            {
                base.Text = Prompt;
                base.ForeColor = PromptForeColor;
            }
            base.OnLeave(e);
        }
    }
}
