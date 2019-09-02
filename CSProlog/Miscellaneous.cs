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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace Prolog
{
    using ArrayList = List<object>;
    using Hashtable = Dictionary<object, object>;
    public partial class PrologEngine
    {
        #region ConfigSettings

        public static class ConfigSettings // configuration settings
        {
            public static readonly bool CSharpStrings;
            public static readonly bool DiscontiguousAllowed;
            public static readonly bool DisableControlC;
            public static readonly string AnswerFalse;
            public static readonly string AnswerTrue;
            public static readonly bool ResolveEscapes;
            public static readonly int HistorySize;
            public static readonly bool NewlineInStringAllowed;
            public static readonly bool VerbatimStringsAllowed;
            public static readonly string InitialConsultFile;
            public static readonly string CsPrologHelpFile;
            public static readonly bool DoOccursCheck;
            public static readonly string DefaultDateTimeFormat;
            public static readonly string SmtpHost;
            public static readonly bool OnErrorShowStackTrace;
            public static int UnifyCountCacheThreshold;

            static ConfigSettings()
            {
                try
                {
                    // read configuration settings
                    DisableControlC = GetConfigSetting("DisableControlC", false);
                    CSharpStrings = GetConfigSetting("CSharpStrings", true);
                    DiscontiguousAllowed = GetConfigSetting("DiscontiguousAllowed", false);
                    AnswerFalse = GetConfigSetting("AnswerFalse", "false");
                    AnswerTrue = GetConfigSetting("AnswerTrue", "true");
                    HistorySize = GetConfigSetting("HistorySize", 40);
                    ResolveEscapes = GetConfigSetting("ResolveEscapes", true);
                    NewlineInStringAllowed = GetConfigSetting("NewlineInStringAllowed", true);
                    VerbatimStringsAllowed = GetConfigSetting("VerbatimStringsAllowed", true);
                    InitialConsultFile = GetConfigSetting("InitialConsultFile", null);
                    CsPrologHelpFile = GetConfigSetting("CsPrologHelpFile", null);
                    DoOccursCheck = GetConfigSetting("DoOccursCheck", false);
                    DefaultDateTimeFormat = GetConfigSetting("DefaultDateTimeFormat", "yyyy-MM-dd HH:mm:ss");
                    OnErrorShowStackTrace = GetConfigSetting("OnErrorShowStackTrace", false);
                    workingDirectory = GetConfigSetting("WorkingDirectory", null);
                    UnifyCountCacheThreshold = GetConfigSetting("UnifyCountCacheThreshold", 100);
                    SmtpHost = GetConfigSetting("SmtpHost", null);
                }
                catch (Exception e)
                {
                    IO.Fatal("Error while reading configuration file. Message was:\r\n{0}",
                        e.GetBaseException().Message);
                }
            }

            public static string workingDirectory { get; set; }

            public static string WorkingDirectory // symbolic names expanded
            {
                get
                {
                    string wd;
                    if (String.IsNullOrEmpty(workingDirectory))
                        wd = Directory.GetCurrentDirectory();
                    else if (workingDirectory == "%desktop" || workingDirectory == "%exedir")
                        throw new NotImplementedException();
                    else
                        wd = workingDirectory;

                    if (!wd.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        wd = wd + Path.DirectorySeparatorChar;

                    return wd;
                }
                set { workingDirectory = value; }
            }

            public static string GetConfigSetting(string key, string defaultValue)
            {
                return defaultValue;

            }

            static bool GetConfigSetting(string key, bool defaultValue)
            {
                return defaultValue;
            }

            static int GetConfigSetting(string key, int defaultValue)
            {
                return defaultValue;
            }


            public static void SetWorkingDirectory(string dirName)
            {
                if (dirName == null)
                    workingDirectory = GetConfigSetting("WorkingDirectory", null);
                else
                {
                    workingDirectory = Utils.GetFullDirectoryName(dirName);

                    if (workingDirectory == null)
                        IO.Error("Illegal name '{0}' for working directory", dirName);
                }

                IO.Message("Working directory set to '{0}'", WorkingDirectory);
            }

            public static bool SetWorkingDirectory(BaseTerm term, VarStack varStack)
            {
                if (term.Arity == 0)
                {
                    workingDirectory = GetConfigSetting("WorkingDirectory", null);
                    IO.Message("Working directory set to '{0}'", WorkingDirectory);

                    return true;
                }

                BaseTerm t0 = term.Arg(0);

                if (t0.IsVar)
                {
                    t0.Unify(new StringTerm(workingDirectory), varStack); // symbolic names

                    return true;
                }

                string wd = Utils.DirectoryNameFromTerm(t0);

                if (wd == null)
                {
                    IO.Error("Illegal name '{0}' for working directory", t0.FunctorToString);

                    return false;
                }

                workingDirectory = wd;
                IO.Message("Working directory set to '{0}'", WorkingDirectory);

                return true;
            }
        }

        #endregion ConfigSettings

        #region Producer-Consumer buffer

        public class ProdConsBuffer<T>
        {
            const int MAX_Q_SIZE = -1; // no limit
            Queue<T> queue;

            public ProdConsBuffer()
            {
                queue = new Queue<T>();
            }

            public int Count
            {
                get { return queue.Count; }
            }

            public void TryEnqueue(T item)
            {
                lock (this)
                {
                    if (queue.Count == MAX_Q_SIZE) Monitor.Wait(this);

                    queue.Enqueue(item);
                    Monitor.Pulse(this);
                }
            }

            public T TryDequeue()
            {
                lock (this)
                {
                    if (queue.Count == 0)
                    {
                        Monitor.Wait(this);
                    }

                    T item = queue.Dequeue();
                    Monitor.Pulse(this);

                    return item;
                }
            }


            public void Clear()
            {
                queue.Clear();
            }

            public T Peek()
            {
                return queue.Peek();
            }

            public bool Contains(T item)
            {
                return queue.Contains(item);
            }
        }

        #endregion Producer-Consumer buffer

        #region Globals

        public class Globals
        {
            #region static readonly properties

            public static readonly string DefaultExtension = ".pl";

            #endregion


            #region properties

            static PrologParser currentParser = null;

            #endregion

            #region public fields

            public static CultureInfo CI = CultureInfo.InvariantCulture;

            public static Hashtable ConsultedFiles = new Hashtable();

            //TODO reconsider the use of the statics below
            public static string ConsultFileName = null; // file being currently consulted
            public static string ConsultModuleName = null; // name of current module (if any) in file being consulted

            public static PrologParser CurrentParser
            {
                get => currentParser;
                set => currentParser = value;
            }

            public static int LineNo => (currentParser == null || currentParser.InQueryMode) ? -1 : currentParser.LineNo - 1;

            public static int ColNo => currentParser?.ColNo ?? -1;

            #endregion
        }

        #endregion Globals

        #region PersistentSettings


        #endregion PersistentSettings

        public static class Utils
        {
            static readonly string logFileName = "PL" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            static StreamWriter logFile;
            static bool showMode = true;

            enum readStatus
            {
                Content
            }


            public static string AtomFromVarChar(string s)
            {
                return s.Replace("'", "''").ToAtom();
            }


            static string WDF(string fileName)
            {
                // prefix fileName with the name of the WorkingDirectory (config setting),
                // but only if it does not have a path prefixed already
                string wd = ConfigSettings.WorkingDirectory;


                if (fileName.IndexOf(Path.DirectorySeparatorChar) == -1)
                    return ConfigSettings.WorkingDirectory + fileName;

                return fileName;
            }

            public static string FileNameFromTerm(BaseTerm t, string defExt)
            {
                if (t.IsVar) return null;

                return ExtendedFileName(t.FunctorToString, defExt);
            }


            public static string DirectoryNameFromTerm(BaseTerm t) => 
                !t.IsAtomOrString ? null : GetFullDirectoryName(t.FunctorToString.Dequoted());


            public static string GetFullDirectoryName(string wd)
            {
                try
                {
                    if (!wd.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        wd = wd + Path.DirectorySeparatorChar;

                    if (wd.Length == 3 && wd.EndsWith(@":\")) return wd; // GetDirectoryName returns null for root dir

                    return Path.GetDirectoryName(wd);
                }
                catch (Exception e)
                {
                    IO.Error(e.Message);

                    return null;
                }
            }


            public static string FileNameFromSymbol(string s, string defExt)
            {
                return (String.IsNullOrEmpty(s) ? null : ExtendedFileName(s, defExt));
            }


            public static string ExtendedFileName(string s, string defExt)
            {
                string fileName = null;

                try
                {
                    fileName = s.Dequoted();

                    if (!Path.HasExtension(fileName))
                        fileName = Path.ChangeExtension(fileName, defExt);

                    fileName = WDF(fileName);

                    return Path.GetFullPath(fileName);
                }
                catch (Exception e)
                {
                    IO.Error("Error in file name '{0}'\r\n{1}", fileName, e.Message);

                    return null;
                }
            }


            public static BaseTerm FindRegexMatches(
                OperatorTable opTable, string source, string matchPattern, string[] options)
            {
                Regex re = null;
                RegexOptions reOptions = RegexOptions.None;

                if (options != null)
                {
                    foreach (string o in options)
                        switch (o.ToLower())
                        {
                            case "ignorecase":
                                reOptions |= RegexOptions.IgnoreCase;
                                break;
                            case "multiline":
                                reOptions |= RegexOptions.Multiline;
                                break;
                            case "singleline":
                                reOptions |= RegexOptions.Singleline;
                                break;
                            case "explicitcapture":
                                reOptions |= RegexOptions.ExplicitCapture;
                                break;
                            case "cultureinvariant":
                                reOptions |= RegexOptions.CultureInvariant;
                                break;
                            default:
                                IO.Error("match_regex -- unsupported option '{0}'", o);
                                break;
                        }

                    ;
                }

                try
                {
                    re = new Regex(matchPattern, reOptions); //, RegexOptions.Multiline);
                }
                catch (Exception x)
                {
                    IO.Error("Error in regular expression '{0}'\r\nMessage: {1}", matchPattern, x.Message);
                }

                Match m = re.Match(source);

                if (!m.Success) return null;

                int[] gnums = re.GetGroupNumbers();
                BaseTerm groups = new ListTerm();

                while (m.Success)
                {
                    for (int i = 1; i < gnums.Length; i++) // start at group 1 (0 is the fully captured match string)
                    {
                        Group g = m.Groups[gnums[i]];
                        BaseTerm captures = new ListTerm();
                        string groupId = re.GetGroupNames()[i];
                        int groupNo;
                        BaseTerm groupIdTerm;

                        foreach (Capture c in g.Captures)
                            captures = ((ListTerm) captures).AppendElement(new StringTerm(c.ToString()));

                        if (int.TryParse(groupId, out groupNo))
                            groupIdTerm = new DecimalTerm(groupNo);
                        else
                            groupIdTerm = new StringTerm(re.GetGroupNames()[i]);

                        groups = ((ListTerm) groups).AppendElement(
                            new OperatorTerm(opTable, ":", groupIdTerm, captures));
                    }

                    m = m.NextMatch();
                }

                if (((ListTerm) groups).ProperLength == 1)
                {
                    groups = groups.Arg(0); // groups is <groupname>:[<captures>]

                    if (groups.Arg(0) is DecimalTerm)
                        groups = groups.Arg(1); // anonymous group, just return the list of captures
                }

                return groups;
            }


            public static string Format(ListTerm lt)
            {
                if (!lt.IsProperList || lt.ProperLength != 2)
                {
                    IO.Error("Format list must be a proper list of length 2");

                    return null;
                }

                BaseTerm fstring = lt.Arg(0);
                BaseTerm args = lt.Arg(1).Arg(0);

                return Format(fstring, args);
            }


            public static string Format(BaseTerm t, BaseTerm args)
            {
                if (!(t is StringTerm))
                {
                    IO.Error("Improper format string");

                    return null;
                }

                return Format(t.FunctorToString, args);
            }


            public static string Format(string fmtString, BaseTerm args)
            {
                string result = fmtString;

                if (args is ListTerm)
                {
                    ListTerm lt = (ListTerm) args;

                    if (!lt.IsProperList) return null;

                    try
                    {
                        return string.Format(result, lt.ToStringArray());
                    }
                    catch
                    {
                        IO.Error("Error while applying arguments to format string '{0}'", result);
                    }
                }
                else
                    try
                    {
                        return string.Format(result, args.ToString().Dequoted("'").Dequoted("\""));
                    }
                    catch
                    {
                        IO.Error("Error while applying arguments to format string '{0}'", result);
                    }

                return null;
            }



            public static string WrapWithMargin(string s, string margin, int lenMax)
                // Break up a string into pieces that are at most lenMax characters long, by
                // inserting spaces that are at most lenMax positions apart from each other.
                // If possible, spaces are inserted after 'separator characters'; otherwise
                // they are simply inserted at each lenMax position. Prefix a margin to the 2nd+ string.
            {
                const string separators = @" +-/*^!@():,.;=[]{}<>\";

                StringBuilder sb = new StringBuilder();
                bool first = true;
                int p = 0;
                int rem = s.Length - p;

                while (rem > lenMax)
                {
                    // get the position < lenMax of the last separator character
                    int i = s.Substring(p, lenMax).LastIndexOfAny(separators.ToCharArray(), lenMax - 1);
                    int segLen;

                    if (i == -1) segLen = lenMax;
                    else segLen = i + 1;

                    if (first) first = false;
                    else sb.Append(margin);
                    sb.Append(s.Substring(p, segLen));
                    sb.Append(Environment.NewLine);

                    p += segLen;
                    rem -= segLen;
                }

                if (first) first = false;
                else sb.Append(margin);

                if (rem != 0)
                {
                    sb.Append(s.Substring(p));
                    sb.Append(Environment.NewLine);
                }

                return sb.ToString();
            }


            public static void Assert(bool b, string s) // testing only
            {
                if (!b)
                    IO.WriteLine("Assertion violated:\r\n" + s);
                //throw new Exception ("Assertion violated:\r\n" + s);
            }


            public static void Assert(bool b, string s, params object[] o)
            {
                if (!b)
                    IO.WriteLine("Assertion violated:\r\n" + string.Format(s, o));
                //throw new Exception ("Assertion violated:\r\n" + string.Format (s, o));
            }


            public static void Check(bool b, string s)
            {
                if (!b)
                {
                    IO.Warning("Warning -- Check violated:\r\n" + s);
                }
            }


            public static void Check(bool b, string s, params object[] o)
            {
                if (!b)
                {
                    IO.Warning("Warning -- Check violated:\r\n" + string.Format(s, o));
                }
            }


            public static string ForceSpaces(string s, int lenMax)
                // Break up a string into pieces that are at most lenMax characters long, by
                // inserting spaces that are at most lenMax positions apart from each other.
                // If possible, spaces are inserted after 'separator characters'; otherwise
                // they are simply inserted at each lenMax position.
            {
                const string separators = " -/:,.;";

                if (lenMax < 2) IO.Error("Second argument of wrap must be > 1");

                return ForceSpaces(s, lenMax - 1, separators, 0); // 0 is current pos in separators
            }


            static string ForceSpaces(string s, int lenMax, string separators, int i)
            {
                int len = s.Length;
                StringBuilder sb = new StringBuilder();
                string blank = Environment.NewLine;

                // special cases
                if (len <= lenMax) return s; // nothing to do

                if (i == separators.Length) // done with all separators -- now simply insert spaces
                {
                    int r = len; // rest

                    while (r > 0)
                    {
                        sb.Append(s.Substring(len - r, (r > lenMax) ? lenMax : r));
                        sb.Append(blank);
                        r -= lenMax;
                    }

                    return sb.ToString().Trim();
                }
                // end of special cases

                string[] words = s.Split(new Char[] {separators[i]}); // split, using the current separator

                for (int k = 0; k < words.Length; k++)
                {
                    string t = ForceSpaces(words[k], lenMax, separators,
                        i + 1); // apply the next separator to each word

                    // do not re-place the separator after the last word
                    sb.Append(t + (k == words.Length - 1
                                  ? ""
                                  : (separators[i] + blank))); // recursively handle all seps
                }

                return sb.ToString();
            }

            // Return the ISO week Number for a date. Week 1 of a year is the
            // first week of the year in which there are more than three days, i.e.
            // the week in which the first Thursday of the year lies.
            public static int WeekNo(DateTime date)
            {
                // special case: if the date is in a week that starts on December 29,
                // 30 or 31 (i.e. date day in sun..wed), then return 1
                if (date.Month == 12 && date.Day >= 29 && date.DayOfWeek <= DayOfWeek.Wednesday)
                    return 1;

                DateTime jan1 = new DateTime(date.Year, 1, 1); // January 1st
                DayOfWeek jan1Day = jan1.DayOfWeek;
                // Jan 1st is in week 1 if jan1Day is in sun..wed, since only in that case
                // there are > 3 days in the week. Calculate the start date of week 1.
                // We want to know the number of days of week 1 that are in week 1
                bool jan1inWk1 = (jan1Day <= DayOfWeek.Wednesday); // indicates whether Jan 1st is in week 1
                DateTime startWk1 =
                    jan1.Subtract(new TimeSpan((int) jan1Day - (jan1inWk1 ? 7 : 0), 0, 0, 0));
                // Calculate the Number of days between the given date and the start
                // date of week 1 and (integer) divide that by 7. This is the weekno-1.
                return 1 + (date - startWk1).Days / 7;
            }



            // Log file, debugging
            public static void SetShow(bool mode)
            {
                showMode = mode;
            }


            public static void OpenLog()
            {
                logFile = new StreamWriter(new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite));
            }


            public static void WriteLogLine(bool abort, string s, params object[] pa)
            {
                if (abort)
                {
                    try
                    {
                        logFile.WriteLine(s, pa);
                        throw new Exception(String.Format(s, pa));
                    }
                    finally
                    {
                        CloseLog();
                    }
                }
                else
                {
                    IO.WriteLine(s, pa);
                    logFile.WriteLine(s, pa);
                }
            }


            public static void WriteLogLine(string s, params object[] pa)
            {
                WriteLogLine(false, s, pa);
            }


            public static void WriteLine(string s, params object[] pa)
            {
                WriteLogLine(false, s, pa);
            }


            public static void WriteLine(string s)
            {
                WriteLogLine(false, s);
            }


            public static void Show(string s, params object[] pa)
            {
                if (showMode) WriteLogLine(false, s, pa);
            }


            public static void Show(string s)
            {
                if (showMode) WriteLogLine(false, s);
            }


            public static void CloseLog()
            {
                logFile.Flush();
                logFile.Dispose();
            }
        }


        static string Spaces(int n)
        {
            return new string(' ', n);
        }

        #region Combination

        public class Combination // without repetition
        {
            IEnumerator<ListTerm> iterator;
            int k;

            public Combination(ListTerm t, int k)
            {
                BaseTermSet ts = new BaseTermSet(t);
                this.k = k;
                iterator = CombinationsEnum(ts, k).GetEnumerator();
            }

            public IEnumerator<ListTerm> Iterator
            {
                get { return iterator; }
            }


            IEnumerable<ListTerm> CombinationsEnum(List<BaseTerm> terms, int length)
            {
                for (int i = 0; i < terms.Count; i++)
                {
                    if (length == 1)
                        yield return new ListTerm(terms[i]);
                    // If > 1, return this one plus all combinations one shorter.
                    // Only use terms after the current one for the rest of the combinations
                    else
                        foreach (BaseTerm next in CombinationsEnum(terms.GetRange(i + 1, terms.Count - (i + 1)),
                            length - 1))
                            yield return new ListTerm(terms[i], next);
                }
            }
        }

        #endregion Combination

        #region Permutation

        public class Permutation
        {
            BaseTerm[] configuration;

            public Permutation(ListTerm t)
            {
                BaseTermSet ts = new BaseTermSet(t);
                ts.Sort();
                configuration = ts.ToArray();
            }

            public bool NextPermutation()
            {
                /*
                 Knuth's method
                 1. Find the largest index j such that a[j] < a[j+1]. If no such index exists, 
                    the permutation is the last permutation.
                 2. Find the largest index l such that a[j] < a[l]. Since j+1 is such an index, 
                    l is well defined and satisfies j < l.
                 3. Swap a[j] with a[l].
                 4. Reverse the sequence from a[j+1] up to and including the final element a[n].
                */
                int maxIndex = -1;

                for (var i = configuration.Length - 2; i >= 0; i--)
                {
                    if (configuration[i].CompareTo(configuration[i + 1]) == -1)
                    {
                        maxIndex = i;
                        break;
                    }
                }

                if (maxIndex < 0) return false;

                int maxIndex2 = -1;

                for (int i = configuration.Length - 1; i >= 0; i--)
                    if (configuration[maxIndex].CompareTo(configuration[i]) == -1)
                    {
                        maxIndex2 = i;

                        break;
                    }

                var tmp = configuration[maxIndex];
                configuration[maxIndex] = configuration[maxIndex2];
                configuration[maxIndex2] = tmp;

                for (int i = maxIndex + 1, j = configuration.Length - 1; i < j; i++, j--)
                {
                    tmp = configuration[i];
                    configuration[i] = configuration[j];
                    configuration[j] = tmp;
                }

                return true;
            }

            public IEnumerator<ListTerm> GetEnumerator()
            {
                do
                {
                    yield return ListTerm.ListFromArray(configuration);
                } while (NextPermutation());
            }
        }

        #endregion Permutation
    }

    public static class Extensions
    {
        static Regex atomPattern = new Regex( // \p{Ll} means Unicode lowercase letter
            @"^([+\-*/\\^<=>`~:.?@#$&]+|\p{Ll}[\w_]*|('[^']*')+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        static Regex unsignedInteger = new Regex(
            @"^(\d+)?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        static Regex signedNumber = new Regex(
            @"^([+-]?((\d+\.)?\d+)((E|e)[+-]?\d+)?)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        static Regex signedImagNumber = new Regex(
            @"^([+-]?((\d+\.)?\d+)((E|e)[+-]?\d+)?i?)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        // decimal d = decimal.Parse (value, System.Globalization.NumberStyles.HexNumber) or:
        // Convert.ToInt32(value, 16). The 16 is the "fromBase" parameter, 16 for hex
        // currently not used:
        static Regex hexNumber = new Regex(
            @"^(0x[0-9a-fA-F]+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );


        static Regex tokens = new Regex(
            // identifiers, signed numbers and sequences of non-whites, separated by whites
            @"\s*(?<token>([\p{L}_]+\d*|[+-]?((\d+\.)?\d+)((E|e)[+-]?\d+)?|\S+))\s*",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );


        static Regex stringLiteral = new Regex(
            @"^(?<char>(\\('|""|\\|0|a|b|f|n|r|t|v|u[0-9a-fA-F]{4}|x[0-9a-fA-F]{1,4}|U[0-9a-fA-F]{8}|.?))|[^\\])+$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        //@"\\(?:(?<h>'|""|\\|0|a|b|f|n|r|t|v)|u(?<h>[0-9a-fA-F]{4})|x(?<h>[0-9a-fA-F]{1,4})|U(?<h>[0-9a-fA-F]{8})|(?<h>.))",

        static Regex escapedChar = new Regex(
            @"\\(?:(?<h>'|""|\\|0|a|b|f|n|r|t|v)|u(?<h>[0-9a-fA-F]{4})|x(?<h>[0-9a-fA-F]{1,4})|U(?<h>[0-9a-fA-F]{8}))",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        // StringBuilder
        public static void AppendPacked(this StringBuilder sb, string s)
        {
            sb.Append(s.Packed());
        }

        public static void AppendPacked(this StringBuilder sb, string s, bool mustPack)
        {
            if (mustPack)
                sb.Append(s.Packed());
            else
                sb.AppendPossiblySpaced(s);
        }

        public static void AppendLine(this StringBuilder sb, string format, params object[] args)
        {
            sb.AppendLine(string.Format(format, args));
        }

        public static void AppendPossiblySpaced(this StringBuilder sb, string s)
        {
            if (string.IsNullOrEmpty(s)) return;

            if (sb.Length == 0)
                sb.Append(s);
            else
            {
                char startChar = s[0];
                char finalChar = sb[sb.Length - 1];

                // Determine whether a space must be inserted. Rules (De Bosschere, cf. docu):
                //
                // 1. A variable or an identifier shall not be followed by an alphanumeric;
                // 2. A quoted token shall not be followed by a quote;
                // 3. A numeric token shall never be followed by a digit;
                // 4. A Special (+, -, :-, ...) shall not be followed by a Special;
                // 5. A prefix operator is separated from an open token ('(', '[', '{') by a space
                //
                // Pathologic cases:
                // - op(300, xfy, .) -> .(1,2) must be output as 1 .2 (*not* as 1 . 2, because
                //                      then the '. ' will be interpreted as end of input.
                // - op(300, xf, e10) -> e10(3.1) must be output as 3.1 e10
                // In practice (and here), numeric constants are separated from identifiers
                // (not from specials) by a space: 10is 30mod 20 => 10 is 30 mod 20
                if ((finalChar.IsSpecialAtomChar() && startChar.IsSpecialAtomChar()) ||
                    (Char.IsLetterOrDigit(finalChar) && Char.IsLetterOrDigit(startChar)) ||
                    (Char.IsDigit(finalChar) && startChar == '.') ||
                    (finalChar == '@' && startChar == '"')
                )
                    sb.Append(' ');

                sb.Append(s);
            }
        }

        // string
        public static string Reverse(this string s)
        {
            char[] a = s.ToCharArray();
            Array.Reverse(a);

            return new string(a);
        }

        public static string Mirror(this string s)
        {
            char[] a = s.ToCharArray();
            const string pairs = "{}{[][<><()(";
            Array.Reverse(a);
            int p;

            for (int i = 0; i < a.Length; i++)
                if ((p = pairs.IndexOf(a[i])) > -1)
                    a[i] = pairs[p + 1];

            return new string(a);
        }

        public static bool Contains(this string s, char c)
        {
            return s.IndexOf(c) >= 0;
        }

        public static bool HasUnsignedIntegerFormat(this string s)
        {
            return unsignedInteger.Match(s).Success;
        }

        public static bool HasSignedRealNumberFormat(this string s)
        {
            return signedNumber.Match(s).Success;
        }

        public static bool HasSignedImagNumberFormat(this string s)
        {
            return signedImagNumber.Match(s).Success;
        }


        public static string[] Tokens(this string s)
        {
            MatchCollection mc = tokens.Matches(s);
            string[] result = new string [mc.Count];

            for (int i = 0; i < mc.Count; i++)
                result[i] = mc[i].Value.Trim();

            return result;
        }


        public static string RemoveUnnecessaryAtomQuotes(this string s)
        {
            int len = s.Length;
            string a;

            // check whether the string is quoted at all
            if (len < 2 || s[0] != '\'' || s[len - 1] != '\'') return s;

            // check whether the unquoted version is an Atom
            return (atomPattern.Match((a = s.Substring(1, len - 2))).Success) ? a : s;
        }


        public static bool HasAtomFormat(this string s)
        {
            return atomPattern.Match(s).Success;
        }


        public static string ToAtom(this string s) // numbers are quoted
        {
            if (s.HasAtomFormat()) return s.RemoveUnnecessaryAtomQuotes(); // e.g. 'a' -> a

            return '\'' + s.Replace("'", "''") + '\'';
        }


        public static string ToAtomic(this string s, out TermType type) // numbers are not quoted
        {
            if (s.HasSignedRealNumberFormat())
            {
                type = TermType.Number;

                return s;
            }

            if (s.HasSignedImagNumberFormat())
            {
                type = TermType.ImagNumber;

                return s.Substring(0, s.Length - 1);
            }

            type = TermType.Atom;

            return s.ToAtom();
        }


        public static string MakeAtomic(this string s)
        {
            TermType type;

            return s.ToAtomic(out type);
        }


        public static string Dequoted(this string s) // remove ' or " quotes (if any)
        {
            if (s == null) return null;

            int len = s.Length;

            // check whether the string is quoted at all
            if (len < 2 || (s[0] != '\'' && s[0] != '"') || (s[0] != s[len - 1]))
                return s;

            return s.Substring(1, len - 2).Replace(new string(s[0], 2), new string(s[0], 1));
        }


        public static string Dequoted(this string s, string quote) // remove ' or " quotes (if any)
        {
            int len = s.Length;

            // check whether the string is quoted at all
            if (quote.Length == 0 || len < 2 || s[0] != quote[0] || s[len - 1] != quote[0])
                return s;

            return s.Substring(1, len - 2).Replace(quote + quote, quote);
        }


        public static string Enquote(this string s, char quoteChar) // enclose in ' or " quotes
        {
            string q = quoteChar.ToString();

            return (s == null) ? q + q : q + s.Replace(q, q + q) + q;
        }


        public static string EscapeDoubleQuotes(this string s) // replace " by \"
        {
            return s.Replace(@"""", @"\""");
        }


        public static string Packed(this string s)
        {
            return '(' + s + ')';
        }


        public static string AddEndDot(this string s)
        {
            string t = s.Trim();

            return (t.EndsWith(".")) ? t : t + '.';
        }


        public static string Packed(this string s, bool mustPack)
        {
            return mustPack ? '(' + s + ')' : s;
        }

        // resolves escaped characters, cf. CSProlog.exe.config file
        public static string Unescaped(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            if (stringLiteral.Match(s).Success)
                return escapedChar.Replace(s,
                    match => (string.Format("{0}", match.Groups[1].Value.ResolveEscape())));

            PrologEngine.IO.Error("Unrecognized escape sequence in string \"{0}\"" +
                                  "\r\n(cf. CSProlog.exe.config in .exe-directory)", s);

            return null;
        }

        static string ResolveEscape(this string s) // resolve escaped characters
        {
            switch (s)
            {
                case "\'":
                case "\"":
                case "\\": return s;
                case "0": return "\0";
                case "a": return "\a";
                case "b": return "\b";
                case "f": return "\f";
                case "n": return "\n";
                case "r": return "\r";
                case "t": return "\t";
                case "v": return "\v";
            }

            // ignore the / if it precedes a character that does not need an escape
            if (s.Length == 1) return s;

            // according to the regex, s now can only be a hex number
            return ((char) Int32.Parse(s, NumberStyles.HexNumber)).ToString();
        }


        public static string Repeat(this string s, int n)
        {
            return (new String('*', n)).Replace("*", s);
        }

        // Levenshtein Distance, i.e. http://www.merriampark.com/ld.htm
        public static float Levenshtein(this string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return string.IsNullOrEmpty(b) ? 0.0F : 1.0F;

            if (string.IsNullOrEmpty(b))
                return 1.0F;

            int n = a.Length;
            int m = b.Length;
            int[,] d = new int [n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;

            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                int d0 = d[i - 1, j] + 1;
                int d1 = d[i, j - 1] + 1;
                int d2 = d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                d[i, j] = Math.Min(Math.Min(d0, d1), d2);
            }

            return (float) d[n, m] / Math.Max(n, m);
        }

        // Jon Skeet -- C# In Depth, 1st edition, p.176
        public abstract class Range<T> : IEnumerable<T> where T : IComparable<T>
        {
            readonly T end;
            readonly T start;

            public Range(T start, T end)
            {
                if (start.CompareTo(end) > 0)
                    throw new ArgumentOutOfRangeException();

                this.start = start;
                this.end = end;
            }

            public T Start
            {
                get { return start; }
            }

            public T End
            {
                get { return end; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                T value = start;

                while (value.CompareTo(end) < 0)
                {
                    yield return value;
                    value = GetNextValue(value);
                }

                if (value.CompareTo(end) == 0)
                {
                    yield return value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Contains(T value)
            {
                return value.CompareTo(start) >= 0 && value.CompareTo(end) <= 0;
            }

            protected abstract T GetNextValue(T current);
        }
    }

    // Burrows–Wheeler transform -- cf. https://en.wikipedia.org/wiki/Burrows%E2%80%93Wheeler_transform

    // Code from https://gist.github.com/Lordron/5039958

    /* Main program sample
     
        const string str = "Better ask the way than to go astray!";
  
        byte[] buffer_in = Encoding.UTF8.GetBytes(str);
        byte[] buffer_out = new byte[buffer_in.Length];
        byte[] buffer_decode = new byte[buffer_in.Length];
  
        BWTImplementation bwt = new BWTImplementation();
  
        int primary_index = 0;
        bwt.bwt_encode(buffer_in, buffer_out, buffer_in.Length, ref primary_index);
        bwt.bwt_decode(buffer_out, buffer_decode, buffer_in.Length, primary_index);
  
        Console.WriteLine("Decoded string: {0}", Encoding.UTF8.GetString(buffer_decode));
  
     */
    class BWTImplementation
    {
        public void bwt_encode(byte[] buf_in, byte[] buf_out, int size, ref int primary_index)
        {
            int[] indices = new int [size];
            for (int i = 0; i < size; i++)
                indices[i] = i;

            Array.Sort(indices, 0, size, new BWTComparator(buf_in, size));

            for (int i = 0; i < size; i++)
                buf_out[i] = buf_in[(indices[i] + size - 1) % size];

            for (int i = 0; i < size; i++)
            {
                if (indices[i] == 1)
                {
                    primary_index = i;
                    return;
                }
            }
        }

        public void bwt_decode(byte[] buf_encoded, byte[] buf_decoded, int size, int primary_index)
        {
            byte[] F = new byte [size];
            int[] buckets = new int [0x100];
            int[] indices = new int [size];

            for (int i = 0; i < 0x100; i++)
                buckets[i] = 0;

            for (int i = 0; i < size; i++)
                buckets[buf_encoded[i]]++;

            for (int i = 0, k = 0; i < 0x100; i++)
            {
                for (int j = 0; j < buckets[i]; j++)
                {
                    F[k++] = (byte) i;
                }
            }

            for (int i = 0, j = 0; i < 0x100; i++)
            {
                while (i > F[j] && j < size - 1)
                {
                    j++;
                }

                buckets[i] = j;
            }

            for (int i = 0; i < size; i++)
                indices[buckets[buf_encoded[i]]++] = i;

            for (int i = 0, j = primary_index; i < size; i++)
            {
                buf_decoded[i] = buf_encoded[j];
                j = indices[j];
            }
        }
    }

    class BWTComparator : IComparer<int>
    {
        private byte[] rotlexcmp_buf = null;
        private int rottexcmp_bufsize = 0;

        public BWTComparator(byte[] array, int size)
        {
            rotlexcmp_buf = array;
            rottexcmp_bufsize = size;
        }

        public int Compare(int li, int ri)
        {
            int ac = rottexcmp_bufsize;
            while (rotlexcmp_buf[li] == rotlexcmp_buf[ri])
            {
                if (++li == rottexcmp_bufsize)
                    li = 0;
                if (++ri == rottexcmp_bufsize)
                    ri = 0;
                if (--ac <= 0)
                    return 0;
            }

            if (rotlexcmp_buf[li] > rotlexcmp_buf[ri])
                return 1;

            return -1;
        }
    }
}