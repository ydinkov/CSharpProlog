//#define showToken

using System;

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
        #region PrologParser

        public partial class PrologParser : BaseParser<OpDescrTriplet>
        {
            #region ScanVerbatimString

            protected void ScanVerbatimString()
            {
                do
                {
                    if (ch == DQUOTE)
                    {
                        NextCh();

                        if (ch != DQUOTE)
                        {
                            symbol.TerminalId = VerbatimStringLiteral;

                            break;
                        }
                    }

                    NextCh();
                } while (!endText);

                symbol.Start++;
                symbol.Class = SymbolClass.Text;
                symbol.Final = streamInPtr.Position;

                if (symbol.TerminalId != VerbatimStringLiteral)
                    SyntaxError = string.Format(
                        "Unterminated verbatim string: {0}\r\n(remember to use \"\" instead of \\\" for an embedded \")",
                        symbol.ToString());
            }

            #endregion

            #region ScanQuotedAtom

            void ScanQuotedAtom(out bool canUnquote)
            {
                canUnquote = true;
                bool specialsOnly = true;
                bool first = true;

                do
                {
                    NextCh();

                    if (!streamInPtr.EndLine)
                    {
                        if (ch == '\'') // possibly an escape
                        {
                            NextCh();

                            if (streamInPtr.EndLine || ch != '\'') // done
                            {
                                specialsOnly &= !first;
                                canUnquote &= !first; // '' is an atom !!!
                                symbol.TerminalId = Atom;
                            }
                            else // atom contains quote
                                canUnquote = false;
                        }
                        else
                        {
                            if (first)
                                canUnquote = Char.IsLower(ch);
                            else
                                canUnquote = canUnquote && (ch == '_' || Char.IsLetterOrDigit(ch));

                            specialsOnly = specialsOnly && (ch.IsSpecialAtomChar());
                        }

                        first = false;
                    }
                } while (!(streamInPtr.EndLine) && symbol.TerminalId != Atom);

                if (streamInPtr.EndLine && symbol.TerminalId != StringLiteral)
                    SyntaxError = "Unterminated atom: " + symbol.ToString();

                canUnquote |= specialsOnly;

                // check whether the atom is an operator:
                int start = symbol.Start + (canUnquote ? 1 : 0);
                int stop = streamInPtr.Position - (canUnquote ? 1 : 0);
                TerminalDescr tRec;

                if (terminalTable.Find(StreamInClip(start, stop), out tRec))
                    if (tRec.Payload != null)
                    {
                        symbol.TerminalId = Operator;
                        symbol.Payload = tRec.Payload;
                    }
            }

            #endregion ScanQuotedAtom

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
                bool Break = false;
                bool canUnquote;

                do
                {
                    #region basic and conditional definition symbol handling

                    do
                    {
                        while (Char.IsWhiteSpace(ch)) NextCh();

                        symbol.Start = streamInPtr.Position;
                        symbol.LineNo = streamInPtr.LineNo;
                        symbol.LineStart = streamInPtr.LineStart;
                        symbol.TerminalId = Undefined;
                        symbol.Class = SymbolClass.None;
                        canUnquote = false; // used for quoted atoms only

                        if (endText)
                        {
                            symbol.TerminalId = EndOfInput;
                            cdh.HandleSymbol(symbol); // check for missing #endif missing
                        }
                        else if (streamInPtr.EndLine)
                            symbol.TerminalId = EndOfLine;
                        else if (Char.IsDigit(ch))
                            ScanNumber();
                        else if (ch == SQUOTE)
                            ScanQuotedAtom(out canUnquote);
                        else if (ch == DQUOTE)
                            ScanString();
                        else
                            ScanIdOrTerminalOrCommentStart();

                        symbol.Final = streamInPtr.Position;
                        symbol.IsFollowedByLayoutChar = (ch == '%' || Char.IsWhiteSpace(ch)); // '/*' not covered

                        if (symbol.Class == SymbolClass.Comment) break;

                        if (cdh.IsExpectingId || symbol.Class == SymbolClass.Meta)
                            cdh.HandleSymbol(symbol); // if expecting: symbol must be an identifier
                    } while (cdh.CodeIsInactive || symbol.Class == SymbolClass.Meta);

                    #endregion

                    if (canUnquote)
                    {
                        symbol.Start++;
                        symbol.Final--;
                    }

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
                            case Atom:
                                Break = true;
                                break;
                            case EndOfInput:
                                Break = true;
                                break;
                            case VerbatimStringStart:
                                ScanVerbatimString();
                                Break = true;
                                break;
                            case CommentStart:
                                if (stringMode)
                                    Break = true;

                                if (!DoComment("*/", true, streamInPtr.FOnLine))
                                    ErrorMessage = "Unterminated comment starting at line " + symbol.LineNo.ToString();

                                break;
                            case CommentSingle:
                                if (stringMode) Break = true;
                                else Break = false;
                                DoComment("\n", false, streamInPtr.FOnLine);
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

            #region ParseTerm

            public BaseTerm ParseTerm()
            {
                if (symbol.TerminalId == EndOfInput) return null;

                BaseTerm result;

                try
                {
                    OptionalPrologTerm(new TerminalSet(terminalCount), out result);
                    lineCount = LineNo;

                    return result;
                }
                catch (UnhandledParserException)
                {
                    throw;
                }
                catch (ParserException e)
                {
                    throw new Exception(e.Message);
                }
                catch (SyntaxException e)
                {
                    throw new Exception(e.Message);
                }
                catch (Exception e) // other errors
                {
                    errorMessage = String.Format("*** Line {0}: {1}{2}", LineNo, e.Message,
                        showErrTrace ? Environment.NewLine + e.StackTrace : null);

                    throw new Exception(errorMessage);
                }
            }

            #endregion ParseTerm

            #region Variables and initialization

            const char USCORE = '_';
            char extraUnquotedAtomChar = '_';

            PrologEngine engine;
            PredicateTable ps;
            OperatorTable opTable;
            TermNode queryNode = null;

            public TermNode QueryNode
            {
                get { return queryNode; }
            }

            public int nargs;

            public const string IMPLIES = ":-";
            public const string DCGIMPL = "-->";
            public const string ARROW = "->";
            public const string DOT = ".";
            public const string CUT = "!";
            public const string OP = "op";
            public const string WRAP = "wrap";
            public const string STRINGSTYLE = "stringstyle";
            public const string CURL = "{}";
            public const string EQ = "=";
            public const string COLON = ":";
            public const string COMMA = ",";
            public const string QCOMMA = "','";
            public const string SEMI = ";";
            public const string SLASH = "/";
            public const string LISTPATOPEN = "[!";
            public const string LISTPATCLOSE = "!]";
            public const string TREEPATOPEN = "{!";
            public const string TREEPATCLOSE = "!}";
            public const string ELLIPSIS = "..";
            public const string SUBTREE = "\\";
            public const string NEGATE = "~";
            public const string PLUSSYM = "+";
            public const string TIMESSYM = "*";
            public const string QUESTIONMARK = "?";

            int savePlusSym;
            int saveTimesSym;
            int saveQuestionMark;

            public static readonly int Infinite = int.MaxValue;

            BaseTerm readTerm; // result of read (X)

            public BaseTerm ReadTerm
            {
                get { return readTerm; }
            }

            bool inQueryMode;
            bool jsonMode; // determines how curly brackets will be interpreted ('normal' vs. list)
            bool readingDcgClause; // determines how to interpret {t1, ... , tn}

            public bool InQueryMode
            {
                get { return inQueryMode; }
            }

            void Initialize()
            {
                SetReservedOperators(false);
                terminalTable[PLUSSYM] = Operator;
                terminalTable[TIMESSYM] = Operator;
                terminalTable[QUESTIONMARK] = Atom;
                terminalTable.Remove("module"); // will be temporarily set after an initial ':-' only
                terminalTable.Remove("dynamic"); // ...
                terminalTable.Remove("discontiguous");
                terminalTable.Remove("alldiscontiguous");
                //terminalTable.Remove ("stringstyle");
                jsonMode = false; // Standard Prolog interpretation

                if (!ConfigSettings.VerbatimStringsAllowed)
                    terminalTable.Remove(@"@""");
            }


            void Terminate()
            {
                inQueryMode = true;
            }

            #endregion Variables and initialization

            #region Operator handling

            public OperatorDescr AddPrologOperator(int prec, string type, string name, bool user)
            {
                AssocType assoc = AssocType.None;

                try
                {
                    assoc = (AssocType) Enum.Parse(typeof(AssocType), type);
                }
                catch
                {
                    IO.Error("Illegal operator type '{0}'", type);
                }

                if (prec < 0 || prec > 1200)
                    IO.Error("Illegal precedence value {0} for operator '{1}'", prec, name);

                TerminalDescr td;
                OpDescrTriplet triplet;

                if (terminalTable.Find(name, out td))

                {
                    // some operator symbols (:-, -->, +, ...) already exist prior to their op/3 definition
                    if (td.Payload == null)
                    {
                        td.Payload = opTable.Add(prec, type, name, user);
                        td.IVal = Operator;
                    }
                    else // no need to add it -- just change its properties
                    {
                        ((OpDescrTriplet) td.Payload).Assign(name, prec, assoc, user);
                        td.IVal = Operator;
                    }

                    triplet = (OpDescrTriplet) td.Payload;
                }
                else // new operator
                {
                    triplet = opTable.Add(prec, type, name, user);
                    terminalTable.Add(name, Operator, triplet);
                }

                return triplet[assoc];
            }


            public void RemovePrologOperator(string type, string name, bool user)
            {
                AssocType assoc = AssocType.None;

                try
                {
                    assoc = (AssocType) Enum.Parse(typeof(AssocType), type);
                }
                catch
                {
                    IO.Error("Illegal operator type '{0}'", type);
                }

                TerminalDescr td;

                if (terminalTable.Find(name, out td) && td.Payload != null)
                {
                    if (!user)
                        IO.Error("Undefine of operator ({0}, {1}) not allowed", type, name);
                    else
                    {
                        ((OpDescrTriplet) td.Payload).Unassign(name, assoc);

                        foreach (OperatorDescr od in ((OpDescrTriplet) td.Payload))
                            if (od.IsDefined)
                                return;

                        terminalTable.Remove(name); // name no longer used
                    }
                }
                else // unknown operator
                    IO.Error("Operator not found: ({0}, {1})", type, name);
            }


            public bool IsOperator(string key)
            {
                return (terminalTable[key] == Operator);
            }


            public OpDescrTriplet GetOperatorDescr(string key)
            {
                TerminalDescr td;

                if (terminalTable.Find(key, out td)) return (OpDescrTriplet) td.Payload;

                return null;
            }


            void AddReservedOperators()
            {
                AddPrologOperator(1200, "xfx", IMPLIES, false);
                AddPrologOperator(1200, "fx", IMPLIES, false);
                AddPrologOperator(1200, "xfx", DCGIMPL, false);
                AddPrologOperator(1150, "xfy", ARROW, false);
                CommaOpDescr = AddPrologOperator(1050, "xfy", COMMA, false);
                opTable.Find(COMMA, out CommaOpTriplet);
                SemiOpDescr = AddPrologOperator(1100, "xfy", SEMI, false);
            }


            bool isReservedOperatorSetting;

            void SetReservedOperators(bool asOpr)
            {
                if (asOpr) // parsed as Operator
                {
                    terminalTable[IMPLIES] = Operator;
                    terminalTable[DCGIMPL] = Operator;
                }
                else // parsed 'normally'
                {
                    terminalTable[IMPLIES] = ImpliesSym;
                    terminalTable[DCGIMPL] = DCGArrowSym;
                    terminalTable[OP] = OpSym;
                }

                isReservedOperatorSetting = asOpr;
            }

            bool SetCommaAsSeparator(bool mode)
            {
                bool result = (terminalTable[COMMA] == Comma); // returnvalue is *current* status

                terminalTable[COMMA] = mode ? Comma : Operator; // 'Comma' is separator

                return result;
            }

            #endregion Operator handling

            #region Wrapper handling

            public void AddBracketPair(string openBracket, string closeBracket, bool useAsList)
            {
                if (openBracket == closeBracket)
                    IO.Error("Wrapper open and wrapper close token must be different");

                if (useAsList)
                {
                    engine.AltListTable.Add(ref openBracket, ref closeBracket); // possible quotes are removed
                    terminalTable.AddOrReplace(AltListOpen, "AltListOpen", openBracket);
                    terminalTable.AddOrReplace(AltListClose, "AltListClose", closeBracket);
                }
                else
                {
                    engine.WrapTable.Add(ref openBracket, ref closeBracket);
                    terminalTable.AddOrReplace(WrapOpen, "WrapOpen", openBracket);
                    terminalTable.AddOrReplace(WrapClose, "WrapClose", closeBracket);
                }
            }


            // specific method for interpreting '{' and '}' as list delimiters
            public void SetJsonMode(bool mode)
            {
                if (jsonMode = mode)
                    AddBracketPair("{", "}", true);
                else
                {
                    terminalTable.AddOrReplace(LCuBracket, "LCuBracket", "{");
                    terminalTable.AddOrReplace(RCuBracket, "RCuBracket", "}");
                    engine.AltListTable.Remove("{");
                }
            }

            public bool GetJsonMode()
            {
                return jsonMode;
            }

            #endregion Wrapper handling

            #region ScanIdOrTerminal

            protected override void ScanIdOrTerminalOrCommentStart()
            {
                TerminalDescr tRec;
                bool special = ch.IsSpecialAtomChar();
                bool firstLow = Char.IsLower(ch);
                StreamPointer iPtr = streamInPtr;
                StreamPointer tPtr = iPtr;
                int aLen = (ch.IsIdStartChar() || special) ? 1 : 0; // length of longest Atom sofar
                int tLen = 0; // length of longest Terminal sofar
                int fCnt = 0; // count of calls to FindCharAndSubtree
                bool isDot = (ch == '.'); // remains valid only if symbol length is 1
                terminalTable.FindCharInSubtreeReset();

                while (fCnt++ >= 0 && terminalTable.FindCharInSubtree(ch, out tRec))
                {
                    if (tRec != null)
                    {
                        symbol.TerminalId = tRec.IVal;
                        symbol.Payload = tRec.Payload;
                        symbol.Class = tRec.Class;
                        tLen = fCnt;
                        tPtr = streamInPtr; // next char to be processed

                        if (symbol.TerminalId == CommentStart || symbol.TerminalId == CommentSingle) return;

                        if (terminalTable.AtLeaf) break; // terminal cannot be extended
                    }

                    NextCh();

                    if (aLen == fCnt &&
                        (special && ch.IsSpecialAtomChar() ||
                         !special && ch.IsIdAtomContinueChar(extraUnquotedAtomChar)
                        )
                    )
                    {
                        aLen++;
                        iPtr = streamInPtr;
                    }
                } // fCnt++ by last (i.e. failing) Call

                // At this point: (in the Prolog case, read 'Identifier and Atom made up
                // from specials' for 'Identifier'):
                // - tLen has length of BaseTrie terminal (if any, 0 otherwise);
                // - aLen has length of Identifier (if any, 0 otherwise);
                // - fCnt is the number of characters inspected (for Id AND terminal)
                // - The character pointer is on the last character inspected (for both)
                // Iff aLen = fCnt then the entire sequence read sofar is an Identifier.
                // Now try extending the Identifier, only meaningful if iLen = fCnt.
                // Do not do this for an Atom made up from specials if a Terminal was recognized !!

                if (aLen == fCnt)
                {
                    while (true)
                    {
                        NextCh();

                        if (special ? ch.IsSpecialAtomChar() : ch.IsIdAtomContinueChar(extraUnquotedAtomChar))
                        {
                            aLen++;
                            iPtr = streamInPtr;
                        }
                        else
                            break;
                    }
                }

                if (aLen > tLen) // tLen = 0 iff Terminal == Undefined
                {
                    if (firstLow || special)
                        symbol.TerminalId = Atom;
                    else
                        symbol.TerminalId = Identifier;

                    symbol.Class = SymbolClass.Id;
                    InitCh(iPtr);
                }
                else if (symbol.TerminalId == Undefined)
                    InitCh(iPtr);
                else // we have a terminal != Identifier
                {
                    if (aLen == tLen) symbol.Class = SymbolClass.Id;
                    InitCh(tPtr);
                }

                NextCh();

                // a bit hacky: find erroneous conditional define symbols 
                // such as e.g. !ifxxx (space omitted between !if and xxx)

                if (symbol.Class == SymbolClass.Meta)
                {
                    int pos = streamInPtr.Position;

                    while (ch.IsIdAtomContinueChar(extraUnquotedAtomChar)) NextCh();

                    if (pos != streamInPtr.Position) symbol.TerminalId = Undefined;
                }

                // Dot-patch: a '.' is a Dot only if it is followed by layout,
                // otherwise it is an atom (or an operator if defined as such)
                if (isDot && aLen == 1 && (ch == '\0' || ch == '%' || Char.IsWhiteSpace(ch)))
                    symbol.TerminalId = Dot;
            }

            public void SetDollarAsPossibleUnquotedAtomChar(bool set)
            {
                extraUnquotedAtomChar = set ? '$' : '_';
            }

            #endregion
        }

        #endregion PrologParser
    }

    #region Extensions class

    static class PrologParserExtensions
    {
        public static bool IsSpecialAtomChar(this char c)
        {
            return (@"+-*/\^<=>`~:.?@#$&".IndexOf(c) != -1);
        }

        public static bool IsIdAtomContinueChar(this char c, char extraUnquotedAtomChar)
        {
            return (Char.IsLetterOrDigit(c) || c == '_' || c == extraUnquotedAtomChar);
        }
    }

    #endregion Extensions class
}