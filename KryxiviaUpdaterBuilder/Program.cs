using KryxiviaUpdater.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;
using System.Text.Json;

namespace KryxiviaUpdater.Builder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Enter 'C' for creating new maj or 'U' for uploading");
                var choice = Console.ReadLine();
                if(choice == "C" || choice == "c")
                {
                    create();
                }else if(choice == "U" || choice == "u" )
                {
                    upload();
                }
            }
        }

        static void create()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("kryxivia_builder.json"));
            if (!File.Exists("versionApp.json")) File.WriteAllText("versionApp.json", JsonConvert.SerializeObject(new VersionApp()));
            var versionApp = JsonConvert.DeserializeObject<VersionApp>(File.ReadAllText("versionApp.json"));

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
            if (newVersionFiles.Count() <= 0)
            {
                Console.WriteLine("No files have been modified since the last version");
                return;
            }

            Directory.CreateDirectory(version);
            var compactor = new VersionCompactor($"{version}", newVersionFiles.ToList());
            var files = compactor.CompressAndZip();
            foreach(var file in newVersionFiles.ToList())
            {
                var directoryPath = Path.GetDirectoryName(Path.Combine(new string[]{
                version, file.FilePath }));
                Directory.CreateDirectory(directoryPath);
                File.Copy(file.FilePath, Path.Combine(new string[]
                {
                    version,
                    file.FilePath
                }));
            }

            File.WriteAllText($"{version}/files", files.ToString());
            versionApp.FilesChecksum = currentFilesChecksum;
            versionApp.Versions.Add(version);
            versionApp.NotYetUploaded.Add(version);
            Console.WriteLine("Write base directory");

            string @base = "0.0.0";
            if (Directory.Exists(@base))
            {
                var directory = new DirectoryInfo(@base);
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
            }

            Directory.CreateDirectory(@base);

            compactor = new VersionCompactor(@base, currentFilesChecksum.ToList());
            files = compactor.CompressAndZip();
            File.WriteAllText($"{@base}/files", files.ToString());
            if(!versionApp.NotYetUploaded.Contains("0.0.0")) versionApp.NotYetUploaded.Add("0.0.0");

            File.WriteAllText("versionApp.json", JsonConvert.SerializeObject(versionApp));
            Console.WriteLine("Successfully finished.");
        }
        static void upload()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("kryxivia_builder.json"));
            using (var sftp = new SftpClient(config.Server, config.User, config.Password))
            {
                Console.WriteLine("Try to connect to SFTP ...");
                sftp.Connect();
                Console.WriteLine("Connected !");
                if (!File.Exists("versionApp.json")) File.WriteAllText("versionApp.json", JsonConvert.SerializeObject(new VersionApp()));
                var versionApp = JsonConvert.DeserializeObject<VersionApp>(File.ReadAllText("versionApp.json"));

                var needUploaded = versionApp.NotYetUploaded.ToList();
                sftp.ChangeDirectory(config.SftpPath);
                foreach (var version in needUploaded)
                {
                    Console.WriteLine($"start upload {version}");
                    var files = Directory.GetFiles(version, "*.*", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        using (var fs = File.OpenRead(f))
                        {
                            var file = f.Replace('\\', '/');
                            if (file.Contains(config.PathFolder))
                            {
                                file = file.Replace($"{version}/", "");
                            }
                            var directoryPath = Path.GetDirectoryName(file).Replace('\\', '/');
                            CreateDirectoryRecursively(sftp, directoryPath);
                            sftp.UploadFile(fs, Path.Combine(new string[]{
                                file
                            }), true, null);
                        }

                    }
                    versionApp.NotYetUploaded.Remove(version);
                }

                using (var fs = File.OpenRead("versionApp.json"))
                {
                    sftp.UploadFile(fs, "versionApp.json", true, null);
                }
                File.WriteAllText("versionApp.json", JsonConvert.SerializeObject(versionApp));
                Console.WriteLine("upload finish");
                sftp.Disconnect();
            }
        }

        static public void CreateDirectoryRecursively(SftpClient client, string path)
        {
            string current = "";

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            while (!string.IsNullOrEmpty(path))
            {
                int p = path.IndexOf('/');
                current += '/';
                if (p >= 0)
                {
                    current += path.Substring(0, p);
                    path = path.Substring(p + 1);
                }
                else
                {
                    current += path;
                    path = "";
                }

                try
                {
                    SftpFileAttributes attrs = client.GetAttributes(current);
                    if (!attrs.IsDirectory)
                    {
                        throw new Exception("not directory");
                    }
                }
                catch (SftpPathNotFoundException)
                {
                    if (current.Contains('/'))
                    {
                        if(!client.Exists(current.Remove(0, 1)))
                            client.CreateDirectory(current.Remove(0,1));

                    }
                    else
                    {
                        if (!client.Exists(current.Remove(0, 1)))
                            client.CreateDirectory(current);
                    }
                }
            }
        }
    }
}
