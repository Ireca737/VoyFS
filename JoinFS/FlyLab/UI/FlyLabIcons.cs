#if !CONSOLE
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Central FlyLabFS icon provider. Icons live in the FlyLab layer and do not replace upstream JoinFS resources.
    /// </summary>
    internal static class FlyLabIcons
    {
        private const string ApplicationIconResource = "JoinFS.FlyLab.Resources.flylabfs.ico";
        private static Icon applicationIcon;

        public static Icon ApplicationIcon
        {
            get
            {
                if (applicationIcon != null)
                    return applicationIcon;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ApplicationIconResource))
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
