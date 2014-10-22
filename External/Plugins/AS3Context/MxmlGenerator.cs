using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ASCompletion;
using ASCompletion.Completion;
using ASCompletion.Context;
using ASCompletion.Model;
using ASCompletion.Settings;
using PluginCore;
using PluginCore.Controls;
using PluginCore.Helpers;
using PluginCore.Localization;
using XMLCompletion;

namespace AS3Context
{
    class MxmlGenerator
    {
        public static void ContextualGenerator(ScintillaNet.ScintillaControl Sci)
        {
            if (MxmlComplete.mxmlContext == null || MxmlComplete.mxmlContext.Model == null)
                return;

            ScintillaNet.ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci == null) return;

            var tagContext = XMLComplete.GetXMLContextTag(sci, sci.CurrentPos);
            // more context
            var parentTag = XMLComplete.GetParentTag(sci, tagContext);

            if (parentTag.Name == null || parentTag.Name == "fx:Component")
            {
                List<ICompletionListItem> known = new List<ICompletionListItem>();
                string label = TextHelper.GetString("ASCompletion.Label.ImplementInterface");
                known.Add(new MxmlGeneratorItem(label, GeneratorJobType.ImplementInterface, MxmlComplete.context.CurrentClass, "flash.desktop.IFilePromise"));
                CompletionList.Show(known, false);
            }
        }

        public static void FillEventList(List<ICompletionListItem> listItems, string eventName, string eventType, ClassModel inClass)
        {
            string tmp = TextHelper.GetString("ASCompletion.Label.GenerateHandler");
            string labelEvent = String.Format(tmp, "Event");
            string labelContext = String.Format(tmp, eventType);
            string[] choices = (eventType != "Event") ?
                new string[] { labelContext, labelEvent } :
                new string[] { labelEvent };
            foreach (string choice in choices)
            {
                listItems.Add(new MxmlGeneratorItem(choice,
                    choice == labelContext ? GeneratorJobType.ComplexEvent : GeneratorJobType.BasicEvent,
                    inClass, new EventGenerationContext { EventName = eventName, EventType = eventType }));
            }
        }

        public static void GenerateJob(GeneratorJobType job, ClassModel inClass, string label, Object data)
        {
            var currPos = ASContext.CurSciControl.CurrentPos;
            var currLine = ASContext.CurSciControl.LineFromPosition(currPos);

            MxmlComplete.context.CurrentLine = currLine;
            if (MxmlComplete.context.CurrentModel.OutOfDate) MxmlComplete.context.UpdateCurrentFile(false);

            var context = MxmlComplete.mxmlContext;
            var sci = ASContext.CurSciControl;

            // TODO: Namespace may not be mx and fx
            string scriptTag, componentTag;
            if (context.DocumentType == MxmlFilterContext.FlexDocumentType.Flex3)
            {
                scriptTag = "mx:Script";
                componentTag = "mx:Component";
            }
            else
            {
                scriptTag = "fx:Script";
                componentTag = "fx:Component";
            }

            MxmlInlineRange owner = null;
            int ownerStart = 0, ownerEnd = -1;
            // We look for the owner component of the element triggering the generation
            for (int i = 0, count = context.Outline.Count; i < count; i++)
            {
                var element = context.Outline[i];
                if (element.LineFrom > currLine || element.Start > currPos)
                {
                    if (owner == null)
                    {
                        owner = context.Outline[0];
                        ownerEnd = context.Outline.Count - 1;
                        break;
                    }
                    if (element.Start > owner.End)
                    {
                        ownerEnd = i - 1;
                        break;
                    }
                    continue;
                }

                if (element.Tag == componentTag) // AFAIK nested components aren't allowed
                {
                    owner = element;
                    ownerStart = i + 2;
                }
                else if (owner != null && element.End > owner.End)
                {
                    ownerStart = 1;
                    owner = null;
                }
            }

            // We look for a suited script element. 
            // This for loop could be merged with the previous one, complexity would raise, but performance improve, it could be looked into later
            MxmlInlineRange scriptElement = null;
            MxmlInlineRange component = null;
            for (int i = ownerStart; i <= ownerEnd; i++)
            {
                var element = context.Outline[i];
                if (component != null && element.Start < component.End) continue;

                component = null;
                if (element.Tag == componentTag)
                {
                    component = element;
                }
                else if (element.Tag == scriptTag)
                {
                    int tmp = element.Start + scriptTag.Length;
                    if (element.End > tmp + 10)
                    {
                        bool fromSource = false;
                        string attribute;
                        while ((attribute = MxmlFilter.GetAttributeName(sci.Text, ref tmp)) != null)
                            if (attribute == "source") fromSource = true;

                        if (fromSource) continue;
                    }
                    scriptElement = element;
                    break;
                }
            }

            int memberLine;
            if (scriptElement == null)
            {
                string snip = String.Format("$(Boundary)\n<{0}>\n\t<![CDATA[\n\t\n\t]]>\n</{0}>\n", scriptTag);

                int pos = context.Outline[0].Start + 1;
                int c;

                while (true)
                {
                    c = sci.CharAt(pos);
                    if (c == '>' || c == '<') break;
                    pos++;
                }

                int prePos = sci.CurrentPos;
                int preLength = sci.TextLength;
                sci.CurrentPos = ++pos;
                sci.SetSel(pos, pos);
                memberLine = sci.LineFromPosition(pos) + 4;
                SnippetHelper.InsertSnippetText(sci, pos, snip);
                sci.CurrentPos = (prePos > pos) ? prePos + sci.TextLength - preLength : prePos;
                MxmlComplete.context.UpdateCurrentFile(false);
                context = MxmlComplete.mxmlContext;
            }
            else
            {
                for (int i = 0, count = context.As3Ranges.Count; i < count; i++)
                {
                    var range = context.As3Ranges[i];
                    if (range.Start < scriptElement.Start || range.End > scriptElement.End) continue;
                }

                int pos = scriptElement.End;
                char c;
                bool foundEndTag = false;
                while (pos > 0)
                {
                    c = (char)sci.CharAt(pos);
                    if (!char.IsWhiteSpace(c))
                    {
                        if (!foundEndTag)
                        {
                            if (c == '<') foundEndTag = true;
                        }
                        else
                            break;
                    }
                    pos--;
                }
                memberLine = sci.LineFromPosition(pos);
            }

            var member = new MemberModel("dummy", "void", FlagType.Function, Visibility.Protected);
            member.LineFrom = memberLine - 2;
            member.LineTo = memberLine - 1;

            switch (job)
            {
                case GeneratorJobType.BasicEvent:
                case GeneratorJobType.ComplexEvent:
                    var eventContext = (EventGenerationContext)data;
                    var dummyEvent = eventContext.EventName;
                    var dummyMatch = Regex.Match(dummyEvent, "(?<event>.+)");

                    string contextToken;
                    string target = null;

                    if (MxmlComplete.parentTag != null)
                    {
                        var tagName = MxmlComplete.tagContext.Name;
                        var classType = tagName.Substring(tagName.IndexOf(':'));
                        int classCount = 0;

                        MxmlInlineRange bestMatch = null;
                        for (int i = 1, count = context.Outline.Count; i < count; i++)
                        {
                            var element = context.Outline[i];
                            if (element.LineFrom > currLine || element.Start > currPos)
                                break;

                            if (element.Tag.EndsWith(classType))
                                classCount++;
                            else continue;

                            if ((element.LineTo >= currLine && element.End >= currPos) || element.End == -1)
                                bestMatch = element;
                        }

                        if (bestMatch != null)
                            target = bestMatch.Model != null && bestMatch.Model.Name != null ? 
                                bestMatch.Model.Name : ASGenerator.Camelize(classType.Substring(1)) + classCount.ToString();
                    }

                    switch (ASContext.CommonSettings.HandlerNamingConvention)
                    {
                        case HandlerNamingConventions.handleTargetEventName:
                            if (target == null) contextToken = "handle" + ASGenerator.Capitalize(eventContext.EventName);
                            else contextToken = "handle" + ASGenerator.Capitalize(target) + ASGenerator.Capitalize(eventContext.EventName);
                            break;
                        case HandlerNamingConventions.onTargetEventName:
                            if (target == null) contextToken = "on" + ASGenerator.Capitalize(eventContext.EventName);
                            else contextToken = "on" + ASGenerator.Capitalize(target) + ASGenerator.Capitalize(eventContext.EventName);
                            break;
                        case HandlerNamingConventions.target_eventNameHandler:
                            if (target == null) contextToken = eventContext.EventName + "Handler";
                            else contextToken = target + "_" + eventContext.EventName + "Handler";
                            break;
                        default: //HandlerNamingConventions.target_eventName
                            if (target == null) contextToken = eventContext.EventName;
                            else contextToken = target + "_" + eventContext.EventName;
                            break;
                    }

                    if (ASContext.CommonSettings.MethodsGenerationLocations == MethodsGenerationLocations.AfterSimilarAccessorMethod)
                    {
                        // TODO: Find latest
                    }
                    
                    ASGenerator.SetJobContext(contextToken, eventContext.EventType, member, dummyMatch);
                    sci.ConfigurationLanguage = "as3";
                    int preLength = sci.TextLength;
                    ASGenerator.GenerateJob(job, member, inClass, label, null);
                    int newPos = member.LineTo < currLine ? currPos + sci.TextLength - preLength : currPos;

                    sci.SetSel(newPos, newPos);
                    sci.InsertText(newPos, contextToken + "(event)");
                    sci.SetSel(newPos, newPos + contextToken.Length);

                    sci.ConfigurationLanguage = "xml";

                    return;

                case GeneratorJobType.ImplementInterface:
                    ASGenerator.SetJobContext(null, data.ToString(), member, null);
                    sci.ConfigurationLanguage = "as3";
                    ASGenerator.GenerateJob(GeneratorJobType.ImplementInterface, member, MxmlComplete.context.CurrentClass,
                                            null, null);
                    break;
            }
        }

        private class EventGenerationContext
        {
            public string EventName;
            public string EventType;
        }
    }

    /// <summary>
    /// Generation completion list item
    /// </summary>
    class MxmlGeneratorItem : ICompletionListItem
    {
        private string label;
        private GeneratorJobType job;
        private ClassModel inClass;
        private Object data;

        public MxmlGeneratorItem(string label, GeneratorJobType job, ClassModel inClass)
        {
            this.label = label;
            this.job = job;
            this.inClass = inClass;
        }

        public MxmlGeneratorItem(string label, GeneratorJobType job, ClassModel inClass, Object data)
            : this(label, job, inClass)
        {

            this.data = data;
        }

        public string Label
        {
            get { return label; }
        }
        public string Description
        {
            get { return TextHelper.GetString("Info.GeneratorTemplate"); }
        }

        public System.Drawing.Bitmap Icon
        {
            get { return (System.Drawing.Bitmap)ASContext.Panel.GetIcon(PluginUI.ICON_DECLARATION); }
        }

        public string Value
        {
            get
            {
                MxmlGenerator.GenerateJob(job, inClass, label, data);
                return null;
            }
        }

        public Object Data
        {
            get
            {
                return data;
            }
        }
    }

}
