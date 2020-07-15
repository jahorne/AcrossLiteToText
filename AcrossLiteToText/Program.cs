using System;
using System.Collections.Generic;
using System.IO;

namespace AcrossLiteToText
{
    /// <summary>
    /// This file contains the Main entry point for the AcrossLiteToText console app.
    /// It's main purpose is to demonstrate the Across Lite binary file parsing, and
    /// text file generation capability, of the associated Puzzle class.
    ///
    /// Usage:  AcrossLiteToText inputFileOrFolder outputFolder (parameters optional)
    /// </summary>
    
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string from;                // filename or directory of .puz file(s)
            string toFolder = null;     // directory to create converted .txt file(s)

            List<string> fileNames = new List<string>();    // list of files to convert

            // Get the names of the input file or folder, and the output folder.

            if (args.Length == 0)
            {
                DisplayUsage();
                Console.Write("Enter filename or folder for .puz files to convert: ");
                from = Console.ReadLine();
            }
            else
            {
                from = args[0];

                if (args.Length > 1)
                    toFolder = args[1];
            }

            // If we didn't get a from file or folder, bail.

            if (string.IsNullOrEmpty(from))
                return;

            // If we didn't get a toFolder, ask for it.

            if (string.IsNullOrWhiteSpace(toFolder))
            {
                Console.Write("Enter folder to write .txt files (or Enter for same folder): ");
                toFolder = Console.ReadLine();
            }

            // See if from has specified a single file

            if (File.Exists(from))
            {
                if (string.IsNullOrEmpty(toFolder))
                {
                    // If toFolder hasn't been set, try to make it the same as the
                    // folder of the from file. Look for backslash or forward slash.

                    char[] pathSeparators = {'\\', '/'};
                    int index = from.LastIndexOfAny(pathSeparators);
                    toFolder = index == -1 ? "." : from.Substring(0, index);
                }

                // Save the filename to convert later

                FileInfo puzFile = new FileInfo(from);
                fileNames.Add(puzFile.FullName);
            }

            // If from wasn't a file, maybe it was a folder.

            else if (Directory.Exists(from))
            {
                // If toFolder is specified, use that.
                // Otherwise, use the input folder as output too.

                DirectoryInfo dir = new DirectoryInfo(from);

                if (string.IsNullOrWhiteSpace(toFolder))
                    toFolder = from;

                // Save all the .puz files in the from folder to convert later.

                foreach (FileInfo puzFile in dir.GetFiles("*.puz"))
                    fileNames.Add(puzFile.FullName);
            }

            // Couldn't fine a file OR a folder, so bail

            else
            {
                Console.WriteLine($"ERROR: could not find a file or folder named \"{from}\"");
                return;
            }

            // Make sure toFolder directory exists

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

            // For each filename collected, create the text file
            // and write the contents of each file to the console.

            if (fileNames.Count == 0)
            {
                Console.WriteLine("No Across Lite files found");
            }
            else
            {
                foreach (string fileName in fileNames)
                    OutputTextFileFromPuzFile(fileName, toFolder);

                Console.WriteLine("");
                Console.WriteLine("==============");
                Console.WriteLine("");
                Console.WriteLine($"Number of files converted: {fileNames.Count}");
                Console.WriteLine("");

                foreach (string fileName in fileNames)
                    Console.WriteLine($"\t{fileName}");
            }
        }


        /// <summary>
        /// Create a Puzzle object from the puzFile, and write out its equivalent text file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="toFolder"></param>
        private static void OutputTextFileFromPuzFile(string fileName, string toFolder)
        {
            FileInfo puzFile = new FileInfo(fileName);

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
            Console.WriteLine("");
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
            Console.WriteLine("PLEASE RESPECT THE COPYRIGHTS ON PUBLISHED CROSSWORDS.");
            Console.WriteLine("You need permission from the rights holders for most public and for all commercial uses.");
            Console.WriteLine();
            Console.WriteLine("(c) 2020 by Jim Horne.");
            Console.WriteLine();
        }
    }
}
