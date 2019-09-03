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

// Parser Generator version 4.0 -- Date/Time: 03-Apr-14 21:12:01


    public partial class PrologEngine
    {
        #region PrologParser

        public partial class PrologParser : BaseParser<OpDescrTriplet>
        {
            public static readonly string VersionTimeStamp = "2014-04-03 21:12:01";
            protected override char ppChar => '!';

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
            private const int Operator = 22;
            private const int Atom = 23;
            private const int VerbatimStringStart = 24;
            private const int Dot = 25;
            private const int Anonymous = 26;
            private const int CutSym = 27;
            private const int ImpliesSym = 28;
            private const int PromptSym = 29;
            private const int DCGArrowSym = 30;
            private const int BuiltinCSharp = 31;
            private const int LSqBracket = 32;
            private const int RSqBracket = 33;
            private const int LCuBracket = 34;
            private const int RCuBracket = 35;
            private const int VBar = 36;
            private const int OpSym = 37;
            private const int WrapSym = 38;
            private const int BuiltinSym = 39;
            private const int ProgramSym = 40;
            private const int ReadingSym = 41;
            private const int EnsureLoaded = 42;
            private const int Discontiguous = 43;
            private const int StringStyle = 44;
            private const int AllDiscontiguous = 45;
            private const int Module = 46;
            private const int Dynamic = 47;
            private const int ListPatternOpen = 48;
            private const int ListPatternClose = 49;
            private const int EllipsisSym = 50;
            private const int SubtreeSym = 51;
            private const int NegateSym = 52;
            private const int PlusSym = 53;
            private const int TimesSym = 54;
            private const int QuestionMark = 55;
            private const int QuestionMarks = 56;
            private const int TrySym = 57;
            private const int CatchSym = 58;
            private const int WrapOpen = 59;
            private const int WrapClose = 60;
            private const int AltListOpen = 61;
            private const int AltListClose = 62;
            private const int Slash = 63;

            private const int VerbatimStringLiteral = 64;

            // Total number of terminals:
            public const int terminalCount = 65;

            public static void FillTerminalTable(BaseTrie terminalTable)
            {
                terminalTable.Add(iVal: Undefined, @class: SymbolClass.None, "Undefined");
                terminalTable.Add(iVal: Comma, @class: SymbolClass.None, "Comma", ",");
                terminalTable.Add(iVal: LeftParen, @class: SymbolClass.Group, "LeftParen", "(");
                terminalTable.Add(iVal: RightParen, @class: SymbolClass.Group, "RightParen", ")");
                terminalTable.Add(iVal: Identifier, @class: SymbolClass.Id, "Identifier");
                terminalTable.Add(iVal: IntLiteral, @class: SymbolClass.Number, "IntLiteral");
                terminalTable.Add(iVal: ppDefine, @class: SymbolClass.Meta, "ppDefine", "!define");
                terminalTable.Add(iVal: ppUndefine, @class: SymbolClass.Meta, "ppUndefine", "!undefine");
                terminalTable.Add(iVal: ppIf, @class: SymbolClass.Meta, "ppIf", "!if");
                terminalTable.Add(iVal: ppIfNot, @class: SymbolClass.Meta, "ppIfNot", "!ifnot");
                terminalTable.Add(iVal: ppElse, @class: SymbolClass.Meta, "ppElse", "!else");
                terminalTable.Add(iVal: ppElseIf, @class: SymbolClass.Meta, "ppElseIf", "!elseif");
                terminalTable.Add(iVal: ppEndIf, @class: SymbolClass.Meta, "ppEndIf", "!endif");
                terminalTable.Add(iVal: RealLiteral, @class: SymbolClass.Number, "RealLiteral");
                terminalTable.Add(iVal: ImagLiteral, @class: SymbolClass.Number, "ImagLiteral");
                terminalTable.Add(iVal: StringLiteral, @class: SymbolClass.Text, "StringLiteral");
                terminalTable.Add(iVal: CharLiteral, @class: SymbolClass.Text, "CharLiteral");
                terminalTable.Add(iVal: CommentStart, @class: SymbolClass.Comment, "CommentStart", "/*");
                terminalTable.Add(iVal: CommentSingle, @class: SymbolClass.Comment, "CommentSingle", "%");
                terminalTable.Add(iVal: EndOfLine, @class: SymbolClass.None, "EndOfLine");
                terminalTable.Add(iVal: ANYSYM, @class: SymbolClass.None, "ANYSYM");
                terminalTable.Add(iVal: EndOfInput, @class: SymbolClass.None, "EndOfInput");
                terminalTable.Add(iVal: Operator, @class: SymbolClass.None, "Operator");
                terminalTable.Add(iVal: Atom, @class: SymbolClass.None, "Atom");
                terminalTable.Add(iVal: VerbatimStringStart, @class: SymbolClass.None, "VerbatimStringStart", @"@""");
                terminalTable.Add(iVal: Dot, @class: SymbolClass.None, "Dot");
                terminalTable.Add(iVal: Anonymous, @class: SymbolClass.None, "Anonymous", "_");
                terminalTable.Add(iVal: CutSym, @class: SymbolClass.None, "CutSym", "!");
                terminalTable.Add(iVal: ImpliesSym, @class: SymbolClass.None, "ImpliesSym", ":-");
                terminalTable.Add(iVal: PromptSym, @class: SymbolClass.None, "PromptSym", "?-");
                terminalTable.Add(iVal: DCGArrowSym, @class: SymbolClass.None, "DCGArrowSym", "-->");
                terminalTable.Add(iVal: BuiltinCSharp, @class: SymbolClass.None, "BuiltinCSharp", ":==");
                terminalTable.Add(iVal: LSqBracket, @class: SymbolClass.Group, "LSqBracket", "[");
                terminalTable.Add(iVal: RSqBracket, @class: SymbolClass.Group, "RSqBracket", "]");
                terminalTable.Add(iVal: LCuBracket, @class: SymbolClass.Group, "LCuBracket", "{");
                terminalTable.Add(iVal: RCuBracket, @class: SymbolClass.Group, "RCuBracket", "}");
                terminalTable.Add(iVal: VBar, @class: SymbolClass.None, "VBar", "|");
                terminalTable.Add(iVal: OpSym, @class: SymbolClass.None, "OpSym", "op");
                terminalTable.Add(iVal: WrapSym, @class: SymbolClass.None, "WrapSym", "wrap");
                terminalTable.Add(iVal: BuiltinSym, @class: SymbolClass.None, "BuiltinSym", "&builtin");
                terminalTable.Add(iVal: ProgramSym, @class: SymbolClass.None, "ProgramSym", "&program");
                terminalTable.Add(iVal: ReadingSym, @class: SymbolClass.None, "ReadingSym", "&reading");
                terminalTable.Add(iVal: EnsureLoaded, @class: SymbolClass.None, "EnsureLoaded", "ensure_loaded");
                terminalTable.Add(iVal: Discontiguous, @class: SymbolClass.None, "Discontiguous", "discontiguous");
                terminalTable.Add(iVal: StringStyle, @class: SymbolClass.None, "StringStyle", "stringstyle");
                terminalTable.Add(iVal: AllDiscontiguous, @class: SymbolClass.None, "AllDiscontiguous",
                    "alldiscontiguous");
                terminalTable.Add(iVal: Module, @class: SymbolClass.None, "Module", "module");
                terminalTable.Add(iVal: Dynamic, @class: SymbolClass.None, "Dynamic", "dynamic");
                terminalTable.Add(iVal: ListPatternOpen, @class: SymbolClass.Group, "ListPatternOpen", "[!");
                terminalTable.Add(iVal: ListPatternClose, @class: SymbolClass.Group, "ListPatternClose", "!]");
                terminalTable.Add(iVal: EllipsisSym, @class: SymbolClass.None, "EllipsisSym", "..");
                terminalTable.Add(iVal: SubtreeSym, @class: SymbolClass.None, "SubtreeSym", @"\");
                terminalTable.Add(iVal: NegateSym, @class: SymbolClass.None, "NegateSym", "~");
                terminalTable.Add(iVal: PlusSym, @class: SymbolClass.None, "PlusSym", "+");
                terminalTable.Add(iVal: TimesSym, @class: SymbolClass.None, "TimesSym", "*");
                terminalTable.Add(iVal: QuestionMark, @class: SymbolClass.None, "QuestionMark", "?");
                terminalTable.Add(iVal: QuestionMarks, @class: SymbolClass.None, "QuestionMarks", "??");
                terminalTable.Add(iVal: TrySym, @class: SymbolClass.None, "TrySym", "TRY");
                terminalTable.Add(iVal: CatchSym, @class: SymbolClass.None, "CatchSym", "CATCH");
                terminalTable.Add(iVal: WrapOpen, @class: SymbolClass.None, "WrapOpen");
                terminalTable.Add(iVal: WrapClose, @class: SymbolClass.None, "WrapClose");
                terminalTable.Add(iVal: AltListOpen, @class: SymbolClass.None, "AltListOpen");
                terminalTable.Add(iVal: AltListClose, @class: SymbolClass.None, "AltListClose");
                terminalTable.Add(iVal: Slash, @class: SymbolClass.None, "Slash");
                terminalTable.Add(iVal: VerbatimStringLiteral, @class: SymbolClass.None, "VerbatimStringLiteral");
            }

            #endregion Terminal definition

            #region Constructor

            public PrologParser(PrologEngine engine)
            {
                this.engine = engine;
                ps = engine.Ps;
                terminalTable = engine.terminalTable;
                opTable = engine.OpTable;
                symbol = new Symbol(this);
                streamInPrefix = "";
                streamInPreLen = 0;
                AddReservedOperators();
            }

            public PrologParser()
            {
                terminalTable = new BaseTrie(terminalCount: terminalCount, false);
                FillTerminalTable(terminalTable: terminalTable);
                symbol = new Symbol(this);
                streamInPrefix = "";
                streamInPreLen = 0;
                AddReservedOperators();
            }

            #endregion constructor

            #region PARSER PROCEDURES

            public override void RootCall()
            {
                PrologCode(new TerminalSet(terminalCount: terminalCount, EndOfInput));
            }


            public override void Delegates()
            {
            }


            #region PrologCode

            private void PrologCode(TerminalSet _TS)
            {
                SetCommaAsSeparator(false); // Comma-role only if comma is separating arguments
                terminalTable[key: OP] = OpSym;
                terminalTable[key: WRAP] = WrapSym;
                inQueryMode = false;
                try
                {
                    SeeEndOfLine = false;
                    terminalTable[key: ELLIPSIS] = Operator;
                    terminalTable[key: SUBTREE] = Operator;
                    terminalTable[key: STRINGSTYLE] = Atom;
                    if (terminalTable[key: NEGATE] == NegateSym) terminalTable[key: NEGATE] = Atom;
                    GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                        ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, OpSym, WrapSym,
                        BuiltinSym, ProgramSym, ReadingSym, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                        AltListOpen, AltListClose, VerbatimStringLiteral), false, true);
                    if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                        Operator, Atom,
                        Anonymous, CutSym, LSqBracket, LCuBracket, OpSym, WrapSym, BuiltinSym, ProgramSym, ReadingSym,
                        ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral))
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                            RealLiteral, ImagLiteral, StringLiteral,
                            Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, OpSym, WrapSym,
                            BuiltinSym, ProgramSym, ReadingSym, ListPatternOpen, TrySym, WrapOpen,
                            WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral), false, true);
                        if (symbol.TerminalId == BuiltinSym)
                        {
                            symbol.SetProcessed();
                            Initialize();
                            Predefineds(_TS: _TS);
                        }
                        else if (symbol.TerminalId == ProgramSym)
                        {
                            symbol.SetProcessed();
                            Initialize();
                            Program(_TS: _TS);
                        }
                        else if (symbol.TerminalId == ReadingSym)
                        {
                            symbol.SetProcessed();
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out readTerm);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot), true, true);
                        }
                        else
                        {
                            engine.EraseVariables();
                            inQueryMode = true;
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket,
                                OpSym, WrapSym, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                                AltListOpen, AltListClose, VerbatimStringLiteral), false, true);
                            if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom,
                                Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                                AltListOpen, AltListClose, VerbatimStringLiteral))
                            {
                                terminalTable[key: OP] = Atom;
                                terminalTable[key: WRAP] = Atom;
                                SetReservedOperators(true);
                                Query(new TerminalSet(terminalCount: terminalCount, Dot), body: out queryNode);
                            }
                            else if (symbol.TerminalId == OpSym)
                            {
                                OpDefinition(new TerminalSet(terminalCount: terminalCount, Dot), true);
                                queryNode = null;
                            }
                            else
                            {
                                WrapDefinition(new TerminalSet(terminalCount: terminalCount, Dot));
                                queryNode = null;
                            }

                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot), true, true);
                        }
                    }
                }
                finally
                {
                    Terminate();
                }
            }

            #endregion

            #region Program

            private void Program(TerminalSet _TS)
            {
                var firstReport = true;
                while (true)
                {
                    GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                        ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, ImpliesSym, PromptSym, LSqBracket, LCuBracket,
                        ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral), false, true);
                    if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                        Operator, Atom,
                        Anonymous, CutSym, ImpliesSym, PromptSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym,
                        WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral))
                        ClauseNode(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                            RealLiteral, ImagLiteral, StringLiteral,
                            Operator, Atom, Anonymous, CutSym, ImpliesSym, PromptSym, LSqBracket,
                            LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen,
                            AltListClose, VerbatimStringLiteral), firstReport: ref firstReport);
                    else
                        break;
                }
            }

            #endregion

            #region ClauseNode

            private void ClauseNode(TerminalSet _TS, ref bool firstReport)
            {
                BaseTerm head;
                TermNode body = null;
                ClauseNode c;
                engine.EraseVariables();
                var lineNo = symbol.LineNo;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    Operator, Atom, Anonymous, CutSym, ImpliesSym, PromptSym, LSqBracket,
                    LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen,
                    AltListClose, VerbatimStringLiteral), false, true);
                if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                    Operator, Atom,
                    Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                    AltListOpen, AltListClose, VerbatimStringLiteral))
                {
                    PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot, ImpliesSym, DCGArrowSym),
                        t: out head);
                    if (!head.IsCallable)
                        IO.Error("Illegal predicate head: {0}", head.ToString());
                    if (engine.Ps.Predefineds.ContainsKey(head.Key))
                        IO.Error("Predefined predicate or operator '{0}' cannot be redefined.", head.FunctorToString);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot, ImpliesSym, DCGArrowSym), false, true);
                    if (symbol.IsMemberOf(ImpliesSym, DCGArrowSym))
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, ImpliesSym, DCGArrowSym), false, true);
                        if (symbol.TerminalId == ImpliesSym)
                        {
                            symbol.SetProcessed();
                            Query(new TerminalSet(terminalCount: terminalCount, Dot), body: out body);
                        }
                        else
                        {
                            symbol.SetProcessed();
                            BaseTerm t;
                            readingDcgClause = true;
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out t);
                            readingDcgClause = false;
                            body = t.ToDCG(lhs: ref head);
                        }
                    }

                    c = new ClauseNode( head, body: body);

                    if (engine.showSingletonWarnings)
                        engine.ReportSingletons(c: c, lineNo - 1, firstReport: ref firstReport);
                    ps.AddClause(clause: c);
                }
                else if (symbol.TerminalId == PromptSym)
                {
                    symbol.SetProcessed();
                    var m = inQueryMode;
                    var o = isReservedOperatorSetting;
                    try
                    {
                        inQueryMode = true;
                        SetReservedOperators(true);
                        Query(new TerminalSet(terminalCount: terminalCount, Dot), body: out queryNode);
                        IO.Error("'?-' querymode in file not yet supported");
                    }
                    finally
                    {
                        inQueryMode = m;
                        SetReservedOperators(asOpr: o);
                    }
                }
                else
                {
                    symbol.SetProcessed();
                    terminalTable[key: STRINGSTYLE] = StringStyle;
                    terminalTable.Add(iVal: Module, "Module", "module");
                    terminalTable.Add(iVal: Discontiguous, "Discontiguous", "discontiguous");
                    terminalTable.Add(iVal: Dynamic, "Dynamic", "dynamic");
                    try
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, Atom, LSqBracket, OpSym, WrapSym,
                            EnsureLoaded, Discontiguous, StringStyle,
                            AllDiscontiguous, Module, Dynamic), false, true);
                        if (symbol.TerminalId == OpSym)
                        {
                            OpDefinition(new TerminalSet(terminalCount: terminalCount, Dot), true);
                        }
                        else if (symbol.TerminalId == Dynamic)
                        {
                            DynamicDirective(new TerminalSet(terminalCount: terminalCount, Dot));
                        }
                        else if (symbol.TerminalId == WrapSym)
                        {
                            WrapDefinition(new TerminalSet(terminalCount: terminalCount, Dot));
                        }
                        else if (symbol.TerminalId == EnsureLoaded)
                        {
                            symbol.SetProcessed();
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                            if (symbol.TerminalId == Atom)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            var fileName = Utils.ExtendedFileName(symbol.ToString().ToLower(), ".pl");
                            if (Globals.ConsultedFiles[key: fileName] == null)
                            {
                                ps.Consult(fileName: fileName);
                                Globals.ConsultedFiles[key: fileName] = true;
                            }

                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                        }
                        else if (symbol.TerminalId == Discontiguous)
                        {
                            symbol.SetProcessed();
                            BaseTerm t;
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out t);
                            ps.SetDiscontiguous( t);
                        }
                        else if (symbol.TerminalId == StringStyle)
                        {
                            symbol.SetProcessed();
                            BaseTerm t;
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out t);
                            engine.SetStringStyle( t);
                        }
                        else if (symbol.TerminalId == AllDiscontiguous)
                        {
                            symbol.SetProcessed();
                            ps.SetDiscontiguous(true);
                        }
                        else if (symbol.TerminalId == Module)
                        {
                            symbol.SetProcessed();
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                            try
                            {
                                SetCommaAsSeparator(true);
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                                if (symbol.TerminalId == Atom)
                                    symbol.SetProcessed();
                                else
                                    symbol.SetProcessed();
                                ps.SetModuleName(symbol.ToString());
                                IO.Warning("line {0} -- :- 'module' directive not implemented -- ignored",
                                    symbol.LineNo);
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma), true, true);
                            }
                            finally
                            {
                                SetCommaAsSeparator(false);
                            }

                            BaseTerm t;
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, RightParen), t: out t);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                        }
                        else if (symbol.TerminalId == LSqBracket)
                        {
                            symbol.SetProcessed();
                            var lines = 0;
                            var files = 0;
                            try
                            {
                                while (true)
                                {
                                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false,
                                        true);
                                    if (symbol.TerminalId == Atom)
                                        symbol.SetProcessed();
                                    else
                                        symbol.SetProcessed();
                                    var fileName = Utils.FileNameFromSymbol(symbol.ToString(), ".pl");
                                    SetCommaAsSeparator(false);
                                    lines += ps.Consult(fileName: fileName);
                                    files++;
                                    SetCommaAsSeparator(true);
                                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RSqBracket), false,
                                        true);
                                    if (symbol.TerminalId == Comma)
                                        symbol.SetProcessed();
                                    else
                                        break;
                                }

                                if (files > 1) IO.Message("Grand total is {0} lines", lines);
                            }
                            finally
                            {
                                SetCommaAsSeparator(false);
                            }

                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RSqBracket), true, true);
                        }
                        else
                        {
                            SimpleDirective(new TerminalSet(terminalCount: terminalCount, Dot));
                        }
                    }
                    finally
                    {
                        terminalTable.Remove("module");
                        terminalTable.Remove("discontiguous");
                        terminalTable.Remove("dynamic");
                        terminalTable[key: ELLIPSIS] = Atom;
                        terminalTable[key: STRINGSTYLE] = Atom;
                        terminalTable[key: SLASH] = Operator;
                        terminalTable[key: SUBTREE] = Operator;
                    }
                }

                GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot), true, true);
            }

            #endregion

            #region DynamicDirective

            private void DynamicDirective(TerminalSet _TS)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, Dynamic), true, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Operator, Atom), false, true);
                if (symbol.IsMemberOf(Operator, Atom))
                {
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                    if (symbol.TerminalId == Atom)
                        symbol.SetProcessed();
                    else
                        symbol.SetProcessed();
                }
                else
                {
                    symbol.SetProcessed();
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                    if (symbol.TerminalId == Atom)
                        symbol.SetProcessed();
                    else
                        symbol.SetProcessed();
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                }

                var name = symbol.ToString();
                var saveSlash = terminalTable[key: SLASH];
                int arity;
                try
                {
                    terminalTable[key: SLASH] = Slash;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Slash), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral), true, true);
                    arity = symbol.ToInt();
                    IO.Warning("line {0} -- :- 'dynamic' directive not implemented -- ignored", symbol.LineNo);
                }
                finally
                {
                    terminalTable[key: SLASH] = saveSlash;
                }
            }

            #endregion

            #region SimpleDirective

            private void SimpleDirective(TerminalSet _TS)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, Atom), true, true);
                var directive = symbol.ToString();
                var spaceAfter = symbol.IsFollowedByLayoutChar;
                string argument = null;
                var arity = -1;
                var saveSlash = terminalTable[key: SLASH];
                try
                {
                    terminalTable[key: SLASH] = Slash;
                    GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen), false, true);
                    if (symbol.TerminalId == LeftParen)
                    {
                        symbol.SetProcessed();
                        GetSymbol(
                            new TerminalSet(terminalCount: terminalCount, IntLiteral, StringLiteral, Operator, Atom),
                            false, true);
                        if (symbol.IsMemberOf(Operator, Atom))
                        {
                            if (spaceAfter)
                                IO.Error("Illegal space between directive '{0}' and left parenthesis", directive);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                            if (symbol.TerminalId == Atom)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            argument = symbol.ToString();
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen, Slash), false, true);
                            if (symbol.TerminalId == Slash)
                            {
                                symbol.SetProcessed();
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral), true, true);
                                arity = symbol.ToInt();
                            }
                        }
                        else
                        {
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, StringLiteral), false,
                                true);
                            if (symbol.TerminalId == StringLiteral)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            argument = symbol.ToString().Dequoted();
                        }

                        GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                    }

                    ps.HandleSimpleDirective(this, directive: directive, argument: argument, arity);
                }
                finally
                {
                    terminalTable[key: SLASH] = saveSlash;
                }
            }

            #endregion

            #region OpDefinition

            private void OpDefinition(TerminalSet _TS, bool user)
            {
                string name;
                string assoc;
                try
                {
                    SetCommaAsSeparator(true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, OpSym), true, true);
                    int prec;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, CutSym), false, true);
                    if (symbol.TerminalId == IntLiteral)
                    {
                        symbol.SetProcessed();
                        prec = symbol.ToInt();
                    }
                    else
                    {
                        symbol.SetProcessed();
                        prec = -1;
                    }

                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                    if (symbol.TerminalId == Atom)
                        symbol.SetProcessed();
                    else
                        symbol.SetProcessed();
                    assoc = symbol.ToString();
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Operator, Atom, LSqBracket,
                        OpSym, WrapSym, EnsureLoaded,
                        Discontiguous, StringStyle, AllDiscontiguous, Module, Dynamic, WrapOpen,
                        WrapClose), false, true);
                    if (symbol.TerminalId == LSqBracket)
                    {
                        symbol.SetProcessed();
                        while (true)
                        {
                            PotentialOpName(new TerminalSet(terminalCount: terminalCount, Comma, RSqBracket),
                                name: out name);
                            if (prec == -1)
                                RemovePrologOperator(type: assoc, name: name, user: user);
                            else
                                AddPrologOperator(prec: prec, type: assoc, name: name, user: user);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RSqBracket), false, true);
                            if (symbol.TerminalId == Comma)
                                symbol.SetProcessed();
                            else
                                break;
                        }

                        GetSymbol(new TerminalSet(terminalCount: terminalCount, RSqBracket), true, true);
                    }
                    else
                    {
                        PotentialOpName(new TerminalSet(terminalCount: terminalCount, RightParen), name: out name);
                        if (prec == -1)
                            RemovePrologOperator(type: assoc, name: name, user: user);
                        else
                            AddPrologOperator(prec: prec, type: assoc, name: name, user: user);
                    }

                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                }
                finally
                {
                    SetCommaAsSeparator(false);
                }
            }

            #endregion

            #region WrapDefinition

            private void WrapDefinition(TerminalSet _TS)
            {
                // wrapClose is set to the reverse of wrapOpen if only one argument is supplied.
                string wrapOpen;
                string wrapClose = null;
                var useAsList = false;
                try
                {
                    SetCommaAsSeparator(true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, WrapSym), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                    PotentialOpName(new TerminalSet(terminalCount: terminalCount, Comma, RightParen),
                        name: out wrapOpen);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RightParen), false, true);
                    if (symbol.TerminalId == Comma)
                    {
                        symbol.SetProcessed();
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, RightParen, Operator, Atom,
                            VBar, OpSym, WrapSym, EnsureLoaded,
                            Discontiguous, StringStyle, AllDiscontiguous, Module, Dynamic, WrapOpen,
                            WrapClose), false, true);
                        if (symbol.IsMemberOf(LeftParen, Operator, Atom, VBar, OpSym, WrapSym, EnsureLoaded,
                            Discontiguous, StringStyle,
                            AllDiscontiguous, Module, Dynamic, WrapOpen, WrapClose))
                        {
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Operator, Atom, VBar,
                                OpSym, WrapSym, EnsureLoaded,
                                Discontiguous, StringStyle, AllDiscontiguous, Module, Dynamic, WrapOpen,
                                WrapClose), false, true);
                            if (symbol.TerminalId == VBar)
                            {
                                symbol.SetProcessed();
                                useAsList = true;
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RightParen), false,
                                    true);
                                if (symbol.TerminalId == Comma)
                                {
                                    symbol.SetProcessed();
                                    PotentialOpName(new TerminalSet(terminalCount: terminalCount, RightParen),
                                        name: out wrapClose);
                                    wrapClose = symbol.ToString();
                                }
                            }
                            else
                            {
                                PotentialOpName(new TerminalSet(terminalCount: terminalCount, RightParen),
                                    name: out wrapClose);
                                wrapClose = symbol.ToString();
                            }
                        }
                    }

                    if (wrapClose == null) wrapClose = wrapOpen.Mirror();
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                    AddBracketPair(openBracket: wrapOpen, closeBracket: wrapClose, useAsList: useAsList);
                }
                finally
                {
                    SetCommaAsSeparator(false);
                }
            }

            #endregion

            #region PotentialOpName

            private void PotentialOpName(TerminalSet _TS, out string name)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Operator, Atom, OpSym, WrapSym,
                        EnsureLoaded, Discontiguous,
                        StringStyle, AllDiscontiguous, Module, Dynamic, WrapOpen, WrapClose), false,
                    true);
                if (symbol.IsMemberOf(Operator, Atom, OpSym, WrapSym, EnsureLoaded, Discontiguous, StringStyle,
                    AllDiscontiguous,
                    Module, Dynamic, WrapOpen, WrapClose))
                {
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom, OpSym, WrapSym,
                        EnsureLoaded, Discontiguous, StringStyle,
                        AllDiscontiguous, Module, Dynamic, WrapOpen, WrapClose), false, true);
                    if (symbol.TerminalId == Atom)
                        symbol.SetProcessed();
                    else if (symbol.TerminalId == Operator)
                        symbol.SetProcessed();
                    else if (symbol.TerminalId == WrapOpen)
                        symbol.SetProcessed();
                    else if (symbol.TerminalId == WrapClose)
                        symbol.SetProcessed();
                    else
                        ReservedWord(_TS: _TS);
                    name = symbol.ToString();
                }
                else
                {
                    symbol.SetProcessed();
                    PotentialOpName(new TerminalSet(terminalCount: terminalCount, RightParen), name: out name);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                }
            }

            #endregion

            #region ReservedWord

            private void ReservedWord(TerminalSet _TS)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, OpSym, WrapSym, EnsureLoaded, Discontiguous,
                    StringStyle, AllDiscontiguous,
                    Module, Dynamic), false, true);
                if (symbol.TerminalId == OpSym)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == WrapSym)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == EnsureLoaded)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == Discontiguous)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == StringStyle)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == AllDiscontiguous)
                    symbol.SetProcessed();
                else if (symbol.TerminalId == Module)
                    symbol.SetProcessed();
                else
                    symbol.SetProcessed();
            }

            #endregion

            #region Predefineds

            private void Predefineds(TerminalSet _TS)
            {
                do
                {
                    Predefined(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                        ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, ImpliesSym, LSqBracket, LCuBracket,
                        ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral));
                    GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                        ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, ImpliesSym, LSqBracket, LCuBracket,
                        ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral), false, true);
                } while (!_TS.Contains(terminal: symbol.TerminalId));
            }

            #endregion

            #region Predefined

            private void Predefined(TerminalSet _TS)
            {
                BaseTerm head;
                var opt = true;
                TermNode body = null;
                engine.EraseVariables();
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    Operator, Atom, Anonymous, CutSym, ImpliesSym, LSqBracket, LCuBracket,
                    ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                    VerbatimStringLiteral), false, true);
                if (symbol.TerminalId == ImpliesSym)
                {
                    symbol.SetProcessed();
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Atom, OpSym, WrapSym), false, true);
                    if (symbol.TerminalId == OpSym)
                        OpDefinition(new TerminalSet(terminalCount: terminalCount, Dot), false);
                    else if (symbol.TerminalId == WrapSym)
                        WrapDefinition(new TerminalSet(terminalCount: terminalCount, Dot));
                    else
                        SimpleDirective(new TerminalSet(terminalCount: terminalCount, Dot));
                }
                else
                {
                    PrologTerm(
                        new TerminalSet(terminalCount: terminalCount, Dot, ImpliesSym, DCGArrowSym, BuiltinCSharp),
                        t: out head);
                    GetSymbol(
                        new TerminalSet(terminalCount: terminalCount, Dot, ImpliesSym, DCGArrowSym, BuiltinCSharp),
                        false, true);
                    if (symbol.IsMemberOf(ImpliesSym, DCGArrowSym, BuiltinCSharp))
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, ImpliesSym, DCGArrowSym, BuiltinCSharp),
                            false, true);
                        if (symbol.TerminalId == BuiltinCSharp)
                        {
                            symbol.SetProcessed();
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Operator, Atom), false, true);
                            if (symbol.TerminalId == Atom)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            ps.AddPredefined(new ClauseNode( head, new TermNode(symbol.ToString())));
                            opt = false;
                        }
                        else if (symbol.TerminalId == ImpliesSym)
                        {
                            symbol.SetProcessed();
                            Query(new TerminalSet(terminalCount: terminalCount, Dot), body: out body);
                            ps.AddPredefined(new ClauseNode( head, body: body));
                            opt = false;
                        }
                        else
                        {
                            symbol.SetProcessed();
                            BaseTerm term;
                            readingDcgClause = true;
                            PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out term);
                            readingDcgClause = false;
                            body = term.ToDCG(lhs: ref head);
                            ps.AddPredefined(new ClauseNode( head, body: body));
                            opt = false;
                        }
                    }

                    if (opt) ps.AddPredefined(new ClauseNode( head, null));
                }

                GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot), true, true);
            }

            #endregion

            #region Query

            private void Query(TerminalSet _TS, out TermNode body)
            {
                BaseTerm t = null;
                PrologTerm(_TS: _TS, t: out t);
                body = t.ToGoalList();
            }

            #endregion

            #region PrologTerm

            private void PrologTerm(TerminalSet _TS, out BaseTerm t)
            {
                var saveStatus = SetCommaAsSeparator(false);
                PrologTermEx(_TS: _TS, t: out t);
                SetCommaAsSeparator(mode: saveStatus);
            }

            #endregion

            #region PrologTermEx

            private void PrologTermEx(TerminalSet _TS, out BaseTerm t)
            {
                string functor;
                OpDescrTriplet triplet;
                bool spaceAfter;
                var tokenSeqToTerm = new TokenSeqToTerm(opTable: opTable);
                do
                {
                    triplet = null;
                    BaseTerm[] args = null;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                        RealLiteral, ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                        TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral), false, true);
                    if (symbol.TerminalId == Operator)
                    {
                        symbol.SetProcessed();
                        spaceAfter = symbol.IsFollowedByLayoutChar;
                        triplet = symbol.Payload;
                        var commaAsSeparator = !spaceAfter && tokenSeqToTerm.PrevTokenWasOperator;
                        GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral, StringLiteral,
                                Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                                TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral),
                            false, true);
                        if (symbol.TerminalId == LeftParen)
                        {
                            symbol.SetProcessed();
                            ArgumentList(new TerminalSet(terminalCount: terminalCount, RightParen), args: out args,
                                commaIsSeparator: commaAsSeparator);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                        }

                        if (args == null)
                        {
                            tokenSeqToTerm.Add(triplet: triplet); // single operator
                        }
                        else if (commaAsSeparator)
                        {
                            tokenSeqToTerm.AddOperatorFunctor(triplet: triplet,
                                args: args); // operator as functor with >= 1 args
                        }
                        else
                        {
                            tokenSeqToTerm.Add(triplet: triplet);
                            tokenSeqToTerm.Add(args[0]);
                        }
                    }
                    else
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                            RealLiteral, ImagLiteral,
                            StringLiteral, Atom, Anonymous, CutSym, LSqBracket, LCuBracket,
                            ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                            VerbatimStringLiteral), false, true);
                        if (symbol.TerminalId == Atom)
                        {
                            symbol.SetProcessed();
                            functor = symbol.ToString();
                            spaceAfter = symbol.IsFollowedByLayoutChar;
                            GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral, StringLiteral,
                                Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                                TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                                VerbatimStringLiteral), false, true);
                            if (symbol.TerminalId == LeftParen)
                            {
                                symbol.SetProcessed();
                                ArgumentList(new TerminalSet(terminalCount: terminalCount, RightParen), args: out args,
                                    true);
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                            }

                            tokenSeqToTerm.AddFunctorTerm( functor, spaceAfter: spaceAfter, args: args);
                        }
                        else if (symbol.TerminalId == LeftParen)
                        {
                            symbol.SetProcessed();
                            var saveStatus = SetCommaAsSeparator(false);
                            PrologTermEx(new TerminalSet(terminalCount: terminalCount, RightParen), t: out t);
                            SetCommaAsSeparator(mode: saveStatus);
                            tokenSeqToTerm.Add( t);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                        }
                        else if (symbol.TerminalId == Identifier)
                        {
                            GetIdentifier(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket,
                                ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                                VerbatimStringLiteral), t: out t);
                            tokenSeqToTerm.Add( t);
                        }
                        else if (symbol.TerminalId == Anonymous)
                        {
                            symbol.SetProcessed();
                            tokenSeqToTerm.Add(new AnonymousVariable());
                        }
                        else if (symbol.TerminalId == CutSym)
                        {
                            symbol.SetProcessed();
                            tokenSeqToTerm.Add(new Cut(0));
                        }
                        else if (symbol.TerminalId == AltListOpen)
                        {
                            AltList(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                    RealLiteral, ImagLiteral, StringLiteral,
                                    Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                                    TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral),
                                term: out t);
                            tokenSeqToTerm.Add( t);
                        }
                        else if (symbol.TerminalId == LSqBracket)
                        {
                            List(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                                    ImagLiteral, StringLiteral,
                                    Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                                    TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral),
                                term: out t);
                            tokenSeqToTerm.Add( t);
                        }
                        else if (symbol.IsMemberOf(IntLiteral, RealLiteral))
                        {
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, RealLiteral), false,
                                true);
                            if (symbol.TerminalId == IntLiteral)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            tokenSeqToTerm.Add(new DecimalTerm(symbol.ToDecimal()));
                        }
                        else if (symbol.TerminalId == ImagLiteral)
                        {
                            symbol.SetProcessed();
                            tokenSeqToTerm.Add(new ComplexTerm(0, symbol.ToImaginary()));
                        }
                        else if (symbol.TerminalId == StringLiteral)
                        {
                            symbol.SetProcessed();
                            var s = symbol.ToUnquoted();
                            s = ConfigSettings.ResolveEscapes ? s.Unescaped() : s.Replace("\"\"", "\"");
                            tokenSeqToTerm.Add(engine.NewIsoOrCsStringTerm( s));
                        }
                        else if (symbol.TerminalId == VerbatimStringLiteral)
                        {
                            symbol.SetProcessed();
                            var s = symbol.ToUnquoted();
                            s = s.Replace("\"\"", "\"");
                            tokenSeqToTerm.Add(engine.NewIsoOrCsStringTerm( s));
                        }
                        else if (symbol.TerminalId == LCuBracket)
                        {
                            DCGBracketList(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom, Anonymous, CutSym, LSqBracket,
                                LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen,
                                AltListClose, VerbatimStringLiteral), term: out t);
                            tokenSeqToTerm.Add( t);
                        }
                        else if (symbol.TerminalId == WrapOpen)
                        {
                            symbol.SetProcessed();
                            var wrapOpen = symbol.ToString();
                            GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral, StringLiteral,
                                Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                                TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                                VerbatimStringLiteral), false, true);
                            if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom,
                                Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                                AltListOpen, AltListClose, VerbatimStringLiteral))
                            {
                                var wrapClose = engine.WrapTable.FindCloseBracket(openBracket: wrapOpen);
                                var saveStatus = SetCommaAsSeparator(false);
                                ArgumentList(new TerminalSet(terminalCount: terminalCount, WrapClose), args: out args,
                                    true);
                                SetCommaAsSeparator(mode: saveStatus);
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, WrapClose), true, true);
                                if (symbol.ToString() != wrapClose)
                                    IO.Error("Illegal wrapper close token: got '{0}' expected '{1}'",
                                        symbol.ToString(), wrapClose);
                                tokenSeqToTerm.Add(new WrapperTerm(wrapOpen: wrapOpen, wrapClose: wrapClose, a: args));
                            }

                            if (args == null) tokenSeqToTerm.Add(new AtomTerm(wrapOpen.ToAtom()));
                        }
                        else if (symbol.IsMemberOf(WrapClose, AltListClose))
                        {
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, WrapClose, AltListClose), false,
                                true);
                            if (symbol.TerminalId == WrapClose)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            var orphanCloseBracket = symbol.ToString();
                            tokenSeqToTerm.Add(new AtomTerm(orphanCloseBracket.ToAtom()));
                        }
                        else if (symbol.TerminalId == ListPatternOpen)
                        {
                            symbol.SetProcessed();
                            ListPatternMembers(new TerminalSet(terminalCount: terminalCount, ListPatternClose),
                                t: out t);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, ListPatternClose), true, true);
                            tokenSeqToTerm.Add( t);
                        }
                        else
                        {
                            TryCatchClause(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                                RealLiteral, ImagLiteral,
                                StringLiteral, Operator, Atom, Anonymous, CutSym, LSqBracket,
                                LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen,
                                AltListClose, VerbatimStringLiteral), tokenSeqToTerm: tokenSeqToTerm, t: out t);
                        }
                    }

                    GetSymbol(_TS.Union(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                            ImagLiteral, StringLiteral,
                            Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                            TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose, VerbatimStringLiteral),
                        false, true);
                } while (!_TS.Contains(terminal: symbol.TerminalId));

                tokenSeqToTerm.ConstructPrefixTerm( out t);
            }

            #endregion

            #region GetIdentifier

            private void GetIdentifier(TerminalSet _TS, out BaseTerm t)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, Identifier), true, true);
                var id = symbol.ToString();
                t = engine.GetVariable( id);
                if (t == null)
                {
                    t = new NamedVariable(name: id);
                    engine.SetVariable( t, s: id);
                }
                else
                {
                    engine.RegisterVarNonSingleton( id);
                }
            }

            #endregion

            #region ArgumentList

            private void ArgumentList(TerminalSet _TS, out BaseTerm[] args, bool commaIsSeparator)
            {
                var b = isReservedOperatorSetting;
                var argList = new List<BaseTerm>();
                BaseTerm a;
                var saveStatus = SetCommaAsSeparator(mode: commaIsSeparator);
                SetReservedOperators(true);
                while (true)
                {
                    PrologTermEx(_TS.Union(terminalCount: terminalCount, Comma), t: out a);
                    argList.Add(item: a);
                    GetSymbol(_TS.Union(terminalCount: terminalCount, Comma), false, true);
                    if (symbol.TerminalId == Comma)
                        symbol.SetProcessed();
                    else
                        break;
                }

                SetCommaAsSeparator(mode: saveStatus);
                SetReservedOperators(asOpr: b);
                args = argList.ToArray();
            }

            #endregion

            #region ListPatternMembers

            private void ListPatternMembers(TerminalSet _TS, out BaseTerm t)
            {
                var b = isReservedOperatorSetting;
                List<SearchTerm> searchTerms;
                var saveStatus = SetCommaAsSeparator(true);
                var saveEllipsis = terminalTable[key: ELLIPSIS];
                var saveNegate = terminalTable[key: NEGATE];
                var saveSubtree = terminalTable[key: SUBTREE];
                SetReservedOperators(true);
                bool isRangeVar;
                var lastWasRange = false;
                var rangeTerms = new List<ListPatternElem>();
                try
                {
                    var isSearchTerm = false;
                    BaseTerm RangeVar = null;
                    BaseTerm minLenTerm;
                    BaseTerm maxLenTerm;
                    BaseTerm altListName = null;
                    minLenTerm = maxLenTerm = new DecimalTerm(0);
                    searchTerms = null;
                    bool isSingleVar;
                    var isNegSearch = false;
                    while (true)
                    {
                        terminalTable[key: ELLIPSIS] = EllipsisSym;
                        terminalTable[key: NEGATE] = NegateSym;
                        terminalTable[key: SUBTREE] = SubtreeSym;
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                            RealLiteral, ImagLiteral, StringLiteral,
                            Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                            EllipsisSym, NegateSym, TrySym, WrapOpen, WrapClose, AltListOpen,
                            AltListClose, VerbatimStringLiteral), false, true);
                        if (symbol.IsMemberOf(LCuBracket, EllipsisSym))
                        {
                            if (lastWasRange)
                            {
                                rangeTerms.Add(new ListPatternElem(minLenTerm: minLenTerm, maxLenTerm: maxLenTerm,
                                    rangeBindVar: RangeVar, null, null, false, false));
                                RangeVar = null;
                            }

                            Range(_TS.Union(terminalCount: terminalCount, Comma), minLenTerm: out minLenTerm,
                                maxLenTerm: out maxLenTerm);
                            lastWasRange = true;
                        }
                        else
                        {
                            isRangeVar = false;
                            AlternativeTerms(_TS.Union(terminalCount: terminalCount, Comma, LCuBracket, EllipsisSym),
                                saveEllipsis: saveEllipsis, saveNegate: saveNegate, saveSubtree: saveSubtree,
                                searchTerms: out searchTerms, altListName: out altListName,
                                isSingleVar: out isSingleVar, isNegSearch: out isNegSearch
                            );
                            isSearchTerm = true;
                            GetSymbol(_TS.Union(terminalCount: terminalCount, Comma, LCuBracket, EllipsisSym), false,
                                true);
                            if (symbol.IsMemberOf(LCuBracket, EllipsisSym))
                            {
                                if (!isSingleVar) IO.Error("Range specifier may be preceded by a variable only");
                                if (lastWasRange)
                                    rangeTerms.Add(new ListPatternElem(minLenTerm: minLenTerm, maxLenTerm: maxLenTerm,
                                        rangeBindVar: RangeVar, null, null, false, false));
                                Range(_TS.Union(terminalCount: terminalCount, Comma), minLenTerm: out minLenTerm,
                                    maxLenTerm: out maxLenTerm);
                                isRangeVar = true;
                                lastWasRange = true;
                                isSearchTerm = false;
                            }

                            if (isRangeVar)
                                RangeVar = searchTerms[0].term;
                            else
                                lastWasRange = false;
                        }

                        if (isSearchTerm)
                        {
                            rangeTerms.Add(new ListPatternElem(minLenTerm: minLenTerm, maxLenTerm: maxLenTerm,
                                rangeBindVar: RangeVar, altListVar: altListName, altSearchTerms: searchTerms,
                                isNegSearch: isNegSearch, false));
                            isSearchTerm = false;
                            RangeVar = null;
                            altListName = null;
                            searchTerms = null;
                            minLenTerm = maxLenTerm = new DecimalTerm(0);
                        }

                        GetSymbol(_TS.Union(terminalCount: terminalCount, Comma), false, true);
                        if (symbol.TerminalId == Comma)
                            symbol.SetProcessed();
                        else
                            break;
                    }

                    if (lastWasRange)
                        rangeTerms.Add(new ListPatternElem(minLenTerm: minLenTerm, maxLenTerm: maxLenTerm,
                            rangeBindVar: RangeVar, null, null, false, false));
                    t = new ListPatternTerm(rangeTerms.ToArray());
                }
                finally
                {
                    SetCommaAsSeparator(mode: saveStatus);
                    terminalTable[key: ELLIPSIS] = saveEllipsis;
                    terminalTable[key: NEGATE] = saveNegate;
                    terminalTable[key: SUBTREE] = saveSubtree;
                    SetReservedOperators(asOpr: b);
                }
            }

            #endregion

            #region AlternativeTerms

            private void AlternativeTerms(TerminalSet _TS,
                int saveEllipsis, int saveNegate, int saveSubtree, out List<SearchTerm> searchTerms,
                out BaseTerm altListName, out bool isSingleVar, out bool isNegSearch)
            {
                searchTerms = new List<SearchTerm>();
                BaseTerm t;
                altListName = null;
                var first = true;
                DownRepFactor downRepFactor = null;
                isNegSearch = false;
                while (true)
                {
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral,
                        RealLiteral, ImagLiteral, StringLiteral,
                        Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                        NegateSym, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral), false, true);
                    if (symbol.TerminalId == NegateSym)
                    {
                        if (isNegSearch)
                            IO.Error("Only one '~' allowed (which will apply to the entire alternatives list)");
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, NegateSym), true, true);
                        isNegSearch = true;
                    }

                    //terminalTable [ELLIPSIS] = saveEllipsis;
                    //terminalTable [NEGATE]   = saveNegate;
                    //terminalTable [SUBTREE]  = saveSubtree;
                    PrologTermEx(_TS.Union(terminalCount: terminalCount, CutSym, VBar), t: out t);
                    //terminalTable [ELLIPSIS] = EllipsisSym;
                    //terminalTable [NEGATE]   = NegateSym;
                    //terminalTable [SUBTREE]  = SubtreeSym;
                    if (!first) searchTerms.Add(new SearchTerm(downRepFactor: downRepFactor, term: t));
                    //if (t is AnonymousVariable)
                    //  IO.Warning ("Anonymous variable in alternatives list makes it match anything");
                    GetSymbol(_TS.Union(terminalCount: terminalCount, CutSym, VBar), false, true);
                    if (symbol.IsMemberOf(CutSym, VBar))
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, CutSym, VBar), false, true);
                        if (symbol.TerminalId == VBar)
                        {
                            symbol.SetProcessed();
                            if (first) searchTerms.Add(new SearchTerm(downRepFactor: downRepFactor, term: t));
                        }
                        else
                        {
                            symbol.SetProcessed();
                            if (first)
                            {
                                if (t is Variable)
                                {
                                    if (isNegSearch)
                                        IO.Error("'~' not allowed before alternatives list name");
                                    else
                                        altListName = t;
                                }
                                else
                                {
                                    IO.Error("Variable expected before !");
                                }

                                first = false;
                            }
                            else
                            {
                                IO.Error("Only one ! allowed for alternatives list");
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (first) searchTerms.Add(new SearchTerm(downRepFactor: downRepFactor, term: t));
                isSingleVar = searchTerms.Count == 1 && searchTerms[0].term is Variable;
            }

            #endregion

            #region Range

            private void Range(TerminalSet _TS, out BaseTerm minLenTerm, out BaseTerm maxLenTerm)
            {
                try
                {
                    savePlusSym = terminalTable[key: PLUSSYM];
                    saveTimesSym = terminalTable[key: TIMESSYM];
                    saveQuestionMark = terminalTable[key: QUESTIONMARK];
                    terminalTable[key: PLUSSYM] = PlusSym;
                    terminalTable[key: TIMESSYM] = TimesSym;
                    terminalTable[key: QUESTIONMARK] = QuestionMark;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LCuBracket, EllipsisSym), false, true);
                    if (symbol.TerminalId == LCuBracket)
                    {
                        var minLen = 0;
                        var maxLen = 0;
                        minLenTerm = maxLenTerm = DecimalTerm.ZERO;
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, LCuBracket), true, true);
                        GetSymbol(
                            _TS.Union(terminalCount: terminalCount, Comma, Identifier, IntLiteral, PlusSym, TimesSym,
                                QuestionMark), false, true);
                        if (symbol.IsMemberOf(Comma, Identifier, IntLiteral, RCuBracket))
                        {
                            GetSymbol(
                                new TerminalSet(terminalCount: terminalCount, Comma, Identifier, IntLiteral,
                                    RCuBracket), false, true);
                            if (symbol.IsMemberOf(Identifier, IntLiteral))
                            {
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, Identifier, IntLiteral), false,
                                    true);
                                if (symbol.TerminalId == IntLiteral)
                                {
                                    symbol.SetProcessed();
                                    minLen = maxLen = symbol.ToInt();
                                    minLenTerm = maxLenTerm = new DecimalTerm( minLen);
                                }
                                else
                                {
                                    GetIdentifier(new TerminalSet(terminalCount: terminalCount, Comma, RCuBracket),
                                        t: out minLenTerm);
                                    maxLenTerm = minLenTerm;
                                }
                            }

                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, RCuBracket), false, true);
                            if (symbol.TerminalId == Comma)
                            {
                                symbol.SetProcessed();
                                maxLen = Infinite;
                                maxLenTerm = new DecimalTerm( Infinite);
                                GetSymbol(
                                    new TerminalSet(terminalCount: terminalCount, Identifier, IntLiteral, RCuBracket),
                                    false, true);
                                if (symbol.IsMemberOf(Identifier, IntLiteral))
                                {
                                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Identifier, IntLiteral),
                                        false, true);
                                    if (symbol.TerminalId == IntLiteral)
                                    {
                                        symbol.SetProcessed();
                                        if (minLen > (maxLen = symbol.ToInt()))
                                            IO.Error(
                                                "Range lower bound {0} not allowed to be greater than range upper bound {1}",
                                                minLen, maxLen);
                                        maxLenTerm = new DecimalTerm( maxLen);
                                    }
                                    else
                                    {
                                        GetIdentifier(new TerminalSet(terminalCount: terminalCount, RCuBracket),
                                            t: out maxLenTerm);
                                    }
                                }
                            }
                        }
                        else if (symbol.TerminalId == TimesSym)
                        {
                            symbol.SetProcessed();
                            minLenTerm = new DecimalTerm(0);
                            maxLenTerm = new DecimalTerm( Infinite);
                        }
                        else if (symbol.TerminalId == PlusSym)
                        {
                            symbol.SetProcessed();
                            minLenTerm = new DecimalTerm(1);
                            maxLenTerm = new DecimalTerm( Infinite);
                        }
                        else if (symbol.TerminalId == QuestionMark)
                        {
                            symbol.SetProcessed();
                            minLenTerm = new DecimalTerm(0);
                            maxLenTerm = new DecimalTerm(1);
                        }

                        GetSymbol(new TerminalSet(terminalCount: terminalCount, RCuBracket), true, true);
                    }
                    else
                    {
                        symbol.SetProcessed();
                        minLenTerm = new DecimalTerm(0);
                        maxLenTerm = new DecimalTerm( Infinite);
                    }
                }
                finally
                {
                    terminalTable[key: PLUSSYM] = savePlusSym;
                    terminalTable[key: TIMESSYM] = saveTimesSym;
                    terminalTable[key: QUESTIONMARK] = saveQuestionMark;
                }
            }

            #endregion

            #region TryCatchClause

            private void TryCatchClause(TerminalSet _TS, TokenSeqToTerm tokenSeqToTerm, out BaseTerm t)
            {
                GetSymbol(new TerminalSet(terminalCount: terminalCount, TrySym), true, true);
                var nullClass = false;
                tokenSeqToTerm.Add(new TryOpenTerm());
                tokenSeqToTerm.Add(triplet: CommaOpTriplet);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                PrologTermEx(new TerminalSet(terminalCount: terminalCount, RightParen), t: out t);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                tokenSeqToTerm.Add( t);
                tokenSeqToTerm.Add(triplet: CommaOpTriplet);
                var ecNames = new List<string>();
                var catchSeqNo = 0;
                do
                {
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, CatchSym), true, true);
                    if (nullClass)
                        IO.Error("No CATCH-clause allowed after CATCH-clause without exception class");
                    string exceptionClass = null;
                    BaseTerm msgVar = null;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, Atom),
                        false, true);
                    if (symbol.IsMemberOf(Identifier, IntLiteral, Atom))
                    {
                        GetSymbol(new TerminalSet(terminalCount: terminalCount, Identifier, IntLiteral, Atom), false,
                            true);
                        if (symbol.IsMemberOf(IntLiteral, Atom))
                        {
                            var saveStatus = SetCommaAsSeparator(true);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, IntLiteral, Atom), false, true);
                            if (symbol.TerminalId == Atom)
                                symbol.SetProcessed();
                            else
                                symbol.SetProcessed();
                            if (ecNames.Contains(exceptionClass = symbol.ToString()))
                                IO.Error("Duplicate exception class name '{0}'", exceptionClass);
                            else
                                ecNames.Add(item: exceptionClass);
                            GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, LeftParen, Identifier),
                                false, true);
                            if (symbol.IsMemberOf(Comma, Identifier))
                            {
                                GetSymbol(new TerminalSet(terminalCount: terminalCount, Comma, Identifier), false,
                                    true);
                                if (symbol.TerminalId == Comma) symbol.SetProcessed();
                                GetIdentifier(new TerminalSet(terminalCount: terminalCount, LeftParen), t: out msgVar);
                            }

                            SetCommaAsSeparator(mode: saveStatus);
                        }
                        else
                        {
                            GetIdentifier(new TerminalSet(terminalCount: terminalCount, LeftParen), t: out msgVar);
                        }
                    }

                    nullClass = nullClass || exceptionClass == null;
                    if (msgVar == null) msgVar = new AnonymousVariable();
                    tokenSeqToTerm.Add(new CatchOpenTerm(exceptionClass: exceptionClass, msgVar: msgVar,
                        seqNo: catchSeqNo++));
                    tokenSeqToTerm.Add(triplet: CommaOpTriplet);
                    t = null;
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen), true, true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, RightParen, Identifier,
                        IntLiteral, RealLiteral, ImagLiteral,
                        StringLiteral, Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket,
                        ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                        VerbatimStringLiteral), false, true);
                    if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                        Operator, Atom,
                        Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                        AltListOpen, AltListClose, VerbatimStringLiteral))
                        PrologTermEx(new TerminalSet(terminalCount: terminalCount, RightParen), t: out t);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RightParen), true, true);
                    if (t != null)
                    {
                        tokenSeqToTerm.Add( t);
                        tokenSeqToTerm.Add(triplet: CommaOpTriplet);
                    }

                    GetSymbol(_TS.Union(terminalCount: terminalCount, CatchSym), false, true);
                } while (!_TS.Contains(terminal: symbol.TerminalId));

                tokenSeqToTerm.Add( TC_CLOSE);
            }

            #endregion

            #region OptionalPrologTerm

            private void OptionalPrologTerm(TerminalSet _TS, out BaseTerm t)
            {
                t = null;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    EndOfInput, Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket,
                    ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                    VerbatimStringLiteral), false, true);
                if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                    Operator, Atom,
                    Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                    AltListOpen, AltListClose, VerbatimStringLiteral))
                {
                    PrologTerm(new TerminalSet(terminalCount: terminalCount, Dot), t: out t);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, Dot), true, true);
                }
                else
                {
                    symbol.SetProcessed();
                }
            }

            #endregion

            #region List

            private void List(TerminalSet _TS, out BaseTerm term)
            {
                BaseTerm afterBar = null;
                terminalTable[key: OP] = Atom;
                terminalTable[key: WRAP] = Atom;
                BaseTerm[] elements = null;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LSqBracket), true, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    Operator, Atom, Anonymous, CutSym, LSqBracket, RSqBracket, LCuBracket,
                    ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                    VerbatimStringLiteral), false, true);
                if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                    Operator, Atom,
                    Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                    AltListOpen, AltListClose, VerbatimStringLiteral))
                {
                    ArgumentList(new TerminalSet(terminalCount: terminalCount, RSqBracket, VBar), args: out elements,
                        true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, RSqBracket, VBar), false, true);
                    if (symbol.TerminalId == VBar)
                    {
                        symbol.SetProcessed();
                        PrologTerm(new TerminalSet(terminalCount: terminalCount, RSqBracket), t: out afterBar);
                    }
                }

                terminalTable[key: OP] = OpSym;
                terminalTable[key: WRAP] = WrapSym;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, RSqBracket), true, true);
                term = afterBar == null ? new ListTerm() : afterBar;
                if (elements != null) term = ListTerm.ListFromArray(ta: elements, afterBar: term);
            }

            #endregion

            #region AltList

            private void AltList(TerminalSet _TS, out BaseTerm term)
            {
                BaseTerm afterBar = null;
                terminalTable[key: OP] = Atom;
                terminalTable[key: WRAP] = Atom;
                BaseTerm[] elements = null;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, AltListOpen), true, true);
                var altListOpen = symbol.ToString();
                var altListClose = engine.AltListTable.FindCloseBracket(openBracket: altListOpen);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen,
                    TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                    VerbatimStringLiteral), false, true);
                if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                    Operator, Atom,
                    Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                    AltListOpen, AltListClose, VerbatimStringLiteral))
                {
                    ArgumentList(new TerminalSet(terminalCount: terminalCount, VBar, AltListClose), args: out elements,
                        true);
                    GetSymbol(new TerminalSet(terminalCount: terminalCount, VBar, AltListClose), false, true);
                    if (symbol.TerminalId == VBar)
                    {
                        symbol.SetProcessed();
                        PrologTerm(new TerminalSet(terminalCount: terminalCount, AltListClose), t: out afterBar);
                    }
                }

                terminalTable[key: OP] = OpSym;
                terminalTable[key: WRAP] = WrapSym;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, AltListClose), true, true);
                if (symbol.ToString() != altListClose)
                    IO.Error("Illegal alternative list close token: got '{0}' expected '{1}'",
                        symbol.ToString(), altListClose);
                term = afterBar == null
                    ? new AltListTerm(leftBracket: altListOpen, rightBracket: altListClose)
                    : afterBar;
                if (elements != null)
                    term = AltListTerm.ListFromArray(leftBracket: altListOpen, rightBracket: altListClose, ta: elements,
                        afterBar: term);
            }

            #endregion

            #region DCGBracketList

            private void DCGBracketList(TerminalSet _TS, out BaseTerm term)
            {
                terminalTable[key: OP] = Atom;
                terminalTable[key: WRAP] = Atom;
                BaseTerm[] elements = null;
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LCuBracket), true, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, LeftParen, Identifier, IntLiteral, RealLiteral,
                    ImagLiteral, StringLiteral,
                    Operator, Atom, Anonymous, CutSym, LSqBracket, LCuBracket, RCuBracket,
                    ListPatternOpen, TrySym, WrapOpen, WrapClose, AltListOpen, AltListClose,
                    VerbatimStringLiteral), false, true);
                if (symbol.IsMemberOf(LeftParen, Identifier, IntLiteral, RealLiteral, ImagLiteral, StringLiteral,
                    Operator, Atom,
                    Anonymous, CutSym, LSqBracket, LCuBracket, ListPatternOpen, TrySym, WrapOpen, WrapClose,
                    AltListOpen, AltListClose, VerbatimStringLiteral))
                    ArgumentList(new TerminalSet(terminalCount: terminalCount, RCuBracket), args: out elements, true);
                GetSymbol(new TerminalSet(terminalCount: terminalCount, RCuBracket), true, true);
                term = BaseTerm.NULLCURL;
                if (elements != null)
                    if (readingDcgClause)
                        for (var i = elements.Length - 1; i >= 0; i--)
                            term = new DcgTerm(elements[i], t1: term);
                    else
                        term = new CompoundTerm( CURL, args: elements);
            }

            #endregion

            #endregion PARSER PROCEDURES
        }

        #endregion PrologParser
    }
}