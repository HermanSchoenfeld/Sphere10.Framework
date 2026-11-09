# Sphere10.Framework.NET.Tests

NUnit tests for the expression scanner and parser of the [math runtime](../../src/Sphere10.Framework.Runtime/MathRuntime/README.md) in `Sphere10.Framework.Runtime`. The project also references `Sphere10.Framework` and `Sphere10.Framework.NUnit`.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.NET.Tests/Sphere10.Framework.NET.Tests.csproj -c Release -p:BuildRevision=0
```

Run the parser fixture only:

```powershell
dotnet test tests/Sphere10.Framework.NET.Tests/Sphere10.Framework.NET.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~MathCompilerParserTests"
```

The fixtures use the `Sphere10.Framework.UnitTests` namespace. The suite needs no network, database or desktop session.

## Contents

### Math

Fixtures for the `Sphere10.Framework.Maths.Compiler` scanner and parser.

| Fixture | Purpose |
| --- | --- |
| [MathCompilerParserTests](Math/MathCompilerParserTests.cs) | `Parser.ParseExpression` syntax trees for operator precedence and associativity, unary minus, powers, power towers and mixed multiplication and division. |
| [MathCompilerScannerTests](Math/MathCompilerScannerTests.cs) | `Scanner` tokens for numbers, decimals, reals with signs, identifiers, operators, brackets, parentheses, punctuation, logical and comparison operators, the `let`, `if`, `then` and `else` keywords and the end of the code. |
