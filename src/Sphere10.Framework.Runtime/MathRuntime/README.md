# Math Expression Runtime

Scans, parses, type-checks and compiles single-variable math expressions into `IFunction` objects that evaluate a `double`.

The pipeline runs `Scanner` (text to `Token`s), `Parser` (a precedence-climbing recursive-descent parser that builds a `SyntaxTree`), `BasicTypeChecker` (checks identifiers and calls against an `IMathContext`) and `BasicFunctionGenerator` (emits a C# class implementing `IDynamicallyCompiledFunction` and compiles it). The usual entry point is `DefaultMathContext.GenerateFunction(expression)`, which assumes the parameter is named `x`. `IMathContext` holds variables and functions, and a context with a `ParentContext` falls back to the parent for lookups and assignments. `IFunction` itself is declared in the core library at [`Maths/IFunction.cs`](../../Sphere10.Framework/Maths/IFunction.cs).

All types live in the `Sphere10.Framework.Maths.Compiler` namespace. This is legacy code. Code generation uses CodeDom `CSharpCodeProvider.CompileAssemblyFromSource`, which modern .NET runtimes do not support, and the emitted source references an assembly that is not part of this repository. The scanner, parser and syntax trees work on their own and are the reusable part. [CSharpBuilder](../CSharpBuilder/README.md) is the sibling domain that builds C# source text with the same CodeDom approach.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CodeErrorException`](CodeErrorException.cs) | class | Compiler error carrying line number, start and end positions and optional offsets used when formatting the message |
| [`CodeErrorsException`](CodeErrorsException.cs) | class | Aggregates the list of `CodeErrorException`s found during one compilation phase |
| [`CompilerException`](CompilerException.cs) | class | Base `ApplicationException` for every compiler-generated exception |
| [`CompilerUtil`](CompilerUtil.cs) | class | Static `IsValidVariable` check that a name starts with a letter and continues with letters or digits |
| [`InternalCompilerException`](InternalCompilerException.cs) | class | Signals a compiler defect, such as an operator declared without its `OpAttr` or an unresolvable token |

### CodeGen

| Type | Kind | Purpose |
|---|---|---|
| [`BasicFunctionGenerator`](CodeGen/BasicFunctionGenerator.cs) | class | Parses and type-checks an expression, emits C# for it and compiles it into an `IFunction`, with virtual emit hooks per node type |
| [`BasicTypeChecker`](CodeGen/BasicTypeChecker.cs) | class | Checks that variables and functions exist in the math context and that declarations and calls take one parameter, returning the errors found |
| [`CodeGenerationException`](CodeGen/CodeGenerationException.cs) | class | Compiler exception reserved for code-generation failures, which no code in this project currently throws |
| [`IDynamicallyCompiledFunction`](CodeGen/IDynamicallyCompiledFunction.cs) | interface | `IFunction` produced by code generation that exposes the `IMathContext` it evaluates against |

### Parser

| Type | Kind | Purpose |
|---|---|---|
| [`Aryness`](Parser/Operator.cs) | enum | Whether an operator is unary or binary |
| [`Associativity`](Parser/Associativity.cs) | enum | Left or right associativity of an operator |
| [`BinaryOperatorTree`](Parser/BinaryOperatorTree.cs) | class | Syntax node for a binary operator with left-hand and right-hand operands |
| [`FactorTree`](Parser/FactorTree.cs) | class | Leaf syntax node for an identifier or number token |
| [`FunctionCallTree`](Parser/FunctionCallTree.cs) | class | Syntax node for a function call and its argument expressions |
| [`FunctionDeclarationTree`](Parser/FunctionDeclarationTree.cs) | class | Syntax node for `LET name(params) = expression` with its parameter tokens and body |
| [`IfThenElseTree`](Parser/IfThenElseTree.cs) | class | Conditional syntax node that adds an else expression to `IfThenTree` |
| [`IfThenTree`](Parser/IfThenTree.cs) | class | Conditional syntax node with a condition and an expression |
| [`OpAttr`](Parser/Operator.cs) | class | Attribute on each `Operator` member giving its precedence, aryness and associativity |
| [`Operator`](Parser/Operator.cs) | enum | Supported operators, from member selection and power down to logical and/or, annotated with `OpAttr` |
| [`Parser`](Parser/Parser.cs) | class | Precedence-climbing recursive-descent parser that turns scanner tokens into expression or function-declaration trees |
| [`ParserException`](Parser/ParserException.cs) | class | Parse error naming the unexpected token and the token types that were expected |
| [`SyntaxTree`](Parser/SyntaxTree.cs) | class | Base syntax node holding the token it was built from |
| [`UnaryOperatorTree`](Parser/UnaryOperatorTree.cs) | class | Syntax node for a unary operator and its operand |

### Runtime

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultFunctionContext`](Runtime/DefaultFunctionContext.cs) | class | Dictionary-backed `IFunctionContext` that defers to the parent context's functions when a name is not local |
| [`DefaultMathContext`](Runtime/DefaultMathContext.cs) | class | Default `IMathContext` with variable and function contexts and `GenerateFunction` overloads that compile an expression |
| [`DefaultVariableContext`](Runtime/DefaultVariableContext.cs) | class | Dictionary-backed `IVariableContext` of `double` values that defers to the parent context's variables |
| [`IFunctionContext`](Runtime/IFunctionContext.cs) | interface | Named `IFunction` store with contains, remove and indexer access |
| [`IMathContext`](Runtime/IMathContext.cs) | interface | Scope of variables and functions with an optional parent and expression compilation |
| [`IVariableContext`](Runtime/IVariableContext.cs) | interface | Named `double` variable store with contains, remove and indexer access |

### Scanner

| Type | Kind | Purpose |
|---|---|---|
| [`Scanner`](Scanner/Scanner.cs) | class | State-machine tokenizer that reads a `TextReader` and returns one `Token` per `GetNextToken` call |
| [`ScannerErrorState`](Scanner/ScannerErrorState.cs) | enum | Reason a scan failed, such as a blank mantissa or exponent or an unexpected character |
| [`ScannerException`](Scanner/ScannerException.cs) | class | Scan error carrying the `ScannerErrorState` and the source position |
| [`ScannerState`](Scanner/ScannerState.cs) | enum | States of the scanner's finite-state machine |
| [`Token`](Scanner/Token.cs) | class | Scanned token with its type, text value, line and start and end positions |
| [`TokenType`](Scanner/TokenType.cs) | enum | Token kinds: scalars, identifiers, operators, brackets, keywords (`LET`, `IF`, `THEN`, `ELSE`) and end of code |
