using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcrossLiteToText
{
    class Puzzle
    {
        public bool IsValid { get; }                   // true if parsing successful
        public bool IsLocked { get; }                               // true if the puzzle is locked, in which case IsValid will also be false

        public IEnumerable<string> Text => TextVersion();


        private readonly char[,] _grid;
        private int[,] _rebusNums;
        private bool[,] _hasCircle;

        // Circles

        public bool HasCircles { get; private set; }
        public byte[] CircleTableBytes { get; private set; }
        public int CircleCount { get; private set; }

        // Rebus

        public Dictionary<int, string> RebusDict { get; private set; }  // Rebus substitution dictionary
        public string RebusString { get; private set; }
        public bool IsRebus { get; private set; }
        public byte[] RebusTableBytes { get; private set; }
        public int RebusCount { get; private set; }


        private readonly string _title, _author, _copyright, _notepad;

        private readonly int _rowCount, _colCount;

        readonly List<string> _acrossClues = new List<string>();
        readonly List<string> _downClues = new List<string>();


        private byte[] GridTableBytes { get; }

        public readonly Encoding Ansi = Encoding.GetEncoding("ISO-8859-1");

        public Puzzle(byte[] b, bool bIsDiagramless = false)
        {
            const int columnsOffset = 0x2c;             // number of columns is at this offset in Across Lite file
            const int rowsOffset = 0x2d;                // number of rows is in next byte
            const int gridOffset = 0x34;                // standard location to start parsing grid data in binary stream

            // Reject locked puzzles unless they're Variety type

            // BUG BUGBUG handle encrypted puzzles if answers are typed in

            IsLocked = b[0x32] != 0 || b[0x33] != 0;    // is Across Lite file encrypted?

            if (IsLocked)
                return;

            // Looks like we have a good puzzle

            _colCount = b[columnsOffset];        // number of columns
            _rowCount = b[rowsOffset];           // number of rows

            // We now know how big the puzzle is so we can generate the grids

            _grid = new char[_rowCount, _colCount];
            int[,] gridNums = new int[_rowCount, _colCount];
            int i = gridOffset;     // i indexes through byte array b[] starting at standard offset location

            // If the next byte is NOT either a empty square or a filled in square indicator,
            // the puzzle has been fixed up, either to include Subs in older puzzles or to show solved but
            // still encrypted puzzles.
            //
            // 0x2E means black square (block) and 0x2D is empty square.
            // Note 0x2E is valid in both sections so find first non black square and check if it's a blank

            int nAnswerOffset = gridOffset + (_rowCount * _colCount);
            int nOff = nAnswerOffset;
            bool bFixed = false;        // assume didn't have to manually enter solution (fixing is necessary for old V1 rebus puzzles)

            while (b[nOff] == 0x2E || b[nOff] == 0x3A) // go to first non-black square
                nOff++;

            if (b[nOff] != 0x2D) // if it's not a space character
            {
                bFixed = true;
                i = nAnswerOffset;
            }

            // i now points to start of grid with unencrypted solution


            GridTableBytes = new byte[_rowCount * _colCount];

            int j = 0;

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    char cLetter = (char)b[i++];

                    if (cLetter == '.' || cLetter == ':')          // : is used for diagramless     BUG BUGBUG -- leave as is?
                    {
                        cLetter = '.';      // normalize to .
                    }

                    _grid[r, c] = cLetter;
                    GridTableBytes[j++] = (byte)cLetter;
                }
            }

            // Now that the grid is filled in, a second pass can accurately assign grid numbers

            int num = 1;
            byte[] numTable = new byte[_rowCount * _colCount];

            j = 0;

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    if (_grid[r, c] != '.')
                    {
                        if ((c == 0 || _grid[r, c - 1] == '.') && c != _colCount - 1 && _grid[r, c + 1] != '.')
                        {
                            numTable[j] = (byte)num;
                            gridNums[r, c] = num++;
                        }
                        else if ((r == 0 || _grid[r - 1, c] == '.') && r != _rowCount - 1 && _grid[r + 1, c] != '.')
                        {
                            numTable[j] = (byte)num;
                            gridNums[r, c] = num++;
                        }
                    }

                    j++;
                }
            }


            // Get Title and Author, adjusting i along the way

            i = gridOffset + (2 * _rowCount * _colCount); // move past grid

            _title = BinaryString(b, ref i); // get title
            i++;        // skip past \0 delimiter

            _author = BinaryString(b, ref i); // get and then parse Author
            i++;        // skip past \0 again

            // Get copyright string. Per Litsoft notes, the © symbol is automatically added,
            // so we can delete it if it's found here.

            _copyright = BinaryString(b, ref i);

            if (_copyright.StartsWith("©"))
                _copyright = _copyright.Substring(1).Trim();

            IsRebus = bFixed ? ParseFixedRebus(b, i) : ParseRebus(b, i);

            // Sometimes Across Lite has fake rebus indicators. If no actual rebus entries are found,
            // sRebusString and byRebusTable should be manually reset.

            if (!IsRebus)
            {
                RebusString = null;
                RebusTableBytes = Array.Empty<byte>();
            }


            // Figure out clues. They are ordered in Across Lite in an odd way.
            // Look for the next numbered cell. If there is an across clue starting there,
            // that's the next clue. If there is only a down starting there, it's next.
            // If there is both an across and a down, the across comes first.
            // Then proceed to next grid number.

            i++;    // skip past \0 again

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    int gridNum = gridNums[r, c];

                    if (gridNum != 0) // if there is a grid number here
                    {
                        // Look first for an across clue

                        if ((c == 0 || _grid[r, c - 1] == '.') && c != _colCount - 1 && _grid[r, c + 1] != '.')
                        {
                            _acrossClues.Add(BinaryString(b, ref i));
                            i++;
                        }

                        // Next look for a down clue at this same grid number using similar logic to above

                        if ((r == 0 || _grid[r - 1, c] == '.') && r != _rowCount - 1 && _grid[r + 1, c] != '.')
                        {
                            _downClues.Add(BinaryString(b, ref i));
                            i++;
                        }
                    }
                }
            }

            _notepad = BinaryString(b, ref i);

            HasCircles = ParseCircles(b, i); // Find all squares that have circles in them

            // For diagramless, set black squares back to ':'

            if (bIsDiagramless)
            {
                for (int r = 0; r < _rowCount; r++)
                    for (int c = 0; c < _colCount; c++)
                        if (_grid[r, c] == '.')
                            _grid[r, c] = ':';
            }

            IsValid = true;

        }



        private string BinaryString(byte[] b, ref int i)
        {
            int nStart = i;

            while (b[i] != 0)
                i++;

            return Ansi.GetString(b, nStart, i - nStart).Trim();
        }




        /// <summary>
        /// Fills byCircle[r,c] array with true for each square that includes a circle and false otherwise,
        /// and also populates 1-D byCircleTable[n] array for SQL and updates public var nCircleCount.
        /// </summary>
        /// <param name="b">binary array to parse</param>
        /// <param name="i">offset in b to start searching</param>
        /// <returns>true iff at least one circle was found</returns>
        private bool ParseCircles(byte[] b, int i)
        {
            bool bFound = false; // assume none found

            // Search for GEXT which marks the start of the circle data

            while (i < (b.Length - (_rowCount * _colCount)))
            {
                if (b[i] == 'G' && b[i + 1] == 'E' && b[i + 2] == 'X' && b[i + 3] == 'T')
                {
                    bFound = true; // need to check later
                    break;
                }

                i++;
            }

            if (bFound)
            {
                int j = 0;          // index into 1-D byCircleTable array
                i += 8;             // offset from GEXT
                bFound = false;     // reset, so now check if circles actually found

                _hasCircle = new bool[_rowCount, _colCount];
                CircleTableBytes = new byte[_rowCount * _colCount];

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++, i++, j++)
                    {
                        if (b[i] == 0x80 || b[i] == 0xC0)   // 0x80 means circle, 0xC0 means circle in diagramless
                        {
                            _hasCircle[r, c] = true;
                            CircleTableBytes[j] = 1;
                            CircleCount++;
                            bFound = true;
                        }
                        else
                        {
                            _hasCircle[r, c] = false;
                            CircleTableBytes[j] = 0;
                        }
                    }
                }
            }

            return bFound;
        }


        /// <summary>
        /// Look for rebus info in binary array. Updates public var nRebusCount.
        /// </summary>
        /// <param name="b">binary array to parse</param>
        /// <param name="i">offset in b to start searching</param>
        /// <returns>true iff at least one rebus entry was found</returns>
        private bool ParseRebus(byte[] b, int i)
        {
            bool bFound = false; // assume not found

            // Search for GRBS meaning rebus substitution data

            while (i < (b.Length - (_rowCount * _colCount)))
            {
                if (b[i] == 'G' && b[i + 1] == 'R' && b[i + 2] == 'B' && b[i + 3] == 'S')
                {
                    bFound = true;  // need to check later
                    break;
                }

                i++;
            }

            if (bFound)             // if GRBS was found
            {
                int j = 0;          // index into 1-D byRebusTable array
                i += 8;             // offset from GRBS
                bFound = false;     // reset, so now check if rebus entries actually exist

                _rebusNums = new int[_rowCount, _colCount];
                RebusTableBytes = new byte[_rowCount * _colCount];

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++)
                    {
                        int nSubNumber = b[i++];

                        RebusTableBytes[j++] = (byte)nSubNumber;
                        _rebusNums[r, c] = nSubNumber;

                        if (nSubNumber > 0)
                        {
                            bFound = true;
                            RebusCount++;
                        }
                    }
                }

                // If actual rebus data found, parse it and return true

                if (bFound)
                {
                    i += 9; // skip to start of substring table

                    StringBuilder sb = new StringBuilder();
                    while (b[i] != 0)
                        sb.Append((char)b[i++]);

                    RebusString = sb.ToString();
                    RebusDict = CrackSubstring(RebusString);
                }
            }

            return bFound;
        }


        /// <summary>
        /// Similarly, for older puzzles where answers, including rebus, were fixed up (manually entered in Across Lite).
        /// Updates public var nRebusCount.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="i"></param>
        /// <returns>true iff at least one rebus entry was found</returns>
        private bool ParseFixedRebus(byte[] b, int i)
        {
            bool bFound = false; // assume none found

            // Search for RUSR meaning user rebus substitution data

            while (i < (b.Length - (_rowCount * _colCount)))
            {
                if (b[i] == 'R' && b[i + 1] == 'U' && b[i + 2] == 'S' && b[i + 3] == 'R')
                {
                    bFound = true;
                    break;
                }

                i++;
            }

            if (bFound)
            {
                i += 8; // fix up offset

                _rebusNums = new int[_rowCount, _colCount];
                int nSubIndex = 0;

                StringBuilder sbSubs = new StringBuilder();
                RebusTableBytes = new byte[_rowCount * _colCount];
                int j = 0;

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++)
                    {
                        if (b[i] != 0)
                        {
                            RebusCount++;
                            StringBuilder sb = new StringBuilder();

                            while (b[i] != 0)
                                sb.Append((char)b[i++]);

                            _rebusNums[r, c] = nSubIndex + 1;
                            sbSubs.Append($"{nSubIndex:D2}:{sb};");
                            nSubIndex++;
                            RebusTableBytes[j] = (byte)nSubIndex;
                        }

                        i++;
                        j++;
                    }
                }

                // if table is empty, report back as if there was no table

                if (RebusCount == 0)
                {
                    bFound = false;
                }
                else
                {
                    RebusString = sbSubs.ToString();
                    RebusDict = CrackSubstring(RebusString);
                }
            }

            return bFound;
        }


        /// <summary>
        /// CrackSubstring takes a string which looks like " 1:FIRST; 2:SECOND; 3:THIRD;"
        /// or possibly "19:SECOND;26FIRST;33THIRD;35HOME;" and creates a dictionary where the
        /// string part is keyed to the number plus one since that's how Across Lite binary format works.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static Dictionary<int, string> CrackSubstring(string s)
        {
            Dictionary<int, string> subKeyVal = new Dictionary<int, string>();

            string[] rawParts = s.Trim().Split(';');

            // Key is number before colon plus offset (1)

            int nNumParts = rawParts.Length - 1;    // ignore trailing ';' string

            for (int i = 0; i < nNumParts; i++)
            {
                string subInfo = rawParts[i];
                string[] subInfoParts = subInfo.Split(':');
                int nKey = Convert.ToInt32(subInfoParts[0]);
                subKeyVal.Add(nKey + 1, subInfoParts[1]);
            }

            return subKeyVal;
        }


        private IEnumerable<string> TextVersion()
        {
            List<string> lines = new List<string>
            {
                "<ACROSS PUZZLE V2>",
                "<TITLE>",
                $"\t{_title}",
                "<AUTHOR>",
                $"\t{_author}",
                "<COPYRIGHT>",
                $"\t{_copyright}",
                "<SIZE>",
                $"\t{_colCount}x{_rowCount}",
                "<GRID>"
            };

            // Output the grid

            for (int r = 0; r < _rowCount; r++)
            {
                string line = "\t";

                for (int c = 0; c < _colCount; c++)
                    line += _grid[r, c];

                lines.Add(line);
            }

            // Clues

            lines.Add("<ACROSS>");
            lines.AddRange(_acrossClues.Select(line => $"\t{line}"));

            lines.Add("<DOWN>");
            lines.AddRange(_downClues.Select(line => $"\t{line}"));

            if (!string.IsNullOrEmpty(_notepad))
            {
                lines.Add("<NOTEPAD>");
                lines.Add(_notepad);
            }


            return lines;

        }
    }
}
