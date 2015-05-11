/* 
 * Extensibility idea: We could have a collection of IWizardProviders, each one could be like the following:
 * 
 * IWizardProvider
 * -----------------------------------------------------
 * + bool IsValidProject(Project project)
 * + ITemplateProcessor DisplayWizard(HashTable options)   <--- Tentatively this should also fill options["result"]
 * 
 * This way we could inject new wizards, attached to a type + language possibly (CLASShaxe for example), and so centralize logic, avoid registering too much event listeners, etc.
 */

#region imports

using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

using PluginCore.Localization;
using PluginCore.Managers;
using PluginCore;

using ProjectManager.Projects;

using ASCompletion.Model;

using ASClassWizard.Resources;
using ASClassWizard.Wizards;

using System.Collections.Generic;

#endregion

namespace ASClassWizard
{
    public class PluginMain : IPlugin
    {
        private String pluginName = "ASClassWizard";
        private String pluginGuid = "a2c159c1-7d21-4483-aeb1-38d9fdc4c7f3";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "Provides an ActionScript class wizard for FlashDevelop.";
        private String pluginAuth = "FlashDevelop Team";

        private ITemplateProcessor lastFileOptions;

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
        
        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
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
                        if (type.IndexOf('.') > -1)
                            type = type.Substring(0, type.IndexOf('.')).ToUpperInvariant();
                        if (type == "CLASS" && (project.Language.StartsWith("as") || project.Language == "haxe"))
                        {
                            evt.Handled = true;
                            String className = table.ContainsKey("className") ? table["className"] as String : TextHelper.GetString("Wizard.Label.NewClass");
                            DisplayClassWizard(table["inDirectory"] as String, templatePath, className, table["constructorArgs"] as String, table["constructorArgTypes"] as List<String>);
                        }
                        else if (type == "INTERFACE" && project.Language == "as3")
                        {
                            evt.Handled = true;
                            String interfaceName = table.ContainsKey("interfaceName") ? table["interfaceName"] as String : "INewInterface";
                            DisplayInterfaceWizard(table["inDirectory"] as String, templatePath, interfaceName, table["fromClass"] as ClassModel);
                        }
                        else if (type == "MXML" && project.Language == "as3" && AS3Context.MxmlFilter.GetCatalogs().Count > 0)
                        {
                            evt.Handled = true;
                            DisplayMxmlWizard(table["inDirectory"] as String, templatePath);
                        }
                        if (lastFileOptions != null)
                            table["result"] = lastFileOptions.ClassModel;
                    }
                    break;

                case EventType.FileSwitch:
                    if (lastFileOptions != null && PluginBase.MainForm.CurrentDocument.FileName == lastFileOptions.TargetFile)
                    {
                        lastFileOptions.FileSwitched();
                    }
                    lastFileOptions = null;
                    break;

                case EventType.ProcessArgs:
                    TextEvent te = e as TextEvent;
                    project = PluginBase.CurrentProject as Project;
                    if (lastFileOptions != null && project != null && (project.Language.StartsWith("as") || project.Language == "haxe"))
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
            catch (System.NullReferenceException)
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
                    TargetFile = newFilePath,
                    Language = project.Language,
                    ClassModel = model,
                    CreateInheritedMethods = dialog.getGenerateInheritedMethods(),
                    CreateConstructor = dialog.getGenerateConstructor(),
                    ConstructorArgs = constructorArgs,
                    ConstructorArgTypes = constructorArgTypes
                };
                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    MainForm.FileFromTemplate(templatePath, newFilePath);
                }
                catch (Exception ex)
                {
                    ErrorManager.ShowError(ex);
                    lastFileOptions = null;
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
                dialog.ClassModel.InFile.FileName = newFilePath;
                lastFileOptions = new AS3ClassOptions
                {
                    TargetFile = newFilePath,
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
                    lastFileOptions = null;
                }
            }
        }

        private void DisplayMxmlWizard(String inDirectory, String templateFile)
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
            using (MxmlClassWizard dialog = new MxmlClassWizard())
            {
                dialog.Project = project;
                dialog.Directory = inDirectory;
                if (package != null)
                {
                    package = package.Replace(Path.DirectorySeparatorChar, '.');
                    dialog.Package = package;
                }
                DialogResult conflictResult = DialogResult.OK;
                string iPackage, path, newFilePath;
                do
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    iPackage = dialog.Package;
                    path = Path.Combine(classpath, iPackage.Replace('.', Path.DirectorySeparatorChar));
                    newFilePath = Path.ChangeExtension(Path.Combine(path, dialog.ClassName), ".mxml");
                    if (File.Exists(newFilePath))
                    {
                        string title = " " + TextHelper.GetString("FlashDevelop.Title.ConfirmDialog");
                        string message = TextHelper.GetString("PluginCore.Info.FolderAlreadyContainsFile");
                        conflictResult = MessageBox.Show(PluginBase.MainForm, string.Format(message, newFilePath, "\n"), title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        if (conflictResult == DialogResult.No) return;
                    }
                } while (conflictResult == DialogResult.Cancel);

                string templatePath = templateFile + ".wizard";
                dialog.ClassModel.InFile.FileName = newFilePath;
                lastFileOptions = new MxmlClassOptions
                {
                    TargetFile = newFilePath,
                    ClassModel = dialog.ClassModel,
                    ClassModelPrefix = dialog.BaseClassPrefix,
                    Namespaces = dialog.Namespaces,
                    ImplementInterfaces = dialog.ImplementInterfaces
                };

                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    MainForm.FileFromTemplate(templatePath, newFilePath);
                }
                catch (Exception ex)
                {
                    ErrorManager.ShowError(ex);
                    lastFileOptions = null;
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
            if (lastFileOptions != null)
            {
                args = lastFileOptions.ProcessFileTemplate(args);
            }
            return args;
        }

        #endregion

    }
    
}
