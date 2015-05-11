using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ASCompletion.Completion;
using ASCompletion.Context;
using ASCompletion.Model;
using PluginCore;
using PluginCore.Utilities;

namespace ASClassWizard.Resources
{
    public class AS3ClassOptions : ITemplateProcessor
    {
        public bool CreateInheritedMethods;
        public bool CreateConstructor;
        public string ConstructorArgs;
        public List<string> ConstructorArgTypes;
        public string Language;

        public ClassModel ClassModel { get; set; }

        public string TargetFile { get; set; }

        public virtual string ProcessFileTemplate(string args)
        {
            string package = ClassModel.InFile.Package;
            string fileName = Path.GetFileNameWithoutExtension(TargetFile);
            args = args.Replace("$(FileName)", fileName);
            if (package == "") args = args.Replace(" $(Package)", "");
            args = args.Replace("$(Package)", package);
            if (package != "") args = args.Replace("$(FileNameWithPackage)", package + "." + fileName);
            else args = args.Replace("$(FileNameWithPackage)", fileName);

            return ProcessModelArgs(args);
        }

        public void FileSwitched()
        {
            if (ClassModel.Implements == null || !CreateInheritedMethods) return;

            foreach (String cname in ClassModel.Implements)
            {
                ASContext.Context.CurrentModel.Check();
                ClassModel inClass = ASContext.Context.CurrentModel.GetPublicClass();
                ASGenerator.SetJobContext(null, cname, null, null);
                ASGenerator.GenerateJob(GeneratorJobType.ImplementInterface, null, inClass, null, null);
            }
        }

        protected virtual string ProcessModelArgs(string args)
        {
            Int32 eolMode = (Int32)PluginBase.MainForm.Settings.EOLMode;
            String lineBreak = LineEndDetector.GetNewLineMarker(eolMode);
            ClassModel cmodel;
            List<String> imports = new List<string>();
            string extends = "";
            string implements = "";
            string access = "";
            string inheritedMethods = "";
            string paramString = "";
            string superConstructor = "";
            string classMetadata = "";
            int index;
            ClassModel model = ClassModel;
            // resolve imports
            if (model.Implements != null && model.Implements.Count > 0)
            {
                bool isHaxe2 = PluginBase.CurrentSDK != null && PluginBase.CurrentSDK.Name.ToLower().Contains("haxe 2");
                implements = " implements ";
                string[] _implements;
                index = 0;
                foreach (string item in model.Implements)
                {
                    _implements = item.Split('.');
                    if (_implements.Length > 1) imports.Add(item);
                    implements += (index > 0 ? (isHaxe2 ? ", implements " : ", ") : "") + _implements[_implements.Length - 1];
                    index++;
                }
            }
            if (!string.IsNullOrEmpty(model.ExtendsType))
            {
                String super = model.ExtendsType;
                string[] _extends = super.Split('.');
                if (_extends.Length > 1) imports.Add(super);
                extends = " extends " + _extends[_extends.Length - 1];
                var processContext = ASContext.GetLanguageContext(Language);
                if (CreateConstructor && processContext != null && ConstructorArgs == null)
                {
                    cmodel = processContext.GetModel(super.LastIndexOf('.') < 0 ? "" : super.Substring(0, super.LastIndexOf('.')), _extends[_extends.Length - 1], "");
                    if (!cmodel.IsVoid())
                    {
                        foreach (MemberModel member in cmodel.Members)
                        {
                            if (member.Name == cmodel.Constructor)
                            {
                                paramString = member.ParametersString();
                                AddImports(imports, member, cmodel, processContext);
                                superConstructor = "super(";
                                index = 0;
                                if (member.Parameters != null)
                                    foreach (MemberModel param in member.Parameters)
                                    {
                                        if (param.Name.StartsWith(".")) break;
                                        superConstructor += (index > 0 ? ", " : "") + param.Name;
                                        index++;
                                    }
                                superConstructor += ");\n" + (Language == "as3" ? "\t\t\t" : "\t\t");
                                break;
                            }
                        }
                    }
                }
            }
            if (ConstructorArgs != null)
            {
                paramString = ConstructorArgs;
                foreach (String type in ConstructorArgTypes)
                {
                    if (!imports.Contains(type))
                    {
                        imports.Add(type);
                    }
                }
            }
            if (Language == "as3")
            {
                access = (model.Access & Visibility.Public) > 0 ? "public " : "internal ";
                access += (model.Flags & FlagType.Dynamic) > 0 ? "dynamic " : "";
                access += (model.Flags & FlagType.Final) > 0 ? "final " : "";
            }
            else if (Language == "haxe")
            {
                access = (model.Access & Visibility.Public) > 0 ? "public " : "private ";
                access += (model.Flags & FlagType.Dynamic) > 0 ? "dynamic " : "";
                if ((model.Flags & FlagType.Final) > 0) classMetadata += "@:final\n";
            }
            else
            {
                access = (model.Flags & FlagType.Dynamic) > 0 ? "dynamic " : "";
            }
            var membersSrc = new StringBuilder();
            if (ClassModel.Members != null)
            {
                IASContext context = ASContext.Context;
                // For now only supported on AS3 and interfaces, so I'm avoiding many checks and cases
                foreach (var m in ClassModel.Members.Items)
                {
                    if ((m.Flags & FlagType.Getter) > 0)
                    {
                        var type = m.Type ?? context.Features.voidKey;
                        var dot = type.LastIndexOf('.');
                        if (dot > -1) imports.Add(type);
                        membersSrc.Append("function get ").Append(m.Name).Append("():").Append(type.Substring(dot + 1)).Append(";").Append(lineBreak).Append("\t\t");
                    }
                    else if ((m.Flags & FlagType.Setter) > 0)
                    {
                        membersSrc.Append("function set ").Append(m.Name).Append("(value:");

                        if (m.Parameters != null && m.Parameters.Count > 0)
                        {
                            var type = m.Parameters[0].Type ?? context.Features.objectKey;
                            var dot = type.LastIndexOf('.');
                            if (dot > -1) imports.Add(type);
                            membersSrc.Append(type.Substring(dot + 1));
                        }
                        else
                            membersSrc.Append(context.Features.objectKey);

                        membersSrc.Append("):").Append(context.Features.voidKey).Append(";").Append(lineBreak).Append("\t\t");
                    }
                    else if ((m.Flags & FlagType.Function) > 0)
                    {
                        membersSrc.Append("function ").Append(m.Name).Append("(");

                        if (m.Parameters != null)
                        {
                            foreach (var p in m.Parameters)
                            {
                                var type = p.Type ?? context.Features.objectKey;
                                var dot = type.LastIndexOf('.');
                                if (dot > -1) imports.Add(type);
                                membersSrc.Append(p.Name).Append(":").Append(type.Substring(dot + 1));

                                if (p.Value != null) membersSrc.Append("=").Append(p.Value);

                                membersSrc.Append(", ");
                            }

                            membersSrc.Remove(membersSrc.Length - 2, 2);
                        }

                        var rtype = m.Type ?? context.Features.voidKey;
                        var rdot = rtype.LastIndexOf('.');
                        if (rdot > -1) imports.Add(rtype);
                        membersSrc.Append("):").Append(rtype.Substring(rdot + 1)).Append(";").Append(lineBreak).Append("\t\t");
                    }
                }
                if (membersSrc.Length > 0)
                {
                    membersSrc.Append(lineBreak).Append(Language == "as3" ? "\t\t" : string.Empty);
                }
            }
            var importsSrc = new StringBuilder();
            string prevImport = null;
            imports.Sort();
            foreach (string import in imports)
            {
                if (prevImport != import)
                {
                    prevImport = import;
                    if (import.LastIndexOf('.') == -1) continue;
                    if (import.Substring(0, import.LastIndexOf('.')) == model.InFile.Package) continue;
                    importsSrc.Append((Language == "as3" ? "\t" : "")).Append("import ").Append(import).
                        Append(";").Append(lineBreak);
                }
            }
            if (importsSrc.Length > 0)
            {
                importsSrc.Append((Language == "as3" ? "\t" : "")).Append(lineBreak);
            }
            args = args.Replace("$(Import)", importsSrc.ToString());
            args = args.Replace("$(Extends)", extends);
            args = args.Replace("$(Implements)", implements);
            args = args.Replace("$(Access)", access);
            args = args.Replace("$(InheritedMethods)", inheritedMethods);
            args = args.Replace("$(ConstructorArguments)", paramString);
            args = args.Replace("$(Super)", superConstructor);
            args = args.Replace("$(ClassMetadata)", classMetadata);
            args = args.Replace("$(Members)", membersSrc.ToString());
            return args;
        }

        private void AddImports(List<String> imports, MemberModel member, ClassModel inClass, IASContext processContext)
        {
            AddImport(imports, member.Type, inClass, processContext);
            if (member.Parameters != null)
            {
                foreach (MemberModel item in member.Parameters)
                {
                    AddImport(imports, item.Type, inClass, processContext);
                }
            }
        }

        private void AddImport(List<string> imports, String cname, ClassModel inClass, IASContext processContext)
        {
            ClassModel aClass = processContext.ResolveType(cname, inClass.InFile);
            if (aClass != null && !aClass.IsVoid() && aClass.InFile.Package != "")
            {
                imports.Add(aClass.QualifiedName);
            }
        }

    }
}
