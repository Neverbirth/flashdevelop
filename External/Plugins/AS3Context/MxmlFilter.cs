using System;
using System.Collections.Generic;
using System.Text;
using ASCompletion.Model;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace AS3Context
{
    class MxmlContextBase
    {
        public MemberList MxmlMembers = new MemberList();
        public string BaseTag = string.Empty;
        public List<string> States;
        public List<MxmlInlineRange> Outline = new List<MxmlInlineRange>();
        public List<InlineRange> As3Ranges = new List<InlineRange>();
    }

    class MxmlFilterContext : MxmlContextBase
    {
        public enum FlexDocumentType
        {
            Unknown, Flex3, Flex4, FlexJs
        }

        public Dictionary<string, string> Namespaces = new Dictionary<string, string>();
        public List<MxmlCatalog> Catalogs = new List<MxmlCatalog>();
        public List<MxmlContextBase> Components;
        public FileModel Model;

        private FlexDocumentType? _documentType;
        public FlexDocumentType DocumentType
        {
            get
            {
                if (_documentType == null)
                {
                    _documentType = FlexDocumentType.Unknown;
                    if (Namespaces != null)
                    {
                        string nsUri;
                        // Should we compare against the root tag namespace instead? some checks indicate this is the way of doing it
                        if (Namespaces.TryGetValue("basic", out nsUri) && nsUri == "library://ns.apache.org/flexjs/basic")
                            _documentType = FlexDocumentType.FlexJs;
                        else if (Namespaces.TryGetValue("fx", out nsUri) && nsUri == "http://ns.adobe.com/mxml/2009")
                            _documentType = FlexDocumentType.Flex4;
                        else if (Namespaces.TryGetValue("mx", out nsUri) && (nsUri == MxmlFilter.OLD_MX || nsUri == MxmlFilter.BETA_MX))
                            _documentType = FlexDocumentType.Flex3;
                    }
                }

                return _documentType.Value;
            }
        }

    }

    class MxmlInlineRange : InlineRange
    {
        public string Tag;
        public int LineFrom;
        public int LineTo;
        public int Level;
        public MemberModel Model;

        public MxmlInlineRange(string syntax, int start, int end)
            : base(syntax, start, end)
        { }
    }

    #region MXML Filter
    class MxmlFilter
    {
        static private readonly Regex tagName = new Regex("<(?<name>/?[a-z][a-z0-9_:]*)[\\s>]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static public string OLD_MX = "http://www.adobe.com/2006/mxml";
        static public string BETA_MX = "library://ns.adobe.com/flex/halo";
        static public string NEW_MX = "library://ns.adobe.com/flex/mx";

        static private List<MxmlCatalog> catalogs = new List<MxmlCatalog>();
        static private Dictionary<string, MxmlCatalogs> archive = new Dictionary<string, MxmlCatalogs>();

        /// <summary>
        /// Reset catalogs for new classpath definition
        /// </summary>
        static public void ClearCatalogs()
        {
            catalogs.Clear();
        }

        /// <summary>
        /// Check if a catalog was already extracted from indicated SWC 
        /// </summary>
        static public bool HasCatalog(string file)
        {
            return archive.ContainsKey(file);
        }

        /// <summary>
        /// Read a SWC catalog file
        /// </summary>
        static public void AddCatalogs(string file, byte[] rawData)
        {
            try
            {
                FileInfo info = new FileInfo(file);
                MxmlCatalogs cat;
                if (HasCatalog(file))
                {
                    cat = archive[file];
                    if (cat.TimeStamp == info.LastWriteTime)
                    {
                        if (cat.Count > 0)
                            catalogs.AddRange(cat.Values);
                        return;
                    }
                }

                cat = new MxmlCatalogs();
                cat.Read(file, rawData);
                cat.TimeStamp = info.LastWriteTime;
                archive[file] = cat;
                if (cat.Count > 0)
                    catalogs.AddRange(cat.Values);
            }
            catch (XmlException ex) { Console.WriteLine(ex.Message); }
            catch (Exception) { }
        }

        /// <summary>
        /// Add an archived SWC catalog
        /// </summary>
        static public void AddCatalog(string file)
        {
            MxmlCatalogs cat = archive[file];
            if (cat.Count > 0)
                catalogs.AddRange(cat.Values);
        }

        /// <summary>
        /// Read a manifest file
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="file"></param>
        static public void AddManifest(string uri, string file)
        {
            try
            {
                FileInfo info = new FileInfo(file);
                MxmlCatalogs cat;
                if (archive.ContainsKey(file))
                {
                    cat = archive[file];
                    if (cat.TimeStamp == info.LastWriteTime)
                    {
                        if (cat.Count > 0)
                            catalogs.AddRange(cat.Values);
                        return;
                    }
                }

                cat = new MxmlCatalogs();
                cat.TimeStamp = info.LastWriteTime;
                cat.Read(file, null, uri);
                archive[file] = cat;
                if (cat.Count > 0)
                    catalogs.AddRange(cat.Values);
            }
            catch (XmlException ex) { Console.WriteLine(ex.Message); }
            catch (Exception) { }
        }

        /// <summary>
        /// Called if a FileModel needs filtering
        /// - define inline AS3 ranges
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        static public string FilterSource(string name, string src, MxmlFilterContext ctx)
        {
            List<InlineRange> as3ranges = ctx.As3Ranges;
            List<MxmlInlineRange> outline = ctx.Outline;
            MemberList mxmlMembers = ctx.MxmlMembers;

            StringBuilder sb = new StringBuilder("package{");
            int len = src.Length - 8;
            int line = 0;
            bool firstNode = true;
            bool inXml = true;
            bool inCdata = false;
            bool inComment = false;
            InlineRange inlineRange = null;
            string fxTag = null;
            var tagStack = new Stack<MxmlInlineRange>();
            var componentScripts = new List<StringBuilder>();
            MxmlContextBase componentContext = null;
            StringBuilder componentScript = null;
            int anonNo = 0;
            for (int i = 0; i < len; i++)
            {
                char c = src[i];
            xmlStart:
                // keep newlines
                if (c == 10 || c == 13)
                {
                    if (c == 13) line++;
                    else if (i > 0 && src[i - 1] != 13) line++;
                    sb.Append(c);
                    if (componentScript != null) componentScript.Append(c);
                }
                // XML comment
                else if (inComment)
                {
                    if (i < len - 3 && c == '-' && src[i + 1] == '-' && src[i + 2] == '>')
                    {
                        inComment = false;
                        i += 2;
                    }
                    continue;
                }
                // in XML
                else if (inXml)
                {
                    if (c == '<' && i < len)
                    {
                        if (src[i + 1] == '!')
                        {
                            if (src[i + 2] == '-' && src[i + 3] == '-')
                            {
                                inComment = true;
                                i += 3;
                                continue;
                            }
                        }
                        else
                        {
                            if (firstNode && src[i + 1] != '?')
                            {
                                int space = src.IndexOfAny(new char[] { ' ', '\t', '\n' }, i);
                                string tag = GetXMLContextTag(src, space);
                                if (tag != null && space > 0)
                                {
                                    firstNode = false;
                                    ctx.BaseTag = tag;
                                    var mxmlTag = new MxmlInlineRange("xml", i, -1)
                                    {
                                        LineFrom = line,
                                        LineTo = -1,
                                        Tag = tag,
                                        Level = tagStack.Count
                                    };
                                    outline.Add(mxmlTag);
                                    tagStack.Push(mxmlTag);
                                    ReadNamespaces(ctx, src, space);

                                    // TODO: Namespace may not be mx and fx
                                    fxTag = ctx.DocumentType == MxmlFilterContext.FlexDocumentType.Flex3 ? "mx:" : "fx:";
                                    string type = MxmlComplete.ResolveType(ctx, tag);
                                    sb.Append("public class ").Append(name)
                                        .Append(" extends ").Append(type).Append('{');
                                }
                            }
                            else if (!firstNode)
                            {
                                int space = src.IndexOfAny(new char[] { ' ', '\t', '\n', '>' }, i);
                                if (src[space - 1] == '/') space--;
                                string tag = GetXMLContextTag(src, space);
                                if (tag != null && space > 0)
                                {
                                    int tagStart = i;
                                    if (src[space] == '\n')
                                    {
                                        if (src[space - 1] == '\r')
                                        {
                                            sb.Append('\r');
                                        }
                                        line++;
                                        sb.Append('\n');
                                    }
                                    i = space;
                                    if (tag.StartsWith("/"))
                                    {
                                        if (inlineRange != null)
                                        {
                                            inlineRange.End = tagStart - 1;
                                            as3ranges.Add(inlineRange);
                                            inlineRange = null;
                                        }

                                        if (tag == "/" + fxTag + "Component" && componentScript != null)
                                        {
                                            componentScript.Append("}");
                                            componentScripts.Add(componentScript);
                                            componentContext.Outline.Add(tagStack.Peek());
                                            componentScript = null;
                                            componentContext = null;
                                        }

                                        if (tagStack.Count == 0 || tag != "/" + tagStack.Peek().Tag) // Unbalanced or malformed
                                        {
                                            // Display warning to the user through the output panel or our own (m)xml outline panel?
                                        }
                                        else
                                        {
                                            var cMxmlTag = tagStack.Pop();
                                            cMxmlTag.End = i;
                                            cMxmlTag.LineTo = line;
                                        }
                                        continue;
                                    }

                                    switch (tag)
                                    {
                                        case "mx:State":
                                        case "s:State":
                                            var mxCtx = componentContext ?? ctx;
                                            var nameSpace = string.Empty;
                                            var nameSpaceIndex = mxCtx.BaseTag.IndexOf(':');
                                            if (nameSpaceIndex > -1)
                                                nameSpace = mxCtx.BaseTag.Substring(0, nameSpaceIndex + 1);
                                            if ((tagStack.Count == 2 || componentContext != null) && tagStack.Peek().Tag == nameSpace + "states")
                                                ReadStateTag(mxCtx, src, ref i);
                                            break;
                                        default:
                                            if (tag == fxTag + "Script" || tag == fxTag + "Style")
                                            {
                                                if (tagStack.Count == 1 || (tagStack.Count >= 4 && componentScript != null))
                                                {
                                                    string attributeName;
                                                    bool fromSource = false;
                                                    do
                                                    {
                                                        attributeName = GetAttributeName(src, ref i);
                                                        if (attributeName == "source") fromSource = true;
                                                    } while (attributeName != null);
                                                    i--;
                                                    if (!fromSource && src[i] != '/' && src[i + 1] != '>')
                                                    {
                                                        inlineRange = new InlineRange(tag.EndsWith("t") ? "as3" : "css", -1, -1);
                                                    }
                                                }
                                            } 
                                            else if (tag == fxTag + "Component")
                                            {
                                                if (tagStack.Count >= 2)
                                                {
                                                    string attributeName, className = null;
                                                    do
                                                    {
                                                        attributeName = GetAttributeName(src, ref i);
                                                        if (attributeName == "className")
                                                            className = GetAttributeValue(src, ref i);
                                                    } while (attributeName != null);
                                                    i--;
                                                    if (src[i] != '/' && src[i + 1] != '>')
                                                    {
                                                        if (className != null)
                                                            componentScript = new StringBuilder("class ")
                                                                .Append(className);
                                                        else
                                                            componentScript = new StringBuilder("[ExcludeClass]")
                                                                .AppendLine().Append("class _inline" + anonNo++);  // This should be hidden from autocompletion... ExcludeClass will be handled later
                                                    }
                                                }
                                            }
                                            else if (componentScript != null && componentContext == null)
                                            {
                                                if (ctx.Components == null) ctx.Components = new List<MxmlContextBase>();
                                                componentContext = new MxmlContextBase {BaseTag = tag};
                                                ctx.Components.Add(componentContext);
                                                string type = MxmlComplete.ResolveType(ctx, tag);
                                                // Better to use a topLevel Element? opinions? it will be more work for both the developer and the machine, but it won't appear in the auto-completion list
                                                componentScript.Append(" extends ").Append(type)
                                                    .Append("{private var outerDocument:")
                                                    .Append(name).Append(";");
                                            }
                                            break;
                                    }

                                    var oMxmlTag = new MxmlInlineRange("xml", tagStart, -1)
                                    {
                                        LineFrom = line,
                                        LineTo = -1,
                                        Tag = tag,
                                        Level = tagStack.Count
                                    };
                                    outline.Add(oMxmlTag);

                                    if (src[i] == '/' && src[i + 1] == '>')
                                    {
                                        i++;
                                        inXml = false;
                                        oMxmlTag.End = i;
                                        oMxmlTag.LineTo = line;
                                        continue;
                                    }
                                    else if (src[i] == '>')
                                    {
                                        inXml = false;
                                        if (inlineRange != null) inlineRange.Start = i + 1;
                                    }
                                    tagStack.Push(oMxmlTag);
                                }
                            }
                        }
                    }
                    else if (c == '=' && i > 4) // <node id="..."
                    {
                        int off = i - 1;
                        while (char.IsWhiteSpace(src[off]) && off > 4) off--;
                        if (src[off - 2] <= 32 && src[off - 1] == 'i' && src[off] == 'd' && tagStack.Count > 0)
                        {
                            string tag = tagStack.Peek().Tag;
                            string id = GetAttributeValue(src, ref i);
                            if (!string.IsNullOrEmpty(id))
                            {
                                MemberModel member = new MemberModel(id, tag, FlagType.Variable | FlagType.Dynamic, Visibility.Public);
                                member.LineTo = member.LineFrom = line;
                                string type = MxmlComplete.ResolveType(ctx, tag);
                                var members = componentContext == null ? mxmlMembers : componentContext.MxmlMembers;
                                var builder = componentScript ?? sb;
                                members.Add(member);
                                builder.Append("public var ").Append(id)
                                    .Append(':').Append(type).Append(';');

                                tagStack.Peek().Model = member;
                            }
                        }
                    }
                    else if (c == '/' && src[i + 1] == '>')
                    {
                        i++;
                        if (tagStack.Count > 0)
                        {
                            var mxmlTag = tagStack.Pop();
                            mxmlTag.LineTo = line;
                            mxmlTag.End = i;
                        }
                    }
                    else if (c == '>')
                    {
                        inXml = false;
                        if (inlineRange != null) inlineRange.Start = i + 1;
                    }
                }
                // in element value
                else
                {
                    if (c == '<')
                    {
                        if (!inCdata && i < len && src[i + 1] == '!' && src[i + 2] == '[' && src.Substring(i + 2, 7) == "[CDATA[")
                        {
                            i += 8;
                            inCdata = true;
                            if (inlineRange != null)
                            {
                                inlineRange.End = i - 8;
                                as3ranges.Add(inlineRange);
                                inlineRange = new InlineRange(inlineRange.Syntax, i + 1, -1);
                            }
                        }
                        else
                        {
                            inXml = true;
                            if (inlineRange != null)
                            {
                                inlineRange.End = i;
                                as3ranges.Add(inlineRange);
                                inlineRange = null;
                            }
                            /* I have mixed feelings about this, we could either add some extra flag and/or clutter code some more, or make i--
                             * Decided to save some cycles and checks doing this. Large mxml files may thank it.
                               xmlStart should be below else if (inXml), but variable scope won't allow for it... */
                            goto xmlStart; 
                        }
                    }
                    else if (c == ']' && inCdata && src[i + 1] == ']' && src[i + 2] == '>')
                    {
                        inCdata = false;
                        if (inlineRange != null)
                        {
                            inlineRange.End = i;
                            as3ranges.Add(inlineRange);
                            inlineRange = new InlineRange(inlineRange.Syntax, i + 3, -1);
                        }
                        i += 2;
                    }
                    else
                    {
                        if (inlineRange != null && inlineRange.Syntax == "as3")
                        {
                            if (componentScript == null) sb.Append(c);
                            else componentScript.Append(c);
                        }
                    }
                }
            }
            if (inlineRange != null)
            {
                inlineRange.End = src.Length;
                as3ranges.Add(inlineRange);
            }
            sb.Append("}}");

            if (componentScript != null) componentScripts.Add(componentScript);

            if (componentScripts.Count > 0)
            {
                foreach (var script in componentScripts)
                    sb.AppendLine().AppendLine().Append(script.ToString());
            }

            return sb.ToString();
        }

        private static void ReadNamespaces(MxmlFilterContext ctx, string src, int i)
        {
            // declared ns
            int len = src.Length;
            while (i < len)
            {
                string name = GetAttributeName(src, ref i);
                if (name == null) break;
                string value = GetAttributeValue(src, ref i);
                if (value == null) break;
                if (name.StartsWith("xmlns"))
                {
                    string[] qname = name.Split(':');
                    if (qname.Length == 1) ctx.Namespaces["*"] = value;
                    else ctx.Namespaces[qname[1]] = value;
                }
            }
            // find catalogs
            foreach (string ns in ctx.Namespaces.Keys)
            {
                string uri = ctx.Namespaces[ns];
                if (uri == BETA_MX) uri = OLD_MX;
                foreach (MxmlCatalog cat in catalogs)
                    if (cat.URI == uri)
                    {
                        cat.NS = ns;
                        ctx.Catalogs.Add(cat);
                    }
            }
        }

        private static void ReadStateTag(MxmlContextBase ctx, string src, ref int i)
        {
            string attrName;
            do
            {
                attrName = GetAttributeName(src, ref i);
                if (attrName == "name")
                {
                    string value = GetAttributeValue(src, ref i);
                    if (value == null) continue;

                    if (ctx.States == null) ctx.States = new List<string>();
                    if (!ctx.States.Contains(value)) ctx.States.Add(value);
                }
                else if (attrName == "stateGroups")
                {
                    string value = GetAttributeValue(src, ref i);
                    if (value == null) continue;

                    foreach (var stateGroup in value.Split(','))
                    {
                        var groupName = stateGroup.Trim();
                        if (ctx.States == null) ctx.States = new List<string>();
                        if (!ctx.States.Contains(value)) ctx.States.Add(groupName);
                    }
                }
            } while (attrName != null);
            i--;
        }

        /// <summary>
        /// Get the attribute name
        /// </summary>
        internal static string GetAttributeName(string src, ref int i)
        {
            string name = "";
            char c;
            int oldPos = i;
            int len = src.Length;
            bool skip = true;
            while (i < len)
            {
                c = src[i++];
                if (c == '\'' || c == '"' || c == '<' || c == '>' || c == '&' || c == '/') return null;
                if (skip && c > 32) skip = false;
                if (c == '=')
                {
                    if (!skip) return name;
                    else break;
                }
                else if (!skip && c > 32) name += c;
            }
            i = oldPos;
            return null;
        }

        /// <summary>
        /// Get the attribute value
        /// </summary>
        internal static string GetAttributeValue(string src, ref int i)
        {
            string value = "";
            char c;
            int oldPos = i;
            int len = src.Length;
            bool skip = true;
            while (i < len)
            {
                c = src[i++];
                if (c == 10 || c == 13) break;
                if (c == '"')
                {
                    if (!skip) return value;
                    else skip = false;
                }
                else if (!skip) value += c;
            }
            i = oldPos;
            return null;
        }

        /// <summary>
        /// Gets the xml context tag
        /// </summary> 
        private static string GetXMLContextTag(string src, int position)
        {
            if (position < 0) return null;
            StringBuilder sb = new StringBuilder();
            char c = src[position - 1];
            position -= 2;
            sb.Append(c);
            while (position >= 0)
            {
                c = src[position];
                sb.Insert(0, c);
                if (c == '>') return null;
                if (c == '<') break;
                position--;
            }
            string tag = sb.ToString();
            Match mTag = tagName.Match(tag + " ");
            if (mTag.Success) return mTag.Groups["name"].Value;
            return null;
        }

        /// <summary>
        /// Called if a FileModel needs filtering
        /// - modify parsed model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        static public void FilterSource(FileModel model, MxmlFilterContext ctx)
        {
            ctx.Model = model;
            model.InlinedIn = "xml";
            model.InlinedRanges = ctx.As3Ranges;

            if (model.MetaDatas == null) model.MetaDatas = new List<ASMetaData>();
            foreach (string key in ctx.Namespaces.Keys)
            {
                ASMetaData meta = new ASMetaData("Namespace");
                meta.Params = new Dictionary<string, string>();
                meta.Params.Add(key, ctx.Namespaces[key]);
                model.MetaDatas.Add(meta);
            }

            ClassModel aClass = model.GetPublicClass();
            if (aClass == ClassModel.VoidClass)
                return;
            aClass.Comments = "<" + ctx.BaseTag + "/>";

            Dictionary<string, string> resolved = new Dictionary<string, string>();
            foreach (MemberModel mxmember in ctx.MxmlMembers)
            {
                string tag = mxmember.Type;
                string type = null;
                if (!resolved.TryGetValue(tag, out type))
                {
                    type = MxmlComplete.ResolveType(ctx, tag);
                    resolved[tag] = type;
                }
                MemberModel member = aClass.Members.Search(mxmember.Name, FlagType.Variable, Visibility.Public);
                if (member != null)
                {
                    member.Comments = "<" + tag + "/>";
                    member.Type = type;
                }
            }

            for (int i = 1, count = model.Classes.Count - 1; i < count; i++)
            {
                var pClass = model.Classes[i];
                var diff = pClass.LineFrom - ctx.Components[i - 1].Outline[0].LineFrom;
                pClass.LineFrom -= diff;
                pClass.LineTo -= diff;
                foreach (var pModel in pClass.Members.Items)
                {
                    pModel.LineFrom -= diff;
                    pModel.LineTo -= diff;
                }
            }
        }
    }
    #endregion

    #region Catalogs
    class MxmlCatalogs : Dictionary<String, MxmlCatalog>
    {
        public String FileName;
        public DateTime TimeStamp;

        public void Read(string fileName, byte[] rawData)
        {
            Read(fileName, rawData, null);
        }

        public void Read(string fileName, byte[] rawData, string defaultURI)
        {
            FileName = fileName;
            XmlReader reader;
            if (rawData == null) reader = new XmlTextReader(fileName);
            else reader = new XmlTextReader(new MemoryStream(rawData));

            MxmlCatalog cat = null;
            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element
                    && reader.Name == "component")
                {
                    string className = reader.GetAttribute("className") ?? reader.GetAttribute("class");
                    string name = reader.GetAttribute("name") ?? reader.GetAttribute("id");
                    string uri = reader.GetAttribute("uri") ?? defaultURI;
                    if (uri == MxmlFilter.BETA_MX) uri = MxmlFilter.OLD_MX;

                    if (cat == null || cat.URI != uri)
                    {
                        if (!TryGetValue(uri, out cat))
                        {
                            cat = new MxmlCatalog();
                            cat.URI = uri;
                            Add(uri, cat);
                        }
                    }
                    if (className == "__AS3__.vec.Vector") className = "Vector";
                    cat[name] = className.Replace(':', '.');
                }
            }
        }
    }

    class MxmlCatalog : Dictionary<string, string>
    {
        public string URI;
        public string NS;
    }
    #endregion
}
