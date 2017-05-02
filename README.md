# CSharpProlog [![NuGet Package](https://img.shields.io/nuget/v/CSProlog.svg)](https://www.nuget.org/packages/CSProlog/) [![Build status](https://ci.appveyor.com/api/projects/status/prufu2gwyb63l3ua?svg=true)](https://ci.appveyor.com/project/jsakamoto/csharpprolog)
A C# implementation of Prolog

```csharp
// PM> Install-Package CSProlog -pre
using System;
using Prolog;

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
```

## License

[GNU LGPL v.3](LICENSE)
