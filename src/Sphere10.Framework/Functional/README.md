# Functional

Generic arithmetic and comparison operators compiled from expression trees, and helpers that build and wrap delegates.

`Operator<T>` exposes the standard operators of a type (`Add`, `Subtract`, `Multiply`, `Divide`, `Negate`, the bitwise operators and the comparisons) as cached delegates, so generic code can do arithmetic on any `T` that defines them. `Operator<TValue, TResult>` does the same for mixed operand types where the result has the first operand's type. `Tools.OperatorTool` wraps both behind generic methods with type inference. `Tools.Expressions` compiles the unary and binary expression delegates behind them, and a type without the operator gets a delegate that throws `InvalidOperationException` when called. `Tools.Lambda` builds `Action` wrappers that retry, fail over, ignore or handle exceptions, or run on a task, and the matching `Action` extensions are in [Extensions](../Extensions/README.md).

`Tools.Expressions`, `Tools.Lambda` and `Tools.OperatorTool` are in the `Tools` namespace. The two `Operator` classes are in `Sphere10.Framework`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Operator<T>`](Operator.cs) | static class | Cached delegates for the arithmetic, bitwise and comparison operators of a single type, plus `Zero`. |
| [`Operator<TValue, TResult>`](Operator.cs) | static class | Cached delegates for conversion and arithmetic between two operand types. |
| [`Tools.Expressions`](ExpressionTool.cs) | tools | Compiles unary and binary expression bodies into delegates, optionally converting operands to the result type. |
| [`Tools.Lambda`](LambdaTool.cs) | tools | Builds action wrappers for retry, fail-over, exception handling and asynchronous execution, setter expressions and no-op delegates. |
| [`Tools.OperatorTool`](OperatorTool.cs) | tools | Generic `Add`, `Subtract`, `Multiply`, `Divide`, comparison, bitwise and null-aware accumulation methods with type inference. |
