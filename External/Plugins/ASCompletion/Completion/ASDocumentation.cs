using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using ASCompletion.Context;
using ASCompletion.Model;
using CommonMark;
using PluginCore;
using PluginCore.Controls;
using PluginCore.Localization;
using PluginCore.Managers;
using PluginCore.Utilities;
using ScintillaNet;
using System.Text;

namespace ASCompletion.Completion
{
    public class CommentBlock
    {
        public string Description;
        public string InfoTip;
        public string Return;
        public bool IsFunctionWithArguments;
        public List<string> ParamName;
        public List<string> ParamDesc;
        public List<string> TagName;
        public List<string> TagDesc;
    }
    
    public class ASDocumentation
    {
        static private List<ICompletionListItem> docVariables;
        
        #region regular_expressions
        static private Regex re_tags = new Regex("<[/]?(p|br)[/]?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion
        
        #region Comment generation
        static ASDocumentation()
        {
            CommonMarkSettings.Default.AdditionalFeatures = CommonMarkAdditionalFeatures.All;
        }

        static public bool OnChar(ScintillaControl Sci, int Value, int position, int style)
        {
            if (style == 3 || style == 124)
            {
                switch (Value)
                {
                    // documentation tag
                    case '@': return HandleDocTagCompletion(Sci);
                    
                    // documentation bloc
                    case '*': return ASContext.Context.DocumentationGenerator.ContextualGenerator(Sci, position, new List<ICompletionListItem>());
                }
            }
            return false;
        }
        
        static private bool HandleDocTagCompletion(ScintillaControl Sci)
        {
            if (ASContext.CommonSettings.JavadocTags == null || ASContext.CommonSettings.JavadocTags.Length == 0)
                return false;

            string txt = Sci.GetLine(Sci.CurrentLine).TrimStart();
            if (!Regex.IsMatch(txt, "^\\*[\\s]*\\@"))
                return false;
            
            // build tag list
            if (docVariables == null)
            {
                docVariables = new List<ICompletionListItem>();
                foreach (string tag in ASContext.CommonSettings.JavadocTags)
                {
                    docVariables.Add(new TagItem(tag));
                }               
            }
            
            // show
            CompletionList.Show(docVariables, true, "");
            return true;
        }
        
        /// <summary>
        /// Documentation tag template completion list item
        /// </summary>
        private class TagItem : ICompletionListItem
        {
            private string label;
            
            public TagItem(string label) 
            {
                this.label = label;
            }
            
            public string Label { 
                get { return label; }
            }
            public string Description {
                get { return TextHelper.GetString("Label.DocTagTemplate"); }
            }
            
            public Bitmap Icon {
                get { return (Bitmap)ASContext.Panel.GetIcon(PluginUI.ICON_DECLARATION); }
            }
            
            public string Value { 
                get { return label; }
            }
        }
        #endregion

        #region Tooltips

        static private Regex reNewLine = new Regex("(?:\r\n|\r|\n)", RegexOptions.Compiled);
        static private Regex reDocTags = new Regex(@"^\s*@(?<tag>[a-z]+)(?:\s|$|<\/p>)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static private Regex reHtmlTags = new Regex(@"<[/]?\w+[^>]*>");
        static private Regex reSplitParams = new Regex("(?<var>[\\w$]+)\\s", RegexOptions.Compiled);

        static public CommentBlock ParseComment(string comment)
        {
            // check if comment comes from code, should we do this in ASFileParser? would say so, but for now this seems safe enough
            byte commentType = 0; // 0: html, 1: plain, 2: markdown
            if (comment.Length > 0 && char.IsWhiteSpace(comment[0]))
            {
                bool asdoc = false;
                for (int i = 1, count = comment.Length; i < count; i++)
                {
                    char c = comment[i];
                    if (!char.IsWhiteSpace(c))
                    {
                        asdoc = c == '*';
                        break;
                    }
                }

                if (asdoc)
                {
                    // If asdoc we are going to look for html tags, if we don't have we are going to insert linebreaks ourselves
                    if (!reHtmlTags.IsMatch(comment))
                    {
                        commentType = 1;

                        string[] lines = reNewLine.Split(comment);
                        StringBuilder cleanComment = new StringBuilder();
                    
                        foreach (string line in lines)
                        {
                            string temp = line.Trim();
                            if (cleanComment.Length > 0) cleanComment.Append(" <br/>\n").Append(temp);
                            else cleanComment.Append(temp);
                        }
                        comment = cleanComment.ToString();
                    }

                    comment = comment.Replace("*", "").Replace("~~", "*");
                }
                else
                {
                    commentType = 2;

                    string[] lines = reNewLine.Split(comment);
                    StringBuilder cleanComment = new StringBuilder();
                    foreach (string line in lines)
                    {
                        string temp = line.Trim();
                        if (cleanComment.Length > 0) cleanComment.Append('\n').Append(temp);
                        else cleanComment.Append(temp);
                    }
                    comment = CommonMarkConverter.Convert(cleanComment.ToString());
                }
            }

            // extraction
            CommentBlock cb = new CommentBlock();
            MatchCollection tags = reDocTags.Matches(comment);
            
            if (tags.Count == 0)
            {
                cb.Description = comment.Trim();
                return cb;
            }
            
            if (tags[0].Index > 0) cb.Description = comment.Substring(0, tags[0].Index).Trim();
            else cb.Description = "";
            cb.TagName = new List<string>();
            cb.TagDesc = new List<string>();
            
            Group gTag;
            for(int i=0; i<tags.Count; i++)
            {
                gTag = tags[i].Groups["tag"];
                string tag = gTag.Value;
                int start = gTag.Index+gTag.Length;
                int end = (i<tags.Count-1) ? tags[i+1].Index : comment.Length;
                string desc = comment.Substring(start, end-start).Trim();
                if (commentType == 1 && desc.EndsWith("<br/>")) desc = desc.Substring(0, desc.Length - 5);
                if (tag == "param")
                {
                    Match mParam = reSplitParams.Match(desc);
                    if (mParam.Success)
                    {
                        Group mVar = mParam.Groups["var"];
                        if (cb.ParamName == null) {
                            cb.ParamName = new List<string>();
                            cb.ParamDesc = new List<string>();
                        }
                        cb.ParamName.Add(mVar.Value);
                        cb.ParamDesc.Add(desc.Substring(mVar.Index + mVar.Length).TrimStart());
                    }
                }
                else if (tag == "return")
                {
                    cb.Return = desc;
                }
                else if (tag == "infotip")
                {
                    cb.InfoTip = desc;
                    if (cb.Description.Length == 0) cb.Description = cb.InfoTip;
                }
                cb.TagName.Add(tag);
                cb.TagDesc.Add(desc);
            }

            return cb;
        }

        static public string EscapeComment(string src)
        {
            return src.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
        
        static public string GetTipDetails(MemberModel member, string highlightParam)
        {
            try
            {
                string tip = (UITools.Manager.ShowDetails) ? GetTipFullDetails(member, highlightParam) : GetTipShortDetails(member, highlightParam);
                // remove paragraphs from comments
                return tip;
            }
            catch(Exception ex)
            {
                ErrorManager.ShowError(/*"Error while parsing comments.\n"+ex.Message,*/ ex);
                return "";
            }
        }

        static public string RemoveHTMLTags(string tip)
        {
            return re_tags.Replace(tip, "");
        }
        
        /// <summary>
        /// Short contextual details to display in tips
        /// </summary>
        /// <param name="member">Member data</param>
        /// <param name="highlightParam">Parameter to detail</param>
        /// <returns></returns>
        static public string GetTipShortDetails(MemberModel member, string highlightParam)
        {
            if (member == null || member.Comments == null || !ASContext.CommonSettings.SmartTipsEnabled) return "";
            CommentBlock cb = ParseComment(member.Comments);
            cb.IsFunctionWithArguments = IsFunctionWithArguments(member);
            return GetTipShortDetails(cb, highlightParam);
        }

        static bool IsFunctionWithArguments(MemberModel member)
        {
            return member != null && (member.Flags & FlagType.Function) > 0
                && member.Parameters != null && member.Parameters.Count > 0;
        }

        /// <summary>
        /// Short contextual details to display in tips
        /// </summary>
        /// <param name="cb">Parsed comments</param>
        /// <returns>Formated comments</returns>
        static public string GetTipShortDetails(CommentBlock cb, string highlightParam)
        {
            string details = "";
            
            // get parameter detail
            if (!string.IsNullOrEmpty(highlightParam) && cb.ParamName != null)
            {
                for(int i=0; i<cb.ParamName.Count; i++)
                {
                    if (highlightParam == cb.ParamName[i])
                    {
                        details += "<br/>" + MethodCallTip.HLTextStyleBeg + highlightParam + ":" + MethodCallTip.HLTextStyleEnd
                                + Get2LinesOf(cb.ParamDesc[i], true).TrimStart();
                        return details;
                    }
                }
            }
            // get description extract
            if (ASContext.CommonSettings.SmartTipsEnabled)
            {
                string info = !string.IsNullOrEmpty(cb.InfoTip) 
                    ? cb.InfoTip
                    : Get2LinesOf(cb.Description, cb.IsFunctionWithArguments);

                details += "<br/><div>" + info + "</div>";
            }

            return details;
        }

        static private string GetShortcutDocs()
        {
            Color themeForeColor = PluginBase.MainForm.GetThemeColor("MethodCallTip.InfoColor");
            string foreColorString = themeForeColor != Color.Empty ? DataConverter.ColorToHex(themeForeColor).Replace("0x", "#") : "#666666:MULTIPLY";
            return "<br/><span style=\"color:" + foreColorString + "\"><i>(" + TextHelper.GetString("Info.ShowDetails") + ")</i></span>";
        }

        /// <summary>
        /// Split multiline text and return 2 lines or less of text
        /// </summary>
        static public string Get2LinesOf(string text)
        {
            return GetNLinesOf(text, 2, false);
        }

        static public string Get2LinesOf(string text, bool alwaysAddShortcutDocs)
        {
            return GetNLinesOf(text, 2, alwaysAddShortcutDocs);
        }

        static protected List<string> blockElements = new List<string>
        {
            "P", "IMG", "BLOCKQUOTE", "IMG", "PRE", "TR", "DIV", "H1", "H2", "H3", "H4", "H5", "H6", "HR", "LI", "OL", "UL", "TABLE"
        };

        static public string GetNLinesOf(string text, int lineCount, bool alwaysAddShortcutDocs)
        {
            // Since we use HTML this is a bit complex and not 100% accurate... maybe we should just separate the shortcut tip from the text,
            // use different views, and hide the extra height. Maybe we could also use HTMLRenderer for this? it would be slower tho.
            List<string> tags = new List<string>();
            string lastTag = null;
            byte element = 0; // 1:tag, 2:attribute, 3: attribute value, 4:comment
            int lineNo = 0;
            int i, count;
            bool normalLineEnd = false;
            bool followsAnonBlock = false;

            for (i = 0, count = text.Length; i < count && lineNo < lineCount; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '<':
                        if (element != 0) continue;

                        if (i >= count - 1) break;

                        if (i < count - 3 && text[i + 1] == '!' && text[i + 2] == '-' && text[i + 3] == '-')
                        {
                            i += 3;
                            element = 4;
                            continue;
                        }

                        string tag;
                        StringBuilder tagBuilder = new StringBuilder();
                        bool closeTag = false;

                        element = 1;
                        if (text[i + 1] == '/')
                        {
                            i++;
                            closeTag = true;
                        }

                        c = text[++i];
                        while (!char.IsWhiteSpace(c) && c != '>' && c != '/' && i < count)
                        {
                            tagBuilder.Append(c);
                            c = text[++i];
                        }
                        i--;
                        tag = tagBuilder.ToString().ToUpperInvariant();

                        if (!closeTag)
                        {
                            if (tag == "BR")
                            {
                                lineNo++;
                                if (lineNo >= lineCount) i -= 3;
                            }
                            else
                            {
                                if (blockElements.Contains(tag))
                                {
                                    if (tag == "PRE")
                                    {
                                        normalLineEnd = true;
                                    }
                                    if ((!string.IsNullOrEmpty(lastTag) && lastTag != "BR" && lastTag != "TD" && lastTag != "TH") || followsAnonBlock)
                                    {
                                        lineNo++;

                                        if (lineNo >= lineCount)
                                        {
                                            i -= (tag.Length + 1);
                                            break;
                                        }
                                    }
                                }
                                tags.Add(tagBuilder.ToString());
                            }
                        }
                        else
                        {
                            if (tag == "PRE")
                            {
                                normalLineEnd = false;
                            }

                            for (int j = tags.Count - 1; j >= 0; j--)
                            {
                                if (tags[j].Equals(tag, StringComparison.OrdinalIgnoreCase))
                                {
                                    tags.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                        followsAnonBlock = false;
                        lastTag = tag;
                        break;

                    case '>':
                        if (element == 1)
                        {
                            element = 0;

                            if (text[i - 1] == '/')
                            {
                                
                            }
                        }
                        else if (element == 4 && text[i - 1] == '-' && text[i - 2] == '-')
                        {
                            element = 0;
                        }

                        break;

                    case '=':
                        if (element != 1 && element != 2) continue;

                        element = 3;

                        c = text[++i];
                        while (!char.IsWhiteSpace(c) && i < count)
                        {
                            c = text[++i];
                        }

                        while (text[++i] != c && i < count)
                        {
                        }
                        i++;
                        element = 1;

                        break;

                    case '\r':
                        if (normalLineEnd)
                        {
                            lineNo++;
                        }

                        if (i < count - 1 && text[i + 1] == '\n') i++;

                        break;

                    case '\n':
                        if (normalLineEnd)
                        {
                            lineNo++;
                        }
                        break;

                    default:
                        followsAnonBlock = element == 0 && tags.Count == 0 && (!char.IsWhiteSpace(c) || followsAnonBlock);

                        break;
                }
            }

            StringBuilder result = new StringBuilder(text.Substring(0, i).TrimEnd());

            for (int j = tags.Count - 1; j >= 0; j--)
            {
                result.Append("</").Append(tags[j]).Append('>');
            }

            if (i < text.Length || alwaysAddShortcutDocs) result.Append(" \u2026").Append(GetShortcutDocs());
            return result.ToString();
        }
        
        /// <summary>
        /// Extract member comments for display in the completion list
        /// </summary>
        /// <param name="member">Member data</param>
        /// <param name="highlightParam">Parameter to highlight</param>
        /// <returns>Formated comments</returns>
        static public string GetTipFullDetails(MemberModel member, string highlightParam)
        {
            if (member == null || member.Comments == null || !ASContext.CommonSettings.SmartTipsEnabled) return "";
            CommentBlock cb = ParseComment(member.Comments);
            cb.IsFunctionWithArguments = IsFunctionWithArguments(member);
            return GetTipFullDetails(cb, highlightParam);
        }

        /// <summary>
        /// Extract comments for display in the completion list
        /// </summary>
        /// <param name="cb">Parsed comments</param>
        /// <returns>Formated comments</returns>
        static public string GetTipFullDetails(CommentBlock cb, string highlightParam)
        {
            var details = new StringBuilder();
            if (cb.Description.Length > 0)
            {
                if (ASContext.CommonSettings.DescriptionLinesLimit > 0)
                {
                    details.Append(GetNLinesOf(cb.Description, ASContext.CommonSettings.DescriptionLinesLimit, false));
                    if (details.Length != cb.Description.Length) details.Append(" \u2026<br/>");
                }
                else details.Append(cb.Description);
            }
            
            // @usage
            if (cb.TagName != null)
            {
                bool hasUsage = false;
                for(int i=0; i<cb.TagName.Count; i++)
                    if (cb.TagName[i] == "usage") 
                    {
                        hasUsage = true;
                        details.Append("<br/>&nbsp;&nbsp;&nbsp;&nbsp;").Append(cb.TagDesc[i]);
                    }
                if (hasUsage) details.Append("<br/>");
            }
            
            // @param
            if (cb.ParamName != null && cb.ParamName.Count > 0)
            {
                details.Append("<p style=\"margin-bottom: 0\">Param:");
                for(int i=0; i<cb.ParamName.Count; i++)
                {
                    details.Append("<br/>&nbsp;&nbsp;&nbsp;&nbsp;");
                    if (highlightParam == cb.ParamName[i])
                    {
                        details.Append(MethodCallTip.HLBgStyleBeg)
                            .Append(MethodCallTip.HLTextStyleBeg).Append(highlightParam).Append(":").Append(MethodCallTip.HLTextStyleEnd).Append(" ")
                            .Append(cb.ParamDesc[i]) 
                            .Append(MethodCallTip.HLBgStyleEnd);
                    }
                    else details.Append(cb.ParamName[i]).Append(": ").Append(cb.ParamDesc[i]);
                }
                details.Append("</p>");
            }
            
            // @return
            if (cb.Return != null)
            {
                details.Append("<p style=\"margin-bottom: 0\">Return:");

                if (!string.IsNullOrEmpty(cb.Return))
                {
                    details.Append("<br/>&nbsp;&nbsp;&nbsp;&nbsp;").Append(cb.Return);
                }

                details.Append("</p>");
            }
            
            return "<br/><br/><div>" + details + "</div>";
        }
        #endregion
    }

}
