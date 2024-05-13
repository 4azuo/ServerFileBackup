using System;
using System.IO;
using System.Linq;

namespace ServerFileBackup
{
    public class Program
    {
        private const int BACKUP_DETAIL_LIMIT = 1;

        /// <summary>
        /// Backup folder's files to [.svf.bk] folder.
        /// [.svf.bk] folder will be deleted after x days.
        /// </summary>
        /// <param name="args">
        /// Example 1: "a/b/c/" 2
        /// Example 2: "a/b/c/" 2 "a/b/d/" 5
        /// </param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length % 2 != 0 || 
                args.Select((x, i) => new { Value = x, Index = i }).Any(x =>
                {
                    if (x.Index % 2 == 1 && !int.TryParse(x.Value, out _))
                        return true;
                    return false;
                }))
                throw new ArgumentException();

            for (int i = 0; i < args.Length; i += 2)
            {
                var folder = args[i];
                var maxBk = int.Parse(args[i + 1]);
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    throw new DirectoryNotFoundException();

                const string FOLDER_BK_NM = ".svf.bk";
                if (folder.EndsWith(FOLDER_BK_NM))
                    throw new Exception($"Cant backup '{FOLDER_BK_NM}' folder");

                var nowDate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                var nowDateFolder = $"{folder}/d.{nowDate}{FOLDER_BK_NM}/";
                var nowTime = int.Parse(DateTime.Now.ToString("HHmmss"));
                var nowTimeFolder = $"{nowDateFolder}/t.{nowTime}{FOLDER_BK_NM}/";
                var bkLimit = nowDate - maxBk;

                Directory.CreateDirectory(nowDateFolder);
                Directory.CreateDirectory(nowTimeFolder);

                foreach (var file in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly))
                {
                    File.Copy(file, $"{nowDateFolder}/{Path.GetFileName(file)}", true);
                    File.Copy(file, $"{nowTimeFolder}/{Path.GetFileName(file)}", true);
                }

                foreach (var dir in Directory.GetDirectories(folder, $"d.*{FOLDER_BK_NM}", SearchOption.TopDirectoryOnly))
                {
                    var date = int.Parse(Path.GetFileName(dir).Split('.')[1]);
                    if (date < bkLimit)
                        Directory.Delete(dir, true);
                    else if (date < nowDate - BACKUP_DETAIL_LIMIT)
                    {
                        foreach (var tDir in Directory.GetDirectories(folder, $"t.*{FOLDER_BK_NM}", SearchOption.TopDirectoryOnly))
                        {
                            Directory.Delete(tDir, true);
                        }
                    }
                }
            }
        }
    }
}
