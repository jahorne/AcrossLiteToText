using System;
using System.IO;

namespace AcrossLiteToText
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;

            // Look for command line parameter

            args = new string[5];   // bug bugbug remove !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            args[0] = @"C:\Users\jimh\Desktop\";


            if (args.Length > 0)
            {
                if (File.Exists(args[0]))                           // single file
                {
                    FileInfo file = new FileInfo(args[0]);
                    OutputTextfile(file);
                    count++;
                }
                else if (Directory.Exists(args[0]))                 // folder
                {
                    DirectoryInfo dir = new DirectoryInfo(args[0]);

                    foreach (FileInfo file in dir.GetFiles("*.puz"))
                    {
                        OutputTextfile(file);
                        count++;
                    }
                }

                if (count == 0)
                    Console.WriteLine("No Across Lite files found");
            }
            else
            {
                DisplayUsage();
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
                if (puz.IsLocked)
                    Console.WriteLine("ERROR: " + file.Name + " appears to be locked");
                else
                    Console.WriteLine("ERROR: " + file.Name + " appears to be an invalid Across Lite file");

                return;
            }

            string sTextFileName = file.FullName.Replace(".puz", ".txt");
            File.WriteAllLines(sTextFileName, puz.Text, puz.AnsiEncoding);
            Console.WriteLine($"{sTextFileName} created");
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
            Console.WriteLine("\tPuz2Txt filename.puz");
            Console.WriteLine("\tPuz2Txt foldername        (convert all .puz files in folder)");
            Console.WriteLine("\tPuz2Txt .                 (use . for current folder)");
            Console.WriteLine();
            Console.WriteLine("Note: some valid puzzle files cannot be represented as text.");
            Console.WriteLine();
            Console.WriteLine("(c) 2020 by Jim Horne. All rights reserved.");
            Console.WriteLine();
        }
    }
}
