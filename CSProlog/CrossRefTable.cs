using System;
using System.Collections.Generic;
using System.IO;

namespace Prolog
{
    /* _______________________________________________________________________________________________
      |                                                                                               |
      |  C#Prolog -- Copyright (C) 2007-2014 John Pool -- j.pool@ision.nl                             |
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

    // PrologParser Generator version 4.0 -- Date/Time: 22-12-2010 8:42:54

    public partial class PrologEngine
    {
        // CrossRefTable implements a cross reference table for the predicates making up the 'program'.
        // Each predicate corresponds to a row. The entries in the row are the predicates that are called.
        // CalculateClosure () calculates the indirect calls (i.e. if A calls B and B calls C directly,
        // then the closure would calculate the indirect call A -> C.
        // Direct calls have an entry value 'false', indirect calls an entry value 'true'.

        #region CrossRefTable

        public class CrossRefTable : Dictionary<string, bool?>
        {
            private readonly List<PredicateDescr> axis; // predicate names sorted alphabetically
            private bool? result;

            public CrossRefTable()
            {
                axis = new List<PredicateDescr>();
                FindAllCalls = false;
            }

            private int dimension => axis.Count;
            public bool FindAllCalls { get; set; }


            public void Reset()
            {
                Clear();
                axis.Clear();
                FindAllCalls = false;
            }


            public void AddPredicate(PredicateDescr pd)
            {
                // add predicate only if not already present
                var i = axis.BinarySearch(item: pd);

                if (i < 0) axis.Insert(index: ~i, item: pd); // keep range sorted
            }

            // find all predicates called by 'row'
            public IEnumerable<PredicateDescr> Row(PredicateDescr row)
            {
                foreach (var col in axis)
                    if ((result = this[row: row, col: col]) != null)
                        if (FindAllCalls || result == false)
                            yield return col;
            }

            // find all predicates that call 'col'
            public IEnumerable<PredicateDescr> Col(PredicateDescr col)
            {
                foreach (var row in axis)
                    if ((result = this[row: row, col: col]) != null)
                        if (FindAllCalls || result == false)
                            yield return col;
            }


            // Warshall algorithm -- Journal of the ACM, Jan. 1962, pp. 11-12
            // This algorithm calculates the indirect calls.
            public void CalculateClosure()
            {
                int i, j, k;

                for (i = 0; i < dimension; i++)
                for (j = 0; j < dimension; j++)
                    if (this[i: j, j: i] != null)
                        for (k = 0; k < dimension; k++)
                            if (this[i: i, j: k] != null)
                                this[i: j, j: k] =
                                    true; // 'true' to indicate entry is indirect call (result of closure)
            }


            public void GenerateCsvFile(string fileName)
            {
                bool? value;
                StreamWriter sr = null;
                int rowTotal;
                var colTotal = new int [axis.Count];

                try
                {
                    sr = new StreamWriter(File.OpenWrite(path: fileName));

                    sr.WriteLine(Enquote(
                        "Call table. Row calls column entries. 'D' stands for direct call, 'I' for indirect call"));
                    sr.WriteLine();

                    // column titles
                    for (var j = 0; j < dimension; j++)
                    {
                        sr.Write(";{0}", Enquote( axis[index: j].Name));
                        colTotal[j] = 0;
                    }

                    sr.WriteLine(";TOTAL");

                    // rows
                    for (var i = 0; i < dimension; i++)
                    {
                        sr.Write(Enquote( axis[index: i].Name)); // row title
                        rowTotal = 0;

                        for (var j = 0; j < dimension; j++)
                        {
                            sr.Write(';');

                            if ((value = this[i: i, j: j]) != null)
                            {
                                sr.Write(value == false ? "D" : "I");
                                rowTotal++;
                                colTotal[j]++;
                            }
                        }

                        sr.WriteLine(";{0}", arg0: rowTotal);
                    }

                    // column totals
                    sr.Write("TOTAL");

                    for (var j = 0; j < dimension; j++) sr.Write(";{0}", colTotal[j]);

                    sr.WriteLine();
                }
                catch (Exception e)
                {
                    IO.Error("Error while trying to save Excel spreadsheet '{0}'. Message was:\r\n{1}" + e.Message);
                }
                finally
                {
                    if (sr != null) sr.Dispose();
                }
            }

            private string Enquote(string s)
            {
                return '"' + s.Replace("\"", "\"\"") + '"';
            }

            private string CompoundKey(PredicateDescr row, PredicateDescr col)
            {
                return string.Format("{0} {1}", arg0: row.Name, arg1: col.Name);
            }


            #region indices

            // used only when registering the direct calls, not for the closure (indirect calls)
            public bool? this[PredicateDescr row, PredicateDescr col]
            {
                set => this[CompoundKey(row: row, col: col)] = false;

                get
                {
                    if (TryGetValue(CompoundKey(row: row, col: col), value: out result)) return result;

                    return null;
                }
            }

            // used only when calculating the indirect calls (CalculateClosure)
            public bool? this[int i, int j]
            {
                set
                {
                    var key = CompoundKey(axis[index: i], axis[index: j]);
                    bool? result;

                    // do not overwrite 'false', as this would hide the fact that a direct call exists
                    if (!TryGetValue(key: key, value: out result))
                        this[key: key] = true;
                }

                get
                {
                    if (TryGetValue(CompoundKey(axis[index: i], axis[index: j]), value: out result)) return result;

                    return null;
                }
            }

            #endregion indices
        }

        #endregion CrossRefTable

        #region PredicateDescr CompareTo ()

        public partial class PredicateDescr : IComparable<PredicateDescr>
        {
            public int CompareTo(PredicateDescr pd)
            {
                var result = Functor.CompareTo(strB: pd.Functor);

                if (result == 0) return Arity.CompareTo( pd.Arity);

                return result;
            }
        }

        #endregion
    }
}