using System.Collections.Generic;
using System.Text;
using ASCompletion.Context;
using ASCompletion.Model;
using PluginCore;
using PluginCore.Utilities;

namespace ASClassWizard.Resources
{
    public class MxmlClassOptions : ITemplateProcessor
    {

        public string TargetFile { get; set; }
        public ClassModel ClassModel { get; set; }

        public string ClassModelPrefix;
        public Dictionary<string, string> Namespaces;
        public bool ImplementInterfaces;

        public virtual string ProcessFileTemplate(string args)
        {
            int eolMode = (int)PluginBase.MainForm.Settings.EOLMode;
            string lineBreak = LineEndDetector.GetNewLineMarker(eolMode);
            var namespaces = new StringBuilder();
            foreach (var ns in Namespaces)
            {
                if (namespaces.Length > 0)
                {
                    namespaces.Append(lineBreak);
                    AddNewLineSpaces(namespaces);
                }
                namespaces.Append("xmlns");
                if (ns.Key != "")
                    namespaces.Append(":").Append(ns.Key);
                namespaces.Append("=\"").Append(ns.Value).Append("\"");
            }

            var implements = new StringBuilder();
            if (ClassModel.Implements != null && ClassModel.Implements.Count > 0)
            {
                implements.Append(lineBreak);
                AddNewLineSpaces(implements);
                implements.Append("implements=\"");
                foreach (var implement in ClassModel.Implements)
                {
                    implements.Append(implement).Append(", ");
                }
                implements.Remove(implements.Length - 2, 2);
                implements.Append("\"");
            }

            args = args.Replace("$(ExtendsNs)", ClassModelPrefix);
            args = args.Replace("$(Extends)", ClassModel.Type);
            args = args.Replace("$(Namespaces)", namespaces.ToString());
            args = args.Replace("$(Implements)", implements.ToString());

            return args;
        }

        private void AddNewLineSpaces(StringBuilder sb)
        {
            for (int i = 0, count = ClassModelPrefix.Length + ClassModel.Type.Length + 3; i < count; i++)
                sb.Append(' ');
        }

        public void FileSwitched()
        {
            if (ClassModel.Implements == null || !ImplementInterfaces) return;

            ASContext.Context.CurrentModel.Check();
            // Update mxml context
            ASContext.Context.UpdateCurrentFile(false);

            foreach (var implement in ClassModel.Implements)
            {
                AS3Context.MxmlGenerator.GenerateJob(ASCompletion.Completion.GeneratorJobType.ImplementInterface, null, null, implement);
                ASContext.Context.CurrentModel.Check();
            }
        }

    }
}
