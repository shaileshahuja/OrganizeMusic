using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace ReorganiseMusic
{
    class Program
    {
        static string fileName = "configurations.xml";
        static string inputFolder, outputFolder, logfile;
        static int yearID;
        static Dictionary<int, FileData> dict = new Dictionary<int, FileData>();
        private static int count = 0;
        static void Main(string[] args)
        {
            LoadConfigurations();
            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;

            objFolder = shell.NameSpace(inputFolder);

            for (int i = 0; i < short.MaxValue; i++)
            {
                string header = objFolder.GetDetailsOf(null, i);
                if (String.IsNullOrEmpty(header))
                    break;
                if (header.Equals("Year"))
                {
                    yearID = i;
                    break;
                }
            }
            CollectFiles(inputFolder);
            if (Directory.Exists(outputFolder))
                DeleteDirectory(outputFolder);
            Directory.CreateDirectory(outputFolder);
            OrganisedCopy(outputFolder);
        }
        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
        private static void OrganisedCopy(string outputFolder)
        {
            foreach (var data in dict)
            {
                int finalYear = data.Value.modified.Year;
                if (data.Value.year > 1950 && data.Value.year < 2020)
                    finalYear = data.Value.year;
                string path = Path.Combine(outputFolder, finalYear.ToString());
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = Path.Combine(path, data.Value.modified.Month.ToString());
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = Path.Combine(path, data.Value.name);
                try
                {
                    File.Copy(data.Value.path, path);
                    System.Console.WriteLine("Copied to " + path);
                }
                catch
                {

                }
            }
        }

        private static void CollectFiles(string folder)
        {
            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder = shell.NameSpace(folder);
            
            foreach (Shell32.FolderItem2 item in objFolder.Items())
            {
                if (item.IsFolder)
                    CollectFiles(item.Path);
                else
                {
                    if (!item.Type.ToUpper().StartsWith("MP3") && !item.Type.ToUpper().StartsWith("MPEG"))
                    {
                        LogError(item.Name + " has unsuupported file type of " + item.Type);
                        continue;
                    }
                    FileData fileData = new FileData();
                    fileData.name = item.Name;
                    fileData.size = item.Size;
                    fileData.modified = item.ModifyDate;
                    fileData.path = item.Path;
                    fileData.type = item.Type;
                    int.TryParse(objFolder.GetDetailsOf(item, yearID), out fileData.year);
                    string properName = fileData.name.Split(new char[] { '.' })[0];
                    if (dict.ContainsKey(fileData.size))
                    {
                        LogError(fileData.name + " clashed with " + dict[fileData.size].name);
                        count++;
                    }
                    dict[fileData.size] = fileData;
                }
            }
        }
        private static void LogError(string message)
        {
            StreamWriter file = new StreamWriter(logfile, true);
            file.WriteLine(message);
            System.Console.WriteLine(message);
            file.Close();
        }
        private static void LoadConfigurations()
        {
            XDocument doc = XDocument.Load(fileName);
            XElement root = doc.Root;
            inputFolder = root.Element("input").Value;
            outputFolder = root.Element("output").Value;
            logfile = root.Element("log").Value;
        }
    }
}

public class FileData
{
    public string name, type, path;
    public int size, year;
    public DateTime modified, webDate;

}
