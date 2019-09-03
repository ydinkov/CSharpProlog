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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Prolog
{
    using ApplicationException = Exception;
    using Stack = Stack<object>;


    #region Exceptions

    internal enum PrologException
    {
        ioException
    }

    public class AbortQueryException : ApplicationException
    {
        public AbortQueryException() : base("\r\n  Execution terminated by user")
        {
        }
    }

    #endregion Exceptions

    #region Engine

    public partial class PrologEngine
    {
        private static readonly string IOException = "ioException";
        //static string XmlException = "xmlException";

        #region ChoicePoint

        public class ChoicePoint
        {
            protected bool active;
            protected TermNode goalListHead;
            protected ClauseNode nextClause; // next clause to be tried for goalListHead

            public ChoicePoint(TermNode goal, ClauseNode nextClause)
            {
                goalListHead = goal;
                this.nextClause = nextClause;
                active = true;
            }

            public TermNode GoalListHead => goalListHead;

            public ClauseNode NextClause
            {
                get => nextClause;
                set => nextClause = value;
            }

            public bool IsActive => active;

            public void Kill()
            {
                active = false;
            }

            public override string ToString()
            {
                return string.Format("choicepoint\r\ngoal {0}\r\nclause {1}\r\nactive {2}", arg0: goalListHead,
                    arg1: nextClause, arg2: active);
            }
        }

        #endregion ChoicePoint

        #region CacheCheckPoint

        // pushed on the VarStack for determining whether or not to store an answer in the cache
        private class CacheCheckPoint : TermNode
        {
            public CacheCheckPoint(CachePort port, TermNode saveGoal)
            {
                Port = port;
                SaveGoal = saveGoal;
                level = saveGoal.Level;
            }

            public CachePort Port { get; }

            public TermNode SaveGoal { get; }


            public override string ToString()
            {
                return "[" + Port + "-cachecheckpoint] " + SaveGoal.Term + " ...";
            }
        }

        #endregion CacheCheckPoint

        #region VarStack

        public class VarStack : Stack
        {
            private Stack swap;

            public VarStack()
            {
                swap = new Stack();
            }

            //public override void Push (object o)
            //{
            //  base.Push (o);

            //  if (o is Variable)
            //    IO.WriteLine ("Pushed var functor={0} value={1}", ((Variable)o).Functor, o);
            //  else
            //    IO.WriteLine ("Pushed {0}", o);
            //}

            //public override object Pop ()
            //{
            //  object o = base.Pop ();

            //  IO.WriteLine ("Popped {0}", o);

            //  return o;
            //}

            public void DisableChoices(int n)
            {
                var i = Count;

                foreach (var o in this) // works its way down from the top !!!
                {
                    if (i-- == n) return;

                    if (o is SpyPoint)
                        ((SpyPoint) o).Kill(); // do not retain (failure) spypoints
                    else if (o is ChoicePoint)
                        ((ChoicePoint) o).Kill();
                }
            }
        }

        #endregion VarStack

        #region VarValues

        public interface IVarValue
        {
            string Name { get; }
            ITermNode Value { get; }
            string DataType { get; }
        }

        public class VarValue : IVarValue
        {
            public string name;
            public BaseTerm value;


            public VarValue(string name, BaseTerm value)
            {
                this.name = name;
                this.value = value;
                IsSingleton = true;
            }

            public bool IsSingleton { get; set; }

            public string Name => name;
            public ITermNode Value => value;
            public string DataType => value.TermType.ToString().ToLower();

            public override string ToString()
            {
                if (!value.IsVar)
                {
                    var mustPack = value.Precedence >= 700;

                    return string.Format("{0} = {1}", name, value.ToString().Packed(mustPack: mustPack));
                }

                return null;
            }
        }

        public class VarValues : Dictionary<string, VarValue>
        {
            public BaseTerm GetValue(string name)
            {
                VarValue result;
                TryGetValue(key: name, value: out result); // result is null if value not found

                return result == null ? null : result.value;
            }
        }

        #endregion VarValues

        #region Solution

        public interface ISolution
        {
            IEnumerable<IVarValue> VarValuesIterator { get; }
            bool IsLast { get; }
            bool Solved { get; }
        }

        // contains the answer to a query or the response to a history command
        public class Solution : ISolution
        {
            private readonly PrologEngine engine;
            private string msg;
            private readonly VarValues variables;

            public Solution(PrologEngine engine)
            {
                this.engine = engine;
                variables = new VarValues();
                VarValuesIterator = GetEnumerator();
                Solved = true;
                msg = null;
            }

            public bool Solved { get; set; }

            public bool IsLast { get; set; }

            public IEnumerable<IVarValue> VarValuesIterator { get; }


            public IEnumerable<IVarValue> GetEnumerator()
            {
                foreach (IVarValue varValue in variables.Values)
                {
                    if (engine.Halted) yield break;

                    yield return varValue;
                }
            }


            public void Clear()
            {
                variables.Clear();
            }


            public void SetMessage(string msg)
            {
                this.msg = msg;
            }


            public void SetMessage(string msg, params object[] args)
            {
                SetMessage(string.Format(format: msg, args: args));
            }


            public void ResetMessage()
            {
                msg = null;
            }


            public void SetVar(string name, BaseTerm value)
            {
                variables[key: name] = new VarValue(name: name, value: value);
            }


            public void RegisterVarNonSingleton(string name)
            {
                variables[key: name].IsSingleton = false;
            }


            public void ReportSingletons(ClauseNode c, int lineNo, ref bool firstReport)
            {
                var singletons = new List<string>();

                foreach (var var in variables.Values)
                    if (var.IsSingleton)
                        singletons.Add(item: var.Name);

                if (singletons.Count == 0) return;

                if (firstReport)
                {
                    IO.WriteLine();
                    firstReport = false;
                }

                IO.Write("    Warning: '{0}' at line {1} has {2}singleton variable{3} [",
                    c.Head.Name, lineNo,
                    singletons.Count == 1 ? "a " : null,
                    singletons.Count == 1 ? null : "s");

                var first = true;

                foreach (var name in singletons)
                {
                    if (first) first = false;
                    else IO.Write(", ");

                    IO.Write( name);
                }

                IO.WriteLine("]");
            }


            public BaseTerm GetVar(string name)
            {
                return variables.GetValue(name: name);
            }


            public override string ToString()
            {
                if (engine.Halted) return null;

                if (msg != null) return msg;

                var totSecs = engine.ProcessorTime().TotalSeconds;

                var time = totSecs > 0.2 // arbitrary threshold, show 'interesting' values only
                    ? string.Format(" ({0:f3} sec)", arg0: totSecs)
                    : null;

                if (!Solved) return NO + time;

                var sb = new StringBuilder();
                string answer = null;
                BaseTerm term;

                foreach (var varValue in variables.Values)
                    if (!(term = varValue.value).IsVar && varValue.name[0] != '_')
                        sb.AppendFormat("\r\n {0}", arg0: varValue);

                return answer = (sb.Length == 0 ? YES : sb.ToString()) + time;
            }
        }

        #endregion Solution

        private static readonly string WELCOME =
            @"|
| Copyright (C) 2007-2014 John Pool
|
| C#Prolog comes with ABSOLUTELY NO WARRANTY. This is free software, licenced
| under the GNU General Public License, and you are welcome to redistribute it
| under certain conditions. Enter 'license.' at the command prompt for details.";

        private static readonly string VERSION = "4.1.0";
        private static readonly string RELEASE = PrologParser.VersionTimeStamp;
        public static string IntroText;

        /* The parser's terminalTable is associated with the engine, not with the parser.
         * For each consult (or read), a new instance of the parser is created. However,
         * when each parser would have a new terminalTable, any operator definitions from
         * previously consulted files would get lost (operators are symbols that must be
         * recognized by the parser and hence they are stored in the terminalTable)
         */
        private BaseParser<OpDescrTriplet>.BaseTrie terminalTable;
        private string query;
        private Solution solution;
        private static OperatorDescr CommaOpDescr;
        private static OpDescrTriplet CommaOpTriplet;
        private static OperatorDescr SemiOpDescr;
        private static OperatorDescr EqualOpDescr;
        private static OperatorDescr ColonOpDescr;
        private PredicateCallOptions predicateCallOptions;

        private OpenFiles openFiles;
        private const int INF = int.MaxValue;
        private VarStack varStack; // stack of variable bindings and choice points
        private GlobalTermsTable globalTermsTable;
        private Stack<int> catchIdStack;
        private int tryCatchId;

        public class OpenFiles : Dictionary<string, FileTerm>
        {
            public FileTerm GetFileReader(string fileName)
            {
                FileTerm ft;

                TryGetValue(fileName.ToLower(), value: out ft);

                return ft;
            }

            public FileTerm GetFileWriter(string fileName)
            {
                FileTerm ft;

                TryGetValue(fileName.ToLower(), value: out ft);

                return ft;
            }

            public void CloseAllOpenFiles()
            {
                foreach (var ft in Values)
                    ft.Close();

                Clear();
            }
        }

        private class GlobalTermsTable
        {
            private readonly Dictionary<string, int> counterTable;
            private readonly Dictionary<string, BaseTerm> globvarTable;

            public GlobalTermsTable()
            {
                counterTable = new Dictionary<string, int>();
                globvarTable = new Dictionary<string, BaseTerm>();
            }

            public void getctr(string a, out int value)
            {
                if (!counterTable.TryGetValue(key: a, value: out value))
                    IO.Error("Value of counter '{0}' is not set", a);
            }

            public void setctr(string a, int value)
            {
                counterTable[key: a] = value;
            }

            public void incctr(string a)
            {
                int value;

                if (counterTable.TryGetValue(key: a, value: out value))
                    counterTable[key: a] = value + 1;
                else
                    IO.Error("Value of counter '{0}' is not set", a);
            }

            public void decctr(string a)
            {
                int value;

                if (counterTable.TryGetValue(key: a, value: out value))
                    counterTable[key: a] = value - 1;
                else
                    IO.Error("Value of counter '{0}' is not set", a);
            }

            public void getvar(string name, out BaseTerm value)
            {
                if (!globvarTable.TryGetValue(key: name, value: out value))
                    IO.Error("Value of '{0}' is not set", name);
            }

            public void setvar(string name, BaseTerm value)
            {
                globvarTable[key: name] = value;
            }

            public bool varhasvalue(string name)
            {
                BaseTerm value;

                return globvarTable.TryGetValue(key: name, value: out value);
            }
        }

        private TermNode goalListHead;
        private readonly BasicIo io;
        private bool userInterrupted;
        private bool trace;
        private bool debug;
        private bool redo; // set by CanBacktrack if a choice point was found
        private bool qskip;
        private bool rushToEnd;
        private bool xmlTrace;
        private bool reporting; // debug (also set by 'trace') || xmlTrace
        private bool profiling;

        private ClauseNode retractClause;
        private int levelMin; // lowest recursion level while spying -- for determining left margin
        private int levelMax; // used while spying for determining end of skip
        private int prevLevel;
        private ChoicePoint currentCp;
        private object lastCp;
        private long startTime;
        private Stopwatch procTime;
        private readonly CommandHistory cmdBuf;
        private static int maxWriteDepth; // Set by maxwritedepth/1. Subterms beyond this depth are written as "..."
        private int queryTimeout; // maximum Number of milliseconds that a command may run -- 0 means unlimited

        private bool
            findFirstClause; // find the first clause of predicate that matches the current goal goal (-last head)

        private bool csharpStrings = ConfigSettings.CSharpStrings;
        private bool userSetShowStackTrace = ConfigSettings.OnErrorShowStackTrace; // default value

        private bool
            showSingletonWarnings =
                true; // want to be able to turn off singleton warnings at the file level (default to true and will need to add to reset code to make sure it isn't persisting across files)

        #region unique number generators

        private static void NextUnifyCount()
        {
            CurrUnifyCount++;
        }

        private static int CurrUnifyCount { get; set; }

        private static int UnifyDelta(int refUnifyCount)
        {
            return CurrUnifyCount - refUnifyCount;
        }

        #endregion

        public bool Error { get; private set; }

        public OperatorTable OpTable { get; private set; }

        public BracketTable WrapTable { get; private set; }

        public BracketTable AltListTable { get; private set; }

        public PredicateTable Ps { get; private set; }

        public string Query
        {
            get => query;
            set => query = value.Trim();
        }


        static PrologEngine()
        {
            IntroText = string.Format(
                "|\r\n| Welcome to C#Prolog MS-Windows version {0}, parser {1}\r\n{2}",
                arg0: VERSION, arg1: RELEASE, arg2: WELCOME);
            CurrUnifyCount = 0; // running total number of unifications
        }


        public PrologEngine()
            : this(new DosIO())
        {
        }

        public PrologEngine(BasicIo io)
            : this( io, true)
        {
        }

        public PrologEngine(bool persistentCommandHistory)
            : this(new DosIO(), persistentCommandHistory: persistentCommandHistory)
        {
        }

        public PrologEngine(BasicIo io, bool persistentCommandHistory)
        {
            IO.BasicIO = this.io = io;
            Reset();
            cmdBuf = new CommandHistory(enablePersistence: persistentCommandHistory);
            PostBootstrap();
        }

        public int CmdNo => cmdBuf.cmdNo;
        public bool Debugging => debug;
        public bool Halted { get; set; }

        private PrologParser parser;
        private static readonly string YES = "\r\n" + ConfigSettings.AnswerTrue;
        private static readonly string NO = "\r\n" + ConfigSettings.AnswerFalse;
        private int gensymInt;

        public static bool MaxWriteDepthExceeded(int level)
        {
            return maxWriteDepth != -1 && level > maxWriteDepth;
        }


        #region Engine initialization and finalization

        public void Reset()
        {
            Initialize();
            ReadBuiltinPredicates();
        }


        private void Initialize() // also called by ClearAll command
        {
            varStack = new VarStack();
            solution = new Solution(this);
            SolutionIterator = GetEnumerator();
            OpTable = new OperatorTable();
            WrapTable = new BracketTable();
            AltListTable = new BracketTable();
            globalTermsTable = new GlobalTermsTable();
            Ps = new PredicateTable(this);
            catchIdStack = new Stack<int>();
            openFiles = new OpenFiles();
            tryCatchId = 0;

            Error = false;
            Halted = false;
            trace = false;
            qskip = false;
            xmlTrace = false;
            startTime = -1;
            procTime = Stopwatch.StartNew();
            currentFileReader = null;
            currentFileWriter = null;
            maxWriteDepth = -1; // i.e. no max depth
            predicateCallOptions = new PredicateCallOptions();

            terminalTable = new BaseParser<OpDescrTriplet>.BaseTrie(PrologParser.terminalCount, true);
            PrologParser.FillTerminalTable(terminalTable: terminalTable);
            parser = new PrologParser(this); // now this.terminalTable is passed on as well
        }

        private void PostBootstrap()
        {
            if (!OpTable.IsBinaryOperator(name: PrologParser.EQ, od: out EqualOpDescr))
                IO.Error("No definition found for binary operator '{0}'", PrologParser.EQ);
            else if (!OpTable.IsBinaryOperator(name: PrologParser.COLON, od: out ColonOpDescr))
                IO.Error("No definition found for binary operator '{0}'", PrologParser.COLON);
        }


        private void ReadBuiltinPredicates()
        {
            Ps.Reset();
            parser.SetDollarAsPossibleUnquotedAtomChar(true);
            parser.StreamIn = Bootstrap.PredefinedPredicates;
            parser.SetDollarAsPossibleUnquotedAtomChar(false);
            Ps.Predefineds["0!"] = true;
            Ps.Predefineds["0true"] = true;
            Ps.Predefineds["0fail"] = true;
            Ps.Predefineds["2;"] = true;
            Ps.Predefineds["2,"] = true;
            retractClause = Ps[BaseTerm.MakeKey("retract", 1)].ClauseList;
            Ps.ResolveIndices();
        }

        #endregion Engine initialization and finalization

        #region Query execution preparation and finalization

        public IEnumerable<ISolution> SolutionIterator;

        public IEnumerable<ISolution> GetEnumerator()
        {
            try
            {
                if (PrepareSolutions(query: query))
                {
                    do
                    {
                        Execute(); // run the query

                        solution.IsLast = Halted || !FindChoicePoint();

                        yield return solution;
                    } while (!Halted && CanBacktrack(false));
                }
                else // history command
                {
                    solution.IsLast = true;

                    yield return solution;
                }
            }
            finally
            {
                PostQueryTidyUp(); // close all potentially open files and SQL connections
            }
        }


        public ISolution GetFirstSolution(string query)
        {
            Query = query;
            var solutions = SolutionIterator.GetEnumerator();
            solutions.MoveNext();

            return solutions.Current;
        }


        public bool PrepareSolutions(string query)
        {
            try
            {
                solution.ResetMessage();
                solution.Solved = true;
                varStack.Clear();
                catchIdStack.Clear();
                Ps.Uncacheall();
                tryCatchId = 0;
                findFirstClause = true;

                if (cmdBuf.CheckForHistoryCommands(ref query, this))
                    return false;

                userInterrupted = false;
                parser.StreamIn = query;
                rushToEnd = false;
                xmlTrace = false;
                levelMin = 0;
                levelMax = INF;
                prevLevel = -1;
                lastCp = null;
                gensymInt = 0;
                io.Reset(); // clear input character buffer
                goalListHead = parser.QueryNode;

                if (goalListHead == null) return false;
            }
            catch (UserException x)
            {
                Error = true;
                solution.SetMessage(msg: x.Message);
                solution.Solved = false;
            }
            catch (Exception x)
            {
                Error = true;
                solution.SetMessage("{0}{1}\r\n",
                    x.Message, userSetShowStackTrace ? Environment.NewLine + x.StackTrace : "");

                return false;
            }

            return true;
        }


        public void PostQueryTidyUp()
        {
            openFiles.CloseAllOpenFiles();
            currentFileReader = null;
            currentFileWriter = null;

        }

        #endregion Query execution preparation and finalization

        #region Goal last execution

        private void Execute()
        {
            ElapsedTime();
            ProcessorTime();

            try
            {
                solution.Solved = ExecuteGoalList();
            }
            catch (AbortQueryException x)
            {
                solution.SetMessage(msg: x.Message);
                solution.Solved = true;
            }
            catch (UserException x)
            {
                Error = true;
                solution.SetMessage(msg: x.Message);
                solution.Solved = false;
            }
            catch (Exception x)
            {
                Error = true;
                solution.SetMessage("{0}{1}\r\n",
                    x.Message, userSetShowStackTrace ? Environment.NewLine + x.StackTrace : "");
                solution.Solved = false;
            }
        }



        /*  Although in the code below a number of references is made to 'caching' (storing
            intermediate results of a calculation) this feature is currently not available.
            The reason is that it proved much more complicated than initially thought.
            I left the various fragments in the code, as I want to sort this out later more
            thoroughly.
    
            So technically spoken, the bool 'caching' will never have a true-value in the code
            below (nor anywhere else).
        */
        // The ExecuteGoalList() algorithm is the standard algorithm as for example
        // described in Ivan Bratko's "Prolog Programming for Artificial Intelligence",
        // 3rd edition p.45+
        private bool ExecuteGoalList()
        {
            // variables declaration used in goal loop to save stack space
            int stackSize;
            TermNode currClause;
            BaseTerm cleanClauseHead;
            BI builtinId;
            int level;
            BaseTerm t;
            TermNode saveGoal;
            TermNode p;
            TermNode pHead;
            TermNode pTail;
            TermNode tn0;
            TermNode tn1;
            var caching = false;
            redo = false; // set by CanBacktrack if a choice point was found

            while (goalListHead != null) // consume the last of goalNodes until it is exhausted
            {
                if (userInterrupted)
                    throw new AbortQueryException();

                if (goalListHead.Term is TryCatchTerm)
                {
                    if (goalListHead.Term is TryOpenTerm)
                    {
                        catchIdStack.Push(item: ((TryOpenTerm) goalListHead.Term)
                            .Id); // CATCH-id of corresponding CATCH-clause(s) now on top
                        goalListHead = goalListHead.NextGoal;

                        continue;
                    }

                    if (goalListHead.Term is CatchOpenTerm)
                    {
                        if (((CatchOpenTerm) goalListHead.Term).SeqNo == 0) // only once:
                            catchIdStack.Pop(); // CATCH-id of CATCH-clause enclosing this TRY/CATCH now on top

                        while (goalListHead.Term != TC_CLOSE)
                            goalListHead = goalListHead.NextGoal;

                        continue;
                    }

                    if (goalListHead.Term == TC_CLOSE)
                    {
                        goalListHead = goalListHead.NextGoal;

                        continue;
                    }
                }
                else if (goalListHead is CacheCheckPoint
                ) // check whether there was a successful exit of a cacheable predicate ...
                {
                    var ccp = (CacheCheckPoint) goalListHead;
                    saveGoal = ccp.SaveGoal;
                    saveGoal.PredDescr.Cache(saveGoal.Term.Copy(), true);
                    goalListHead = saveGoal.NextGoal;

                    continue;
                }

                if (goalListHead is SpyPoint)
                {
                    var sp = ((SpyPoint) goalListHead).SaveGoal;

                    // debugger resets saveGoal and returns true if user enters r(etry) or f(ail)
                    if (!Debugger( SpyPort.Exit, sp, null, false, 1))
                        goalListHead = sp.NextGoal;

                    continue;
                }

                stackSize = varStack.Count; // varStack reflects the current program state

                // FindPredicateDefinition tries to find in the program the predicate definition for the
                // functor+arity of goalNode.Term. This definition is stored in goalListHead.NextClause
                if (findFirstClause)
                {
                    if (!goalListHead.FindPredicateDefinition(predicateTable: Ps)) // undefined predicate
                    {
                        var goal = goalListHead.Head;

                        switch (Ps.ActionWhenUndefined( goal.FunctorToString, a: goal.Arity))
                        {
                            case UndefAction.Fail: // pretend the predicate exists, with 'fail' as first and only clause
                                goalListHead.PredDescr = Ps[key: BaseTerm.FAIL.Key];
                                goalListHead.NextClause = goalListHead.PredDescr.ClauseList;
                                break;
                            case UndefAction.Error:
                                return IO.Error("Undefined predicate: {0}", goal.Name);
                            default:
                                var pd = Ps.FindClosestMatch(predName: goal.Name);
                                var suggestion = pd == null
                                    ? null
                                    : string.Format(". Maybe '{0}' is what you mean?", arg0: pd.Name);
                                IO.Error("Undefined predicate: {0}{1}", goal.Name, suggestion);
                                break;
                        }
                    }

                    findFirstClause = false; // i.e. advance to the next clause upon backtracking (redoing)
                }

                if (profiling && goalListHead.PredDescr != null)
                {
                    goalListHead.PredDescr.IncProfileCount();
                    caching = goalListHead.PredDescr.IsCacheable;
                }

                currClause = goalListHead.NextClause; // the first or next clause of the predicate definition

                // in order to be able to retry goal etc.
                if (reporting) varStack.Push(new SpyPoint(p: SpyPort.Call, g: goalListHead));

                saveGoal = goalListHead; // remember the original saveGoal (which may be NextGoal-ed, see below)

                if (currClause.NextClause == null) // no redo possible => fail, make explicit when tracing
                {
                    if (reporting
                    ) // to be able to detect failure (i.e. when we have to pop beyond this entry) in CanBacktrack
                        varStack.Push(new SpyPoint(p: SpyPort.Fail, g: goalListHead));

                    if (caching)
                        varStack.Push(new CacheCheckPoint( CachePort.Fail, saveGoal: goalListHead));
                }
                else // currClause.NextClause will be tried upon backtracking
                {
                    varStack.Push(currentCp = new ChoicePoint(goal: goalListHead, nextClause: currClause.NextClause));
                }

                cleanClauseHead =
                    currClause.Head.Copy(); // instantiations must be retained for clause body -> create newVars
                level = goalListHead.Level;

                if (reporting &&
                    Debugger(redo ? SpyPort.Redo : SpyPort.Call, saveGoal, cleanClauseHead,
                        currClause.NextGoal == null, 2))
                    continue; // Debugger may return some previous version of saveGoal (retry- or fail-command)

                // UNIFICATION of the current goal and the (clause of the) predicate that matches it
                if (cleanClauseHead.Unify( goalListHead.Term, varStack: varStack))
                {
                    var currCachedClauseMustFail =
                        currClause is CachedClauseNode && !((CachedClauseNode) currClause).Succeeds;

                    currClause = currClause.NextNode; // body - if any - of the matching predicate definition clause

                    // FACT
                    if (currClause == null) // body is null, so matching was against a fact
                    {
                        if (reporting && Debugger( SpyPort.Exit, goalListHead, null, false, 3)) continue;

                        if (currCachedClauseMustFail) // act as if the clause has a (!, fail) body
                            InsertCutFail();
                        else
                            goalListHead = goalListHead.NextGoal;

                        findFirstClause = true;
                    }
                    // BUILT-IN
                    else if ((builtinId = currClause.BuiltinId) != BI.none)
                    {
                        if (builtinId == BI.call)
                        {
                            t = goalListHead.Head.Arg(0);

                            if (t.IsVar) return IO.Error("Unbound variable '{0}' in goal list", ((Variable) t).Name);

                            if (goalListHead.Head.Arity > 1) // implementation of SWI call/1..8
                            {
                                AddCallArgs(GoalListHead: goalListHead);
                                t = goalListHead.Head;
                            }

                            tn0 = t.ToGoalList(stackSize, goalListHead.Level + 1);

                            if (reporting)
                                tn0.Append(new SpyPoint(p: SpyPort.Exit, g: saveGoal));

                            goalListHead = goalListHead == null ? tn0 : tn0.Append( goalListHead.NextGoal);
                            findFirstClause = true;
                        }
                        else if (builtinId == BI.or)
                        {
                            if (reporting)
                            {
                                varStack.Pop();
                                varStack.Pop();
                            }

                            tn1 = goalListHead.Head.Arg(1).ToGoalList(stackSize: stackSize, level: goalListHead.Level);
                            varStack.Push(new ChoicePoint(goalListHead == null
                                ? tn1
                                : tn1.Append( goalListHead.NextGoal), null));

                            tn0 = goalListHead.Head.Arg(0).ToGoalList(stackSize: stackSize, level: goalListHead.Level);
                            goalListHead = goalListHead == null ? tn0 : tn0.Append( goalListHead.NextGoal);
                            findFirstClause = true;
                        }
                        else if (builtinId == BI.cut)
                        {
                            varStack.DisableChoices(n: goalListHead.Term.TermId);
                            goalListHead = goalListHead.NextGoal;
                            findFirstClause = true;
                        }
                        else if (builtinId == BI.fail)
                        {
                            if (!(redo = CanBacktrack())) return false;
                        }
                        else if (DoBuiltin(biId: builtinId, findFirstClause: out findFirstClause))
                        {
                            if (reporting && Debugger( SpyPort.Exit, saveGoal, null, false, 5))
                                findFirstClause = true;
                        }
                        else if (!(redo = CanBacktrack()))
                        {
                            return false;
                        }
                    }
                    // PREDICATE RULE
                    else // replace goal by body of matching clause of defining predicate
                    {
                        pHead = null;
                        pTail = null;
                        BaseTerm currTerm;

                        while (currClause != null)
                        {
                            currTerm = currClause.Term;

                            if (currTerm is TryOpenTerm)
                            {
                                ((TryOpenTerm) currTerm).Id = ++tryCatchId;
                                p = new TermNode( currTerm, null, 0);
                            }
                            else if (currTerm is CatchOpenTerm)
                            {
                                ((CatchOpenTerm) currTerm).Id = tryCatchId; // same id as for corresponding TRY
                                p = new TermNode(currTerm.Copy(false), null, 0);
                            }
                            else if (currTerm is Cut)
                            {
                                p = new TermNode(new Cut(stackSize: stackSize), null,
                                    goalListHead.Level + 1); // save the pre-unification state
                            }
                            else // Copy (false): keep the varNo constant over all terms of the predicate head+body
                                // (otherwise each term would get new variables, independent of their previous incarnations)
                            {
                                p = new TermNode(currTerm.Copy(false), currClause.PredDescr,
                                    goalListHead.Level + 1); // gets the newVar version
                            }

                            if (pHead == null)
                                pHead = p;
                            else
                                pTail.NextGoal = p;

                            pTail = p;
                            currClause = currClause.NextNode;
                        }

                        // If caching is on for this predicate, insert a CacheCheck Exitpoint, which will be
                        // picked up at the start of the loop loop if and when saveGoal has succeeded.
                        // It will only be picked up if the goal has actually succeeded, because a failure would
                        // have changed the goal last (and effectively removed the exit point)
                        if (caching)
                        {
                            pTail.NextGoal = new CacheCheckPoint( CachePort.Exit, saveGoal: saveGoal);
                            pTail = pTail.NextNode;
                        }

                        if (reporting)
                        {
                            pTail.NextGoal = new SpyPoint(p: SpyPort.Exit, g: saveGoal);
                            pTail = pTail.NextNode;
                        }

                        pTail.NextGoal = goalListHead.NextGoal;
                        goalListHead = pHead; // will never be a spypoint
                        findFirstClause = true;
                    }
                }
                else if (!(redo = CanBacktrack())) // unify failed - try backtracking
                {
                    return false;
                }

            } // end of while

            return true;
        }


        private void InsertCutFail()
        {
            var fail = new ClauseNode( BaseTerm.FAIL, null);
            fail.NextGoal = goalListHead.NextGoal;
            var cut = new ClauseNode( BaseTerm.CUT, null);
            cut.NextGoal = fail;
            goalListHead = cut;
        }


        private bool CanBacktrack() // returns true if choice point was found
        {
            return CanBacktrack(true);
        }

        private bool CanBacktrack(bool local) // local = false if user wants more (so as not to trigger the debugger)
        {
            object o;
            ChoicePoint cp;

            findFirstClause = false; // to prevent resetting to the first clause upon re-entering ExecuteGoalList

            while (varStack.Count != 0)
            {
                o = varStack.Pop();
                lastCp = o;

                if (o is CacheCheckPoint) // goal failed (otherwise we would not find this entry) -- cache a failure
                {
                    var saveGoal = ((CacheCheckPoint) o).SaveGoal;
                    var pd = saveGoal.PredDescr;
                    pd.Cache(saveGoal.Term.Copy(), false);
                }
                else if (reporting && o is SpyPoint)
                {
                    if (local && ((SpyPoint) o).Port == SpyPort.Fail)
                        Debugger( SpyPort.Fail, ((SpyPoint) o).SaveGoal, null, false,
                            6); // may reset saveGoal
                }
                else if (o is Variable)
                {
                    ((Variable) o).Unbind();
                }
                else if (o is ChoicePoint && ((ChoicePoint) o).IsActive)
                {
                    goalListHead = (cp = (ChoicePoint) o).GoalListHead; // this was the goal we wanted to prove ...

                    if (cp.NextClause == null) // no next predicate clause ...
                        findFirstClause = true; // ... so find predicate belonging to the goal last head
                    else
                        goalListHead.NextClause = cp.NextClause; // ... and this is next predicate clause to be tried

                    return true;
                }
            }

            return false;
        }


        private bool FindChoicePoint()
        {
            foreach (var o in varStack)
                if (o is ChoicePoint && ((ChoicePoint) o).IsActive)
                    return true;

            return false;
        }

        #endregion Goal last execution

        #region TRY/CATCH

        private void Throw(string exceptionClass, string exceptionMessage)
        {
            if (!SearchMatchingCatchClause(exceptionClass: exceptionClass, exceptionMessage: exceptionMessage))
            {
                var comma = exceptionClass == null || exceptionMessage == null ? null : ", ";
                var msg = string.Format("No CATCH found for throw( {0}{1}\"{2}\")",
                    arg0: exceptionClass, arg1: comma, arg2: exceptionMessage);
                IO.Error(msg: msg);
            }
        }

        private void Throw(string exceptionClass, string exceptionFmtMessage, params object[] args)
        {
            Throw(exceptionClass, string.Format(format: exceptionFmtMessage, args: args));
        }


        private void AddCallArgs(TermNode GoalListHead)
        {
            var callPred = GoalListHead.Head.Arg(0);
            var arity0 = callPred.Arity;
            var arity = arity0 + GoalListHead.Head.Arity - 1;
            var callArgs = new BaseTerm [arity];

            for (var i = 0; i < arity0; i++)
                callArgs[i] = callPred.Arg(pos: i);

            for (var i = arity0; i < arity; i++)
                callArgs[i] = GoalListHead.Head.Arg(1 + i - arity0);

            GoalListHead.Head = new CompoundTerm( callPred.Functor, args: callArgs);
        }


        private enum Status
        {
            NextCatchId,
            CompareIds,
            NextGoalNode,
            TestGoalNode,
            TryMatch
        }

        private bool SearchMatchingCatchClause(string exceptionClass, string exceptionMessage)
        {
            /* A TRY/CATCH predicate has the following format:
      
               TRY (<terms>) CATCH [<exception class>] (<terms>) CATCH [<exception class>] (<terms>) ...
      
               So, a TRY/CATCH statement can have more than one CATCH-clauses, each labeled with
               the name of an 'exception class' that can be given freely by the user and that corresponds
               to the exception class name in the throw/2/3 predicate.
      
               When a clause containing a TRY/CATCH is expanded (i.e. when ExecuteGoalList () places
               the clause body at the beginning of the goal list), not only the TRY-body, but also the
               CATCH-bodies are put on the list. Each TRY/CATCH statement is given a unique number (Id).
      
               Upon normal execution (no exception occurring), TRY-bodies when appearing on the goal list
               are executed normally, whereas CATCH-bodies are simply skipped. The only action taken
               is that upon hitting upon a TRY, the Id of the TRY/CATCH is pushed on a stack,
               which indicates that in case of an exception, the CATCH-clause(s) with this Id is the
               first candidate for execution.
      
               When an exception occurs, the goal list is searched for the first CATCH-clause with the
               Id found on top of the stack. If the exception matches the CATCH clause (i.e. the exception
               class name supplied as throw/2/3-parameter matches the exception class name of the CATCH-
               clause OR the CATCH-clause does not have an exception class name), execution is resumed with
               the first CATCH-body predicate as head of the goal list. If none of the CATCH-clauses match,
               the next Id (beloning to the caller TRY/CATCH) is popped off the stack and the search is
               continued.
      
               I am not sure what to do about unbinding. It seems obvious, when control is given to a
               calling predicate, to undo the variable bindings that occurred within the called predicate.
               But what to do in the case of backtracking, when the called predicate contained a choice
               point? In that case, the variable bindings up to the choice point should be restored again,
               which seems to entail a complicated strategy of temporarily unbinding variables.
               For the time being I decided not to do any unbinding when an exception is thrown, but
               this may prove to be a wrong decision. Anyway, CatchOpenTerm has a property SaveStackSize,
               which in future may be used when the varstack has to be popped and unbindings have to
               be carried out.
            */

            if (catchIdStack.Count == 0) return false;

            var catchId = catchIdStack.Pop();
            var catchIdFoundInGoalList = false; // true iff the catchId was found in the list of goals
            var status = Status.TestGoalNode;
            CatchOpenTerm t = null;

            while (true) // finite state engine implementation turned out to be easiest
                switch (status)
                {
                    case Status.TestGoalNode:
                        if (goalListHead == null) return false;

                        if (goalListHead.Term is CatchOpenTerm)
                        {
                            t = (CatchOpenTerm) goalListHead.Term;
                            status = Status.CompareIds;
                        }
                        else
                        {
                            status = Status.NextGoalNode;
                        }

                        break;
                    case Status.NextGoalNode:
                        goalListHead = goalListHead.NextGoal;
                        status = Status.TestGoalNode;
                        break;
                    case Status.NextCatchId: // get the Id of a potentially matching CATCH-clause
                        if (catchIdStack.Count == 0) return false;

                        catchId = catchIdStack.Pop();
                        catchIdFoundInGoalList = false;
                        status = Status.CompareIds;
                        break;
                    case Status.CompareIds:
                        if (t.Id == catchId)
                        {
                            catchIdFoundInGoalList = true;
                            status = Status.TryMatch;
                        }
                        else if (catchIdFoundInGoalList) // CATCH-id does not match anymore, so try ...
                        {
                            status = Status.NextCatchId; // ... the next CATCH (at an enclosing level)
                        }
                        else
                        {
                            status = Status.NextGoalNode; // catchId not yet found, try next goal
                        }

                        break;
                    case Status.TryMatch:
                        if (t.ExceptionClass == null || t.ExceptionClass == exceptionClass)
                        {
                            t.MsgVar.Unify(new StringTerm( exceptionMessage), varStack: varStack);

                            return true;
                        }

                        status = Status.NextGoalNode;
                        break;
                }
        }

        #endregion TRY/CATCH

        #region Debugging

        private bool Debugger(SpyPort port, TermNode goalNode, BaseTerm currClause, bool isFact, int callNo)
        {
            if (!reporting) return false;

            // only called if reporting = true. This means that at least one of the following conditions hold:
            // (1) debug is true. This means that trace = true and/or we must check whether this port has a spypoint
            // (2) xmlTrace = true.
            // Console-interaction will only occur if debug && (trace || spied)
            var spied = false;
            bool console;
            string s;
            var free = 0;
            string lmar;

            if (!trace) // determine spied-status
            {
                if (goalNode.PredDescr == null) goalNode.FindPredicateDefinition(predicateTable: Ps);

                spied = goalNode.Spied && (goalNode.SpyPort | port) == goalNode.SpyPort;
            }

            console = debug && (trace || spied);

            // continue only if either trace or spied, or if an XML-trace is to be constructed
            if (!console && !xmlTrace) return false;

            lmar = null;
            var goal = goalNode.Head;
            var level = goalNode.Level;

            if (@"\tdebug\tnodebug\tspy\tnospy\tnospyall\tconsult\ttrace\tnotrace\txmltrace\t".IndexOf(
                    value: goal.FunctorToString) != -1) return false;

            if (console) // this part is not required for xmlTrace
            {
                if (!qskip && level >= levelMax) return false;

                levelMax = INF; // recover from (q)s(kip) command
                qskip = false; // ...
                const int widthMin = 20; // minimal width of writeable portion of line

                var width = 140;
                var indent = 3 * (level - levelMin);
                var condensedLevel = 0;

                while (indent > width - widthMin)
                {
                    indent -= width - widthMin;
                    condensedLevel++;
                }

                if (condensedLevel == 0)
                {
                    lmar = "|  ".Repeat(n: level).Substring(0, length: indent);
                    free = width - indent;
                }
                else
                {
                    var dots = "| ... ";
                    lmar = dots + "|  ".Repeat(n: level).Substring(0, length: indent);
                    free = width - indent - dots.Length;
                }

                IO.Write( lmar);
            }

            switch (port)
            {
                case SpyPort.Call:
                    if (console)
                    {
                        s = Utils.WrapWithMargin(goal.ToString(), lmar + "|     ", lenMax: free);
                        IO.Write("{0,2:d2} Goal: {1}", level, s);
                        s = Utils.WrapWithMargin(currClause.ToString(), margin: lmar, lenMax: free);
                        IO.Write("{0}{1,2:d2} {2}: {3}", lmar, level, "Try ", s);
                    }

                    break;
                case SpyPort.Redo:
                    if (console)
                    {
                        s = Utils.WrapWithMargin(currClause.ToString(), lmar + "|     ", lenMax: free);
                        IO.Write("{0,2:d2} {1}: {2}", level, "Try ", s); // fact or clause
                    }

                    break;
                case SpyPort.Fail:
                    if (console)
                    {
                        s = Utils.WrapWithMargin(goal.ToString(), lmar + "|     ", lenMax: free);
                        IO.Write("{0,2:d2} Fail: {1}", level, s);
                    }

                    break;
                case SpyPort.Exit:
                    if (console)
                    {
                        s = Utils.WrapWithMargin(goal.ToString(), lmar + "         ", lenMax: free);
                        IO.Write("{0,2:d2} Exit: {1}", level, s);
                    }

                    break;
            }

            prevLevel = level;

            redo = false;

            if (rushToEnd || !console) return false;

            return DoDebuggingAction( port, lmar, goalNode);
        }


        private bool DoDebuggingAction(SpyPort port, string lmar, TermNode goalNode)
        {
            const string prompt = "|  TODO: ";
            const string filler = "|        ";
            int level;
            string cmd;
            var n = INF;
            var leap = 0; // difference between current level and new level

            while (true)
            {
                level = goalNode.Level;

                while (true)
                {
                    IO.Write(lmar + prompt);
                    cmd = IO.ReadLine().Replace(" ", "");

                    if (cmd.Length > 1)
                    {
                        var cmd0 = cmd.Substring(0, 1);

                        try
                        {
                            n = int.Parse(cmd.Substring(1));
                        }
                        catch
                        {
                            break;
                        }

                        if ("sr".IndexOf( cmd0) != -1 && Math.Abs( n) > level)
                        {
                            IO.WriteLine(lmar + filler + "*** Illegal value {0} -- must be in the range -{1}..{1}", n,
                                level);
                        }
                        else
                        {
                            if (n != INF && "cloqfgan+-.?h".IndexOf( cmd0) != -1)
                            {
                                IO.WriteLine(lmar + filler + "*** Unexpected argument {0}", n);
                            }
                            else
                            {
                                if (n < 0)
                                {
                                    level += n;
                                    leap = -n;
                                }
                                else
                                {
                                    leap = level - n;
                                    level = n;
                                }

                                cmd = cmd0;

                                break;
                            }
                        }
                    }
                    else
                    {
                        leap = 0;

                        if (cmd != "") cmd = cmd.Substring(0, 1);

                        break;
                    }
                }

                switch (cmd)
                {
                    case "": // creap
                    case "c": // ...
                        return false;
                    case "l": // leap
                        SetSwitch("Tracing", ref trace, false);
                        return false;
                    case "value": // skip
                        if (n == INF) levelMax = level;
                        else levelMax = n + 1;
                        return false;
                    case "t": // out (skip to Exit or Fail)
                        IO.WriteLine(lmar + filler + "*** NOT YET IMPLEMENTED");
                        break;
                    case "q": // q-skip (skip subgoals except if a spypoint was set)
                        levelMax = level;
                        qskip = true;
                        return false;
                    case "r": // retry
                        if (port == SpyPort.Call && leap == 0)
                        {
                            IO.WriteLine(lmar + filler + "*** retry command has no effect here");

                            return false;
                        }
                        else
                        {
                            RetryCurrentGoal(level: level);

                            return true;
                        }
                    case "f": // Fail
                        if (port == SpyPort.Fail)
                        {
                            IO.WriteLine(lmar + filler + "*** fail command has no effect here");

                            return false;
                        }
                        else
                        {
                            if (!CanBacktrack()) throw new AbortQueryException();

                            return true;
                        }
                    case "i": // ancestors
                        ShowAncestorGoals(lmar + filler);
                        break;
                    case "n": // nodebug
                        SetSwitch("Debugging", ref debug, false);
                        return false;
                    case "a":

                        throw new AbortQueryException();
                    case "+": // spy this
                        goalNode.PredDescr.SetSpy(true,  goalNode.Term.FunctorToString,
                            goalNode.Term.Arity, SpyPort.Full, false);
                        return false;
                    case "-": // nospy this
                        goalNode.PredDescr.SetSpy(false, goalNode.Term.FunctorToString,
                            goalNode.Term.Arity, SpyPort.Full, false);
                        return false;
                    case ".": // run to completion
                        rushToEnd = true;
                        return false;
                    case "?": // help
                    case "h": // ...
                        string[] help =
                        {
                            "c, CR       creep       Single-step to the next port",
                            "l           leap        Resume running, switch tracing off; stop at the next spypoint.",
                            "value [<N>] skip        If integer N provided: skip to Exit or Fail port of level N.",
                            "t           out         NOT YET IMPLEMENTED. Skip to the Exit or Fail port of the ancestor.",
                            "q           quasi-skip  Same as skip, but will stop if an intermediate spypoint is found.",
                            "r [<N>]     retry       Transfer control back to the Call port at level N.",
                            "f           fail        Fail the current goal.",
                            "i           ancestors   Show ancestor goals.",
                            "n           nodebug     Switch the debugger off.",
                            "a           abort       Abort the execution of the current query.",
                            "+           spy this    Set a spypoint on the current goal.",
                            "-           nospy this  Remove the spypoint for the current goal, if it exists.",
                            ".           rush        Run to completion without furder prompting.",
                            "?, h        help        Show this text."
                        };
                        foreach (var line in help)
                            IO.WriteLine(lmar + filler + line);
                        break;
                    default:
                        IO.WriteLine(lmar + filler + "*** Unknown command '{0}' -- enter ? or h for help", cmd);
                        break;
                }
            }
        }


        public void RetryCurrentGoal(int level)
        {
            object o;

            while (varStack.Count != 0)
            {
                o = varStack.Pop();

                if (o is SpyPoint && ((SpyPoint) o).Port == SpyPort.Call)
                {
                    goalListHead = ((SpyPoint) o).SaveGoal;

                    if (goalListHead.Level == level)
                    {
                        goalListHead
                            .FindPredicateDefinition(predicateTable: Ps); // clause had been forwarded -- reset it

                        return;
                    }
                }
                else if (o is Variable)
                {
                    ((Variable) o).Unbind();
                }
            }
        }


        private void ShowAncestorGoals(string lmar)
        {
            var ancestors = new Stack<TermNode>();
            TermNode g;
            int l;
            var lPrev = INF;

            foreach (var o in varStack) // works from the top down to the bottom
                if (o is SpyPoint && ((SpyPoint) o).Port == SpyPort.Call)
                    if ((l = (g = ((SpyPoint) o).SaveGoal).Level) < lPrev
                    ) // level decreases or stays equal at each step
                    {
                        ancestors.Push(item: g);
                        lPrev = l;
                    }

            while (ancestors.Count != 0) // revert the order
            {
                g = ancestors.Pop();
                IO.WriteLine(lmar + "{0}>{1}", Spaces(n: g.Level), g.Term);
            }
        }


        #endregion Debugging

        #region Command history

        private static readonly string HistoryHelpText =
            @"
  Command history commands:
  ========================
  !!                : show numbered list of previous commands
  !                 : repeat previous command
  !<n>              : repeat command number <n>
  !/<old>/<new>/    : repeat previous command, with <old> replaced by <new>.
                     / may be any char, and the end / may be omitted.
  !<n>/<old>/<new>/ : same for command number <n>
  !c                : clear the history
  !?                : help (this text)

  History commands must not be followed by a '.'
";

        private class CommandHistory : List<string>
        {

            public int cmdNo => Count + 1;
            private int maxNo; // maximum number of commands to be retained

          

            public CommandHistory(bool enablePersistence)
            {
                maxNo = Math.Abs( ConfigSettings.HistorySize);

                if (enablePersistence)
                    throw new NotImplementedException();
            }


            private new void Add(string cmd)
            {
                if (Count == 0 || cmd != this[Count - 1]) base.Add(item: cmd);
            }


            // returns true if a history command was entered
            public bool CheckForHistoryCommands(ref string query, PrologEngine engine)
            {
                var s = query.Trim();
                int i;

                if (s.Length == 0) return true;

                if (s.EndsWith("."))
                {
                    Add(cmd: query);

                    return false;
                }

                if (s[0] == '!')
                {
                    var len = s.Length;

                    if (s == "!!")
                    {
                        engine.solution.SetMessage(msg: HistoryList);
                        query = null;

                        return true;
                    }

                    if (s == "!c")
                    {
                        ClearHistory();
                        engine.solution.SetMessage("\r\n--- Command history cleared");
                        query = null;

                        return true;
                    }

                    if (s == "!?")
                    {
                        engine.solution.SetMessage(msg: HistoryHelpText);
                        query = null;

                        return true;
                    }

                    if (len == 1)
                    {
                        if (Count > 0) Add(query = this[Count - 1]);
                    }
                    else if (int.TryParse(s.Substring(1, len - 1), result: out i))
                    {
                        if (i < 1 || i > Count)
                        {
                            query = null;

                            return true;
                        }

                        Add(query = this[i - 1]);
                    }
                    else
                    {
                        // check for find/replace: ![commandno]<sepchar><findstr><sepchar><replacestr>[<sepchar>]
                        var r = new Regex(
                            @"^!(?<cno>\d+)?(?<sep>\S).{3,}$"); // find the command nr, the separator char, and check on length
                        var m = r.Match(input: query);

                        if (!m.Success)
                        {
                            IO.Error("Unrecognized history command '{0}'", s);
                            query = null;

                            return true;
                        }

                        var sep = m.Groups["sep"].Value[0];
                        var cmdNo = m.Groups["cno"].Captures.Count > 0
                            ? Convert.ToInt32( m.Groups["cno"].Value)
                            : Count;

                        if (cmdNo < 1 || cmdNo > Count)
                        {
                            IO.Error("No command {0}", cmdNo);
                            query = null;

                            return true;
                        }

                        // replace oldSep in command by newSep some char that is bound not to occur in command

                        r = new Regex(@"^!\d*(?:\f(?<str>[^\f]*)){2}\f?$"); // 2 occurances of str
                        m = r.Match(query.Replace(sep, '\f'));

                        CaptureCollection cc;

                        if (!m.Success || (cc = m.Groups["str"].Captures)[0].Value.Length == 0)
                        {
                            IO.Error("Unrecognized history command '{0}'", s);
                            query = null;

                            return true;
                        }

                        Add(query = this[cmdNo - 1].Replace(oldValue: cc[0].Value, newValue: cc[1].Value));
                    }

                    IO.WriteLine("?- " + query);

                    return false;
                }

                if (query.Trim().EndsWith("/")) // TEMP
                {
                    query = "/";
                    engine.Halted = true;

                    return true;
                }

                return false;
            }


            private string HistoryList
            {
                get
                {
                    var sb = new StringBuilder();

                    for (var i = 0; i < Count; i++) sb.AppendFormat("\r\n{0,2} {1}", i + 1, this[index: i]);

                    return sb.ToString();
                }
            }


            private void ClearHistory()
            {
                Clear();
            }
        }


       

        #endregion Command history

        #region Named variables

        private BaseTerm GetVariable(string s)
        {
            return solution.GetVar(name: s);
        }


        private void SetVariable(BaseTerm t, string s)
        {
            solution.SetVar(name: s, value: t);
        }


        private void EraseVariables()
        {
            solution.Clear();
        }

        private void RegisterVarNonSingleton(string s)
        {
            solution.RegisterVarNonSingleton(name: s);
        }

        public void ReportSingletons(ClauseNode c, int lineNo, ref bool firstReport)
        {
            solution.ReportSingletons(c: c, lineNo: lineNo, firstReport: ref firstReport);
        }

        #endregion Named variables

        #region Miscellaneous

        public void SetProfiling(bool mode)
        {
            profiling = mode;
        }


        public void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            userInterrupted = true;
            e.Cancel = true;
        }


        public string Prompt => string.Format("\r\n{0}{1} ?- ", Debugging ? "[d]" : "", arg1: CmdNo);


        public int ElapsedTime() // returns number of milliseconds since last Call
        {
            var prevStartTime = startTime == -1 ? DateTime.Now.Ticks : startTime;

            return (int) ((startTime = DateTime.Now.Ticks) - prevStartTime) / 10000;
        }


        public TimeSpan ProcessorTime() // returns numer of milliseconds since last Call
        {
            var processorTime = procTime.Elapsed;
            procTime.Reset();
            procTime.Start();
            return processorTime;
        }


        public long ClockTicksMSecs()
        {
            return DateTime.Now.Ticks / 10000;
        }


        public void CheckInitialConsultFile()
        {
            var initialConsultFileName = ConfigSettings.InitialConsultFile;

            if (string.IsNullOrEmpty( initialConsultFileName)) return;

            if (File.Exists(path: initialConsultFileName))
            {
                IO.WriteLine();
                Consult(fileName: initialConsultFileName);
            }
            else
            {
                var msg = string.Format(
                    "Initial file to be consulted not found (config file says: '{0}')\r\n",
                    arg0: initialConsultFileName);

                IO.Warning(msg: msg);
            }
        }




        public void Consult(string fileName)
        {
            var csharpStringsSave = csharpStrings;
            // string as ISO-style charcode lists or as C# strings

            try
            {
                Ps.Consult(fileName: fileName);
            }
            finally
            {
                csharpStrings = csharpStringsSave;
            }
        }

        public void Consult(Stream stream, string streamName = null)
        {
            var csharpStringsSave = csharpStrings;
            // string as ISO-style charcode lists or as C# strings

            try
            {
                Ps.Consult(stream: stream, streamName: streamName);
            }
            finally
            {
                csharpStrings = csharpStringsSave;
            }
        }

        public void ConsultFromString(string prologCode, string codeTitle = null)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes( prologCode)))
            {
                Consult(stream: ms, streamName: codeTitle);
            }
        }

        public void CreateFact(string functor, BaseTerm[] args)
        {
            Ps.Assert(new CompoundTerm( functor, args: args), true);
        }


        public void SetStringStyle(BaseTerm t)
        {
            var arg = t.FunctorToString;

            if (!(arg == "csharp" || arg == "iso"))
                IO.Error("Illegal argument '{0}' for setstringstyle/1 -- must be 'iso' or 'csharp'", t);

            csharpStrings = arg == "csharp";
        }


        private void SetSwitch(string switchName, ref bool switchVar, bool mode)
        {
            var current = switchVar;

            if (current == (switchVar = mode))
                IO.Message("{0} already {1}", switchName, mode ? "on" : "off");
            else
                IO.Message("{0} switched {1}", switchName, mode ? "on" : "off");
        }

        #endregion Miscellaneous
    }

    #endregion Engine
}