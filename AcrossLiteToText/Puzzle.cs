using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AcrossLiteToText
{
    class Puzzle
    {
        public bool IsValid { get; }                   // true if parsing successful
        public bool IsLocked { get; }                               // true if the puzzle is locked, in which case IsValid will also be false
        public int RowCount { get; }                                // grid dimensions
        public int ColCount { get; }
        public string Title { get; }                   // Text strings are directly from Across Lite but are cleaned up to remove curly quotes and apostophes
        public string Author { get; }
        public string Copyright { get; }
        public string Notepad { get; }
        public string AcrossClues { get; }             // Clues separated by \r\n
        public string DownClues { get;}


        // Public arrays with letters, numbers, substitutions, and circle data

        public char[,] Grid { get; private set; }               // array that holds the letters in each box
        public int[,] GridNums { get; private set; }            // array that has the grid numbers
        public int[,] RebusNums { get; private set; }           // array that has indexes for substitution table
        public bool[,] HasCircle { get; private set; }          // true for each square with a circle

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

        // Constants

        const int ColumnsOffset = 0x2c;                  // number of columns is at this offset in Across Lite file
        const int RowsOffset = 0x2d;                    // number of rows is in next byte
        const int GridOffset = 0x34;                    // standard location to start parsing grid data in binary stream


        // BUG BUGBUG CHECK THESE

        private byte[] _numTable;                   // linear array of grid clue numbers
        private byte[] GridTableBytes { get; }

        public Puzzle(byte[] b, bool bIsDiagramless = false)
        {
            // Reject locked puzzles unless they're Variety type
            // Admins can load locked puzzles

            IsLocked = b[0x32] != 0 || b[0x33] != 0;    // is Across Lite file encrypted?

            if (IsLocked)
                return;

            // Looks like we have a good puzzle

            ColCount = b[ColumnsOffset];        // number of columns
            RowCount = b[RowsOffset];           // number of rows

            // We now know how big the puzzle is so we can generate the grids

            Grid = new char[RowCount, ColCount];
            GridNums = new int[RowCount, ColCount];
            int i = GridOffset;     // i indexes through byte array b[] starting at standard offset location

            // If the next byte is NOT either a empty square or a filled in square indicator,
            // the puzzle has been fixed up, either to include Subs in older puzzles or to show solved but
            // still encrypted puzzles.
            //
            // 0x2E means black square (block) and 0x2D is empty square.
            // Note 0x2E is valid in both sections so find first non black square and check if it's a blank

            int nAnswerOffset = GridOffset + (RowCount * ColCount);
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

            // fill grid and check letters used and scrabble scores

            bool[] bUsed = new bool[26];
            GridTableBytes = new byte[RowCount * ColCount];
            int j = 0;

            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColCount; c++)
                {
                    char cLetter = (char)b[i++];

                    if (cLetter >= 'A' && cLetter <= 'Z')
                    {
                        int nLetterNum = cLetter - 'A';
                        bUsed[nLetterNum] = true;
                    }
                    else if (cLetter == '.' || cLetter == ':')          // : is used for diagramless
                    {
                        cLetter = '.';      // normalize to .
                    }

                    Grid[r, c] = cLetter;
                    GridTableBytes[j++] = (byte)cLetter;
                }
            }

            // Now that the grid is filled in, a second pass can accurately assign grid numbers

            int num = 1;
            _numTable = new byte[RowCount * ColCount];

            j = 0;

            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColCount; c++)
                {
                    if (Grid[r, c] != '.')
                    {
                        if ((c == 0 || Grid[r, c - 1] == '.') && c != ColCount - 1 && Grid[r, c + 1] != '.')
                        {
                            _numTable[j] = (byte)num;
                            GridNums[r, c] = num++;
                        }
                        else if ((r == 0 || Grid[r - 1, c] == '.') && r != RowCount - 1 && Grid[r + 1, c] != '.')
                        {
                            _numTable[j] = (byte)num;
                            GridNums[r, c] = num++;
                        }
                    }

                    j++;
                }
            }


            // Get Title and Author, adjusting i along the way

            i = GridOffset + (2 * RowCount * ColCount); // move past grid

            Title = BinaryString(b, ref i); // get title
            i++;        // skip past \0 delimiter

            Author = BinaryString(b, ref i); // get and then parse Author
            i++;        // skip past \0 again

            Copyright = BinaryString(b, ref i);     // get Copyright

            if (bFixed)
                IsRebus = ParseFixedRebus(b, i);
            else
                IsRebus = ParseRebus(b, i);

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


            List<string> acrossClues = new List<string>();
            List<string> downClues = new List<string>();

            i++;    // skip past \0 again

            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColCount; c++)
                {
                    int gridNum = GridNums[r, c];

                    if (gridNum != 0) // if there is a grid number here
                    {
                        // Look first for an across clue

                        if ((c == 0 || Grid[r, c - 1] == '.') && c != ColCount - 1 && Grid[r, c + 1] != '.')
                        {
                            acrossClues.Add(BinaryString(b, ref i));
                            i++;
                        }

                        // Next look for a down clue at this same grid number using similar logic to above

                        if ((r == 0 || Grid[r - 1, c] == '.') && r != RowCount - 1 && Grid[r + 1, c] != '.')
                        {
                            downClues.Add(BinaryString(b, ref i));
                            i++;
                        }
                    }
                }
            }

            AcrossClues = string.Join("\n", acrossClues);
            DownClues = string.Join("\n", downClues);

            Notepad = BinaryString(b, ref i);

            HasCircles = ParseCircles(b, i); // Find all squares that have circles in them


            // For diagramless, set black squares back to ':'

            if (bIsDiagramless)
            {
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColCount; c++)
                        if (Grid[r, c] == '.')
                            Grid[r, c] = ':';
            }

            IsValid = true;

        }


















        private static string BinaryString(byte[] b, ref int i)
        {
            int nStart = i;

            while (b[i] != 0)
                i++;

            return Encoding.Default.GetString(b, nStart, i - nStart).Trim(); // default encoding means ANSI, not UTF-8
        }


        /// <summary>
        /// GetAcrossAnswer determines the across word at the specified grid location
        /// </summary>
        /// <param name="r">row</param>
        /// <param name="c">col</param>
        /// <returns></returns>
        private string GetAcrossAnswer(int r, int c)
        {
            StringBuilder sb = new StringBuilder();

            while (c < ColCount && Grid[r, c] != '.')
            {
                if (IsRebus && RebusNums[r, c] > 0)
                    sb.Append(RebusDict[RebusNums[r, c]]);
                else
                    sb.Append(Grid[r, c]);

                c++;
            }

            return sb.ToString();
        }


        /// <summary>
        /// GetDownAnswer determines the down word at the specified grid location
        /// </summary>
        /// <param name="r">row</param>
        /// <param name="c">col</param>
        /// <returns></returns>
        private string GetDownAnswer(int r, int c)
        {
            StringBuilder sb = new StringBuilder();

            while (r < RowCount && Grid[r, c] != '.')
            {
                if (IsRebus && RebusNums[r, c] > 0)
                    sb.Append(RebusDict[RebusNums[r, c]]);
                else
                    sb.Append(Grid[r, c]);

                r++;
            }

            return sb.ToString();
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

            while (i < (b.Length - (RowCount * ColCount)))
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

                HasCircle = new bool[RowCount, ColCount];
                CircleTableBytes = new byte[RowCount * ColCount];

                for (int r = 0; r < RowCount; r++)
                {
                    for (int c = 0; c < ColCount; c++, i++, j++)
                    {
                        if (b[i] == 0x80 || b[i] == 0xC0)   // 0x80 means circle, 0xC0 means circle in diagramless
                        {
                            HasCircle[r, c] = true;
                            CircleTableBytes[j] = 1;
                            CircleCount++;
                            bFound = true;
                        }
                        else
                        {
                            HasCircle[r, c] = false;
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

            while (i < (b.Length - (RowCount * ColCount)))
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

                RebusNums = new int[RowCount, ColCount];
                RebusTableBytes = new byte[RowCount * ColCount];

                for (int r = 0; r < RowCount; r++)
                {
                    for (int c = 0; c < ColCount; c++)
                    {
                        int nSubNumber = b[i++];

                        RebusTableBytes[j++] = (byte)nSubNumber;
                        RebusNums[r, c] = nSubNumber;

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

            while (i < (b.Length - (RowCount * ColCount)))
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

                RebusNums = new int[RowCount, ColCount];
                int nSubIndex = 0;

                StringBuilder sbSubs = new StringBuilder();
                RebusTableBytes = new byte[RowCount * ColCount];
                int j = 0;

                for (int r = 0; r < RowCount; r++)
                {
                    for (int c = 0; c < ColCount; c++)
                    {
                        if (b[i] != 0)
                        {
                            RebusCount++;
                            StringBuilder sb = new StringBuilder();

                            while (b[i] != 0)
                                sb.Append((char)b[i++]);

                            RebusNums[r, c] = nSubIndex + 1;
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

    }
}
