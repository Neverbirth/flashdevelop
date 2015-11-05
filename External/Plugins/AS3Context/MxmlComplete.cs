using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AS3Context.Utils;
using ASCompletion;
using ASCompletion.Completion;
using ASCompletion.Context;
using ASCompletion.Model;
using PluginCore;
using PluginCore.Controls;
using PluginCore.Helpers;
using ScintillaNet;
using XMLCompletion;

namespace AS3Context
{
    class MxmlComplete
    {
        internal const int BlankStyle = 0;
        internal const int TagStyle = 1;
        internal const int AttributeStyle = 3;
        internal const int AttributeValueStyle = 6;
        internal const int AttributeEqualStyle = 8;

        static protected internal readonly Regex re_lastDot =
            new Regex("\\.[^<]", RegexOptions.RightToLeft | RegexOptions.Compiled);

        static public bool IsDirty;
        static public Context context;
        static public MxmlFilterContext mxmlContext;

        //TODO: Remove with new completion list
        private static string attrFile;

        #region shortcuts
        public static bool GotoDeclaration()
        {
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci == null) return false;
            if (sci.ConfigurationLanguage != "xml" || !sci.ContainsFocus) return false;

            int pos = sci.CurrentPos;

            OpenDocumentToDeclaration(sci, GetExpression(sci, pos));

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

        public static MxmlResult GetExpression(ScintillaNet.ScintillaControl sci, int pos)
        {
            int style = sci.BaseStyleAt(pos);
            int i = pos;
            if (style == TagStyle)
            {
                int len = sci.TextLength;
                while (i < len)
                {
                    char c = (char)sci.CharAt(i);
                    if (c <= 32 || c == '/' || c == '>') break;
                    i++;
                }
            }
            XMLContextTag ctag = XMLComplete.GetXMLContextTag(sci, i);
            if (ctag.Name == null) return null;
            string word = sci.GetWordFromPosition(pos);
            if (word == null) return null;
            if (style != TagStyle)
            {
                if (sci.WordEndPosition(pos, true) == pos) style = sci.BaseStyleAt(pos - 1);
            }
            else
                word = ctag.Name.Substring(ctag.Name.IndexOf(':') + 1);

            if (style == AttributeValueStyle) return null; // TODO: attribute values

            string type = ResolveType(mxmlContext, ctag.Name);
            ClassModel model = context.ResolveType(type, mxmlContext.Model);

            bool isAttribute;
            if (model.IsVoid()) // try resolving tag as member of parent tag
            {
                parentTag = GetParentTag(sci.MBSafeCharPosition(ctag.Position), ctag.Closed || ctag.Closing);
                if (parentTag != null)
                {
                    type = ResolveType(mxmlContext, parentTag.Tag);
                    model = context.ResolveType(type, mxmlContext.Model);
                    if (model.IsVoid()) return null;
                }
                else return null;

                isAttribute = true;
            }
            else isAttribute = style == AttributeStyle;

            if (isAttribute)
            {
                bool hasDot = false;
                if (style == TagStyle)
                {
                    var attrParts = word.Split('.');
                    word = sci.GetWordFromPosition(pos);
                    if (attrParts.Length > 1 && word == attrParts[1]) hasDot = true;
                    else word = attrParts[0];
                }
                else
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
                    return new MxmlResult { State = word };
                }
                else
                {
                    return ResolveAttribute(model, word);
                }
            }
            else
            {
                ASResult found = new ASResult();
                found.InFile = model.InFile;
                found.Type = model;
                return new MxmlResult {ASResult = found};
            }
        }

        static public string GetToolTipText(MxmlResult expression)
        {
            string text = null;
            if (expression.MetaTag != null)
            {
                var inClass = expression.ASResult.InClass;
                text = "Meta " + expression.MetaTag.Name;
                if (inClass != ClassModel.VoidClass)
                    text += "\n[COLOR=#666666:MULTIPLY]in " + MemberModel.FormatType(inClass.QualifiedName) + "[/COLOR]";

                if (expression.MetaTag.Comments != null && ASCompletion.Context.ASContext.CommonSettings.SmartTipsEnabled)
                {
                    string details;
                    if (UITools.Manager.ShowDetails)
                    {
                        CommentBlock cb = ASDocumentation.ParseComment(expression.MetaTag.Comments);
                        details = ASDocumentation.GetTipFullDetails(cb, null);
                    }
                    else
                    {
                        CommentBlock cb = ASDocumentation.ParseComment(expression.MetaTag.Comments);
                        details = " \u2026" + ASDocumentation.GetTipShortDetails(cb, null);
                    }
                    text += details.TrimStart(new char[] { ' ', '\u2026' });
                }
            }
            else if (expression.ASResult != null)
            {
                var member = expression.ASResult.Member;
                if (member != null && member.Comments == null && (member.Flags & FlagType.Setter) > 0)
                {
                    // Hacky comment resolution
                    var flags = FlagType.Getter | FlagType.Setter;
                    var tmpClass = expression.ASResult.InClass;
                    string tmpComments = null;
                    string preComments = member.Comments;
                    tmpClass.ResolveExtends();

                    while (tmpClass != null && !tmpClass.IsVoid())
                    {
                        foreach (MemberModel cmember in tmpClass.Members)
                            if (cmember.Name == member.Name && (cmember.Flags & flags) > 0 && cmember.Comments != null)
                            {
                                tmpComments = cmember.Comments;
                                break;
                            }

                        if (tmpComments != null)
                            break;

                        tmpClass = tmpClass.Extends;
                        if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                            break;
                    }

                    member.Comments = tmpComments;
                    text = ASComplete.GetToolTipText(expression.ASResult);
                    member.Comments = preComments;
                }
                else
                {
                    text = ASComplete.GetToolTipText(expression.ASResult);
                }
            }
            else if (expression.State != null)
                text = "State \"" + expression.State + "\"";
            
            return text;
        }

        public static void OpenDocumentToDeclaration(ScintillaNet.ScintillaControl sci, MxmlResult found)
        {
            if (found == null) return;

            if (!string.IsNullOrEmpty(found.State))
            {
                int pos = sci.CurrentPos;
                string word = found.State;
                if (context.CurrentModel.OutOfDate) context.UpdateCurrentFile(false);
                MxmlContextBase ctx = mxmlContext.GetComponentContext(sci.MBSafeCharPosition(pos));
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
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            if (!GetContext(data)) return false;

            if (!string.IsNullOrEmpty(tagContext.Name) && tagContext.Name.IndexOf(':') > 0)
                return HandleNamespace(data);

            List<ICompletionListItem> mix = new List<ICompletionListItem>();
            List<string> excludes = new List<string>();

            string containedType = AddParentAttributes(mix, excludes); // current tag attributes

            if (containedType != null) // container children tag
                foreach (string ns in mxmlContext.Namespaces.Keys)
                {
                    string uri = mxmlContext.Namespaces[ns];
                    if (ns != "*") mix.Add(new NamespaceItem(ns, uri));

                    if (!allTags.ContainsKey(ns))
                        continue;
                    if (containedType == "Object")
                        foreach (string tag in allTags[ns])
                        {
                            if (ns == "*") mix.Add(new HtmlTagItem(tag, tag));
                            else mix.Add(new LazyMxmlTagItem(tag, ns, uri));
                        }
                    else
                    {
                        var containedTypeModel = context.ResolveType(containedType, mxmlContext.Model);
                        if ((containedTypeModel.Flags & FlagType.Final) > 0)
                            foreach (string tag in allTags[ns].Where(t => ResolveType(mxmlContext, ns, t) == containedTypeModel.QualifiedName))
                                mix.Add(new LazyMxmlTagItem(tag, ns, uri));
                        else
                            foreach (string tag in allTags[ns])
                            {
                                string cname = ResolveType(mxmlContext, ns, tag);
                                string package = string.Empty;
                                Match m = re_lastDot.Match(cname);
                                if (m.Success)
                                {
                                    package = cname.Substring(0, m.Index);
                                    cname = cname.Substring(m.Index + 1);
                                }
                                var model = context.GetModel(package, cname, string.Empty);
                                if (containedTypeModel.IsAssignableFrom(model))
                                    mix.Add(new MxmlTagItem(tag, model.Comments, (ns == "*" ? tag : ns + ":" + tag), uri));
                            }
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

        private static string AddParentAttributes(List<ICompletionListItem> mix, List<string> excludes)
        {
            string containedType = "Object";
            if (parentTag != null) // add parent tag members
            {
                if (tagContext.Closing) // closing tag, only show parent tag
                {
                    containedType = null;
                    mix.Add(new HtmlTagItem(parentTag.Tag.Substring(parentTag.Tag.IndexOf(':') + 1), parentTag.Tag));
                }
                else
                {
                    int nsSeparator = parentTag.Tag.IndexOf(':');
                    string parentNs = nsSeparator > -1 ? parentTag.Tag.Substring(0, nsSeparator) : null;
                    string parentName = nsSeparator > -1 ? parentTag.Tag.Substring(nsSeparator + 1) : parentTag.Tag;
                    string parentType = ResolveType(mxmlContext, parentNs ?? "*", parentName);
                    ClassModel parentClass = context.ResolveType(parentType, mxmlContext.Model);
                    if (!parentClass.IsVoid())
                    {
                        parentClass.ResolveExtends();
                        containedType = GetTagAttributes(parentClass, mix, excludes, parentNs);
                    }
                    else
                    {
                        var gfTag = GetParentTag(parentTag.Start, false);
                        if (gfTag != null)
                        {
                            var gfType = ResolveType(mxmlContext, gfTag.Tag);
                            var gfModel = context.ResolveType(gfType, mxmlContext.Model);
                            if (gfModel.IsVoid()) return null;

                            var result = ResolveAttribute(gfModel, parentName);
                            if (result.ASResult == null) return null;
                            if (result.ASResult.Member != null)
                                containedType = GetChildrenType(result.ASResult.Member, result.ASResult.InClass);
                            else if (result.MetaTag != null)
                            {
                                // TODO: Add
                                //GetAutoCompletionValuesFromMetaData(result.ASResult.InClass, )
                            }
                        }
                    }

                    if (containedType == "mx.core.IFactory")
                        mix.Add(mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex3
                                    ? new HtmlTagItem("Component", "mx:Component", MxmlFilter.OLD_MX)
                                    : new HtmlTagItem("Component", "fx:Component", MxmlFilter.NEW_MX)
                            );
                }
            }
            return containedType;
        }

        static public string GetChildrenType(MemberModel member, ClassModel ownerClass)
        {
            string type;
            if ((member.Flags & FlagType.Setter) > 0)
            {
                if (member.Parameters == null || member.Parameters.Count == 0)  // Wrong setter
                    return null;

                type = member.Parameters[0].Type;
            }
            else
                type = member.Type;

            string containedType = context.ResolveType(type, ownerClass.InFile).QualifiedName;
            if (containedType == "mx.core.IDeferredInstance" && member.MetaDatas != null)
            {
                foreach (var memberMeta in member.MetaDatas)
                {
                    if (memberMeta.Name == "InstanceType")
                    {
                        containedType = memberMeta.Params["*"];
                        break;
                    }
                }
            }

            if (containedType == "Array")
            {
                if (member.MetaDatas != null)
                    foreach (var memberMeta in member.MetaDatas)
                        if (memberMeta.Name == "ArrayElementType")
                        {
                            containedType = memberMeta.Params["*"];
                            break;
                        }

                if (containedType == "Array") containedType = "Object";
            }
            else if (containedType.StartsWith("Vector.<"))
            {
                containedType = containedType.Substring(8, containedType.Length - 9);
            }

            return containedType;
        }

        static public bool HandleNamespace(object data)
        {
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
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

            string containedType = AddParentAttributes(mix, excludes); // current tag attributes

            if (containedType != null && allTags.ContainsKey(ns)) // container children tags
            {
                if (containedType == "Object")
                    foreach (string tag in allTags[ns])
                        mix.Add(new LazyMxmlTagItem(tag, ns, uri));
                else
                {
                    var containedTypeModel = context.ResolveType(containedType, mxmlContext.Model);
                    if ((containedTypeModel.Flags & FlagType.Final) > 0)
                        foreach (string tag in allTags[ns].Where(t => ResolveType(mxmlContext, ns, t) == containedTypeModel.QualifiedName))
                            mix.Add(new LazyMxmlTagItem(tag, ns, uri));
                    else
                        foreach (string tag in allTags[ns])
                        {
                            string cname = ResolveType(mxmlContext, ns, tag);
                            string package = string.Empty;
                            Match m = re_lastDot.Match(cname);
                            if (m.Success)
                            {
                                package = cname.Substring(0, m.Index);
                                cname = cname.Substring(m.Index + 1);
                            }
                            var model = context.GetModel(package, cname, string.Empty);
                            if (containedTypeModel.IsAssignableFrom(model))
                                mix.Add(new MxmlTagItem(tag, model.Comments, ns + ":" + tag, uri));
                        }
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
            CompletionList.Show(items, true, tagContext.Name ?? "");
            CompletionList.MinWordLength = 0;
            return true;
        }

        static public bool HandleElementClose(object data)
        {
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            if (!GetContext(data)) return false;

            if (tagContext.Closing) return false;

            string type = ResolveType(mxmlContext, tagContext.Name);
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;

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
                            snip += string.Format("\n\t@namespace {0} \"{1}\";", ns, uri);
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
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            if (!GetContext(data)) return false;

            string type = ResolveType(mxmlContext, tagContext.Name);
            ClassModel tagClass = context.ResolveType(type, mxmlContext.Model);
            if (tagClass.IsVoid()) return true;

            var dotFound = false;
            var src = tagContext.Tag;
            for (var i = src.Length - 1; i >= 0; i--)
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
                if (tagContext.Closing) return true;

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
            CompletionList.OnInsert += CompletionList_OnAttributeInsert;
            // TODO: Replace with hidden event
            CompletionList.OnCancel += CompletionList_OnAttributeInsert;
            attrFile = PluginBase.MainForm.CurrentDocument.FileName;
            if (!string.IsNullOrEmpty(tokenContext)) CompletionList.Show(items, false, tokenContext);
            else CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
            return true;
        }

        static private void CompletionList_OnAttributeInsert(ScintillaNet.ScintillaControl sender, int position, string text, char trigger, ICompletionListItem item)
        {
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            if (trigger == '\n' && mxmlContext != null && mxmlContext.Model != null && sender.ConfigurationLanguage == "xml" && PluginBase.MainForm.CurrentDocument.FileName == attrFile)
            {
                int pos = position + sender.MBSafeTextLength(text);

                int len = sender.Length;
                int i = pos;
                // 0: Initial, 1: Space, 2: =, 3: =", 4: Wrong
                int state = 0;
                while (i < len)
                {
                    char c = (char)sender.CharAt(i);
                    if (char.IsWhiteSpace(c))
                    {
                        if (state == 0)
                            state = 1;
                    }
                    else if (c == '=')
                    {
                        if (state < 2)
                        {
                            pos = i + 1;
                            state = 2;
                        }
                        else
                            break;
                    }
                    else if (c == '\"' || c == '\'')
                    {
                        if (state == 2)
                        {
                            pos = i + 1;
                            state = 3;
                        }
                        else if (state == 0)
                            state = 4;
                        break;
                    }
                    else if (c == '/' || c == '>')
                    {
                        break;
                    }
                    else
                    {
                        if (state == 0)
                            state = 4;
                        break;
                    }
                    i++;
                }

                if (state < 2)
                {
                    sender.InsertText(pos, "=\"\"");
                    pos += 2;
                    sender.SetSel(pos, pos);


                    i = position;
                    if (sender.CharAt(position - 1) == '.')
                        text = MxmlFilter.GetCurrentAttributeName(sender, ref i);

                    List<ICompletionListItem> items = GetAutoCompletionValuesFromAttribute(text, "");

                    if (items == null) return;

                    CompletionList.Show(items, true);
                    CompletionList.MinWordLength = 0;
                }
                else if (state != 4)
                {
                    if (state == 2)
                    {
                        sender.InsertText(pos, "\"");
                        pos++;
                    }
                    sender.SetSel(pos, pos);
                }
            }
        }

        static internal void OnChar(ScintillaNet.ScintillaControl sci, int value)
        {
            if (value != '.') return;

            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            int pos = sci.CurrentPos - 2;
            int style = sci.BaseStyleAt(pos);

            if (style == BlankStyle)
            {
                // Retry in case sci hasn't refreshed yet
                sci.Colourise(0, -1);
                style = sci.BaseStyleAt(pos);
            }

            if (style == AttributeStyle)
            {
                char c = (char)sci.CharAt(pos);
                while (!char.IsWhiteSpace(c))
                {
                    if (c == '.') return;

                    c = (char) sci.CharAt(--pos);
                }

                var items = GetAutoCompletionValuesFromType("State");
                if (items == null || items.Count == 0) return;
                items.Sort(new MXMLListItemComparer());
                CompletionList.OnInsert += CompletionList_OnAttributeInsert;
                attrFile = PluginBase.MainForm.CurrentDocument.FileName;
                CompletionList.OnCancel += CompletionList_OnAttributeInsert;
                CompletionList.Show(items, true);
                CompletionList.MinWordLength = 0;
            }
            else if (style == TagStyle)
            {
                var builder = new StringBuilder();
                char c = (char)sci.CharAt(--pos);
                while (!char.IsWhiteSpace(c) && c != '<' && c != '/')
                {
                    if (c == '.') return;

                    builder.Insert(0, c);

                    c = (char)sci.CharAt(--pos);
                }

                if (builder.Length == 0) return;

                var tag = builder.ToString();

                string type = ResolveType(mxmlContext, tag);
                ClassModel model = context.ResolveType(type, mxmlContext.Model);

                if (model.IsVoid()) // try resolving tag as member of parent tag
                {
                    parentTag = GetParentTag(sci.MBSafeCharPosition(pos), c == '/');
                    if (parentTag == null) return;
                    type = ResolveType(mxmlContext, parentTag.Tag);
                    model = context.ResolveType(type, mxmlContext.Model);
                    if (model.IsVoid()) return;
                }

                var items = GetAutoCompletionValuesFromType("State");
                if (items == null || items.Count == 0) return;
                items.Sort(new MXMLListItemComparer());
                CompletionList.Show(items, true);
                CompletionList.MinWordLength = 0;
            }
            else if (style == AttributeValueStyle)
            {
                var tag = XMLComplete.GetXMLContextTag(sci, pos);
                if (!tag.Tag.StartsWith("<") || tag.Tag.StartsWith("<!") || tag.Closing || tag.Closed || GetParentTag(sci.MBSafeCharPosition(sci.CurrentPos), true) != null) 
                    return;

                pos++;
                string attrValue;
                attrValue = MxmlFilter.GetCurrentAttributeValue(sci, ref pos);
                attrValue += ".";
                string currentAttribute = MxmlFilter.GetCurrentAttributeName(sci, ref pos);

                if (!currentAttribute.ToUpperInvariant().StartsWith("XMLNS:")) return;
                string[] steps = attrValue.Split('.');
                List<ICompletionListItem> mix = GetAutoCompletionValuesFromImports(steps);

                if (mix.Count == 0) return;

                if (mix.Count == MxmlFilter.GetCatalogs().Count)
                    tokenContext = attrValue;

                List<ICompletionListItem> items = new List<ICompletionListItem>();
                string previous = null;
                foreach (ICompletionListItem item in mix)
                {
                    if (previous == item.Label) continue;
                    previous = item.Label;
                    items.Add(item);
                }

                if (!string.IsNullOrEmpty(tokenContext)) CompletionList.Show(items, false, tokenContext);
                else CompletionList.Show(items, true);
                CompletionList.MinWordLength = 0;
            }

        }

        static public bool HandleAttributeValue(object data)
        {
            CompletionList.OnInsert -= CompletionList_OnAttributeInsert;
            CompletionList.OnCancel -= CompletionList_OnAttributeInsert;
            if (!GetContext(data)) return false;

            var sci = ASContext.CurSciControl;
            int pos = sci.CurrentPos;

            string attrValue = MxmlFilter.GetCurrentAttributeValue(sci, ref pos);
            string currentAttribute = MxmlFilter.GetCurrentAttributeName(sci, ref pos);

            List<ICompletionListItem> items = GetAutoCompletionValuesFromAttribute(currentAttribute, attrValue);

            if (items == null) return true;

            if (!string.IsNullOrEmpty(tokenContext)) CompletionList.Show(items, false, tokenContext);
            else CompletionList.Show(items, true);
            CompletionList.MinWordLength = 0;
            return true;
        }

        private static string GetTagAttributes(ClassModel tagClass, List<ICompletionListItem> mix, List<string> excludes, string ns)
        {
            ClassModel curClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = tagClass;
            FlagType mask = FlagType.Variable | FlagType.Setter | FlagType.Getter;
            Visibility acc = context.TypesAffinity(curClass, tmpClass);
            string containedType = null;

            // Check for special MXML ancestors to see if we only allow some particular type of children
            if (tmpClass.InFile.Package != "mx.builtin" && tmpClass.InFile.Package != "fx.builtin")
            {
                mix.Add(new HtmlAttributeItem("id", "String", null, ns));

                if (parentTag != null)
                {
                    if (parentTag.Tag == "fx:Vector")
                    {
                        int i = parentTag.Start + 10;
                        string attrName, attrValue;
                        string src = ASContext.CurSciControl.Text;

                        containedType = "Object";
                        do
                        {
                            attrName = MxmlFilter.GetAttributeName(src, ref i);
                            attrValue = MxmlFilter.GetAttributeValue(src, ref i);
                            if (attrName == "type")
                            {
                                if (attrValue != null) containedType = attrValue;
                                break;
                            }
                        } while (attrName != null);
                    }
                    else
                    {
                        var grandParentTag = GetParentTag(parentTag.Start, false);
                        var componentTag = mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex3 ? "mx:Component" : "fx:Component";
                        if (grandParentTag == null || grandParentTag.Tag == componentTag)
                            containedType = "Object";
                    }
                }
            }
            else containedType = "Object";

            // Add special attributes
            if (mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex4 || mxmlContext.DocumentType == MxmlFilterContext.FlexDocumentType.FlexJs)
            {
                // NOTE: Not sure at this moment if FlexJS does support all of these properties. It does at least some of them.
                if (parentTag == null || parentTag.Tag == "fx:Component")
                    mix.Add(new HtmlAttributeItem("implements", "Interface", "http://ns.adobe.com/mxml/2009"));
                else if (ns != null)
                {
                    if (ns != "fx" && parentTag.Level != 0)
                    {
                        mix.Add(new HtmlTagItem("includeIn", ns + ":includeIn", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlTagItem("excludeFrom", ns + ":excludeFrom", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlTagItem("itemCreationPolicy", ns + ":itemCreationPolicy", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlTagItem("itemDestructionPolicy", ns + ":itemDestructionPolicy", "http://ns.adobe.com/mxml/2009"));
                    }
                    else if (parentTag.Tag == "fx:Vector")
                        mix.Add(new HtmlTagItem("type", ns + ":type", "http://ns.adobe.com/mxml/2009"));
                }
                else
                {
                    if (tagContext.NameSpace != "fx")
                    {
                        mix.Add(new HtmlAttributeItem("includeIn", "Array", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlAttributeItem("excludeFrom", "Array", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlAttributeItem("itemCreationPolicy", "String", "http://ns.adobe.com/mxml/2009"));
                        mix.Add(new HtmlAttributeItem("itemDestructionPolicy", "String", "http://ns.adobe.com/mxml/2009"));
                    }
                    else if (tagContext.Name == "fx:Vector")
                        mix.Add(new HtmlAttributeItem("type", "Class", "http://ns.adobe.com/mxml/2009"));
                }
            }

            string defaultProperty = null;
            var propertyComments = new Dictionary<string, MxmlAttributeComment>();
            while (tmpClass != null && !tmpClass.IsVoid())
            {
                string className = tmpClass.Name;
                // look for containers
                if (containedType == null)
                {
                    if (tmpClass.Implements != null
                        && (tmpClass.Implements.Contains("IContainer") // Flex
                        || tmpClass.Implements.Contains("IVisualElementContainer")
                        || tmpClass.Implements.Contains("IFocusManagerContainer")
                        || tmpClass.Implements.Contains("IPopUpHost") // FlexJS
                        || tmpClass.Implements.Contains("IParent")))
                        containedType = "Object";
                    else
                    {
                        if (defaultProperty == null)
                        {
                            if (tmpClass.MetaDatas != null)
                                foreach (var meta in tmpClass.MetaDatas)
                                    if (meta.Kind == ASMetaKind.DefaultProperty)
                                    {
                                        defaultProperty = meta.Params["*"];
                                        break;
                                    }
                        }
                        if (defaultProperty != null)
                        {
                            FlagType flags = FlagType.Setter | FlagType.Variable;
                            foreach (var member in tmpClass.Members.Items)
                                if (member.Name == defaultProperty && (member.Flags & flags) > 0)
                                {
                                    containedType = GetChildrenType(member, tmpClass);

                                    break;
                                }
                        }
                    }
                }

                // Get class members
                foreach (MemberModel member in tmpClass.Members)
                {
                    if ((member.Flags & FlagType.Dynamic) > 0 && (member.Access & acc) > 0 && (member.Flags & mask) > 0)
                    {
                        // We want to get the documentation, to make things harder the standard is to decorate getters although it's not mandatory, in fact, some framework types do not follow it.
                        // This makes sense for normal AS completion tho. NOTE: The current normal AS completion misses documentation if the setter is the one with it
                        if ((member.Flags & FlagType.Setter) > 0)
                        {
                            MxmlAttributeComment attrc;
                            if (propertyComments.TryGetValue(member.Name, out attrc))
                            {
                                if (attrc.Setter == null)
                                {
                                    attrc.Setter = member;
                                    attrc.ClassName = className;
                                }
                            }
                            else
                            {
                                propertyComments[member.Name] = new MxmlAttributeComment { Setter = member, ClassName = className };
                            }
                        }
                        else if ((member.Flags & FlagType.Getter) > 0 && !string.IsNullOrEmpty(member.Comments))
                        {
                            MxmlAttributeComment attrc;
                            if (propertyComments.TryGetValue(member.Name, out attrc))
                            {
                                if (attrc.Comment == null) attrc.Comment = member.Comments;
                            }
                            else
                            {
                                propertyComments[member.Name] = new MxmlAttributeComment {Comment = member.Comments};
                            }
                        }
                        else
                        {
                            string mtype = member.Type;
                            mix.Add(new MxmlAttributeItem(member.Name, member.Comments, mtype, className, ns));
                        }
                    }
                }

                // Get available Metatags
                ExploreMetadatas(tmpClass, mix, excludes, ns, tagClass == tmpClass);

                tmpClass = tmpClass.Extends;
                if (tmpClass != null && tmpClass.InFile.Package == "" && tmpClass.Name == "Object")
                    break;
                // members visibility
                acc = context.TypesAffinity(curClass, tmpClass);
            }

            foreach (var attrc in propertyComments.Values)
            {
                if (attrc.Setter == null) continue;
                MemberModel member = attrc.Setter;
                string mtype;
                if (member.Parameters != null && member.Parameters.Count > 0)
                    mtype = member.Parameters[0].Type;
                else mtype = null;
                mix.Add(new MxmlAttributeItem(member.Name, attrc.Comment ?? member.Comments, mtype, attrc.ClassName, ns));
            }

            return containedType;
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

                                if (!hasGetterSetter && (member.MetaDatas == null || member.MetaDatas.Count == 0))
                                {
                                    hasGetterSetter = true;
                                    setterType = mtype;
                                    continue;
                                }
                                return GetAutoCompletionValuesFromInspectable(mtype, metas ?? member.MetaDatas);
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

        private static List<ICompletionListItem> GetAutoCompletionValuesFromAttribute(string attribute, string currentValue)
        {
            List<ICompletionListItem> mix;

            if (parentTag == null && attribute.ToUpperInvariant().StartsWith("XMLNS:"))
            {
                string[] steps = currentValue.Split('.');

                mix = GetAutoCompletionValuesFromImports(steps);

                if (mix.Count == 0) return null;

                if (mix.Count == MxmlFilter.GetCatalogs().Count || steps.Length == 1)
                    tokenContext = currentValue;
            }
            else
            {
                string type = ResolveType(mxmlContext, tagContext.Name);
                ClassModel tagClass = context.ResolveType(type, mxmlContext.Model);
                if (tagClass.IsVoid()) return null;
                tagClass.ResolveExtends();

                mix = GetTagAttributeValues(tagClass, null, attribute.Split('.')[0]);
                if (mix == null || mix.Count == 0) return null;
                mix.Sort(new MXMLListItemComparer());
            }

            // cleanup and show list
            List<ICompletionListItem> items = new List<ICompletionListItem>();
            string previous = null;
            foreach (ICompletionListItem item in mix)
            {
                if (previous == item.Label) continue;
                previous = item.Label;
                items.Add(item);
            }

            return items;
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
                            Debug.Assert(false, "Please, check this case");
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
                var sci = ASContext.CurSciControl;
                MxmlContextBase ctx = mxmlContext.GetComponentContext(sci.MBSafeCharPosition(sci.CurrentPos));
                if (ctx.States != null)
                {
                    var result = new List<ICompletionListItem>(ctx.States.Count);
                    foreach (var state in ctx.States) result.Add(new HtmlAttributeItem(state));

                    return result;
                }
            }
            return null;
        }

        private static List<ICompletionListItem> GetAutoCompletionValuesFromImports(string[] steps)
        {
            var mix = new List<ICompletionListItem>();

            // Decided to add available namespaces always
            foreach (string ns in MxmlFilter.GetCatalogs().Keys)
                mix.Add(new MemberItem(new MemberModel(ns, ns, FlagType.Import, 0)));
            mix.Sort(new MXMLListItemComparer());

            MemberList elements = new MemberList();

            // root types & packages
            FileModel baseModel = context.ResolvePackage(null, false);
            MemberList baseElements = baseModel != null ? baseModel.Imports : null;

            // other classes in same package
            MemberList localElements = null;
            if (context.Features.hasPackages && context.CurrentClass.InFile.Package != "")
            {
                FileModel packageElements = context.ResolvePackage(context.CurrentClass.InFile.Package, false);

                if (packageElements != null)
                    localElements = packageElements.Imports;
            }
            for (int i = 0, count = steps.Length; i < count; i++)
            {
                var current = i != count - 1 ? steps[i] : string.Empty;
                //TODO: Improve with .NET 3.5
                if (baseElements == null && localElements == null) break;
                bool found = false;
                if (baseElements != null)
                    foreach (var import in baseElements.Items)
                    {
                        if ((import.Flags & FlagType.Package) > 0 && import.Name.StartsWith(current))
                        {
                            if (i != count - 1)
                            {
                                baseModel = context.ResolvePackage(import.FullName, false);
                                baseElements = baseModel != null ? baseModel.Imports : null;
                                localElements = null;
                                found = true;
                                break;
                            }
                            elements.Add(import);
                        }
                    }

                if (!found) baseElements = null;

                if (localElements != null)
                    foreach (var import in localElements.Items)
                    {
                        if ((import.Flags & FlagType.Package) > 0 && import.Name.StartsWith(current))
                        {
                            if (i != count - 1)
                            {
                                baseModel = context.ResolvePackage(import.FullName, false);
                                localElements = baseModel != null ? baseModel.Imports : null;
                                baseElements = null;
                                found = true;
                                break;
                            }
                            elements.Add(import);
                        }
                    }

                if (!found) localElements = null;
            }

            if (elements.Count > 0)
            {
                elements.Sort();
                foreach (var import in elements.Items)
                    mix.Add(new MemberItem(import));
            }

            return mix;
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
                    mix.Add(new MxmlAttributeItem(meta.Params["name"] + add, meta.Comments, type, className, ns));
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

        #region Helper Classes
        private class MxmlAttributeComment
        {
            public string Comment;
            public MemberModel Setter;
            public string ClassName;
        }
        #endregion

        #endregion

        #region context detection
        private static bool GetContext(object data)
        {
            if (mxmlContext == null || mxmlContext.Model == null)
                return false;

            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
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
            parentTag = GetParentTag(sci.MBSafeCharPosition(tagContext.Position), false);

            // rebuild tags cache?
            // NOTE: This cache misses cases like new class names, or having the same amount of classes even if they are different. Could we use some update timestamp?
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

        internal static MxmlInlineRange GetParentTag(int basePos, bool shouldRecheck)
        {
            MxmlInlineRange retVal = null;
            foreach (var m in mxmlContext.Outline)
            {
                if (m.Start < basePos && (m.End > basePos || m.End == -1))
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
            Dictionary<string, MxmlCatalog> allCatalogs = MxmlFilter.GetCatalogs();
            MemberList allClasses = context.GetAllProjectClasses();
            Dictionary<string, string> packages = new Dictionary<string, string>();
            Dictionary<string, MxmlCatalog> catalogs = new Dictionary<string, MxmlCatalog>();
            
            allTags = new Dictionary<string, List<string>>();

            foreach (var entry in nss)
            {
                string uri = entry.Value;
                MxmlCatalog cat;
                if (uri.EndsWith(".*"))
                    packages[uri.Substring(0, uri.LastIndexOf('.') + 1)] = entry.Key;
                else if (uri == "*")
                    packages["*"] = entry.Key;
                else if (allCatalogs.TryGetValue(uri, out cat))
                    catalogs[entry.Key] = cat;
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
                string ns;
                if (packages.TryGetValue(pkg, out ns))
                {
                    List<String> nsTypes;
                    if (!allTags.TryGetValue(ns, out nsTypes))
                    {
                        nsTypes = new List<string>();
                        allTags.Add(ns, nsTypes);
                    }
                    nsTypes.Add(model.Name.Substring(p + 1));
                }
            }

            foreach (var catEntry in catalogs)
            {
                List<string> cls;
                string ns = catEntry.Key;
                if (!allTags.TryGetValue(ns, out cls))
                {
                    cls = new List<string>();
                    allTags[ns] = cls;
                }
                cls.AddRange(catEntry.Value.Keys);
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
            string uri;
            if (!ctx.Namespaces.TryGetValue(ns, out uri) || uri == "*")
                return name;

            if (uri.EndsWith(".*"))
                return uri.Substring(0, uri.Length - 1) + name;

            MxmlCatalog cat;
            string type;
            if (MxmlFilter.GetCatalogs().TryGetValue(uri, out cat) && cat.TryGetValue(name, out type))
                return type;
            
            return name;
        }

        private static MxmlResult ResolveAttribute(ClassModel model, string word)
        {
            MxmlResult result = new MxmlResult();
            ClassModel curClass = mxmlContext.Model.GetPublicClass();
            ClassModel tmpClass = model;
            Visibility acc = context.TypesAffinity(curClass, tmpClass);
            FlagType flags = FlagType.Setter | FlagType.Variable;
            tmpClass.ResolveExtends();

            while (tmpClass != null && !tmpClass.IsVoid())
            {
                foreach (MemberModel member in tmpClass.Members)
                    if ((member.Flags & flags) > 0 && (member.Access & acc) > 0
                        && member.Name == word)
                    {
                        result.ASResult = new ASResult
                        {
                            InFile = tmpClass.InFile,
                            Member = member,
                            InClass = tmpClass
                        };
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
                            var asResult = new ASResult
                            {
                                InFile = tmpClass.InFile,
                                InClass = tmpClass
                            };

                            result.MetaTag = meta;
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

    public class MxmlTagItem : HtmlTagItem
    {

        protected string comments;
        private CommentBlock cb;

        public MxmlTagItem(string name, string comments, string tag, string uri) : base(name, tag, uri)
        {
            this.comments = comments;
        }

        public MxmlTagItem(string name, string comments, string tag) : base(name, tag)
        {
            this.comments = comments;
        }

        public override String Description
        {
            get
            {
                if (!ASContext.CommonSettings.SmartTipsEnabled || String.IsNullOrEmpty(comments)) return base.Description;
                if (cb == null) cb = ASDocumentation.ParseComment(comments);
                string tip = (UITools.Manager.ShowDetails) ? ASDocumentation.GetTipFullDetails(cb, null) : ASDocumentation.GetTipShortDetails(cb, null);
                // remove paragraphs from comments
                return base.Description + Environment.NewLine + ASDocumentation.RemoveHTMLTags(tip).Trim();
            }
        }
    }

    public class LazyMxmlTagItem : MxmlTagItem
    {

        protected string ns;
        private bool modelResolved;

        public LazyMxmlTagItem(string name, string ns, string uri)
            : base(name, null, ns == "*" ? name : ns + ":" + name, uri)
        {
            this.ns = ns;
        }

        public LazyMxmlTagItem(string name, string ns)
            : base(name, null, ns == "*" ? name : ns + ":" + name)
        {
            this.ns = ns;
        }

        public override String Description
        {
            get
            {
                if (ASContext.CommonSettings.SmartTipsEnabled && !modelResolved)
                {
                    string cname = MxmlComplete.ResolveType(MxmlComplete.mxmlContext, ns, Name);
                    string package = string.Empty;
                    Match m = MxmlComplete.re_lastDot.Match(cname);
                    if (m.Success)
                    {
                        package = cname.Substring(0, m.Index);
                        cname = cname.Substring(m.Index + 1);
                    }
                    var model = MxmlComplete.context.GetModel(package, cname, string.Empty);
                    comments = model.Comments;
                    modelResolved = true;
                }
                return base.Description;
            }
        }
    }

    public class MxmlAttributeItem : HtmlAttributeItem
    {

        protected string comments;
        private CommentBlock cb;

        public MxmlAttributeItem(string name, string comments, string type, string className, string ns) : base(name, type, className, ns)
        {
            this.comments = comments;
        }

        public MxmlAttributeItem(string name, string comments, string type, string className) : base(name, type, className)
        {
            this.comments = comments;
        }

        public MxmlAttributeItem(string name, string comments) : base(name)
        {
            this.comments = comments;
        }

        public override String Description
        {
            get
            {
                if (!ASContext.CommonSettings.SmartTipsEnabled || String.IsNullOrEmpty(comments)) return base.Description;
                if (cb == null) cb = ASDocumentation.ParseComment(comments);
                string tip = (UITools.Manager.ShowDetails) ? ASDocumentation.GetTipFullDetails(cb, null) : ASDocumentation.GetTipShortDetails(cb, null);
                // remove paragraphs from comments
                return base.Description + Environment.NewLine + ASDocumentation.RemoveHTMLTags(tip).Trim();
            }
        }
    }

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

        public Bitmap Icon
        {
            get { return (Bitmap)ASContext.Panel.GetIcon(icon); }
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
