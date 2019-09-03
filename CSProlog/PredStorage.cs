//#define arg1index // if (un)defined, do the same in TermNodeList.cs !!!

#define enableSpying

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Prolog
{
    using Hashtable = Dictionary<object, object>;
    using SortedList = SortedList<object, object>;

  

    public partial class PrologEngine
    {
        public enum UndefAction
        {
            None,
            Fail,
            Succeed,
            Warning,
            Error
        }

        public class PredicateTable
        {
            // True if a predicate's clauses are not grouped together but are scattered
            // over a source file. Should normally not be used (used for code from others).
            private const string SLASH = "/";
            private readonly Dictionary<string, UndefAction> actionWhenUndefined; // currently not used
            private bool allDiscontiguous;
            private readonly Stack<string> consultFileStack;
            private readonly Stack<PrologParser> consultParserStack;
            private bool crossRefInvalid;
            private readonly CrossRefTable crossRefTable;
            private readonly Hashtable definedInCurrFile;
            private readonly PrologEngine engine;
            private readonly Hashtable isDiscontiguous;
            private readonly Hashtable moduleName;
            private readonly Dictionary<string, PredicateDescr> predTable;
            private string prevIndex;

            public PredicateTable(PrologEngine engine)
            {
                this.engine = engine;
                predTable = new Dictionary<string, PredicateDescr>();
                crossRefTable = new CrossRefTable();
                crossRefInvalid = true;
                Predefineds = new Hashtable();
                moduleName = new Hashtable();
                definedInCurrFile = new Hashtable();
                isDiscontiguous = new Hashtable();
                actionWhenUndefined = new Dictionary<string, UndefAction>();
                consultFileStack = new Stack<string>();
                consultParserStack = new Stack<PrologParser>();
            }

            public Hashtable Predefineds { get; }

            private string ConsultFileName => consultFileStack.Count == 0 ? null : consultFileStack.Peek();


            public PredicateDescr this[string key]
            {
                get
                {
                    PredicateDescr result;
                    predTable.TryGetValue(key: key, value: out result);

                    return result;
                }
                set => predTable[key: key] = value;
            }


            public void Reset()
            {
                predTable.Clear();
                Predefineds.Clear();
                moduleName.Clear();
                definedInCurrFile.Clear();
                isDiscontiguous.Clear();
                actionWhenUndefined.Clear();
                prevIndex = null;
                consultFileStack.Clear();
                consultParserStack.Clear();
            }


            public bool IsPredefined(string key) => Predefineds.ContainsKey(key);


            public void InvalidateCrossRef()
            {
                crossRefInvalid = true;
            }


            public void SetActionWhenUndefined(string f, int a, UndefAction u)
            {
                actionWhenUndefined[BaseTerm.MakeKey( f, a: a)] = u;
            }


            public UndefAction ActionWhenUndefined(string f, int a)
            {
                UndefAction u;
                actionWhenUndefined.TryGetValue(BaseTerm.MakeKey( f, a: a), value: out u);

                return u;
            }


            public bool IsPredicate(string functor, int arity)
            {
                return Contains(BaseTerm.MakeKey( functor, a: arity));
            }

            private PredicateDescr SetClauseList(string f, int a, ClauseNode c)
            {
                var key = BaseTerm.MakeKey( f, a: a);
                var pd = this[key: key];

                if (pd == null)
                    this[key: key] = pd =
                        new PredicateDescr(null, ConsultFileName, f, a, clauseList: c);
                else
                    pd.SetClauseListHead(c: c);

                pd.AdjustClauseListEnd();

                return pd;
            }


            public bool Contains(string key)
            {
                return this[key: key] != null;
            }


            public override string ToString()
            {
                return predTable.ToString();
            }

            public int Consult(string fileName)
            {
                return Consult(null, streamName: fileName);
            }

            public int Consult(Stream stream, string streamName = null)
            {
                // string as ISO-style charcode lists or as C# strings
                var fileName = streamName ?? Guid.NewGuid().ToString("N");
                consultFileStack.Push(item: fileName);
                consultParserStack.Push(item: Globals.CurrentParser);
                var parser = Globals.CurrentParser = new PrologParser(engine: engine);
                allDiscontiguous = false;

                try
                {
                    prevIndex = null;
                    definedInCurrFile.Clear();
                    isDiscontiguous.Clear();
                    Uncacheall();
                    //Globals.ConsultModuleName = null;
                    parser.Prefix = "&program\r\n";
                    IO.Write("--- Consulting {0} ... ", fileName);
                    parser.LoadFromStream(stream: stream, streamName: fileName);
                    IO.WriteLine("{0} lines read", parser.LineCount);
                    InvalidateCrossRef();
                }
                finally
                {
                    engine.showSingletonWarnings = true; // set it back to the default value of true

                    Globals.CurrentParser = consultParserStack.Pop();
                    ;
                    //Globals.ConsultModuleName = null; // Currently not used
                }

                return parser.LineCount;
            }


            public void AddPredefined(ClauseNode clause)
            {
                var head = clause.Head;
                var key = head.Key;
                var pd = this[key: key];

                if (pd == null)
                {
                    Predefineds[key: key] = true; // any value != null will do
                    SetClauseList( head.FunctorToString, a: head.Arity, c: clause); // create a PredicateDescr
                }
                else if (prevIndex != null && key != prevIndex)
                {
                    IO.Error("Definition for predefined predicate '{0}' must be contiguous", head.Index);
                }
                else
                {
                    pd.AppendToClauseList(c: clause);
                }

                prevIndex = key;
            }


            public void SetDiscontiguous(BaseTerm t)
            {
                if (t == null || t.FunctorToString != SLASH || !t.Arg(0).IsAtom || !t.Arg(1).IsInteger)
                    IO.Error("Illegal or missing argument '{0}' for discontiguous/1", t);

                // The predicate descriptor does not yet exist (and may even not come at all!)
                var key = BaseTerm.MakeKey( t.Arg(0).FunctorToString, t.Arg(1).To<short>());

                //IO.WriteLine ("--- Setting discontiguous for {0} in definitionFile {1}", key, Globals.ConsultFileName);
                isDiscontiguous[key: key] = "true";
            }


            public void SetDiscontiguous(bool mode)
            {
                allDiscontiguous = mode;
            }


            public void HandleSimpleDirective(PrologParser p, string directive, string argument, int arity)
            {
                //IO.WriteLine ("HandleSimpleDirective ({0}, {1}, {2})", directive, argument, arity);

                switch (directive)
                {
                    case "workingdir":
                        ConfigSettings.SetWorkingDirectory(dirName: argument);
                        break;
                    case "fail_if_undefined":
                        SetActionWhenUndefined( argument, a: arity, u: UndefAction.Fail);
                        break;
                    case "cache":
                        SetCaching( argument, arity, true);
                        break;
                    case "nocache":
                        SetCaching( argument, arity, false);
                        break;
                    case "cacheall":
                        SetCaching(null, 0, true);
                        break;
                    case "nocacheall":
                        SetCaching(null, 0, false);
                        break;
                    case "stacktrace":
                        if (argument == "on")
                            engine.userSetShowStackTrace = true;
                        else if (argument == "off")
                            engine.userSetShowStackTrace = false;
                        else
                            IO.Error(":- stacktrace: illegal argument '{0}'; use 'on' or 'off' instead", argument);
                        break;
                    case "style_check_singleton_warning":
                        if (argument == "on")
                            engine.showSingletonWarnings = true;
                        else if (argument == "off")
                            engine.showSingletonWarnings = false;
                        else
                            IO.Error(
                                ":- style_check_singleton_warning: illegal argument '{0}'; use 'on' or 'off' instead. It is 'on' by default.",
                                argument);
                        break;
                    case "initialization":
                        IO.Warning("':- initialization' directive not implemented -- ignored");
                        break;
                    default:
                        IO.Error("Unknown directive ':- {0}'", directive);
                        break;
                }
            }


            public void SetModuleName(string n)
            {
                var o = moduleName[key: n];
                var currFile = ConsultFileName;

                if (o == null)
                    moduleName[key: n] = currFile;
                // ConsultModuleName = null;
                else if ((string) o != currFile)
                    IO.Error("Module name {0} already declared in file {1}", n, (string) o);

                // ACTUAL FUNCTIONALITY TO BE IMPLEMENTED, using a 
                // ConsultModuleName stack, analoguous to ConsultFileName
            }


            public void AddClause(ClauseNode clause)
            {
                var head = clause.Head;

                var key = head.Key;
                var index = head.Index;

                if (Predefineds.ContainsKey(key))
                    IO.Error("Modification of predefined predicate {0} not allowed", index);

                if (prevIndex == key) // previous clause was for the same predicate
                {
                    var pd = this[key: key];
                    pd.AppendToClauseList(c: clause);
                }
                else // first predicate or different predicate
                {
                    var pd = this[key: key];

                    if (!definedInCurrFile.ContainsKey( key)
                    ) //  very first clause of this predicate in this file -- reset at start of consult
                    {
                        if (pd != null && pd.DefinitionFile != ConsultFileName)
                            IO.Error("Predicate '{0}' is already defined in {1}", index, pd.DefinitionFile);

                        definedInCurrFile[key: key] = true;
                        pd = SetClauseList( head.FunctorToString, a: head.Arity,
                            c: clause); // implicitly erases all previous definitions
                        pd.IsDiscontiguous = isDiscontiguous.ContainsKey( key) || allDiscontiguous;
                        prevIndex = key;
                    }
                    else // not the first clause. First may be from another definitionFile (which is an error).
                    {
                        // If from same, IsDiscontiguous must hold, unless DiscontiguousAllowed = "1" in .config
                        var b = false;

                        if (pd.IsDiscontiguous || (b = ConfigSettings.DiscontiguousAllowed))
                        {
                            if (b)
                                IO.Warning("Predicate '{0}' is defined discontiguously but is not declared as such",
                                    index);

                            if (pd.DefinitionFile == ConsultFileName)
                                pd.AppendToClauseList(c: clause);
                            else // OK
                                IO.Error("Discontiguous predicate {0} must be in one file (also found in {1})", index,
                                    pd.DefinitionFile);
                        }
                        else if (pd.DefinitionFile == ConsultFileName) // Warning or Error?
                        {
                            IO.Error("Predicate '{0}' occurs discontiguously but is not declared as such", index);
                        }
                        else
                        {
                            IO.Error("Predicate '{0}' is already defined in {1}", index, pd.DefinitionFile);
                        }
                    }
                }
            }


            private bool ListClause(PredicateDescr pd, string functor, int arity, int seqno)
            {
                ClauseNode clause = null;
                string details;

                if ((clause = pd.ClauseList) == null) return false;

                details = "source: " + pd.DefinitionFile;

//        if (pd.IsFirstArgIndexed) details += "; arg1-indexed (jump points marked with '.')";

                IO.WriteLine("\r\n{0}/{1}: ({2}) {3}", functor, arity, details,
                    seqno == 1 ? "" : seqno.ToString().Packed());

                while (clause != null)
                {
                    var currCachedClauseMustFail =
                        clause is CachedClauseNode && !((CachedClauseNode) clause).Succeeds;

                    TermNode next;

//          // prefix a clause that is pointed to by first-argument indexing with '.'
//          IO.Write (" {0}{1}", (pd.IsFirstArgMarked (clause))?".":" ", nextClause.Term);
                    IO.Write("  {0}", clause.Term);

                    if (currCachedClauseMustFail)
                    {
                        IO.Write(" :- !, fail");
                    }
                    else if ((next = clause.NextNode) != null)
                    {
                        var builtinId = next.BuiltinId;
                        IO.Write(" :-{0}", builtinId == BI.none
                            ? next.ToString()
                            : Environment.NewLine + builtinId);
                    }

                    IO.WriteLine(".");
                    clause = clause.NextClause;
                }

                return true;
            }


            public bool ListAll(string functor, int arity, bool showPredefined, bool showUserDefined)
            {
                var result = false; // no predicate <functor>/<arity> assumed
                PredicateDescr pd;

                // for sorting the predicates alphabetically:
                var sl = new SortedDictionary<string, PredicateDescr>();

                foreach (var kv in predTable)
                {
                    pd = kv.Value;

                    if (functor == null || functor == pd.Functor)
                    {
                        var isPredefined = IsPredefined(key: kv.Key);

                        if ((showPredefined && showUserDefined ||
                             showPredefined && isPredefined ||
                             showUserDefined && !isPredefined) &&
                            (arity == -1 || arity == pd.Arity))
                            sl.Add(pd.Functor + pd.Arity, value: pd);
                    }
                }

                var seqNo = 0;

                foreach (var kv in sl)
                    result = ListClause(pd = kv.Value, pd.Functor, pd.Arity, seqno: ++seqNo) || result;

                return result;
            }


            public bool ShowHelp(string functor, int arity, out string suggestion)
            {
                suggestion = null;
                const string HELPRES = "CsProlog.CsPrologHelp";

                // NOTE: .NET3.5+ can retrieve Assembly from a Type object via "Type.Assembly" property, but .NET Standard 1.4 dose not support it.
                var assemblyName = string.Join(", ",
                    GetType().AssemblyQualifiedName.Split(',').Skip(1).Select(s => s.Trim()).ToArray());
                var asm = Assembly.Load(new AssemblyName(assemblyName: assemblyName));
                var rm = new ResourceManager(baseName: HELPRES, assembly: asm);

                if (functor == null)
                {
                    IO.WriteLine(rm.GetString("help$"));
                    IO.WriteLine("\r\n  (*) contains the description of a feature rather than a predicate.");
                    IO.WriteLine("\r\n  Usage: help <predicate>[/<arity>] or help( <predicate>[/<arity>]).");

                    return true;
                }

                if (functor == "history")
                {
                    IO.Write( HistoryHelpText);

                    return true;
                }

                string[] arities;

                if (arity == -1) // no arity given: show all predicates for 'functor'
                {
                    arities = new[] {rm.GetString(name: functor)}; // returns something like "/0/2/3"

                    if (arities[0] == null) return false;

                    if (arities[0] == "(*)") // a little hacky
                        arities[0] = "*";
                    else
                        arities = arities[0].Split(new[] {'/'}, options: StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    arities = new[] {arity.ToString()};
                }

                var found = false;
                var sb = new StringBuilder();
                string content;

                foreach (var a in arities)
                {
                    var key = functor + '/' + a;
                    content = rm.GetString(name: key);

                    if (content == null) break;

                    sb.AppendLine(content.TrimEnd());
                    found = true;
                }

                const string DASHES =
                    "  -------------------------------------------------------------------------------------------";


                if (found)
                {
                    IO.WriteLine("\r\n{0}\r\n{1}\r\n{0}", DASHES, sb.ToString());
                }
                else
                {
                    var pd = FindClosestMatch(functor + (arity > 0 ? '/' + arity.ToString() : null));
                    suggestion =
                        pd == null ? null : string.Format(" Maybe '{0}' is what you mean?", arg0: pd.Name);
                }

                return found;
            }


            public void ResolveIndices() // functor/arity-key resolution
            {
                PredicateDescr pd;

                foreach (var kv in predTable) // traverse all program predicates
                {
                    ResolveIndex(pd = kv.Value);
                    pd.CreateFirstArgIndex(); // check whether first-argument indexing is applicable, and build the index if so
                }
            }


            private void ResolveIndex(PredicateDescr pd)
            {
                var clause = pd.ClauseList;

                while (clause != null
                ) // iterate over all clauses of this predicate. NextClause.BaseTerm contains predicate clauseHead
                {
                    var clauseHead = clause.Head; // clause = clauseHead :- clauseTerm*
                    var clauseTerm = clause.NextNode;

                    while (clauseTerm != null) // non-facts only. Iterate over all clauseTerm-terms at of this clause
                    {
                        if (clauseTerm.BuiltinId == BI.none) clauseTerm.PredDescr = this[key: clauseTerm.Term.Key];
                        // builtins (>=0) are handled differently (in Execute ())

                        clauseTerm = clauseTerm.NextNode;
                    }

                    clause = clause.NextClause;
                }
            }


            public void FindUndefineds()
            {
                var sd = new SortedList();

                foreach (var kv in predTable)
                    FindUndefined(sd: sd, pd: kv.Value);

                IO.WriteLine("The following predicates are undefined:");
                foreach (var kv in sd) IO.WriteLine("  {0}", kv.Key);
            }


            private void FindUndefined(SortedList sd, PredicateDescr pd)
            {
                var clause = pd.ClauseList;
                TermNode clauseTerm;

                while (clause != null) // iterate over all clauses of this predicate
                {
                    clauseTerm = clause.NextNode;

                    while (clauseTerm != null) // non-facts only. Iterate over all clauseTerm-terms of this clause
                    {
                        if (clauseTerm.BuiltinId == BI.none && clauseTerm.PredDescr == null)
                            sd[key: clauseTerm.Term.Index] = null;

                        clauseTerm = clauseTerm.NextNode;
                    }

                    clause = clause.NextClause;
                }
            }

            // try to match the name of an unrecognised command
            public PredicateDescr FindClosestMatch(string predName)
            {
                const float THRESHOLD = 0.5F; // maximum value for likely match
                PredicateDescr closestMatch = null;
                var closestMatchValue = 1.0F;

                foreach (var kv in predTable)
                {
                    var pd = kv.Value;
                    float matchValue;

                    if ((matchValue = pd.Name.Levenshtein(b: predName)) < closestMatchValue)
                    {
                        closestMatchValue = matchValue;
                        closestMatch = pd;
                    }
                }

                return closestMatchValue < THRESHOLD ? closestMatch : null;
            }


#if enableSpying
            public bool SetSpy(bool enabled, string functor, int arity, BaseTerm list)
            {
                SpyPort ports;

                if (list == null)
                {
                    ports = SpyPort.Full;
                }
                else
                {
                    ports = SpyPort.None;
                    string s;

                    while (list.Arity == 2)
                    {
                        s = list.Arg(0).FunctorToString;

                        try
                        {
                            ports |= (SpyPort) Enum.Parse(typeof(SpyPort), value: s);
                        }
                        catch
                        {
                            IO.Error("Illegal value '{0}'", s);
                        }

                        list = list.Arg(1);
                    }
                }

                PredicateDescr pd;

                if (arity == -1)
                {
                    var found = false;

                    foreach (var kv in predTable)
                        if ((pd = kv.Value).Functor == functor)
                        {
                            found = true;
                            pd.SetSpy( enabled, pd.Functor, pd.Arity, ports,
                                warn: !enabled);
                        }

                    if (!found) IO.Error("Predicate does not exist: {0}", functor);

                    return found;
                }

                predTable.TryGetValue(BaseTerm.MakeKey( functor, a: arity), value: out pd);

                if (pd == null)
                {
                    IO.Error("Predicate does not exist: {0}/{1}", functor, arity);

                    return false;
                }

                pd.SetSpy(enabled, functor, arity, ports, warn: !enabled);

                return true;
            }

            public void SetNoSpyAll()
            {
                PredicateDescr pd;

                foreach (var kv in predTable)
                    (pd = kv.Value).SetSpy(false, pd.Functor, pd.Arity, SpyPort.None, false);
            }

            public void ShowSpypoints()
            {
                foreach (var kv in predTable)
                    kv.Value.ShowSpypoint();
            }
#endif


            #region assert/retract

            public void Assert(BaseTerm assertion, bool asserta)
            {
                BaseTerm head;
                TermNode body = null;
                PredicateDescr pd;
                var assertionCopy = assertion.Copy(true);

                if (assertionCopy.HasFunctor( PrologParser.IMPLIES))
                {
                    head = assertionCopy.Arg(0);
                    body = assertionCopy.Arg(1).ToGoalList();
                }
                else
                {
                    head = assertionCopy;
                }

                if (!head.IsCallable) IO.Error("Illegal predicate head '{0}'", head.ToString());

                var key = head.Key;

                if (Predefineds.ContainsKey( key) || head.Precedence >= 1000)
                    IO.Error("assert/1 cannot be applied to predefined predicate or operator '{0}'",
                        assertionCopy.Index);

                predTable.TryGetValue(key: key, value: out pd);
                var newC = new ClauseNode( head, body: body);

                if (pd == null) // first head
                {
                    SetClauseList( head.FunctorToString, a: head.Arity, c: newC);
                    ResolveIndices();
                }
                else if (pd.IsCacheable)
                {
                    IO.Error("assert/1 cannot be applied to cached predicate '{0}'",
                        assertionCopy.Index);
                }
                else if (asserta) // at beginning
                {
                    newC.NextClause = pd.ClauseList; // pd.ClauseList may be null
                    SetClauseList( head.FunctorToString, a: head.Arity, c: newC);
#if arg1index
          pd.CreateFirstArgIndex (); // re-create
#endif
                }
                else // at end
                {
                    pd.AppendToClauseList(c: newC);
#if arg1index
          pd.CreateFirstArgIndex (); // re-create
#endif
                }

                InvalidateCrossRef();
            }

            public bool Retract(BaseTerm t, VarStack varStack, BaseTerm where)
            {
                var key = t.Key;

                if (Predefineds.ContainsKey( key))
                    IO.Error("retract of predefined predicate {0} not allowed", key);

                var pd = this[key: key];

                if (pd == null) return false;

                InvalidateCrossRef();
                var c = pd.ClauseList;
                ClauseNode prevc = null;
                BaseTerm cleanTerm;
                int top;

                while (c != null)
                {
                    cleanTerm = c.Head.Copy();

                    top = varStack.Count;

                    if (cleanTerm.Unify( t, varStack: varStack)) // match found -- remove this term from the chain
                    {
                        if (prevc == null) // remove first clause
                        {
                            if (c.NextClause == null
                            ) // we are about to remove the last remaining clause for this predicate
                            {
                                predTable.Remove(key: key); // ... so remove its PredicateDescr as well
#if arg1index
                pd.CreateFirstArgIndex (); // re-create
#endif
                                ResolveIndices();
                            }
                            else
                            {
                                pd.SetClauseListHead(c: c.NextClause);
                            }
                        }
                        else // not the first
                        {
                            prevc.NextClause = c.NextClause;
                            prevc = c;
                            pd.AdjustClauseListEnd();
#if arg1index
              pd.CreateFirstArgIndex (); // re-create
#endif
                        }

                        return true; // possible bindings must stay intact (e.g. if p(a) then retract(p(X)) yields X=a)
                    }

                    Variable s;
                    for (var i = varStack.Count - top;
                        i > 0;
                        i--) // unbind all vars that got bound by the above Unification
                    {
                        s = (Variable) varStack.Pop();
                        s.Unbind();
                    }

                    prevc = c;
                    c = c.NextClause;
                }

                ResolveIndices();

                return false;
            }


            public bool RetractAll(BaseTerm t, VarStack varStack)
            {
                // remark: first-argument indexing is not affected by deleting clauses

                var key = t.Key;

                if (Predefineds.ContainsKey(key))
                    IO.Error("retract of predefined predicate {0} not allowed", key);

                var pd = this[key: key];

                if (pd == null) return true;

                var c = pd.ClauseList;
                ClauseNode prevc = null;
                var match = false;

                while (c != null)
                {
                    var cleanTerm = c.Term.Copy();

                    if (cleanTerm.IsUnifiableWith( t, varStack: varStack)
                    ) // match found -- remove this head from the chain
                    {
                        match = true; // to indicate that at least one head was found

                        if (prevc == null) // remove first clause
                        {
                            if (c.NextClause == null
                            ) // we are about to remove the last remaining clause for this predicate
                            {
                                predTable.Remove(key: key); // ... so remove its PredicateDescr as well

                                break;
                            }

                            pd.SetClauseListHead(c: c.NextClause);
                        }
                        else // not the first
                        {
                            prevc.NextClause = c.NextClause;
                            prevc = c;
                        }
                    }
                    else
                    {
                        prevc = c;
                    }

                    c = c.NextClause;
                }

                if (match)
                {
#if arg1index
          pd.DestroyFirstArgIndex (); // rebuilt by ResolveIndices()
#endif
                    pd.AdjustClauseListEnd();
                    ResolveIndices();
                }

                return true;
            }


            public bool Abolish(string functor, int arity)
            {
                var key = BaseTerm.MakeKey( functor, a: arity);

                if (Predefineds.ContainsKey(key))
                    IO.Error("abolish of predefined predicate '{0}/{1}' not allowed", functor, arity);

                var pd = this[key: key];

                if (pd == null) return false;

                predTable.Remove(key: key);

#if arg1index
        pd.DestroyFirstArgIndex (); // rebuilt by ResolveIndices()
#endif
                ResolveIndices();

                return true;
            }

            #endregion assert/retract


            // CACHEING CURRENTLY NOT USED

            #region cacheing

            public bool SetCaching(string functor, int arity, bool value)
            {
                if (functor == null) // clear entire cache
                {
                    Uncacheall();

                    IO.Message("Entire cache cleared");

                    return true;
                }

                PredicateDescr pd;
                var found = false;

                foreach (var kv in predTable)
                {
                    pd = kv.Value;

                    if (functor == pd.Functor && (arity == -1 || pd.Arity == arity))
                    {
                        pd.IsCacheable = value;

                        if (!value) pd.Uncache(); // remove cached values

                        found = true;

                        if (value)
                            IO.Message("Caching set on {0}/{1}", functor, pd.Arity);
                        else
                            IO.Message("Caching removed from {0}/{1}", functor, pd.Arity);
                    }
                }

                if (!found)
                    if (arity == -1)
                        IO.Error("Predicate '{0}' not found", functor);
                    else
                        IO.Error("Predicate '{0}/{1}' not found", functor, arity);

                return found;
            }

            // remove all cached clauses from all predicates
            public void Uncacheall()
            {
                foreach (var kv in predTable)
                    kv.Value.Uncache();
            }

            #endregion cacheing


            #region cross reference table

            private void SetupCrossRefTable() //TODO (later...): deal with arguments of not/1 and call/1
            {
                if (!crossRefInvalid) return;

                crossRefTable.Reset();
                PredicateDescr pd;

                foreach (var kv in predTable)
                {
                    pd = kv.Value;
                    var isPredefined = IsPredefined(key: kv.Key);
                    var clause = pd.ClauseList;

                    if (!isPredefined) crossRefTable.AddPredicate(pd: pd);

                    // iterate over NextClause and NextClause.NextNode
                    while (clause != null)
                    {
                        var node = clause.NextNode;

                        while (node != null)
                        {
                            if (node.PredDescr != null && !isPredefined)
                            {
                                PredicateDescr npd;
                                //IO.WriteLine ("{0} uses {1}", pd.Name, node.PredDescr.Name);
                                crossRefTable[pd, npd = node.PredDescr] = false;

                                if (npd.Name == "not/1" || npd.Name == "call/1") // add args to cref
                                {
                                    var arg = node.NextNode;
                                    IO.WriteLine("{0} arg is {1}", npd.Name, arg);
                                }
                            }

                            node = node.NextNode;
                        }

                        clause = clause.NextClause;
                    }
                }

                crossRefTable.CalculateClosure();
                crossRefInvalid = false;
            }


            public void CrossRefTableToSpreadsheet(string fileName)
            {
                SetupCrossRefTable();
                crossRefTable.GenerateCsvFile(fileName: fileName);
            }

            #endregion


            #region profile count -- for storing predicate call counts (profile/0/1 command)

            private class ProfileCountList : List<KeyValuePair<int, string>>
            {
                private static IComparer<KeyValuePair<int, string>> SortProfileCounts => new ProfileCountComparer();

                public void Add(int count, string name)
                {
                    Add(new KeyValuePair<int, string>(key: count, value: name));
                }

                public new void Sort()
                {
                    base.Sort(comparer: SortProfileCounts);
                }

                public int MaxNameLen(int maxEntry)
                {
                    var maxNameLen = 0;
                    var i = 0;

                    foreach (var kv in this)
                    {
                        if (i++ == maxEntry) break;

                        maxNameLen = Math.Max(val1: maxNameLen, val2: kv.Value.Length);
                    }

                    return maxNameLen;
                }

                private class ProfileCountComparer : IComparer<KeyValuePair<int, string>>
                {
                    public int Compare(KeyValuePair<int, string> kv0, KeyValuePair<int, string> kv1)
                    {
                        var result = -kv0.Key.CompareTo( kv1.Key); // descending count order

                        if (result == 0) return kv0.Value.CompareTo(strB: kv1.Value);

                        return result;
                    }
                }
            }


            public void ShowProfileCounts(int maxEntry) // maximum number of entries to be shown
            {
                var profile = new ProfileCountList();
                var maxLen = 0;
                var maxVal = 0;

                foreach (var kv in predTable)
                    if (!IsPredefined(key: kv.Key) && kv.Value.ProfileCount > 0)
                    {
                        profile.Add(count: kv.Value.ProfileCount, name: kv.Value.Name);
                        maxVal = Math.Max(val1: maxVal, val2: kv.Value.ProfileCount);
                        maxLen = 1 + (int) Math.Log10(d: maxVal);
                    }

                profile.Sort();

                IO.WriteLine();
                var format =
                    "  {0,-" + profile.MaxNameLen(maxEntry: maxEntry) +
                    "} : {1," + maxLen + ":G}";

                var entryCount = 0;

                foreach (var kv in profile)
                {
                    if (entryCount++ > maxEntry) break;

                    IO.WriteLine( format, kv.Value, kv.Key);
                }
            }


            public void ClearProfileCounts()
            {
                foreach (var kv in predTable)
                    kv.Value.ProfileCount = 0;
            }

            #endregion profile counts
        }

        // classes for iterating over a predicate's clauses (used by clause/2)

        #region ClauseIterator

        private class ClauseIterator : IEnumerable<BaseTerm>
        {
            private readonly BaseTerm clauseHead;
            private readonly IEnumerator<BaseTerm> iterator;
            private readonly PredicateDescr pd;
            private readonly VarStack varStack;

            public ClauseIterator(PredicateTable predTable, BaseTerm clauseHead, VarStack varStack)
            {
                pd = predTable[key: clauseHead.Key]; // null if not found
                this.clauseHead = clauseHead;
                this.varStack = varStack;
                iterator = GetEnumerator();
            }

            public BaseTerm ClauseBody { get; private set; }

            // A predicate consists of one or more clauses. A clause consist of a head and optionally a
            // body. A head is a term, the body is a sequence of terms. A predicate is stored as a chain
            // of TermNodes, where each TermNode represents a clause. These TermNodes are linked via the
            // nextClause field. In each nextClause/TermNode the clause head is stored in term, and the
            // clause body (which may be null) in NextNode.

            public IEnumerator<BaseTerm> GetEnumerator()
            {
                if (pd == null) yield break;

                var clause = pd.ClauseList;

                while (clause != null) // iterate over all clauses of this predicate
                {
                    var bodyNode = clause.NextNode;

                    var marker = varStack.Count; // register the point to which we must undo unification

                    if (clause.Head.Unify( clauseHead, varStack: varStack))
                    {
                        if (bodyNode == null) // a fact
                            ClauseBody = new BoolTerm(true);
                        else if (bodyNode.BuiltinId == BI.none)
                            ClauseBody = bodyNode.TermSeq();
                        else
                            ClauseBody = new StringTerm("<builtin>");

                        yield return ClauseBody;
                    }

                    // undo unification with clauseHead before attempting the next clause head
                    BaseTerm.UnbindToMarker(varStack: varStack, marker: marker);
                    clause = clause.NextClause;
                }
            }


            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            public bool MoveNext()
            {
                return iterator.MoveNext();
            }
        }

        #endregion ClauseIterator
    }
}