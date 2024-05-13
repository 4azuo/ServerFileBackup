using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerFileBackup
{
    public class Program
    {
        private const string FOLDER_BK_NM = ".svf.bk";
        private const string IGNORED_FOLDER_BK_D = "d\\.\\d{8}" + FOLDER_BK_NM;
        private const string IGNORED_FOLDER_BK_T = "t\\.\\d{6}" + FOLDER_BK_NM;
        private const string IGNORE_FILE = "/.svf.bk.ignore";

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

            var ignoredFormats = (File.Exists(IGNORE_FILE) ? File.ReadAllLines(IGNORE_FILE) : new string[0]).ToList();
            ignoredFormats.Add(IGNORED_FOLDER_BK_D);
            ignoredFormats.Add(IGNORED_FOLDER_BK_T);

            for (int i = 0; i < args.Length; i += 2)
            {
                var folder = args[i];
                var maxBk = int.Parse(args[i + 1]);

                var nowDate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                var nowDateFolder = $@"{folder}\d.{nowDate}{FOLDER_BK_NM}";
                var nowTime = int.Parse(DateTime.Now.ToString("HHmmss"));
                var nowTimeFolder = $@"{nowDateFolder}\t.{nowTime}{FOLDER_BK_NM}";
                var bkLimit = nowDate - maxBk;

                CopyEntireDirectory(folder, nowDateFolder, ignoredFormats);
                CopyEntireDirectory(folder, nowTimeFolder, ignoredFormats);

                foreach (var dir in Directory.GetDirectories(folder, $"d.*{FOLDER_BK_NM}", SearchOption.TopDirectoryOnly))
                {
                    var date = int.Parse(Path.GetFileName(dir).Split('.')[1]);
                    if (date < bkLimit)
                        Directory.Delete(dir, true);
                }
            }
        }

        public static void CopyEntireDirectory(string source, string target, List<string> ignoredFormats, bool overwiteFiles = true)
        {
            CopyEntireDirectory(new DirectoryInfo(source), new DirectoryInfo(target), ignoredFormats, overwiteFiles);
        }

        public static void CopyEntireDirectory(DirectoryInfo source, DirectoryInfo target, List<string> ignoredFormats, bool overwiteFiles = true)
        {
            if (!source.Exists) return;
            if (!target.Exists) target.Create();

            Parallel.ForEach(source.GetDirectories(), (sourceChildDirectory) =>
            {
                if (ignoredFormats.Any(x => Regex.IsMatch(sourceChildDirectory.Name, x)))
                    return;
                CopyEntireDirectory(sourceChildDirectory, new DirectoryInfo(Path.Combine(target.FullName, sourceChildDirectory.Name)), ignoredFormats, overwiteFiles);
            });

            Parallel.ForEach(source.GetFiles(), sourceFile =>
            {
                if (ignoredFormats.Any(x => Regex.IsMatch(sourceFile.Name, x)))
                    return;
                sourceFile.CopyTo(Path.Combine(target.FullName, sourceFile.Name), overwiteFiles);
            });
        }
    }
}
