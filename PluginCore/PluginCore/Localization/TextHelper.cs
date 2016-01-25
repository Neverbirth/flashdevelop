using System;
using System.Reflection;
using System.Resources;
using PluginCore.Managers;

namespace PluginCore.Localization
{
    public class TextHelper
    {
        private static ResourceManager resourceManager = null;
        private static LocaleVersion storedLocale = LocaleVersion.en_US;

        /// <summary>
        /// Gets the specified localized string
        /// </summary>
        public static String GetString(String key)
        {
            String result; String prefix;
            if (PluginBase.MainForm == null || PluginBase.MainForm.Settings == null) return key;
            LocaleVersion localeSetting = PluginBase.MainForm.Settings.LocaleVersion;
            if (resourceManager == null || localeSetting != storedLocale)
            {
                storedLocale = localeSetting;
                String path = "PluginCore.PluginCore.Resources." + storedLocale;
                resourceManager = new ResourceManager(path, Assembly.GetExecutingAssembly());
            }
            prefix = Assembly.GetCallingAssembly().GetName().Name;
            // On different distro we need to use FlashDevelop prefix
            if (prefix == DistroConfig.DISTRIBUTION_NAME) prefix = "FlashDevelop";
            result = resourceManager.GetString(prefix + "." + key);
            if (result == null) result = resourceManager.GetString(key);
            if (result == null)
            {
                TraceManager.Add("No localized string found: " + key);
                result = String.Empty;
            }
            // Replace FlashDevelop with distro name if needed
            if (DistroConfig.DISTRIBUTION_NAME != "FlashDevelop")
            {
                #pragma warning disable CS0162 // Unreachable code detected
                result = result.Replace("FlashDevelop", DistroConfig.DISTRIBUTION_NAME);
                #pragma warning restore CS0162 // Unreachable code detected
            }
            return result;
        }

    }

}
