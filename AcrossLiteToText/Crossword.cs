using System.Collections.Generic;
using System.Xml.Serialization;

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
        [XmlAttribute]
        public int Number;
        [XmlAttribute]
        public string Text;
        [XmlAttribute]
        public string Answer;
    }

    public class Row
    {
        [XmlText]
        public string RowText;
    }

    public class Crossword
    {
        public string Title, Author, Copyright;
        public Dimensions Size;
        public List<Row> Grid;
        public List<Clue> Across, Down;
        public string NotePad;
        public bool HasCircles, IsRebus;
        public string RebusCode;
    }
}