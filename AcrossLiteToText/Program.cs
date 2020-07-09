using System;
using System.IO;

namespace AcrossLiteToText
{
    class Program
    {
        static void Main(string[] args)
        {
            // Look for command line parameter


            args = new string[5];   // bug bugbug remove !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            args[0] = @"C:\Users\jimh\Desktop\TestFolder";


            if (args.Length > 0)
            {
                if (File.Exists(args[0]))                                       // is it a .puz file?
                {
                    FileInfo file = new FileInfo(args[0]);
                    OutputTextfile(file);
                }
                else if (Directory.Exists(args[0]))                             // is it a folder?
                {
                    Console.WriteLine("Directory " + args[0] + " exists!");

                    DirectoryInfo dir = new DirectoryInfo(args[0]);

                    foreach (FileInfo file in dir.GetFiles("*.puz"))            // loop through .puz files in this folder
                    {
                        OutputTextfile(file);
                    }
                }
                else
                {
                    Console.Write("No file or directory found at " + args[0]);
                }
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
                Console.WriteLine("ERROR: " + file.Name + " is not a correctly named Across Lite puzzle file");
                return;
            }

            Console.WriteLine($"Filename is {file.FullName}");


            Puzzle puz = new Puzzle(File.ReadAllBytes(file.FullName));

            if (!puz.IsValid)
            {
                if (puz.IsLocked)
                    Console.WriteLine("ERROR: " + file.Name + " appears to be locked");
                else
                    Console.WriteLine("ERROR: " + file.Name + " appears to be an invalid Across Lite file");

                return;
            }

            Console.WriteLine(puz.Title);
            Console.WriteLine(puz.AcrossClues);

            string sTextFileName = file.FullName.Replace(".puz", ".txt");
            bool bCreated = !File.Exists(sTextFileName);

            //AcrossLiteText alt = new AcrossLiteText(puz);

            //File.WriteAllText(sTextFileName, alt.Text);

            //Console.WriteLine("{0} {1}{2}", file.Name.Replace(".puz", ".txt"), bCreated ? "created" : "updated", alt.IsProblematic ? " - may need manual tweaking!" : "");
        }

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
