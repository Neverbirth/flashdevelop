using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using PluginCore.Managers;
using ASCompletion.Context;
using ASCompletion.Model;
using PluginCore;
using System.Collections;
using System.Text.RegularExpressions;
using PluginCore.Controls;
using PluginCore.Localization;
using AS3Context.Compiler;
using PluginCore.Helpers;
using System.Timers;
using ASCompletion.Completion;
using ProjectManager.Projects;
using ProjectManager.Projects.AS3;

namespace AS3Context
{
    public class Context : AS2Context.Context
    {
        static readonly protected Regex re_genericType =
            new Regex("(?<gen>[^<]+)\\.<(?<type>.+)>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // C:\path\to\Main.as$raw$:31: col: 1:  Error #1084: Syntax error: expecting rightbrace before end of program.
        static readonly protected Regex re_syntaxError =
            new Regex("(?<filename>.*)\\$raw\\$:(?<line>[0-9]+): col: (?<col>[0-9]+):(?<desc>.*)", RegexOptions.Compiled);

        static readonly protected Regex re_customAPI =
            new Regex("[/\\\\](playerglobal|airglobal|builtin)\\.swc", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #region initialization
        private AS3Settings as3settings;
        private bool hasAIRSupport;
        private bool hasMobileSupport;
        private MxmlFilterContext mxmlFilterContext; // extract inlined AS3 ranges & MXML tags
        private System.Timers.Timer timerCheck;
        private string fileWithSquiggles;
        protected bool mxmlEnabled;

        /// <summary>
        /// Do not call directly
        /// </summary>
        public Context()
        {
        }

        public Context(AS3Settings initSettings)
        {
            as3settings = initSettings;

            /* AS-LIKE OPTIONS */

            hasLevels = false;
            docType = "flash.display.MovieClip";

            /* DESCRIBE LANGUAGE FEATURES */

            mxmlEnabled = true;

            // language constructs
            features.hasPackages = true;
            features.hasNamespaces = true;
            features.hasImports = true;
            features.hasImportsWildcard = true;
            features.hasClasses = true;
            features.hasExtends = true;
            features.hasImplements = true;
            features.hasInterfaces = true;
            features.hasEnums = false;
            features.hasGenerics = true;
            features.hasEcmaTyping = true;
            features.hasVars = true;
            features.hasConsts = true;
            features.hasMethods = true;
            features.hasStatics = true;
            features.hasOverride = true;
            features.hasTryCatch = true;
            features.hasE4X = true;
            features.hasStaticInheritance = true;
            features.checkFileName = true;

            // allowed declarations access modifiers
            Visibility all = Visibility.Public | Visibility.Internal | Visibility.Protected | Visibility.Private;
            features.classModifiers = all;
            features.varModifiers = all;
            features.constModifiers = all;
            features.methodModifiers = all;

            // default declarations access modifiers
            features.classModifierDefault = Visibility.Internal;
            features.varModifierDefault = Visibility.Internal;
            features.methodModifierDefault = Visibility.Internal;

            // keywords
            features.dot = ".";
            features.meta = '[';
            features.voidKey = "void";
            features.objectKey = "Object";
            features.booleanKey = "Boolean";
            features.numberKey = "Number";
            features.arrayKey = "Array";
            features.importKey = "import";
            features.typesPreKeys = new string[] { "import", "new", "typeof", "is", "as", "extends", "implements" };
            features.codeKeywords = new string[] { 
                "var", "function", "const", "new", "delete", "typeof", "is", "as", "return", 
                "break", "continue", "if", "else", "for", "each", "in", "while", "do", "switch", "case", "default", "with",
                "null", "true", "false", "try", "catch", "finally", "throw", "use", "namespace"
            };
            features.accessKeywords = new string[] { 
                "extern", "dynamic", "inline", "final", "public", "private", "protected", "internal", "static", "override"
            };
            features.declKeywords = new string[] { "var", "function", "const", "namespace", "get", "set" };
            features.typesKeywords = new string[] { "import", "class", "interface" };
            features.varKey = "var";
            features.constKey = "const";
            features.functionKey = "function";
            features.getKey = "get";
            features.setKey = "set";
            features.staticKey = "static";
            features.finalKey = "final";
            features.overrideKey = "override";
            features.publicKey = "public";
            features.internalKey = "internal";
            features.protectedKey = "protected";
            features.privateKey = "private";
            features.intrinsicKey = "extern";
            features.namespaceKey = "namespace";

            /* INITIALIZATION */

            settings = initSettings;
            //BuildClassPath(); // defered to first use

            // live syntax checking
            timerCheck = new Timer(500);
            timerCheck.SynchronizingObject = PluginBase.MainForm as System.Windows.Forms.Form;
            timerCheck.AutoReset = false;
            timerCheck.Elapsed += new ElapsedEventHandler(timerCheck_Elapsed);
            FlexShells.SyntaxError += new SyntaxErrorHandler(FlexShell_SyntaxError);
        }
        #endregion

        #region classpath management
        /// <summary>
        /// Classpathes & classes cache initialisation
        /// </summary>
        public override void BuildClassPath()
        {
            ReleaseClasspath();
            started = true;
            if (as3settings == null) throw new Exception("BuildClassPath() must be overridden");
            if (contextSetup == null)
            {
                contextSetup = new ContextSetupInfos();
                contextSetup.Lang = settings.LanguageId;
                contextSetup.Platform = "Flash Player";
                contextSetup.Version = as3settings.DefaultFlashVersion;
            }

            // external version definition
            platform = contextSetup.Platform;
            majorVersion = 10;
            minorVersion = 0;
            ParseVersion(contextSetup.Version, ref majorVersion, ref minorVersion);
            hasAIRSupport = platform == "AIR" || platform == "AIR Mobile";
            hasMobileSupport = platform == "AIR Mobile";

            string cpCheck = contextSetup.Classpath != null ?
                String.Join(";", contextSetup.Classpath).Replace('\\', '/') : "";

            // check if CP contains a custom playerglobal.swc
            bool hasCustomAPI = re_customAPI.IsMatch(cpCheck);

            //
            // Class pathes
            //
            classPath = new List<PathModel>();
            MxmlFilter.ClearCatalogs();

            // SDK
            string compiler = PluginBase.CurrentProject != null
                ? PluginBase.CurrentProject.CurrentSDK
                : as3settings.GetDefaultSDK().Path;

            char S = Path.DirectorySeparatorChar;
            if (compiler == null)
                compiler = Path.Combine(PathHelper.ToolDir, "flexlibs");
            string frameworks = compiler + S + "frameworks";
            string sdkLibs = frameworks + S + "libs";

            string sdkLocales = frameworks + S + "locale" + S + PluginBase.MainForm.Settings.LocaleVersion;
            string fallbackLibs = PathHelper.ResolvePath(PathHelper.ToolDir + S + "flexlibs" + S + "frameworks" + S + "libs");
            string fallbackLocale = PathHelper.ResolvePath(PathHelper.ToolDir + S + "flexlibs" + S + "frameworks" + S + "locale" + S + "en_US");

            var buildContext = new BuildEnvironmentContext();
            List<string> addLocales = new List<string>();
            if (!Directory.Exists(sdkLibs) && !sdkLibs.StartsWith("$")) // fallback
            {
                sdkLibs = PathHelper.ResolvePath(PathHelper.ToolDir + S + "flexlibs" + S + "frameworks" + S + "libs" + S + "player");
            }

            if (majorVersion > 0 && !String.IsNullOrEmpty(sdkLibs) && Directory.Exists(sdkLibs))
            {
                // Flex framework
                // If we're Flex we are using the default config files. Actually, it should be the same way for pure AS3 projects
                if (cpCheck.IndexOf("Library/AS3/frameworks/Flex", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool isFlexJs = cpCheck.IndexOf("Library/AS3/frameworks/FlexJS", StringComparison.OrdinalIgnoreCase) >= 0;
                    // framework SWCs
                    string as3Fmk = PathHelper.ResolvePath("Library" + S + "AS3" + S + "frameworks");

                    string defaultConfigFile = "flex-config.xml";

                    if (hasMobileSupport)
                        defaultConfigFile = "airmobile-config.xml";
                    else if (hasAIRSupport)
                        defaultConfigFile = "air-config.xml";
                    else
                    {
                        // If it's decided to load the default config file for pure AS3 projects we can clean up this together with some more code
                        bool swcPresent = false;
                        string playerglobal = MatchPlayerGlobalExact(majorVersion, minorVersion, sdkLibs);
                        if (playerglobal != null) swcPresent = true;
                        else playerglobal = MatchPlayerGlobalExact(majorVersion, minorVersion, fallbackLibs);
                        if (playerglobal == null) playerglobal = MatchPlayerGlobalAny(ref majorVersion, ref minorVersion, fallbackLibs);
                        if (playerglobal == null) playerglobal = MatchPlayerGlobalAny(ref majorVersion, ref minorVersion, sdkLibs);
                        if (playerglobal != null)
                        {
                            // add missing SWC in new SDKs
                            if (!swcPresent && sdkLibs.IndexOf(S + "flexlibs") < 0 && Directory.Exists(compiler))
                            {
                                string swcDir = sdkLibs + S + "player" + S;
                                if (!Directory.Exists(swcDir + "9") && !Directory.Exists(swcDir + "10"))
                                    swcDir += majorVersion + "." + minorVersion;
                                else
                                    swcDir += majorVersion;
                                try
                                {
                                    if (!File.Exists(swcDir + S + "playerglobal.swc"))
                                    {
                                        Directory.CreateDirectory(swcDir);
                                        File.Copy(playerglobal, swcDir + S + "playerglobal.swc");
                                        File.WriteAllText(swcDir + S + "FlashDevelopNotice.txt",
                                            "This 'playerglobal.swc' was copied here automatically by FlashDevelop from:\r\n" + playerglobal);
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    LoadConfigFile(frameworks + S + defaultConfigFile, buildContext);

                    if (hasCustomAPI)
                    {
                        if (hasAIRSupport)
                            buildContext.LibPaths.RemoveAll(
                                x => x.EndsWith("air" + S + "airglobal.swc") || x.EndsWith("air" + S + "aircore.swc") ||
                                     x.EndsWith("air" + S + "applicationupdater.swc"));
                        else
                        {
                            buildContext.LibPaths.RemoveAll(x => x.EndsWith(S + "playerglobal.swc"));
                            buildContext.ExternalLibPaths.RemoveAll(x => x.EndsWith(S + "playerglobal.swc"));
                        }
                    }

                    if (isFlexJs)
                        MxmlFilter.AddManifest("http://ns.adobe.com/mxml/2009", as3Fmk + S + "FlexJS" + S + "manifest.xml");
                    else if (cpCheck.IndexOf("Library/AS3/frameworks/Flex4", StringComparison.OrdinalIgnoreCase) >= 0)
                        MxmlFilter.AddManifest("http://ns.adobe.com/mxml/2009", as3Fmk + S + "Flex4" + S + "manifest.xml");
                    else
                        MxmlFilter.AddManifest(MxmlFilter.OLD_MX, as3Fmk + S + "Flex3" + S + "manifest.xml");
                }
                else
                {
                    // core API SWC
                    if (!hasCustomAPI)
                        if (hasAIRSupport)
                        {
                            buildContext.LibPaths.Add("air" + S + "airglobal.swc");
                            buildContext.LibPaths.Add("air" + S + "aircore.swc");
                            buildContext.LibPaths.Add("air" + S + "applicationupdater.swc");
                        }
                        else
                        {
                            bool swcPresent = false;
                            string playerglobal = MatchPlayerGlobalExact(majorVersion, minorVersion, sdkLibs);
                            if (playerglobal != null) swcPresent = true;
                            else playerglobal = MatchPlayerGlobalExact(majorVersion, minorVersion, fallbackLibs);
                            if (playerglobal == null) playerglobal = MatchPlayerGlobalAny(ref majorVersion, ref minorVersion, fallbackLibs);
                            if (playerglobal == null) playerglobal = MatchPlayerGlobalAny(ref majorVersion, ref minorVersion, sdkLibs);
                            if (playerglobal != null)
                            {
                                // add missing SWC in new SDKs
                                if (!swcPresent && sdkLibs.IndexOf(S + "flexlibs") < 0 && Directory.Exists(compiler))
                                {
                                    string swcDir = sdkLibs + S + "player" + S;
                                    if (!Directory.Exists(swcDir + "9") && !Directory.Exists(swcDir + "10"))
                                        swcDir += majorVersion + "." + minorVersion;
                                    else
                                        swcDir += majorVersion;
                                    try
                                    {
                                        if (!File.Exists(swcDir + S + "playerglobal.swc"))
                                        {
                                            Directory.CreateDirectory(swcDir);
                                            File.Copy(playerglobal, swcDir + S + "playerglobal.swc");
                                            File.WriteAllText(swcDir + S + "FlashDevelopNotice.txt",
                                                "This 'playerglobal.swc' was copied here automatically by FlashDevelop from:\r\n" + playerglobal);
                                        }
                                        playerglobal = swcDir + S + "playerglobal.swc";
                                    }
                                    catch { }
                                }
                                buildContext.ExternalLibPaths.Add(playerglobal);
                            }
                        }
                    addLocales.Add("playerglobal_rb.swc");

                    // Flex core - ie. (Bitmap|Font|ByteArray|...)Asset / Flex(Sprite|MovieClip|Loader...)
                    // Are these needed here?
                    buildContext.LibPaths.Add("flex.swc");
                    buildContext.LibPaths.Add("core.swc");
                }
            }

            AS3Project project = PluginBase.CurrentProject as AS3Project;
            if (project != null)
            {
                if (project.CompileTargets.Count > 0)
                {
                    var targetFile = project.CompileTargets[0].Substring(0, project.CompileTargets[0].LastIndexOf('.'));
                    var localConfiguration = ProjectPaths.GetAbsolutePath(project.Directory, targetFile + "-config.xml");

                    if (localConfiguration != null && File.Exists(localConfiguration)) LoadConfigFile(localConfiguration, buildContext);
                }

                var projectConfig = project.CompilerOptions.LoadConfig;
                if (!string.IsNullOrEmpty(projectConfig) && File.Exists(projectConfig))
                    LoadConfigFile(projectConfig, buildContext);
            }

            CheckAdditionalCompilerOptions(buildContext);
            foreach (string file in addLocales)
            {
                string swcItem = sdkLocales + S + file;
                if (!File.Exists(swcItem)) swcItem = fallbackLocale + S + file;
                AddPath(swcItem);
            }
            buildContext.LibPaths.Reverse();
            foreach (string file in buildContext.LibPaths)  // I do not use AddRange to save some cycles, with LINQ I'd use Concat
            {
                if (File.Exists(file)) AddPath(file);
                else AddPath(sdkLibs + S + file);
            }
            buildContext.ExternalLibPaths.Reverse();
            foreach (string file in buildContext.ExternalLibPaths)
            {
                if (File.Exists(file)) AddPath(file);
                else AddPath(sdkLibs + S + file);
            }
            foreach (string file in buildContext.RslPaths)
            {
                if (File.Exists(file)) AddPath(file);
                else AddPath(sdkLibs + S + file);
            }
            foreach (string file in buildContext.ThemePaths)
            {
                if (File.Exists(file)) AddPath(file);
                else AddPath(sdkLibs + S + file);
            }

            // intrinsics (deprecated, excepted for FP10 Vector.<T>)
            string fp9cp = as3settings.AS3ClassPath + S + "FP9";
            AddPath(PathHelper.ResolvePath(fp9cp));
            if (majorVersion > 9)
            {
                for (int i = 10; i <= majorVersion; i++)
                {
                    string fp10cp = as3settings.AS3ClassPath + S + "FP" + i;
                    AddPath(PathHelper.ResolvePath(fp10cp));
                    for (int j = 1; j <= minorVersion; j++)
                    {
                        string fp101cp = as3settings.AS3ClassPath + S + "FP" + majorVersion + "." + minorVersion;
                        AddPath(PathHelper.ResolvePath(fp101cp));
                    }
                }
            }

            // add external pathes
            List<PathModel> initCP = classPath;
            classPath = new List<PathModel>();
            if (contextSetup.Classpath != null)
            {
                foreach (string cpath in contextSetup.Classpath)
                    AddPath(cpath.Trim());
            }

            // add library
            AddPath(PathHelper.LibraryDir + S + "AS3" + S + "classes");
            // add user pathes from settings
            if (settings.UserClasspath != null && settings.UserClasspath.Length > 0)
            {
                foreach (string cpath in settings.UserClasspath) AddPath(cpath.Trim());
            }

            // add initial pathes
            foreach (PathModel mpath in initCP) AddPath(mpath);

            // parse top-level elements
            InitTopLevelElements();
            if (cFile != null) UpdateTopLevelElements();

            // add current temporaty path
            if (temporaryPath != null)
            {
                string tempPath = temporaryPath;
                temporaryPath = null;
                SetTemporaryPath(tempPath);
            }
            FinalizeClasspath();
        }

        /// <summary>
        /// Look in current project configuration for user-defined config files and Flex themes or namespaces
        /// <param name="addLibs">
        /// The libraries that should be added to the project after scanning the options
        /// </param>
        /// <remarks>
        /// Implementation not 100% right, -load-config arguments should be before -theme ones in order to work correctly
        /// </remarks>
        /// </summary>
        private void CheckAdditionalCompilerOptions(BuildEnvironmentContext context)
        {
            AS3Project project = PluginBase.CurrentProject as AS3Project;
            if (project != null)
            {
                //-compiler.namespaces.namespace http://e4xu.googlecode.com run\manifest.xml
                if (project.CompilerOptions.Additional != null)
                    foreach (string line in project.CompilerOptions.Additional)
                    {
                        string temp = line.Trim();
                        if (temp.StartsWith("-compiler.namespaces.namespace") || temp.StartsWith("-namespace"))
                        {
                            int p = temp.IndexOf(' ');
                            if (p < 0) p = temp.IndexOf('=');
                            if (p < 0) continue;
                            temp = temp.Substring(p + 1).Trim();
                            p = temp.IndexOf(' ');
                            if (p < 0) p = temp.IndexOf(',');
                            if (p < 0) continue;
                            string uri = temp.Substring(0, p);
                            string path = temp.Substring(p + 1).Trim();
                            if (path.StartsWith("\"")) path = path.Substring(1, path.Length - 2);
                            MxmlFilter.AddManifest(uri, PathHelper.ResolvePath(path, project.Directory));
                        }
                        else if (temp.StartsWith("-load-config"))
                        {
                            bool append = false;
                            int p = temp.IndexOf("+=");
                            if (p >= 0) append = true;
                            else
                            {
                                p = temp.IndexOf('=');
                                if (p < 0) p = temp.IndexOf(' ');
                                if (p < 0) continue;
                            }
                            string path;

                            if (append)
                                path = temp.Substring(p + 2).Trim();
                            else
                            {
                                context.Clear();
                                path = temp.Substring(p + 1).Trim();
                            }
                            if (path.StartsWith("\"")) path = path.Substring(1, path.Length - 2);
                            LoadConfigFile(ProjectPaths.GetAbsolutePath(project.Directory, path), context);
                        }
                        else if (temp.StartsWith("-theme") || temp.StartsWith("-compiler.theme"))
                        {
                            bool append = false;
                            int p = temp.IndexOf("+=");
                            if (p >= 0) append = true;
                            else
                            {
                                p = temp.IndexOf('=');
                                if (p < 0) p = temp.IndexOf(' ');
                                if (p < 0) continue;
                            }
                            string path;

                            if (append)
                                path = temp.Substring(p + 2).Trim();
                            else
                            {
                                context.ThemePaths.Clear();
                                path = temp.Substring(p + 1).Trim();
                            }

                            if (path.StartsWith("\"")) path = path.Substring(1, path.Length - 2);
                            AddPathElement(ProjectPaths.GetAbsolutePath(project.Directory, path), context.ThemePaths);
                        }
                    }
            }
        }

        private void LoadConfigFile(string file, BuildEnvironmentContext buildContext)
        {
            try
            {
                string fileDirectory = Path.GetDirectoryName(file);
                bool rslChecked = false;

                using (var reader = XmlReader.Create(file))
                {
                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element) continue;

                        if (reader.Name == "compiler")
                        {
                            reader.ReadStartElement();

                            while (reader.Name != "compiler")
                            {
                                if (reader.NodeType != XmlNodeType.Element)
                                {
                                    reader.Read();
                                    continue;
                                }

                                switch (reader.Name)
                                {
                                    case "external-library-path":
                                        if (reader.GetAttribute("append") != "true")
                                            buildContext.ExternalLibPaths.Clear();
                                        AddConfigPathElements(reader, fileDirectory, buildContext.ExternalLibPaths);

                                        break;

                                    case "library-path":
                                        if (reader.GetAttribute("append") != "true")
                                            buildContext.LibPaths.Clear();
                                        AddConfigPathElements(reader, fileDirectory, buildContext.LibPaths);

                                        break;

                                    case "namespaces":
                                        AddConfigManifestElements(reader, fileDirectory);

                                        break;

                                    case "theme":
                                        if (reader.GetAttribute("append") != "true")
                                            buildContext.ThemePaths.Clear();
                                        AddConfigPathElements(reader, fileDirectory, buildContext.ThemePaths);

                                        break;
                                }

                                reader.Read();
                            }
                        }
                        else if (reader.Name == "runtime-shared-library-path")
                        {
                            if (!rslChecked)
                            {
                                if (reader.GetAttribute("append") != "true")
                                    buildContext.RslPaths.Clear();
                                rslChecked = true;
                            }
                            AddConfigPathElements(reader, fileDirectory, buildContext.RslPaths);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: Display message to the user
            }
        }

        private void AddConfigManifestElements(XmlReader reader, string basePath)
        {
            const string nameSpaceTag = "namespace";
            var eon = reader.Name;
            reader.ReadStartElement();
            while (reader.Name != eon)
            {
                if (reader.NodeType != XmlNodeType.Element && reader.Name != nameSpaceTag)
                {
                    reader.Read();
                    continue;
                }

                string manifestUri = null;
                string manifestPath = null;

                reader.ReadStartElement();
                while (reader.Name != nameSpaceTag)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "uri")
                            manifestUri = reader.ReadElementString();
                        else if (reader.Name == "manifest")
                            manifestPath = reader.ReadElementString();
                    }

                    reader.Read();
                }

                if (!string.IsNullOrEmpty(manifestUri) && !string.IsNullOrEmpty(manifestPath))
                {
                    manifestPath = ProjectPaths.GetAbsolutePath(basePath, manifestPath);
                    if (File.Exists(manifestPath)) MxmlFilter.AddManifest(manifestUri, manifestPath);
                }
                reader.Read();
            }
        }

        private void AddConfigPathElements(XmlReader reader, string basePath, List<string> addLibs)
        {
            var eon = reader.Name;
            reader.ReadStartElement();
            while (reader.Name != eon)
            {
                if (reader.NodeType != XmlNodeType.Element && reader.Name != "path-element" && reader.Name != "filename")
                {
                    reader.Read();
                    continue;
                }

                var value = reader.ReadElementString();

                AddPathElement(ProjectPaths.GetAbsolutePath(basePath, value), addLibs);

                reader.Read();
            }
        }

        private void AddPathElement(string path, List<string> addLibs)
        {
            if (path.Contains("{targetPlayer"))
            {
                path = path.Replace("{targetPlayerMajorVersion}", majorVersion.ToString())
                             .Replace("{targetPlayerMinorVersion}", minorVersion.ToString());
            }

            if (path.Contains("{locale}"))
            {
                var s = Path.DirectorySeparatorChar;
                // Adding first global en_US files, and then selected local ones may resolve to the right ones at the end, 
                //but this method also works and we save time parsing unneeded files
                var localeDic = new Dictionary<string, string>();
                if (path.EndsWith("frameworks" + s + "locale" + s + "{locale}"))
                {
                    // Let's check for available fallback libraries
                    string fallbackLocale =
                        PathHelper.ResolvePath(PathHelper.ToolDir + s + "flexlibs" + s + "frameworks" + s + "locale" +
                                               s + "en_US");

                    GetLocaleLibraries(fallbackLocale, localeDic);
                }

                var tmpValue = path.Replace("{locale}", "en_US");

                GetLocaleLibraries(tmpValue, localeDic);

                if (PluginBase.MainForm.Settings.LocaleVersion != LocaleVersion.en_US)
                {
                    tmpValue = path.Replace("{locale}", PluginBase.MainForm.Settings.LocaleVersion.ToString());

                    GetLocaleLibraries(tmpValue, localeDic);
                }

                foreach (var lib in localeDic.Values)
                    addLibs.Add(lib);
            }
            else
            {
                if (File.Exists(path))
                {
                    if (Path.GetExtension(path).ToUpperInvariant() != ".CSS")
                        addLibs.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    foreach (string library in Directory.GetFiles(path, "*.swc"))
                    {
                        addLibs.Add(library);
                    }
                }
            }
        }

        private void GetLocaleLibraries(string source, Dictionary<string, string> localeDic)
        {
            if (File.Exists(source))
            {
                localeDic[Path.GetFileName(source)] = source;
            }
            else if (Directory.Exists(source))
            {
                foreach (var lib in Directory.GetFiles(source, "*.swc"))
                {
                    localeDic[Path.GetFileName(lib)] = lib;
                }
            }
        }

        /// <summary>
        /// Find any playerglobal.swc
        /// </summary>
        private string MatchPlayerGlobalAny(ref int majorVersion, ref int minorVersion, string sdkLibs)
        {
            char S = Path.DirectorySeparatorChar;
            string libPlayer = sdkLibs + S + "player";
            string playerglobal = null;
            for (int i = minorVersion; i >= 0; i--)
            {
                string version = majorVersion + "." + i;
                if (Directory.Exists(libPlayer + S + version))
                {
                    minorVersion = i;
                    return libPlayer + S + version + S + "playerglobal.swc";
                }
            }
            if (playerglobal == null && Directory.Exists(libPlayer + S + majorVersion))
                playerglobal = "player" + S + majorVersion + S + "playerglobal.swc";

            if (playerglobal == null && majorVersion > 9)
            {
                int tempMajor = majorVersion - 1;
                int tempMinor = 9;
                playerglobal = MatchPlayerGlobalAny(ref tempMajor, ref tempMinor, sdkLibs);
                if (playerglobal != null)
                {
                    majorVersion = tempMajor;
                    minorVersion = tempMinor;
                    return playerglobal;
                }
            }

            return playerglobal;
        }

        /// <summary>
        /// Find version-matching playerglobal.swc
        /// </summary>
        private string MatchPlayerGlobalExact(int majorVersion, int minorVersion, string sdkLibs)
        {
            string playerglobal = null;
            char S = Path.DirectorySeparatorChar;
            string libPlayer = sdkLibs + S + "player";
            if (Directory.Exists(libPlayer + S + majorVersion + "." + minorVersion))
                playerglobal = libPlayer + S + majorVersion + "." + minorVersion + S + "playerglobal.swc";
            if (minorVersion == 0 && Directory.Exists(libPlayer + S + majorVersion))
                playerglobal = libPlayer + S + majorVersion + S + "playerglobal.swc";
            return playerglobal;
        }

        /// <summary>
        /// Build a list of file mask to explore the classpath
        /// </summary>
        public override string[] GetExplorerMask()
        {
            string[] mask = as3settings.AS3FileTypes;
            if (mask == null || mask.Length == 0 || (mask.Length == 1 && mask[1] == ""))
            {
                as3settings.AS3FileTypes = mask = new string[] { "*.as", "*.mxml" };
                return mask;
            }
            else
            {
                List<string> patterns = new List<string>();
                for (int i = 0; i < mask.Length; i++)
                {
                    string m = mask[i];
                    if (string.IsNullOrEmpty(m)) continue;
                    if (m[1] != '.' && m[0] != '.') m = '.' + m;
                    if (m[0] != '*') m = '*' + m;
                    patterns.Add(m);
                }
                return patterns.ToArray();
            }
        }

        /// <summary>
        /// Parse a packaged library file
        /// </summary>
        /// <param name="path">Models owner</param>
        public override void ExploreVirtualPath(PathModel path)
        {
            if (path.WasExplored)
            {
                if (MxmlFilter.HasCatalog(path.Path)) MxmlFilter.AddCatalog(path.Path);

                if (path.FilesCount != 0) // already parsed
                    return;
            }

            try
            {
                if (File.Exists(path.Path) && !path.WasExplored)
                {
                    bool isRefresh = path.FilesCount > 0;
                    //TraceManager.AddAsync("parse " + path.Path);
                    lock (path)
                    {
                        path.WasExplored = true;
                        SwfOp.ContentParser parser = new SwfOp.ContentParser(path.Path);
                        parser.Run();
                        AbcConverter.Convert(parser, path, this);
                    }
                    // reset FCSH
                    if (isRefresh)
                    {
                        EventManager.DispatchEvent(this,
                            new DataEvent(EventType.Command, "ProjectManager.RestartFlexShell", path.Path));
                    }
                }
            }
            catch (Exception ex)
            {
                string message = TextHelper.GetString("Info.ExceptionWhileParsing");
                TraceManager.AddAsync(message + " " + path.Path);
                TraceManager.AddAsync(ex.Message);
                TraceManager.AddAsync(ex.StackTrace);
            }
        }

        /// <summary>
        /// Delete current class's cached file
        /// </summary>
        public override void RemoveClassCompilerCache()
        {
            // not implemented - is there any?
        }

        /// <summary>
        /// Create a new file model without parsing file
        /// </summary>
        /// <param name="fileName">Full path</param>
        /// <returns>File model</returns>
        public override FileModel CreateFileModel(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return new FileModel(fileName);

            fileName = PathHelper.GetLongPathName(fileName);
            if (mxmlEnabled && fileName.EndsWith(".mxml", StringComparison.OrdinalIgnoreCase))
            {
                FileModel nFile = new FileModel(fileName);
                nFile.Context = this;
                nFile.HasFiltering = true;
                return nFile;
            }
            else return base.CreateFileModel(fileName);
        }

        private void GuessPackage(string fileName, FileModel nFile)
        {
            foreach (PathModel aPath in classPath)
                if (fileName.StartsWith(aPath.Path, StringComparison.OrdinalIgnoreCase))
                {
                    string local = fileName.Substring(aPath.Path.Length);
                    char sep = Path.DirectorySeparatorChar;
                    local = local.Substring(0, local.LastIndexOf(sep)).Replace(sep, '.');
                    nFile.Package = local.Length > 0 ? local.Substring(1) : "";
                    nFile.HasPackage = true;
                }
        }

        /// <summary>
        /// Build the file DOM
        /// </summary>
        /// <param name="filename">File path</param>
        protected override void GetCurrentFileModel(string fileName)
        {
            base.GetCurrentFileModel(fileName);
        }

        /// <summary>
        /// Refresh the file model
        /// </summary>
        /// <param name="updateUI">Update outline view</param>
        public override void UpdateCurrentFile(bool updateUI)
        {
            if (mxmlEnabled && cFile != null && cFile != FileModel.Ignore
                && cFile.FileName.EndsWith(".mxml", StringComparison.OrdinalIgnoreCase))
                cFile.HasFiltering = true;
            base.UpdateCurrentFile(updateUI);

            if (cFile.HasFiltering)
            {
                MxmlComplete.mxmlContext = mxmlFilterContext;
                MxmlComplete.context = this;
            }
        }

        /// <summary>
        /// Update the class/member context for the given line number.
        /// Be carefull to restore the context after calling it with a custom line number
        /// </summary>
        /// <param name="line"></param>
        public override void UpdateContext(int line)
        {
            base.UpdateContext(line);
        }

        /// <summary>
        /// Called if a FileModel needs filtering
        /// - define inline AS3 ranges
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public override string FilterSource(string fileName, string src)
        {
            mxmlFilterContext = new MxmlFilterContext();
            return MxmlFilter.FilterSource(Path.GetFileNameWithoutExtension(fileName), src, mxmlFilterContext);
        }

        /// <summary>
        /// Called if a FileModel needs filtering
        /// - modify parsed model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override void FilterSource(FileModel model)
        {
            GuessPackage(model.FileName, model);
            if (mxmlFilterContext != null) MxmlFilter.FilterSource(model, mxmlFilterContext);
        }
        #endregion

        #region syntax checking

        internal void OnFileOperation(NotifyEvent e)
        {
            timerCheck.Stop();
            foreach (ITabbedDocument doc in PluginBase.MainForm.Documents)
                if (doc.FileName == fileWithSquiggles) ClearSquiggles(doc.SciControl);
        }

        public override void TrackTextChange(ScintillaNet.ScintillaControl sender, int position, int length, int linesAdded)
        {
            base.TrackTextChange(sender, position, length, linesAdded);
            if (as3settings != null && !as3settings.DisableLiveChecking && IsFileValid)
            {
                timerCheck.Stop();
                timerCheck.Start();
            }
        }

        private void timerCheck_Elapsed(object sender, ElapsedEventArgs e)
        {
            BackgroundSyntaxCheck();
        }

        /// <summary>
        /// Checking syntax of current file
        /// </summary>
        private void BackgroundSyntaxCheck()
        {
            if (!IsFileValid) return;

            ScintillaNet.ScintillaControl sci = CurSciControl;
            if (sci == null) return;
            ClearSquiggles(sci);

            string src = CurSciControl.Text;
            string sdk = PluginBase.CurrentProject != null && PluginBase.CurrentProject.Language == "as3"
                    ? PluginBase.CurrentProject.CurrentSDK
                    : as3settings.GetDefaultSDK().Path;
            FlexShells.Instance.CheckAS3(CurrentFile, sdk, src);
        }

        private void AddSquiggles(ScintillaNet.ScintillaControl sci, int line, int start, int end)
        {
            if (sci == null) return;
            fileWithSquiggles = CurrentFile;
            int position = sci.PositionFromLine(line) + start;
            sci.AddHighlight(2, (int)ScintillaNet.Enums.IndicatorStyle.Squiggle, 0x000000ff, position, end - start);
        }

        private void ClearSquiggles(ScintillaNet.ScintillaControl sci)
        {
            if (sci == null) return;
            try
            {
                sci.RemoveHighlights(2);
            }
            finally
            {
                fileWithSquiggles = null;
            }
        }

        private void FlexShell_SyntaxError(string error)
        {
            if (!IsFileValid) return;
            Match m = re_syntaxError.Match(error);
            if (!m.Success) return;

            ITabbedDocument document = PluginBase.MainForm.CurrentDocument;
            if (document == null || !document.IsEditable) return;

            ScintillaNet.ScintillaControl sci = document.SplitSci1;
            ScintillaNet.ScintillaControl sci2 = document.SplitSci2;

            if (m.Groups["filename"].Value != CurrentFile) return;
            try
            {
                int line = int.Parse(m.Groups["line"].Value) - 1;
                if (sci.LineCount < line) return;
                int start = MBSafeColumn(sci, line, int.Parse(m.Groups["col"].Value) - 1);
                if (line == sci.LineCount && start == 0 && line > 0) start = -1;
                AddSquiggles(sci, line, start, start + 1);
                AddSquiggles(sci2, line, start, start + 1);
            }
            catch { }
        }

        /// <summary>
        /// Convert multibyte column to byte length
        /// </summary>
        private int MBSafeColumn(ScintillaNet.ScintillaControl sci, int line, int length)
        {
            String text = sci.GetLine(line) ?? "";
            length = Math.Min(length, text.Length);
            return sci.MBSafeTextLength(text.Substring(0, length));
        }

        #endregion

        #region class resolution
        /// <summary>
        /// Evaluates the visibility of one given type from another.
        /// Caller is responsible of calling ResolveExtends() on 'inClass'
        /// </summary>
        /// <param name="inClass">Completion context</param>
        /// <param name="withClass">Completion target</param>
        /// <returns>Completion visibility</returns>
        public override Visibility TypesAffinity(ClassModel inClass, ClassModel withClass)
        {
            if (inClass == null || withClass == null) return Visibility.Public;
            // same file
            if (inClass.InFile == withClass.InFile)
                return Visibility.Public | Visibility.Internal | Visibility.Protected | Visibility.Private;

            // same package
            Visibility acc = Visibility.Public;
            if (inClass.InFile.Package == withClass.InFile.Package) acc |= Visibility.Internal;

            // inheritance affinity
            ClassModel tmp = inClass;
            while (!tmp.IsVoid())
            {
                if (tmp.Type == withClass.Type)
                {
                    acc |= Visibility.Protected;
                    break;
                }
                tmp = tmp.Extends;
            }
            return acc;
        }

        /// <summary>
        /// Return the full project classes list
        /// </summary>
        /// <returns></returns>
        public override MemberList GetAllProjectClasses()
        {
            // from cache
            if (!completionCache.IsDirty && completionCache.AllTypes != null)
                return completionCache.AllTypes;

            MemberList fullList = new MemberList();
            ClassModel aClass;
            MemberModel item;
            // public & internal classes
            string package = CurrentModel.Package;
            foreach (PathModel aPath in classPath) if (aPath.IsValid && !aPath.Updating)
                {
                    aPath.ForeachFile((aFile) =>
                    {
                        if (!aFile.HasPackage)
                            return true; // skip

                        aClass = aFile.GetPublicClass();
                        if (!aClass.IsVoid() && aClass.IndexType == null)
                        {
                            if (aClass.Access == Visibility.Public
                                || (aClass.Access == Visibility.Internal && aFile.Package == package))
                            {
                                item = aClass.ToMemberModel();
                                item.Name = item.Type;
                                fullList.Add(item);
                            }
                        }
                        if (aFile.Package.Length > 0 && aFile.Members.Count > 0)
                        {
                            foreach (MemberModel member in aFile.Members)
                            {
                                item = member.Clone() as MemberModel;
                                item.Name = aFile.Package + "." + item.Name;
                                fullList.Add(item);
                            }
                        }
                        else if (aFile.Members.Count > 0)
                        {
                            foreach (MemberModel member in aFile.Members)
                            {
                                item = member.Clone() as MemberModel;
                                fullList.Add(item);
                            }
                        }
                        return true;
                    });
                }
            // void
            fullList.Add(new MemberModel(features.voidKey, features.voidKey, FlagType.Class | FlagType.Intrinsic, 0));
            // private classes
            fullList.Add(GetPrivateClasses());

            // in cache
            fullList.Sort();
            completionCache.AllTypes = fullList;
            return fullList;
        }

        public override bool OnCompletionInsert(ScintillaNet.ScintillaControl sci, int position, string text, char trigger)
        {
            if (text == "Vector")
            {
                string insert = null;
                string line = sci.GetLine(sci.LineFromPosition(position));
                Match m = Regex.Match(line, @"\svar\s+(?<varname>.+)\s*:\s*Vector\.<(?<indextype>.+)(?=(>\s*=))");
                if (m.Success)
                {
                    insert = String.Format(".<{0}>", m.Groups["indextype"].Value);
                }
                else
                {
                    m = Regex.Match(line, @"\s*=\s*new");
                    if (m.Success)
                    {
                        ASResult result = ASComplete.GetExpressionType(sci, sci.PositionFromLine(sci.LineFromPosition(position)) + m.Index);
                        if (result != null && !result.IsNull() && result.Member != null && result.Member.Type != null)
                        {
                            m = Regex.Match(result.Member.Type, @"(?<=<).+(?=>)");
                            if (m.Success)
                            {
                                insert = String.Format(".<{0}>", m.Value);
                            }
                        }
                    }
                    if (insert == null)
                    {
                        if (trigger == '.' || trigger == '(') return true;
                        insert = ".<>";
                        sci.InsertText(position + text.Length, insert);
                        sci.CurrentPos = position + text.Length + 2;
                        sci.SetSel(sci.CurrentPos, sci.CurrentPos);
                        ASComplete.HandleAllClassesCompletion(sci, "", false, true);
                        return true;
                    }
                }
                if (insert == null) return false;
                if (trigger == '.')
                {
                    sci.InsertText(position + text.Length, insert.Substring(1));
                    sci.CurrentPos = position + text.Length;
                }
                else
                {
                    sci.InsertText(position + text.Length, insert);
                    sci.CurrentPos = position + text.Length + insert.Length;
                }
                sci.SetSel(sci.CurrentPos, sci.CurrentPos);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a type is already in the file's imports
        /// Throws an Exception if the type name is ambiguous 
        /// (ie. same name as an existing import located in another package)
        /// </summary>
        /// <param name="member">Element to search in imports</param>
        /// <param name="atLine">Position in the file</param>
        public override bool IsImported(MemberModel member, int atLine)
        {
            FileModel cFile = ASContext.Context.CurrentModel;
            // same package is auto-imported
            string package = member.Type.Length > member.Name.Length
                ? member.Type.Substring(0, member.Type.Length - member.Name.Length - 1)
                : "";
            if (package == cFile.Package) return true;
            return base.IsImported(member, atLine);
        }

        /// <summary>
        /// Retrieves a class model from its name
        /// </summary>
        /// <param name="cname">Class (short or full) name</param>
        /// <param name="inClass">Current file</param>
        /// <returns>A parsed class or an empty ClassModel if the class is not found</returns>
        public override ClassModel ResolveType(string cname, FileModel inFile)
        {
            // handle generic types
            if (cname != null && cname.IndexOf('<') > 0)
            {
                Match genType = re_genericType.Match(cname);
                if (genType.Success)
                    return ResolveGenericType(genType.Groups["gen"].Value, genType.Groups["type"].Value, inFile);
                else return ClassModel.VoidClass;
            }
            return base.ResolveType(cname, inFile);
        }

        /// <summary>
        /// Retrieve/build typed copies of generic types
        /// </summary>
        private ClassModel ResolveGenericType(string baseType, string indexType, FileModel inFile)
        {
            ClassModel originalClass = base.ResolveType(baseType, inFile);
            if (originalClass.IsVoid()) return originalClass;

            ClassModel indexClass = ResolveType(indexType, inFile);
            if (indexClass.IsVoid()) return originalClass;
            indexType = indexClass.QualifiedName;

            FileModel aFile = originalClass.InFile;
            // is the type already cloned?
            foreach (ClassModel otherClass in aFile.Classes)
                if (otherClass.IndexType == indexType) return otherClass;

            // clone the type
            ClassModel aClass = originalClass.Clone() as ClassModel;

            aClass.Name = baseType + ".<" + indexType + ">";
            aClass.IndexType = indexType;

            string typed = "<" + indexType + ">";
            foreach (MemberModel member in aClass.Members)
            {
                if (member.Name == baseType) member.Name = baseType.Replace("<T>", typed);
                if (member.Type != null && member.Type.IndexOf('T') >= 0)
                {
                    if (member.Type == "T") member.Type = indexType;
                    else member.Type = member.Type.Replace("<T>", typed);
                }
                if (member.Parameters != null)
                {
                    foreach (MemberModel param in member.Parameters)
                    {
                        if (param.Type != null && param.Type.IndexOf('T') >= 0)
                        {
                            if (param.Type == "T") param.Type = indexType;
                            else param.Type = param.Type.Replace("<T>", typed);
                        }
                    }
                }
            }

            aFile.Classes.Add(aClass);
            return aClass;
        }

        protected MemberList GetPrivateClasses()
        {
            MemberList list = new MemberList();
            // private classes
            foreach (ClassModel model in cFile.Classes)
                if (model.Access == Visibility.Private)
                {
                    MemberModel item = model.ToMemberModel();
                    item.Type = item.Name;
                    item.Access = Visibility.Private;
                    list.Add(item);
                }
            // 'Class' members
            if (cClass != null)
                foreach (MemberModel member in cClass.Members)
                    if (member.Type == "Class") list.Add(member);
            return list;
        }

        /// <summary>
        /// Prepare AS3 intrinsic known vars/methods/classes
        /// </summary>
        protected override void InitTopLevelElements()
        {
            string filename = "toplevel.as";
            topLevel = new FileModel(filename);

            if (topLevel.Members.Search("this", 0, 0) == null)
                topLevel.Members.Add(new MemberModel("this", "", FlagType.Variable | FlagType.Intrinsic, Visibility.Public));
            if (topLevel.Members.Search("super", 0, 0) == null)
                topLevel.Members.Add(new MemberModel("super", "", FlagType.Variable | FlagType.Intrinsic, Visibility.Public));
            if (topLevel.Members.Search(features.voidKey, 0, 0) == null)
                topLevel.Members.Add(new MemberModel(features.voidKey, "", FlagType.Intrinsic, Visibility.Public));
            topLevel.Members.Sort();
        }

        #endregion

        #region Command line compiler

        /// <summary>
        /// Retrieve the context's default compiler path
        /// </summary>
        public override string GetCompilerPath()
        {
            return as3settings.GetDefaultSDK().Path ?? "Tools\\flexsdk";
        }

        /// <summary>
        /// Check current file's syntax
        /// </summary>
        public override void CheckSyntax()
        {
            if (IsFileValid && cFile.InlinedIn == null)
            {
                PluginBase.MainForm.CallCommand("Save", null);

                string sdk = PluginBase.CurrentProject != null
                    ? PluginBase.CurrentProject.CurrentSDK
                    : PathHelper.ResolvePath(as3settings.GetDefaultSDK().Path);
                FlexShells.Instance.CheckAS3(cFile.FileName, sdk);
            }
        }

        /// <summary>
        /// Run MXMLC compiler in the current class's base folder with current classpath
        /// </summary>
        /// <param name="append">Additional comiler switches</param>
        public override void RunCMD(string append)
        {
            if (!IsCompilationTarget())
            {
                MessageBar.ShowWarning(TextHelper.GetString("Info.InvalidClass"));
                return;
            }

            string command = (append ?? "") + " -- " + CurrentFile;
            FlexShells.Instance.RunMxmlc(command, as3settings.GetDefaultSDK().Path);
        }

        private bool IsCompilationTarget()
        {
            return (!MainForm.CurrentDocument.IsUntitled && CurrentModel.Version >= 3);
        }

        /// <summary>
        /// Calls RunCMD with additional parameters taken from the classes @mxmlc doc tag
        /// </summary>
        public override bool BuildCMD(bool failSilently)
        {
            if (!IsCompilationTarget())
            {
                MessageBar.ShowWarning(TextHelper.GetString("Info.InvalidClass"));
                return false;
            }

            MainForm.CallCommand("SaveAllModified", null);

            string sdk = PluginBase.CurrentProject != null
                    ? PluginBase.CurrentProject.CurrentSDK
                    : as3settings.GetDefaultSDK().Path;
            FlexShells.Instance.QuickBuild(CurrentModel, sdk, failSilently, as3settings.PlayAfterBuild);
            return true;
        }
        #endregion

        #region Completion Helpers
        public void HandleMetaCompletion(ASExpr expression)
        {
            int position = expression.Position;
            int line = CurSciControl.LineFromPosition(position);
            MemberModel adornatedMember = null;
            if (mxmlFilterContext != null)
            {
                MxmlInlineRange containerTag = null;
                foreach (var tag in mxmlFilterContext.Outline)
                {
                    if (tag.Start > position) break;

                    if (tag.Start <= position && tag.End >= position)
                    {
                        containerTag = tag;
                    }
                }

                if (containerTag != null)
                {
                    // TODO: Namespace may not be fx or mx
                    string metaTag = (mxmlFilterContext.DocumentType == MxmlFilterContext.FlexDocumentType.Flex3) ? "mx:Metadata" : "fx:Metadata";
                    // Although we may not be in the main class metadata tag, any class will do
                    if (containerTag.Tag == metaTag) adornatedMember = mxmlFilterContext.Model.Classes[0];
                }
            }

            if (adornatedMember == null)
            {
                for (int i = cFile.Classes.Count - 1; i >= 0; i--)
                {
                    var tmpClass = cFile.Classes[i];

                    if (tmpClass.LineTo < line) continue;

                    MemberModel tmpMember = null;
                    if (tmpClass.LineFrom >= line) 
                        tmpMember = tmpClass;
                    else
                        foreach (MemberModel member in tmpClass.Members)
                        {
                            if (member.LineFrom >= line)
                            {
                                tmpMember = member;
                                break;
                            }
                        }

                    if (tmpMember != null && (adornatedMember == null || tmpMember.LineFrom <= adornatedMember.LineFrom))
                        adornatedMember = tmpMember;
                }
            }

            MetaEntry.DecoratableField fieldType = 0;
            if (adornatedMember == null) fieldType = MetaEntry.DecoratableField.Attribute;
            else
            {
                if ((adornatedMember.Flags & FlagType.Class) > 0)
                {
                    fieldType = MetaEntry.DecoratableField.Class;
                }
                else if ((adornatedMember.Flags & FlagType.Function) > 0)
                {
                    fieldType = MetaEntry.DecoratableField.Function;
                }
                else if ((adornatedMember.Flags & (FlagType.Variable | FlagType.Getter | FlagType.Setter)) > 0)
                {
                    fieldType = MetaEntry.DecoratableField.Attribute;
                }
            }

            List<ICompletionListItem> list = null;
            if (expression.Separator == ']')
            {
                list = new List<ICompletionListItem>();
                foreach (var meta in as3settings.AS3Meta)
                {
                    if ((meta.DecoratableFields & fieldType) > 0)
                        list.Add(new MetaItem(meta));
                }
            }
            else if (expression.coma == ComaExpression.MetaDataParameter)
            {
                if (expression.WordBefore == null) // TODO: To fix and remove
                    return;

                list = new List<ICompletionListItem>();
                MetaEntry metaEntry = null;
                foreach (var meta in as3settings.AS3Meta)
                {
                    if ((meta.DecoratableFields & fieldType) > 0 && meta.Label == expression.WordBefore)
                    {
                        metaEntry = meta;
                    }
                }

                if (metaEntry == null || metaEntry.Fields == null || metaEntry.Fields.Count == 0) return;

                foreach (var field in metaEntry.Fields)
                {
                    list.Add(new DeclarationItem(field.Name));
                }

            }

            if (list == null || list.Count == 0) return;

            list.Sort(new CompletionItemComparer());

            CompletionList.Show(list, false, expression.Value);
        }

        /// <summary>
        /// Meta completion list item
        /// </summary>
        public class MetaItem : ICompletionListItem
        {
            private MetaEntry meta;

            public MetaItem(MetaEntry meta)
            {
                this.meta = meta;
            }

            public string Label
            {
                get
                {
                    StringBuilder retVal = new StringBuilder(meta.Label);

                    if (meta.Fields != null && meta.Fields.Count > 0)
                    {
                        retVal.Append('(');
                        foreach (var field in meta.Fields)
                        {
                            if (!field.Mandatory)
                            {
                                retVal.Append('[').Append(field.Name).Append(']');
                            } else retVal.Append(field.Name);
                            retVal.Append(", ");
                        }
                        retVal.Remove(retVal.Length - 2, 2);
                        retVal.Append(')');
                    }

                    return retVal.ToString();
                }
            }
            public string Description
            {
                get
                {
                    if (meta.Description != null && meta.Description.Count > 0)
                    {
                        var culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
                        string description;

                        if (!meta.Description.TryGetValue(culture.Name, out description) &&
                            !meta.Description.TryGetValue(culture.TwoLetterISOLanguageName, out description) &&
                            !meta.Description.TryGetValue(meta.DefaultDescriptionKey, out description))
                            
                            return null;

                        return description;
                    }
                    return null;
                }
            }

            public System.Drawing.Bitmap Icon
            {
                get { return (System.Drawing.Bitmap)Panel.GetIcon(ASCompletion.PluginUI.ICON_DECLARATION); }
            }

            public string Value
            {
                get
                {
                    var retVal = new StringBuilder(meta.Label);
                    if (meta.Fields != null)
                    {
                        if (meta.Fields.Count > 1)
                        {
                            int fieldCount = 0;
                            foreach (var field in meta.Fields)
                            {
                                if (field.Mandatory)
                                {
                                    if (fieldCount == 0) retVal.Append('(');
                                    retVal.Append(field.Name).Append("=\"\", ");
                                    fieldCount++;
                                }
                            }

                            if (fieldCount > 0)
                            {
                                retVal.Remove(retVal.Length - 2, 2);
                                retVal.Append(')');
                            }
                        }
                        else if (meta.Fields.Count == 1 && meta.Fields[0].Mandatory)
                        {
                            retVal.Append("(\"\")");
                        }
                    }
                    retVal.Append(']');
                    return retVal.ToString();
                }
            }
        }
        #endregion

        private class BuildEnvironmentContext
        {
            public List<string> LibPaths = new List<string>();
            public List<string> ExternalLibPaths = new List<string>();
            public List<string> ThemePaths = new List<string>();
            public List<string> RslPaths = new List<string>();

            public void Clear()
            {
                LibPaths.Clear();
                ExternalLibPaths.Clear();
                ThemePaths.Clear();
                RslPaths.Clear();
            }
        }
    }
}
