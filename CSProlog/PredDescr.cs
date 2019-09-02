#define enableSpying
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
using System.Collections.Generic;

namespace Prolog
{
    [Flags]
    public enum SpyPort : short
    {
        None = 0,
        Call = 1,
        Exit = 2,
        Fail = 4,
        Redo = 8,
        Full = 15
    }

    public enum CachePort
    {
        Exit,
        Fail
    }

    public partial class PrologEngine
    {
        public partial class PredicateDescr
        {
            private const short ARG0COUNT_MIN = 8;

            /* First-argument indexing
      
               A dictionary is maintained of clauses that have a nonvar first argument.
               Each predicate has its own dictionary. The functors of these first arguments are
               distinct (that is, in the dictionary). For predicate clauses that have identical
               first argument functors, only the first of these clauses is included in the
               dictionary. The clauses are traversed in the order in which they were read in.
      
               If a clause with a *variable* first argument is encountered, it is added
               to the dictionary as well, but no more entries wil be added after that.
               It will serve as a catch-all for instantiated arguments that do not match with
               any of the other dictionary entries.
      
               First-argument indexing is applied when a goal is about to be resolved. If the
               first argument of the goal is a bound term, it will be looked up in the
               dictionary. If it is found, the first clause of the defining predicate (cf.
               ExecuteGoalList()) is set to the dictionary entry. If it is not found, and the
               dictionary has a nonvar-entry, then the first clause is set to that
               entry. The (nonvar) goal fails if it is not found in the dictionary.
      
               First-argument indexing is not used upon backtracking. Choicepoints are simply
               the next clauses after the current one.
      
               Notice that this implementation of first-argument indexing is of limited use if
               there are many similar first arguments that are scattered throughout the
               predicate clauses, especially when backtracking takes place.
               A more sopisticated scheme could be envisaged where all similar fa-clauses are
               chained together. Now, for optimal use the user has to take care that all similar
               fa-clauses are grouped together (which seems advisable anyway).
            */

            private const bool VARARG = true;
            private Dictionary<object, ClauseNode> arg0Index; // for first argument indexing
            protected int arity;
            private ClauseNode clauseListEnd; // keeps track of the position of the last clause
            protected string functor;

            private ClauseNode
                lastCachedClause; // if the predicate is cacheable, each 'calculated' answer is inserted after this node

            public PredicateDescr(string module, string definitionFile, string functor, int arity,
                ClauseNode clauseList)
            {
                Module = module;
                DefinitionFile = definitionFile == null ? "predefined or asserted predicate" : definitionFile;
                this.functor = functor;
                this.arity = arity;
#if enableSpying
                SpyPort = SpyPort.None;
#endif
                ClauseList = clauseListEnd = clauseList;
                lastCachedClause = null; // no cached clauses (yet)
            }

            public string Functor => functor;
            public int Arity => arity;
            public string Name => functor + '/' + arity;
            public string Key => arity + functor;
            public ClauseNode ClauseList { get; private set; }

            public bool IsDiscontiguous { get; set; } = false;

            public bool IsCacheable { get; set; } = false;

            public bool HasCachedValues => lastCachedClause != null;
            public int ProfileCount { get; set; }

            public string Module { get; set; }

            public string DefinitionFile { get; }


            public bool IsFirstArgIndexed => arg0Index != null;

            public void IncProfileCount()
            {
                ProfileCount++;
            }


            public void SetClauseListHead(ClauseNode c)
            {
                ClauseList = clauseListEnd = c;

                while (clauseListEnd.NextClause != null) clauseListEnd = clauseListEnd.NextClause;

                DestroyFirstArgIndex();
            }


            public void AdjustClauseListEnd() // forward clauseListEnd to the last clause
            {
                if ((clauseListEnd = ClauseList) != null)
                    while (clauseListEnd.NextClause != null)
                        clauseListEnd = clauseListEnd.NextClause;
            }


            public void AppendToClauseList(ClauseNode c) // NextClause and ClauseListEnd are != null
            {
                clauseListEnd.NextClause = c;

                do
                {
                    clauseListEnd = clauseListEnd.NextClause;
                } while
                    (clauseListEnd.NextClause != null);

                DestroyFirstArgIndex();
            }

            public bool CreateFirstArgIndex()
            {
                return CreateFirstArgIndex(false); // false: do not create if it already exists
            }


            public bool CreateFirstArgIndex(bool force) // Create the index if the predicate qualifies
            {
                if (arg0Index != null && !force) return false; // index already exists

                // Check each nextClause whether with the addition of this nextClause the predicate
                // still qualifies for first argument indexing.
                // Indexing y/n must be (re)determined after a file consult or an assert.

                var c = ClauseList;
                short arg0Count = 0;

                while (c != null)
                {
                    if (c.Head.Arity != 0) // no first arg
                    {
                        arg0Count++; // Indexing not worthwile if only a few number of clauses

                        if (c.Head.Arg(0).IsVar) break;
                    }

                    c = c.NextClause;
                }

                if (arg0Count < ARG0COUNT_MIN) return false;

                // second pass: build the index

                arg0Index = new Dictionary<object, ClauseNode>();
                c = ClauseList;

                while (c != null)
                {
                    string s;

                    var t = c.Head.Arg(0);

                    if (t.IsVar)
                    {
                        arg0Index[key: VARARG] = c; // stop immediately after having included the first variable

                        break;
                    }

                    if (!arg0Index.ContainsKey(s = t.FunctorToString)) arg0Index[key: s] = c;

                    c = c.NextClause;
                }

                if (arg0Index.Count == 1) // e.g. c(a(1)), c(a(2)), c(a(3)), ...
                {
                    arg0Index = null;

                    return false;
                }

                return true;
            }

            public bool IsFirstArgMarked(ClauseNode c)
            {
                if (arg0Index == null) return false;

                var t = c.Term.Arg(0);
                ClauseNode result;

                if (t.IsVar)
                    arg0Index.TryGetValue(key: VARARG, value: out result);
                else
                    arg0Index.TryGetValue(key: t.FunctorToString, value: out result);

                return result == c;
            }


            public ClauseNode FirstArgNonvarClause(string arg)
            {
                ClauseNode result;
                arg0Index.TryGetValue(key: arg, value: out result);

                return result;
            }


            public ClauseNode FirstArgVarClause()
            {
                ClauseNode result;
                arg0Index.TryGetValue(key: VARARG, value: out result);

                return result;
            }


            public void DestroyFirstArgIndex()
            {
                arg0Index = null;
            }

            //public void DumpClauseList()
            //{
            //  NextClause c = clauseList;

            //  while (c != null)
            //  {
            //    IO.WriteLine("DumpValues -- {0}", c);
            //    c = c.NextClause;
            //  }
            //}

            public override string ToString()
            {
                return string.Format("pd[{0}/{1} clauselist {2}]", arg0: functor, arg1: arity, arg2: ClauseList);
            }
#if enableSpying
            public bool Spied { get; private set; }

            public SpyPort SpyPort { get; private set; }
#endif

            // CACHEING CURRENTLY NOT USED

            #region Cacheing

            // Cache -- analogous to asserting a fact, but specifically for cacheing.
            // Cached terms are inserted at the very beginning of the predicate's clause
            // chain, in the order in which they were determined.
            public void Cache(BaseTerm cacheTerm, bool succeeds)
            {
                IO.WriteLine("Cacheing {0}{1}", cacheTerm, succeeds ? null : " :- !, fail");

                var newCachedClause = new CachedClauseNode(t: cacheTerm, null, succeeds: succeeds);

                if (lastCachedClause == null) // about to add the first cached term
                {
                    newCachedClause.NextClause = ClauseList;
                    ClauseList = newCachedClause;
                }
                else
                {
                    newCachedClause.NextClause = lastCachedClause.NextClause;
                    lastCachedClause.NextClause = newCachedClause;
                }

                lastCachedClause = newCachedClause;
            }


            public void Uncache()
            {
                if (HasCachedValues) // let clauseList start at the first non-cached clause again
                {
                    ClauseList = lastCachedClause.NextClause;
                    lastCachedClause = null;
                }
            }

            #endregion Cacheing


#if enableSpying
            public void SetSpy(bool enabled, string functor, int arity, SpyPort setPorts, bool warn)
            {
                var spySet = "[";

                if (enabled)
                {
                    Spied = true;
                    SpyPort = SpyPort.None;

                    foreach (SpyPort port in Enum.GetValues(typeof(SpyPort)))
                        if (setPorts > 0 && (setPorts | port) == setPorts)
                        {
                            SpyPort |= port;

                            if (port != SpyPort.Full)
                                spySet += port + (port == SpyPort.Redo ? "]" : ",");
                        }

                    if (setPorts != SpyPort.None)
                        IO.Message("Spying {0} enabled for {1}/{2}", spySet, functor, arity);
                }
                else if (Spied) // nospy
                {
                    Spied = false;
                    IO.Message("Spying disabled for {0}/{1}", functor, arity);
                }
                else if (warn)
                {
                    IO.Message("There was no spypoint on {0}/{1}", functor, arity);
                }
            }


            public void ShowSpypoint()
            {
                if (SpyPort == SpyPort.None) return;

                var spySet = "[";

                foreach (SpyPort port in Enum.GetValues(typeof(SpyPort)))
                    if (port > 0 && port != SpyPort.Full && (SpyPort | port) == SpyPort)
                        spySet += port + (port == SpyPort.Redo ? "]" : ",");

                IO.WriteLine("{0}/{1}: {2}", Functor, Arity, spySet);
            }
#endif
        }
    }
}