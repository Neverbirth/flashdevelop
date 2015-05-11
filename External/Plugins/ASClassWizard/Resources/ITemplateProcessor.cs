using ASCompletion.Model;

namespace ASClassWizard.Resources
{
    public interface ITemplateProcessor
    {

        ClassModel ClassModel { get; set; }
        string TargetFile { get; set; }

        string ProcessFileTemplate(string args);
        void FileSwitched();

    }
}
