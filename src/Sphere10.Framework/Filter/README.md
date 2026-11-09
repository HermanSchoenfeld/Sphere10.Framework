# Filter

A serializable filter and sort model that compiles into LINQ expressions for in-memory sequences and `IQueryable` sources.

A filter is a tree of `FilterExpression` nodes. `FilterCondition` leaves compare one property, named as a string, against a `FilterValue` through a `FilterOperator`, and `FilterGroup` nodes combine children with `And` or `Or` to any depth. `FilterBuilder` creates conditions and groups in one expression. `FilterCompiler<T>` turns a tree into an `Expression<Func<T, bool>>` or a compiled delegate, with ordinal case-insensitive string comparisons. `FilterExtensions` applies a filter, a `SortExpression` and paging to an `IEnumerable<T>` and returns the [DataSource](../DataSource/README.md) page type `DataSourceItems<T>`.

```csharp
var filter = FilterBuilder.And(
	FilterBuilder.Condition("Name", FilterOperator.StartsWith, "A"),
	FilterBuilder.Between("Age", 18, 65));
var matches = people.ApplyFilter(filter);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FilterBuilder`](FilterBuilder.cs) | static class | Creates `FilterCondition` leaves (`Condition`, `Between`, `In`) and `FilterGroup` nodes (`And`, `Or`). |
| [`FilterCompiler<T>`](FilterCompiler.cs) | static class | Compiles a filter tree into an `Expression<Func<T, bool>>` or a `Func<T, bool>`. |
| [`FilterCondition`](FilterCondition.cs) | class | Leaf node that compares a named property with a value through an operator. |
| [`FilterConjunction`](FilterConjunction.cs) | enum | Logical conjunction of a group: `And` or `Or`. |
| [`FilterExpression`](FilterExpression.cs) | abstract class | Base of a hierarchical, recursive filter expression tree. |
| [`FilterExtensions`](FilterExtensions.cs) | static class | `ApplyFilter`, `ApplySort` and `ApplyQuery` over in-memory sequences. |
| [`FilterGroup`](FilterGroup.cs) | class | Composite node that combines child expressions with a conjunction. |
| [`FilterOperator`](FilterOperator.cs) | enum | Comparison operators, from `Equals` and `Contains` to `In`, `Between`, `IsEmpty` and `IsNotEmpty`. |
| [`FilterValue`](FilterValue.cs) | abstract class | Base of the operand value of a condition, with `None`, `Single` and `Multiple` factories and a data type hint. |
| [`FilterValueDataType`](FilterValueDataType.cs) | enum | Intended data type of a filter value, used for conversion and editor hints. |
| [`FilterValueMultiple`](FilterValue.cs) | class | Filter value with several operands, such as the bounds of `Between` or the set of `In`. |
| [`FilterValueNone`](FilterValue.cs) | class | Filter value without an operand, for unary operators such as `IsEmpty`. |
| [`FilterValueSingle`](FilterValue.cs) | class | Filter value with one operand, used by most comparison operators. |
| [`FilterValueType`](FilterValueType.cs) | enum | Cardinality of a filter value: `None`, `Single` or `Multiple`. |
| [`SortExpression`](SortExpression.cs) | class | A sort criterion of a property name and a `SortDirection`. |
