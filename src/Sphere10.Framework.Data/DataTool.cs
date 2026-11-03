// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Sphere10.Framework;
using Sphere10.Framework.Data;
using Sphere10.Framework.Data.Csv;

// ReSharper disable CheckNamespace
namespace Tools;

public static partial class Data {
	// The existing binary DataSet representation starts with this format identifier.
	private const int _binaryDataSetVersion = 1;

	public static readonly System.DateTime SQLDateTimeMinValue = new System.DateTime(1753, 1, 1);
	public static readonly System.DateTime SQLDateTimeMaxValue = new System.DateTime(9999, 12, 31);

	public static DataTable ReadCsv(string filename, bool hasHeaders) {
		using (var streamReader = new StreamReader(filename))
			return ReadCsv(streamReader, hasHeaders);
	}

	public static DataTable ReadCsv(StreamReader stream, bool hasHeaders) {
		using (var csvReader = new CsvReader(stream, hasHeaders)) {
			return csvReader.ToDataTable();
		}
	}

	/// <summary>Writes one delimited row using the supplied field formatter and leaves the destination open.</summary>
	/// <remarks>The formatter writes one field; this method owns separators and the final line separator.</remarks>
	public static void WriteCsvRow<T>(TextWriter destination, IEnumerable<T> fields, Action<T, TextWriter> writeField, string delimiter, string lineSeparator) {
		Guard.ArgumentNotNull(destination, nameof(destination));
		Guard.ArgumentNotNull(fields, nameof(fields));
		Guard.ArgumentNotNull(writeField, nameof(writeField));
		Guard.ArgumentNotNullOrEmpty(delimiter, nameof(delimiter));

		// Separate fields rather than trimming a trailing delimiter, including multi-character delimiters.
		var first = true;
		foreach (var field in fields) {
			if (!first)
				destination.Write(delimiter);
			writeField(field, destination);
			first = false;
		}
		destination.Write(lineSeparator);
	}

	/// <summary>Escapes one raw CSV field without writing a separator or closing the destination.</summary>
	public static void WriteCsvField(TextWriter destination, string value, string delimiter = ",") {
		Guard.ArgumentNotNull(destination, nameof(destination));

		destination.Write((value ?? string.Empty).EscapeCSV(delimiter));
	}

	public static DataTable CreateDataTable(IEnumerable<DataTableCellInfo> columnSpecs) {
		var table = new DataTable();
		// create columns
		columnSpecs.ForEach(columnSpec => {
			table.Columns.Add(columnSpec.ColumnName);
			var column = table.Columns[columnSpec.ColumnName];
			if (!columnSpec.ColumnVisible)
				column.ColumnMapping = MappingType.Hidden;
		});
		return table;
	}

	/// <summary>
	/// Creates a DataTable with columns matching the properties of the given entity.
	/// </summary>
	/// <typeparam name="T">The LinqToSQL type</typeparam>
	/// <returns>left suitably spec'd empty DataTable.</returns>
	public static DataTable CreateDataTableForType<T>() {
		var dataTable = new DataTable();
		foreach (var property in typeof(T).GetProperties()) {
			var fieldType = property.PropertyType;
			if (fieldType.IsNullable())
				fieldType = Nullable.GetUnderlyingType(fieldType);

			dataTable.Columns.Add(property.Name, fieldType);
		}
		return dataTable;
	}

	/// <summary>Writes a DataSet using XML or the existing schema-and-values binary representation.</summary>
	public static void WriteDataSet(Stream destination, DataSet source, StreamDataSetFormat format) {
		switch (format) {
			case StreamDataSetFormat.XML:
				source.WriteXml(destination, XmlWriteMode.WriteSchema);
				break;
			case StreamDataSetFormat.Binary:
				WriteBinaryDataSet(destination, source);
				break;
			default:
				throw new ApplicationException("StreamDataSet Format not supported");
		}
	}

	/// <summary>Reads DataSet rows, optionally merging their schema, and leaves the supplied stream open.</summary>
	public static void ReadDataSet(Stream source, DataSet destination, StreamDataSetFormat format, bool mergeSchema) {
		// Temporarily suspend destination constraints while related tables are populated.
		var enforceConstraints = destination.EnforceConstraints;
		destination.EnforceConstraints = false;
		try {
			switch (format) {
				case StreamDataSetFormat.XML:
					destination.ReadXml(source, mergeSchema ? XmlReadMode.ReadSchema : XmlReadMode.IgnoreSchema);
					break;
				case StreamDataSetFormat.Binary:
					ReadBinaryDataSet(source, destination, mergeSchema);
					break;
				default:
					throw new ApplicationException("StreamDataSet Format not supported");
			}
		} finally {
			destination.EnforceConstraints = enforceConstraints;
		}
	}

	// Operator methods
	/// <summary>
	/// Create a like expression for string values, automatically calls FormatValueForExpression
	/// </summary>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <returns></returns>
	public static string LikeExpression(string fieldName, string fieldValue) {
		//TODO Bisognerebbe gestire anche la ricerca di valori speciali. Si dovrebbe ad esempio sostituire le occorrenze di caratteri come * e % (entrambi supportati per la ricerca LIKE) con rispettivamente [*] e [%] e sostituire [ e ] con [[] e []] .
		var param = "%" + fieldValue + "%";
		return fieldName + " LIKE " + ExpressionFormat(param);
	}

	/// <summary>
	/// Create a start with expression for stirng value, automatically calls FormatValueForExpression
	/// </summary>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <returns></returns>
	public static string StartWithExpression(string fieldName, string fieldValue) {
		//TODO Bisognerebbe gestire anche la ricerca di valori speciali. Si dovrebbe ad esempio sostituire le occorrenze di caratteri come * e % (entrambi supportati per la ricerca LIKE) con rispettivamente [*] e [%] e sostituire [ e ] con [[] e []] .
		var param = fieldValue + "%";
		return fieldName + " LIKE " + ExpressionFormat(param);
	}

	/// <summary>
	/// Create an equal expression, automatically calls FormatValueForExpression
	/// </summary>
	/// <returns></returns>
	public static string EqualExpression(string fieldName, object fieldValue) {
		return fieldName + " = " + ExpressionFormat(fieldValue);
	}

	/// <summary>
	/// Create a Not equal expression, automatically calls FormatValueForExpression
	/// </summary>
	/// <returns></returns>
	public static string NotEqualExpression(string fieldName, object fieldValue) {
		return fieldName + " <> " + ExpressionFormat(fieldValue);
	}

	public static DataTable SelectDistinct(DataTable sourceTable, string fieldName, bool addEmptyValue, object emptyValue) {
		var dataTable = new DataTable();
		dataTable.Columns.Add(fieldName, sourceTable.Columns[fieldName].DataType);

		if (addEmptyValue)
			dataTable.Rows.Add(new object[] { emptyValue });

		var lastValue = emptyValue;
		foreach (var row in sourceTable.Select("", fieldName)) {
			if (lastValue == null || !(ValEquals(lastValue, row[fieldName]))) {
				lastValue = row[fieldName];
				dataTable.Rows.Add(new object[] { lastValue });
			}
		}

		return dataTable;
	}

	public static DataTable SampleDataByInterval(DataView sourceView, string[] columns, string samplingField, int intervalValue, DateTimeInterval intervalType) {
		var newTable = new DataTable();
		for (var index = 0; index < columns.Length; index++)
			newTable.Columns.Add(new DataColumn(columns[index], sourceView.Table.Columns[columns[index]].DataType));

		DateTime lastValue;
		if (sourceView.Count > 0) {
			//Add the first row
			lastValue = (DateTime)sourceView[0][samplingField];
			var newRow = newTable.NewRow();
			for (var i = 0; i < columns.Length; i++)
				newRow[columns[i]] = sourceView[0][columns[i]];
			newTable.Rows.Add(newRow);

			//Add all rows
			for (var i = 1; i < sourceView.Count; i++) {
				var sourceRow = sourceView[i].Row;
				var currentValue = (DateTime)sourceRow[samplingField];
				int currentInterval;

				if (intervalType == DateTimeInterval.Months)
					currentInterval = Tools.Time.MonthsDifference(currentValue, lastValue);
				else
					throw new ApplicationException("Interval not supported");

				if (currentInterval >= intervalValue || (i + 1) == sourceView.Count) {
					if (intervalType == DateTimeInterval.Months)
						lastValue = lastValue.AddMonths(intervalValue);
					else
						throw new ApplicationException("Interval not supported");

					newRow = newTable.NewRow();
					for (var j = 0; j < columns.Length; j++)
						newRow[columns[j]] = sourceRow[columns[j]];
					newTable.Rows.Add(newRow);
				}
			}
		}

		return newTable;
	}

	/// <summary>
	/// Evaluate the given expression using the Compute method of an empty DataTable replacing the values of the specified Row.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="expression"></param>
	/// <returns></returns>
	public static object EvalRowExpression(DataRow row, string expression) {
		//DataTable table = new DataTable();
		//foreach (DataColumn c in row.Table.Columns)
		//    table.Columns.Add(c.ColumnName, c.DataType);

		//table.Rows.Add(row.ItemArray);
		//table.AcceptChanges();

		var expressionToEvaluate = expression;
		for (var index = 0; index < row.Table.Columns.Count; index++) {
			string matchString;

			// First search for [col1] syntax and then for the standard col1 syntax
			matchString = System.Text.RegularExpressions.Regex.Escape("[" + row.Table.Columns[index].ColumnName + "]");

			if (System.Text.RegularExpressions.Regex.IsMatch(expressionToEvaluate, matchString, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				expressionToEvaluate = System.Text.RegularExpressions.Regex.Replace(
					expressionToEvaluate,
					matchString,
					ExpressionFormat(row[index]),
					System.Text.RegularExpressions.RegexOptions.IgnoreCase
				);

			// \b match only whole word
			matchString = @"\b" + System.Text.RegularExpressions.Regex.Escape(row.Table.Columns[index].ColumnName) + @"\b";

			if (System.Text.RegularExpressions.Regex.IsMatch(expressionToEvaluate, matchString, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				expressionToEvaluate = System.Text.RegularExpressions.Regex.Replace(
					expressionToEvaluate,
					matchString,
					ExpressionFormat(row[index]),
					System.Text.RegularExpressions.RegexOptions.IgnoreCase
				);
		}

		return Eval(expressionToEvaluate, null);
	}

	// Evaluate methods
	/// <summary>
	/// Evaluate the given expression using the Compute method of a empty DataTable. Replace the parameters using the string.Format method ({0} syntax)
	/// </summary>
	/// <param name="expression"></param>
	/// <param name="parameters"></param>
	/// <returns></returns>
	public static object Eval(string expression, params object[] parameters) {
		// Keep expression evaluation independent between callers.
		using var expressionTable = new DataTable();
		object[] formattedParameters = null;
		if (parameters != null) {
			formattedParameters = new string[parameters.Length];
			for (var index = 0; index < parameters.Length; index++)
				formattedParameters[index] = ExpressionFormat(parameters[index]);
		}

		string expressionToEvaluate;
		if (formattedParameters != null)
			expressionToEvaluate = string.Format(expression, formattedParameters);
		else
			expressionToEvaluate = expression;

		return expressionTable.Compute(expressionToEvaluate, string.Empty);
	}

	/// <summary>
	/// Format the specified value in a string that can be used inside an expression.
	/// </summary>
	/// <returns></returns>
	public static string ExpressionFormat(object data) {
		if (data is DateTime)
			return ExpressionFormat((DateTime)data);
		if (data is int)
			return ExpressionFormat((int)data);
		if (data is long)
			return ExpressionFormat((long)data);
		if (data is float)
			return ExpressionFormat((float)data);
		if (data is double)
			return ExpressionFormat((double)data);
		if (data is decimal)
			return ExpressionFormat((decimal)data);
		if (data is bool)
			return ExpressionFormat((bool)data);
		if (data is string)
			return ExpressionFormat((string)data);
		if (data is char)
			return ExpressionFormat((char)data);
		if (data is System.DBNull)
			return ExpressionFormat((System.DBNull)data);
		throw new ApplicationException("Type not supported for expression");
	}

	// Expression convert methods
	/// <summary>
	/// Create a string with this format: #MM/dd/yyyy# . This is the default format for DataSet expressions. Can be used for example with the DataTable.Select method.
	/// </summary>
	/// <param name="date"></param>
	/// <returns></returns>
	public static string ExpressionFormat(DateTime date) {
		return "#" + date.ToString("MM/dd/yyyy") + "#";
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(int data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(long data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(float data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(double data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(decimal data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	/// <summary>
	/// Create a string with an InvariantCulture format.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(bool data) {
		return data.ToString(System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Replace any special sql character (like single quote) and replace it with the valid sql equivalent. Then add the appropriate quote if the type require (string).
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(string data) {
		data = data.Replace("'", "''");
		return "'" + data + "'";
	}

	/// <summary>
	/// Replace any special sql character (like single quote) and replace it with the valid sql equivalent. Then add the appropriate quote if the type require (string).
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(char data) {
		return ExpressionFormat(data.ToString());
	}

	/// <summary>
	/// Return NULL
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static string ExpressionFormat(System.DBNull data) {
		return "NULL";
	}

	// Preserve the binary DataSet contract: version 1, length-prefixed XSD, table row counts and marked field values.
	private static void WriteBinaryDataSet(Stream destination, DataSet source) {
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, destination, leaveOpen: true);
		using var context = new SerializationContext();
		var bytesSerializer = new ByteArraySerializer(SizeDescriptorStrategy.UseUInt32);
		PrimitiveSerializer<int>.Instance.Serialize(_binaryDataSetVersion, writer, context);
		using var schemaStream = new MemoryStream();
		source.WriteXmlSchema(schemaStream);
		bytesSerializer.Serialize(schemaStream.ToArray(), writer, context);

		// Activate a value serializer once per column and reuse it for every current row.
		foreach (DataTable table in source.Tables) {
			var serializers = table.Columns.Cast<DataColumn>().Select(column => new DataSetValueSerializer(column.DataType)).ToArray();
			var rows = table.Select(null, null, DataViewRowState.CurrentRows);
			PrimitiveSerializer<int>.Instance.Serialize(rows.Length, writer, context);
			foreach (var row in rows)
				for (var column = 0; column < serializers.Length; column++) {
					var value = row[column];
					var marker = value == null ? (byte)0 : value == DBNull.Value ? (byte)1 : (byte)2;
					PrimitiveSerializer<byte>.Instance.Serialize(marker, writer, context);
					if (marker == 2)
						serializers[column].Serialize(value, writer, context);
				}
		}
	}

	// Consume every serialized table and field even when the destination schema omits them.
	private static void ReadBinaryDataSet(Stream source, DataSet destination, bool mergeSchema) {
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, source, leaveOpen: true);
		using var context = new SerializationContext();
		if (PrimitiveSerializer<int>.Instance.Deserialize(reader, context) != _binaryDataSetVersion)
			throw new BinaryDataSetVersionException();
		var schemaLength = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		using var schemaStream = new MemoryStream(reader.ReadBytesOrThrow(schemaLength));
		using var separateSchema = mergeSchema ? null : new DataSet();
		var schema = mergeSchema ? destination : separateSchema;
		schema.ReadXmlSchema(schemaStream);

		// The schema establishes traversal order; status bytes distinguish null, DBNull and stored values.
		foreach (DataTable schemaTable in schema.Tables) {
			var destinationTable = destination.Tables.Contains(schemaTable.TableName) ? destination.Tables[schemaTable.TableName] : null;
			var serializers = schemaTable.Columns.Cast<DataColumn>().Select(column => new DataSetValueSerializer(column.DataType)).ToArray();
			var rows = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
			for (var rowIndex = 0; rowIndex < rows; rowIndex++) {
				var row = destinationTable?.NewRow();
				for (var column = 0; column < serializers.Length; column++) {
					var marker = PrimitiveSerializer<byte>.Instance.Deserialize(reader, context);
					var value = marker switch {
						0 => null,
						1 => DBNull.Value,
						2 => serializers[column].Deserialize(reader, context),
						_ => throw new BinaryDataSetInvalidException()
					};
					var name = schemaTable.Columns[column].ColumnName;
					if (row != null && destinationTable.Columns.Contains(name))
						row[name] = value;
				}
				if (row != null)
					destinationTable.Rows.Add(row);
			}
		}
		destination.AcceptChanges();
	}

	// Helper methods
	/// <summary>
	/// Compares two values to see if they are equal. Also compares DBNULL.Value.
	/// Note: If your DataTable contains object fields, then you must extend this
	/// function to handle them in a meaningful way if you intend to group on them.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool ValEquals(object left, object right) {
		if (left == DBNull.Value && right == DBNull.Value) //  both are DBNull.Value
			return true;
		if (left == DBNull.Value || right == DBNull.Value) //  only one is DBNull.Value
			return false;
		return (left.Equals(right)); // value type standard comparison
	}
}
