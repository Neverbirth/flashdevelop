﻿using System;
using System.Collections.Generic;
using System.Text;
using ASCompletion;
using ASCompletion.Context;
using XMLCompletion;
using ASCompletion.Model;
using ASCompletion.Completion;
using PluginCore;
using PluginCore.Controls;
using System.Text.RegularExpressions;
using System.IO;
using PluginCore.Helpers;

namespace AS3Context
{
    class MxmlComplete
    {
        static public bool IsDirty;
        static public Context context;
        static public MxmlFilterContext mxmlContext;

        #region shortcuts
        public static bool GotoDeclaration()
        {
            ScintillaNet.ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci == null) return false;
            if (sci.ConfigurationLanguage != "xml") return false;

            int pos = sci.CurrentPos;
            int style = sci.BaseStyleAt(pos);
            XMLContextTag ctag;
            if (style == 1)
            {
                int len = sci.TextLength;
                while (pos < len)
                {
                    char c = (char)sci.CharAt(pos);
                    if (c <= 32 || c == '/' || c == '>') break;
                    pos++;
                }
            }
            ctag = XMLComplete.GetXMLContextTag(sci, pos);
            if (ctag.Name == null) return true;
            pos = sci.CurrentPos;
            string word = sci.GetWordFromPosition(pos);
            if (word == null) return true;
            if (style != 1)
            {
                if (sci.WordEndPosition(pos, true) == pos) style = sci.BaseStyleAt(pos - 1);
            }
            else
                word = ctag.Name.Substring(ctag.Name.IndexOf(':') + 1);

            if (style == 6) return true; // TODO: attribute values

            string type = ResolveType(mxmlContext, ctag.Name);
            ClassModel model = context.ResolveType(type, mxmlContext.Model);

            bool isAttribute;
            if (model.IsVoid()) // try resolving tag as member of parent tag
            {
                parentTag = GetParentTag(ctag.Position, ctag.Closed || ctag.Closing);
                if (parentTag != null)
                {
                    type = ResolveType(mxmlContext, parentTag.Tag);
                    model = context.ResolveType(type, mxmlContext.Model);
                    if (model.IsVoid()) return true;
                }
                else return true;

                isAttribute = true;
            }
            else isAttribute = style == 3;

            if (isAttribute)
            {
                bool hasDot = false;
                if (style == 1)
                {
                    var attrParts = word.Split('.');
                    word = sci.GetWordFromPosition(pos);
                    if (attrParts.Length > 1 && word == attrParts[1]) hasDot = true;
                    else word = attrParts[0];
                } else 
                    while (pos >= 0)
                    {
                        char c = (char)sci.CharAt(pos);
                        if (c == '.')
                        {
                            hasDot = true;
                            break;
                        }
                        if (c <= 32 || c == '<' || c == ':') break;
                        pos--;
                    }
                if (hasDot) // it's a state modifier
                {
                    OpenDocumentToDeclaration(sci, new MxmlResult {State = word});
                }
                else
                {
                    MxmlResult found = ResolveAttribute(model, word);
                    OpenDocumentToDeclaration(sci, found);
                }
            }
            else
            {
                ASResult found = new ASResult();
                found.InFile = model.InFile;
                found.Type = model;
                ASComplete.OpenDocumentToDeclaration(sci, found);
            }
            return true;
        }

        private static void GetStateInfo(string src, int pos, string state, out int end)
        {
            end = -1;

            string attrName;
            do
            {
                attrName = MxmlFilter.GetAttributeName(src, ref pos);
                if (attrName == "name")
                {
                    string value = MxmlFilter.GetAttributeValue(src, ref pos);
                    if (value == null) continue;

                    if (value == state)
                    {
                        end = pos - 1;
                        return;
                    }
                }
                else if (attrName == "stateGroups")
                {
                    string value = MxmlFilter.GetAttributeValue(src, ref pos);
                    if (value == null) continue;

                    foreach (var stateGroup in value.Split(','))
                    {
                        var groupName = stateGroup.Trim();

                        if (groupName == state)
                        {
                            end = pos - 1;
                            return;
                        }
                    }
                }
            } while (attrName != null);
        }

        private static void OpenDocumentToDeclaration(ScintillaNet.ScintillaControl sci, MxmlResult found)
        {
            if (found == null) return;

            if (!string.IsNullOrEmpty(found.State))
            {
                int pos = sci.CurrentPos;
                string word = found.State;
                if (context.CurrentModel.OutOfDate) context.UpdateCurrentFile(false);
                MxmlContextBase ctx = mxmlContext.GetComponentContext(pos);
                if (ctx.States != null && ctx.States.Contains(word))
                {
                    MxmlInlineRange cr = ctx.Outline[0];
                    int stateLevel = ctx == mxmlContext ? cr.Level + 2 : cr.Level + 3;
                    string src = sci.Text;
                    var stack = new Stack<MxmlInlineRange>();
                    for (int i = 0, count = mxmlContext.Outline.Count; i < count; i++)
                    {
                        var o = mxmlContext.Outline[i];
                        if (o.Start < cr.Start) continue;
                        if (o.Start > cr.End) break;
                        if (stack.Count > 0 && stack.Peek().Level >= o.Level) stack.Pop();
                        if (o.Tag == "mx:State" || o.Tag == "s:State")
                        {
                            var nameSpace = string.Empty;
                            var nameSpaceIndex = ctx.BaseTag.IndexOf(':');
                            if (nameSpaceIndex > -1)
                                nameSpace = ctx.BaseTag.Substring(0, nameSpaceIndex + 1);
                            if (o.Level == stateLevel && stack.Peek().Tag == nameSpace + "states")
                            {
                                int end;
                                GetStateInfo(src, o.Start + o.Tag.Length + 1, word, out end);

                                if (end > -1)
                                {
                                    // For Back command
                                    int lookupLine = sci.LineFromPosition(sci.CurrentPos);
                                    int lookupCol = sci.CurrentPos - sci.PositionFromLine(lookupLine);
                                    ASContext.Panel.SetLastLookupPosition(ASContext.Context.CurrentFile, lookupLine, lookupCol);

                                    int start = end - sci.MBSafeTextLength(word);
                                    sci.EnsureVisible(sci.LineFromPosition(start));
                                    sci.SetSel(start, end);

                                    sci.Focus();

                                    break;
                                }
                            }
                        }
                        stack.Push(o);
                    }
                }
            }
            else if (found.MetaTag != null)
            {
                FileModel model = found.ASResult.InFile;
                if (model == null || model.FileName == "") return;

                // for Back command
                if (sci != null)
                {
                    int lookupLine = sci.LineFromPosition(sci.CurrentPos);
                    int lookupCol = sci.CurrentPos - sci.PositionFromLine(lookupLine);
                    ASContext.Panel.SetLastLookupPosition(ASContext.Context.CurrentFile, lookupLine, lookupCol);
                }

                if (model != ASContext.Context.CurrentModel)
                {
                    // We'll assume meta tags are just on one line and there are no more than one meta on one line...
                    if (model.FileName.Length > 0 && File.Exists(model.FileName))
                    {
                        ASContext.MainForm.OpenEditableDocument(model.FileName, false);
                        sci = ASContext.CurSciControl;
                        int i = found.MetaTag.LineFrom;
                        string line = sci.GetLine(i);
                        int start = sci.LineIndentPosition(i);
                        sci.EnsureVisible(i);
                        sci.SetSel(start, sci.PositionFromLine(i) + sci.MBSafeTextLength(line) - 1);
                    }
                    else
                    {
                        ASComplete.OpenVirtualFile(model);
                        sci = ASContext.CurSciControl;
                        string pattern = string.Format("\\[\\s*{0}\\s*\\(.*name\\s*=\\s*\"{1}\"", found.MetaTag.Kind == ASMetaKind.Style ? "Style" : "Event",
                            found.MetaTag.Params["name"]);
                        Regex re = new Regex(pattern);
                        for (int i = 0, count = sci.LineCount; i < count; i++)
                        {
                            string line = sci.GetLine(i);

                            Match m = re.Match(line);
                            if (m.Success)
                            {
                                int start = sci.LineIndentPosition(i);
                                sci.EnsureVisible(i);
                                sci.SetSel(start, sci.PositionFromLine(i) + sci.MBSafeTextLength(line) - 1);
                                break;
                            }
                        }

                    }
                }

            }
            else if (found.ASResult != null)
                ASComplete.OpenDocumentToDeclaration(sci, found.ASResult);
            
        }

        #endregion

        #region tag completion
        static internal XMLContextTag tagContext;
        static internal MxmlInlineRange parentTag;
        static private string tokenContext;
        static private string checksum;
        static private Dictionary<string, List<string>> allTags;
        //static private Regex reIncPath = new Regex("[\"']([^\"']+)", RegexOptions.Compiled);
        static private Regex reIncPath = new Regex("(\"|')([^\r\n]+)(\\1)", RegexOptions.Compiled);
        static private Dictionary<string, FileModel> includesCache = new Dictionary<string, FileModel>();

        /// <summary>
        /// Called 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static public bool HandleElement(object data)
        {
            if (!GetContext(data)) return false;

            if (!string.IsNullOrEmpty(tagContext.Name) && tagContext.Name.IndexOf(':') > 0)
                return HandleNamespace(data);

            List<ICompletionListItem> mix = new List<ICompletionListItem>();
            List<string> excludes = new List<string>();

            bool isContainer = AddParentAttributes(mix, excludes); // current tag attributes

            if (isContainer) // container children tag
                foreach (string ns in mxmlContext.Namespaces.Keys)
                {
                    string uri = mxmlContext.Namespaces[ns];
                    if (ns != "*") mix.Add(new NamespaceItem(ns, uri));

                    if (!allTags.ContainsKey(ns))
                        continue;
                    foreach (string tag in allTags[ns])
                    {
                        if (ns == "*") mix.Add(new HtmlTagItem(tag, tag));
                        else mix.Add(new HtmlTagItem(tag, ns + ":" + tag, uri));
                    }
                }

            // cleanup and show list
            mix.Sort(new MXMLListItemComparer());
            List<ICompletionListItem> items = new List<ICompletionListItem>();
            string previous = null;
            foreach (ICompletionListItem item in mix)
            {
                if (previous == item.Label) continue;
                previous = item.Label;
                if (excludes.Contains(previous)) continue;
                items.Add(item);
            }

            if (mix.Count == 0) return true;
            if (!string.IsNullOrEmpty(tagContext.Name)) CompletionList.Show(items, false, tagContext.Name);
            else CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
            return true;
        }

        private static bool AddParentAttributes(List<ICompletionListItem> mix, List<string> excludes)
        {
            bool isContainer = true;
            if (parentTag != null) // add parent tag members
            {
                if (tagContext.Closing) // closing tag, only show parent tag
                {
                    isContainer = false;
                    mix.Add(new HtmlTagItem(parentTag.Tag.Substring(parentTag.Tag.IndexOf(':') + 1), parentTag.Tag));
                }
                else
                {
                    string parentType = ResolveType(mxmlContext, parentTag.Tag);
                    ClassModel parentClass = context.ResolveType(parentType, mxmlContext.Model);
                    if (!parentClass.IsVoid())
                    {
                        parentClass.ResolveExtends();
                        int nsSeparator = parentTag.Tag.IndexOf(':');
                        isContainer = GetTagAttributes(parentClass, mix, excludes, nsSeparator > -1 ? 
                            parentTag.Tag.Substring(0, nsSeparator) : null);
                    }
                }
            }
            return isContainer;
        }

        static public bool HandleNamespace(object data)
        {
            if (!GetContext(data) || string.IsNullOrEmpty(tagContext.Name))
                return false;

            int p = tagContext.Name.IndexOf(':');
            if (p < 0) return false;
            string ns = tagContext.Name.Substring(0, p);
            if (!mxmlContext.Namespaces.ContainsKey(ns))
                return true;

            string uri = mxmlContext.Namespaces[ns];
            List<ICompletionListItem> mix = new List<ICompletionListItem>();
            List<string> excludes = new List<string>();

            bool isContainer = AddParentAttributes(mix, excludes); // current tag attributes

            if (isContainer && allTags.ContainsKey(ns)) // container children tags
                foreach (string tag in allTags[ns])
                    mix.Add(new HtmlTagItem(tag, ns + ":" + tag, uri));

            // cleanup and show list
            mix.Sort(new MXMLListItemComparer());
            List<ICompletionListItem> items = new List<ICompletionListItem>();
            string previous = null;
            foreach (ICompletionListItem item in mix)
            {
                if (previous == item.Label) continue;
                previous = item.Label;
                if (excludes.Contains(previous)) continue;
                items.Add(item);
            }

            if (mix.Count == 0) return true;
            CompletionList.Show(items, true, tagContext.Name ?? "");
            CompletionList.MinWordLength = 0;
            return true;
        }

        static public bool HandleElementClose(object data)
        {
            if (!GetContext(data)) return false;

            if (tagContext.Closing) return false;

            string type = ResolveType(mxmlContext, tagContext.Name);
            ScintillaNet.ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;

            if (type.StartsWith("mx.builtin.") || type.StartsWith("fx.builtin.")) // special tags
            {
                if (type.EndsWith(".Script"))
                {
                    string snip = "$(Boundary)\n\t<![CDATA[\n\t$(EntryPoint)\n\t]]>\n</" + tagContext.Name + ">";
                    SnippetHelper.InsertSnippetText(sci, sci.CurrentPos, snip);
                    return true;
                }
                if (type.EndsWith(".Style"))
                {
                    string snip = "$(Boundary)";
                    foreach (string ns in mxmlContext.Namespaces.Keys)
                    {
                        string uri = mxmlContext.Namespaces[ns];
                        if (ns != "fx")
                            snip += String.Format("\n\t@namespace {0} \"{1}\";", ns, uri);
                    }
                    snip += "\n\t$(EntryPoint)\n</" + tagContext.Name + ">";
                    SnippetHelper.InsertSnippetText(sci, sci.CurrentPos, snip);
                    return true;
                }
            }
            return false;
        }

        static public bool HandleAttribute(object data)
        {
            if (!GetContext(data)) return false;

            string type = ResolveType(mxmlContext, tagContext.Name);
            ClassModel tagClass = context.ResolveType(type, mxmlContext.Model);
            if (tagClass.IsVoid()) return true;

            var dotFound = false;
            var src = tagContext.Tag;
            for (var i = tagContext.Tag.Length - 1; i >= 0; i--)
            {
                char c = src[i];
                if (char.IsWhiteSpace(c)) break;

                if (c == '.')
                {
                    if (dotFound) return true;
                    dotFound = true;
                }
            }

            List<ICompletionListItem> items;
            if (!dotFound)
            {
                tagClass.ResolveExtends();

                List<ICompletionListItem> mix = new List<ICompletionListItem>();
                List<string> excludes = new List<string>();
                GetTagAttributes(tagClass, mix, excludes, null);

                // cleanup and show list
                mix.Sort(new MXMLListItemComparer());
                string previous = null;
                items = new List<ICompletionListItem>();
                foreach (ICompletionListItem item in mix)
                {
                    if (previous == item.Label) continue;
                    previous = item.Label;
                    if (excludes.Contains(previous)) continue;
                    items.Add(item);
                }
            }
            else
            {
                items = GetAutoCompletionValuesFromType("State");
                if (items == null || items.Count == 0) return true;
                items.Sort(new MXMLListItemComparer());
            }

            if (items.Count == 0) return true;
            if (!string.IsNullOrEmpty(tokenContext)) CompletionList.Show(items, false, tokenContext);
            else CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
            return true;
        }

        static internal void OnChar(ScintillaNet.ScintillaControl sci, int value)
        {
            if (value != '.') return;

            var xmlTag = XMLComplete.GetXMLContextTag(sci, sci.CurrentPos);

            var src = xmlTag.Tag;

            if (string.IsNullOrEmpty(src) || src.StartsWith("<!") || src.StartsWith("<?") || !src.StartsWith("<")) return;

            bool insideAttribute = false;
            for (int i = src.Length - 2; i >= 0; i--)
            {
                char c = src[i];
                if (Char.IsWhiteSpace(c)) break;
                if (c == '\'' || c == '"' || c == '<' || c == '>' || c == '&' || c == '/' || c == '.') // . is valid, but we don't want to autocomplete if there is already one
                {
                    insideAttribute = false;
                    break;
                }
                insideAttribute = true;
            }
            if (!insideAttribute) return;
            var items = GetAutoCompletionValuesFromType("State");
            if (items == null || items.Count == 0) return;
            items.Sort(new MXMLListItemComparer());
            CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
        }

        static public bool HandleAttributeValue(object data)
        {
            if (!GetContext(data)) return false;

            string type = ResolveType(mxmlContext, tagContext.Name);
            ClassModel tagClass = context.ResolveType(type, mxmlContext.Model);
            if (tagClass.IsVoid()) return true;
            tagClass.ResolveExtends();

            string currentAttribute;
            StringBuilder caBuilder = new StringBuilder();
            bool possibleStartFound = false, startFound = false;
            for (int i = tagContext.Tag.Length - 1; i >= 0; i--)
            {
                char currChar = tagContext.Tag[i];
                if (currChar == '=')
                {
                    possibleStartFound = true;
                }
                else if (startFound)
                {
                    if (Char.IsWhiteSpace(currChar))
                        break;

                    caBuilder.Insert(0, currChar);
                }
                else if (possibleStartFound && !Char.IsWhiteSpace(currChar))
                {
                    startFound = true;
                    caBuilder.Insert(0, currChar);
                }
            }

            currentAttribute = caBuilder.ToString();

            List<ICompletionListItem> mix = GetTagAttributeValues(tagClass, null, currentAttribute);

            if (mix == null || mix.Count == 0) return true;

            // cleanup and show list
            mix.Sort(new MXMLListItemComparer());
            List<ICompletionListItem> items = new List<ICompletionListItem>();
            string previous = null;
            foreach (ICompletionListItem item in mix)
            {
                if (previous == item.Label) continue;
                previous = item.Label;
                items.Add(item);
            }

            if (items.Count == 0) return true;
            if (!string.IsNullOrEmpty(tokenContext)) CompletionList.Show(items, false, tokenContext);
            else CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
            return true;
        }

        private static bool GetTagAttributes(ClassModel tagClass, List<ICompletionListItem> mix, List<string> excludes, string ns)
        {
            ClassModel curClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = tagClass;
            FlagType mask = FlagType.Variable | FlagType.Setter;
            Visibility acc = context.TypesAffinity(curClass, tmpClass);
            bool isContainer = false;

            if (tmpClass.InFile.Package != "mx.builtin" && tmpClass.InFile.Package != "fx.builtin")
                mix.Add(new HtmlAttributeItem("id", "String", null, ns));
            else isContainer = true;

            if (mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex4 || mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.FlexJs)
            {
                // NOTE: Not sure at this moment if FlexJS does support all of these properties. It does at least some of them.
                if (parentTag == null || parentTag.Tag == "fx:Component")
                    mix.Add(new HtmlAttributeItem("implements", "Interface", "http://ns.adobe.com/mxml/2009"));
                else if (tagContext.NameSpace != "fx")
                {
                    mix.Add(new HtmlAttributeItem("includeIn", "Array", "http://ns.adobe.com/mxml/2009"));
                    mix.Add(new HtmlAttributeItem("excludeFrom", "Array", "http://ns.adobe.com/mxml/2009"));
                    mix.Add(new HtmlAttributeItem("itemCreationPolicy", "String", "http://ns.adobe.com/mxml/2009"));
                    mix.Add(new HtmlAttributeItem("itemDestructionPolicy", "String", "http://ns.adobe.com/mxml/2009"));
                }
                else if (tagContext.Name == "fx:Vector")
                    mix.Add(new HtmlAttributeItem("type", "Class", "http://ns.adobe.com/mxml/2009"));
            }

            while (tmpClass != null && !tmpClass.IsVoid())
            {
                string className = tmpClass.Name;
                // look for containers
                if (!isContainer)
                {
                    if (tmpClass.Implements != null
                        && (tmpClass.Implements.Contains("IContainer") // Flex
                        || tmpClass.Implements.Contains("IVisualElementContainer")
                        || tmpClass.Implements.Contains("IFocusManagerContainer")
                        || tmpClass.Implements.Contains("IPopUpHost") // FlexJS
                        || tmpClass.Implements.Contains("IParent")))
                        isContainer = true;
                }

                foreach (MemberModel member in tmpClass.Members)
                    if ((member.Flags & FlagType.Dynamic) > 0 && (member.Flags & mask) > 0
                        && (member.Access & acc) > 0)
                    {
                        string mtype = member.Type;

                        if ((member.Flags & FlagType.Setter) > 0)
                        {
                            if (member.Parameters != null && member.Parameters.Count > 0)
                                mtype = member.Parameters[0].Type;
                            else mtype = null;
                        }
                        mix.Add(new HtmlAttributeItem(member.Name, mtype, className, ns));
                    }

                ExploreMetadatas(tmpClass, mix, excludes, ns, tagClass == tmpClass);

                tmpClass = tmpClass.Extends;
                if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                    break;
                // members visibility
                acc = context.TypesAffinity(curClass, tmpClass);
            }

            return isContainer;
        }

        private static List<ICompletionListItem> GetTagAttributeValues(ClassModel tagClass, string ns, string attribute)
        {
            ClassModel curClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = tagClass;
            FlagType mask = FlagType.Variable | FlagType.Setter | FlagType.Getter;
            Visibility acc = context.TypesAffinity(curClass, tmpClass);

            if (tmpClass.InFile.Package != "mx.builtin" && tmpClass.InFile.Package != "fx.builtin" && attribute == "id")
                return null;

            List<ICompletionListItem> retVal;
            if (GetAutoCompletionValuesFromSpecialAttribute(attribute, out retVal))
                return retVal;

            // Inspectable metadata should be appended to getter types, and according to latest guidelines, the attribute have both a getter and setter
            // However, if a component has just a setter I want to autocomplete it if possible, also, the getter or setter may be defined in a super class
            bool hasGetterSetter = false;
            List<ASMetaData> metas = null;
            string setterType = null;
            while (tmpClass != null && !tmpClass.IsVoid())
            {
                foreach (MemberModel member in tmpClass.Members)
                    if ((member.Flags & FlagType.Dynamic) > 0 && (member.Flags & mask) > 0
                        && (member.Access & acc) > 0)
                    {
                        if (member.Name == attribute)
                        {
                            string mtype = member.Type;

                            if ((member.Flags & FlagType.Setter) > 0)
                            {
                                if (member.Parameters != null && member.Parameters.Count > 0)
                                    mtype = member.Parameters[0].Type;
                                else mtype = null;

                                if (!hasGetterSetter)
                                {
                                    hasGetterSetter = true;
                                    setterType = mtype;
                                    continue;
                                }
                                return GetAutoCompletionValuesFromInspectable(mtype, metas);
                            }
                            else if ((member.Flags & FlagType.Getter) > 0)
                            {
                                if (!hasGetterSetter)
                                {
                                    hasGetterSetter = true;
                                    metas = member.MetaDatas;
                                    continue;
                                }
                                return GetAutoCompletionValuesFromInspectable(setterType, metas);
                            }

                            return GetAutoCompletionValuesFromType(mtype);
                        }

                    }

                if (GetAutoCompletionValuesFromMetaData(tmpClass, attribute, out retVal))
                    return retVal;

                tmpClass = tmpClass.Extends;
                if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                    break;
                // members visibility
                acc = context.TypesAffinity(curClass, tmpClass);
            }

            if (setterType != null)
                return GetAutoCompletionValuesFromType(setterType);

            return null;
        }

        private static List<ICompletionListItem> GetAutoCompletionValuesFromInspectable(string type, List<ASMetaData> metas)
        {
            if (metas == null || metas.Count == 0)
                return GetAutoCompletionValuesFromType(type);

            foreach (var meta in metas)
            {
                if (meta.Name != "Inspectable") continue;

                string enumValues = null;
                if (meta.Params.TryGetValue("enumeration", out enumValues))
                {
                    var retVal = new List<ICompletionListItem>();
                    foreach (string value in enumValues.Split(','))
                    {
                        var tValue = value.Trim();
                        if (tValue != string.Empty) retVal.Add(new HtmlAttributeItem(tValue));
                    }

                    if (retVal.Count > 0) return retVal;
                }
            }

            return GetAutoCompletionValuesFromType(type);
        }

        private static bool GetAutoCompletionValuesFromMetaData(ClassModel model, string attribute, out List<ICompletionListItem> result)
        {
            if (model != null && model.MetaDatas != null)
            {
                foreach (ASMetaData meta in model.MetaDatas)
                {
                    string name = null;
                    if (!meta.Params.TryGetValue("name", out name) || name != attribute) continue;

                    string type = null;
                    switch (meta.Kind)
                    {
                        case ASMetaKind.Event:
                            string eventType;
                            if (!meta.Params.TryGetValue("type", out eventType)) eventType = "flash.events.Event";
                            result = GetAutoCompletionValuesFromEventType(attribute, eventType);

                            return true;
                        case ASMetaKind.Style:
                            if (meta.Params != null) meta.Params.TryGetValue("type", out type);
                            break;
                        case ASMetaKind.Effect:
                            if (meta.Params != null) type = meta.Params["event"];
                            break;
                        case ASMetaKind.Exclude:
                            break;
                        case ASMetaKind.Include:    // TODO: Check this case...
                            System.Diagnostics.Debug.Assert(false, "Please, check this case");
                            FileModel incModel = ParseInclude(model.InFile, meta);
                            return GetAutoCompletionValuesFromMetaData(incModel.GetPublicClass(), attribute, out result);
                    }
                    if (meta.Params != null && meta.Params.ContainsKey("enumeration"))
                    {
                        var retVal = new List<ICompletionListItem>();
                        foreach (string value in meta.Params["enumeration"].Split(','))
                        {
                            var tValue = value.Trim();
                            if (tValue != string.Empty) retVal.Add(new HtmlAttributeItem(tValue));
                        }
                        result = retVal;

                        return true;
                    }

                    result = GetAutoCompletionValuesFromType(type);

                    return true;
                }
            }

            result = null;
            return false;
        }

        private static List<ICompletionListItem> GetAutoCompletionValuesFromEventType(string eventName, string type)
        {
            ClassModel inClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = inClass;
            ClassModel eventClass = context.ResolveType(type, mxmlContext.Model);
            Visibility acc = Visibility.Default | Visibility.Internal | Visibility.Private | Visibility.Protected | Visibility.Public;

            tmpClass.ResolveExtends();
            eventClass.ResolveExtends();

            var result = new List<ICompletionListItem>();
            var validTypes = new Dictionary<string, bool>();

            // Obtain attribute value
            // check if it's a complex expression
            //   if it is, leave event creation
            //   if it is not, check if it's an existent member
            MemberModel autoCompleteMember = null;

            while (tmpClass != null && !tmpClass.IsVoid())
            {
                foreach (MemberModel member in tmpClass.Members)
                    if ((member.Flags & FlagType.Function) > 0 && (member.Access & acc) > 0 && member.Parameters != null && member.Parameters.Count > 0)
                    {
                        bool validFunction = true;
                        var argType = member.Parameters[0].Type;
                        if (argType != type && argType != "Object" && argType != "*" && !validTypes.TryGetValue(argType, out validFunction))
                        {
                            ClassModel argClass = context.ResolveType(argType, tmpClass.InFile);
                            if (argClass.IsVoid())
                                validTypes[argType] = validFunction = false;
                            else
                            {
                                validTypes[argType] = validFunction = (context.TypesAffinity(eventClass, argClass) & Visibility.Protected) > 0;
                                if (argType != argClass.Type) validTypes[argClass.Type] = validFunction;
                            }
                        }

                        if (!validFunction) continue;

                        for (int i = 1, count = member.Parameters.Count; i < count; i++)
                        {
                            if (member.Parameters[i].Value != null)
                            {
                                validFunction = false;
                                break;
                            }
                        }

                        if (!validFunction) continue;

                        result.Add(new MxmlEventHandlerItem(member));
                    }

                tmpClass = tmpClass.Extends;
                if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                    break;
                // members visibility
                // TODO: Take into account namespaces!
                acc = Visibility.Protected | Visibility.Public;
            }

            bool addEventGeneration = true;
            if (addEventGeneration)
                MxmlGenerator.FillEventList(result, eventName, type.Substring(type.LastIndexOf(".") + 1), inClass);

            return result;
        }

        private static bool GetAutoCompletionValuesFromSpecialAttribute(string attribute, out List<ICompletionListItem> result)
        {
            if (mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex4 || mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.FlexJs)
            {
                if ((parentTag == null || parentTag.Tag == "fx:Component") && attribute == "implements")
                {
                    result = GetAutoCompletionValuesFromType("Interface");
                    return true;
                }

                if (tagContext.NameSpace != "fx")
                {
                    switch (attribute)
                    {
                        case "includeIn":
                        case "excludeFrom":
                            result = GetAutoCompletionValuesFromType("State");
                            return true;
                        case "itemCreationPolicy":
                            result = new List<ICompletionListItem>() { new HtmlAttributeItem("deferred"), new HtmlAttributeItem("immediate") };
                            return true;
                        case "itemDestructionPolicy":
                            result = new List<ICompletionListItem>() { new HtmlAttributeItem("auto"), new HtmlAttributeItem("never") };
                            return true;
                    }
                }
                else if (tagContext.Name == "fx:Vector" && attribute == "type")
                {
                    result = GetAutoCompletionValuesFromType("Class");
                    return true;
                }

            }

            result = null;
            return false;
        }

        private static List<ICompletionListItem> GetAutoCompletionValuesFromType(string type)
        {
            if (type == "Boolean")
            {
                return new List<ICompletionListItem>() 
                {
                    new HtmlAttributeItem("true"),
                    new HtmlAttributeItem("false")
                };
            }
            else if (type == "Class")
            {
                ASComplete.HandleAllClassesCompletion(PluginBase.MainForm.CurrentDocument.SciControl, tokenContext,
                                                      true, false);
            }
            else if (type == "Function")
            {
                ClassModel tmpClass = mxmlContext.Model.GetPublicClass();
                Visibility acc = Visibility.Default | Visibility.Internal | Visibility.Private | Visibility.Protected | Visibility.Public;

                tmpClass.ResolveExtends();

                List<ICompletionListItem> result = null;
                var validTypes = new Dictionary<string, bool>();
                while (tmpClass != null && !tmpClass.IsVoid())
                {
                    foreach (MemberModel member in tmpClass.Members)
                        if ((member.Flags & FlagType.Function) > 0 && (member.Access & acc) > 0)
                        {
                            if (result == null) result = new List<ICompletionListItem>();
                            result.Add(new MemberItem(member));
                        }

                    tmpClass = tmpClass.Extends;
                    if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                        break;
                    // members visibility
                    // TODO: Take into account namespaces!
                    acc = Visibility.Protected | Visibility.Public;
                }

                return result;
            }
            else if (type == "Interface")
            {
                MemberList known = ASContext.Context.GetAllProjectClasses();
                if (known.Count == 0) return null;

                string prev = null;
                List<ICompletionListItem> result = null;
                foreach (MemberModel member in known)
                {
                    if ((member.Flags & FlagType.Interface) == 0 || prev == member.Name)
                        continue;
                    prev = member.Name;
                    if (result == null) result = new List<ICompletionListItem>();
                    result.Add(new MemberItem(member));
                }
                return result;
            }
            else if (type == "State")
            {
                MxmlContextBase ctx = mxmlContext.GetComponentContext(ASContext.CurSciControl.CurrentPos);
                if (ctx.States != null)
                {
                    var result = new List<ICompletionListItem>(ctx.States.Count);
                    foreach (var state in ctx.States) result.Add(new HtmlAttributeItem(state));

                    return result;
                }
            }
            return null;
        }

        private static void ExploreMetadatas(ClassModel model, List<ICompletionListItem> mix, List<string> excludes, string ns, bool isCurrentModel)
        {
            if (model == null || model.MetaDatas == null)
                return;
            string className = model.IsVoid() ? Path.GetFileNameWithoutExtension(model.InFile.FileName) : model.Name;
            foreach (ASMetaData meta in model.MetaDatas)
            {
                string add = null;
                string type = null;
                switch (meta.Kind)
                {
                    case ASMetaKind.Event: add = ":e"; break;
                    case ASMetaKind.Style:
                        add = ":s";
                        if (meta.Params == null || !meta.Params.TryGetValue("type", out type)) type = "Object";
                        break;
                    case ASMetaKind.Effect:
                        add = ":x";
                        if (meta.Params != null) type = meta.Params["event"];
                        break;
                    case ASMetaKind.Exclude:
                        if (meta.Params != null) excludes.Add(meta.Params["name"]);
                        break;
                    case ASMetaKind.Include:
                        FileModel incModel = ParseInclude(model.InFile, meta);
                        ExploreMetadatas(incModel.GetPublicClass(), mix, excludes, ns, isCurrentModel);
                        break;
                }
                if (add != null && meta.Params.ContainsKey("name"))
                    mix.Add(new HtmlAttributeItem(meta.Params["name"] + add, type, className, ns));
            }
        }

        private static FileModel ParseInclude(FileModel fileModel, ASMetaData meta)
        {
            Match m = reIncPath.Match(meta.RawParams);
            if (m.Success)
            {
                string path = m.Groups[2].Value;
                if (path.Length == 0) return null;

                // retrieve from cache
                if (includesCache.ContainsKey(path))
                    return includesCache[path];

                // relative path?
                string fileName = path;
                if (!Path.IsPathRooted(fileName))
                {
                    if (fileName[0] == '/' || fileName[0] == '\\')
                        fileName = Path.Combine(fileModel.BasePath, fileName);
                    else
                        fileName = Path.Combine(Path.GetDirectoryName(fileModel.FileName), fileName);
                }

                // parse & cache
                if (!File.Exists(fileName)) return null;
                string src = File.ReadAllText(fileName);
                if (src.IndexOf("package") < 0) src = "package {" + src + "}";
                ASFileParser parser = new ASFileParser();
                FileModel model = new FileModel(path);
                parser.ParseSrc(model, src);

                includesCache[path] = model;
                return model;
            }
            return null;
        }
        #endregion

        #region context detection
        private static bool GetContext(object data)
        {
            if (mxmlContext == null || mxmlContext.Model == null)
                return false;

            ScintillaNet.ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci == null) return false;

            // XmlComplete context
            try
            {
                if (data is XMLContextTag)
                    tagContext = (XMLContextTag)data;
                else
                {
                    object[] o = (object[])data;
                    tagContext = (XMLContextTag)o[0];
                    tokenContext = (string)o[1];
                }
            }
            catch
            {
                return false;
            }

            // more context
            parentTag = GetParentTag(tagContext.Position, false);

            // rebuild tags cache?
            string sum = "" + context.GetAllProjectClasses().Count;
            foreach (string uri in mxmlContext.Namespaces.Values)
                sum += uri;
            if (IsDirty || sum != checksum)
            {
                checksum = sum;
                GetAllTags();
            }
            return true;
        }

        private static MxmlInlineRange GetParentTag(int basePos, bool shouldRecheck)
        {
            MxmlInlineRange retVal = null;
            foreach (var m in mxmlContext.Outline)
            {
                if (m.Start < basePos && m.End > basePos)
                {
                    retVal = m;
                }
                else if (m.Start >= basePos)
                {
                    if (shouldRecheck && retVal != null)
                        return GetParentTag(retVal.Start, false);
                    break;
                }
            }

            return retVal;
        }
        #endregion

        #region tag resolution
        private static void GetAllTags()
        {
            Dictionary<string, string> nss = mxmlContext.Namespaces;
            MemberList allClasses = context.GetAllProjectClasses();
            Dictionary<string, string> packages = new Dictionary<string, string>();
            allTags = new Dictionary<string, List<string>>();

            foreach (string key in nss.Keys)
            {
                string uri = nss[key];
                if (uri.EndsWith(".*"))
                    packages[uri.Substring(0, uri.LastIndexOf('.') + 1)] = key;
                else if (uri == "*")
                    packages["*"] = key;
            }

            foreach (MemberModel model in allClasses)
            {
                if ((model.Flags & FlagType.Class) == 0 || (model.Flags & FlagType.Interface) != 0)
                    continue;
                int p = model.Type.IndexOf('.');
                string bns = p > 0 ? model.Type.Substring(0, p) : "";
                if (bns == "mx" || bns == "fx" || bns == "spark")
                    continue;

                p = model.Type.LastIndexOf('.');
                string pkg = model.Type.Substring(0, p + 1);
                if (pkg == "") pkg = "*";
                if (packages.ContainsKey(pkg))
                {
                    string ns = packages[pkg];
                    if (!allTags.ContainsKey(ns)) allTags.Add(ns, new List<string>());
                    allTags[ns].Add(model.Name.Substring(p + 1));
                }
            }

            foreach (MxmlCatalog cat in mxmlContext.Catalogs)
            {
                List<string> cls = allTags.ContainsKey(cat.NS) ? allTags[cat.NS] : new List<string>();
                cls.AddRange(cat.Keys);
                allTags[cat.NS] = cls;
            }
        }

        public static string ResolveType(MxmlFilterContext ctx, string tag)
        {
            if (tag == null || ctx == null) return "void";
            int p = tag.IndexOf(':');
            if (p < 0) return ResolveType(ctx, "*", tag);
            else return ResolveType(ctx, tag.Substring(0, p), tag.Substring(p + 1));
        }

        public static string ResolveType(MxmlFilterContext ctx, string ns, string name)
        {
            if (!ctx.Namespaces.ContainsKey(ns))
                return name;

            string uri = ctx.Namespaces[ns];
            if (uri == "*")
                return name;
            if (uri.EndsWith(".*"))
                return uri.Substring(0, uri.Length - 1) + name;

            if (uri == MxmlFilter.BETA_MX)
                uri = MxmlFilter.OLD_MX;

            foreach (MxmlCatalog cat in ctx.Catalogs)
            {
                if (cat.URI == uri && cat.ContainsKey(name))
                    return cat[name];
            }
            return name;
        }

        private static MxmlResult ResolveAttribute(ClassModel model, string word)
        {
            MxmlResult result = new MxmlResult();
            ClassModel curClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = model;
            Visibility acc = context.TypesAffinity(curClass, tmpClass);
            tmpClass.ResolveExtends();

            while (tmpClass != null && !tmpClass.IsVoid())
            {
                foreach (MemberModel member in tmpClass.Members)
                    if ((member.Flags & FlagType.Dynamic) > 0 && (member.Access & acc) > 0
                        && member.Name == word)
                    {
                        var asResult = new ASResult();
                        asResult.InFile = tmpClass.InFile;
                        if (member.LineFrom == 0) // cached model, reparse
                        {
                            asResult.InFile.OutOfDate = true;
                            asResult.InFile.Check();
                            if (asResult.InFile.Classes.Count > 0)
                            {
                                asResult.InClass = asResult.InFile.Classes[0];
                                asResult.Member = asResult.InClass.Members.Search(member.Name, member.Flags, 0);
                            }
                        }
                        else asResult.Member = member;
                        result.ASResult = asResult;
                        return result;
                    }

                // TODO includes
                if (tmpClass.MetaDatas != null)
                    foreach (var meta in tmpClass.MetaDatas)
                    {
                        string name;
                        if ((meta.Kind == ASMetaKind.Event || meta.Kind == ASMetaKind.Style) && meta.Params.TryGetValue("name", out name) && 
                            name == word)
                        {
                            var asResult = new ASResult { InFile = tmpClass.InFile };
                            if (meta.LineFrom == 0) // cached model, reparse
                            {
                                asResult.InFile.OutOfDate = true;
                                asResult.InFile.Check();
                                if (asResult.InFile.Classes.Count > 0)
                                {
                                    asResult.InClass = asResult.InFile.Classes[0];
                                    if (asResult.InClass.MetaDatas != null) 
                                        foreach (var m in asResult.InClass.MetaDatas)
                                            if (m.Kind == meta.Kind && meta.Params.TryGetValue("name", out name) && name == word) 
                                                result.MetaTag = m;
                                }
                            }
                            else
                            {
                                asResult.InClass = tmpClass;
                                result.MetaTag = meta;
                            }

                            result.ASResult = asResult;

                            return result;
                        }
                    }

                tmpClass = tmpClass.Extends;
                if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                    break;
                // members visibility
                acc = context.TypesAffinity(curClass, tmpClass);
            }
            return result;
        }
        #endregion
    }

    class MXMLListItemComparer : IComparer<ICompletionListItem>
    {

        public int Compare(ICompletionListItem a, ICompletionListItem b)
        {
            if (a is MxmlGeneratorItem)
            {
                if (!(b is MxmlGeneratorItem))
                {
                    return -1;
                }
            }
            else if (b is MxmlGeneratorItem)
            {
                return 1;
            }

            string a1;
            string b1;
            if (a.Label.Equals(b.Label, StringComparison.OrdinalIgnoreCase))
            {
                if (a is HtmlAttributeItem && b is HtmlTagItem) return 1;
                else if (b is HtmlAttributeItem && a is HtmlTagItem) return -1;
            }
            if (a is IHtmlCompletionListItem)
            {
                a1 = ((IHtmlCompletionListItem)a).Name;
                if (a.Icon == XMLComplete.StyleAttributeIcon) a1 += " 2";
                else if (a.Icon == XMLComplete.HtmlAttributeIcon) a1 += " 1";
                else a1 += " 0";
                if (a.Value.StartsWith("mx:")) a1 += "z"; // push down mx: tags
            }
            else a1 = a.Label;
            if (b is IHtmlCompletionListItem)
            {
                b1 = ((IHtmlCompletionListItem)b).Name;
                if (b.Icon == XMLComplete.StyleAttributeIcon) b1 += " 2";
                else if (b.Icon == XMLComplete.HtmlAttributeIcon) b1 += " 1";
                else b1 += " 0";
                if (b.Value.StartsWith("mx:")) b1 += "z"; // push down mx: tags
            }
            else b1 = b.Label;
            return string.Compare(a1, b1);
        }
         
    }

    class MxmlResult
    {

        public ASResult ASResult;
        public ASMetaData MetaTag;
        public string State;

    }

    #region completion list
    /// <summary>
    /// Event member completion list item
    /// </summary>
    public class MxmlEventHandlerItem : ICompletionListItem
    {
        private MemberModel member;
        private int icon;

        public MxmlEventHandlerItem(MemberModel oMember)
        {
            member = oMember;
            icon = PluginUI.GetIcon(member.Flags, member.Access);
        }

        public string Label
        {
            get { return member.FullName; }
        }

        public string Description
        {
            get
            {
                return ClassModel.MemberDeclaration(member) + ASDocumentation.GetTipDetails(member, null);
            }
        }

        public System.Drawing.Bitmap Icon
        {
            get { return (System.Drawing.Bitmap)ASContext.Panel.GetIcon(icon); }
        }

        public string Value
        {
            get
            {
                return member.Name + "(event)";
            }
        }
    }
    #endregion

}
