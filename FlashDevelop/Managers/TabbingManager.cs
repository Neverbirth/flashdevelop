using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlashDevelop.Docking;
using PluginCore;

namespace FlashDevelop.Managers
{
    class TabbingManager
    {
        public static Timer TabTimer;
        public static List<ITabbedDocument> TabHistory;
        public static Int32 SequentialIndex;

        static TabbingManager()
        {
            TabTimer = new Timer();
            TabTimer.Interval = 100;
            TabTimer.Tick += new EventHandler(OnTabTimer);
            TabHistory = new List<ITabbedDocument>();
            SequentialIndex = 0;
        }

        /// <summary>
        /// Checks to see if the Control key has been released
        /// </summary>
        private static void OnTabTimer(Object sender, EventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == 0)
            {
                TabTimer.Enabled = false;
                TabHistory.Remove(Globals.MainForm.CurrentDocument);
                TabHistory.Insert(0, Globals.MainForm.CurrentDocument);
            }
        }

        /// <summary>
        /// Sets an index of the current document
        /// </summary>
        public static void UpdateSequentialIndex(ITabbedDocument document)
        {
            ITabbedDocument[] documents = Globals.MainForm.Documents;
            Int32 count = documents.Length;
            for (Int32 i = 0; i < count; i++)
            {
                if (document == documents[i])
                {
                    SequentialIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// Activates next document in tabs
        /// </summary>
        public static void NavigateTabsSequentially(Int32 direction)
        {
            ITabbedDocument current = Globals.CurrentDocument;
            ITabbedDocument[] documents = Globals.MainForm.Documents;
            Int32 count = documents.Length; if (count <= 1) return;
            for (Int32 i = 0; i < count; i++)
            {
                if (documents[i] == current)
                {
                    if (direction > 0)
                    {
                        if (i < count - 1) documents[i + 1].Activate();
                        else documents[0].Activate();
                    }
                    else if (direction < 0)
                    {
                        if (i > 0) documents[i - 1].Activate();
                        else documents[count - 1].Activate();
                    }
                }
            }
        }

        /// <summary>
        /// Activates next document in tabs. Optionally only between documents in the same window as the current one
        /// </summary>
        public static void NavigateTabsSequentially(Int32 direction, Boolean fromDocumentGroup)
        {
            ITabbedDocument current = Globals.CurrentDocument;
            ITabbedDocument[] documents;
            if (!fromDocumentGroup) documents = Globals.MainForm.Documents;
            else
            {
                var parentForm = ((TabbedDocument)Globals.MainForm.CurrentDocument).Parent.FindForm();
                documents = Globals.MainForm.Documents.Where(x => ((TabbedDocument)x).Parent.FindForm() == parentForm).ToArray();
            }
            Int32 count = documents.Length; if (count <= 1) return;
            for (Int32 i = 0; i < count; i++)
            {
                if (documents[i] == current)
                {
                    if (direction > 0)
                    {
                        if (i < count - 1) documents[i + 1].Activate();
                        else documents[0].Activate();
                    }
                    else if (direction < 0)
                    {
                        if (i > 0) documents[i - 1].Activate();
                        else documents[count - 1].Activate();
                    }
                }
            }
        }

        /// <summary>
        /// Visual Studio style keyboard tab navigation: similar to Alt-Tab
        /// </summary>
        public static void NavigateTabHistory(Int32 direction)
        {
            NavigateTabHistory(direction, false);
        }

        /// <summary>
        /// Visual Studio style keyboard tab navigation: similar to Alt-Tab. Optionally only between documents in the same window as the current one
        /// </summary>
        public static void NavigateTabHistory(Int32 direction, Boolean fromDocumentGroup)
        {
            Int32 currentIndex = 0;
            List<ITabbedDocument> tabHistory = null;

            if (!fromDocumentGroup) tabHistory = TabHistory;
            else if (Globals.MainForm.CurrentDocument != null)
            {
                var parentForm = ((TabbedDocument) Globals.MainForm.CurrentDocument).Parent.FindForm();
                tabHistory = TabHistory.Where(x => ((TabbedDocument) x).Parent.FindForm() == parentForm).ToList();
            }

            if (tabHistory == null || tabHistory.Count < 1) return;

            if (direction != 0)
            {
                currentIndex = tabHistory.IndexOf(Globals.MainForm.CurrentDocument);
                currentIndex = (currentIndex + direction) % tabHistory.Count;
                if (currentIndex == -1) currentIndex = TabHistory.Count - 1;
            }
            tabHistory[currentIndex].Activate();
        }

    }

}
