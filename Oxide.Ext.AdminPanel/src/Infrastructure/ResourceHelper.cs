using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public static class ResourceHelper
    {
        /// <summary>
        /// Extracts the embedded resource and saves it to a file if the file is missing.
        /// </summary>
        /// <param name="resourceName">Resource name (contains namespace).</param>
        /// <param name="outputPath">Path for saving file.</param>
        public static async Task ExtractEmbeddedResourceAsync(string resourceName, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");
                }

                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
    }
}