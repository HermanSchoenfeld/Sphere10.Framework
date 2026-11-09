# Text

Fixed-length text record parsing and writing driven by field definitions or by attributes on a record class.

The `FixedLength` subfolder, in the `Sphere10.Framework.Windows.Forms.Text.FixedLength` namespace, reads and writes lines whose fields sit at fixed positions and widths, optionally separated by a character. A record layout is a `FieldList` of `IField` definitions. Build it by hand from `Field` instances, or let `Tools.FixedLength.ExtractFieldListFromType` read it from public properties marked with `FieldAttribute`. `ParseFormatAttribute`, `ValueMappingAttribute` and `StandardValueAttribute` add culture and format, value translation and required values. `LineParser` matches a line with a generated regular expression and fills a record object. `LineWriter` formats a record object or individual field values back into a line. Each failure has its own exception type.

The code has no Windows Forms dependency beyond its location in this project. It came from the same source as the [SourceGrid](../SourceGrid/README.md) port.

```csharp
public class OrderLine {
	[Field(0, 10)] public string Code { get; set; }
	[Field(1, 5)] public int Quantity { get; set; }
}

var parser = new LineParser(typeof(OrderLine));
parser.LoadLine(line);
var order = (OrderLine)parser.FillLineClass(new OrderLine());
```

## Contents

### FixedLength

| Type | Kind | Purpose |
|---|---|---|
| [`FailedPropertyGetFieldException`](FixedLength/FailedPropertyGetFieldException.cs) | class | Thrown when reading a record property for a field fails. |
| [`FailedPropertySetFieldException`](FixedLength/FailedPropertySetFieldException.cs) | class | Thrown when setting a record property from a parsed field fails. |
| [`Field`](FixedLength/Field.cs) | class | `IField` with index, name, length, trim option and a validator that converts between text and values. |
| [`FieldAttribute`](FixedLength/FieldAttribute.cs) | class | Required property attribute giving a field's index and length. |
| [`FieldList`](FixedLength/FieldList.cs) | class | Name-keyed dictionary of `IField` definitions that rejects null fields and returns them sorted by index. |
| [`FieldNotDefinedException`](FixedLength/FieldNotDefinedException.cs) | class | Thrown when no field is defined for a field index. |
| [`FieldParseException`](FixedLength/FieldParseException.cs) | class | Thrown when field text cannot be parsed into its value type. |
| [`FieldStringConvertException`](FixedLength/FieldStringConvertException.cs) | class | Thrown when a field value cannot be formatted as text. |
| [`IField`](FixedLength/IField.cs) | interface | Field definition contract with index, name, length, regular expression pattern and text conversion. |
| [`InvalidFieldLengthException`](FixedLength/InvalidFieldLengthException.cs) | class | Thrown when a field length is not a positive number. |
| [`LineParser`](FixedLength/LineParser.cs) | class | Parses a fixed-length line into field values and fills the attributed properties of a record object. |
| [`LineWriter`](FixedLength/LineWriter.cs) | class | Builds a fixed-length line from field values or from an attributed record object. |
| [`ParseFormatAttribute`](FixedLength/ParseFormatAttribute.cs) | class | Property attribute setting culture, date and number formats and trimming for a field. |
| [`RegExException`](FixedLength/RegExException.cs) | class | Thrown when a regular expression group for a field is not valid. |
| [`StandardValueAttribute`](FixedLength/StandardValueAttribute.cs) | class | Property attribute declaring the mandatory value of a field. |
| [`Tools.FixedLength`](FixedLength/FixedLengthTool.cs) | tools | Builds a `FieldList` from an attributed type, creates field validators and builds the separator expression. |
| [`TypeNotSupportedException`](FixedLength/TypeNotSupportedException.cs) | class | Thrown when a property type has no fixed-length conversion. |
| [`ValueMappingAttribute`](FixedLength/ValueMappingAttribute.cs) | class | Property attribute mapping one text value to a field value, repeatable per property. |
| [`ValueNotSupportedException`](FixedLength/ValueNotSupportedException.cs) | class | Thrown when a value is not supported for a field type. |
| [`ValueNotValidLengthException`](FixedLength/ValueNotValidLengthException.cs) | class | Thrown when a formatted value does not have the field's length. |
