using System.Collections.Generic;


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
    /// These classes are used to serialize the XML
    /// </summary>

    public class Dimensions
    {
        public int Rows;
        public int Cols;
    }

    public class Clue
    {
        public int Number;
        public string Text;
        public string Answer;
    }

    public class Crossword
    {
        public string Title, Author, Copyright;
        public Dimensions Size;
        public string Grid;
        public List<Clue> Across, Down;
        public string NotePad;
    }
}