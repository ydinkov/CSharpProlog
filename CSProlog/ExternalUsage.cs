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

#if mswindows
using System.Windows.Forms;
#endif

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
#if mswindows
    #region Batch Processing
    public bool ProcessArgs (string [] args, bool windowsMode)
    {
      if (args.Length == 0) return false;

      // Process command line arguments. First is a query, optional second
      // arg is number of backtrack attempts (default is 0, 'infinite' = *)
      // If the first argument contains spaces, it must be enclosed in double quotes.
      // Example: pld "['path.pl'], solve( p(2,2), L)" 4
      string command = args [0].Dequoted ().Trim ();
      Query = command + (command.EndsWith (".") ? null : "."); // append a dot if necessary
      int solutionCount = 0; // i.e. find all solutions (backtrack until failure; value corresponds to '*')
      string msg = null;

      if (args.Length > 2)
        msg = string.Format ("Superfluous argument '{0}'", args [2]);
      else if (args.Length == 2 && !(int.TryParse (args [1], out solutionCount) || args [1] == "*"))
        msg = string.Format ("Illegal value '{0}' for maximum number of solutions", args [1]);
      else
        solutionCount = 1;

      if (msg == null)
      {
        int i = 0;
        bool found = false; // true if at least one solution found

        foreach (PrologEngine.ISolution s in SolutionIterator)
        {
          if (Error || (!found && !s.Solved)) // only an immediate 'no' give rise to an error
          {
            msg = s.ToString ();

            break;
          }

          if (++i == solutionCount) break;

          found = true;
        }
      }

      if (msg != null)
      {
        if (windowsMode)
          MessageBox.Show (msg);
        else
          Console.WriteLine (msg);

        Environment.ExitCode = 1; // sets DOS ERRORLEVEL to 1
      }

      return true;
    }
    #endregion Batch Processing
#endif

        #region GetAllSolutionsXml

        // Store solutions in an xml structure
#if !NETSTANDARD
    public string GetAllSolutionsXml (string sourceFileName, string destinFileName, string query)
    {
      return GetAllSolutionsXml (sourceFileName, destinFileName, query, 0);
    }

    public string GetAllSolutionsXml (string sourceFileName, string destinFileName, string query, int maxSolutionCount)
    {
      XmlTextWriter xtw = null;
      StreamWriter sw = null;

      if (destinFileName == null)
        xtw = new XmlTextWriter (new MemoryStream (), System.Text.Encoding.GetEncoding (1252));
      else
        xtw = new XmlTextWriter (destinFileName, null);

      xtw.Formatting = Formatting.Indented;
      xtw.WriteStartDocument ();

      try
      {
        if (sourceFileName != null) 
          Reset ();

        if (sourceFileName != null)
          Consult (sourceFileName);

        Query = query + (query.EndsWith (".") ? null : "."); // append a dot if necessary
        int i = 0;
        bool found = false; // true if at least one solution found
        bool firstSol = true;

        foreach (PrologEngine.ISolution s in SolutionIterator)
        {
          if (Error)
          {
            xtw.WriteStartElement ("error");
            xtw.WriteCData (s.ToString ());
            xtw.WriteEndElement ();

            break;
          }
          else if (!found && !s.Solved)
          {
            xtw.WriteStartElement ("solutions");
            xtw.WriteAttributeString ("success", "false");
            xtw.WriteStartElement ("query");
            xtw.WriteString (query);
            xtw.WriteEndElement ();

            break;
          }

          if (firstSol)
          {
            firstSol = false;
            xtw.WriteStartElement ("solutions");
            xtw.WriteAttributeString ("success", "true");
            xtw.WriteStartElement ("query");
            xtw.WriteString (query);
            xtw.WriteEndElement ();
          }

          bool firstVar = true;

          foreach (PrologEngine.IVarValue varValue in s.VarValuesIterator)
          {
            if (varValue.DataType == "none") break;

            if (firstVar)
            {
              firstVar = false;
              xtw.WriteStartElement ("solution");
            }

            xtw.WriteStartElement ("variable");
            xtw.WriteAttributeString ("name", varValue.Name);
            xtw.WriteAttributeString ("type", varValue.DataType);
            xtw.WriteString (varValue.Value.ToString ());
            xtw.WriteEndElement (); // variable
          }

          if (!firstVar) xtw.WriteEndElement (); // solution

          if (++i == maxSolutionCount) break;

          found = true;
        }

        if (!Error) xtw.WriteEndElement (); // solutions

        xtw.WriteEndDocument ();
        xtw.Flush ();

        if (destinFileName == null)
        {
          if (xtw.BaseStream == null) return null;

          return new ASCIIEncoding ().GetString (((MemoryStream)xtw.BaseStream).ToArray ());
        }
        else
          return null;
      }
      finally
      {
        if (xtw != null) xtw.Close ();
        if (sw != null) sw.Close ();
      }
    }
#endif

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