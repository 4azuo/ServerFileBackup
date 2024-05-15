using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerFileBackup
{
    public class Program
    {
        private const string FOLDER_BK_NM = ".svf.bk";
        private const string IGNORE_FILE = "/.svf.bk.ignore";

        private const string IGNOREREGEX_FOLDER_BK_D = "d\\.\\d{8}" + FOLDER_BK_NM;
        private const string IGNOREREGEX_FOLDER_BK_T = "t\\.\\d{6}" + FOLDER_BK_NM;
        private const string IGNOREREGEX_IGNORE_FILE = "\\.svf\\.bk\\.ignore";

        private static IList<string> IgnoredFormats { get; set; }

        /// <summary>
        /// Backup folder's files to [.svf.bk] folder.
        /// [.svf.bk] folder will be deleted after x days.
        /// </summary>
        /// <param name="args">
        /// Example 1: "a/b/c/" 2
        /// Example 2: "a/b/c/" 2 "a/b/d/" 5
        /// </param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            var now = DateTime.Now;

            if (args.Length < 2 || args.Length % 2 != 0 ||
                args.Select((x, i) => new { Value = x, Index = i }).Any(x =>
                {
                    if (x.Index % 2 == 0 && (string.IsNullOrEmpty(x.Value) || !Directory.Exists(x.Value)))
                        return true;
                    if (x.Index % 2 == 0 && x.Value.EndsWith(FOLDER_BK_NM))
                        return true;
                    if (x.Index % 2 == 1 && !int.TryParse(x.Value, out _))
                        return true;
                    return false;
                }))
            {
                throw new ArgumentException();
            }

            IgnoredFormats = (File.Exists(IGNORE_FILE) ? File.ReadAllLines(IGNORE_FILE) : new string[0]).ToList();
            IgnoredFormats.Add(IGNOREREGEX_FOLDER_BK_D);
            IgnoredFormats.Add(IGNOREREGEX_FOLDER_BK_T);
            IgnoredFormats.Add(IGNOREREGEX_IGNORE_FILE);

            for (int i = 0; i < args.Length; i += 2)
            {
                var folder = args[i];
                var maxBk = int.Parse(args[i + 1]);

                var nowDateInt = int.Parse(now.ToString("yyyyMMdd"));
                var nowDateFolder = $@"{folder}\d.{now:yyyyMMdd}{FOLDER_BK_NM}";
                var nowTimeFolder = $@"{nowDateFolder}\t.{now:HHmmss}{FOLDER_BK_NM}";
                var bkLimit = nowDateInt - maxBk;

                CopyEntireDirectory(folder, nowDateFolder);
                CopyEntireDirectory(folder, nowTimeFolder);

                foreach (var dir in Directory.GetDirectories(folder, $"d.*{FOLDER_BK_NM}", SearchOption.TopDirectoryOnly))
                {
                    var date = int.Parse(Path.GetFileName(dir).Split('.')[1]);
                    if (date < bkLimit)
                        Directory.Delete(dir, true);
                }
            }
        }

        public static void CopyEntireDirectory(string source, string target, bool overwiteFiles = true)
        {
            CopyEntireDirectory(new DirectoryInfo(source), new DirectoryInfo(target), overwiteFiles);
        }

        public static void CopyEntireDirectory(DirectoryInfo source, DirectoryInfo target, bool overwiteFiles = true)
        {
            if (!source.Exists) return;
            if (!target.Exists) target.Create();

            Parallel.ForEach(source.GetDirectories(), (sourceChildDirectory) =>
            {
                if (IsIgnoredFormat(sourceChildDirectory.Name))
                    return;
                CopyEntireDirectory(sourceChildDirectory, new DirectoryInfo(Path.Combine(target.FullName, sourceChildDirectory.Name)), overwiteFiles);
            });

            Parallel.ForEach(source.GetFiles(), sourceFile =>
            {
                if (IsIgnoredFormat(sourceFile.Name))
                    return;
                sourceFile.CopyTo(Path.Combine(target.FullName, sourceFile.Name), overwiteFiles);
            });
        }

        public static bool IsIgnoredFormat(string format)
        {
            return IgnoredFormats.Any(x => Regex.IsMatch(format, x)) || Environment.GetCommandLineArgs()[0] == format;
        }
    }
}
