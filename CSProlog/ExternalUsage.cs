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
using System.Text;



namespace Prolog
{
    #region SolutionSet

    public class SolutionSet
    {
        private Solution currVarSet;
        private readonly List<Solution> solutionSet;

        public SolutionSet()
        {
            solutionSet = new List<Solution>();
            Success = false;
            ErrMsg = null;
        }

        public string Query { get; internal set; }

        public bool Success { get; internal set; }

        public string ErrMsg { get; internal set; }

        public bool HasError => ErrMsg != null;
        public int Count => solutionSet.Count;

        public IEnumerable<Solution> NextSolution
        {
            get
            {
                foreach (var s in solutionSet)
                    yield return s;
            }
        }

        public Solution this[int i] => solutionSet[index: i];

        internal void CreateVarSet()
        {
            solutionSet.Add(currVarSet = new Solution());
        }

        internal void AddToVarSet(string name, string type, string value)
        {
            currVarSet.Add(name: name, type: type, value: value);
            Success = true;
        }

        public override string ToString()
        {
            if (ErrMsg != null)
                return ErrMsg;

            if (Success)
            {
                if (solutionSet.Count == 0) return "yes";

                var sb = new StringBuilder();
                var i = 0;
                foreach (var s in solutionSet)
                    sb.AppendLine("Solution {0}\r\n{1}", ++i, s.ToString());

                return sb.ToString();
            }

            return "no";
        }
    }

    #endregion SolutionSet

    #region Solution

    public class Solution // a solution is a set of variables
    {
        private readonly List<Variable> variables;

        public Solution()
        {
            variables = new List<Variable>();
        }

        private int Count => variables.Count;

        public IEnumerable<Variable> NextVariable
        {
            get
            {
                foreach (var v in variables)
                    yield return v;
            }
        }

        public Variable this[int i] => variables[index: i];

        internal void Add(string name, string type, string value)
        {
            variables.Add(new Variable(name: name, type: type, value: value));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var v in variables)
                sb.AppendLine(v.ToString());

            return sb.ToString();
        }
    }

    #endregion Solution

    #region Variable

    public class Variable
    {
        public Variable(string name, string type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; }

        public string Type { get; }

        public string Value { get; }

        public override string ToString()
        {
            return string.Format("{0} ({1}) = {2}", arg0: Name, arg1: Type, arg2: Value);
        }
    }

    #endregion Variable

    public partial class PrologEngine
    {

        #region GetAllSolutionsXml

        // Store solutions in an xml structure


        #endregion GetAllSolutionsXml


        #region GetAllSolutions

        // Store solutions in an GetAllSolutions class
        public SolutionSet GetAllSolutions(string sourceFileName, string query)
        {
            return GetAllSolutions(sourceFileName: sourceFileName, query: query, 0);
        }

        public SolutionSet GetAllSolutions(string sourceFileName, string query, int maxSolutionCount)
        {
            var solutions = new SolutionSet();

            try
            {
                if (sourceFileName != null) Reset();

                if (sourceFileName != null) Consult(fileName: sourceFileName);

                Query = solutions.Query = query + (query.EndsWith(".") ? null : "."); // append a dot if necessary
                var i = 0;
                var found = false;
                var varFound = false;

                foreach (var s in SolutionIterator)
                {
                    if (Error)
                    {
                        solutions.ErrMsg = s.ToString();

                        break;
                    }

                    if (!found && !s.Solved)
                    {
                        break;
                    }

                    solutions.Success = true;
                    var firstVar = true;

                    foreach (var varValue in s.VarValuesIterator)
                    {
                        if (varValue.DataType == "none") break;

                        if (firstVar)
                        {
                            firstVar = false;
                            solutions.CreateVarSet();
                        }

                        solutions.AddToVarSet(name: varValue.Name, type: varValue.DataType, varValue.Value.ToString());
                        varFound = true;
                    }

                    if (++i == maxSolutionCount || !varFound) break;

                    found = true;
                }
            }
            catch (Exception e)
            {
                solutions.ErrMsg = e.Message;
            }

            return solutions;
        }

        #endregion GetAllSolutions
    }
}