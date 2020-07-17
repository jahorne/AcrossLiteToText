using System.Collections.Generic;

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
