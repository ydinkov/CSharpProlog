//#define arg1index // if (un)defined, do the same in PredStorage.cs !!!
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
using System.Text;

namespace Prolog
{
    /*
      --------
      TermNode
      --------
  
      A TermNode serves two purposes:
      - to store the goals of a command
      - to store predicates (clauses)
  
      A goal is constructed as a simple chained term of TermNodes. This makes it easy
      (and also more efficient in terms of GC) to revert to a previous state upon backtracking
      (in contrast to i.e. an ArrayList).
  
      A predicate consists of one or more clauses. A clause consist of a head and optionally a
      body. A head is a term, the body is a sequence of terms. A predicate is stored as a chain
      of TermNodes, where each TermNode represents a clause. These TermNodes are linked via the
      nextClause field. In each nextClause/TermNode the clause head is stored in term, and the
      clause body (which may be null) in NextNode.
  
    */


    public partial class PrologEngine
    {
        #region TermNode

        public class TermNode
        {
            protected int level; // debugging and tracing (for indentation)
            protected ClauseNode nextClause; // next predicate clause (advanced upon backtracking)
            protected TermNode nextNode; // next node in the chain
            protected PredicateDescr predDescr; // points to the predicate definition for term
            protected BaseTerm term; // for a NextClause: predicate head

            public TermNode()
            {
            }


            public TermNode(BaseTerm term, PredicateDescr predDescr)
            {
                this.predDescr = predDescr;
                this.term = term;
            }


            public TermNode(BaseTerm term, PredicateDescr predDescr, int level)
            {
                this.term = term;
                this.predDescr = predDescr;
                this.level = level;
            }


            public TermNode(string tag) // builtin predicates
            {
                try
                {
                    BuiltinId = (BI) Enum.Parse(typeof(BI), value: tag, false);
                }
                catch
                {
                    IO.Error("Bootstrap.cs: unknown BI enum value '{0}'", tag);
                }
            }


            public TermNode(BaseTerm term, TermNode nextNode)
            {
                this.term = term;
                this.nextNode = nextNode;
            }

            public BaseTerm Head
            {
                get => term;
                set => term = value;
            }

            public ClauseNode NextClause
            {
                get => nextClause;
                set => nextClause = value;
            }


            // put the predicate definition (if found) into the TermNode if it is not already there
            public bool FindPredicateDefinition(PredicateTable predicateTable)
            {
                if (predDescr == null)
                    if ((predDescr = predicateTable[key: term.Key]) == null)
                        return false;

#if arg1index // first-argument indexing enabled
        BaseTerm arg;

        // caching would disturb the search process (since caching does not
        // cause the arg0Index to be rebuild, since this might be to costly)
        if (predDescr.IsFirstArgIndexed && !predDescr.HasCachedValues)
        {
          if ((arg = term.Arg (0)).IsVar)
            nextClause = predDescr.FirstArgVarClause ();
          else // not a variable
          {
            nextClause = predDescr.FirstArgNonvarClause (arg.FunctorToString);

            // check whether there is an indexed var clause
            if (nextClause == null)
              nextClause = predDescr.FirstArgVarClause ();

            // if the above failed, the entire predicate fails (no unification possible)
            if (nextClause == null)
              nextClause = ClauseNode.FAIL;
          }

          if (nextClause == null)
            nextClause = predDescr.ClauseList;
        }
        else // not indexed
#endif
                nextClause = predDescr.ClauseList;

                return true;
            }


            public void Append(BaseTerm t)
            {
                if (term == null) // empty term
                {
                    term = t;

                    return;
                }

                var tail = this;
                var next = nextNode;

                while (next != null)
                {
                    tail = next;
                    next = next.nextNode;
                }

                tail.nextNode = new TermNode(term: t, (PredicateDescr) null);
            }


            public TermNode Append(TermNode t)
            {
                var tail = this;
                var next = nextNode;

                while (next != null) // get the last TermNode
                {
                    tail = next;
                    next = next.nextNode;
                }

                tail.nextNode = t;

                return this;
            }


            public void Clear()
            {
                term = null;
                nextNode = null;
                nextClause = null;
                level = 0;
            }


            public BaseTerm TermSeq()
            {
                return NextNode == null
                    ? Term // last term of TermNode
                    : new OperatorTerm(od: CommaOpDescr, a0: Term, NextNode.TermSeq());
            }


            public override string ToString()
            {
                var sb = new StringBuilder();
                var first = true;
                var indent = 2;
                var tn = this;
                BaseTerm t;

                while (tn != null)
                {
                    if ((t = tn.term) is TryOpenTerm)
                    {
                        if (!first) sb.Append(',');

                        sb.AppendFormat("{0}{1}TRY{0}{1}(", arg0: Environment.NewLine, Spaces(2 * indent++));
                        first = true;
                    }
                    else if (t is CatchOpenTerm)
                    {
                        var co = (CatchOpenTerm) t;
                        var msgVar = co.MsgVar is AnonymousVariable ? null : co.MsgVar.Name;
                        var comma = co.ExceptionClass == null || msgVar == null ? null : ", ";

                        sb.AppendFormat("{0}{1}){0}{1}CATCH {2}{3}{4}{0}{1}(",
                            Environment.NewLine, Spaces(2 * (indent - 1)), co.ExceptionClass, comma, msgVar);
                        first = true;
                    }
                    else if (t == TC_CLOSE)
                    {
                        sb.AppendFormat("{0}{1})", arg0: Environment.NewLine, Spaces(2 * --indent));
                        first = false;
                    }
                    else
                    {
                        if (first) first = false;
                        else sb.Append(',');

                        sb.AppendFormat("{0}{1}{2}", arg0: Environment.NewLine, Spaces(2 * indent), arg2: t);
                    }

                    tn = tn.nextNode;
                }

                return sb.ToString();
            }

            #region public properties

            public int Level
            {
                get => level;
                set => level = value;
            }

            public BaseTerm Term => term;

            public TermNode NextNode
            {
                get => nextNode;
                set => nextNode = value;
            }

            public TermNode NextGoal
            {
                get => nextNode;
                set => nextNode = value;
            }

            public bool Spied => PredDescr == null ? false : PredDescr.Spied;
            public SpyPort SpyPort => PredDescr == null ? SpyPort.None : PredDescr.SpyPort;
            public BI BuiltinId { get; } = BI.none;

            public PredicateDescr PredDescr
            {
                get => predDescr;
                set => predDescr = value;
            }

            #endregion
        }

        #endregion TermNode

        #region SpyPoint

        //  pushed on the VarStack to detect failure, inserted in the saveGoal to detect Exit.
        private class SpyPoint : TermNode // TermNode only used as at-compatible vehicle for port and saveGoal
        {
            public SpyPoint(SpyPort p, TermNode g)
            {
                Port = p;
                SaveGoal = g;
                level = g.Level;
            }

            public SpyPort Port { get; private set; }

            public TermNode SaveGoal { get; }

            public void Kill()
            {
                Port = SpyPort.None;
            }

            public override string ToString()
            {
                return "[" + Port + "-spypoint] " + SaveGoal.Term + " ...";
            }
        }

        #endregion SpyPoint

        #region clause

        // the terms (connected by NextNode) of a single clause
        public class ClauseNode : TermNode
        {
            public static ClauseNode FAIL = new ClauseNode(t: BaseTerm.FAIL, null);

            public ClauseNode()
            {
            }

            public ClauseNode(BaseTerm t, TermNode body)
                : base(term: t, nextNode: body)
            {
            }


            public override string ToString()
            {
                var NL = Environment.NewLine;

                var sb = new StringBuilder(NL + term);

                var first = true;
                var tl = nextNode;

                if (tl == null) return sb.ToString() + '.' + NL;

                while (true)
                {
                    if (first) sb.Append(" :-");

                    sb.Append(NL + "  " + tl.Term);

                    if ((tl = tl.NextNode) == null)
                        return sb.ToString() + '.' + NL;
                    if (!first)
                        sb.AppendFormat(",");

                    first = false;
                }
            }
        }


        // CACHEING CURRENTLY NOT USED
        public class CachedClauseNode : ClauseNode
        {
            public CachedClauseNode(BaseTerm t, TermNode body, bool succeeds) : base(t: t, body: body)
            {
                Succeeds = succeeds;
            }

            // i.e. in doing so fib(1,9999) will effecively be cached as 'fib( 1, 9999) :- !, fail'
            // (the ! is necessary to prevent the resolution process from recalculating the same result
            // again).
            public bool Succeeds { get; }
        }

        #endregion clause
    }
}