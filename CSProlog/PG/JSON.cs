//#define LL1_tracing

using System;
using System.Collections.Generic;

namespace Prolog
{
/* _______________________________________________________________________________________________
  |                                                                                               |
  |  C#Prolog -- Copyright (C) 2007 John Pool -- j.pool@ision.nl                                  |
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

// Parser Generator version 4.0 -- Date/Time: 10-Feb-14 08:40:11


    public partial class PrologEngine
    {
        #region JsonParser

        public partial class JsonParser : BaseParser<object>
        {
            public static readonly string VersionTimeStamp = "2014-02-10 08:40:11";

            #region Constructor

            public JsonParser()
            {
                terminalTable = new BaseTrie(terminalCount: terminalCount, false);
                FillTerminalTable(terminalTable: terminalTable);
                symbol = new Symbol(this);
                streamInPrefix = "";
                streamInPreLen = 0;
            }

            #endregion constructor

            #region NextSymbol, GetSymbol

            protected override bool GetSymbol(TerminalSet followers, bool done, bool genXCPN)
            {
                string s;

                if (symbol.IsProcessed) NextSymbol();

                symbol.SetProcessed(status: done);
                if (parseAnyText || followers.IsEmpty()) return true;

                if (syntaxErrorStat) return false;

                if (symbol.TerminalId == ANYSYM || followers.Contains(terminal: symbol.TerminalId)) return true;

                switch (symbol.TerminalId)
                {
                    case EndOfLine:
                        if (seeEndOfLine) s = "<EndOfLine>";
                        else goto default;
                        s = "<EndOfLine>";
                        break;
                    case EndOfInput:
                        s = "<EndOfInput>";
                        break;
                    default:
                        s = string.Format("\"{0}\"", arg0: symbol);
                        break;
                }

                s = string.Format("*** Unexpected symbol: {0}{1}*** Expected one of: {2}", arg0: s,
                    arg1: Environment.NewLine, terminalTable.TerminalImageSet(ts: followers));
                if (genXCPN)
                    SyntaxError = s;
                else
                    errorMessage = s;

                return true;
            }

            #endregion NextSymbol, GetSymbol


            #region Terminal definition

            /* The following constants are defined in BaseParser.cs:
            const int Undefined = 0;
            const int Comma = 1;
            const int LeftParen = 2;
            const int RightParen = 3;
            const int Identifier = 4;
            const int IntLiteral = 5;
            const int ppDefine = 6;
            const int ppUndefine = 7;
            const int ppIf = 8;
            const int ppIfNot = 9;
            const int ppElse = 10;
            const int ppElseIf = 11;
            const int ppEndIf = 12;
            const int RealLiteral = 13;
            const int ImagLiteral = 14;
            const int StringLiteral = 15;
            const int CharLiteral = 16;
            const int CommentStart = 17;
            const int CommentSingle = 18;
            const int EndOfLine = 19;
            const int ANYSYM = 20;
            const int EndOfInput = 21;
            */
            private const int LSqBracket = 22;
            private const int RSqBracket = 23;
            private const int LCuBracket = 24;
            private const int RCuBracket = 25;
            private const int Colon = 26;
            private const int TrueSym = 27;
            private const int FalseSym = 28;

            private const int NullSym = 29;

            // Total number of terminals:
            public const int terminalCount = 30;

            public static void FillTerminalTable(BaseTrie terminalTable)
            {
                terminalTable.Add(iVal: Undefined, @class: SymbolClass.None, "Undefined");
                terminalTable.Add(iVal: Comma, @class: SymbolClass.None, "Comma", ",");
                terminalTable.Add(iVal: LeftParen, @class: SymbolClass.Group, "LeftParen", "(");
                terminalTable.Add(iVal: RightParen, @class: SymbolClass.Group, "RightParen", ")");
                terminalTable.Add(iVal: Identifier, @class: SymbolClass.Id, "Identifier");
                terminalTable.Add(iVal: IntLiteral, @class: SymbolClass.Number, "IntLiteral");
                terminalTable.Add(iVal: ppDefine, @class: SymbolClass.Meta, "ppDefine", "#define");
                terminalTable.Add(iVal: ppUndefine, @class: SymbolClass.Meta, "ppUndefine", "#undefine");
                terminalTable.Add(iVal: ppIf, @class: SymbolClass.Meta, "ppIf", "#if");
                terminalTable.Add(iVal: ppIfNot, @class: SymbolClass.Meta, "ppIfNot", "#ifnot");
                terminalTable.Add(iVal: ppElse, @class: SymbolClass.Meta, "ppElse", "#else");
                terminalTable.Add(iVal: ppElseIf, @class: SymbolClass.Meta, "ppElseIf", "#elseif");
                terminalTable.Add(iVal: ppEndIf, @class: SymbolClass.Meta, "ppEndIf", "#endif");
                terminalTable.Add(iVal: RealLiteral, @class: SymbolClass.Number, "RealLiteral");
                terminalTable.Add(iVal: ImagLiteral, @class: SymbolClass.Number, "ImagLiteral");
                terminalTable.Add(iVal: StringLiteral, @class: SymbolClass.Text, "StringLiteral");
                terminalTable.Add(iVal: CharLiteral, @class: SymbolClass.Text, "CharLiteral");
                terminalTable.Add(iVal: CommentStart, @class: SymbolClass.Comment, "CommentStart", "/*");
                terminalTable.Add(iVal: CommentSingle, @class: SymbolClass.Comment, "CommentSingle", "%");
                terminalTable.Add(iVal: EndOfLine, @class: SymbolClass.None, "EndOfLine");
                terminalTable.Add(iVal: ANYSYM, @class: SymbolClass.None, "ANYSYM");
                terminalTable.Add(iVal: EndOfInput, @class: SymbolClass.None, "EndOfInput");
                terminalTable.Add(iVal: LSqBracket, @class: SymbolClass.None, "LSqBracket", "[");
                terminalTable.Add(iVal: RSqBracket, @class: SymbolClass.None, "RSqBracket", "]");
                terminalTable.Add(iVal: LCuBracket, @class: SymbolClass.None, "LCuBracket", "{");
                terminalTable.Add(iVal: RCuBracket, @class: SymbolClass.None, "RCuBracket", "}");
                terminalTable.Add(iVal: Colon, @class: SymbolClass.None, "Colon", ":");
                terminalTable.Add(iVal: TrueSym, @class: SymbolClass.None, "TrueSym", "true");
                terminalTable.Add(iVal: FalseSym, @class: SymbolClass.None, "FalseSym", "false");
                terminalTable.Add(iVal: NullSym, @class: SymbolClass.None, "NullSym", "null");
            }

            #endregion Terminal definition

            #region PARSER PROCEDURES

            public override void RootCall()
            {
                JsonStruct(new TerminalSet(terminalCount: terminalCount, EndOfInput));
            }


            public override void Delegates()
            {
            }


            #region JsonStruct

            private void JsonStruct(TerminalSet _TS)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LSqBracket, LCuBracket), false, true);
                if (symbol.TerminalId == LCuBracket)
                    JsonObject(_TS: _TS, t: out jsonListTerm);
                else
                    JsonArray(_TS: _TS, t: out jsonListTerm);
            }

            #endregion

            #region JsonObject

            private void JsonObject(TerminalSet _TS, out BaseTerm t)
            {
                BaseTerm e;
                var listItems = new List<BaseTerm>();
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LCuBracket), true, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, StringLiteral, RCuBracket), false, true);
                if (symbol.TerminalId == StringLiteral)
                    while (true)
                    {
                        JsonPair(new TerminalSet(terminalCount: terminalCount, Comma, RCuBracket), t: out e);
                        listItems.Add(item: e);
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RCuBracket), false, true);
                        if (symbol.TerminalId == Comma)
                            symbol.SetProcessed();
                        else
                            break;
                    }

                GetSymbol(new TerminalSet(terminalCount: terminalCount, RCuBracket), true, true);
                t = JsonTerm.FromArray(listItems.ToArray());
            }

            #endregion

            #region JsonPair

            private void JsonPair(TerminalSet _TS, out BaseTerm t)
            {
                BaseTerm t0, t1;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, StringLiteral), true, true);
                t0 = new StringTerm(symbol.ToString().Dequoted());
                GetSymbol(new TerminalSet(terminalCount: terminalCount, Colon), true, true);
                JsonValue(_TS: _TS, t: out t1);
                t = new OperatorTerm(opTable: opTable, name: PrologParser.COLON, a0: t0, a1: t1);
            }

            #endregion

            #region JsonArray

            private void JsonArray(TerminalSet _TS, out BaseTerm t)
            {
                BaseTerm e;
                var listItems = new List<BaseTerm>();
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LSqBracket), true, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, RealLiteral, StringLiteral,
                    LSqBracket, RSqBracket, LCuBracket,
                    TrueSym, FalseSym, NullSym), false, true);
                if (symbol.IsMemberOf(IntLiteral, RealLiteral, StringLiteral, LSqBracket, LCuBracket, TrueSym, FalseSym,
                    NullSym))
                    while (true)
                    {
                        JsonValue(new TerminalSet(terminalCount: terminalCount, Comma, RSqBracket), t: out e);
                        listItems.Add(item: e);
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RSqBracket), false, true);
                        if (symbol.TerminalId == Comma)
                            symbol.SetProcessed();
                        else
                            break;
                    }

                GetSymbol(new TerminalSet(terminalCount: terminalCount, RSqBracket), true, true);
                t = new CompoundTerm("array",
                    ListTerm.ListFromArray(listItems.ToArray(), afterBar: BaseTerm.EMPTYLIST));
            }

            #endregion

            #region JsonValue

            private void JsonValue(TerminalSet _TS, out BaseTerm t)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, RealLiteral, StringLiteral,
                    LSqBracket, LCuBracket, TrueSym,
                    FalseSym, NullSym), false, true);
                if (symbol.TerminalId == LCuBracket)
                    JsonObject(_TS: _TS, t: out t);
                else if (symbol.TerminalId == LSqBracket)
                    JsonArray(_TS: _TS, t: out t);
                else
                    JsonLiteral(_TS: _TS, t: out t);
            }

            #endregion

            #region JsonLiteral

            private void JsonLiteral(TerminalSet _TS, out BaseTerm t)
            {
                GetSymbol(
                    new TerminalSet(terminalCount: terminalCount, IntLiteral, RealLiteral, StringLiteral, TrueSym,
                        FalseSym, NullSym), false,
                    true);
                if (symbol.IsMemberOf(IntLiteral, RealLiteral))
                {
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, RealLiteral), false, true);
                    if (symbol.TerminalId == IntLiteral)
                        symbol.SetProcessed();
                    else
                        symbol.SetProcessed();
                    t = new DecimalTerm(symbol.ToDecimal());
                }
                else if (symbol.TerminalId == StringLiteral)
                {
                    symbol.SetProcessed();
                    t = new StringTerm(symbol.ToString().Dequoted());
                }
                else if (symbol.TerminalId == TrueSym)
                {
                    symbol.SetProcessed();
                    t = new AtomTerm("true");
                }
                else if (symbol.TerminalId == FalseSym)
                {
                    symbol.SetProcessed();
                    t = new AtomTerm("false");
                }
                else
                {
                    symbol.SetProcessed();
                    t = new AtomTerm("null");
                }
            }

            #endregion

            #endregion PARSER PROCEDURES
        }

        #endregion JsonParser
    }
}