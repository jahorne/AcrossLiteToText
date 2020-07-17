using System.Collections.Generic;
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
    /// Classes to serialize for XML
    /// </summary>

    public class Dimensions
    {
        public int Rows;
        public int Cols;
    }

    public class Clue
    {
        [XmlAttribute]
        public string Answer;
        [XmlAttribute]
        public int Number;
        [XmlText]
        public string Text;
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
}