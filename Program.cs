using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerFileBackup
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
                throw new Exception("Arguments not enough!");

            var folder = args[0];
            var maxBk = args[1];
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                throw new Exception("Folder not found!");

            const string FOLDER_BK_NM = ".svf.bk";
            if (folder.EndsWith(FOLDER_BK_NM))
                throw new Exception($"Cant backup '{FOLDER_BK_NM}' folder");

            var now = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            var bkLimit = now - int.Parse(maxBk);

            var nowFolder = $"{folder}/{now}{FOLDER_BK_NM}/";
            Directory.CreateDirectory(nowFolder);
            foreach (var file in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, $"{nowFolder}/{Path.GetFileName(file)}", true);
            }

            foreach (var dir in Directory.GetDirectories(folder, $"*{FOLDER_BK_NM}", SearchOption.TopDirectoryOnly))
            {
                var date = int.Parse(Path.GetFileName(dir).Substring(0, 8));
                if (date < bkLimit)
                    Directory.Delete(dir, true);
            }
        }
    }
}
