using System.IO;
using PluginCore.Helpers;

namespace AS3Context.Utils
{
    internal static class ResourceUtils
    {

        static public string CheckResource(string resName, string fileName)
        {
            string path = Path.Combine(PathHelper.DataDir, "AS3Context");
            string fullPath = Path.Combine(path, fileName);
            if (!File.Exists(fullPath))
            {
                string id = "AS3Context.Resources." + resName;
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (BinaryReader br = new BinaryReader(assembly.GetManifestResourceStream(id)))
                {
                    using (FileStream bw = File.Create(fullPath))
                    {
                        byte[] buffer = br.ReadBytes(1024);
                        while (buffer.Length > 0)
                        {
                            bw.Write(buffer, 0, buffer.Length);
                            buffer = br.ReadBytes(1024);
                        }
                        bw.Close();
                    }
                    br.Close();
                }
            }
            return fullPath;
        }
    }
}
