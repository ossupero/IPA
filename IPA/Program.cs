﻿using IPA.Patcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPA
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1 || !args[0].EndsWith(".exe"))
            {
                Fail("Drag an (executable) file on the exe!");
            }

            string launcherSrc = Path.Combine("IPA", "Launcher.exe");
            string dataSrcPath = Path.Combine("IPA", "Data");
            string pluginsFolder = "Plugins";
            string projectName = Path.GetFileNameWithoutExtension(args[0]);
            string dataDstPath = Path.Combine(Path.GetDirectoryName(args[0]), projectName + "_Data");
            string managedPath = Path.Combine(dataDstPath, "Managed");
            string engineFile = Path.Combine(managedPath, "UnityEngine.dll");
            string assemblyFile = Path.Combine(managedPath, "Assembly-Csharp.dll");


            // Sanitizing
            if (!File.Exists(launcherSrc)) Fail("Couldn't find DLLs! Make sure you extracted all contents of the release archive.");
            if(!Directory.Exists(dataDstPath) || !File.Exists(engineFile) || !File.Exists(assemblyFile))
            {
                Fail("Game does not seem to be a Unity project. Could not find the libraries to patch. ");
            } 

            try
            {
                // Copying
                Console.Write("Updating files... ");
                CopyAll(new DirectoryInfo(dataSrcPath), new DirectoryInfo(dataDstPath));
                Console.WriteLine("Successfully updated files!");

                if (!Directory.Exists(pluginsFolder))
                {
                    Console.WriteLine("Creating plugins folder... ");
                    Directory.CreateDirectory(pluginsFolder);
                }

                // Patching
                var patchedModule = PatchedModule.Load(engineFile);
                if(!patchedModule.IsPatched)
                {
                    Console.Write("Patching UnityEngine.dll... ");
                    BackupManager.MakeBackup(engineFile);
                    patchedModule.Patch();
                    Console.WriteLine("Done!");
                }

                // Virtualizing
                var virtualizedModule = VirtualizedModule.Load(assemblyFile);
                if(!virtualizedModule.IsVirtualized)
                {
                    Console.Write("Virtualizing Assembly-Csharp.dll... ");
                    BackupManager.MakeBackup(assemblyFile);
                    virtualizedModule.Virtualize();
                    Console.WriteLine("Done!");
                }
            } catch(Exception e)
            {
                Fail("Oops! This should not have happened.\n\n" + e);
            }

            Console.WriteLine("Finished!");
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                string targetFile = Path.Combine(target.FullName, fi.Name);
                if (!File.Exists(targetFile) || File.GetLastWriteTimeUtc(targetFile) < fi.LastWriteTimeUtc)
                {
                    Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }


        static void Fail(string message)
        {
            Console.Error.Write("ERROR: " + message);
            if (!Environment.CommandLine.Contains("--nowait"))
            {
                Console.WriteLine("\n\n[Press any key to quit]");
                Console.ReadKey();
            }
            Environment.Exit(1);
        }
    }
}
