﻿using QuickRestore.Properties;
using System;
using System.IO;

namespace QuickRestore
{
    public class Program
    {
        public static string BackupPath;

        private static void Main(string[] args)
        {
            try
            {
                RunProgram(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An Error Occurred:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }

                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void RunProgram(string[] args)
        {

            CheckBackupFolderExists();

            if (args.Length >= 1)
            {
                var settings = Settings.Default;

                // Optional database name
                if (args.Length == 2)
                {
                    if (args[1].Contains("?"))
                    {
                        PrintHelpAndExit();
                    }
                    
                    settings.DatabaseName = args[1];
                }

                if (args[0].Contains("b"))
                {
                    DatabaseBackup.Backup(settings);
                }
                else if (args[0].Contains("r"))
                {
                    DatabaseRestore.Restore(settings);
                }
                else
                {
                    PrintHelpAndExit();
                }

            }
            else
            {
                PrintHelpAndExit();
            }

        }

        private static void PrintHelpAndExit()
        {
            Console.WriteLine(@"-b  <databasename> : perform backup on <databasename>(optional)");
            Console.WriteLine(@"-r  <databasename> : perform restore of <databasename>(optional)");
            Environment.Exit(0);
        }

        private static void CheckBackupFolderExists()
        {
            if (Directory.Exists(Settings.Default.BackupFolder)) return;

            Console.WriteLine("The backup folder '{0}' does not exist", Settings.Default.BackupFolder);
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}
