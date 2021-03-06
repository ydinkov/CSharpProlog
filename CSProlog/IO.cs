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
using System.IO;
using System.Text;

namespace Prolog
{
    using ApplicationException = Exception;


    #region BasicIo

    public abstract class BasicIo
    {
        public void Write(string s, params object[] o)
        {
            Write(string.Format(format: s, args: o));
        }

        public void WriteLine(string s, params object[] o)
        {
            WriteLine(string.Format(format: s, args: o));
        }

        #region abstract methods

        public abstract string ReadLine();

        public abstract int ReadChar();

        public abstract void Write(string s);

        public abstract void WriteLine(string s);

        public abstract void WriteLine();

        public abstract void Clear();

        public abstract void Reset();

        #endregion abstract methods
    }

    #endregion BasicIo

    #region DosIO. Base class for DOS IO

    public class DosIO : BasicIo
    {
        public override string ReadLine()
        {
            return Console.ReadLine();
        }


        public override int ReadChar()
        {
            return Console.ReadKey().KeyChar;
        }


        public override void Write(string s)
        {
            Console.Write( s);
        }


        public override void WriteLine(string s)
        {
            Console.WriteLine( s);
        }


        public override void WriteLine()
        {
            Console.WriteLine();
        }


        public override void Clear()
        {
            Console.Clear();
        }


        public override void Reset()
        {
        }
    }

    #endregion DosIO

    #region FileIO. Base class for text File IO

    public class FileIO : BasicIo
    {
        private readonly StreamReader inFile;
        private readonly StreamWriter outFile;

        public FileIO(string inFileName, string outFileName)
        {
            // no try/catch, as I would not know how to handle the exception caught
            inFile = new StreamReader(File.OpenRead(path: inFileName));
            outFile = new StreamWriter(File.OpenWrite(path: outFileName));
            outFile.AutoFlush = true; // file will contain all output even if not closed explicitly
        }

        public override string ReadLine()
        {
            if (inFile == null)
                throw new ApplicationException("FileIO class: input file is not defined");

            return inFile.ReadLine();
        }


        public override int ReadChar()
        {
            if (inFile == null)
                throw new ApplicationException("FileIO class: input file is not defined");

            return inFile.Read();
        }


        public override void Write(string s)
        {
            if (outFile == null)
                throw new ApplicationException("FileIO class: output file is not defined");

            outFile.Write( s);
        }


        public override void WriteLine(string s)
        {
            if (outFile == null)
                throw new ApplicationException("FileIO class: output file is not defined");

            outFile.WriteLine( s);
        }


        public override void WriteLine()
        {
            if (outFile == null)
                throw new ApplicationException("FileIO class: output file is not defined");

            outFile.WriteLine();
        }


        public override void Clear()
        {
        }


        public override void Reset()
        {
        }


        public void Close()
        {
            if (inFile != null) inFile.Dispose();

            if (outFile != null) outFile.Dispose();
        }
    }

    #endregion FileIO

    public partial class PrologEngine
    {
        private FileReaderTerm currentFileReader;
        private FileWriterTerm currentFileWriter;

        public BasicIo BasicIO
        {
            set => IO.BasicIO = value;
        }


        // for IO *not* generated by Prolog predicates and not subject to
        // current input and current output (i.e. error messages etc.)
        public static class IO
        {
            public static bool Verbose = true; // message display
            private static int debugLevel;
            public static BasicIo BasicIO { get; set; }

            public static void SetDebugLevel(int level)
            {
                debugLevel = level;
            }

            public static bool Error(string msg, params object[] o)
            {
                Error(string.Format(format: msg, args: o));

                return false;
            }

            public static bool Error(string msg)
            {
                if (Globals.LineNo == -1) // interactive
                    throw new ParserException("\r\n*** error: " + msg);
                throw new ParserException(string.Format("\r\n*** error in line {0} at position {1}: {2}",
                    arg0: Globals.LineNo, arg1: Globals.ColNo, arg2: msg));
            }


            public static void Warning(string msg, params object[] o)
            {
                BasicIO.WriteLine(string.Format("\r\n*** warning: " + msg, args: o));
            }


            public static void Warning(string msg)
            {
                BasicIO.WriteLine("\r\n*** warning: " + msg);
            }


            public static void Message(string msg, params object[] o)
            {
                BasicIO.WriteLine(string.Format("\r\n--- " + msg, args: o));
            }


            public static void Message(string msg)
            {
                BasicIO.WriteLine("\r\n--- " + msg);
            }


            public static void Fatal(string msg, params object[] o)
            {
                throw new Exception("\r\n*** fatal: " + string.Format(format: msg, args: o));
            }


            public static void Fatal(string msg)
            {
                throw new Exception("\r\n*** fatal: " + msg);
            }


            public static string ReadLine()
            {
                return BasicIO.ReadLine();
            }


            public static int ReadChar()
            {
                return BasicIO.ReadChar();
            }


            public static void Write(string s, params object[] o)
            {
                BasicIO.Write(string.Format(format: s, args: o));
            }


            public static void Write(string s)
            {
                BasicIO.Write( s);
            }


            public static void WriteLine(string s, params object[] o)
            {
                BasicIO.WriteLine(string.Format(format: s, args: o));
            }


            public static void WriteLine(string s)
            {
                BasicIO.WriteLine( s);
            }


            public static void WriteLine()
            {
                BasicIO.WriteLine();
            }


            public static void ClearScreen()
            {
                BasicIO.Clear();
            }
        }

        #region BaseRead(Line/Term/Char)CurrentInput and BaseWriteCurrentOutput

        // BaseReadCurrentInput. Input is read from StandardInput.
        // StandardInput is the file set by the see command, or Console if no such file exists.
        private string BaseReadLineCurrentInput() // returns null at end of file
        {
            return currentFileReader == null ? IO.ReadLine() : currentFileReader.ReadLine();
        }


        private BaseTerm BaseReadTermCurrentInput()
        {
            if (currentFileReader == null)
            {
                var query = new StringBuilder();
                string line;
                var p = new PrologParser(this);

                var first = true;

                while (true)
                {
                    IO.Write("|: ");

                    if (first) first = false;
                    else query.AppendLine();

                    if ((line = IO.ReadLine()) == null) return FileTerm.END_OF_FILE;

                    query.Append(line = line.Trim());

                    if (line.EndsWith(".")) break;
                }

                p.StreamIn = "&reading\r\n" + query; // equal to parser ReadingSym
                var result = p.ReadTerm;

                return result == null ? FileTerm.END_OF_FILE : result;
            }

            return currentFileReader.ReadTerm();
        }


        private int BaseReadCharCurrentInput() // returns -1 at end of file
        {
            return currentFileReader == null ? IO.ReadChar() : currentFileReader.ReadChar();
        }


        // BaseWriteCurrentOutput
        // Output from print, display, tab, put etc. is written to StandardOutput.
        // StandardOutput is the file set by the tell command, or Console if
        // no such file exists.
        private void BaseWriteCurrentOutput(string s)
        {
            if (currentFileWriter == null)
                IO.Write( s);
            else
                currentFileWriter.Write( s);
        }


        private void BaseWriteCurrentOutput(string s, object[] args)
        {
            BaseWriteCurrentOutput(string.Format(format: s, args: args));
        }

        #endregion BaseRead(Line/Term/Char)CurrentInput and BaseWriteCurrentOutput


        #region Read(Line/Char) and Write(Line)

        private BaseTerm ReadTerm()
        {
            return BaseReadTermCurrentInput();
        }


        private string ReadLine()
        {
            return BaseReadLineCurrentInput();
        }


        private int ReadChar()
        {
            return BaseReadCharCurrentInput();
        }


        private void Write(BaseTerm t, bool dequote)
        {
            if (t.IsString)
                BaseWriteCurrentOutput(dequote ? t.FunctorToString : '"' + t.FunctorToString + '"');
            else if (t.IsAtom)
                BaseWriteCurrentOutput(dequote ? t.FunctorToString.Dequoted("'") : t.FunctorToString);
            else
                BaseWriteCurrentOutput(t.ToString());
        }


        public void Write(string s)
        {
            BaseWriteCurrentOutput( s);
        }


        public void Write(string s, params object[] args)
        {
            BaseWriteCurrentOutput( s, args: args);
        }


        public void WriteLine(string s)
        {
            BaseWriteCurrentOutput(s + Environment.NewLine);
        }


        public void WriteLine(string s, params object[] args)
        {
            BaseWriteCurrentOutput(s + Environment.NewLine, args: args);
        }


        public void NewLine()
        {
            BaseWriteCurrentOutput( Environment.NewLine);
        }

        #endregion Read(Line/Char) and Write(Line)
    }
}