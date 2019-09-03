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

using System.Collections.Generic;
using System.Text;

// Arrays: not in this version. The whole concept needs to be thought over more carefully.

namespace Prolog
{
    public partial class PrologEngine
    {
        // untested code

        #region TermArray

        public class TermArray
        {
            // Any array (i.e. with an arbitrary number of subscripts) is mapped to a one-dimensional 
            // one (baseArray). In doing so, it is possible to accomodate arrays with an arbitrary 
            // number of subscripts.
            private readonly BaseTerm[] baseArray;
            private readonly int[] dimensions;

            public TermArray(string name, int[] dimensions)
            {
                Name = name;
                this.dimensions = dimensions;
                var length = 1;

                for (var i = 0; i < dimensions.Length; i++)
                {
                    if (dimensions[i] <= 0)
                        IO.Error("Dimension {0} of array '{1}' has illegal value {2}", i, name, dimensions[i]);

                    length *= dimensions[i];
                }

                baseArray = new BaseTerm [length];
            }

            public int Rank => dimensions.Length;
            public string Name { get; }

            public BaseTerm GetEntry(int[] subscripts) // subscripts are zero-based
            {
                return baseArray[CalculateOffset(subscripts: subscripts)];
            }

            public void SetEntry(int[] subscripts, BaseTerm t)
            {
                baseArray[CalculateOffset(subscripts: subscripts)] = t;
            }

            // CalculateOffset calculates the mapping of [i1, i2, ..., iN] to the index in this 1-D array
            private int CalculateOffset(int[] subscripts) // f(i1, i2, ..., iN) => 0 .. d1*d2*...*dN-1
            {
                for (var i = 0; i < subscripts.Length; i++)
                    if (subscripts[i] < 0 || subscripts[i] >= dimensions[i])
                        IO.Error("Value of index {0} is {1} but must be in the range 0..{2}",
                            i, subscripts[i], dimensions[i] - 1);

                var offset = subscripts[0];

                for (var i = 1; i < subscripts.Length; i++)
                    offset = offset * dimensions[i] + subscripts[i];

                return offset;
            }
        }

        #endregion TermArray

        #region ArrayVariable

        public class ArrayVariable : NamedVariable
        {
            private readonly List<BaseTerm> subscripts;
            private TermArray ta;

            public ArrayVariable(string name, TermArray ta, ListTerm subscripts)
            {
                this.ta = ta;
                this.name = ta.Name;
                this.subscripts = subscripts.ToTermList();

                if (this.subscripts.Count != ta.Rank)
                    IO.Error("Wrong number of subscripts for '{0}': expected {1}, got {2}",
                        name, ta.Rank, this.subscripts.Count);
            }

            public override string ToWriteString(int level)
            {
                var sb = new StringBuilder( name);
                var first = true;

                foreach (var t in subscripts)
                {
                    if (first) first = false;
                    else sb.Append(CommaAtLevel(level: level));

                    sb.Append(t.IsGround ? t.Eval() : t); // at end of query vs. any other situation
                }

                return sb.ToString();
            }
        }

        #endregion ArrayVariable
    }
}