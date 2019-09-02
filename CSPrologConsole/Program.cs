using System;
using Prolog;

namespace CSPrologConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var prolog = new PrologEngine(persistentCommandHistory: false);

            // 'socrates' is human.
            prolog.ConsultFromString("human(socrates).");
            // human is bound to die.
            prolog.ConsultFromString("mortal(X) :- human(X).");

            // Question: Shall 'socrates' die?
            var solution = prolog.GetFirstSolution(query: "mortal(socrates).");
            Console.WriteLine(solution.Solved); // = "True" (Yes!)
        }
    }
}