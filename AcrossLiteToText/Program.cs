using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

// Copyright (C) 2020, Jim Horne
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You can see the license in detail here:
// https://github.com/jahorne/AcrossLiteToText/blob/master/LICENSE


namespace AcrossLiteToText
{
    /// <summary>
    /// 
    /// This file contains the Main entry point for the AcrossLiteToText console app.
    /// It's main purpose is to demonstrate the Across Lite binary file parsing, and
    /// text file generation capability, of the associated Puzzle class.
    ///
    /// Usage:  AcrossLiteToText inputFileOrFolder outputFolder (parameters optional)
    /// 
    /// </summary>

    internal static class Program
    {
        private static void Main(string[] args)
        {
            string from;                // filename or directory of .puz file(s)
            string toFolder = null;     // directory to create converted .txt file(s)

            List<FileInfo> fileList = new List<FileInfo>();     // files to convert

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

                // Save the file info to convert later

                fileList.Add(new FileInfo(from));
            }

            // If from wasn't a file, maybe it was a folder.

            else if (Directory.Exists(from))
            {
                // If toFolder is specified, use that.
                // Otherwise, use the input folder as output too.

                DirectoryInfo dir = new DirectoryInfo(from);

                if (string.IsNullOrWhiteSpace(toFolder))
                    toFolder = from;

                // Save all the .puz files in the from folder

                fileList.AddRange(dir.GetFiles("*.puz"));
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

            // We're ready to convert files

            if (fileList.Count == 0)
            {
                Console.WriteLine("No Across Lite files found");
            }
            else
            {
                // For each fileInfo collected, create a Puzzle object,
                // and write out the text and XML files.

                foreach (FileInfo fi in fileList)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"Converting {fi.FullName}");
                    Console.WriteLine();

                    if (!fi.Name.EndsWith(".puz"))
                    {
                        Console.WriteLine($"ERROR: {fi.FullName} is not a correctly named Across Lite puzzle file");
                        continue;
                    }

                    Puzzle puz = new Puzzle(File.ReadAllBytes(fi.FullName));

                    // Use this to output text file to console: Console.Write(string.Join(Environment.NewLine, puz.Text));
                    // Or use this to output XML file to console: puz.Xml.Save(Console.Out); Console.WriteLine();

                    if (!puz.IsValid)
                    {
                        Console.WriteLine($"\tERROR: {fi.Name} appears to be an invalid Across Lite file");
                        continue;
                    }

                    if (puz.IsLocked)
                        Console.WriteLine($"\tWARNING: {fi.Name} appears to be locked");

                    // TEXT files

                    string textFileName = @$"{toFolder}{Path.DirectorySeparatorChar}{fi.Name.Replace(".puz", ".txt")}";
                    bool bTextFileExisted = File.Exists(textFileName);
                    File.WriteAllLines(textFileName, puz.Text, puz.AnsiEncoding);

                    Console.WriteLine(File.Exists(textFileName)
                        ? $"\t{textFileName} {(bTextFileExisted ? "replaced" : "created")}"
                        : $"\tERROR: could not create {textFileName}");

                    // XML files

                    string xmlFileName = textFileName.Replace(".txt", ".xml");
                    XmlWriterSettings settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
                    XmlWriter writer = XmlWriter.Create(xmlFileName, settings);

                    bool bXmlFileExisted = File.Exists(xmlFileName);
                    puz.Xml.Save(writer);

                    Console.WriteLine(File.Exists(xmlFileName)
                        ? $"\t{xmlFileName} {(bXmlFileExisted ? "replaced" : "created")}"
                        : $"\tERROR: could not create {xmlFileName}");
                }
            }
        }


        /// <summary>
        /// Usage information
        /// </summary>
        private static void DisplayUsage()
        {
            Console.WriteLine();
            Console.WriteLine("AcrossLiteToText converts Across Lite .puz files to text files.");
            Console.WriteLine();
            Console.WriteLine("Specify a file or folder. Examples:");
            Console.WriteLine();
            Console.WriteLine("AcrossLiteToText filename.puz    (convert single file)");
            Console.WriteLine("AcrossLiteToText foldername      (convert all .puz files in folder)");
            Console.WriteLine("AcrossLiteToText .               (use . for current folder)");
            Console.WriteLine("AcrossLiteToText in out          (specify input and output folders");
            Console.WriteLine();
            Console.WriteLine("PLEASE RESPECT THE COPYRIGHTS ON PUBLISHED CROSSWORDS.");
            Console.WriteLine("You need permission from the rights holders for most public and for all commercial uses.");
            Console.WriteLine();
            Console.WriteLine("See https://github.com/jahorne/AcrossLiteToText for more info.");
            Console.WriteLine();
            Console.WriteLine("This program (c) 2020 by Jim Horne, licensed under GNU General Public License v3.0.");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
