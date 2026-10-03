#if !CONSOLE
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Central FlyLabFS icon provider. Icons live in the FlyLab layer and do not replace upstream JoinFS resources.
    /// </summary>
    internal static class FlyLabIcons
    {
        private const string ApplicationIconFileName = "flylabfs.ico";
        private static Icon applicationIcon;

        public static Icon ApplicationIcon
        {
            get
            {
                if (applicationIcon != null)
                    return applicationIcon;

                Assembly assembly = Assembly.GetExecutingAssembly();

                // Resolve by filename instead of relying on a hard-coded manifest namespace.
                // This keeps the FlyLab branding layer resilient if the project/resource namespace changes.
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("." + ApplicationIconFileName, StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                {
                    string availableResources = string.Join(Environment.NewLine, assembly.GetManifestResourceNames());
                    throw new InvalidOperationException(
                        "FlyLabFS icon resource '" + ApplicationIconFileName + "' was not found." + Environment.NewLine +
                        "Embedded resources:" + Environment.NewLine + availableResources);
                }

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                        applicationIcon = new Icon(stream);
                }

                return applicationIcon;
            }
        }
    }
}
#endif
