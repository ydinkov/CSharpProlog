using System.Collections.Generic;
using System.Text;

namespace Prolog
{
    public partial class PrologEngine
    {
        public enum TT
        {
            In = 0,
            Pre = 1,
            Post = 2,
            BoS,
            Term,
            InPre,
            InPost,
            EoS,
            Zero
        } // token types

        private class TokenSeqToTerm
        {
            private static readonly OperandToken SeqStartToken;
            private static readonly OperandToken SeqEndToken;

            private static readonly TC[,] TokenCombi =
            {
                {TC.In_In, TC.In_Pr, TC.In_Po, TC.In_BoS, TC.In_Term, TC.In_InPr, TC.In_InPo, TC.In_EoS},
                {TC.Pr_In, TC.Pr_Pr, TC.Pr_Po, TC.Pr_BoS, TC.Pr_Term, TC.Pr_InPr, TC.Pr_InPo, TC.Pr_EoS},
                {TC.Po_In, TC.Po_Pr, TC.Po_Po, TC.Po_BoS, TC.Po_Term, TC.Po_InPr, TC.Po_InPo, TC.Po_EoS},
                {TC.BoS_In, TC.BoS_Pr, TC.BoS_Po, TC.BoS_BoS, TC.BoS_Term, TC.BoS_InPr, TC.BoS_InPo, TC.BoS_EoS},
                {
                    TC.Term_In, TC.Term_Pr, TC.Term_Po, TC.Term_BoS, TC.Term_Term, TC.Term_InPr, TC.Term_InPo,
                    TC.Term_EoS
                },
                {
                    TC.InPr_In, TC.InPr_Pr, TC.InPr_Po, TC.InPr_BoS, TC.InPr_Term, TC.InPr_InPr, TC.InPr_InPo,
                    TC.InPr_EoS
                },
                {
                    TC.InPo_In, TC.InPo_Pr, TC.InPo_Po, TC.InPo_BoS, TC.InPo_Term, TC.InPo_InPr, TC.InPo_InPo,
                    TC.InPo_EoS
                },
                {TC.EoS_In, TC.EoS_Pr, TC.EoS_Po, TC.EoS_BoS, TC.EoS_Term, TC.EoS_InPr, TC.EoS_InPo, TC.EoS_EoS}
            };

            private bool inOpAtBoS; // special case: infix operator (no pre- or post definition) at BoS,

            private readonly TokenStack IS;
            private readonly TokenStack OS;
            private readonly TokenStack PS;

            // ... only allowed if it is stand-alone or immediately followed by an argument list
            private BaseToken newToken;

            static TokenSeqToTerm()
            {
                SeqStartToken = new OperandToken(type: TT.BoS);
                SeqEndToken = new OperandToken(type: TT.EoS);
            }

            public TokenSeqToTerm(OperatorTable opTable)
            {
                IS = new TokenStack("IS");
                OS = new TokenStack("OS"); // operator stack
                PS = new TokenStack("PS");
                IS.Push(item: SeqStartToken);
                inOpAtBoS = false;
            }

            private BaseToken topToken => IS.Top;

            private OperatorToken topOperator => topToken is OperatorToken ? topToken as OperatorToken : null;

            private OperatorToken OSOperator => OS.Top as OperatorToken;


            public bool PrevTokenWasOperator => topToken == SeqStartToken || topToken is OperatorToken;


            public void AddFunctorTerm(string functor, bool spaceAfter, BaseTerm[] args)
            {
                if (args == null)
                {
                    Add(new AtomTerm(value: functor));
                }
                else
                {
                    // space between atom (non-operator) and left parenthesis not allowed
                    if (spaceAfter) IO.Error("No space allowed between '{0}' and '('", functor);

                    if (functor == PrologParser.DOT && args.Length == 2)
                        Add(new ListTerm(args[0], args[1]));
                    else
                        Add(new CompoundTerm(functor: functor, args: args));
                }
            }


            public void Add(BaseTerm term)
            {
                CheckTokenPair(newToken = new OperandToken(term: term));
                IS.Push(item: newToken);
            }

            public void Add(OpDescrTriplet triplet)
            {
                newToken = new OperatorToken(triplet: triplet, topOperator == null ? null : topOperator.od);
                CheckTokenPair(newToken: newToken);
                IS.Push(item: newToken);
            }


            public void AddArgs(BaseTerm[] args)
            {
                if (args.Length == 1) // i.e. only a single term between parentheses
                    Add(args[0]);
                else if (args.Length == 2)
                    Add(new OperatorTerm(od: CommaOpDescr, args[0], args[1])); // a list of terms between parentheses
                else
                    Add(new CompoundTerm(functor: CommaOpDescr.Name,
                        args: args)); // a list of terms between parentheses
            }

            public void AddOperatorFunctor(OpDescrTriplet triplet, BaseTerm[] args)
            {
                switch (args.Length)
                {
                    case 1:
                        if (triplet.HasPrefixDef)
                            Add(new OperatorTerm(triplet[role: TT.Pre], args[0]));
                        else if (triplet.HasPostfixDef)
                            Add(new OperatorTerm(triplet[role: TT.Post], args[0]));
                        else
                            Add(new CompoundTerm(functor: triplet.Name, args: args));
                        break;
                    case 2:
                        if (triplet.HasInfixDef)
                            Add(new OperatorTerm(triplet[role: TT.In], args[0], args[1]));
                        else
                            Add(new CompoundTerm(functor: triplet.Name, args: args));
                        break;
                    default:
                        Add(new CompoundTerm(functor: triplet.Name, args: args));
                        break;
                }
            }

            // check the incoming token against the previous one (on top of IS)
            private void CheckTokenPair(BaseToken newToken)
            {
                var topToken = IS.Top;
                var newOperator = newToken is OperatorToken ? (OperatorToken) newToken : null;
                var combi = TokenCombi[(int) topToken.Role, (int) newToken.Role];
                string msg;

                switch (combi)
                {
                    case TC.Term_Term:
                    case TC.Term_Pr:
                    case TC.In_In:
                    case TC.In_Po:
                    case TC.In_InPo:
                    case TC.Pr_In:
                    case TC.Pr_Po:
                    case TC.Pr_InPo:
                    case TC.Po_Term:
                    case TC.Po_Pr:
                        IO.Error("Syntax error -- {0} may not be followed by {1}", topToken, newToken);
                        break;

                    case TC.In_EoS:
                    case TC.Pr_EoS:
                        if (!ProcessIfStandAloneOperator())
                            IO.Error("Syntax error -- Unexpected end of term after {0}", topToken);
                        break;

                    case TC.BoS_In:
                        inOpAtBoS = true;
                        break;

                    case TC.Po_EoS:
                    case TC.BoS_Po:
                    case TC.BoS_InPo:
                        ProcessIfStandAloneOperator();
                        break;

                    case TC.In_Term:
                        if (inOpAtBoS)
                        {
                            IO.Error("Syntax error -- {0} may not be followed by {1}", topToken, newToken);
                            inOpAtBoS = false;
                        }

                        break;

                    case TC.BoS_Term:
                    case TC.BoS_Pr:
                    case TC.Term_In:
                    case TC.Term_Po:
                    case TC.Term_InPo:
                    case TC.Term_EoS:
                    case TC.Pr_Term:
                    case TC.Po_InPo:
                        break;

                    case TC.BoS_InPr:
                        newOperator.SetRole(role: TT.Pre);
                        break;
                    case TC.Term_InPr:
                        newOperator.SetRole(role: TT.In);
                        break;
                    case TC.In_Pr:
                        if (!topOperator.od.HasValidRightArg(that: newOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.Po_In:
                        if (!newOperator.od.HasValidLeftArg(that: topOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.Pr_Pr:
                        if (!topOperator.od.HasValidArg(that: newOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.Po_Po:
                        if (!newOperator.od.HasValidArg(that: topOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.In_InPr:
                        newOperator.SetRole(role: TT.Pre);
                        if (!topOperator.od.HasValidRightArg(that: newOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.Pr_InPr:
                        newOperator.SetRole(role: TT.Pre);
                        if (!topOperator.od.HasValidArg(that: newOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.Po_InPr:
                        newOperator.SetRole(role: TT.In);
                        if (!newOperator.od.HasValidLeftArg(that: topOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.InPo_Term:
                        topOperator.SetRole(role: TT.In);
                        if (!topOperator.od.HasValidLeftArg(that: topOperator.prevOd, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.InPo_Pr:
                        topOperator.SetRole(role: TT.In);
                        if (!topOperator.od.HasValidLeftArg(that: topOperator.prevOd, msg: out msg)) IO.Error(msg: msg);
                        if (!topOperator.od.HasValidRightArg(that: newOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.InPo_In:
                        topOperator.SetRole(role: TT.Post);
                        if (!topOperator.od.HasValidArg(that: topOperator.prevOd, msg: out msg)) IO.Error(msg: msg);
                        if (!newOperator.od.HasValidLeftArg(that: topOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.InPo_Po:
                        topOperator.SetRole(role: TT.Post);
                        if (!topOperator.od.HasValidArg(that: topOperator.prevOd, msg: out msg)) IO.Error(msg: msg);
                        if (!newOperator.od.HasValidArg(that: topOperator.od, msg: out msg)) IO.Error(msg: msg);
                        break;
                    case TC.InPo_InPr:
                        var topInOpValid =
                            topOperator.triplet[role: TT.In]
                                .HasValidRightArg(newOperator.triplet[role: TT.Pre], msg: out msg);
                        var newInOpValid =
                            newOperator.triplet[role: TT.In]
                                .HasValidLeftArg(topOperator.triplet[role: TT.Post], msg: out msg);
                        if (topInOpValid)
                        {
                            if (newInOpValid)
                            {
                                IO.Error("Ambiguous operator combination: '{0}' followed by '{1}'",
                                    topOperator.triplet, newOperator.triplet);
                            }
                            else
                            {
                                topOperator.SetRole(role: TT.In);
                                newOperator.SetRole(role: TT.Pre);
                            }
                        }
                        else
                        {
                            topOperator.SetRole(role: TT.Post);
                            newOperator.SetRole(role: TT.In);
                        }

                        break;
                    case TC.InPo_InPo:
                    case TC.InPo_EoS:
                        topOperator.SetRole(role: TT.Post);
                        if (!ProcessIfStandAloneOperator())
                            if (!topOperator.od.HasValidArg(that: topOperator.prevOd, msg: out msg))
                                IO.Error(msg: msg);
                        break;
                    default:
                        IO.Fatal("TokenCombi case '{0}' not covered", combi);
                        break;
                }
            }


            private bool ProcessIfStandAloneOperator()
            {
                var result = IS.Count == 2;

                if (result)
                    topOperator.SetRole(role: TT.Zero);

                return result;
            }


            public void ConstructPrefixTerm(out BaseTerm term)
            {
                CheckTokenPair(newToken: SeqEndToken); // force a check on the last token added
                //DumpIS ();
                term = null;
                InfixToPrefix();
                term = PrefixToTerm();
            }


            //void DumpIS ()
            //{
            //  StringBuilder sb = new StringBuilder ();
            //
            //  BaseToken [] bt = IS.ToArray<BaseToken> ();
            //
            //  for (int i = bt.Length-1; i >= 0; i--)
            //    sb.Append (bt [i].ToString ());
            //
            //  IO.WriteLine (1, "ConstructPrologTerm: {0}", sb.ToString ());
            //}

            /*
              InfixToPrefix
              =============
              The algorithm can be described as the classical shunt-yard problem.
              For each token in turn in the input infix expression (reading from right to left):
      
              1. If (the token on top of) IS is an operand or a stand-alone operator:
                 - Move it to PS.
              2. If IS is an infix operator:
                 - Move all operators with lower priority from OS to PS;
                 - While there are operators with equal priority left on OS:
                   - If IS is left-associative:
                     - If OS is right-associative: clash (yfx xfy)
                   - If IS is right-associative:
                     - If OS is left-associative: ambiguous (xfy yfx)
                     - If OS is right-associative: move OS to PS
                 - Move IS to OS.
              3. If IS is a prefix operator:
                 - Move all postfix operators to PS that are on OS and that have a precedence
                   which is lower than or equal to the precedence of the prefix operator.
                 - Move the prefix operator to PS.
              4. If IS is a postfix operator:
                 - Move it to OS.
      
              Finally:
      
              5. - Move all remaining operators from US to PS.
      
            */

            private void InfixToPrefix()
            {
                while (topToken != SeqStartToken)
                    if (topToken is OperandToken || topOperator.IsZerofix)
                    {
                        IS.MoveTopTo(targetStack: PS);
                    }
                    else // OperatorToken
                    {
                        // move all lower-precedence infixes from OS to PS (low prec = strong binding)
                        while (!OS.IsEmpty && OSOperator.Prec < topOperator.Prec)
                            OS.MoveTopTo(targetStack: PS);

                        if (topOperator.IsInfix)
                        {
                            // deal with equal precedences
                            if (!OS.IsEmpty && OSOperator.Prec == topOperator.Prec)
                            {
                                if (OSOperator.LeftRelOp == RelOp.LT) // (IS, OS) = (?fx, xf?)
                                {
                                    if (topOperator.RightRelOp == RelOp.LT) // IS top ?fx
                                        IO.Error("Operator clash: '{0}' with '{1}'",
                                            topOperator.od, OSOperator.od);
                                    else
                                        OS.MoveTopTo(targetStack: PS);
                                }
                                else // OSOperator.LeftRelOp == RelOp.LE
                                {
                                    if (topOperator.RightRelOp == RelOp.LE) // (IS, OS) = (?fy, yf?)
                                        IO.Error("Ambiguous operator combination: '{0}' and '{1}'",
                                            topOperator.od, OSOperator.od);
                                }
                            }

                            IS.MoveTopTo(targetStack: OS);
                        }
                        else if (topOperator.IsPrefix)
                        {
                            while (!OS.IsEmpty && OSOperator.Prec == topOperator.Prec) // De Bosschere p.772
                                OS.MoveTopTo(targetStack: PS);

                            // no need to move it to OS first, since any subsequent token will effectuate that anyway
                            IS.MoveTopTo(targetStack: PS);
                        }
                        else // postfix
                        {
                            IS.MoveTopTo(targetStack: OS);
                        }
                    }

                while (!OS.IsEmpty) OS.MoveTopTo(targetStack: PS);
            }


            private BaseTerm PrefixToTerm()
            {
                BaseTerm t0, t1;
                BaseToken token = null;

                try
                {
                    token = PS.Pop();
                }
                catch // should not occur -- please report if it does
                {
                    IO.Error("PrefixToTerm (): Unanticipated error in expression -- please report");
                }

                if (token is OperandToken)
                    return ((OperandToken) token).term;

                var oprToken = (OperatorToken) token;

                if (oprToken.IsZerofix) // operator as operand; no arguments
                    return new OperatorTerm(name: oprToken.triplet.Name);

                t0 = PrefixToTerm();

                if (oprToken.IsInfix)
                {
                    t1 = PrefixToTerm(); // get the second operand from the PS-stack

                    return new OperatorTerm(od: oprToken.od, a0: t0, a1: t1);
                }

                return new OperatorTerm(od: oprToken.od, a: t0);
            }


            public override string ToString()
            {
                var sb = new StringBuilder();

                foreach (var t in IS)
                    sb.AppendLine(t.ToString());

                return sb.ToString();
            }

            #region TokenStack

            private class TokenStack : Stack<BaseToken>
            {
                public TokenStack(string name)
                {
                    Name = name;
                }

                public string Name { get; }

                public bool IsEmpty => Count == 0;

                public BaseToken Top // no empty stack check!
                    =>
                        Peek();

                public void MoveTopTo(TokenStack targetStack)
                {
                    targetStack.Push(Pop());
                }
            }

            #endregion TokenStack

            /*
              The parser (defined in pl.grm, expanded by a preprocessor to pl.cs) splits the input
              in a sequence of tokens. These tokens are actually 'high level' tokens, because the
              parser already recursively reduces a number of syntactical constructs (functors with
              argument lists, terms in parentheses, ordinary lists and grammar lists in curly
              brackets, stand-alone operators) to single terms. This means that the parser can
              actually be regarded as a high level tokenizer, and that the token sequence it
              delivers only contains operators and single terms that have already been created in
              the recursion, and does (therefore) no longer contain parentheses, square brackets,
              and curly brackets. This significantly simplifies the subsequent analysis - in which
              syntax checking and operator overloading and precedence checking takes place.
              The thus-generated sequence of tokens conforms to the following grammar:
      
              (1) E -> F (zfz F)*         // zfz is an infix operator ...
              (2) F -> fz* E zf* | Term   // ... fz prefix, zf postfix
      
              Each token is analysed and compared with the previous token as soon as it has been
              parsed. Not only the order prescribed by the grammar is checked, but also which form
              of an overloaded operator must be chosen and whether consecutive operators have the
              appropriate associativity and precedence. It turns out that the lookahead of one
              symbol is enough to perform this process.
      
              Incoming tokens are pushed on a stack (Infix Stack, IS). There are eight types of
              token: Term, Infix (operator), Prefix, Postfix, Infix/Prefix, Infix/Postfix, and two
              pseudo-tokens: Beginning of Stream and End of Stream. This means that there are 64
              possibilities for the combination <stacktop token, new token>. A number of these
              combinations cannot occur in practice or denotes a syntax error. For a number of
              others, identical processing is required. The cases are dealt with in a large switch
              statement, in which the 64 cases have been written out and combined whenever possible.
      
              A slight complication occurs when an overloaded Infix/Postfix operator is added to the
              sequence. If the top token is a Term or Postfix operator, it cannot be decided which
              role (infix or postfix) must be chosen until the next token arrives. If the next token
              is a term, it must be an infix; if the next token is a postfix, it must be set to
              postfix. In doing so, however, the precedence and associativity must be compared with
              the precedence of the penultimate token (just below the top token).
              To make this slightly easier, a token variable ‘prevOd’ (Operator descriptor) has been 
              introduced, which contains the precedence of the previous token.
      
              The above strategy will guarantee that:
      
              1 the sequence of prefix operators preceding a term have non-increasing precedences;
              2 the sequence of postfix operators following a term have non-decreasing precedences;
              3 the infix operators connecting the factors F in grammar rule (1) above have
                precedence values and associativities that agree with the prefix / postfix operators
                following / preceding them.
      
              The only remaining problem is that sofar no check has been carried out on the
              relations between the infix operators themselves. E.g. in 'T1 inf1 T2 inf2 T3'
              operators inf1 and inf2 may have irreconcilable associativities when their
              precedences are equal. In that case, xfy yfx is ambiguous and ?fx xf? denotes an
              operator clash. These checks are performed in InfixToPrefix()
            */

            private enum TC
            {
                BoS_BoS,
                BoS_Term,
                BoS_In,
                BoS_Pr,
                BoS_Po,
                BoS_InPr,
                BoS_InPo,
                BoS_EoS,
                Term_BoS,
                Term_Term,
                Term_In,
                Term_Pr,
                Term_Po,
                Term_InPr,
                Term_InPo,
                Term_EoS,
                In_BoS,
                In_Term,
                In_In,
                In_Pr,
                In_Po,
                In_InPr,
                In_InPo,
                In_EoS,
                Pr_BoS,
                Pr_Term,
                Pr_In,
                Pr_Pr,
                Pr_Po,
                Pr_InPr,
                Pr_InPo,
                Pr_EoS,
                Po_BoS,
                Po_Term,
                Po_In,
                Po_Pr,
                Po_Po,
                Po_InPr,
                Po_InPo,
                Po_EoS,
                InPr_BoS,
                InPr_Term,
                InPr_In,
                InPr_Pr,
                InPr_Po,
                InPr_InPr,
                InPr_InPo,
                InPr_EoS,
                InPo_BoS,
                InPo_Term,
                InPo_In,
                InPo_Pr,
                InPo_Po,
                InPo_InPr,
                InPo_InPo,
                InPo_EoS,
                EoS_BoS,
                EoS_Term,
                EoS_In,
                EoS_Pr,
                EoS_Po,
                EoS_InPr,
                EoS_InPo,
                EoS_EoS
            }

            #region InputToken

            abstract class BaseToken
            {
                protected TT role; // identical to type, except for overloaded operators that are bound
                protected TT type;

                public BaseToken()
                {
                    group = AssocType.None;
                }

                public OperatorDescr prevOd { get; protected set; } // Previous operator
                public virtual int Prec => 0;

                public TT Role => role;
                protected AssocType group { get; }

                public override string ToString()
                {
                    return string.Format("{0}", group.ToString());
                }
            }


            private class OperandToken : BaseToken
            {
                public OperandToken(BaseTerm term)
                {
                    this.term = term;
                    type = TT.Term;
                    role = type;
                    prevOd = null;
                }

                public OperandToken(TT type) // for BoS and EoS
                {
                    this.type = type;
                    role = type;
                    prevOd = null;
                }

                public BaseTerm term { get; }


                public override string ToString()
                {
                    if (this == SeqStartToken) return "Start of term";

                    if (this == SeqEndToken) return "End of term";

                    return string.Format("{0}", arg0: term);
                }
            }


            private class OperatorToken : BaseToken
            {
                public readonly OpDescrTriplet triplet;

                public OperatorToken(OpDescrTriplet triplet, OperatorDescr prevOd)
                {
                    this.triplet = triplet;
                    this.prevOd = prevOd;

                    if (triplet.HasInfixDef)
                    {
                        if (triplet.HasPrefixDef)
                            type = TT.InPre;
                        else if (triplet.HasPostfixDef)
                            type = TT.InPost;
                        else
                            type = TT.In;
                    }
                    else if (triplet.HasPrefixDef)
                    {
                        type = TT.Pre;
                    }
                    else if (triplet.HasPostfixDef)
                    {
                        type = TT.Post;
                    }

                    role = type;
                }

                public OperatorDescr od => triplet[role: role];
                public override int Prec => od.Prec;
                public RelOp LeftRelOp => od.LeftRelOp;
                public RelOp RightRelOp => od.RightRelOp;
                public bool IsInfix => role != TT.Zero && od.IsInfix;
                public bool IsPrefix => role != TT.Zero && od.IsPrefix;
                public bool IsPostfix => role != TT.Zero && od.IsPostfix;
                public bool IsZerofix => role == TT.Zero;


                // indicate which role an overloaded operator must perform
                public void SetRole(TT role)
                {
                    this.role = role;
                }


                public override string ToString()
                {
                    return
                        $"{(role == TT.Zero ? string.Format("({0})", arg0: triplet.Name) : triplet[role: role].ToString())}";
                }
            }
            #endregion InputToken
        }
    }
}