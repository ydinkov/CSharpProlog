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
using System.Text;



namespace Prolog
{
    public partial class PrologEngine
    {
        #region AltListTerm

        public class AltListTerm : ListTerm
        {
            public AltListTerm(string leftBracket, string rightBracket)
            {
                isAltList = true;
                functor = leftBracket + ".." + rightBracket;
                this.leftBracket = leftBracket;
                this.rightBracket = rightBracket;
            }

            public AltListTerm(string leftBracket, string rightBracket, BaseTerm t0, BaseTerm t1)
                : base(t0.ChainEnd(), t1.ChainEnd())
            {
                isAltList = true;
                functor = leftBracket + ".." + rightBracket;
                this.leftBracket = leftBracket;
                this.rightBracket = rightBracket;
            }

            public AltListTerm(string leftBracket, string rightBracket, BaseTerm[] a)
                : base(a: a)
            {
                isAltList = true;
                functor = leftBracket + ".." + rightBracket;
                this.leftBracket = leftBracket;
                this.rightBracket = rightBracket;
            }

            public override string FunctorToString => functor.ToString().ToAtom();

            public static AltListTerm ListFromArray(
                string leftBracket, string rightBracket, BaseTerm[] ta, BaseTerm afterBar)
            {
                AltListTerm result = null;

                for (var i = ta.Length - 1; i >= 0; i--)
                    result = new AltListTerm(leftBracket: leftBracket, rightBracket: rightBracket, ta[i],
                        result == null ? afterBar : result);

                return result;
            }


            public override ListTerm Reverse()
            {
                var result = new AltListTerm(leftBracket: leftBracket, rightBracket: rightBracket);

                foreach (BaseTerm t in this)
                    result =
                        new AltListTerm(leftBracket: leftBracket, rightBracket: rightBracket, t0: t, t1: result);

                return result;
            }


            public override ListTerm FlattenList()
            {
                var a = FlattenListEx(functor: functor);

                var result = new AltListTerm(leftBracket: leftBracket, rightBracket: rightBracket);

                for (var i = a.Count - 1; i >= 0; i--)
                    result = new AltListTerm(leftBracket: leftBracket, rightBracket: rightBracket, a[index: i],
                        t1: result); // [a0, a0, ...]

                return result;
            }

            private List<BaseTerm> FlattenListEx()
            {
                throw new NotImplementedException();
            }
        }

        #endregion AltListTerm

        #region JsonTerm

        public class JsonTerm : ListTerm
        {
            private static JsonTerm EMPTY = new JsonTerm();
            private JsonTextBuffer jtb;
            private bool noQuotes;

            public JsonTerm()
            {
            }

            public JsonTerm(BaseTerm t0, BaseTerm t1) : base(t0: t0, t1: t1)
            {
            }

            public JsonTerm(ListTerm lt)
            {
                functor = lt.Functor;
                args = lt.Args;
            }

            public int MaxIndentLevel { get; set; } = 1;

            public static JsonTerm FromArray(BaseTerm[] ta)
            {
                var result = new JsonTerm();

                for (var i = ta.Length - 1; i >= 0; i--)
                    result = new JsonTerm(ta[i], t1: result);

                return result;
            }

            public string ToJsonString(int indentDelta, int maxIndentLevel, bool noCommas, bool noQuotes)
            {
                jtb = new JsonTextBuffer(indentDelta: indentDelta, maxIndentLevel: maxIndentLevel, noCommas: noCommas);
                this.noQuotes = noQuotes;
                DoJsonStruct(this);

                return jtb.ToString().Dequoted();
            }


            public override string ToWriteString(int maxIndentLevel)
            {
                return ToJsonString(0, 0, false, false);
            }


            private void DoJsonStruct(BaseTerm t) // object or array
            {
                if (t.FunctorToString == "array")
                    DoJsonArray(t.Arg(0));
                else if (t.IsProperList)
                    DoJsonObject(t: t);
                else
                    IO.Error("Not a well-formed JSON-term: {0}", t.ToString());
            }


            private void DoJsonObject(BaseTerm t) // object, array or literal
            {
                jtb.EmitOpenBracket('{');
                var first = true;

                // traverse list
                foreach (BaseTerm e in (ListTerm) t)
                {
                    jtb.AppendPossibleCommaAndNewLine(first: ref first,
                        MaxIndentLevel: MaxIndentLevel); // '{' <pair>+ '}'
                    DoJsonPair(t: e);
                }

                jtb.EmitCloseBracket('}');
            }


            private void DoJsonArray(BaseTerm t) // '[' <value>+ ']'
            {
                jtb.EmitOpenBracket('[');
                var first = true;

                // traverse list
                foreach (BaseTerm e in (ListTerm) t)
                {
                    jtb.AppendPossibleCommaAndNewLine(first: ref first,
                        MaxIndentLevel: MaxIndentLevel); // newline & indentation
                    DoJsonValue(t: e);
                }

                jtb.EmitCloseBracket(']');
            }


            private void DoJsonPair(BaseTerm t) // <string> ':' <value>
            {
                if (t.Arity < 2)
                    IO.Error("Not a well-formed JSON-term: {0}", t.ToString());

                DoJsonLiteral(t.Arg(0));
                jtb.EmitString(": ");
                var arg1 = t.Arg(1);

                if (arg1.Arity == 0)
                {
                    DoJsonLiteral(t: arg1);
                }
                else
                {
                    jtb.Newline();
                    DoJsonValue(t: arg1);
                }
            }


            private void DoJsonValue(BaseTerm t) // <object> | <array> | <literal>
            {
                if (t.Arity == 0)
                    DoJsonLiteral(t: t);
                else
                    DoJsonStruct(t: t);
            }


            private void DoJsonLiteral(BaseTerm t)
            {
                var s = t.FunctorToString; // not quoted

                if (noQuotes)
                {
                    jtb.EmitString(value: s);
                }
                else
                {
                    if (s.HasSignedRealNumberFormat() || s == "false" || s == "true" || s == "null")
                        jtb.EmitString(value: s);
                    else
                        jtb.EmitString(t.ToString());
                }
            }


            // JSON formatting

            #region JsonTextBuffer

            private class JsonTextBuffer
            {
                private readonly int indentDelta; // increment per indentation level
                private int level;
                private readonly int maxIndentLevel; // no indentation beyond this level (flat structures only)
                private readonly bool noCommas; // no commas between list items
                private readonly StringBuilder sb; // sealed, cannot inheritate from

                public JsonTextBuffer(int indentDelta, int maxIndentLevel, bool noCommas)
                {
                    sb = new StringBuilder();
                    level = 0;
                    this.indentDelta = indentDelta;
                    this.maxIndentLevel = maxIndentLevel;
                    this.noCommas = noCommas;
                }

                private string Indentation => new string(' ', level * indentDelta);


                public void EmitOpenBracket(char c)
                {
                    sb.Append(value: c);

                    if (level <= maxIndentLevel)
                    {
                        level++;
                        sb.Append(value: Indentation);
                    }
                }


                public void EmitCloseBracket(char c)
                {
                    if (level <= maxIndentLevel)
                    {
                        level--;
                        sb.AppendLine();
                        sb.Append(value: Indentation);
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    sb.Append(value: c);
                }


                public void Newline()
                {
                    if (level <= maxIndentLevel)
                    {
                        sb.AppendLine();
                        sb.Append(value: Indentation);
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                }


                public void AppendPossibleCommaAndNewLine(ref bool first, int MaxIndentLevel)
                {
                    if (first)
                        first = false;
                    else if (!noCommas)
                        sb.Append(',');

                    Newline();
                }


                public void EmitString(string value)
                {
                    sb.Append(value: value);
                }


                public override string ToString()
                {
                    return sb.ToString();
                }
            }

            #endregion JsonTextBuffer
        }

        #endregion JsonTerm

        #region IntRangeTerm // to accomodate integer ranges such as X = 1..100, R is X.

        public class IntRangeTerm : CompoundTerm
        {
            private readonly BaseTerm hiBound;
            private readonly IEnumerator iEnum;
            private readonly BaseTerm lowBound;

            public IntRangeTerm(BaseTerm lowBound, BaseTerm hiBound)
                : base("..", a0: lowBound, a1: hiBound)
            {
                this.lowBound = lowBound;
                this.hiBound = hiBound;
                iEnum = GetEnumerator();
            }

            public IntRangeTerm(IntRangeTerm that) // for copying only
                : base("..", a0: that.lowBound, a1: that.hiBound)
            {
                lowBound = that.lowBound;
                hiBound = that.hiBound;
                iEnum = GetEnumerator();
            }

            public override bool IsCallable => false;

            public ListTerm ToList()
            {
                var result = EMPTYLIST;

                var lo = lowBound.To<int>();
                var hi = hiBound.To<int>();

                for (var i = hi; i >= lo; i--)
                    result = new ListTerm(new DecimalTerm(value: i), t1: result);

                return result;
            }

            private IEnumerator GetEnumerator()
            {
                var lo = lowBound.To<int>();
                var hi = hiBound.To<int>();

                for (var i = lo; i <= hi; i++)
                    yield return new DecimalTerm(value: i);
            }

            public bool GetNextValue(out DecimalTerm dt)
            {
                dt = null;

                if (!iEnum.MoveNext()) return false;

                dt = (DecimalTerm) iEnum.Current;

                return true;
            }


            public override string ToWriteString(int level)
            {
                return string.Format("{0}..{1}", lowBound.To<int>(), hiBound.To<int>());
            }


            public override void TreePrint(int level, PrologEngine e)
            {
                e.WriteLine("{0}{1}..{2}", Spaces(2 * level), lowBound.To<int>(), hiBound.To<int>());
            }
        }

        #endregion IntRangeTerm

        #region DbConnectionTerm



        #endregion DbConnectionTerm

        // Auxiliary code for supporting the SQL predicates

        #region DbCommandSet


        #endregion DbCommandSet

        #region ComplexTerm // can be used with the is-operator

        public class ComplexTerm : NumericalTerm
        {
            private static readonly ComplexTerm ZERO = new ComplexTerm(0f, 0f);
            private static ComplexTerm ONE = new ComplexTerm(1f, 0f);
            private static readonly ComplexTerm TWO = new ComplexTerm(2f, 0f);
            private static ComplexTerm I = new ComplexTerm(0f, 1f);
            private static readonly ComplexTerm TWO_I = new ComplexTerm(0f, 2f);
            private static ComplexTerm MINUS_I = new ComplexTerm(0f, -1f);
            private static ComplexTerm MINUS_2I = new ComplexTerm(0f, -2f);
            private ComplexTerm enegx;
            private ComplexTerm eposx;

            public ComplexTerm(decimal re, decimal im)
            {
                Re = (double) re;
                Im = (double) im;
                functor = string.Format("{0}+{1}i", arg0: re, arg1: im);
                termType = TermType.Number;
            }

            public ComplexTerm(DecimalTerm d)
            {
                Re = d.ValueD;
                Im = 0;
                functor = string.Format("{0}+{1}i", arg0: Re, arg1: Im);
                termType = TermType.Number;
            }

            public ComplexTerm(double re, double im)
            {
                Re = re;
                Im = im;
                functor = string.Format("{0}+{1}i", arg0: re, arg1: im);
                termType = TermType.Number;
            }

            public double Re { get; }

            public double Im { get; }

            private double magnitude => Math.Sqrt(Re * Re + Im * Im);
            private double arg => Math.Atan2(y: Im, x: Re);
            public double Magnitude => magnitude;

            public double Phi // name Arg already in use
                =>
                    arg;

            public DecimalTerm ReTerm => new DecimalTerm(value: Re);
            public DecimalTerm ImTerm => new DecimalTerm(value: Im);


            public override int CompareTo(BaseTerm t)
            {
                ComplexTerm c;
                DecimalTerm d;

                if (t is ComplexTerm)
                {
                    c = (ComplexTerm) t;

                    if (Re == c.Re && Im == c.Im) return 0;

                    return magnitude.CompareTo(value: c.magnitude); // compare |this| and |c|
                }

                if (t is DecimalTerm)
                {
                    d = (DecimalTerm) t;

                    if (Im == 0) return Re.CompareTo(value: d.ValueD);

                    return magnitude.CompareTo(value: d.ValueD);
                }

                IO.Error("Relational operator cannot be applied to '{0}' and '{1}'", this, t);

                return 0;
            }


            public override bool Unify(BaseTerm t, VarStack varStack)
            {
                if (t is Variable) return t.Unify(this, varStack: varStack);

                NextUnifyCount();
                const double eps = 1.0e-6; // arbitrary, cosmetic

                if (t is DecimalTerm)
                    return Math.Abs(value: Im) < eps &&
                           Math.Abs(Re - ((DecimalTerm) t).ValueD) < eps;

                if (t is ComplexTerm)
                    return Math.Abs(Re - ((ComplexTerm) t).Re) < eps &&
                           Math.Abs(Im - ((ComplexTerm) t).Im) < eps;

                //if (t is ComplexTerm)
                //  return ( re == ((ComplexTerm)t).Re && im == ((ComplexTerm)t).Im );

                return false;
            }


            private ComplexTerm TimesI()
            {
                return new ComplexTerm(re: -Im, im: Re);
            }


            private ComplexTerm Plus1()
            {
                return new ComplexTerm(Re + 1, im: Im);
            }


            // sum
            public ComplexTerm Add(ComplexTerm c)
            {
                return new ComplexTerm(Re + c.Re, Im + c.Im);
            }

            public ComplexTerm Add(DecimalTerm d)
            {
                return new ComplexTerm(d.ValueD + Re, im: Im);
            }

            // difference
            public ComplexTerm Subtract(ComplexTerm c)
            {
                return new ComplexTerm(Re - c.Re, Im - c.Im);
            }


            public ComplexTerm Subtract(DecimalTerm d)
            {
                return new ComplexTerm(Re - d.ValueD, im: Im);
            }

            // product
            public ComplexTerm Multiply(ComplexTerm c)
            {
                return new ComplexTerm(Re * c.Re - Im * c.Im, Re * c.Im + c.Re * Im);
            }

            public ComplexTerm Multiply(DecimalTerm d)
            {
                return new ComplexTerm(Re * d.ValueD, Im * d.ValueD);
            }

            // quotient
            public ComplexTerm Divide(ComplexTerm c)
            {
                if (c.Re == 0 && c.Im == 0)
                    IO.Error("Division by zero complex number not allowed");

                var denominator = c.Re * c.Re + c.Im * c.Im;
                var newRe = (Re * c.Re + Im * c.Im) / denominator;
                var newIm = (Im * c.Re - Re * c.Im) / denominator;

                return new ComplexTerm(re: newRe, im: newIm);
            }


            public ComplexTerm Negative()
            {
                return new ComplexTerm(re: -Re, im: -Im);
            }


            public ComplexTerm Conjugate()
            {
                return new ComplexTerm(re: Re, im: -Im);
            }


            public ComplexTerm Divide(DecimalTerm d)
            {
                if (d.Value == 0)
                    IO.Error("Division by zero not allowed");

                return new ComplexTerm(Re / d.ValueD, Im / d.ValueD);
            }


            public ComplexTerm Log() // log|z| + i.arg(z), calculate the principal value
            {
                return new ComplexTerm(Math.Log(d: magnitude), im: arg);
            }


            public ComplexTerm Sqrt()
            {
                return new ComplexTerm(Math.Sqrt((magnitude + Re) / 2), Math.Sqrt((magnitude - Re) / 2));
            }


            public ComplexTerm Sqr()
            {
                return new ComplexTerm(Re * Re - Im * Im, 2 * Re * Im);
            }


            public ComplexTerm Exp() // engine^re * (cos (im) + i sin (im))
            {
                var exp = Math.Exp(d: Re);

                return new ComplexTerm(exp * Math.Cos(d: Im), exp * Math.Sin(a: Im));
            }


            public ComplexTerm Abs()
            {
                return new ComplexTerm(re: magnitude, 0);
            }


            public ComplexTerm Power(DecimalTerm d) // z^n = r^n * (cos (n*phi) + i*sin(n*phi))
            {
                if (d.IsInteger)
                {
                    var n = d.To<int>();

                    if (Re == 0 && Im == 0) // try Google 'what is 0 to the power of 0', etc.
                        return new ComplexTerm(n == 0 ? 1 : 0, 0f);

                    var rn = Math.Pow(x: magnitude, y: n);
                    var nArg = n * arg;

                    return new ComplexTerm(rn * Math.Cos(d: nArg), rn * Math.Sin(a: nArg));
                }

                return Log().Multiply(d: d).Exp(); // t^d = exp(log(t^d)) = exp(d*log(t))
            }

            // http://en.wikipedia.org/wiki/Exponentiation#Failure_of_power_and_logarithm_identities
            public ComplexTerm Power(ComplexTerm p) // also see IEEE 754-2008 floating point standard
            {
                if (Re == 0 && Im == 0)
                    return new ComplexTerm(p.Re == 0 && p.Im == 0 ? 1 : 0, 0f);

                return Log().Multiply(c: p).Exp(); // t^p = exp(log(t^p)) = exp(p*log(t))
            }


            public ComplexTerm Sin() // (exp(iz) - exp(-iz)) / 2i
            {
                eposx = TimesI().Exp();
                enegx = TimesI().Negative().Exp();

                return eposx.Subtract(c: enegx).Divide(c: TWO_I);
            }


            public ComplexTerm Cos() // (exp(iz) + exp(-iz)) / 2
            {
                eposx = TimesI().Exp();
                enegx = TimesI().Negative().Exp();

                return eposx.Add(c: enegx).Divide(c: TWO);
            }


            public ComplexTerm Tan() //
            {
                eposx = TimesI().Exp();
                enegx = TimesI().Negative().Exp();

                return eposx.Subtract(c: enegx).Divide(eposx.Add(c: enegx));
            }


            public ComplexTerm Sinh()
            {
                eposx = Exp(); // engine^z
                enegx = Negative().Exp(); // engine^(-z)

                return eposx.Subtract(c: enegx).Divide(c: TWO);
            }


            public ComplexTerm Cosh()
            {
                eposx = Exp(); // engine^z
                enegx = Negative().Exp(); // engine^(-z)

                return eposx.Add(c: enegx).Divide(c: TWO);
            }


            public ComplexTerm Tanh()
            {
                eposx = Exp(); // engine^z
                enegx = Negative().Exp(); // engine^(-z)

                return eposx.Subtract(c: enegx).Divide(eposx.Add(c: enegx));
            }


            public ComplexTerm Asin() // log(z + sqrt(1+z^2))
            {
                return Add(Sqr().Plus1().Sqrt()).Log();
            }


            public ComplexTerm Acos() // 2 * log( sqrt((z+1)/2) + sqrt((z-1)/2))
            {
                return ZERO;
            }


            public ComplexTerm Atan() // (log(1+z) - log(1-z)) / 2
            {
                var c0 = Plus1().Log();
                var c1 = Negative().Plus1().Log();

                return c0.Subtract(c: c1).Divide(c: TWO);
            }


            public override string ToWriteString(int level)
            {
                // rounding: arbitrary, cosmetic, in order to prevent answers like 0+i or 1+0i
                var re = Math.Round(value: Re, 6);
                var im = Math.Round(value: Im, 6);

                if (im == 0) return re == 0 ? "0" : re.ToString("0.######", provider: CIC);

                string ims = null;

                if (im == -1)
                    ims = "-";
                else if (im != 1)
                    ims = im.ToString("0.######", provider: CIC);

                if (re == 0)
                    return string.Format(provider: CIC, "{0}i", arg0: ims);
                return string.Format(provider: CIC, "{0:0.######}{1}{2}i", arg0: re, im < 0 ? null : "+", arg2: ims);
            }
        }

        #endregion ComplexTerm
    }
}