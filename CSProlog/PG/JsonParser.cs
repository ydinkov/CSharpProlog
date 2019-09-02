//#define showToken

namespace Prolog
{
    /* _______________________________________________________________________________________________
      |                                                                                               |
      |  C#Prolog -- Copyright (C) 2007-2014 John Pool -- j.pool@ision.nl                                  |
      |                                                                                               |
      |  This library is free software; you can redistribute it and/or modify it under the terms of   |
      |  the GNU General Public License as published by the Free Software Foundation; either version  |
      |  2 of the License, or any later version.                                                      |
      |                                                                                               |
      |  This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;    |
      |  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.    |
      |  See the GNU General Public License for details, or enter 'license.' at the command prompt.   |
      |_______________________________________________________________________________________________|
    */

    public partial class PrologEngine
    {
        #region JsonParser

        public partial class JsonParser : BaseParser<object>
        {
            private BaseTerm jsonListTerm;
            private OperatorTable opTable;
            public BaseTerm JsonListTerm => jsonListTerm;

            public OperatorTable OpTable
            {
                set => opTable = value;
            }

            #region ScanIdOrTerminal

            protected override void ScanIdOrTerminalOrCommentStart()
            {
                TerminalDescr tRec;
                var firstLow = char.IsLower(c: ch);
                var iPtr = streamInPtr;
                var tPtr = iPtr;
                var aLen = ch.IsIdStartChar() ? 1 : 0; // length of longest Identifier sofar
                var tLen = 0; // length of longest Terminal sofar
                var fCnt = 0; // count of calls to FindCharAndSubtree
                var isDot = ch == '.'; // remains valid only if symbol length is 1
                terminalTable.FindCharInSubtreeReset();

                while (fCnt++ >= 0 && terminalTable.FindCharInSubtree(c: ch, td: out tRec))
                {
                    if (tRec != null)
                    {
                        symbol.TerminalId = tRec.IVal;
                        symbol.Payload = tRec.Payload; // not used
                        symbol.Class = tRec.Class;
                        tLen = fCnt;
                        tPtr = streamInPtr; // next char to be processed

                        if (symbol.TerminalId == CommentStart || symbol.TerminalId == CommentSingle) return;

                        if (terminalTable.AtLeaf) break; // terminal cannot be extended
                    }

                    NextCh();

                    if (aLen == fCnt && (char.IsLetterOrDigit(c: ch) || ch == '_'))
                    {
                        aLen++;
                        iPtr = streamInPtr;
                    }
                } // fCnt++ by last (i.e. failing) Call

                /*
                        At this point: (in the Prolog case, read 'Identifier and Atom made up
                        from specials' for 'Identifier'):
                        - tLen has length of BaseTrie terminal (if any, 0 otherwise);
                        - aLen has length of Identifier (if any, 0 otherwise);
                        - fCnt is the number of characters inspected (for Id AND terminal)
                        - The character pointer is on the last character inspected (for both)
                        Iff aLen = fCnt then the entire sequence read sofar is an Identifier.
                        Now try extending the Identifier, only meaningful if iLen = fCnt.
                        Do not do this for an Atom made up from specials if a Terminal was recognized !!
                */

                if (aLen == fCnt)
                    while (true)
                    {
                        NextCh();

                        if (char.IsLetterOrDigit(c: ch) || ch == '_')
                        {
                            aLen++;
                            iPtr = streamInPtr;
                        }
                        else
                        {
                            break;
                        }
                    }

                if (aLen > tLen) // tLen = 0 iff Terminal == Undefined
                {
                    symbol.TerminalId = Identifier;
                    symbol.Class = SymbolClass.Id;
                    InitCh(c: iPtr);
                }
                else if (symbol.TerminalId == Undefined)
                {
                    InitCh(c: iPtr);
                }
                else // we have a terminal != Identifier
                {
                    if (aLen == tLen) symbol.Class = SymbolClass.Id;
                    InitCh(c: tPtr);
                }

                NextCh();
            }

            #endregion

            #region NextSymbol

            protected override void NextSymbol(string _Proc)
            {
                if (symbol.AbsSeqNo != 0 && streamInPtr.FOnLine) streamInPtr.FOnLine = false;

                symbol.PrevFinal = symbol.Final;

                if (symbol.TerminalId == EndOfInput)
                    SyntaxError = "*** Trying to read beyond end of input";

                prevTerminal = symbol.TerminalId;
                symbol.Class = SymbolClass.None;
                symbol.Payload = null;
                var Break = false;

                do
                {
                    while (char.IsWhiteSpace(c: ch)) NextCh();

                    symbol.Start = streamInPtr.Position;
                    symbol.LineNo = streamInPtr.LineNo;
                    symbol.LineStart = streamInPtr.LineStart;
                    symbol.TerminalId = Undefined;

                    if (endText)
                        symbol.TerminalId = EndOfInput;
                    else if (streamInPtr.EndLine)
                        symbol.TerminalId = EndOfLine;
                    else if (char.IsDigit(c: ch))
                        ScanNumber();
                    else if (ch == DQUOTE)
                        ScanString();
                    else
                        ScanIdOrTerminalOrCommentStart();

                    symbol.Final = streamInPtr.Position;

                    if (symbol.TerminalId == EndOfLine)
                    {
                        eoLineCount++;
                        NextCh();
                        Break = seeEndOfLine;
                    }
                    else
                    {
                        eoLineCount = 0;

                        switch (symbol.TerminalId)
                        {
                            case Identifier:
                                Break = true;
                                break;
                            case EndOfInput:
                                Break = true;
                                break;
                            case CommentStart:
                                if (stringMode)
                                    Break = true;

                                if (!DoComment("*/", true, firstOnLine: streamInPtr.FOnLine))
                                    ErrorMessage = "Unterminated comment starting at line " + symbol.LineNo;

                                break;
                            case CommentSingle:
                                if (stringMode) Break = true;
                                else Break = false;
                                DoComment("\n", false, firstOnLine: streamInPtr.FOnLine);
                                eoLineCount = 1;

                                if (seeEndOfLine)
                                {
                                    symbol.TerminalId = EndOfLine;
                                    Break = true;
                                }

                                break;
                            default:
                                if (seeEndOfLine && symbol.TerminalId != EndOfLine) streamInPtr.FOnLine = false;

                                Break = true;
                                break;
                        }
                    }
                } while (!Break);

                symbol.AbsSeqNo++;
                symbol.RelSeqNo++;
#if showToken // a Console.Clear () will wipe out this output!
        IO.WriteLine ("NextSymbol[{0}] line {1}: '{2}' [{3}]",
                           symbol.AbsSeqNo, symbol.LineNo, symbol.ToString (), symbol.ToName ());
#endif
            }

            #endregion NextSymbol

            #region ScanNumber

            protected override void ScanNumber() // overridden: also scans hex numbers
            {
                bool isReal;
                var hexFound = false;
                StreamPointer savPosition;
                var ok = true;

                do
                {
                    NextCh();

                    if (!(ok = char.IsDigit(c: ch)))
                        if (ok = ch.IsHexChar())
                            hexFound = true;
                } while (ok);

                symbol.TerminalId = IntLiteral;
                isReal = true; // assumed until proven conversily

                if (!hexFound)
                {
                    if (ch == '.') // fractional part?
                    {
                        // save dot position
                        savPosition = streamInPtr;
                        NextCh();

                        if (char.IsDigit(c: ch))
                        {
                            symbol.TerminalId = RealLiteral;

                            do
                            {
                                NextCh();
                            } while (char.IsDigit(c: ch));
                        }
                        else // not a digit after period
                        {
                            InitCh(c: savPosition); // 'unread' dot
                            isReal = false; // ... and remember this
                        }
                    }

                    if (isReal) // integer or real, possibly with scale factor
                    {
                        savPosition = streamInPtr;

                        if (ch == 'e' || ch == 'E')
                        {
                            // scale factor
                            NextCh();

                            if (ch == '+' || ch == '-') NextCh();

                            if (char.IsDigit(c: ch))
                            {
                                do
                                {
                                    NextCh();
                                } while (char.IsDigit(c: ch));

                                symbol.TerminalId = RealLiteral;
                            }
                            else if (!stringMode) // Error in real syntax
                            {
                                InitCh(c: savPosition);
                            }
                        }
                    }
                }

                symbol.Final = streamInPtr.Position;
            }


            protected override bool ScanFraction()
            {
                var savPosition = streamInPtr; // Position of '.'

                do
                {
                    NextCh();
                } while (char.IsDigit(c: ch));

                var result = streamInPtr.Position > savPosition.Position + 1; // a fraction

                if (result)
                {
                    if (ch == 'i')
                    {
                        symbol.TerminalId = ImagLiteral;
                        NextCh();
                    }
                    else
                    {
                        symbol.TerminalId = RealLiteral;
                    }
                }
                else
                {
                    InitCh(c: savPosition);
                }

                return result;
            }

            #endregion ScanNumber
        }

        #endregion JsonParser
    }

    #region Extensions class

    internal static class JsonParserExtensions
    {
        public static bool IsHexChar(this char c)
        {
            return c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
        }
    }

    #endregion Extensions class
}