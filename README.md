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
## Solution Layout
### CSProlog
Prolog Engine

### CSProlog.Core.Test
Unit Tests

### PL.NETCore
Dotnet Core Console Interactive Interpreter (tested in linux and windows)

### PLd
DOS Console Interactive Interpreter

### PLw
Windows Forms Example

### PLx
An example of how to use the engine within another Program



## License

[GNU LGPL v.3](LICENSE)
