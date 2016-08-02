using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlashDevelop.Controls;
using FlashDevelop.Dialogs;
using FlashDevelop.Managers;
using PluginCore;
using PluginCore.Managers;

namespace FlashDevelop
{
    // TODO: Share dialogs? clean method signatures and rename
    // TODO: Fix problem with QuickFind resizing of the documents
    // TODO: Proper shortcuts working without hacks, at least for some main ones like Ctrl+Shift+1, or Save
    class WinFormsEditorController : IEditorController
    {
        private QuickFind quickFind;
        private FRInFilesDialog frInFilesDialog;
        private FRInDocDialog frInDocDialog;
        private GoToDialog gotoDialog;

        private Form owner;
        public object Owner
        {
            get { return owner; }
        }

        public object QuickFindControl
        {
            get { return quickFind; }
        }

        public bool CanSearch
        {
            get { return this.quickFind.CanSearch; }
            set { this.quickFind.CanSearch = value; }
        }

        public WinFormsEditorController(Form owner)
        {
            this.owner = owner;
            this.quickFind = new QuickFind();
            this.gotoDialog = new GoToDialog();
            this.frInFilesDialog = new FRInFilesDialog();
            this.frInDocDialog = new FRInDocDialog(this);
        }

        public bool? ProcessCmdKey(Keys keyData)
        {
            /**
            * Notify plugins. Don't notify ControlKey or ShiftKey as it polls a lot
            */
            KeyEvent ke = new KeyEvent(EventType.Keys, keyData);
            Keys keyCode = keyData & Keys.KeyCode;
            if ((keyCode != Keys.ControlKey) && (keyCode != Keys.ShiftKey))
            {
                EventManager.DispatchEvent(this, ke);
            }
            if (!ke.Handled)
            {
                /**
                * Ignore basic control keys if sci doesn't have focus.
                */
                if (Globals.SciControl == null || !Globals.SciControl.IsFocus)
                {
                    if (keyData == (Keys.Control | Keys.C)) return false;
                    else if (keyData == (Keys.Control | Keys.V)) return false;
                    else if (keyData == (Keys.Control | Keys.X)) return false;
                    else if (keyData == (Keys.Control | Keys.A)) return false;
                    else if (keyData == (Keys.Control | Keys.Z)) return false;
                    else if (keyData == (Keys.Control | Keys.Y)) return false;
                }
                /**
                * Process special key combinations and allow "chaining" of 
                * Ctrl-Tab commands if you keep holding control down.
                */
                if ((keyData & Keys.Control) != 0)
                {
                    Boolean sequentialTabbing = Globals.MainForm.AppSettings.SequentialTabbing;
                    if ((keyData == (Keys.Control | Keys.Next)) || (keyData == (Keys.Control | Keys.Tab)))
                    {
                        TabbingManager.TabTimer.Enabled = true;
                        if (keyData == (Keys.Control | Keys.Next) || sequentialTabbing)
                        {
                            TabbingManager.NavigateTabsSequentially(1);
                        }
                        else TabbingManager.NavigateTabHistory(1);
                        return true;
                    }
                    if ((keyData == (Keys.Control | Keys.Prior)) || (keyData == (Keys.Control | Keys.Shift | Keys.Tab)))
                    {
                        TabbingManager.TabTimer.Enabled = true;
                        if (keyData == (Keys.Control | Keys.Prior) || sequentialTabbing)
                        {
                            TabbingManager.NavigateTabsSequentially(-1);
                        }
                        else TabbingManager.NavigateTabHistory(-1);
                        return true;
                    }
                }
                if (keyData == ShortcutManager.GetRegisteredItem("SearchMenu.FindAndReplace").Custom)
                {
                    this.FindAndReplace(null, null);
                    return true;
                }
                else if (keyData == ShortcutManager.GetRegisteredItem("SearchMenu.FindAndReplaceInFiles").Custom)
                {
                    this.FindAndReplaceInFiles(null, null);
                    return true;
                }
                else if (keyData == ShortcutManager.GetRegisteredItem("SearchMenu.QuickFind").Custom)
                {
                    this.QuickFind(null, null);
                    return true;
                }
                else if (keyData == ShortcutManager.GetRegisteredItem("SearchMenu.QuickFindNext").Custom)
                {
                    this.FindNext(null, null);
                    return true;
                }
                else if (keyData == ShortcutManager.GetRegisteredItem("SearchMenu.QuickFindPrevious").Custom)
                {
                    this.FindPrevious(null, null);
                    return true;
                }
                return null;
            }
            return true;
        }

        public void ApplyAllSettings()
        {
            this.frInFilesDialog.UpdateSettings();
        }

        /// <summary>
        /// Sets the text to find globally
        /// </summary>
        public void SetFindText(Object sender, String text)
        {
            if (sender != this.quickFind) this.quickFind.SetFindText(text);
            if (sender != this.frInDocDialog) this.frInDocDialog.SetFindText(text);
        }

        /// <summary>
        /// Sets the case setting to find globally
        /// </summary>
        public void SetMatchCase(Object sender, Boolean matchCase)
        {
            if (sender != this.quickFind) this.quickFind.SetMatchCase(matchCase);
            if (sender != this.frInDocDialog) this.frInDocDialog.SetMatchCase(matchCase);
        }

        /// <summary>
        /// Sets the whole word setting to find globally
        /// </summary>
        public void SetWholeWord(Object sender, Boolean wholeWord)
        {
            if (sender != this.quickFind) this.quickFind.SetWholeWord(wholeWord);
            if (sender != this.frInDocDialog) this.frInDocDialog.SetWholeWord(wholeWord);
        }

        /// <summary>
        /// Opens a goto dialog
        /// </summary>
        public void GoTo(Object sender, System.EventArgs e)
        {
            if (!this.gotoDialog.Visible) this.gotoDialog.Show();
            else this.gotoDialog.Activate();
        }

        /// <summary>
        /// Displays the next result
        /// </summary>
        public void FindNext(Object sender, System.EventArgs e)
        {
            Boolean update = !Globals.Settings.DisableFindTextUpdating;
            Boolean simple = !Globals.Settings.DisableSimpleQuickFind && !this.quickFind.Visible;
            this.frInDocDialog.FindNext(true, update, simple);
        }

        /// <summary>
        /// Displays the previous result
        /// </summary>
        public void FindPrevious(Object sender, System.EventArgs e)
        {
            Boolean update = !Globals.Settings.DisableFindTextUpdating;
            Boolean simple = !Globals.Settings.DisableSimpleQuickFind && !this.quickFind.Visible;
            this.frInDocDialog.FindNext(false, update, simple);
        }

        /// <summary>
        /// Opens a find and replace dialog
        /// </summary>
        public void FindAndReplace(Object sender, System.EventArgs e)
        {
            if (!this.frInDocDialog.Visible) this.frInDocDialog.Show();
            else this.frInDocDialog.Activate();
        }

        /// <summary>
        /// Opens a find and replace in files dialog
        /// </summary>
        public void FindAndReplaceInFiles(Object sender, System.EventArgs e)
        {
            if (!this.frInFilesDialog.Visible) this.frInFilesDialog.Show();
            else this.frInFilesDialog.Activate();
        }

        /// <summary>
        /// Opens a find and replace in files dialog with a location
        /// </summary>
        public void FindAndReplaceInFilesFrom(Object sender, System.EventArgs e)
        {
            ToolStripItem button = (ToolStripItem)sender;
            String path = ((ItemData)button.Tag).Tag;
            if (!this.frInFilesDialog.Visible) this.frInFilesDialog.Show(); // Show first..
            else this.frInFilesDialog.Activate();
            this.frInFilesDialog.SetFindPath(path);
        }

        /// <summary>
        /// Shows the quick find control
        /// </summary>
        public void QuickFind(Object sender, System.EventArgs e)
        {
            this.quickFind.ShowControl();
        }
    }
}
