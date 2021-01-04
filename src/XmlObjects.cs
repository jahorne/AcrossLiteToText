using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
// ReSharper disable NotAccessedField.Global
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global


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
        public int Rows { get; set; }
        public int Cols { get; set; }
    }

    public class Clue
    {
        [XmlAttribute] public int Num { get; set; }
        [XmlAttribute] public string Ans { get; set; }
        [XmlText] [JsonPropertyName("Clue")] public string Text { get; set; }
    }

    public class Row
    {
        [XmlText] [JsonPropertyName("Row")] public string RowText { get; set; }
    }
    public class Rebus
    {
        [XmlAttribute] public string Codes { get; set; }
        [XmlText] public bool IsRebus { get; set; }
    }

    public class Crossword
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Copyright { get; set; }
        public Dimensions Size { get; set; }
        public List<Row> Grid { get; set; }
        public List<Clue> Across { get; set; }
        public List<Clue> Down { get; set; }
        public string NotePad { get; set; }
        public bool HasCircles { get; set; }
        public Rebus IsRebus { get; set; }
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
