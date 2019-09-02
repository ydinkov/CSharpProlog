/*-----------------------------------------------------------------------------------------

  C#Prolog -- Copyright (C) 2007-2015 John Pool -- j.pool@ision.nl

  This library is free software; you can redistribute it and/or modify it under the terms of
  the GNU Lesser General Public License as published by the Free Software Foundation; either 
  version 3.0 of the License, or any later version.

  This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
  See the GNU Lesser General Public License (http://www.gnu.org/licenses/lgpl-3.0.html), or 
  enter 'license' at the command prompt.

-------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Prolog
{
    public partial class PrologEngine
    {
        #region Try/Catch

        public class TryCatchTerm : AtomTerm
        {
            public TryCatchTerm(string a) : base(value: a)
            {
            }
        }

        private static readonly TryCatchTerm TC_CLOSE = new TryCatchTerm(")");

        public class TryOpenTerm : TryCatchTerm
        {
            public TryOpenTerm() : base("TRY")
            {
            }

            public int Id { get; set; }
        }

        public class CatchOpenTerm : TryCatchTerm
        {
            public CatchOpenTerm(string exceptionClass, BaseTerm msgVar, int seqNo)
                : base("CATCH")
            {
                ExceptionClass = exceptionClass;
                SeqNo = seqNo;
                MsgVar = (Variable) msgVar;
            }

            public CatchOpenTerm(int id, string exceptionClass, BaseTerm msgVar, int seqNo, int saveStackSize)
                : base("CATCH")
            {
                Id = id;
                ExceptionClass = exceptionClass;
                SeqNo = seqNo;
                SaveStackSize = saveStackSize;
                MsgVar = (Variable) msgVar;
            }

            public string ExceptionClass { get; }

            public int Id { get; set; }

            public int SeqNo // CATCH-clauses for one TRY are number from 0 onwards
            {
                get;
            }

            public int SaveStackSize { get; set; } // not used
            public Variable MsgVar { get; }
        }

        #endregion Try/Catch

        #region NewIsoOrCsStringTerm

        public BaseTerm NewIsoOrCsStringTerm(string s)
        {
            if (csharpStrings)
                return new StringTerm(value: s);
            return new ListTerm(charCodeString: s);
        }

        #endregion NewIsoOrCsStringTerm

        #region Variable

        public class Variable : BaseTerm
        {
            public BaseTerm newVar;
            protected BaseTerm uLink;
            protected int unifyCount; // used for tabling
            public int verNo;
            protected int visitNo; // for cycle detection only

            public Variable()
            {
                varNo = varNoMax++;
                verNo = 0;
                unifyCount = 0;
                termType = TermType.UnboundVar;
            }

            public Variable(int i)
            {
                varNo = i;
                verNo = 0;
                unifyCount = 0;
                termType = TermType.UnboundVar;
            }

            public BaseTerm ULink => uLink;

            protected int varNo
            {
                get => termId;
                set => termId = value;
            }

            public override string Name => "_" + varNo;
            public override int Arity => -1;
            public int UnifyCount => unifyCount;
            public int VarNo => varNo;

            public int VisitNo // for cycle detection only -- not yet operational
                =>
                    visitNo;

            public override bool IsUnified => uLink != null;

            public bool IsUnifiedWith(Variable v)
            {
                return ChainEnd() == v.ChainEnd();
            }

            public override BaseTerm ChainEnd()
            {
                return IsUnified ? uLink.ChainEnd() : this;
            }

            // - this is a non-unified var, or is is a var with a refUnifyCount > arg  -> return this
            // - return ChainEnd (refUnifyCount)
            public override BaseTerm ChainEnd(int refUnifyCount)
            {
                return uLink == null || unifyCount > refUnifyCount
                    ? this
                    : uLink.ChainEnd(unifyCount: refUnifyCount); // resolves to uLink for a nonvar
            }

            protected override int CompareValue(BaseTerm t)
            {
                return varNo.CompareTo(value: ((Variable) t).varNo);
            }

            public override bool Unify(BaseTerm t, VarStack varStack)
            {
                if (IsUnified) return ChainEnd().Unify(t: t, varStack: varStack);

                if (t.IsUnified) return Unify(t.ChainEnd(), varStack: varStack);

                NextUnifyCount();
                Bind(t: t);
                varStack.Push(this);

                return true;
            }


            public void Bind(BaseTerm t)
            {
                if (this == t) return; // cannot bind to self

                uLink = t;
                unifyCount = CurrUnifyCount;
            }


            public void Unbind()
            {
                uLink = null;
                unifyCount = 0;
            }


            public override string ToWriteString(int level)
            {
                if (uLink == null) return Name;

                return uLink.ToWriteString(level: level);
            }
        }

        // carries a variable's symbolic name as found in the source
        public class NamedVariable : Variable
        {
            protected string name;

            public NamedVariable(string name)
            {
                this.name = name;
                termType = TermType.NamedVar;
            }

            public NamedVariable()
            {
                termType = TermType.NamedVar;
            }

            public override string Name => name;

            protected override int CompareValue(BaseTerm t)
            {
                return name.CompareTo(strB: ((NamedVariable) t).name);
            }
        }

        // not really necessary, but it can be convenient to recognize one
        public class AnonymousVariable : Variable
        {
        }

        #endregion Variable

        #region AtomTerm

        public class AtomTerm : BaseTerm
        {
            public AtomTerm(object functor)
            {
                this.functor = functor;
                termType = TermType.Atom;
            }


            public AtomTerm(string value)
            {
                functor = value.Unescaped();
                termType = TermType.Atom;
            }


            public AtomTerm(char value)
            {
                functor = value.ToString().Unescaped();
                termType = TermType.Atom;
            }

            public override bool IsCallable => true;

            public override bool IsEvaluatable // engine, pi, i, today, ...
                =>
                    true;


            protected override int CompareValue(BaseTerm t)
            {
                return FunctorToString.CompareTo(strB: t.FunctorToString);
            }


            public override string ToWriteString(int level)
            {
                return FunctorToString == PrologParser.DOT ? "'.'" : FunctorToString;
            }
        }

        #endregion AtomTerm

        #region ClauseTerm

        public class ClauseTerm : BaseTerm
        {
            public ClauseTerm(ClauseNode c) // Create a BaseTerm from a NextClause (= Head + Body)
            {
                if (c.NextNode == null) // fact
                {
                    CopyValuesFrom(t: c.Head);
                }
                else
                {
                    functor = PrologParser.IMPLIES;
                    args = new BaseTerm [2];
                    args[0] = c.Head;
                    termType = TermType.Atom;
                    assocType = AssocType.xfx;
                    args[1] = c.NextNode.TermSeq();
                    precedence = 1200;
                }
            }
        }

        #endregion ClauseTerm

       

        #region WrapperTerm

        public class WrapperTerm : CompoundTerm
        {
            private readonly string wrapClose;
            private readonly string wrapOpen;

            public WrapperTerm(string wrapOpen, string wrapClose, BaseTerm[] a)
                : base((wrapOpen + ".." + wrapClose).ToAtom(), args: a)
            {
                this.wrapOpen = wrapOpen;
                this.wrapClose = wrapClose;
                termType = TermType.Compound;
            }

            public WrapperTerm(WrapperTerm that, BaseTerm[] a) // for Copy only
                : base((that.wrapOpen + ".." + that.wrapClose).ToAtom(), args: a)
            {
                wrapOpen = that.wrapOpen;
                wrapClose = that.wrapClose;
                termType = TermType.Compound;
            }

            private string wrapFunctor => (wrapOpen + ".." + wrapClose).ToAtom();

            public override string ToWriteString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var sb = new StringBuilder(wrapOpen + SpaceAtLevel(level: level));
                var first = true;

                for (var i = 0; i < arity; i++)
                {
                    if (first) first = false;
                    else sb.Append(CommaAtLevel(level: level));

                    sb.AppendPacked(Arg(pos: i).ToWriteString(level + 1), mustPack: Arg(pos: i).FunctorIsBinaryComma);
                }

                sb.Append(SpaceAtLevel(level: level) + wrapClose);

                return sb.ToString();
            }


            public override string ToDisplayString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var sb = new StringBuilder(value: wrapFunctor);

                var first = true;

                sb.Append("(");

                foreach (var a in Args)
                {
                    if (first) first = false;
                    else sb.Append(CommaAtLevel(level: level));

                    sb.Append(a.ToDisplayString(level + 1));
                }

                sb.Append(")");

                return sb.ToString();
            }

            public override void TreePrint(int level, PrologEngine e)
            {
                var margin = Spaces(2 * level);

                if (arity == 0)
                {
                    e.WriteLine("{0}{1}", margin, wrapFunctor);

                    return;
                }

                e.WriteLine("{0}{1}", margin, wrapOpen);

                foreach (var a in args)
                    a.TreePrint(level + 1, e: e);

                e.WriteLine("{0}{1}", margin, wrapClose);
            }
        }

        #endregion WrapperTerm

        #region CompoundTerm

        public class CompoundTerm : BaseTerm
        {
            public CompoundTerm(string functor, BaseTerm[] args)
            {
                this.functor = functor;
                this.args = args;
                termType = TermType.Compound;
            }


            public CompoundTerm(string functor, BaseTerm a)
            {
                this.functor = functor;
                args = new BaseTerm [1];
                args[0] = a;
                termType = TermType.Compound;
            }


            public CompoundTerm(string functor, BaseTerm a0, BaseTerm a1)
            {
                this.functor = functor;
                args = new BaseTerm [2];
                args[0] = a0;
                args[1] = a1;
                termType = TermType.Compound;
            }


            public CompoundTerm(object functor, BaseTerm[] args)
            {
                this.functor = functor;
                this.args = args;
                termType = TermType.Compound;
            }


            public CompoundTerm(string functor) // degenerated case (for EMPTYLIST and operator)
            {
                this.functor = functor;
                termType = TermType.Atom;
            }


            public CompoundTerm()
            {
            }


            public CompoundTerm(CompoundTerm that)
            {
                functor = that.functor;
                args = that.args;
            }

            public override bool IsCallable => true;
            public override bool IsEvaluatable => true;


            protected override int CompareValue(BaseTerm t)
            {
                var result = Arity.CompareTo(value: t.Arity); // same FunctorString: lowest arity first

                if (result != 0) return result; // different arities

                if (Arity == 0) return FunctorToString.CompareTo(strB: t.FunctorToString);

                for (var i = 0; i < Arity; i++)
                    if ((result = Arg(pos: i).CompareTo(t.Arg(pos: i))) != 0)
                        return result;

                return 0;
            }


            public override string ToWriteString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var sb = new StringBuilder();

                if (FunctorToString == PrologParser.COMMA && arity == 2)
                {
                    sb = new StringBuilder("(" + Arg(0).ToWriteString(level: level) + CommaAtLevel(level: level)
                                           + Arg(1).ToWriteString(level: level) + ")");
                }
                else if (this == NULLCURL)
                {
                    return PrologParser.CURL;
                }
                else if (FunctorToString == PrologParser.CURL)
                {
                    sb.Append("{");
                    var first = true;

                    foreach (var arg in Args)
                    {
                        if (first) first = false;
                        else sb.Append(CommaAtLevel(level: level));

                        sb.Append(arg.ToWriteString(level + 1).Packed(mustPack: arg.FunctorIsBinaryComma));
                    }

                    sb.Append("}");
                }
                else
                {
                    sb.AppendPossiblySpaced(FunctorIsBinaryComma ? "','" : FunctorToString);
                    sb.Append("(");
                    var first = true;

                    for (var i = 0; i < arity; i++)
                    {
                        if (first) first = false;
                        else sb.Append(CommaAtLevel(level: level));

                        sb.AppendPacked(Arg(pos: i).ToWriteString(level + 1),
                            mustPack: Arg(pos: i).FunctorIsBinaryComma);
                    }

                    sb.Append(")");
                }

                return sb.ToString();
            }


            public override string ToDisplayString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var functor = FunctorToString == PrologParser.CURL ? "'{{}}'" : FunctorToString;

                var sb = new StringBuilder(FunctorIsBinaryComma ? "','" : functor);
                var first = true;

                sb.Append("(");

                foreach (var a in Args)
                {
                    if (first) first = false;
                    else sb.Append(CommaAtLevel(level: level));

                    sb.Append(a.ToDisplayString(level + 1));
                }

                sb.Append(")");

                return sb.ToString();
            }
        }

        #endregion CompoundTerm

        #region OperatorTerm

        public class OperatorTerm : CompoundTerm
        {
            private readonly AssocType assoc;

            // ConfigSettings.VerbatimStringsAllowed
            public OperatorDescr od;

            public OperatorTerm(OperatorTable opTable, string name, BaseTerm a0, BaseTerm a1)
                : base(functor: name, a0: a0, a1: a1)
            {
                if (!opTable.IsBinaryOperator(name: name, od: out od))
                    IO.Fatal("OperatorTerm/4: not a binary operator: '{0}'", name);

                assoc = od.Assoc;
                precedence = (short) od.Prec;
            }


            public OperatorTerm(OperatorDescr od, BaseTerm a0, BaseTerm a1)
                : base(functor: od.Name, a0: a0, a1: a1)
            {
                this.od = od;
                assoc = od.Assoc;
                precedence = (short) od.Prec;
            }


            public OperatorTerm(OperatorDescr od, BaseTerm a)
                : base(functor: od.Name, a: a)
            {
                this.od = od;
                assoc = od.Assoc;
                precedence = (short) od.Prec;
            }


            public OperatorTerm(OperatorDescr od, BaseTerm[] a)
                : base(functor: od.Name, args: a)
            {
                this.od = od;
                assoc = od.Assoc;
                precedence = (short) od.Prec;
            }


            public OperatorTerm(string name) // stand-alone operator used as term
                : base(functor: name)
            {
                od = null;
                assoc = AssocType.None;
                precedence = 1001;
            }


            public override bool HasUnaryOperator()
            {
                return od.IsPostfix || od.IsPrefix;
            }


            public override bool HasBinaryOperator()
            {
                return od.IsInfix;
            }


            public override bool HasUnaryOperator(params string[] names)
            {
                if (!(od.IsPrefix || od.IsPostfix)) return false;

                foreach (var name in names)
                    if (od.Name == name)
                        return true;

                return false;
            }


            public override bool HasBinaryOperator(params string[] names)
            {
                if (!od.IsInfix) return false;

                foreach (var name in names)
                    if (od.Name == name)
                        return true;

                return false;
            }


            public override string ToWriteString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var sb = new StringBuilder();
                bool mustPack;

                if (arity == 2)
                {
                    mustPack = precedence < Arg(0).Precedence ||
                               precedence == Arg(0).Precedence && (assoc == AssocType.xfx || assoc == AssocType.xfy);
                    sb.AppendPacked(Arg(0).ToWriteString(level + 1), mustPack: mustPack);

                    sb.AppendPossiblySpaced(s: FunctorToString);

                    mustPack =
                        precedence < Arg(1).Precedence ||
                        precedence == Arg(1).Precedence && (assoc == AssocType.xfx || assoc == AssocType.yfx);
                    sb.AppendPacked(Arg(1).ToWriteString(level + 1), mustPack: mustPack);

                    return sb.ToString();
                }

                if (arity == 1)
                {
                    switch (assoc)
                    {
                        case AssocType.fx:
                            sb.Append(value: FunctorToString);
                            sb.AppendPacked(Arg(0).ToWriteString(level + 1), precedence <= Arg(0).Precedence);
                            break;
                        case AssocType.fy:
                            sb.Append(value: FunctorToString);
                            sb.AppendPacked(Arg(0).ToWriteString(level + 1), precedence < Arg(0).Precedence);
                            break;
                        case AssocType.xf:
                            sb.AppendPacked(Arg(0).ToWriteString(level + 1), precedence <= Arg(0).Precedence);
                            sb.AppendPossiblySpaced(s: FunctorToString);
                            break;
                        case AssocType.yf:
                            sb.AppendPacked(Arg(0).ToWriteString(level + 1), precedence < Arg(0).Precedence);
                            sb.AppendPossiblySpaced(s: FunctorToString);
                            break;
                    }

                    return sb.ToString();
                }

                return FunctorToString;
            }


            public override string ToDisplayString(int level)
            {
                if (MaxWriteDepthExceeded(level: level)) return "...";

                var sb = new StringBuilder(FunctorIsBinaryComma ? "','" : FunctorToString);

                if (Arity > 0)
                {
                    var first = true;

                    sb.Append("(");

                    foreach (var a in Args)
                    {
                        if (first) first = false;
                        else sb.Append(CommaAtLevel(level: level));

                        sb.Append(a.ToDisplayString(level + 1));
                    }

                    sb.Append(")");
                }

                return sb.ToString();
            }
        }

        #endregion OperatorTerm

        #region ValueTerm

        public class ValueTerm : BaseTerm // a BaseTerm that can be expression-evaluated by is/2.
        {
            public override bool IsEvaluatable => true;
        }

        #region StringTerm

        public class StringTerm : ValueTerm
        {
            public StringTerm(string value)
            {
                functor = value;
                Value = value;
                termType = TermType.String;
            }

            public StringTerm(char value)
            {
                functor = value.ToString();
                termType = TermType.String;
            }

            public StringTerm()
            {
                functor = string.Empty;
                termType = TermType.String;
            }

            public string Value { get; set; }


            public override string ToWriteString(int level)
            {
                return '"' + FunctorToString.Replace(@"\", @"\\").Replace(@"""", @"\""") + '"';
            }
        }

        #endregion

        #region NumericalTerm

        public class NumericalTerm : ValueTerm
        {
            public NumericalTerm()
            {
                termType = TermType.Number;
            }
        }

        #endregion NumericalTerm

        #region DecimalTerm

        public class DecimalTerm : ValueTerm
        {
            private const double EPS = 1.0e-6; // arbitrary, cosmetic
            public static DecimalTerm ZERO;
            public static DecimalTerm ONE;
            public static DecimalTerm MINUS_ONE;

            static DecimalTerm()
            {
                ZERO = new DecimalTerm(0);
                ONE = new DecimalTerm(1);
                MINUS_ONE = new DecimalTerm(-1);
            }

            public DecimalTerm() // required for ComplexTerm
            {
                termType = TermType.Number;
            }

            public DecimalTerm(decimal value)
            {
                Value = value;
                functor = value;
                termType = TermType.Number;
            }

            public DecimalTerm(int value)
            {
                functor = Value = value;
                termType = TermType.Number;
            }

            public DecimalTerm(double value)
            {
                functor = Value = (decimal) value;
                termType = TermType.Number;
            }

            public DecimalTerm(long value)
            {
                functor = Value = value;
                termType = TermType.Number;
            }

            public decimal Value { get; }

            public double ValueD => (double) Value;
            public override string FunctorToString => Value.ToString(provider: CIC);


            public override bool Unify(BaseTerm t, VarStack varStack)
            {
                if (t is Variable) return t.Unify(this, varStack: varStack);

                NextUnifyCount();

                if (t is DecimalTerm)
                    return Value == ((DecimalTerm) t).Value;

                if (t is ComplexTerm)
                    return Math.Abs(value: ((ComplexTerm) t).Im) < EPS &&
                           Math.Abs(ValueD - ((ComplexTerm) t).Re) < EPS;

                return false;
            }


            protected override int CompareValue(BaseTerm t)
            {
                return To<decimal>().CompareTo(t.To<decimal>());
            }


            // sum
            public DecimalTerm Add(DecimalTerm d)
            {
                return new DecimalTerm(Value + Value);
            }

            public ComplexTerm Add(ComplexTerm c)
            {
                return new ComplexTerm(ValueD + c.Re, im: c.Im);
            }

            // difference
            public DecimalTerm Subtract(DecimalTerm d)
            {
                return new DecimalTerm(ValueD - d.ValueD);
            }

            public ComplexTerm Subtract(ComplexTerm c)
            {
                return new ComplexTerm(ValueD - c.Re, im: -c.Im);
            }

            // product
            public DecimalTerm Multiply(DecimalTerm d)
            {
                return new DecimalTerm(Value * d.Value);
            }

            public ComplexTerm Multiply(ComplexTerm c)
            {
                return new ComplexTerm(ValueD * c.Re, ValueD * c.Im);
            }

            // quotient
            public DecimalTerm Divide(DecimalTerm d)
            {
                if (d.Value == 0)
                    IO.Error("Division by zero not allowed");

                return new DecimalTerm(Value / d.Value);
            }

            public virtual ComplexTerm Divide(ComplexTerm c)
            {
                if (c.Re == 0 && c.Im == 0)
                    IO.Error("Division by zero complex number not allowed");

                var denominator = c.Re * c.Re + c.Im * c.Im;
                var newRe = ValueD * c.Re / denominator;
                var newIm = -ValueD * c.Im / denominator;

                return new ComplexTerm(re: newRe, im: newIm);
            }


            public virtual DecimalTerm Exp()
            {
                return new DecimalTerm(Math.Exp(d: ValueD));
            }


            public override string ToWriteString(int level)
            {
                if (Value == Math.Truncate(d: Value)) return Value.ToString();

                return Value.ToString(Math.Abs(value: Value) < (decimal) EPS ? "e" : "0.######", provider: CIC);
            }
        }

        #endregion

        #region DateTimeTerm

        public class DateTimeTerm : ValueTerm
        {
            public DateTimeTerm(DateTime value)
            {
                functor = value;
                termType = TermType.DateTime;
            }

            protected override int CompareValue(BaseTerm t)
            {
                return To<DateTime>().CompareTo(t.To<DateTime>());
            }

            public override string ToWriteString(int level)
            {
                return "'" + ((DateTime) functor).ToString(format: ConfigSettings.DefaultDateTimeFormat) + "'";
            }
        }

        #endregion

        #region TimeSpanTerm

        public class TimeSpanTerm : ValueTerm
        {
            public TimeSpanTerm(TimeSpan value)
            {
                functor = value;
                termType = TermType.TimeSpan;
            }

            protected override int CompareValue(BaseTerm t)
            {
                return To<TimeSpan>().CompareTo(t.To<TimeSpan>());
            }

            public override string ToWriteString(int level)
            {
                return "'" + (TimeSpan) functor + "'";
            }
        }

        #endregion

        #region BoolTerm

        public class BoolTerm : ValueTerm
        {
            //static byte orderPosition = 7;

            public BoolTerm(bool value)
            {
                functor = value;
                termType = TermType.Bool;
            }

            protected override int CompareValue(BaseTerm t)
            {
                return To<bool>().CompareTo(t.To<bool>());
            }

            public override string ToWriteString(int level)
            {
                return (bool) functor ? "true" : "false";
            }
        }

        #endregion

        #region FileTerm

        public class FileTerm : BaseTerm
        {
            // in order to be able to close all open streams after command termination:
            public static AtomTerm END_OF_FILE;
            protected PrologEngine engine;
            protected string fileName;

            static FileTerm()
            {
                END_OF_FILE = new AtomTerm("end_of_file");
            }

            public virtual bool IsOpen => false;

            public virtual void Close()
            {
            }
        }


        public class FileReaderTerm : FileTerm
        {
            private FileStream fs;

            private PrologParser p;
            private TextReader tr;

            public FileReaderTerm(PrologEngine engine, string fileName)
            {
                this.engine = engine;
                functor = this.fileName = fileName;
                termType = TermType.FileReader;
            }

            public FileReaderTerm(PrologEngine engine, TextReader tr)
            {
                this.engine = engine;
                functor = fileName = "<standard input>";
                this.tr = tr;
                termType = TermType.FileReader;
            }

            public override bool IsOpen => tr != null;
            public bool Eof => tr == null || tr.Peek() == -1;

            public void Open()
            {
                try
                {
                    if (tr == null)
                    {
                        fs = new FileStream(path: fileName, mode: FileMode.Open, access: FileAccess.Read,
                            share: FileShare.Read);
                        tr = new StreamReader(stream: fs);
                    }

                    p = new PrologParser(engine: engine);
                    p.SetInputStream(tr: tr);
                    p.InitParse();
                }
                catch (Exception e)
                {
                    engine.Throw(exceptionClass: IOException,
                        "Error while opening file '{0}' for input.\r\nMessage was:\r\n{1}",
                        fileName, e.Message);
                }
            }


            public int ReadChar() // returns -1 at end of file
            {
                return p.ReadChar();
            }


            public string ReadLine() // returns null at end of file
            {
                return p.ReadLine();
            }


            public BaseTerm ReadTerm()
            {
                var result = p.ParseTerm();

                return result == null ? END_OF_FILE : result;
            }


            public override void Close()
            {
                if (p != null) p.ExitParse();

                if (tr != null)
                    tr.Dispose();
            }
        }


        public class FileWriterTerm : FileTerm
        {
            private FileStream fs;
            private TextWriter tw;

            public FileWriterTerm(PrologEngine engine, string fileName)
            {
                this.engine = engine;
                functor = this.fileName = fileName;
                termType = TermType.FileWriter;
            }

            public FileWriterTerm(TextWriter tw)
            {
                functor = fileName = "<standard output>";
                this.tw = tw;
                termType = TermType.FileWriter;
            }

            public override bool IsOpen => tw != null;

            public void Open()
            {
                try
                {
                    if (tw == null)
                    {
                        fs = new FileStream(path: fileName, mode: FileMode.Create, access: FileAccess.Write);
                        tw = new StreamWriter(stream: fs);
                    }
                }
                catch (Exception e)
                {
                    engine.Throw(exceptionClass: IOException,
                        "Error while opening file '{0}' for output.\r\nMessage was:\r\n{1}",
                        fileName, e.Message);
                }
            }


            public void WriteTerm(BaseTerm t)
            {
                tw.WriteLine("{0}.", arg0: t);
            }


            public void Write(string s)
            {
                tw.Write(value: s);
            }


            public void Write(string s, params object[] args)
            {
                tw.Write(format: s, arg: args);
            }


            public void NewLine()
            {
                tw.Write(value: Environment.NewLine);
            }


            public override void Close()
            {
                if (tw != null)
                    tw.Dispose();
            }
        }

        #endregion FileTerm

        #endregion ValueTerm

        #region CollectionTerm

        public class CollectionTerm : AtomTerm // for creating collections of terms (e.g. setof)
        {
            private readonly BaseTermSet set;

            public CollectionTerm(DupMode dupMode)
                : base("<term collection>")
            {
                set = new BaseTermSet(dm: dupMode);
            }

            public int Count => set.Count;
            public override bool IsCallable => false;

            public void Add(BaseTerm t)
            {
                set.Add(item: t);
            }

            public void Insert(BaseTerm t)
            {
                set.Insert(termToInsert: t);
            }

            public ListTerm ToList()
            {
                return set.ToList();
            }
        }

        #endregion CollectionTerm

        #region BaseTermListTerm

        public class BaseTermListTerm<T> : AtomTerm
        {
            public BaseTermListTerm()
                : base("<term list>")
            {
                List = new List<T>();
            }

            public BaseTermListTerm(List<T> list)
                : base("<term collection>")
            {
                List = list;
            }

            public List<T> List { get; }

            public int Count => List.Count;
            public T this[int n] => List[index: n];
            public override bool IsCallable => false;
        }

        #endregion BaseTermListTerm

        #region Cut

        public class Cut : BaseTerm
        {
            public Cut(int stackSize)
            {
                functor = PrologParser.CUT;
                TermId = stackSize;
            }

            public override bool IsCallable => false;
            public override bool IsEvaluatable => false;

            public override string ToWriteString(int level)
            {
                return PrologParser.CUT;
            }
        }

        #endregion Cut

        #region ListTerm

        public class ListTerm : CompoundTerm
        {
            protected ListTerm EmptyList = EMPTYLIST;
            protected bool isAltList = false;
            protected string leftBracket = "[";
            protected string rightBracket = "]";

            public ListTerm()
                : base("[]")
            {
            }

            public ListTerm(BaseTerm t)
                : base(functor: PrologParser.DOT, t.ChainEnd(), a1: EMPTYLIST)
            {
            }

            public ListTerm(BaseTerm t0, BaseTerm t1)
                : base(functor: PrologParser.DOT, t0.ChainEnd(), t1.ChainEnd())
            {
            }

            // for ListPattern; *not* intended for creating a list from an array, use ListFromArray
            public ListTerm(BaseTerm[] a)
                : base(functor: PrologParser.DOT, args: a)
            {
            }

            public ListTerm(string charCodeString)
                : base(functor: PrologParser.DOT)
            {
                if (charCodeString.Length == 0)
                {
                    functor = "[]";

                    return;
                }

                CharCodeString = charCodeString;
                args = new BaseTerm [2];

                args[0] = new DecimalTerm((decimal) charCodeString[0]);
                args[1] = new ListTerm(charCodeString.Substring(1));
            }

            public override bool IsEvaluatable // evaluate all members
                =>
                    true;

            public string LeftBracket => leftBracket;
            public string RightBracket => rightBracket;
            public string CharCodeString { get; }

            public bool IsEvaluated { get; set; } = false;

            //public override bool IsCallable { get { return false; } }
            private int properLength // only defined for proper lists
            {
                get
                {
                    var t = ChainEnd();
                    var len = 0;

                    while (t.Arity == 2 && t is ListTerm)
                    {
                        t = t.Arg(1);
                        len++;
                    }

                    return t.IsEmptyList ? len : -1;
                }
            }

            public int ProperLength => properLength;

            public override bool IsListNode
            {
                get
                {
                    var t = ChainEnd();
                    return t is ListTerm && t.Arity == 2;
                }
            }

            public override bool IsProperOrPartialList
            {
                get
                {
                    var t = ChainEnd();

                    while (t.Arity == 2 && t is ListTerm) t = t.Arg(1);

                    return t.IsEmptyList || t is Variable;
                }
            }


            public override bool IsProperList // e.g. [foo] (= [foo|[]])
            {
                get
                {
                    var t = ChainEnd();

                    while (t.Arity == 2 && t is ListTerm) t = t.Arg(1);

                    return t.IsEmptyList;
                }
            }


            public override bool IsPartialList // e.g. [foo|Atom]
            {
                get
                {
                    var t = ChainEnd();

                    while (t.Arity == 2 && t is ListTerm) t = t.Arg(1);

                    return t is Variable;
                }
            }


            public override bool IsPseudoList // e.g.: [foo|baz]
            {
                get
                {
                    var t = ChainEnd();

                    while (t.Arity == 2 && t is ListTerm) t = t.Arg(1);

                    return !(t.IsEmptyList || t is Variable);
                }
            }


            public static ListTerm ListFromArray(BaseTerm[] ta, BaseTerm afterBar)
            {
                ListTerm result = null;

                for (var i = ta.Length - 1; i >= 0; i--)
                    result = new ListTerm(ta[i], result == null ? afterBar : result);

                return result;
            }


            public List<BaseTerm> ToList()
            {
                var result = new List<BaseTerm>();

                foreach (BaseTerm t in this)
                    result.Add(item: t);

                return result;
            }


            public BaseTerm[] ToTermArray()
            {
                var length = 0;

                foreach (BaseTerm t in this) length++;

                var result = new BaseTerm [length];

                length = 0;

                foreach (BaseTerm t in this) result[length++] = t;

                return result;
            }


            public static ListTerm ListFromArray(BaseTerm[] ta)
            {
                return ListFromArray(ta: ta, afterBar: EMPTYLIST);
            }


            public IEnumerator GetEnumerator()
            {
                var t = ChainEnd();

                while (t.Arity == 2)
                {
                    yield return t.Arg(0);

                    t = t.Arg(1);
                }
            }


            public virtual ListTerm Reverse()
            {
                var result = EmptyList;

                foreach (BaseTerm t in this) result = new ListTerm(t0: t, t1: result);

                return result;
            }


            public BaseTerm Append(BaseTerm list) // append t to 'this'
            {
                if (IsEmptyList) return list; // not necessarily a ListTerm

                if (list.IsEmptyList) return this;

                BaseTerm t0, t1;
                t1 = t0 = this;

                // find rightmost '.'-term and replace its right arg by t
                while (t1.Arity == 2)
                {
                    t0 = t1;
                    t1 = t1.Arg(1);
                }

                ((ListTerm) t0.ChainEnd()).SetArg(1, t: list);

                return this;
            }


            public BaseTerm AppendElement(BaseTerm last) // append last to 'this'
            {
                return Append(new ListTerm(t: last));
            }


            public virtual ListTerm FlattenList()
            {
                var a = FlattenListEx(functor: functor); // only sublists with the same functor

                var result = EmptyList;

                for (var i = a.Count - 1; i >= 0; i--)
                    result = new ListTerm(a[index: i], t1: result); // [a0, a0, ...]

                return result;
            }


            protected List<BaseTerm> FlattenListEx(object functor)
            {
                BaseTerm t = this;
                BaseTerm t0;
                var result = new List<BaseTerm>();

                while (t.IsListNode)
                {
                    if ((t0 = t.Arg(0)).IsProperOrPartialList && ((ListTerm) t0).functor.Equals(obj: functor))
                        result.AddRange(((ListTerm) t0).FlattenListEx(functor: functor));
                    else
                        result.Add(item: t0);

                    t = t.Arg(1);
                }

                if (t.IsVar) result.Add(item: t); // open tail, i.e. [1|M]

                return result;
            }

            // Intersection and Union: nice excercise for later.
            // Should also cope with partial and pseudo lists (cf. SWI-Prolog)
            public ListTerm Intersection(ListTerm that)
            {
                var result = EmptyList;

                foreach (ListTerm t0 in this)
                foreach (ListTerm t1 in that)
                {
                }

                return result;
            }


            public ListTerm Union(ListTerm that)
            {
                var result = EmptyList;

                return result;
            }


            public bool ContainsAtom(string atom)
            {
                var t = ChainEnd();

                while (t.Arity == 2)
                {
                    if (t.Arg(0).FunctorToString == atom) return true;

                    t = t.Arg(1);
                }

                return false;
            }


            public override string ToWriteString(int level)
            {
                // insert an extra space in case of non-standard list brackets
                var altListSpace = isAltList ? " " : null;

                if (IsEmptyList)
                    return leftBracket + altListSpace + rightBracket;

                if (MaxWriteDepthExceeded(level: level)) return "[...]";

                var sb = new StringBuilder(leftBracket + altListSpace);
                var t = ChainEnd();

                var first = true;

                while (t.IsListNode)
                {
                    if (first) first = false;
                    else sb.Append(CommaAtLevel(level: level));

                    sb.AppendPacked(t.Arg(0).ToWriteString(level + 1), mustPack: t.Arg(0).FunctorIsBinaryComma);
                    t = t.Arg(1);
                }

                if (!t.IsEmptyList)
                    sb.AppendFormat("|{0}", t.ToWriteString(level + 1).Packed(mustPack: t.FunctorIsBinaryComma));

                sb.Append(altListSpace + rightBracket);

                if (CharCodeString != null) // show string value in comment
                    sb.AppendFormat("  /*{0}*/", CharCodeString.Replace("*/", "\\x2A/"));

                return sb.ToString();
            }


            public override string ToDisplayString(int level)
            {
                if (IsEmptyList) return "leftBracket + rightBracket";

                var sb = new StringBuilder(".(");
                sb.Append(Arg(0).ToDisplayString(level: level));
                sb.Append(CommaAtLevel(level: level));
                sb.Append(Arg(1).ToDisplayString(level: level));
                sb.Append(")");

                return sb.ToString();
            }


            public override void TreePrint(int level, PrologEngine e)
            {
                var margin = Spaces(2 * level);

                if (IsEmptyList)
                {
                    e.WriteLine("{0}{1}", margin, EMPTYLIST);

                    return;
                }

                e.WriteLine("{0}{1}", margin, leftBracket);

                var t = ChainEnd();

                while (t.IsListNode)
                {
                    t.Arg(0).TreePrint(level + 1, e: e);
                    t = t.Arg(1);
                }

                e.WriteLine("{0}{1}", margin, rightBracket);
            }


            public string[] ToStringArray()
            {
                if (!IsProperList) return null;

                var result = new string [properLength];

                var t = ChainEnd();
                var i = 0;

                while (t.Arity == 2 && t is ListTerm)
                {
                    var s = t.Arg(0).ToString();
                    result[i++] = s.Dequoted("'").Dequoted("\"").Unescaped();
                    t = t.Arg(1);
                }

                return result;
            }

            /*
                From: http://www.sics.se/isl/quintus/html/quintus/lib-lis-prl.html
      
                What is a "Proper" List?
      
                Several of the predicate descriptions below indicate that a particular predicate
                only works when a particular argument "is a proper list".
      
                A proper list is either the Atom [] or else it is of the form [_|L] where L is a
                proper list.
      
                X is a partial list if and only if var(X) or X is [_|L] where L is a partial list.
                A term is a list if it is either a proper list or a partial list; that is, [_|foo]
                is not normally considered to be a list because its tail is neither a variable nor [].
      
                Note that the predicate list(X) defined in library(lists) really tests whether
                X is a proper list. The name is retained for compatibility with earlier releases
                of the library. Similarly, is_set(X) and is_ordset(X) test whether X is a proper
                list that possesses the additional properties defining sets and ordered sets.
      
                The point of the definition of a proper list is that a recursive procedure working
                its way down a proper list can be certain of terminating. Let us take the case of
                list/2 as an example. list(X, L) ought to be true when append(_, [X], L) is true.
                The obvious way of doing this is
      
                     list(List, [List]).
                     list(List, [_|End]) :-
                             list(List, End).
      
                If called with the second argument a proper list, this definition can be sure of
                terminating (though it will leave an extra choice point behind). However, if you Call
      
                     | ?- list(X, L), properLength(L, 0).
      
                where L is a variable, it will backtrack forever, trying ever longer lists.
                Therefore, users should be sure that only proper lists are used in those argument
                positions that require them.
      
               */
        }

        #endregion ListTerm

        #region DcgTerm

        public class DcgTerm : CompoundTerm
        {
            public DcgTerm(BaseTerm t, ref BaseTerm z)
                : base(functor: t.FunctorToString, new BaseTerm [t.Arity + 2])
            {
                for (var i = 0; i < t.Arity; i++) args[i] = t.Arg(pos: i);

                args[arity - 2] = z;
                args[arity - 1] = z = new Variable();
            }

            public DcgTerm(BaseTerm t) : base(functor: PrologParser.CURL, a0: t, a1: NULLCURL)
            {
            }

            public DcgTerm(BaseTerm t0, BaseTerm t1) : base(functor: PrologParser.CURL, a0: t0, a1: t1)
            {
            }

            public DcgTerm() : base(functor: PrologParser.CURL)
            {
            }

            public DcgTerm(object functor, BaseTerm[] args) : base(functor: functor, args: args)
            {
            }


            public override bool IsDcgList => ChainEnd() == NULLCURL || ChainEnd() is DcgTerm;

            public DcgTerm FlattenDcgList()
            {
                var a = FlattenDcgListEx();

                var result = NULLCURL; // {}

                for (var i = a.Count - 1; i >= 0; i--)
                    result = new DcgTerm(a[index: i], t1: result); // {a0, a0, ...}

                return result;
            }

            private List<BaseTerm> FlattenDcgListEx()
            {
                BaseTerm t = this;
                BaseTerm t0;
                var result = new List<BaseTerm>();

                while (t.FunctorToString == PrologParser.CURL && t.Arity == 2)
                {
                    if ((t0 = t.Arg(0)).IsDcgList)
                        result.AddRange(((DcgTerm) t0).FlattenDcgListEx());
                    else
                        result.Add(item: t0);

                    t = t.Arg(1);
                }

                if (t.IsVar) result.Add(item: t);

                return result;
            }


            public override string ToDisplayString(int level)
            {
                if (this == NULLCURL) return PrologParser.CURL;

                var sb = new StringBuilder("'{}'(");
                sb.Append(Arg(0).ToDisplayString(level: level));
                sb.Append(CommaAtLevel(level: level));
                sb.Append(Arg(1).ToDisplayString(level: level));
                sb.Append(")");

                return sb.ToString();
            }


            public override void TreePrint(int level, PrologEngine e)
            {
                var margin = Spaces(2 * level);

                e.WriteLine("{0}{1}", margin, '{');

                var t = ChainEnd();

                while (t.IsListNode)
                {
                    t.Arg(0).TreePrint(level + 1, e: e);
                    t = t.Arg(1);
                }

                e.WriteLine("{0}{1}", margin, '}');
            }
        }

        #endregion DcgTerm

        #region BinaryTerm // for accomodating binary data i.e. from SQL-queries

        public class BinaryTerm : BaseTerm
        {
            private readonly byte[] data;

            public BinaryTerm(byte[] data)
            {
                functor = "(binary data)";
                this.data = data;
                termType = TermType.Binary;
            }

            public override bool IsCallable => false;


            protected override int CompareValue(BaseTerm t)
            {
                return FunctorToString.CompareTo(strB: t.FunctorToString);
            }


            public override string ToWriteString(int level)
            {
                return string.Format("\"(byte[{0}] binary data)\"", arg0: data.Length);
            }
        }

        #endregion BinaryTerm

        #region UserClassTerm

        public class UserClassTerm<T> : BaseTerm
        {
            public UserClassTerm(T obj)
            {
                UserObject = obj;
            }

            public T UserObject { get; set; }
        }

        #endregion UserClassTerm
    }
}