using System;
using System.Collections.Generic;
using System.IO;

namespace AcrossLiteToText
{
    /// <summary>
    /// This file contains the Main entry point for the AcrossLiteToText console app.
    /// It's main purpose is to demonstrate Across Lite binary file parsing, and
    /// text file generation, in the associated Puzzle class.
    /// </summary>
    
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string from;                // filename or directory of .puz file(s)
            string toFolder = null;     // directly to place resulting .txt file(s)

            List<string> fileNames = new List<string>();    // list of files converted

            if (args.Length == 0)
            {
                DisplayUsage();
                Console.Write("Enter filename or folder for input .puz files: ");
                from = Console.ReadLine();
            }
            else
            {
                from = args[0];

                if (args.Length > 1)
                    toFolder = args[1];
            }

            if (!string.IsNullOrEmpty(from) && string.IsNullOrEmpty(toFolder))
            {
                Console.Write("Enter folder to write .txt files: ");
                toFolder = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(from))
                return;

            if (File.Exists(from))                              // single file
            {
                if (string.IsNullOrEmpty(toFolder))
                {
                    // If toFolder hasn't been set, try to make it the same as the
                    // folder of the from file.

                    int index = from.LastIndexOf('\\');
                    toFolder = index == -1 ? "." : from.Substring(0, index);
                }

                FileInfo file = new FileInfo(from);
                OutputTextFileFromPuzFile(file, toFolder);
                fileNames.Add(file.FullName);
            }
            else if (Directory.Exists(from))                    // folder
            {
                DirectoryInfo dir = new DirectoryInfo(from);

                if (string.IsNullOrEmpty(toFolder))
                    toFolder = from;

                foreach (FileInfo file in dir.GetFiles("*.puz"))
                {
                    OutputTextFileFromPuzFile(file, toFolder);
                    fileNames.Add(file.FullName);
                }
            }

            if (!string.IsNullOrEmpty(toFolder) && !Directory.Exists(toFolder))
            {
                if (!Directory.Exists(toFolder))
                {
                    try
                    { 
                        Directory.CreateDirectory(toFolder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: Cannot create folder {toFolder} : {ex.Message}");
                        return;
                    }
                }
            }

            if (fileNames.Count == 0)
                Console.WriteLine("No Across Lite files found");
            else
            {
                Console.WriteLine("");
                Console.WriteLine($"Number of files converted: {fileNames.Count}:");

                foreach (string file in fileNames)
                    Console.WriteLine($"\t{file}");
            }
        }


        static void OutputTextFileFromPuzFile(FileInfo puzFile, string toFolder)
        {
            if (!puzFile.Name.EndsWith(".puz"))
            {
                Console.WriteLine($"ERROR: {puzFile.Name} is not a correctly named Across Lite puzzle file");
                return;
            }

            Puzzle puz = new Puzzle(File.ReadAllBytes(puzFile.FullName));

            if (!puz.IsValid)
            {
                Console.WriteLine($"ERROR: {puzFile.Name} appears to be an invalid Across Lite file");
                return;
            }

            if (puz.IsLocked)
                Console.WriteLine($"WARNING: {puzFile.Name} appears to be locked");

            // Write out the text file

            string textFileName = @$"{toFolder}\{puzFile.Name.Replace(".puz", ".txt")}";
            File.WriteAllLines(textFileName, puz.Text, puz.AnsiEncoding);

            // Copy lines to the console as well

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine($"==========> {textFileName} created");
            Console.WriteLine("");

            Console.Write(string.Join(Environment.NewLine, puz.Text));
            Console.WriteLine(Environment.NewLine);
        }


        /// <summary>
        /// Usage information if no input found
        /// </summary>
        private static void DisplayUsage()
        {
            Console.WriteLine();
            Console.WriteLine("AcrossLiteToText converts Across Lite .puz files to text files.");
            Console.WriteLine("Specify a file or folder. Examples:");
            Console.WriteLine();
            Console.WriteLine("AcrossLiteToText filename.puz    (convert single file)");
            Console.WriteLine("AcrossLiteToText foldername      (convert all .puz files in folder)");
            Console.WriteLine("AcrossLiteToText .               (use . for current folder)");
            Console.WriteLine("AcrossLiteToText in out          (specify input and output folders");
            Console.WriteLine();
            Console.WriteLine("Note: some valid puzzle files cannot be represented as text.");
            Console.WriteLine();
            Console.WriteLine("(c) 2020 by Jim Horne.");
            Console.WriteLine();
        }
    }
}
