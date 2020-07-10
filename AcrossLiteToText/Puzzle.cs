using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcrossLiteToText
{
    /// <summary>
    /// This class parses a binary .puz (Across Lite) file, and provides a
    /// method to output a text version of that crossword.
    /// </summary>
    
    internal class Puzzle
    {
        // Across Lite encodes non-ASCII characters in strings in ANSI,
        // in particular, the version in codepage 1252, specified as ISO-8859-1.
        // Strings are read, and then must be written to text files, using this encoding.

        public readonly Encoding AnsiEncoding = Encoding.GetEncoding("ISO-8859-1");

        // Public properties

        public bool IsValid { get; }                // true if file successfully parsed
        public bool IsLocked { get; }               // true if the Across Lite puzzle is locked

        public IEnumerable<string> Text => TextVersion();

        // Private properties for all Across Lite V1 data

        private readonly string _title, _author, _copyright, _notepad;
        private readonly int _rowCount, _colCount;
        private readonly char[,] _grid;
        private readonly int _gridSize;
        private readonly bool _isDiagramless;
        private const char block = '.';

        private readonly List<string> _acrossClues = new List<string>();
        private readonly List<string> _downClues = new List<string>();

        // Circles

        private readonly bool _hasCircles;      // does this puzzle have any circles?
        private bool[,] _hasCircle;             // true if individual grid squares have circles?

        // Rebus

        private readonly bool _isRebus;         // does this puzzle have any rebus entries?
        private int[,] _rebusKeys;              // 0 means no rebus in this square, otherwise it's the dictionary key

        private Dictionary<int, string> _rebusDict = new Dictionary<int, string>();
        private static char RebusKey(int nValue) => nValue < 10 ? (char)(nValue + '0') : (char)(nValue + 'a' - 10);


        /// <summary>
        /// Constructors takes a byte array of the .puz file contents.
        /// Call with something like:
        ///     Puzzle puz = new Puzzle(File.ReadAllBytes(file.FullName));
        /// </summary>
        /// <param name="b"></param>
        public Puzzle(byte[] b)
        {
            // Standard locations of key data

            const int columnsOffset = 0x2c;             // number of columns is at this offset in Across Lite file
            const int rowsOffset = 0x2d;                // number of rows is in next byte
            const int gridOffset = 0x34;                // standard location to start parsing grid data in binary stream

            // Reject locked puzzles unless they're Variety type

            // BUG -- handle encrypted puzzles if answers are typed in

            IsLocked = b[0x32] != 0 || b[0x33] != 0;    // is Across Lite file encrypted?

            if (IsLocked)
                return;

            // Looks like we have a good puzzle

            _colCount = b[columnsOffset];           // number of columns
            _rowCount = b[rowsOffset];              // number of rows
            _gridSize = _colCount * _rowCount;      // size of grid info in byte array

            // We now know how big the puzzle is so we can generate the grids

            _grid = new char[_rowCount, _colCount];
            int[,] gridNumbers = new int[_rowCount, _colCount];

            // i indexes through byte array b[] starting at standard offset location.
            // It is updated as each new datapoint is extracted.

            int i = gridOffset;

            // If the next byte is NOT either a empty square or a filled in square indicator,
            // the puzzle has been fixed up, either to include Subs in older puzzles or to show solved but
            // still encrypted puzzles.
            //
            // 0x2E means black square (block) and 0x2D is empty square.
            // Note 0x2E is valid in both sections so find first non black square and check if it's a blank

            int answerOffset = gridOffset + _gridSize;
            int nOff = answerOffset;
            bool isManuallyFilled = false;  // assume didn't have to manually enter solution (fixing is necessary for old V1 rebus puzzles)

            while (b[nOff] == 0x2E || b[nOff] == 0x3A) // go to first non-black square
                nOff++;

            if (b[nOff] != 0x2D)        // if it's not a space character
            {
                isManuallyFilled = true;
                i = answerOffset;
            }

            // i now points to start of grid with unencrypted solution,
            // so we can fill the _grid array with answer letters in each square.

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    char cLetter = (char)b[i++];

                    if (cLetter == ':')
                    {
                        _isDiagramless = true;      // : indicates "black" square for diagramless
                        _grid[r, c] = block;        // but normalize normalize to . and fix later.
                    }
                    else
                        _grid[r, c] = cLetter;
                }
            }

            // Now that the grid is filled in, a second pass can accurately assign grid numbers

            int num = 1;

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    if (_grid[r, c] != block)
                    {
                        // if start of Across word

                        if ((c == 0 || _grid[r, c - 1] == block) && c != _colCount - 1 && _grid[r, c + 1] != block)
                        {
                            gridNumbers[r, c] = num++;
                        }

                        // else if start of Down word

                        else if ((r == 0 || _grid[r - 1, c] == block) && r != _rowCount - 1 && _grid[r + 1, c] != block)
                        {
                            gridNumbers[r, c] = num++;
                        }
                    }
                }
            }

            // Move the i index past the grid

            i = gridOffset + (2 * _gridSize);

            // Get title, author, and copyright. NextString() adjusts i along the way.

            // Litsoft docutmentation says copyright symbol not needed, so perhaps
            // copyright should be NextString().Replace("©", "").Trim();

            _title = NextString();
            _author = NextString();
            _copyright = NextString();

            // Figure out clues. They are ordered in Across Lite in an odd way.
            // Look for the next numbered cell. If there is an across clue starting there,
            // that's the next clue. If there is only a down starting there, it's next.
            // If there is both an across and a down, the across comes first.
            // Then proceed to next grid number.

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    int gridNum = gridNumbers[r, c];

                    if (gridNum != 0)   // if there is a grid number here
                    {
                        // Look first for an across clue

                        if ((c == 0 || _grid[r, c - 1] == block) && c != _colCount - 1 && _grid[r, c + 1] != block)
                        {
                            _acrossClues.Add(NextString());
                        }

                        // Next look for a down clue at this same grid number using similar logic to above

                        if ((r == 0 || _grid[r - 1, c] == block) && r != _rowCount - 1 && _grid[r + 1, c] != block)
                        {
                            _downClues.Add(NextString());
                        }
                    }
                }
            }

            _notepad = NextString();

            // Finally, get Circle and Rebus data.
            // These functions return a boolean indicating whether circles or rebus squares exist,
            // and they also fill the relevant private property variables.

            _hasCircles = ParseCircles(b, i);
            //_isRebus = isManuallyFilled ? ParseFixedRebus(b, i) : ParseRebus(b, i);

            _isRebus = ParseRebus(isManuallyFilled ? "RUSR" : "GRBS", b, i);

            IsValid = true;

            //
            // NextString() is a LOCAL FUNCTION so it has access to the byte array b and the index i.
            //
            
            string NextString()
            {
                int nStart = i;     // remember starting location

                // find string length by searching for terminating character '\0'

                while (b[i] != 0)
                    i++;            

                string str = AnsiEncoding.GetString(b, nStart, i - nStart).Trim();

                i++;                // move index past trailing '\0' so it's ready for the next NexString()
                return str;
            }
        }


        /// <summary>
        /// Fills _hasCircle[r,c] array with true for each square that includes a circle, and false otherwise.
        /// </summary>
        /// <param name="b">binary array to parse</param>
        /// <param name="i">offset in b to start searching</param>
        /// <returns>true if at least one circle was found</returns>
        private bool ParseCircles(IReadOnlyList<byte> b, int i)
        {
            bool bFound = false; // assume none found

            // Search for GEXT which marks the start of the circle data

            while (i < b.Count - _gridSize)
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
                i += 8;             // offset from GEXT
                bFound = false;     // reset, so now check if circles actually found

                _hasCircle = new bool[_rowCount, _colCount];

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++, i++)
                    {
                        if (b[i] == 0x80 || b[i] == 0xC0)   // 0x80 means circle, 0xC0 means circle in diagramless
                        {
                            _hasCircle[r, c] = true;
                            bFound = true;
                        }
                    }
                }
            }

            return bFound;
        }


        /// <summary>
        /// Look for rebus info in binary array.
        /// User marker "GRBS" for unlocked rebus puzzle, or "RUSR" for manually solved puzzles.
        /// </summary>
        /// <param name="b">binary array to parse</param>
        /// <param name="i">offset in b to start searching</param>
        /// <returns>true if at least one rebus entry was found</returns>
        private bool ParseRebus(string marker, IReadOnlyList<byte> b, int i)
        {
            bool bFound = false;    // assume not found

            while (i < b.Count - _gridSize)
            {
                if (b[i] == marker[0] && b[i + 1] == marker[1] && b[i + 2] == marker[2] && b[i + 3] == marker[3])
                {
                    bFound = true;  // need to check later
                    break;
                }

                i++;
            }

            if (bFound)             // if marker found
            {
                i += 8;             // offset from marker
                bFound = false;     // reset, so now check if rebus entries actually exist

                _rebusKeys = new int[_rowCount, _colCount];

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++)
                    {
                        int nSubNumber = b[i++];

                        _rebusKeys[r, c] = nSubNumber;

                        if (nSubNumber > 0)
                            bFound = true;
                    }
                }

                // If actual rebus data found, parse it and return true

                if (bFound)
                {
                    i += 9;     // skip to start of substring table

                    StringBuilder sb = new StringBuilder();

                    while (b[i] != 0)
                        sb.Append((char)b[i++]);

                    _rebusDict = CrackSubstring(sb.ToString());
                }
            }

            return bFound;
        }


        /// <summary>
        /// CrackSubstring takes a string which looks like " 1:FIRST; 2:SECOND; 3:THIRD;"
        /// or possibly "19:SECOND;26FIRST;33THIRD;35HOME;" and creates a dictionary where the
        /// string part is keyed to the number plus one (since that's how Across Lite binary format works.)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Dictionary<int, string> CrackSubstring(string str)
        {
            Dictionary<int, string> subKeyVal = new Dictionary<int, string>();

            string[] rawParts = str.Trim().Split(';');

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
            Dictionary<string, int> rebusDict = new Dictionary<string, int>();              // for standard rebus
            Dictionary<string, char> circleAndRebusDict = new Dictionary<string, char>();   // for rebus AND circle
            List<string> circleAndRebusList = new List<string>();

            int key = 0;                    // standard rebus uses numbers
            char rebusCircleKey = 'z';      // start from the end to minimize conflict risk

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
                string line = "\t";     // each row starts a new line

                for (int c = 0; c < _colCount; c++)
                {
                    bool hasRebus = _isRebus && _rebusKeys[r, c] != 0;
                    bool hasCircle = _hasCircles && _hasCircle[r, c];

                    if (hasRebus && hasCircle)
                    {
                        string rebusData = $"{_rebusDict[_rebusKeys[r, c]]}:{_rebusDict[_rebusKeys[r, c]][0]}";

                        if (circleAndRebusDict.TryGetValue(rebusData, out char ch))
                        {
                            line += ch;
                        }
                        else
                        {
                            line += rebusCircleKey;
                            circleAndRebusDict.Add(rebusData, rebusCircleKey);
                            circleAndRebusList.Add($"{rebusCircleKey}:{_rebusDict[_rebusKeys[r, c]]}:{_rebusDict[_rebusKeys[r, c]][0]}");
                            rebusCircleKey--;
                        }
                    }
                    else if (hasRebus)
                    {
                        string rebusData = $"{_rebusDict[_rebusKeys[r, c]]}:{_grid[r, c]}";

                        if (rebusDict.TryGetValue(rebusData, out int n))
                        {
                            line += RebusKey(n);
                        }
                        else
                        {
                            rebusDict.Add(rebusData, key);
                            line += RebusKey(key);
                            key++;
                        }
                    }
                    else if (hasCircle)
                    {
                        line += char.ToLower(_grid[r, c]);
                    }
                    else if (_isDiagramless && _grid[r, c] == block)
                    {
                        line += ":";
                    }
                    else
                    {
                        line += _grid[r, c];
                    }
                }

                lines.Add(line);
            }

            if (_hasCircles || _isRebus)
            {
                lines.Add("<REBUS>");

                if (_hasCircles)
                    lines.Add("MARK;");

                // Rebus strings are added to a list so they can be sorted, just to look prettier.

                List<string> rebusDataList = rebusDict.Select(r => $"{RebusKey(r.Value)}:{r.Key}").ToList();

                rebusDataList.Sort();

                lines.AddRange(rebusDataList);

                // Output rebus into for squares that also have circles.

                lines.AddRange(circleAndRebusList);
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
