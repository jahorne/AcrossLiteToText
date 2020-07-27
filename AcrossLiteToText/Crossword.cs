using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
// ReSharper disable NotAccessedField.Global
// ReSharper disable CollectionNeverQueried.Global


// Copyright (C) 2020, Jim Horne
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You can see the license in detail here:
// https://github.com/jahorne/AcrossLiteToText/blob/master/LICENSE


namespace AcrossLiteToText
{
    /// <summary>
    /// Classes to serialize for XML.
    /// Crossword object defines a single puzzle.
    /// Crosswords is a Crossword collection.
    /// </summary>

    public class Dimensions
    {
        public int Rows;
        public int Cols;
    }

    public class Clue
    {
        [XmlAttribute] public int Num;
        [XmlAttribute] public string Ans;
        [XmlText] public string Text;
    }

    public class Row
    {
        [XmlText] public string RowText;
    }

    public class RebusCode
    {
        [XmlText] public string CodeText;
    }

    public class Crossword
    {
        public string Title, Author, Copyright;
        public Dimensions Size;
        public List<Row> Grid;
        public List<Clue> Across, Down;
        public string NotePad;
        public bool HasCircles, IsRebus;
        public List<RebusCode> RebusCodes;
    }

    public class Crosswords
    {
        [XmlElement] public List<Crossword> Crossword;
    }


    /// <summary>
    /// Convert generic object to XmlDocument.
    /// Useful for converting Crossword or Crosswords.
    /// </summary>
    public static class Utilities
    {
        public static XmlDocument SerializeToXmlDocument(object input)
        {
            XmlSerializer ser = new XmlSerializer(input.GetType());

            using MemoryStream memStream = new MemoryStream();
            ser.Serialize(memStream, input);

            memStream.Position = 0;

            XmlReaderSettings settings = new XmlReaderSettings { IgnoreWhitespace = true };

            using XmlReader xtr = XmlReader.Create(memStream, settings);
            XmlDocument xd = new XmlDocument();
            xd.Load(xtr);

            return xd;
        }
    }
}