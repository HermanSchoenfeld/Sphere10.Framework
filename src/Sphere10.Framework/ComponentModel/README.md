# Component model

Data-binding lists, type converters and value validators for editors that convert between display strings and typed values.

These types sit in the `Sphere10.Framework.ComponentModel` namespace and follow the `System.ComponentModel` model. `BoundListBase<T>` and `BoundList<T>` expose an `IList<T>` to a list or grid control through `IBoundList`, track added, removed and edited items, and raise list-change events. `ValidatorBase` and `TypeConverterValidator` convert an editor's raw object or string into a typed value, check it against nulls, limits and standard values, and format it back for display. The converters (`NumberTypeConverter`, `CurrencyTypeConverter`, `PercentTypeConverter`, `DateTimeTypeConverter`) supply the culture-aware string formats that a validator uses.

The WinForms SourceGrid editors and validated text boxes consume this model, and `Sphere10.Framework.Data` adds a `BoundDataView` over `DataView`. New data-bound screens normally use the `IDataSource<TItem>` contract in [DataSource](../DataSource/README.md) instead.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BoundList<T>`](BoundList.cs) | class | Binds a list control to an `IList<T>`, with sorting when the list is a `List<T>`. |
| [`BoundListBase<T>`](BoundListBase.cs) | abstract class | Base `IBoundList` implementation that tracks added, removed and edited items and raises `ListChanged`, `ListCleared` and `ItemDeleted`. |
| [`ConvertingObjectEventArgs`](ConvertingObjectEventArgs.cs) | class | Carries the value, destination type and `ConvertingStatus` of one conversion request. |
| [`ConvertingObjectEventHandler`](ConvertingObjectEventHandler.cs) | delegate | Receives a value-conversion request and its mutable conversion status. |
| [`ConvertingStatus`](ConvertingStatus.cs) | enum | State of a conversion request: `Converting`, `Error` or `Completed`. |
| [`CurrencyTypeConverter`](CurrencyTypeConverter.cs) | class | `NumberTypeConverter` that formats and parses numbers with the currency symbol. |
| [`DateTimeTypeConverter`](DateTimeTypeConverter.cs) | class | Converts dates using configured display and parse formats with the selected culture. |
| [`IBoundList`](IBoundList.cs) | interface | List abstraction that a grid binds to for item access, editing, deletion, sorting and property discovery. |
| [`IValidator`](IValidator.cs) | interface | Contract for converting objects and strings to typed values, validating them and formatting them for display. |
| [`ItemDeletedEventArgs<T>`](ItemDeletedEventArgs.cs) | class | Event arguments that carry the item removed from a bound list. |
| [`NumberTypeConverter`](NumberTypeConverter.cs) | class | Converts numeric types to and from strings with a custom number format and `NumberStyles`. |
| [`PercentTypeConverter`](PercentTypeConverter.cs) | class | Converts `float`, `double` and `decimal` values to and from strings with the percent symbol. |
| [`TypeConverterValidator`](TypeConverterValidator.cs) | class | Validator that converts values through a `TypeConverter`. |
| [`ValidatorBase`](ValidatorBase.cs) | abstract class | Component base for validators, with null handling, minimum and maximum values, standard values, culture and conversion events. |
| [`ValueConversionException`](ValueConversionException.cs) | class | Thrown when a value cannot be converted to the destination type. |
| [`ValueMapping`](ValueMapping.cs) | class | Maps values to string values and display strings for a bound validator. |
