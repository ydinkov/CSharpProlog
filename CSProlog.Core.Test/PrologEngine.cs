using System;
using Xunit;
using Prolog;

namespace CSProlog.Core.Test
{
    public class PrologEngineTest
    {
        [Fact]
        public void BasicTest()
        {
            var prolog = new PrologEngine(persistentCommandHistory: false);

            // 'socrates' is human.
            prolog.ConsultFromString("human(socrates).");
            // 'R2-D2' is droid.
            prolog.ConsultFromString("droid(r2d2).");
            // human is bound to die.
            prolog.ConsultFromString("mortal(X) :- human(X).");

            // Question: Shall 'socrates' die?
            var solution1 = prolog.GetFirstSolution(query: "mortal(socrates).");
            Assert.True(solution1.Solved); // = "True" (Yes)

            // Question: Shall 'R2-D2' die?
            var solution2 = prolog.GetFirstSolution(query: "mortal(r2d2).");
            Assert.False(solution2.Solved); // = "False" (No)
        }
    }
}
