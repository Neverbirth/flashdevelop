using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;
using PluginCore.Localization;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore;
using ProjectManager.Projects;
using ASCompletion.Model;
using ASCompletion.Context;
using ASClassWizard.Resources;
using ASClassWizard.Wizards;
using ASCompletion.Completion;
using System.Collections.Generic;
using System.Linq;

namespace ASClassWizard
{
    public class PluginMain : IPlugin
    {
        private String pluginName = "ASClassWizard";
        private String pluginGuid = "a2c159c1-7d21-4483-aeb1-38d9fdc4c7f3";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "Provides an ActionScript class wizard for FlashDevelop.";
        private String pluginAuth = "FlashDevelop Team";

        private AS3ClassOptions lastFileOptions;
        private String lastFileFromTemplate;
        private IASContext processContext;
        private String processOnSwitch;
        private String constructorArgs;
        private List<String> constructorArgTypes;

        #region Required Properties
        
        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public Int32 Api
        {
            get { return 1; }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public String Name
        {
            get { return this.pluginName; }
        }

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
        {
            get { return this.pluginGuid; }
        }

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
        {
            get { return this.pluginAuth; }
        }

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
        {
            get { return this.pluginDesc; }
        }

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
        {
            get { return this.pluginHelp; }
        }

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return null; }
        }
        
        #endregion
        
        #region Required Methods

        public void Initialize()
        {
            this.AddEventHandlers();
            this.InitLocalization();
        }
        
        public void Dispose()
        {
            // Nothing here...
        }
        
        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority priority)
        {
            Project project;
            switch (e.Type)
            {
                case EventType.Command:
                    DataEvent evt = (DataEvent)e;
                    if (evt.Action == "ProjectManager.CreateNewFile")
                    {
                        Hashtable table = evt.Data as Hashtable;
                        project = PluginBase.CurrentProject as Project;
                        String templatePath = table["templatePath"] as String;
                        if (templatePath == null || !IsWizardTemplate(templatePath)) return;
                        string type = Path.GetFileNameWithoutExtension(templatePath);
                        type = type.Substring(0, type.IndexOf('.')).ToUpperInvariant();
                        if (type == "CLASS" && (project.Language.StartsWithOrdinal("as") || project.Language == "haxe"))
                        {
                            evt.Handled = true;
                            String className = table.ContainsKey("className") ? table["className"] as String : TextHelper.GetString("Wizard.Label.NewClass");
                            DisplayClassWizard(table["inDirectory"] as String, templatePath, className, table["constructorArgs"] as String, table["constructorArgTypes"] as List<String>);
                            if (lastFileOptions != null)
                                table["result"] = lastFileOptions.ClassModel;
                            lastFileOptions = null;
                        }
                        else if (type == "INTERFACE" && (project.Language == "as3" || project.Language == "haxe"))
                        {
                            evt.Handled = true;
                            String interfaceName = table.ContainsKey("interfaceName") ? table["interfaceName"] as String : "INewInterface";
                            DisplayInterfaceWizard(table["inDirectory"] as String, templatePath, interfaceName, table["fromClass"] as ClassModel);
                            if (lastFileOptions != null)
                                table["result"] = lastFileOptions.ClassModel;
                            lastFileOptions = null;
                        }
                    }
                    break;

                case EventType.FileSwitch:
                    if (PluginBase.MainForm.CurrentDocument.FileName == processOnSwitch)
                    {
                        processOnSwitch = null;
                        if (lastFileOptions == null || lastFileOptions.ClassModel.Implements == null) return;
                        foreach (String cname in lastFileOptions.ClassModel.Implements)
                        {
                            ASContext.Context.CurrentModel.Check();
                            ClassModel inClass = ASContext.Context.CurrentModel.GetPublicClass();
                            ASGenerator.SetJobContext(null, cname, null, null);
                            ASGenerator.GenerateJob(GeneratorJobType.ImplementInterface, null, inClass, null, null);
                        }
                    }
                    break;

                case EventType.ProcessArgs:
                    TextEvent te = e as TextEvent;
                    project = PluginBase.CurrentProject as Project;
                    if (lastFileFromTemplate != null && project != null && (project.Language.StartsWithOrdinal("as") || project.Language == "haxe"))
                    {
                        te.Value = ProcessArgs(project, te.Value);
                    }
                    break;
            }
        }

        private bool IsWizardTemplate(string templateFile)
        {
            return templateFile != null && File.Exists(templateFile + ".wizard");
        }
        
        #endregion

        #region Custom Methods

        public static IMainForm MainForm { get { return PluginBase.MainForm; } }

        public void AddEventHandlers()
        {
            EventManager.AddEventHandler(this, EventType.Command | EventType.ProcessArgs);
            EventManager.AddEventHandler(this, EventType.FileSwitch, HandlingPriority.Low);
        }

        public void InitLocalization()
        {
            this.pluginDesc = TextHelper.GetString("Info.Description");
        }

        private void DisplayClassWizard(String inDirectory, String templateFile, String className, String constructorArgs, List<String> constructorArgTypes)
        {
            Project project = PluginBase.CurrentProject as Project;
            String classpath = project.AbsoluteClasspaths.GetClosestParent(inDirectory) ?? inDirectory;
            String package;
            try
            {
                package = GetPackage(classpath, inDirectory);
                if (package == "")
                {
                    // search in Global classpath
                    Hashtable info = new Hashtable();
                    info["language"] = project.Language;
                    DataEvent de = new DataEvent(EventType.Command, "ASCompletion.GetUserClasspath", info);
                    EventManager.DispatchEvent(this, de);
                    if (de.Handled && info.ContainsKey("cp"))
                    {
                        List<string> cps = info["cp"] as List<string>;
                        if (cps != null)
                        {
                            foreach (string cp in cps)
                            {
                                package = GetPackage(cp, inDirectory);
                                if (package != "")
                                {
                                    classpath = cp;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                package = "";
            }
            using (AS3ClassWizard dialog = new AS3ClassWizard())
            {
                bool isHaxe = project.Language == "haxe";
                dialog.Project = project;
                dialog.Directory = inDirectory;
                dialog.StartupClassName = className;
                if (package != null)
                {
                    package = package.Replace(Path.DirectorySeparatorChar, '.');
                    dialog.StartupPackage = package;
                }
                DialogResult conflictResult = DialogResult.OK;
                string cPackage, path, newFilePath;
                do
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    cPackage = dialog.getPackage();
                    path = Path.Combine(classpath, cPackage.Replace('.', Path.DirectorySeparatorChar));
                    newFilePath = Path.ChangeExtension(Path.Combine(path, dialog.getClassName()),
                                                              isHaxe ? ".hx" : ".as");
                    if (File.Exists(newFilePath))
                    {
                        string title = " " + TextHelper.GetString("FlashDevelop.Title.ConfirmDialog");
                        string message = TextHelper.GetString("PluginCore.Info.FolderAlreadyContainsFile");
                        conflictResult = MessageBox.Show(PluginBase.MainForm,
                            string.Format(message, newFilePath, "\n"), title,
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        if (conflictResult == DialogResult.No) return;
                    }
                } while (conflictResult == DialogResult.Cancel);

                string templatePath = templateFile + ".wizard";
                this.lastFileFromTemplate = newFilePath;
                this.constructorArgs = constructorArgs;
                this.constructorArgTypes = constructorArgTypes;
                ClassModel model = new ClassModel
                {
                    Name = dialog.getClassName(),
                    ExtendsType = dialog.getSuperClass(),
                    Implements = dialog.hasInterfaces() ? dialog.getInterfaces() : null,
                    InFile = new FileModel(string.Empty) { Package = cPackage }
                };
                if (dialog.isPublic()) model.Access |= Visibility.Public;
                if (dialog.isDynamic()) model.Flags |= FlagType.Dynamic;
                if (dialog.isFinal()) model.Flags |= FlagType.Final;
                lastFileOptions = new AS3ClassOptions
                {
                    Language = project.Language,
                    ClassModel = model,
                    CreateInheritedMethods = dialog.getGenerateInheritedMethods(),
                    CreateConstructor = dialog.getGenerateConstructor(),
                };
                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    MainForm.FileFromTemplate(templatePath, newFilePath);
                }
                catch (Exception ex)
                {
                    ErrorManager.ShowError(ex);
                }
            }
        }

        private void DisplayInterfaceWizard(String inDirectory, String templateFile, String interfaceName, ClassModel fromClass)
        {
            Project project = PluginBase.CurrentProject as Project;
            String classpath = project.AbsoluteClasspaths.GetClosestParent(inDirectory) ?? inDirectory;
            String package;
            try
            {
                package = GetPackage(classpath, inDirectory);
                if (package == string.Empty)
                {
                    // search in Global classpath
                    Hashtable info = new Hashtable();
                    info["language"] = project.Language;
                    DataEvent de = new DataEvent(EventType.Command, "ASCompletion.GetUserClasspath", info);
                    EventManager.DispatchEvent(this, de);
                    if (de.Handled && info.ContainsKey("cp"))
                    {
                        List<string> cps = info["cp"] as List<string>;
                        if (cps != null)
                        {
                            foreach (string cp in cps)
                            {
                                package = GetPackage(cp, inDirectory);
                                if (package != string.Empty)
                                {
                                    classpath = cp;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                package = string.Empty;
            }
            using (AS3InterfaceWizard dialog = new AS3InterfaceWizard())
            {
                bool isHaxe = project.Language == "haxe";
                dialog.Project = project;
                dialog.Directory = inDirectory;
                dialog.InterfaceName = interfaceName;
                if (package != null)
                {
                    package = package.Replace(Path.DirectorySeparatorChar, '.');
                    dialog.Package = package;
                }
                dialog.ClassModel = fromClass;
                DialogResult conflictResult = DialogResult.OK;
                string iPackage, path, newFilePath;
                do
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    iPackage = dialog.Package;
                    path = Path.Combine(classpath, iPackage.Replace('.', Path.DirectorySeparatorChar));
                    newFilePath = Path.ChangeExtension(Path.Combine(path, dialog.InterfaceName), isHaxe ? ".hx" : ".as");
                    if (File.Exists(newFilePath))
                    {
                        string title = " " + TextHelper.GetString("FlashDevelop.Title.ConfirmDialog");
                        string message = TextHelper.GetString("PluginCore.Info.FolderAlreadyContainsFile");
                        conflictResult = MessageBox.Show(PluginBase.MainForm, string.Format(message, newFilePath, "\n"), title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        if (conflictResult == DialogResult.No) return;
                    }
                } while (conflictResult == DialogResult.Cancel);

                string templatePath = templateFile + ".wizard";
                lastFileFromTemplate = newFilePath;
                dialog.ClassModel.InFile.FileName = newFilePath;
                lastFileOptions = new AS3ClassOptions
                {
                    Language = project.Language,
                    ClassModel = dialog.ClassModel
                };

                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    MainForm.FileFromTemplate(templatePath, newFilePath);
                }
                catch (Exception ex)
                {
                    ErrorManager.ShowError(ex);
                }
            }
        }

        private string GetPackage(string classpath, string path)
        {
            if (!path.StartsWith(classpath, StringComparison.OrdinalIgnoreCase))
                return "";
            string subPath = path.Substring(classpath.Length).Trim(new char[] { '/', '\\', ' ', '.' });
            return subPath.Replace(Path.DirectorySeparatorChar, '.');
        }

        public string ProcessArgs(Project project, string args)
        {
            if (lastFileFromTemplate != null)
            {
                string package = lastFileOptions != null ? lastFileOptions.ClassModel.InFile.Package : "";
                string fileName = Path.GetFileNameWithoutExtension(lastFileFromTemplate);
                args = args.Replace("$(FileName)", fileName);
                if (args.Contains("$(FileNameWithPackage)") || args.Contains("$(Package)"))
                {
                    if (package == "") args = args.Replace(" $(Package)", "");
                    args = args.Replace("$(Package)", package);
                    if (package != "") args = args.Replace("$(FileNameWithPackage)", package + "." + fileName);
                    else args = args.Replace("$(FileNameWithPackage)", fileName);
                    if (lastFileOptions != null)
                    {
                        args = ProcessFileTemplate(args);
                    }
                }
                lastFileFromTemplate = null;
            }
            return args;
        }

        private string ProcessFileTemplate(string args)
        {
            Int32 eolMode = (Int32)MainForm.Settings.EOLMode;
            String lineBreak = LineEndDetector.GetNewLineMarker(eolMode);
            List<String> imports = new List<string>();
            string extends = "";
            string implements = "";
            string access = "";
            string inheritedMethods = "";
            string paramString = "";
            string superConstructor = "";
            string classMetadata = "";
            int index;
            ClassModel model = lastFileOptions.ClassModel;
            // resolve imports
            if (model.Implements != null && model.Implements.Count > 0)
            {
                string implementContinuation;
                index = 0;

                if (lastFileOptions.Language == "haxe")
                {
                    bool isHaxe2 = PluginBase.CurrentSDK != null && PluginBase.CurrentSDK.Name.ToLower().Contains("haxe 2");
                    if (isHaxe2)
                    {
                        implements = " implements ";
                        implementContinuation = ", implements ";
                    }
                    else  
                    {
                        implements = implementContinuation = (model.Flags & FlagType.Interface) != 0 ? " extends " : " implements ";
                    }
                }
                else
                {
                    implements = " implements ";
                    implementContinuation = ", ";
                }

                foreach (string item in model.Implements)
                {
                    var interfaceParts = item.Split('.');
                    if (interfaceParts.Length > 1) imports.Add(item);
                    implements += (index > 0 ? implementContinuation : "") + interfaceParts[interfaceParts.Length - 1];
                    if (lastFileOptions.CreateInheritedMethods)
                    {
                        processOnSwitch = lastFileFromTemplate; 
                        // let ASCompletion generate the implementations when file is opened
                    }
                    index++;
                }
            }
            if (!string.IsNullOrEmpty(model.ExtendsType))
            {
                var superClassFullName = model.ExtendsType;
                var _extends = superClassFullName.Split('.');
                var superClassShortName = _extends[_extends.Length - 1];
                var fileName = Path.GetFileNameWithoutExtension(lastFileFromTemplate);
                if (_extends.Length > 1) imports.Add(superClassFullName);
                extends = fileName == superClassShortName ? $" extends {superClassFullName}" : $" extends {superClassShortName}";

                processContext = ASContext.GetLanguageContext(lastFileOptions.Language);
                if (lastFileOptions.CreateConstructor && processContext != null && constructorArgs == null)
                {
                    var lastDotIndex = superClassFullName.LastIndexOf('.');
                    var cmodel = processContext.GetModel(lastDotIndex < 0 ? "" : superClassFullName.Substring(0, lastDotIndex), superClassShortName, "");
                    if (!cmodel.IsVoid())
                    {
                        if ((cmodel.Flags & FlagType.TypeDef) != 0)
                        {
                            var tmp = cmodel;
                            while (!tmp.IsVoid())
                            {
                                if (!string.IsNullOrEmpty(tmp.Constructor))
                                {
                                    cmodel = tmp;
                                    break;
                                }
                                tmp.ResolveExtends();
                                tmp = tmp.Extends;
                            }
                        }
                        foreach (MemberModel member in cmodel.Members)
                        {
                            if (member.Name == cmodel.Constructor)
                            {
                                paramString = member.ParametersString();
                                AddImports(imports, member, cmodel);
                                superConstructor = "super(";
                                index = 0;
                                if (member.Parameters != null)
                                foreach (MemberModel param in member.Parameters)
                                {
                                    if (param.Name.StartsWith('.')) break;
                                    var pname = TemplateUtils.GetParamName(param);
                                    superConstructor += (index > 0 ? ", " : "") + pname;
                                    index++;
                                }
                                superConstructor += ");\n" + (lastFileOptions.Language == "as3" ? "\t\t\t" : "\t\t");
                                break;
                            }
                        }
                    }
                }
                processContext = null;
            }
            if (constructorArgs != null)
            {
                paramString = constructorArgs;
                foreach (String type in constructorArgTypes)
                {
                    if (!imports.Contains(type))
                    {
                        imports.Add(type);
                    }
                }
            }
            if (lastFileOptions.Language == "as3")
            {
                access = (model.Access & Visibility.Public) > 0 ? "public " : "internal ";
                access += (model.Flags & FlagType.Dynamic) > 0 ? "dynamic " : "";
                access += (model.Flags & FlagType.Final) > 0 ? "final " : "";
            }
            else if (lastFileOptions.Language == "haxe")
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
            if (lastFileOptions.ClassModel.Members != null)
            {
                IASContext context = ASContext.Context;
                var boundary = lastFileOptions.Language == "as3" ? "\t\t" : "\t";
                if (!PluginBase.Settings.UseTabs)
                    boundary = boundary.Replace("\t", new string(' ', PluginBase.Settings.IndentSize));
                foreach (var m in lastFileOptions.ClassModel.Members.Items)
                {
                    if ((m.Flags & FlagType.Getter) > 0)
                    {
                        var type = m.Type ?? context.Features.objectKey;
                        var dot = type.LastIndexOf('.');
                        if (dot > -1) imports.Add(type);

                        if (lastFileOptions.Language == "haxe")
                        {
                            membersSrc.Append("var ").Append(m.Name).Append("(");

                            if (m.Parameters != null)
                            {
                                foreach (var p in m.Parameters)
                                {
                                    membersSrc.Append(p.Name).Append(", ");
                                }

                                membersSrc.Remove(membersSrc.Length - 2, 2);
                            }

                            membersSrc.Append(")");
                        }
                        else
                        {
                            membersSrc.Append("function get ").Append(m.Name).Append("()");
                        }

                        membersSrc.Append(":").Append(type.Substring(dot + 1)).Append(";")
                            .Append(lineBreak).Append(boundary);
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

                        membersSrc.Append("):").Append(context.Features.voidKey).Append(";").Append(lineBreak).Append(boundary);
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
                        membersSrc.Append("):").Append(rtype.Substring(rdot + 1)).Append(";").Append(lineBreak).Append(boundary);
                    }
                    else if ((m.Flags & FlagType.Variable) > 0)
                    {
                        membersSrc.Append("var ").Append(m.Name);

                        if (!string.IsNullOrEmpty(m.Type))
                        {
                            var type = m.Type;
                            var dot = type.LastIndexOf('.');
                            if (dot > -1) imports.Add(type);
                            membersSrc.Append(":").Append(type.Substring(dot + 1));
                        }
                        membersSrc.Append(";").Append(lineBreak).Append(boundary);
                    }
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
                    importsSrc.Append((lastFileOptions.Language == "as3" ? "\t" : "")).Append("import ").Append(import).
                        Append(";").Append(lineBreak);
                }
            }
            if (importsSrc.Length > 0)
            {
                importsSrc.Append((lastFileOptions.Language == "as3" ? "\t" : "")).Append(lineBreak);
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

        private void AddImports(List<String> imports, MemberModel member, ClassModel inClass)
        {
            AddImport(imports, member.Type, inClass);
            if (member.Parameters != null)
            {
                foreach (MemberModel item in member.Parameters)
                {
                    AddImport(imports, item.Type, inClass);
                }
            }
        }

        private void AddImport(List<string> imports, String cname, ClassModel inClass)
        {
            ClassModel aClass = processContext.ResolveType(cname, inClass.InFile);
            if (aClass != null && !aClass.IsVoid() && aClass.InFile.Package != "")
            {
                imports.Add(aClass.QualifiedName);
            }
        }

        #endregion
    }
}
