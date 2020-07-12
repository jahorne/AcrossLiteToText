using System;
using System.Collections.Generic;
using System.IO;

namespace AcrossLiteToText
{
    class Program
    {
        static void Main(string[] args)
        {
            string from, toFolder;
            List<string> fileNames = new List<string>();

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
                    toFolder = args[1];         // BUG output folder not working yet
            }

            if (File.Exists(from))                              // single file
            {
                FileInfo file = new FileInfo(from);
                OutputTextfile(file);
                fileNames.Add(file.FullName);
            }
            else if (Directory.Exists(from))                    // folder
            {
                DirectoryInfo dir = new DirectoryInfo(from);

                foreach (FileInfo file in dir.GetFiles("*.puz"))
                {
                    OutputTextfile(file);
                    fileNames.Add(file.FullName);
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


        static void OutputTextfile(FileInfo file)
        {
            if (!file.Name.EndsWith(".puz"))
            {
                Console.WriteLine($"ERROR: {file.Name} is not a correctly named Across Lite puzzle file");
                return;
            }

            Puzzle puz = new Puzzle(File.ReadAllBytes(file.FullName));

            if (!puz.IsValid)
            {
                Console.WriteLine($"ERROR: {file.Name} appears to be an invalid Across Lite file");
                return;
            }

            if (puz.IsLocked)
                Console.WriteLine($"WARNING: {file.Name} appears to be locked");

            // Write out the text file

            string sTextFileName = file.FullName.Replace(".puz", ".txt");
            File.WriteAllLines(sTextFileName, puz.Text, puz.AnsiEncoding);

            // Copy lines to the console as well

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine($"==========> {sTextFileName} created");
            Console.WriteLine("");

            Console.Write(string.Join(Environment.NewLine, puz.Text));
            Console.WriteLine(Environment.NewLine);
        }


        /// <summary>
        /// Usuage information if no input found
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
