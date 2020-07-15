using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


// Copyright (C) 2020, Jim Horne
//
// This program is free software.
// See https://github.com/jahorne/AcrossLiteToText/blob/master/LICENSE


namespace AcrossLiteToText
{
    /// <summary>
    ///
    ///The constructor here takes a byte array from a binary Across Lite.puz file.
    ///
    /// A public Text property returns a list of strings that can be directly
    /// written to a text file using the appropriate encoding.
    ///
    /// Parse Across Lite file: Puzzle puz = new Puzzle(File.ReadAllBytes(file.FullName));
    ///
    /// Generate text file: File.WriteAllLines(sTextFileName, puz.Text, puz.AnsiEncoding);
    ///
    /// This code is derived from a similar class in XWord Info.
    /// 
    /// </summary>

    internal class Puzzle
    {
        // Public properties

        public bool IsValid { get; }                // true if file successfully parsed
        public bool IsLocked { get; }               // true if the Across Lite puzzle is locked

        public IEnumerable<string> Text => TextVersion();

        // Across Lite encodes non-ASCII characters in ANSI, in particular,
        // the version of ANSI defined by codepage 1252, specified as ISO-8859-1.
        // Strings are read, and then must be written to text files, using this encoding.

        public readonly Encoding AnsiEncoding = Encoding.GetEncoding("ISO-8859-1");

        // Private properties

        private readonly string _title, _author, _copyright, _notepad;
        private readonly int _rowCount, _colCount;
        private readonly char[,] _grid;
        private readonly int _gridSize;
        private readonly bool _isDiagramless;
        private const char Block = '.';

        private readonly List<string> _acrossClues = new List<string>();
        private readonly List<string> _downClues = new List<string>();

        // Circles

        private readonly bool _hasCircles;      // does this puzzle have any circles?
        private bool[,] _hasCircle;             // true if individual grid squares have circles?

        // Rebus

        private readonly bool _isRebus;         // does this puzzle have any rebus entries?
        private int[,] _rebusKeys;              // 0 means no rebus in this square, otherwise it's the dictionary key

        private Dictionary<int, string> _rebusLookup = new Dictionary<int, string>();


        /// <summary>
        /// Constructor takes a byte array of the .puz file contents.
        /// </summary>
        /// <param name="b"></param>
        public Puzzle(byte[] b)
        {
            // Standard locations of key data

            const int columnsOffset = 0x2c;             // number of columns is at this offset in Across Lite file
            const int rowsOffset = 0x2d;                // number of rows is in next byte
            const int gridOffset = 0x34;                // standard location to start parsing grid data in binary stream

            // Check if puzzle is locked.
            // We'll proceed anyway, in case the puzzle is manually solved.

            IsLocked = b[0x32] != 0 || b[0x33] != 0;    // is Across Lite file encrypted?

            // Grid dimensions

            _colCount = b[columnsOffset];           // number of columns
            _rowCount = b[rowsOffset];              // number of rows
            _gridSize = _colCount * _rowCount;      // size of grid info in byte array

            // We now know how big the puzzle is so we can generate the grids

            _grid = new char[_rowCount, _colCount];
            int[,] gridNumbers = new int[_rowCount, _colCount];

            // i indexes through byte array b[] starting at standard offset location.
            // It is updated as each new data point is extracted.

            int i = gridOffset;

            // If the next byte is NOT either a empty square or a filled in square indicator,
            // the puzzle has been fixed up, either to include Subs in older puzzles or to show solved but
            // still encrypted puzzles.
            //
            // 0x2E means black square (block) and 0x2D is empty square.
            // Note 0x2E is valid in both sections so find first non black square and check if it's a blank.

            int answerOffset = gridOffset + _gridSize;
            int nOff = answerOffset;
            bool isManuallySolved = false;  // assume didn't have to manually enter solution

            // go to first non-black square

            while (b[nOff] == 0x2E || b[nOff] == 0x3A)
                nOff++;

            // if it's not a space character, we mark the puzzle as manually solved and
            // move i to the start of the hand-entered solution

            if (b[nOff] != 0x2D)
            {
                isManuallySolved = true;
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
                        _grid[r, c] = Block;        // but normalize normalize to . and fix later.
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
                    if (_grid[r, c] != Block)
                    {
                        // If an Across answer starts here, it needs a grid number

                        if ((c == 0 || _grid[r, c - 1] == Block) && c != _colCount - 1 && _grid[r, c + 1] != Block)
                        {
                            gridNumbers[r, c] = num++;
                        }

                        // Or if a Down answer starts here

                        else if ((r == 0 || _grid[r - 1, c] == Block) && r != _rowCount - 1 && _grid[r + 1, c] != Block)
                        {
                            gridNumbers[r, c] = num++;
                        }
                    }
                }
            }

            // We're ready to start reading string data.
            // Start by moving i past the grid.

            i = gridOffset + (2 * _gridSize);

            // Get title, author, and copyright. NextString() adjusts i along the way.

            _title = NextString();
            _author = NextString();
            _copyright = NextString();      // or perhaps NextString().Replace("©", "").Trim();

            // Figure out clues. They are ordered in Across Lite in an odd way.
            // Look for the next numbered cell. If an Across answer starts there,
            // that's the next clue. If there is only a Down starting there, it's next.
            // If there is both an Across and a Down, the Across comes first.
            // Then proceed to next grid number.

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    if (gridNumbers[r, c] != 0)         // if there is a grid number here...
                    {
                        // If it's the start of an Across answer, then the next string is an Across clue

                        if ((c == 0 || _grid[r, c - 1] == Block) && c != _colCount - 1 && _grid[r, c + 1] != Block)
                        {
                            _acrossClues.Add(NextString());
                        }

                        // Next look for a Down clue at this same grid number using similar logic to above

                        if ((r == 0 || _grid[r - 1, c] == Block) && r != _rowCount - 1 && _grid[r + 1, c] != Block)
                        {
                            _downClues.Add(NextString());
                        }
                    }
                }
            }

            _notepad = NextString();

            // Finally, get Circle and Rebus data.
            // These functions return a boolean indicating whether circles or rebus squares exist,
            // and they fill the relevant arrays.

            _hasCircles = ParseCircles(b, i);
            _isRebus = ParseRebus(isManuallySolved, b, i);

            IsValid = true;

            // End of constructor


            //
            // NextString() is a LOCAL function so it captures the byte array b and the index i.
            //
            
            string NextString()
            {
                int startLocation = i;

                // find string length by searching for terminating '\0'

                while (b[i] != 0)
                    i++;            

                string str = AnsiEncoding.GetString(b, startLocation, i - startLocation).Trim();

                // Move index past trailing '\0' so it's ready for the next NexString()
                // and return result.

                i++;
                return str;
            }
        }


        /// <summary>
        /// Fill _hasCircle[r, c] array with true for each square that includes a circle.
        /// </summary>
        /// <param name="b">binary array to parse</param>
        /// <param name="n">offset in b to start searching</param>
        /// <returns>true if at least one circle was found</returns>
        private bool ParseCircles(IReadOnlyList<byte> b, int n)
        {
            const string marker = "GEXT";   // marks the start of the circle data
            bool found = false;             // assume none found

            // Search for marker that indicates start of circle data

            while (n < b.Count - _gridSize)
            {
                if (b[n] == marker[0] && b[n + 1] == marker[1] && b[n + 2] == marker[2] && b[n + 3] == marker[3])
                {
                    found = true;  // need to check later
                    break;
                }

                n++;
            }

            if (found)              // if marker found (might be bogus)
            {
                n += 8;             // offset from GEXT
                found = false;      // reset

                _hasCircle = new bool[_rowCount, _colCount];    // array to store circle data

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++, n++)
                    {
                        // 0x80 means circle, 0xC0 means circle in diagramless

                        if (b[n] == 0x80 || b[n] == 0xC0)
                        {
                            _hasCircle[r, c] = true;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }


        /// <summary>
        /// Fill _rebusKeys[r, c] array with key for each rebus entry found.
        /// Convert rebus details into dictionary.
        /// The standard rebus data indicator is GRBS but RUSR is used if
        /// the puzzle has been manually solved, perhaps because it is locked.
        /// </summary>
        /// <param name="isManuallySolved">If true, look at user-entered solution</param>
        /// <param name="b">binary array to parse</param>
        /// <param name="n">offset into b to start searching</param>
        /// <returns>true if at least one rebus entry found</returns>
        private bool ParseRebus(bool isManuallySolved, IReadOnlyList<byte> b, int n)
        {
            string marker = isManuallySolved ? "RUSR" : "GRBS";
            bool found = false;

            while (n < b.Count - _gridSize)
            {
                if (b[n] == marker[0] && b[n + 1] == marker[1] && b[n + 2] == marker[2] && b[n + 3] == marker[3])
                {
                    found = true;   // need to check later
                    break;
                }

                n++;
            }

            if (found)              // if marker found (might be bogus)
            {
                n += 8;             // offset from marker
                found = false;      // reset

                _rebusKeys = new int[_rowCount, _colCount];     // array to location of rebus squares

                for (int r = 0; r < _rowCount; r++)
                {
                    for (int c = 0; c < _colCount; c++)
                    {
                        int rebusKey = b[n++];

                        _rebusKeys[r, c] = rebusKey;

                        if (rebusKey > 0)
                            found = true;
                    }
                }

                // If actual rebus data found, parse it and return true

                if (found)
                {
                    n += 9;     // skip to start of substring table

                    StringBuilder sb = new StringBuilder();

                    while (b[n] != 0)
                        sb.Append((char)b[n++]);

                    _rebusLookup = CrackRebusSubstitutionString(sb.ToString());
                }
            }

            return found;
        }


        /// <summary>
        /// This function takes a string which looks like " 1:FIRST; 2:SECOND; 3:THIRD;"
        /// or possibly "19:SECOND;26FIRST;33THIRD;35HOME;" and creates a dictionary where the
        /// string part is keyed to the number plus one (since that's what Across Lite binary uses.)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Dictionary<int, string> CrackRebusSubstitutionString(string str)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();

            string[] rawParts = str.Trim().Split(';');

            // Key is number before colon plus offset (1)

            int partsCount = rawParts.Length - 1;       // ignore part with trailing ';'

            for (int n = 0; n < partsCount; n++)
            {
                string rebusData = rawParts[n];
                string[] rebusParts = rebusData.Split(':');
                int nKey = Convert.ToInt32(rebusParts[0]);
                dict.Add(nKey + 1, rebusParts[1]);
            }

            return dict;
        }


        /// <summary>
        /// Return the text version of this puzzle data as a list of strings, one for each line.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> TextVersion()
        {
            // Start with standard header information

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

            // Usually this just means output rows in _grid, one per line but circles
            // and rebus squares have special handling.

            Dictionary<string, char> rebusDict = new Dictionary<string, char>();    // rebus keys and associated strings

            int rebusNumber = 0;        // standard rebus uses numbers, starting here and increasing
            char rebusCircleKey = 'z';  // circles with rebus uses letters, starting here and going backwards to reduce conflict odds

            for (int r = 0; r < _rowCount; r++)
            {
                string line = "\t";     // each row starts a new line

                for (int c = 0; c < _colCount; c++)
                {
                    bool hasRebus = _isRebus && _rebusKeys[r, c] != 0;      // true if this square has a rebus
                    bool hasCircle = _hasCircles && _hasCircle[r, c];       // true if this square has a circle

                    if (hasRebus)
                    {
                        // Look for existing rebus element, and reuse that same key if found.
                        // Otherwise, use increasing numbers for standard rebus squares,
                        // or decreasing lower-case letters for rebus squares that also have circles.

                        string rebusData = $"{_rebusLookup[_rebusKeys[r, c]]}:{_grid[r, c]}";

                        if (rebusDict.TryGetValue(rebusData, out char ch))
                        {
                            line += ch;
                        }
                        else
                        {
                            if (hasCircle)
                            {
                                line += rebusCircleKey;
                                rebusDict.Add(rebusData, rebusCircleKey--);
                            }
                            else
                            {
                                char rebusKey = GetRebusKey(rebusNumber++);
                                line += rebusKey;
                                rebusDict.Add(rebusData, rebusKey);
                            }
                        }
                    }
                    else if (hasCircle)
                    {
                        // Circles are indicated with lower-case letters

                        line += char.ToLower(_grid[r, c]);
                    }
                    else if (_isDiagramless && _grid[r, c] == Block)
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

            // <REBUS> indicates circles or true rebus strings.
            // MARK: means squares with lower-case letters should be circled.

            if (_hasCircles || _isRebus)
            {
                lines.Add("<REBUS>");

                if (_hasCircles)
                    lines.Add("MARK;");

                lines.AddRange(rebusDict.Select(r => $"{r.Value}:{r.Key}"));
            }

            // Clues

            lines.Add("<ACROSS>");
            lines.AddRange(_acrossClues.Select(line => $"\t{line}"));

            lines.Add("<DOWN>");
            lines.AddRange(_downClues.Select(line => $"\t{line}"));

            // Notepad

            if (!string.IsNullOrEmpty(_notepad))
            {
                lines.Add("<NOTEPAD>");
                lines.Add(_notepad);
            }

            return lines;


            // Local function to convert rebus int value to a character, '0' to '9' first, then 'a' to 'z'.

            static char GetRebusKey(int nValue) => nValue < 10 ? (char)(nValue + '0') : (char)(nValue + 'a' - 10);
        }
    }
}