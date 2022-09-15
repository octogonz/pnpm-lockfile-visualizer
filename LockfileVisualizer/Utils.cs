using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace LockfileVisualizer
{
    public static class Utils
    {
        public static string ExeFolder;
        public static string DataFolder;

        public static bool OpenFileInWindowsExplorer(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }
            filePath = System.IO.Path.GetFullPath(filePath);
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
            return true;
        }

        public static string FormatTimespanAgo(TimeSpan ago)
        {
            string lastFetched = "";
            if (ago.TotalDays >= 90)
                lastFetched += $"{(int)ago.TotalDays / 30} months ago";
            else if (ago.TotalDays >= 14)
                lastFetched += $"{(int)ago.TotalDays / 7} weeks ago";
            else if (ago.TotalDays >= 2)
                lastFetched += $"{(int)ago.TotalDays} days ago";
            else if (ago.TotalHours >= 2)
                lastFetched += $"{(int)ago.TotalHours} hours ago";
            else if (ago.TotalMinutes >= 2)
                lastFetched += $"{(int)ago.TotalMinutes} minutes ago";
            else if (ago.TotalSeconds >= 10)
                lastFetched += $"{(int)ago.TotalSeconds} seconds ago";
            else
                lastFetched += $"just now";

            return lastFetched;
        }

        public static void RegisterAvalonSchema(string name, params string[] extension)
        {
            using (Stream? s = typeof(App).Assembly.GetManifestResourceStream("LockfileVisualizer.AvalonEdit-Yaml.xshd"))
            {
                if (s == null)
                {
                    throw new Exception("Resource not found");
                }
                using (var reader = new XmlTextReader(s))
                {
                    IHighlightingDefinition hl = HighlightingLoader.Load(reader, HighlightingManager.Instance);

                    HighlightingManager.Instance.RegisterHighlighting(name, extension, hl);
                }
            }
        }

        static Utils()
        {
            ProcessModule? mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null)
            {
                throw new Exception("Unable to determine main module");
            }
            string? exeFolder = Path.GetDirectoryName(mainModule.FileName);
            if (exeFolder == null)
            {
                throw new Exception("Module directory not found");
            }

            Utils.ExeFolder = exeFolder;
            if (File.Exists(Path.Combine(Utils.ExeFolder, "..\\..\\..\\LockfileVisualizer.csproj")))
            {
                // Development
                Utils.DataFolder = Path.GetFullPath(Path.Combine(Utils.ExeFolder, "..\\..\\..\\Data"));
            }
            else
            {
                // Standalone
                Utils.DataFolder = Path.Combine(Utils.ExeFolder, "Data");
            }

            if (!Directory.Exists(Utils.DataFolder))
            {
                Directory.CreateDirectory(Utils.DataFolder);
            }
        }
    }
}