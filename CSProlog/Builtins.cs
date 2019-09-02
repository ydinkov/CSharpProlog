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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace Prolog
{
    public enum BI // builtins
    {
        none,
        abolish,
        arg,
        append,
        append2,
        assert,
        asserta,
        assertz,
        atom_,
        atom_string,
        atomic,
        between,
        bool_,
        bw_transform,
        cache,
        call,
        clause,
        clearall,
        clearprofile,
        clipboard,
        cls,
        collection_add,
        collection_exit,
        collection_init,
        combination,
        compound,
        config_setting,
        console,
        consult,
        copy_term,
        crossref,
        current_op,
        cut,
        dayname,
        dcg_flat,
        date_part,
        datetime,
        dayofweek,
        dayofyear,
        debug,
        dec_counter,
        display,
        environment,
        eq_num,
        eq_str,
        errorlevel,
        expand_term,
        fail,
        fileexists,
        flat,
        float_,
        format,
        functor,
        ge_num,
        ge_ord,
        gensym,
        genvar,
        get,
        get_counter,
        get0,
        getenvvar,
        getvar,
        ground,
        gt_num,
        gt_ord,
        halt,
        help,
        inc_counter,
        integer,
        ip_address,
        is_,
        json_term,
        json_xml,
        le_num,
        le_ord,
        leapyear,
        length,
        license,
        list,
        listing,
        listing0,
        listing0X,
        listing0XN,
        listingX,
        listingXN,
        lt_num,
        lt_ord,
        make_help_resx,
        maxwritedepth,
        member,
        name,
        ne_num,
        ne_str,
        ne_uni,
        nl,
        nocache,
        nodebug,
        nonvar,
        noprofile,
        nospy,
        nospyall,
        notrace,
        noverbose,
        now,
        number,
        numbervars,
        numcols,
        or,
        permutation,
        pp_defines,
        predicatePN,
        predicateX,
        print,
        profile,
        put,
        query_timeout,
        read,
        readatoms,
        readatom,
        readeof,
        readln,
        regex_match,
        regex_replace,
        retract,
        retractall,
        reverse,
        sendmail,
        see,
        seeing,
        seen,
        set_counter,
        setenvvar,
        setvar,
        shell_d,
        shell_p,
        shell_x,
        shell_sync_d,
        shell_sync_p,
        shell_sync_x,
        shell_dos,
        shell_exe,
        showfile,
        showprofile,
        silent,
        sort,
        spy,
        spypoints,
        sql_connect,
        sql_connection,
        sql_command,
        sql_disconnect,
        sql_select,
        stacktrace,
        callstack,
        statistics,
        string_,
        string_datetime,
        string_term,
        string_words,
        stringstyle,
        succ,
        tab,
        tell,
        telling,
        term_pattern,
        throw_,
        time_part,
        timespan,
        today,
        told,
        trace,
        treeprint,
        undefineds,
        unifiable,
        univ,
        username,
        userroles,
        validdate,
        validtime,
        var,
        verbose,
        version,
        weekno,
        workingdir,
        write,
        writef,
        writeln,
        writelnf,
        xml_term,
        xmltrace,
        xml_transform
    }

    public partial class PrologEngine
    {
        private string currentInputName;
        private string currentOutputName;

        private bool DoBuiltin(BI biId, out bool findFirstClause)
        {
            findFirstClause = false;
            var term = goalListHead.Term;
            BaseTerm t0, t1, t2, t3, t4, t5;
            int n, y, m, d, h, s;
            int arity;
            string functor;
            bool result;
            var inFile = false;
            var outFile = false;
            var dupMode = DupMode.DupAccept; // for setof, bagoff, findall
            TermType type;
            string a, x;
            string fileName;
            int cntrValue;
            DateTime dati;
            TimeSpan ti;

            predicateCallOptions.Clear();

            #region switch

            switch (biId)
            {

                case BI.consult: // individual file or list of files
                    t0 = term.Arg(0);

                    if (t0.IsProperList)
                    {
                        var lines = 0;
                        var files = 0;

                        while (t0.Arity == 2)
                        {
                            fileName = Utils.FileNameFromTerm(t0.Arg(0), ".pl");

                            if (fileName == null) return false;

                            lines += Ps.Consult(fileName: fileName);
                            files++;
                            t0 = t0.Arg(1);
                        }

                        if (files > 1) IO.Message("Grand total is {0} lines", lines);

                        Ps.ResolveIndices();

                        break;
                    }

                    if (t0.IsAtomOrString)
                    {
                        fileName = Utils.FileNameFromTerm(t: t0, ".pl");

                        if (fileName == null) return false;

                        IO.Write("--- Consulting {0} ... ", fileName);
                        Ps.Consult(fileName: fileName);
                        IO.WriteLine("{0} lines read", parser.LineCount);
                        Ps.ResolveIndices();

                        break;
                    }

                    return IO.Error("Unable to read file '{0}'", t0.Arg(0));

                case BI.asserta:
                    Ps.Assert(term.Arg(0), true); // true: at beginning
                    break;

                case BI.assert:
                case BI.assertz:
                    Ps.Assert(term.Arg(0), false);
                    break;

                case BI.retract:
                    if (Ps.Retract(term.Arg(0), varStack: varStack, null))
                    {
                        currentCp.NextClause = retractClause;
                    }
                    else
                    {
                        CanBacktrack();
                        return false;
                    }

                    break;

                case BI.retractall: // retractall
                    Ps.RetractAll(term.Arg(0), varStack: varStack);
                    break;

                case BI.spy: // leash modes [call, exit, redo, fail]
                case BI.nospy:
                    result = true;
                    t0 = term.Arg(0);

                    if (term.Arity == 2) t3 = term.Arg(1);
                    else t3 = null; // leash t

                    if (t0.HasFunctor("/") && t0.Arity == 2 && (t1 = t0.Arg(0)).IsAtom && (t2 = t0.Arg(1)).IsInteger)
                        result = Ps.SetSpy(term.HasFunctor("spy"), functor: t1.FunctorToString, t2.To<int>(), list: t3);
                    else if (t0.Arity == 0)
                        result = Ps.SetSpy(term.HasFunctor("spy"), functor: t0.FunctorToString, -1, list: t3);

                    if (!result) return false;

                    if (!debug)
                    {
                        debug = true;
                        IO.Message("Debugging switched on");
                    }

                    break;

                case BI.nospyall:
                    Ps.SetNoSpyAll();
                    break;

                case BI.verbose:
                    IO.Verbose = true;
                    break;

                case BI.noverbose:
                case BI.silent:
                    IO.Verbose = false;
                    break;

                case BI.trace:
                case BI.notrace:
                    SetSwitch("Tracing", switchVar: ref trace, term.HasFunctor("trace"));
                    if (trace) debug = true;
                    reporting = debug || xmlTrace;
                    break;

                case BI.debug:
                case BI.nodebug:
                    SetSwitch("Debugging", switchVar: ref debug, term.HasFunctor("debug"));
                    reporting = debug || xmlTrace;
                    break;

                case BI.help:
                    string suggestion = null;

                    if (term.Arity == 0) // help on help
                    {
                        Ps.ShowHelp(null, -1, suggestion: out suggestion);
                    }
                    else if (term.Arity == 1) // specific predicate, potentially with an arity
                    {
                        result = false;

                        if ((t0 = term.Arg(0)).HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom &&
                            t0.Arg(1).IsInteger)
                            result = Ps.ShowHelp(functor: t0.Arg(0).FunctorToString, t0.Arg<int>(1),
                                suggestion: out suggestion);
                        else if (term.Arg(0).IsAtom) // predicate functor without arity: take all arities
                            result = Ps.ShowHelp(functor: term.Arg(0).FunctorToString, -1, suggestion: out suggestion);

                        if (!result)
                            IO.Warning("Predicate '{0}' not found.{1}", term.Arg(0), suggestion);
                    }

                    break;

                case BI.cache:
                    if (SetCaching(term: term, true)) break;
                    return false;

                case BI.nocache:
                    if (SetCaching(term: term, false)) break;
                    return false;

                // bagof, setof, findall
                case BI.collection_init:
                    if (term.Arg(0).HasFunctor("setof"))
                        dupMode = DupMode.DupIgnore;
                    else
                        dupMode = DupMode.DupAccept;

                    term.Arg(1).Unify(new CollectionTerm(dupMode: dupMode), varStack: varStack);

                    break;

                case BI.collection_add:
                    if ((t2 = term.Arg(2)).IsVar) return false;

                    // t2 must be copied because it is unbound during backtracking
                    if (term.Arg(0).HasFunctor("setof"))
                        ((CollectionTerm) term.Arg(1)).Insert(t2.Copy());
                    else
                        ((CollectionTerm) term.Arg(1)).Add(t2.Copy());

                    break;

                case BI.collection_exit:
                    var ct = (CollectionTerm) term.Arg(1);

                    // bagof and setof must Fail if there are no matches; findall will Succeed
                    if (term.Arg(0).FunctorToString != "findall" && ct.Count == 0) return false;

                    var ccc = ct.ToList();

                    term.Arg(2).Unify(ct.ToList(), varStack: varStack);

                    break;

                case BI.version: // version(V, R)
                    if (!term.Arg(0).Unify(new AtomTerm(value: VERSION), varStack: varStack)) return false;
                    if (!term.Arg(1).Unify(new AtomTerm(value: RELEASE), varStack: varStack)) return false;
                    break;

                case BI.halt:
                    Halted = true;
                    break;

                case BI.reverse: // reverse( ?X, ?R) -- proper list X is the reversed version of list R
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsVar)
                    {
                        if (t1.IsVar || !t1.IsProperList) return false;

                        t0.Unify(((ListTerm) t1).Reverse(), varStack: varStack);
                    }
                    else // t0 has a value
                    {
                        if (!t0.IsProperList) return false;

                        if (!t1.Unify(((ListTerm) t0).Reverse(), varStack: varStack))
                            return false;
                    }

                    break;

                case BI.combination: // combination( +P, +K, ?Q) -- list Q is the 'next' K-combination of list P
                    t1 = term.Arg(1);
                    t2 = term.Arg(2); // combination size (k)

                    if (!t1.IsProperList || !t2.IsInteger) return false;

                    if (t1.IsEmptyList)
                    {
                        if (term.Arg(3).Unify(t: BaseTerm.EMPTYLIST, varStack: varStack)) break;

                        return false;
                    }

                    Combination cmb;
                    IEnumerator<ListTerm> iCombi = null;
                    t0 = term.Arg(0);

                    if (t0.IsVar) // first call only, Arg(0) contains State info
                    {
                        cmb = new Combination((ListTerm) t1, t2.To<int>());
                        iCombi = cmb.Iterator;
                        t0.Unify(new UserClassTerm<IEnumerator<ListTerm>>(obj: iCombi), varStack: varStack);

                        break;
                    }

                    iCombi = ((UserClassTerm<IEnumerator<ListTerm>>) t0).UserObject;

                    while (true)
                    {
                        if (!iCombi.MoveNext())
                        {
                            term.SetArg(0, t: BaseTerm.VAR);

                            return false;
                        }

                        if (term.Arg(3).Unify(t: iCombi.Current, varStack: varStack)) break;

                        return false;
                    }

                    break;

                case BI.permutation: // permutation( +P, ?Q) -- list Q is the 'next' permutation of list P
                    t1 = term.Arg(1);

                    if (!t1.IsProperList) return false;

                    if (t1.IsEmptyList)
                    {
                        if (term.Arg(2).Unify(t: BaseTerm.EMPTYLIST, varStack: varStack)) break;

                        return false;
                    }

                    Permutation pmt;
                    IEnumerator<ListTerm> iPermut = null;
                    t0 = term.Arg(0);

                    if (t0.IsVar) // first call only, Arg(0) contains State info
                    {
                        pmt = new Permutation((ListTerm) t1);
                        iPermut = pmt.GetEnumerator();
                        t0.Unify(new UserClassTerm<IEnumerator<ListTerm>>(obj: iPermut), varStack: varStack);

                        break;
                    }

                    iPermut = ((UserClassTerm<IEnumerator<ListTerm>>) t0).UserObject;

                    while (true)
                    {
                        if (!iPermut.MoveNext())
                        {
                            term.SetArg(0, t: BaseTerm.VAR);

                            return false;
                        }

                        if (term.Arg(2).Unify(t: iPermut.Current, varStack: varStack)) break;

                        return false;
                    }

                    break;

                case BI.length: // properLength( L, N)
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsProperOrPartialList)
                    {
                        n = 0;

                        while (t0.IsListNode)
                        {
                            n++;
                            t0 = t0.Arg(1);
                        }

                        if (t0.IsVar && t1.IsNatural) // cope with calls such as properLength( [1,2,3|T], 9)
                        {
                            if ((n = t1.To<int>() - n) < 0) return false;

                            t2 = BaseTerm.EMPTYLIST;

                            for (var i = 0; i < n; i++)
                                t2 = new ListTerm(new Variable(), t1: t2);

                            t0.Unify(t: t2, varStack: varStack);

                            break;
                        }

                        if (!term.Arg(1).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;
                    }
                    else if (t0.IsAtomOrString)
                    {
                        if (!term.Arg(1).Unify(new DecimalTerm(value: t0.FunctorToString.Length), varStack: varStack))
                            return false;
                    }
                    else // create a list with N elements
                    {
                        if (!t1.IsNatural) return false;

                        arity = t1.To<int>();
                        t1 = BaseTerm.EMPTYLIST;

                        for (var i = 0; i < arity; i++)
                            t1 = new ListTerm(new Variable(), t1: t1);

                        t0.Unify(t: t1, varStack: varStack);
                    }

                    break;


                case BI.sort: // sort( L, S)
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsProperList)
                    {
                        if (!(t1.IsProperList || t1.IsVar)) return false;

                        var tlist = new BaseTermSet(list: t0);
                        tlist.Sort();

                        if (!t1.Unify(tlist.ToList(), varStack: varStack)) return false;
                    }
                    else
                    {
                        return false;
                    }

                    break;

                case BI.succ: // succ(?N0, ?N1) -- succeeds if N1-N0 = 1
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsVar)
                    {
                        if (t1.IsVar || !t1.IsInteger) return false;

                        t0.Unify(new DecimalTerm(t1.To<int>() - 1), varStack: varStack);
                    }
                    else if (t1.IsVar)
                    {
                        t1.Unify(new DecimalTerm(t0.To<int>() + 1), varStack: varStack);
                    }
                    else if (!t0.IsInteger || !t1.IsInteger || t0.To<int>() != t1.To<int>() - 1)
                    {
                        return false;
                    }

                    break;

                case BI.functor: // functor( T, F, N)
                    t0 = term.Arg(0);

                    if (t0.IsVar)
                    {
                        t1 = term.Arg(1);

                        if (t1.IsVar) return false;

                        functor = t1.FunctorToString;
                        t2 = term.Arg(2);

                        if (t2.IsNatural)
                            arity = t2.To<int>();
                        else
                            return false;

                        var args = new BaseTerm [arity];

                        for (var i = 0; i < arity; i++) args[i] = new Variable();

                        if (!t0.Unify(CreateNewTerm(t: t2, arity: arity, functor: functor, args: args),
                            varStack: varStack)) return false;

                        break;
                    }
                    else
                    {
                        if (!term.Arg(1).Unify(new AtomTerm(functor: t0.Functor), varStack: varStack)) return false;

                        if (!term.Arg(2).Unify(new DecimalTerm(value: t0.Arity), varStack: varStack)) return false;

                        break;
                    }

                case BI.arg: // arg( N, BaseTerm, A)
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsVar || t1.IsVar) return false;

                    n = t0.To<int>(); // N is 1-based

                    if (n <= 0 || n > t1.Arity) return false;

                    if (!t1.Arg(n - 1).Unify(term.Arg(2), varStack: varStack)) return false;

                    break;

                case BI.abolish: // abolish( X/N)
                    t0 = term.Arg(0);
                    result = true;
                    if (t0.HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom && t0.Arg(1).IsInteger)
                        result = Ps.Abolish(functor: t0.Arg(0).FunctorToString, t0.Arg(1).To<short>());
                    else
                        result = false;
                    if (!result) return false;
                    break;

                case BI.gensym: // gensym( X)
                    if (term.Arity == 1)
                    {
                        t0 = new AtomTerm("v" + gensymInt++);

                        if (t0.Unify(term.Arg(0), varStack: varStack))
                            break;
                        return false;
                    }
                    else
                    {
                        if (!term.Arg(0).IsAtom) return false;

                        t0 = new AtomTerm(term.Arg(0).FunctorToString + gensymInt++);

                        if (t0.Unify(term.Arg(1), varStack: varStack))
                            break;
                        return false;
                    }

                case BI.var:
                    if (!term.Arg(0).IsVar ||
                        term.Arity == 2 &&
                        !term.Arg(1).Unify(new StringTerm(value: term.Arg(0).Name), varStack: varStack))
                        return false;
                    break;

                case BI.nonvar:
                    if (!term.Arg(0).IsVar) break;
                    return false;

                case BI.atom_:
                    if (term.Arg(0).IsAtom) break;
                    return false;

                case BI.atomic:
                    if (term.Arg(0).IsAtomic) break;
                    return false;

                case BI.integer:
                    if (term.Arg(0).IsInteger) break;
                    return false;

                case BI.float_:
                    if (term.Arg(0).IsFloat) break;
                    return false;

                case BI.number:
                    if (term.Arg(0).IsNumber) break;
                    return false;

                case BI.compound:
                    if (term.Arg(0).IsCompound) break;
                    return false;

                case BI.list:
                    if (term.Arg(0).IsProperList) break;
                    return false;

                case BI.string_:
                    if (term.Arg(0).IsString) break;
                    return false;

                case BI.bool_:
                    if (term.Arg(0).IsBool) break;
                    return false;

                case BI.datetime: // datetime/1/4/7
                    t0 = term.Arg(0);

                    if (term.Arity == 1)
                    {
                        if (!t0.IsDateTime) return false;
                    }
                    else if (t0.IsDateTime)
                    {
                        dati = t0.To<DateTime>();

                        if (!term.Arg(1).Unify(new DecimalTerm(value: dati.Year), varStack: varStack) ||
                            !term.Arg(2).Unify(new DecimalTerm(value: dati.Month), varStack: varStack) ||
                            !term.Arg(3).Unify(new DecimalTerm(value: dati.Day), varStack: varStack) ||
                            term.Arity == 7 &&
                            (!term.Arg(4).Unify(new DecimalTerm(value: dati.Hour), varStack: varStack) ||
                             !term.Arg(5).Unify(new DecimalTerm(value: dati.Minute), varStack: varStack) ||
                             !term.Arg(6).Unify(new DecimalTerm(value: dati.Second), varStack: varStack)
                            ))
                            return false;
                    }
                    else if (t0.IsVar)
                    {
                        if (term.Arity == 4)
                            dati = new DateTime(
                                term.Arg(1).To<int>(),
                                term.Arg(2).To<int>(),
                                term.Arg(3).To<int>());
                        else
                            dati = new DateTime(
                                term.Arg(1).To<int>(),
                                term.Arg(2).To<int>(),
                                term.Arg(3).To<int>(),
                                term.Arg(4).To<int>(),
                                term.Arg(5).To<int>(),
                                term.Arg(6).To<int>());

                        if (!t0.Unify(new DateTimeTerm(value: dati), varStack: varStack)) return false;
                    }
                    else
                    {
                        IO.Error("datetime/4/7: first argument must be either a DateTime or a var");

                        return false;
                    }

                    break;

                case BI.timespan:
                    t0 = term.Arg(0);

                    if (term.Arity == 1)
                    {
                        if (!t0.IsTimeSpan) return false;
                    }
                    else if (t0.IsTimeSpan)
                    {
                        ti = t0.To<TimeSpan>();

                        if (!term.Arg(4).Unify(new DecimalTerm(value: ti.Hours), varStack: varStack) ||
                            !term.Arg(5).Unify(new DecimalTerm(value: ti.Minutes), varStack: varStack) ||
                            !term.Arg(6).Unify(new DecimalTerm(value: ti.Seconds), varStack: varStack)
                        )
                            return false;
                    }
                    else if (t0.IsVar)
                    {
                        ti = new TimeSpan(
                            term.Arg(1).To<int>(),
                            term.Arg(2).To<int>(),
                            term.Arg(3).To<int>());

                        if (!t0.Unify(new TimeSpanTerm(value: ti), varStack: varStack)) return false;
                    }
                    else
                    {
                        IO.Error("timespan/4: first argument must be either a TimeSpan or a var");

                        return false;
                    }

                    break;

                case BI.is_: // X is Y
                    t0 = term.Arg(1).Eval();
                    if (term.Arg(0).Unify(t: t0, varStack: varStack)) break;
                    return false;

                case BI.ne_uni: // X \= Y
                    if (term.Arg(0).Unify(term.Arg(1), varStack: varStack)) return false;
                    break;

                case BI.eq_num: // X =:=
                    if (term.Arg<decimal>(0) == term.Arg<decimal>(1)) break;
                    return false;

                case BI.ne_num: // X =\= Y
                    if (term.Arg<decimal>(0) != term.Arg<decimal>(1)) break;
                    return false;

                case BI.lt_num: // X < Y
                    if (term.Arg<decimal>(0) < term.Arg<decimal>(1)) break;
                    return false;

                case BI.le_num: // X =< Y
                    if (term.Arg<decimal>(0) <= term.Arg<decimal>(1)) break;
                    return false;

                case BI.gt_num: // X > Y
                    if (term.Arg<decimal>(0) > term.Arg<decimal>(1)) break;
                    return false;

                case BI.ge_num: // X >= Y
                    if (term.Arg<decimal>(0) >= term.Arg<decimal>(1)) break;
                    return false;

                case BI.eq_str: // X == Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) == 0) break;
                    return false;

                case BI.ne_str: // X \== Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) != 0) break;
                    return false;

                case BI.lt_ord: // X @< Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) < 0) break;
                    return false;

                case BI.le_ord: // X @=< Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) <= 0) break;
                    return false;

                case BI.gt_ord: // X @> Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) > 0) break;
                    return false;

                case BI.ge_ord: // X @>= Y
                    if (term.Arg(0).CompareTo(term.Arg(1)) >= 0) break;
                    return false;

                case BI.univ: // X =.. Y
                    t0 = term.Arg(0);

                    if (t0.IsVar
                    ) // create a function or operator representation of the term rhs, and bind that to the lhs
                    {
                        t1 = term.Arg(1);

                        if (t1.IsVar || !t1.IsProperList) return false;

                        if (t1.Arg(0).IsVar) return false; // not a valid functor

                        functor = t1.Arg(0).FunctorToString.ToAtom();
                        // convert rest of term to arguments: calculate arity first
                        t1 = t1.Arg(1);
                        arity = 0;
                        t2 = t1;

                        while (t2.Arity == 2)
                        {
                            arity++;
                            t2 = t2.Arg(1);
                        }

                        // create arguments
                        var args = new BaseTerm [arity];

                        for (var i = 0; i < arity; i++)
                        {
                            args[i] = t1.Arg(0);
                            t1 = t1.Arg(1);
                        }

                        t0.Unify(CreateNewTerm(t: t1, arity: arity, functor: functor, args: args), varStack: varStack);

                        break;
                    }
                    else // create a list representation of the lhs and unify that with the rhs
                    {
                        arity = t0.Arity;
                        var args = new BaseTerm [arity];
                        t1 = BaseTerm.EMPTYLIST;

                        for (var i = arity; i > 0; i--)
                            t1 = new ListTerm(t0.Arg(i - 1), t1: t1); // [arg1, arg2, ...]

                        t1 = new ListTerm(new AtomTerm(value: t0.FunctorToString),
                            t1: t1); // [functor, arg1, arg2, ...]

                        if (!t1.Unify(term.Arg(1), varStack: varStack)) return false;

                        break;
                    }

                case BI.unifiable: // X can be unified with Y, but without variable bindings
                    if (!term.Arg(0).IsUnifiableWith(term.Arg(1), varStack: varStack)) return false;
                    break;

                #region IO

                #region Reading

                case BI.fileexists:
                    t0 = term.Arg(0);

                    fileName = Utils.FileNameFromTerm(t: t0, ".pl");

                    if (fileName == null || !File.Exists(path: fileName)) return false;
                    break;

                case BI.see: // see( F)
                    t0 = term.Arg(0);

                    if (!t0.IsAtomOrString)
                        IO.Error("see/1 argument must be an atom or a string");

                    if (t0.HasFunctor("user"))
                    {
                        currentFileReader = null;

                        break;
                    }

                    if (t0 is FileReaderTerm) // functor previously saved with seeing/1
                    {
                        currentFileReader = (FileReaderTerm) t0;

                        break;
                    }

                    if (t0.HasFunctor("user"))
                    {
                        currentFileReader = null;

                        break;
                    }

                    currentInputName = Utils.FileNameFromTerm(t: t0, ".pl");
                    currentFileReader = (FileReaderTerm) openFiles.GetFileReader(fileName: currentInputName);

                    if (currentFileReader == null)
                    {
                        currentFileReader = new FileReaderTerm(this, fileName: currentInputName);
                        openFiles.Add(key: currentInputName, value: currentFileReader);
                        currentFileReader.Open();
                    }

                    break;

                case BI.seeing:
                    if (currentFileReader == null ||
                        !term.Arg(0).Unify(t: currentFileReader, varStack: varStack)) return false;

                    break;

                case BI.read: // read( ?Term)
                    t0 = ReadTerm();

                    if (!term.Arg(0).Unify(t: t0, varStack: varStack)) return false;

                    break;

                case BI.readatoms: // readatoms( ?List)
                    var line = ReadLine();

                    if (string.IsNullOrEmpty(line = line.Trim()))
                    {
                        t0 = BaseTerm.EMPTYLIST;
                    }
                    else
                    {
                        var words = line.Tokens();
                        var terms = new BaseTerm [words.Length];

                        for (var i = 0; i < words.Length; i++)
                            terms[i] = TermFromWord(words[i]);

                        t0 = ListTerm.ListFromArray(ta: terms);
                    }

                    if (!term.Arg(0).Unify(t: t0, varStack: varStack)) return false;

                    break;

                case BI.readatom: // readatom( A)
                    t0 = TermFromWord(ReadLine());

                    if (!term.Arg(0).Unify(t: t0, varStack: varStack)) return false;

                    break;

                case BI.readln: // readln( L)
                    line = ReadLine();

                    if (line == null || !term.Arg(0).Unify(new StringTerm(value: line), varStack: varStack))
                        return false;

                    break;

                case BI.readeof: // readeof( +F, ?T) -- unify the entire contents of file F with string T
                    if ((t0 = term.Arg(0)).IsVar) return false;

                    x = Utils.FileNameFromTerm(t: t0, ".txt");
                    string fileContents = null;

                    try
                    {
                        fileContents = File.ReadAllText(path: x);
                    }
                    catch (Exception e)
                    {
                        IO.Error("Error reading file {0}. Message was:\r\n{1}", x, e.Message);
                    }

                    if (!term.Arg(1).Unify(new StringTerm(value: fileContents), varStack: varStack)) return false;

                    break;

                case BI.get0: // get0( C): any character
                    n = ReadChar();

                    if (!term.Arg(0).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;

                    break;

                case BI.get: // get( C): skip non-printables
                    while (true)
                    {
                        n = ReadChar();

                        if (!char.IsControl((char) n)) break; // break if printable
                    }

                    if (!term.Arg(0).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;

                    break;

                case BI.seen:
                    if (currentFileReader != null)
                    {
                        currentFileReader.Close();
                        openFiles.Remove(key: currentInputName);
                    }

                    currentFileReader = null;

                    break;

                #endregion Reading

                #region Writing

                case BI.tell: // tell( F)
                    t0 = term.Arg(0);

                    if (!t0.IsAtomOrString)
                        IO.Error("tell/1 argument must be an atom or a string");

                    if (t0.HasFunctor("user"))
                    {
                        currentFileWriter = null;

                        break;
                    }

                    currentOutputName = Utils.FileNameFromTerm(t: t0, ".pl");
                    currentFileWriter = (FileWriterTerm) openFiles.GetFileWriter(fileName: currentOutputName);

                    if (currentFileWriter == null)
                    {
                        currentFileWriter = new FileWriterTerm(this, fileName: currentOutputName);
                        openFiles.Add(key: currentOutputName, value: currentFileWriter);
                        currentFileWriter.Open();
                    }

                    break;

                case BI.telling:
                    if (currentFileWriter == null ||
                        !term.Arg(0).Unify(t: currentFileWriter, varStack: varStack)) return false;

                    break;

                case BI.write:
                    Write(term.Arg(0), true);
                    break;

                case BI.writeln: // writeln( X)
                    Write(term.Arg(0), true);
                    NewLine();
                    break;

                case BI.writef: // writef( X, L) // formatted write, L last
                    string ln = null;
                    goto case BI.writelnf;
                case BI.writelnf: // writef( X, L) // formatted writeln, L last
                    ln = "ln";
                    if (!(term.Arg(0) is StringTerm))
                        IO.Error("First argument of write(0}f/2 must be a string", ln);

                    if (!(term.Arg(1) is ListTerm))
                        IO.Error("Second argument of write{0}f/2 must be a list", ln);

                    var fs = Utils.Format(term.Arg(0), term.Arg(1));

                    if (fs == null) return false;

                    Write(s: fs);

                    if (term.FunctorToString == "writelnf") NewLine();

                    break;

                case BI.put: // put( C)
                    n = term.Arg<int>(0);
                    Write(((char) n).ToString());
                    break;

                case BI.nl:
                    NewLine();
                    break;

                case BI.tab: // tab( +N)
                    n = term.Arg<int>(0);

                    if (n > 0) Write(Spaces(n: n));
                    break;

                case BI.print: // print( X)
                    Write(term.Arg(0), true);
                    break;

                case BI.treeprint: //
                    term.Arg(0).TreePrint(0, this);
                    break;

                case BI.display:
                    Write(term.Arg(0).ToDisplayString(), false);
                    NewLine();
                    break;

                case BI.told:
                    if (currentFileWriter != null)
                    {
                        currentFileWriter.Close();
                        openFiles.Remove(key: currentOutputName);
                    }

                    currentFileWriter = null;
                    break;

                case BI.console:
                    if (term.Arity == 2 && !(term.Arg(0) is StringTerm))
                        IO.Error("First argument of console/1/2 must be a string");

                    if (term.Arity == 2)
                    {
                        if (!(term.Arg(1) is ListTerm))
                            IO.Error("Second argument of console/2 must be a list");

                        a = Utils.Format(term.Arg(0), term.Arg(1));
                        IO.WriteLine(s: a);
                    }
                    else
                    {
                        IO.WriteLine("{0}", term.Arg(0));
                    }

                    break;

                case BI.maxwritedepth:
                    t0 = term.Arg(0);

                    if (t0.IsVar)
                        term.Arg(0).Unify(new DecimalTerm(value: maxWriteDepth), varStack: varStack);
                    else if (t0.IsNatural)
                        maxWriteDepth = t0.To<int>();
                    else
                        return false;

                    break;

                case BI.cls:
                    IO.ClearScreen();
                    break;

                case BI.showfile:
                    t0 = term.Arg(0);
                    fileName = Utils.FileNameFromTerm(t: t0, ".pl");

                    if (fileName == null || !File.Exists(path: fileName)) return false;

                    IO.WriteLine(File.ReadAllText(path: fileName));
                    break;

                #endregion Writing

                #endregion IO

                #region SQL



                #endregion SQL


                case BI.between:
                    IntRangeTerm irt;

                    if (term.Arg(0).IsVar) // first Call only, Arg(0) contains State info
                    {
                        t1 = term.Arg(1);
                        t2 = term.Arg(2);
                        var inf = t2.HasFunctor("inf") || t2.HasFunctor("infinity"); // stolen from SWI

                        if (term.OneOfArgsIsVar(1, 2)) return false;

                        if (!t1.IsInteger || !(t2.IsInteger || inf)) return false;

                        irt = new IntRangeTerm(lowBound: t1, inf ? new DecimalTerm(value: int.MaxValue) : t2);
                        term.Arg(0).Unify(t: irt, varStack: varStack);

                        break;
                    }

                    irt = (IntRangeTerm) term.Arg(0);
                    DecimalTerm dt;

                    if (!irt.GetNextValue(dt: out dt) ||
                        !term.Arg(3).Unify(t: dt, varStack: varStack)) // done
                    {
                        term.SetArg(0, t: BaseTerm.VAR);

                        return false;
                    }

                    break;

                case BI.current_op: // current_op( ?Precedence, ?Assoc, ?Functor)
                    IEnumerator<OperatorDescr> iEnum = null;
                    t0 = term.Arg(0);

                    if (t0.IsVar) // first call only, Arg(0) contains State info
                    {
                        iEnum = OpTable.GetEnumerator();
                        term.Arg(0).Unify(new UserClassTerm<IEnumerator<OperatorDescr>>(obj: iEnum),
                            varStack: varStack);

                        break;
                    }

                    iEnum = ((UserClassTerm<IEnumerator<OperatorDescr>>) t0).UserObject;

                    while (true)
                    {
                        if (!iEnum.MoveNext())
                        {
                            term.SetArg(0, t: BaseTerm.VAR);

                            return false;
                        }

                        var opDescr = iEnum.Current;

                        DecimalTerm it;
                        AtomTerm at, nt;

                        if (term.Arg(1).IsUnifiableWith(it = new DecimalTerm(value: opDescr.Prec),
                                varStack: varStack) &&
                            term.Arg(2).IsUnifiableWith(at = new AtomTerm(opDescr.Assoc.ToString()),
                                varStack: varStack) &&
                            term.Arg(3).IsUnifiableWith(nt = new AtomTerm(value: opDescr.Name), varStack: varStack))
                        {
                            term.Arg(1).Unify(t: it, varStack: varStack);
                            term.Arg(2).Unify(t: at, varStack: varStack);
                            term.Arg(3).Unify(t: nt, varStack: varStack);

                            break;
                        }
                    }

                    break;

                case BI.atom_string: // atom_string( ?A, ?S)
                    t1 = term.Arg(1);

                    if (t1.IsVar) // create a list containing A's characters or character codes
                    {
                        t0 = term.Arg(0);

                        if (!t0.IsAtomic) return false;

                        t2 = NewIsoOrCsStringTerm(t0.FunctorToString.Dequoted());

                        if (!t1.Unify(t: t2, varStack: varStack)) return false;
                    }
                    else // t1 is string
                    {
                        if (t1.IsProperList)
                        {
                            var sb = new StringBuilder();

                            while (t1.Arity == 2)
                            {
                                t2 = t1.Arg(0);

                                if (!t2.IsInteger) return false;

                                sb.Append((char) t2.To<int>());
                                t1 = t1.Arg(1);
                            }

                            if (!term.Arg(0).Unify(TermFromWord(sb.ToString()), varStack: varStack)) return false;
                        }
                        else if (t1.IsString && (a = t1.FunctorToString).Length > 0)
                        {
                            if (!term.Arg(0).Unify(TermFromWord(a.Dequoted()), varStack: varStack)) return false;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    break;

                case BI.string_term: // string_term( ?S, ?T) -- convert string S to Prolog term T and v.v.
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsString)
                    {
                        var p = new PrologParser(this);
                        p.StreamIn = "&reading\r\n" + t0.FunctorToString.AddEndDot();

                        if (!t1.Unify(t: p.ReadTerm, varStack: varStack)) return false;
                    }
                    else if (!t0.Unify(new StringTerm(t1.ToString()), varStack: varStack))
                    {
                        return false;
                    }

                    break;

                case BI.string_words:
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsString)
                    {
                        ListTerm list;
                        line = ((StringTerm) t0).Value;

                        if (string.IsNullOrEmpty(line = line.Trim()))
                        {
                            list = BaseTerm.EMPTYLIST;
                        }
                        else
                        {
                            var words = line.Tokens();
                            var terms = new BaseTerm [words.Length];

                            for (var i = 0; i < words.Length; i++)
                                terms[i] = TermFromWord(words[i]);

                            list = ListTerm.ListFromArray(ta: terms);
                        }

                        if (list.Unify(t: t1, varStack: varStack)) break;
                    }
                    else if (t1.IsProperList)
                    {
                        var sb = new StringBuilder();
                        var first = true;

                        foreach (BaseTerm t in (ListTerm) t1)
                        {
                            if (first) first = false;
                            else sb.Append(' ');

                            sb.Append(value: t);
                        }

                        if (t0.Unify(new StringTerm(sb.ToString()), varStack: varStack)) break;
                    }

                    return false;

                case BI.stringstyle:
                    t0 = term.Arg(0);
                    if (t0.IsVar)
                        t0.Unify(new AtomTerm(csharpStrings ? "csharp" : "iso"), varStack: varStack);
                    else
                        SetStringStyle(t: t0);
                    break;

                case BI.name: // name( ?A, ?L)
                    t1 = term.Arg(1);

                    if (t1.IsVar) // create a list containing atom A's characters or character codes
                    {
                        t0 = term.Arg(0);

                        if (!t0.IsAtomic) return false;

                        var chars = t0.FunctorToString.Dequoted("'").ToCharArray();
                        t0 = BaseTerm.EMPTYLIST;

                        for (var i = chars.Length - 1; i >= 0; i--)
                        {
                            t2 = new DecimalTerm(chars[i]);
                            t0 = new ListTerm(t0: t2, t1: t0);
                        }

                        t1.Unify(t: t0, varStack: varStack);
                    }
                    else
                    {
                        if (t1.IsProperList)
                        {
                            var sb = new StringBuilder();

                            while (t1.Arity == 2)
                            {
                                t2 = t1.Arg(0);

                                if (!t2.IsInteger) return false;

                                sb.Append((char) t2.To<int>());
                                t1 = t1.Arg(1);
                            }

                            a = sb.ToString().ToAtomic(type: out type);

                            if (type == TermType.Number)
                                t2 = new DecimalTerm(int.Parse(s: a));
                            else if (type == TermType.String)
                                t2 = NewIsoOrCsStringTerm(s: a);
                            else
                                t2 = new AtomTerm(value: a);

                            if (!term.Arg(0).Unify(t: t2, varStack: varStack)) return false;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    break;

                case BI.expand_term: // expand_term( +(P-->Q), -R)
                    t0 = term.Arg(0); // P-->Q
                    t1 = term.Arg(1); // R
                    var head = t0.Arg(0);
                    var body = t0.Arg(1).ToDCG(lhs: ref head);
                    t2 = new ClauseTerm(new ClauseNode(t: head, body: body)).Copy();

                    if (!t1.Unify(t: t2, varStack: varStack)) return false;
                    break;

                case BI.numbervars: // numbervars(+X, +B, -E)
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);
                    t2 = term.Arg(2);

                    if (!t1.IsInteger || !t2.IsVar) return false;

                    var k = t1.To<int>();
                    t0.NumberVars(k: ref k, s: varStack);
                    t2.Unify(new DecimalTerm(value: k), varStack: varStack);
                    break;

                case BI.format: // format/3
                    fs = Utils.Format(term.Arg(0), term.Arg(1));

                    if (fs == null) return false;

                    if (!term.Arg(2).Unify(new StringTerm(value: fs), varStack: varStack)) return false;
                    break;
                
                case BI.predicatePN: // predicate( +P/N)
                    t0 = term.Arg(0);
                    result = true;

                    if (t0.HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom && t0.Arg(1).IsInteger)
                        result = Ps.IsPredicate(functor: t0.Arg(0).FunctorToString, t0.Arg<int>(1));
                    else
                        result = false;
                    if (!result)
                        return false;
                    break;

                case BI.predicateX: // predicate( +T)
                    t0 = term.Arg(0);

                    if (t0.IsVar || !Ps.IsPredicate(functor: t0.FunctorToString, arity: t0.Arity)) return false;
                    break;

                // term_pattern( T, P, Dmin, Dmax)
                // find pattern P in term T between depths Dmin and Dmax (incl), and unify result with P
                case BI.term_pattern:
                    t0 = term.Arg(0); // State
                    t1 = term.Arg(1); // term

                    if (t1.IsVar)
                        IO.Error("term_pattern: uninstantiated first argument not allowed");

                    t2 = term.Arg(2); // pattern
                    t3 = term.Arg(3); // Dmin (0 if var)
                    t4 = term.Arg(4); // Dmax (inf if var)
                    t5 = term.Arg(5); // Path ('!' if not wanted)
                    var skipVars = true; // i.e. the search pattern will not match a term variable in t

                    NodeIterator iterable = null;

                    if (t0.IsVar) // first call only, Arg(0) contains State info
                    {
                        iterable = new NodeIterator(root: t1, pattern: t2, minLenTerm: t3, maxLenTerm: t4,
                            skipVars: skipVars, path: t5, varStack: varStack);
                        term.Arg(0).Unify(new UserClassTerm<NodeIterator>(obj: iterable), varStack: varStack);

                        break;
                    }

                    iterable = ((UserClassTerm<NodeIterator>) t0).UserObject;

                    if (!iterable.MoveNext()) // if success, pattern gets bound (and its vars get instantiated)
                    {
                        term.SetArg(0, t: BaseTerm.VAR);

                        return false;
                    }

                    break;

                case BI.ground: // ground( +T)
                    if (!term.Arg(0).IsGround) return false;
                    break;

                case BI.throw_: // throw( [+C,] +T [,+L])
                    t0 = term.Arg(0);
                    t1 = null;
                    string exceptionClass = null;
                    string exceptionMessage;

                    if (term.Arity == 2)
                    {
                        if (t0 is AtomTerm || t0 is DecimalTerm) // exception class
                        {
                            exceptionClass = t0.FunctorToString;
                            t0 = term.Arg(1);
                            t1 = term.Arity == 2 ? null : term.Arg(2);
                        }
                        else
                        {
                            t1 = term.Arg(1);
                        }
                    }
                    else if (term.Arity == 3) // something is wrong
                    {
                        IO.Error("First argument of throw/3 ({0}) is not an atom or an integer", t0);
                    }

                    if (!(t0 is StringTerm))
                        IO.Error("Throw/3: string expected instead of '{0}'", t0);

                    exceptionMessage = t1 == null ? t0.FunctorToString : Utils.Format(t: t0, args: t1);
                    Throw(exceptionClass: exceptionClass, exceptionMessage: exceptionMessage);
                    break;


                case BI.today: // date( ?Y, ?M, ?D)
                    y = DateTime.Today.Year;
                    m = DateTime.Today.Month;
                    d = DateTime.Today.Day;

                    if (!term.Arg(0).Unify(new DecimalTerm(value: y), varStack: varStack)) return false;

                    if (!term.Arg(1).Unify(new DecimalTerm(value: m), varStack: varStack)) return false;

                    if (!term.Arg(2).Unify(new DecimalTerm(value: d), varStack: varStack)) return false;
                    break;

                case BI.now: // time( ?H, ?M, ?S)
                    h = DateTime.Now.Hour;
                    m = DateTime.Now.Minute;
                    s = DateTime.Now.Second;

                    if (!term.Arg(0).Unify(new DecimalTerm(value: h), varStack: varStack)) return false;

                    if (!term.Arg(1).Unify(new DecimalTerm(value: m), varStack: varStack)) return false;

                    if (!term.Arg(2).Unify(new DecimalTerm(value: s), varStack: varStack)) return false;
                    break;

                case BI.validdate: // validdate( +Y, +M, +D)
                    t0 = term.Arg(0);

                    if (t0.IsInteger) y = t0.To<int>();
                    else return false;

                    t1 = term.Arg(1);

                    if (t1.IsInteger) m = t1.To<int>();
                    else return false;

                    t2 = term.Arg(2);

                    if (t2.IsInteger) d = t2.To<int>();
                    else return false;

                    try
                    {
                        new DateTime(year: y, month: m, day: d);
                    }
                    catch
                    {
                        return false;
                    }

                    break;

                case BI.validtime: // validtime( +H, +M, +S)
                    t0 = term.Arg(0);

                    if (t0.IsInteger) h = t0.To<int>();
                    else return false;

                    t1 = term.Arg(1);

                    if (t1.IsInteger) m = t1.To<int>();
                    else return false;

                    t2 = term.Arg(2);

                    if (t2.IsInteger) s = t2.To<int>();
                    else return false;

                    try
                    {
                        new DateTime(2000, 1, 1, hour: h, minute: m, second: s);
                    }
                    catch
                    {
                        return false;
                    }

                    break;

                case BI.string_datetime: // convert a string to a DateTime term
                    t0 = term.Arg(0);

                    if (term.Arity > 2)
                    {
                        if (!t0.IsString || !DateTime.TryParse(s: t0.FunctorToString, result: out dati))
                        {
                            IO.Error("string_datetime: invalid date format: '{0}' for first argument", t0);

                            return false;
                        }

                        if (!term.Arg(1).Unify(new DecimalTerm(value: dati.Year), varStack: varStack) ||
                            !term.Arg(2).Unify(new DecimalTerm(value: dati.Month), varStack: varStack) ||
                            !term.Arg(3).Unify(new DecimalTerm(value: dati.Day), varStack: varStack) ||
                            term.Arity == 7 &&
                            (!term.Arg(4).Unify(new DecimalTerm(value: dati.Hour), varStack: varStack) ||
                             !term.Arg(5).Unify(new DecimalTerm(value: dati.Minute), varStack: varStack) ||
                             !term.Arg(6).Unify(new DecimalTerm(value: dati.Second), varStack: varStack)
                            ))
                            return false;
                    }
                    else
                    {
                        t1 = term.Arg(1);

                        if (t0.IsString)
                        {
                            if (DateTime.TryParse(s: t0.FunctorToString, result: out dati))
                            {
                                if (!t1.Unify(new DateTimeTerm(value: dati), varStack: varStack))
                                    return false;
                            }
                            else
                            {
                                IO.Error("string_datetime: error while parsing first argument: '{0}'", t0);

                                return false;
                            }
                        }
                        else if (t0.IsVar)
                        {
                            if (t1.IsDateTime)
                            {
                                if (!t0.Unify(new StringTerm(value: t1.FunctorToString), varStack: varStack))
                                    return false;
                            }
                            else
                            {
                                IO.Error("string_datetime: second argument is not a DateTime term: '{0}'", t0);

                                return false;
                            }
                        }
                        else
                        {
                            IO.Error("string_datetime: first argument not a string or var: '{0}'", t0);

                            return false;
                        }
                    }

                    break;

                case BI.date_part: // get the date part of a DateTime (time set to 00:00:00)
                    t0 = term.Arg(0);

                    if (!t0.IsDateTime)
                    {
                        IO.Error("date_part: first argument not a DateTime: '{0}'", t0);

                        return false;
                    }

                    if (!term.Arg(1).Unify(new DateTimeTerm(value: t0.To<DateTime>().Date), varStack: varStack))
                        return false;

                    break;

                case BI.time_part: // get the time part of a DateTime (time set to 00:00:00)
                    t0 = term.Arg(0);

                    if (!t0.IsDateTime)
                    {
                        IO.Error("date_part: first argument not a DateTime: '{0}'", t0);

                        return false;
                    }

                    if (!term.Arg(1).Unify(new TimeSpanTerm(value: t0.To<DateTime>().TimeOfDay), varStack: varStack))
                        return false;

                    break;

                case BI.dayname: // dayname( +Y, +M, +D, ?N)
                    DayOfWeek dow;
                    t0 = term.Arg(0);

                    if (t0.IsInteger) y = t0.To<int>();
                    else return false;

                    t1 = term.Arg(1);

                    if (t1.IsInteger) m = t1.To<int>();
                    else return false;

                    t2 = term.Arg(2);

                    if (t2.IsInteger) d = t2.To<int>();
                    else return false;

                    try
                    {
                        dow = new DateTime(year: y, month: m, day: d).DayOfWeek;
                    }
                    catch
                    {
                        return false;
                    }

                    if (!term.Arg(3).Unify(new StringTerm(dow.ToString("G")), varStack: varStack)) return false;
                    break;

                case BI.dayofweek: // dayofweek( +Y, +M, +D, ?N)
                    t0 = term.Arg(0);

                    if (t0.IsInteger) y = t0.To<int>();
                    else return false;

                    t1 = term.Arg(1);

                    if (t1.IsInteger) m = t1.To<int>();
                    else return false;

                    t2 = term.Arg(2);

                    if (t2.IsInteger) d = t2.To<int>();
                    else return false;

                    try
                    {
                        n = (int) new DateTime(year: y, month: m, day: d).DayOfWeek;
                    }
                    catch
                    {
                        return false;
                    }

                    if (!term.Arg(3).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;
                    break;

                case BI.dayofyear: // dayofyear( +Y, +M, +D, ?N)
                    t0 = term.Arg(0);

                    if (t0.IsInteger) y = t0.To<int>();
                    else return false;

                    t1 = term.Arg(1);

                    if (t1.IsInteger) m = t1.To<int>();
                    else return false;

                    t2 = term.Arg(2);

                    if (t2.IsInteger) d = t2.To<int>();
                    else return false;

                    try
                    {
                        n = new DateTime(year: y, month: m, day: d).DayOfYear;
                    }
                    catch
                    {
                        IO.Error("dayofyear: Invalid date (Y:{0} M:{1} D:{2})", y, m, d);

                        return false;
                    }

                    if (!term.Arg(3).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;

                    break;


                case BI.leapyear: // leapyear( +Y)
                    t0 = term.Arg(0);

                    if (t0.IsInteger) y = t0.To<int>();
                    else return false;

                    if (!DateTime.IsLeapYear(year: y)) return false;

                    break;


                case BI.weekno: // weekno(+Y, +M, +D, ?N) // week Number of date Y-M-D, or current week Number
                    if (term.Arity == 4)
                    {
                        t0 = term.Arg(0);

                        if (t0.IsInteger) y = t0.To<int>();
                        else return false;

                        t1 = term.Arg(1);

                        if (t1.IsInteger) m = t1.To<int>();
                        else return false;

                        t2 = term.Arg(2);

                        if (t2.IsInteger) d = t2.To<int>();
                        else return false;

                        try
                        {
                            n = Utils.WeekNo(new DateTime(year: y, month: m, day: d));
                        }
                        catch // invalid date
                        {
                            IO.Error("weekno: Invalid date (Y:{0} M:{1} D:{2})", y, m, d);

                            return false;
                        }
                    }
                    else
                    {
                        n = Utils.WeekNo(date: DateTime.Today);
                    }

                    if (!term.Arg(term.Arity - 1).Unify(new DecimalTerm(value: n), varStack: varStack)) return false;

                    break;


                case BI.flat: // flat( +X, ?Y)
                    t0 = term.Arg(0);

                    if (!t0.IsProperOrPartialList) return false;

                    if (!term.Arg(1).Unify(((ListTerm) t0).FlattenList(), varStack: varStack)) return false;

                    break;


                case BI.dcg_flat: // dcg_flat( +X, ?Y)
                    t0 = term.Arg(0);

                    if (!t0.IsDcgList) return false;

                    if (!term.Arg(1).Unify(((DcgTerm) t0).FlattenDcgList(), varStack: varStack)) return false;

                    break;


                case BI.append2: // conc2( +X, +Y, ?Z), X proper or partial t, Y anything
                    t0 = term.Arg(0);

                    if (!t0.IsProperOrPartialList) return false;

                    if (!term.Arg(2).Unify(((ListTerm) t0).Append(term.Arg(1)), varStack: varStack))
                        return false;

                    break;
                
                case BI.json_term: // json_term( ?J, ?T) converts between JSON file/string and Prolog representation
                    var indentDelta = 2;
                    var maxIndentLevel = int.MaxValue;
                    var noCommas = false;
                    var noQuotes = false;
                    JsonTerm jt = null;

                    if (term.Arity == 3)
                        predicateCallOptions.Set(term.Arg(2));

                    t0 = term.Arg(0);
                    t1 = term.Arg(1);

                    if (t0.IsVar)
                    {
                        if (t1 is JsonTerm)
                        {
                            jt = (JsonTerm) t1;
                        }
                        else
                        {
                            if (t1.IsProperList)
                                jt = new JsonTerm((ListTerm) t1);
                            else
                                IO.Error(
                                    "json_term/2/3 -- second argument cannot be converted to a JSON-structure:\r\n'{0}'",
                                    t1);
                        }

                        predicateCallOptions.Get("indent", 1, value: ref indentDelta);
                        predicateCallOptions.Get("indent", 2, value: ref maxIndentLevel);
                        noCommas = predicateCallOptions.Get("nocommas");
                        noQuotes = predicateCallOptions.Get("noquotes");

                        if (!t0.Unify(
                            new StringTerm(jt.ToJsonString(indentDelta: indentDelta, maxIndentLevel: maxIndentLevel,
                                noCommas: noCommas, noQuotes: noQuotes)),
                            varStack: varStack)) return false;
                        break;
                    }
                    else // t0 contains string in JSON-format (or file name of JSON text file)
                    {
                        //if (options != null)
                        //  jt.Atomize = options.Get ("atomize");

                        x = t0.FunctorToString;
                        inFile = t0.Arity == 1 &&
                                 x == "see"; // is it the functor of a source file containing the XML structure?
                        outFile = t0.Arity == 1 &&
                                  x == "tell"; // ... or the functor of a destination file containing the XML structure?

                        if (inFile || outFile)
                        {
                            x = Utils.FileNameFromTerm(t0.Arg(0), ".json");

                            if (x == null) return false;
                        }

                        if (outFile)
                        {
                            if (t1 is JsonTerm)
                            {
                                jt = (JsonTerm) t1;
                            }
                            else
                            {
                                if (t1.IsProperList)
                                    jt = new JsonTerm((ListTerm) t1);
                                else
                                    IO.Error(
                                        "json_term/2/3 -- second argument cannot be converted to a JSON-structure:\r\n'{0}'",
                                        t1);
                            }

                            predicateCallOptions.Get("indent", 1, value: ref indentDelta);
                            predicateCallOptions.Get("indent", 2, value: ref maxIndentLevel);
                            noCommas = predicateCallOptions.Get("nocommas");
                            noQuotes = predicateCallOptions.Get("noquotes");

                            File.WriteAllText(path: x,
                                jt.ToJsonString(indentDelta: indentDelta, maxIndentLevel: maxIndentLevel,
                                    noCommas: noCommas, noQuotes: noQuotes));
                        }
                        else // parse JSON-string into JsonTerm
                        {
                            var p = new JsonParser();
                            p.OpTable = OpTable;

                            if (inFile)
                                p.StreamIn = File.ReadAllText(path: x);
                            else
                                p.StreamIn = x;

                            if (!t1.Unify(t: p.JsonListTerm, varStack: varStack)) return false;
                        }

                        break;
                    }


                case BI.listing: // listing
                    if (!Ps.ListAll(null, -1, false, true)) return false; // i.e. no predefined, all user

                    break;


                case BI.crossref: // create cross reference table and write it to a spreadsheet
                    t0 = term.Arg(0);

                    if (t0.IsAtomOrString)
                    {
                        fileName = Utils.FileNameFromTerm(t: t0, ".csv");

                        if (fileName == null) return false;

                        IO.Write("\r\n--- Creating cross reference spreadsheet '{0}' ... ", fileName);
                        Ps.CrossRefTableToSpreadsheet(fileName: fileName);
                        IO.WriteLine("Done");

                        break;
                    }

                    return IO.Error("Illegal spreadsheet file name '{0}'", t0.Arg(0));


                case BI.profile:
                    SetProfiling(true);
                    break;


                case BI.noprofile:
                    SetProfiling(false);
                    break;


                case BI.showprofile:
                    if (!profiling)
                    {
                        IO.Message("Profiling is not on. Use profile/0 to switch it on");

                        return false;
                    }

                    if (term.Arity == 0)
                    {
                        Ps.ShowProfileCounts(maxEntry: int.MaxValue);
                    }
                    else
                    {
                        t0 = term.Arg(0);

                        if (t0.IsNatural)
                            Ps.ShowProfileCounts(t0.To<int>());
                        else
                            return IO.Error("Argument for profile/0 must be a positive integer value");
                    }

                    break;


                case BI.clearprofile:
                    Ps.ClearProfileCounts();
                    break;


                case BI.workingdir: // workingdir[( ?D)] -- gets or sets the working directory
                    if (!ConfigSettings.SetWorkingDirectory(term: term, varStack: varStack)) return false;

                    break;


                case BI.listingXN: // listing( X/N)
                    t0 = term.Arg(0);
                    result = true;

                    if (t0.HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom && t0.Arg(1).IsInteger)
                        result = Ps.ListAll(functor: t0.Arg(0).FunctorToString, t0.Arg<int>(1), false, true);
                    else
                        result = false;

                    if (!result) return false;

                    break;


                case BI.listingX: // listing( X) -- t all predicates X/N (i.e. for each N)
                    t0 = term.Arg(0);

                    if (!t0.IsAtom) return false;

                    if (!Ps.ListAll(functor: t0.FunctorToString, -1, false, true)) return false;

                    break;

                case BI.listing0: // listing0
                    if (!Ps.ListAll(null, -1, true, false)) return false; // i.e. no user, all predefined

                    break;


                case BI.listing0XN: // listing0( X/N)
                    t0 = term.Arg(0);
                    result = true;

                    if (t0.HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom && t0.Arg(1).IsInteger)
                        result = Ps.ListAll(functor: t0.Arg(0).FunctorToString, t0.Arg<int>(1), true, false);
                    else
                        result = false;

                    if (!result) return false;

                    break;


                case BI.listing0X: // listing0( X)
                    t0 = term.Arg(0);

                    if (!t0.IsAtom) return false;

                    if (!Ps.ListAll(functor: t0.FunctorToString, -1, true, false)) return false;

                    break;


                //case BI.pp_defines: // pp_defines( X) -- preprocessor symbol definitions -- mainly useful for debugging in nested calls
                //  t0 = term.Arg (0);
                //  if (!t0.IsVar) return false;
                //  t1 = ListTerm.EMPTYLIST;
                //  //IO.WriteLine ("PrologParser.PpSymbols.count = {0}", PrologParser.PpSymbols.Count);
                //  foreach (DictionaryEntry de in PrologParser.PpSymbols)
                //    t1 = new CompoundTerm ((de.Key as string).ToAtom (), new Variable (), t1);
                //  t0.Unify (t1, varStack);
                //  break;

                case BI.undefineds:
                    Ps.FindUndefineds();
                    break;


                case BI.copy_term: // copy_term( X, Y)
                    if (!term.Arg(1).Unify(term.Arg(0).Copy(true, false), varStack: varStack)) return false;
                    break;


                case BI.clearall: // clearall
                    Reset();
                    break;


                case BI.spypoints: // spypoints
                    Ps.ShowSpypoints();
                    break;


                case BI.clause: // clause (Head,Body)
                    var state = term.Arg(0); // State
                    t1 = term.Arg(1); // head
                    t2 = term.Arg(2); // body

                    if (t1.IsVar)
                    {
                        IO.Error("First argument of clause/2 is not sufficiently instantiated");

                        return false;
                    }

                    ClauseIterator iterator = null;

                    if (state.IsVar) // first call only
                    {
                        iterator = new ClauseIterator(predTable: Ps, clauseHead: t1, varStack: varStack);
                        state.Unify(new UserClassTerm<ClauseIterator>(obj: iterator), varStack: varStack);

                        break;
                    }

                    iterator = ((UserClassTerm<ClauseIterator>) state).UserObject;

                    if (!iterator.MoveNext()) // sets iterator.ClauseBody
                    {
                        term.SetArg(0, t: BaseTerm.VAR);

                        return false;
                    }

                    if (!t2.Unify(t: iterator.ClauseBody, varStack: varStack)) return false;

                    break;


                case BI.member: // member( X, L)
                    if ((t0 = term.Arg(0)).IsVar || !(t1 = term.Arg(1)).IsListNode)
                        return false;

                    result = false;

                    while (t1.Arity == 2)
                    {
                        if (result = t0.Unify(t1.Arg(0), varStack: varStack)) break;

                        t1 = t1.Arg(1);
                    }

                    currentCp.Kill(); // no backtracking to follow -> remove the choicepoint for the alternative clauses

                    if (!result) return false;

                    break;

                // BACKTRACKING VERSION
                //          while (t1.Arity == 2)
                //          {
                //            if (result = t0.Unify (t1.Arg (0), varStack))
                //            {
                //              if ((t0 = t1.Arg (1)).Arity == 0) // empty t
                //                currentCp.Kill (); // no backtracking to follow -> remove the choicepoint for the alternative clauses
                //              else
                //                head.Arg (2).Bind (t0);  // set Rest to remainder of t (for backtracking)
                //
                //              break;
                //            }
                //            t1 = t1.Arg (1);
                //          }

                //case BI.append: // append( [_|_], [_|_], L)
                //  if (!(t0 = head.Arg (0)).IsProperOrPartialList) return false;

                //  t1 = head.Arg (1);

                //  if (!head.Arg (2).Unify (((BaseTermListTerm)t0).Append (t1), varStack))
                //    return false;

                //  break;


                case BI.regex_match: // regex_match( +Source, +Pattern, ?Result [, Options])
                    // String 'Source' is matched against C# regular expression string 'Pattern'.
                    // 'Result' is a list containing the matching regular expression groups (a C# regex group
                    // is a subpattern enclosed in parentheses, with an optional name or number). 
                    // In 'Result' a group is represented as a label (group number or group name)
                    // followed by a ':', followed by a list of strings (captures) belonging to that group.
                    // 
                    // Example:
                    //
                    // regex_match("21-02-1951", "(?<Month>\\d{1,2})-(\\d{1,2})-(?<Year>(?:\\d{4}|\\d{2}))", L).
                    //
                    // L = [1:["02"], "Month":["21"], "Year":["1951"]]
                    //
                    // If there is only one group, only the group name and the list of captures will be returned.
                    // If the group has no name, only the list of captures will be returned.
                    // If no group was present in the pattern, the empty list will be returned in case of a match.
                    // If there was no match, the predicate will fail.
                    // 
                    // 'Options' is an optional list containing regex options a la C#. The following options are 
                    // supported: ignorecase, multiline, singleline, explicitcapture, cultureinvariant
                    //
                    if (!((t0 = term.Arg(0)).IsString || t0.IsAtom) || !((t1 = term.Arg(1)).IsString || !t1.IsAtom))
                        return false;

                    var optionsOLD = term.Arity == 4 ? ((ListTerm) term.Arg(3)).ToStringArray() : null;
                    var groups = Utils.FindRegexMatches(opTable: OpTable, source: t0.FunctorToString,
                        matchPattern: t1.FunctorToString, options: optionsOLD);

                    if (groups == null || !term.Arg(2).Unify(t: groups, varStack: varStack)) return false;

                    break;


                case BI.regex_replace: // regex_replace( S, P, R, T)
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);
                    t2 = term.Arg(2);
                    t3 = term.Arg(3);

                    if (!(t0 is StringTerm && t1 is StringTerm && t2 is StringTerm))
                        IO.Error("regex_replace/4 -- first three arguments must be strings");

                    var input = t0.FunctorToString;
                    var pattern = t1.FunctorToString;
                    var replacement = t2.FunctorToString;

                    if (!t3.Unify(
                        new StringTerm(Regex.Replace(input: input, pattern: pattern, replacement: replacement)),
                        varStack: varStack))
                        return false;

                    break;


                case BI.numcols: // numcols( N) -- Number of columns in the DOS-box

                    return false;

                case BI.userroles:

                    IO.Error("userroles only available if compiler symbol mswindows is defined");
                    break;


                // this version actually only returns the number of ClockTicks
                case BI.statistics: // statistics( X, [MSec,_])
                    t1 = term.Arg(1).Arg(0);
                    var time = ClockTicksMSecs();

                    if (!t1.Unify(new DecimalTerm(value: time), varStack: varStack)) return false;

                    break;

                // not operational yet, some next version
                case BI.callstack: // callstack( S, L) -- S is string, L is list representation of current call stack
                    
                    break;


                case BI.stacktrace: // stacktrace( Mode). If on: show C# exception stacktrace; if off: don't
                    t0 = term.Arg(0);

                    if (t0.IsVar && !t0.Unify(new AtomTerm(userSetShowStackTrace ? "on" : "off"), varStack: varStack))
                        return false;

                    var mode = t0.FunctorToString;

                    if (mode == "on")
                        userSetShowStackTrace = true;
                    else if (mode == "off")
                        userSetShowStackTrace = false;
                    else
                        IO.Error(":- stacktrace: illegal argument '{0}'; use 'on' or 'off' instead", mode);

                    break;



                case BI.query_timeout:
                    t0 = term.Arg(0);

                    if (t0.IsVar)
                    {
                        t0.Unify(new DecimalTerm(value: queryTimeout), varStack: varStack);

                        break;
                    }

                    if (!t0.IsInteger || (queryTimeout = t0.To<int>()) < 0) return false;

                    break;

                case BI.get_counter:
                    globalTermsTable.getctr(a: term.Arg(0).FunctorToString, value: out cntrValue);

                    if (!term.Arg(1).Unify(new DecimalTerm(value: cntrValue), varStack: varStack)) return false;
                    break;

                case BI.set_counter:
                    t0 = term.Arg(0);

                    if (!(t0.IsAtom || t0.IsNatural))
                        IO.Error("set_counter: first argument ({0}) must be an atom or a non-negative integer", t0);

                    globalTermsTable.setctr(a: t0.FunctorToString, term.Arg(1).To<int>());
                    break;

                case BI.inc_counter:
                    a = term.Arg(0).FunctorToString;
                    globalTermsTable.getctr(a: a, value: out cntrValue);

                    if (term.Arity == 2 &&
                        !term.Arg(1).Unify(new DecimalTerm(cntrValue + 1), varStack: varStack))
                        return false;

                    globalTermsTable.incctr(a: a);
                    break;

                case BI.dec_counter:
                    a = term.Arg(0).FunctorToString;
                    globalTermsTable.getctr(a: a, value: out cntrValue);

                    if (term.Arity == 2 &&
                        !term.Arg(1).Unify(new DecimalTerm(cntrValue - 1), varStack: varStack))
                        return false;

                    globalTermsTable.decctr(a: a);
                    break;

                case BI.getenvvar:
                    t0 = term.Arg(0);

                    if (!(t0 is StringTerm || t0 is AtomTerm))
                    {
                        IO.Error("setenvvar: first argument ({0}) must be an atom or a string", t0);

                        return false;
                    }

                    a = Environment.GetEnvironmentVariable(variable: t0.FunctorToString);
                    decimal ev;

                    if (decimal.TryParse(s: a, result: out ev))
                        t1 = new DecimalTerm(value: ev);
                    else
                        t1 = new StringTerm(value: a);

                    if (!term.Arg(1).Unify(t: t1, varStack: varStack)) return false;
                    break;

                case BI.setenvvar:
                    t0 = term.Arg(0);

                    if (!(t0 is StringTerm || t0 is AtomTerm))
                    {
                        IO.Error("setenvvar: first argument ({0}) must be an atom or a string", t0);

                        return false;
                    }

                    Environment.SetEnvironmentVariable(
                        variable: t0.FunctorToString, term.Arg(1).ToString());
                    break;

                case BI.getvar:
                    globalTermsTable.getvar(name: term.Arg(0).FunctorToString, value: out t1);
                    if (!term.Arg(1).Unify(t: t1, varStack: varStack)) return false;
                    break;

                case BI.setvar:
                    if (!(term.Arg(0) is AtomTerm)) return false;

                    globalTermsTable.setvar(name: term.Arg(0).FunctorToString, term.Arg(1).Copy());
                    break;


                // Burrows–Wheeler transform, cf. https://en.wikipedia.org/wiki/Burrows%E2%80%93Wheeler_transform
                case BI.bw_transform: // bw_transform( ?Plain, ?Encoded, ?Index)
                    byte[] buffer_in;
                    byte[] buffer_out;
                    byte[] buffer_decode;
                    t0 = term.Arg(0);
                    t1 = term.Arg(1);
                    t2 = term.Arg(2);
                    var bwt = new BWTImplementation();

                    if (t1.IsVar) // encode t0 yielding t1
                    {
                        if (!t0.IsString || !t2.IsVar) return false;

                        buffer_in = Encoding.UTF8.GetBytes(s: t0.FunctorToString);
                        buffer_out = new byte [buffer_in.Length];
                        var primary_index = 0;
                        bwt.bwt_encode(buf_in: buffer_in, buf_out: buffer_out, size: buffer_in.Length,
                            primary_index: ref primary_index);
                        t1.Unify(new StringTerm(Encoding.UTF8.GetString(bytes: buffer_out)), varStack: varStack);
                        t2.Unify(new DecimalTerm(value: primary_index), varStack: varStack);
                    }
                    else if (t1.IsString && t2.IsInteger)
                    {
                        buffer_out = Encoding.UTF8.GetBytes(s: t1.FunctorToString);
                        buffer_decode = new byte [buffer_out.Length];
                        var primary_index = t2.To<int>();
                        bwt.bwt_decode(buf_encoded: buffer_out, buf_decoded: buffer_decode, size: buffer_out.Length,
                            primary_index: primary_index);
                        var decoded = Encoding.UTF8.GetString(bytes: buffer_decode);

                        if (t0.IsVar) // decode t1 into t0
                            t0.Unify(new StringTerm(value: decoded), varStack: varStack);
                        else if (t0.IsString) // t1 decoded must be equal to t0
                            return t0.FunctorToString == decoded;
                        else
                            return false;
                    }
                    else
                    {
                        return false;
                    }

                    break;
            }

            #endregion switch

            goalListHead = goalListHead.NextNode;

            if (reporting) // advance the goalList until a non-spypoint is encountered. Show spy(-Exit)-info.
            {
                TermNode sp = null;

                while (goalListHead is SpyPoint)
                {
                    sp = ((SpyPoint) goalListHead).SaveGoal;

                    if (Debugger(port: SpyPort.Exit, goalNode: sp, null, false, 1)) break;

                    goalListHead = sp.NextNode;
                }
            }

            findFirstClause = true;

            return true;
        }


        private string ConvertToCmdArgs(BaseTerm t)
        {
            if (t.IsProperList)
            {
                var sb = new StringBuilder();

                foreach (var s in ((ListTerm) t).ToStringArray())
                    sb.AppendFormat(" {0}", arg0: s);

                return sb.ToString();
            }

            if (t is Variable) return null;

            return ' ' + t.FunctorToString;
        }


        private BaseTerm TypedTerm(string a)
        {
            BaseTerm t2;
            TermType type;

            a = a.ToAtomic(type: out type);

            if (type == TermType.Number)
                t2 = new DecimalTerm(decimal.Parse(s: a, provider: CIC));
            else
                t2 = new AtomTerm(value: a);

            return t2;
        }


        private BaseTerm TermFromWord(string word)
        {
            TermType type;
            word = word.ToAtomic(type: out type);

            if (type == TermType.Number)
                try
                {
                    return new DecimalTerm(decimal.Parse(s: word, style: NumberStyles.Any, provider: CIC));
                }
                catch
                {
                    throw new ParserException("*** Unable to convert \"" + word + "\" to a number");
                }

            return new AtomTerm(value: word);
        }


        private bool SetCaching(BaseTerm term, bool value)
        {
            var result = false;

            if (term.Arity == 0) // all predicates
            {
                Ps.SetCaching(null, 0, value: value);
                result = true;
            }
            else if (term.Arity == 1) // specific predicate, potentially with an arity
            {
                var t0 = term.Arg(0);

                if (t0.HasFunctor("/") && t0.Arity == 2 && t0.Arg(0).IsAtom && t0.Arg(1).IsInteger)
                    result = Ps.SetCaching(functor: t0.Arg(0).FunctorToString, t0.Arg<int>(1), value: value);
                else if (term.Arg(0).IsAtom) // predicate functor without arity: take all arities
                    result = Ps.SetCaching(functor: term.Arg(0).FunctorToString, -1, value: value);
            }

            return result;
        }


        private BaseTerm CreateNewTerm(BaseTerm t, int arity, string functor, BaseTerm[] args)
        {
            OperatorDescr od;

            if (arity == 0)
            {
                if (OpTable.HasOpDef(name: functor))
                    t = new OperatorTerm(name: functor);
                else
                    t = new AtomTerm(functor.ToAtom());
            }
            else if (arity == 1 && OpTable.IsUnaryOperator(name: functor, od: out od))
            {
                t = new OperatorTerm(od: od, args[0]);
            }
            else if (arity == 2 && OpTable.IsBinaryOperator(name: functor, od: out od))
            {
                t = new OperatorTerm(od: od, args[0], args[1]);
            }
            else if (arity == 2 && functor == PrologParser.DOT)
            {
                t = new ListTerm(args[0], args[1]);
            }
            else
            {
                t = new CompoundTerm(functor: functor, args: args);
            }

            return t;
        }

        private class PredicateCallOptions
        {
            private List<object> o;
            private Dictionary<string, List<object>> table;

            public PredicateCallOptions()
            {
                table = new Dictionary<string, List<object>>();
            }

            public void Set(BaseTerm list)
            {
                table = new Dictionary<string, List<object>>();
                CopyFromList((ListTerm) list);
            }

            public void Clear()
            {
                table.Clear();
            }

            private void Register<T>(string s, T value)
            {
                if (table.TryGetValue(key: s, value: out o))
                    o.Add(item: value);
                else
                    table[key: s] = new List<object> {value};
            }


            public void Register(string s)
            {
                table[key: s] = null;
            }

            public bool Get<T>(string s, int argNo, ref T value) where T : struct
            {
                if (table.TryGetValue(key: s, value: out o))
                {
                    if (o == null)
                        IO.Error("No argument list allowed for option '{0}'", s);
                    else if (argNo <= o.Count)
                        value = (T) o[argNo - 1];

                    return true;
                }

                return false;
            }


            public bool Get<T>(string s, ref T value) where T : struct
            {
                return Get(s: s, 1, value: ref value);
            }


            public bool Get(string s)
            {
                return table.TryGetValue(key: s, value: out o);
            }


            public void CopyFromList(ListTerm list)
            {
                foreach (BaseTerm t in list)
                {
                    var optionName = t.FunctorToString;

                    if (t.Arity == 0)
                        Register(s: optionName);
                    else
                        foreach (var a in t.Args)
                            if (a.IsNumber)
                                Register(s: optionName, a.To<int>());
                            else if (a.IsAtom)
                                Register(s: optionName, value: a.FunctorToString);
                            else if (a.IsString)
                                Register(s: optionName, a.FunctorToString.ToAtom());
                }
            }


            public override string ToString()
            {
                var sb = new StringBuilder();
                var first0 = true;
                sb.Append('[');

                foreach (var entry in table)
                {
                    if (first0) first0 = false;
                    else sb.Append(", ");

                    sb.Append(value: entry.Key);

                    if (entry.Value != null)
                    {
                        sb.Append('(');
                        var first1 = true;

                        foreach (var o in entry.Value)
                        {
                            if (first1) first1 = false;
                            else sb.Append(",");

                            sb.Append(value: o);
                        }

                        sb.Append(')');
                    }
                }

                sb.Append(']');

                return sb.ToString();
            }
        }
    }
}