using KryxiviaUpdater.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json.Nodes;

namespace KryxiviaUpdater.Builder
{
    internal class Program
    {
        private static Config config;
        static void Main(string[] args)
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("kryxivia_builder.json"));
            if (!File.Exists("version_app.json")) File.WriteAllText("version_app.json", JsonConvert.SerializeObject(new VersionApp()));
            var versionApp = JsonConvert.DeserializeObject<VersionApp>(File.ReadAllText("version_app.json"));

            string version = "";
            do
            {
                Console.WriteLine("Enter the version of the updater (format: xx.xx.xx): ");
                version = Console.ReadLine();
                if (versionApp.Versions.Contains(version)) continue;
                if (Regex.IsMatch(version, @"^([0-9]{1,2}).([0-9]{1,2}).([0-9]{1,2})$")) break;
            } while (true);

            Console.WriteLine("Start listing files.");


            var currentFilesChecksum = new List<FileCheckSum>();
            foreach (var file in Directory.GetFiles(config.PathFolder, "*.*", SearchOption.AllDirectories))
            {
                currentFilesChecksum.Add(new FileCheckSum
                {
                    Checksum = Utils.GenerateChecksum(file),
                    FilePath = file,
                });
            }

            var newVersionFiles = currentFilesChecksum.Except(versionApp.FilesChecksum);
            if(newVersionFiles.Count() <= 0)
            {
                Console.WriteLine("No files have been modified since the last version");
                return;
            }
            
            Directory.CreateDirectory(version);
            var compactor = new VersionCompactor($"{version}", newVersionFiles.ToList());
            compactor.CompressAndZip();

            versionApp.FilesChecksum = currentFilesChecksum;
            versionApp.Versions.Add(version);
            File.WriteAllText("version_app.json", JsonConvert.SerializeObject(versionApp));
            Console.WriteLine("Successfully finished.");
        }

        static string buildFileUrl(string filePath) => config.URL + filePath;



    }
}
