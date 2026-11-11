// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Data;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace Sphere10.Framework.Data.Tests;

/// <summary>Checks the extracted DataSet wire contract and editable DataView binding lifecycle.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExtractedDataSetTests {
	/// <summary>Verifies file data sets write the sphere10 namespace and read existing dev age files.</summary>
	[TestCase(StreamDataSetFormat.Binary, false)]
	[TestCase(StreamDataSetFormat.XML, false)]
	[TestCase(StreamDataSetFormat.Binary, true)]
	[TestCase(StreamDataSetFormat.XML, true)]
	public void FileDataSetsWriteTheSphere10NamespaceAndReadExistingDevAgeFiles(StreamDataSetFormat format, bool legacyNamespace) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "values.xml");
		using var source = CreateDataSet();
		var file = new SampleFileDataSet(path, format);
		file.Save(source);
		var document = new XmlDocument();
		document.Load(path);
		Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo("https://sphere10.com/FileDataSet"));

		if (legacyNamespace)
			File.WriteAllText(path, File.ReadAllText(path).Replace("https://sphere10.com/FileDataSet", "http://www.devage.com/FileDataSet"));
		using var loaded = file.Load();
		Assert.That(loaded.Tables[0].Rows[0].ItemArray, Is.EqualTo(source.Tables[0].Rows[0].ItemArray));

		// Explicit saves replace the whole file and use the Framework namespace, including after legacy reads.
		loaded.Tables[0].Rows[0]["Text"] = "x";
		file.Save(loaded);
		document.Load(path);
		Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo("https://sphere10.com/FileDataSet"));
		using var reopened = file.Load();
		Assert.That(reopened.Tables[0].Rows[0]["Text"], Is.EqualTo("x"));
	}

	/// <summary>Verifies formats round trip schema and values without owning the stream.</summary>
	[TestCase(StreamDataSetFormat.Binary)]
	[TestCase(StreamDataSetFormat.XML)]
	public void FormatsRoundTripSchemaAndValuesWithoutOwningTheStream(StreamDataSetFormat format) {
		using var source = CreateDataSet();
		using var stream = new MemoryStream();
		Tools.Data.WriteDataSet(stream, source, format);
		Assert.That(stream.CanWrite, Is.True);
		stream.Position = 0;
		using var loaded = new DataSet();
		Tools.Data.ReadDataSet(stream, loaded, format, mergeSchema: true);

		Assert.That(stream.CanRead, Is.True);
		Assert.That(loaded.Tables, Has.Count.EqualTo(1));
		Assert.That(loaded.Tables[0].Rows[0].ItemArray, Is.EqualTo(source.Tables[0].Rows[0].ItemArray));
		Assert.That(loaded.EnforceConstraints, Is.True);
	}

	/// <summary>Verifies binary fields keep original lengths oa dates characters and decimal words.</summary>
	[Test]
	public void BinaryFieldsKeepOriginalLengthsOADatesCharactersAndDecimalWords() {
		using var source = CreateDataSet();
		using var stream = new MemoryStream();
		Tools.Data.WriteDataSet(stream, source, StreamDataSetFormat.Binary);
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream, leaveOpen: true);
		Assert.That(reader.ReadInt32(), Is.EqualTo(1));
		var schemaLength = reader.ReadInt32();
		reader.ReadBytesOrThrow(schemaLength);
		Assert.That(reader.ReadInt32(), Is.EqualTo(1));
		var values = reader.ReadBytesOrThrow(checked((int)(stream.Length - stream.Position)));

		// Each stored value starts with marker2; DBNull starts with marker1 and has no payload.
		var expected = HexEncoding.Decode(
			"022EFB" +
			"0204030201" +
			"020807060504030201" +
			"020000C03F" +
			"0200000000000002C0" +
			"02E2040000000000000000000000000280" +
			"022D4E" +
			"0201" +
			"02FE" +
			"0202000000C3A9" +
			"0203000000000102" +
			"021000000033221100554477668899AABBCCDDEEFF" +
			"02000000000000F83F" +
			"01"
		);
		Assert.That(values, Is.EqualTo(expected));
	}

	/// <summary>Verifies reading into a subset schema consumes skipped fields and tables.</summary>
	[Test]
	public void ReadingIntoASubsetSchemaConsumesSkippedFieldsAndTables() {
		using var source = CreateDataSet();
		var extra = source.Tables.Add("Omitted");
		extra.Columns.Add("Text", typeof(string));
		extra.Rows.Add("still consumed");
		using var stream = new MemoryStream();
		Tools.Data.WriteDataSet(stream, source, StreamDataSetFormat.Binary);
		stream.Position = 0;
		using var destination = new DataSet();
		var included = destination.Tables.Add("Values");
		included.Columns.Add("Text", typeof(string));
		Tools.Data.ReadDataSet(stream, destination, StreamDataSetFormat.Binary, mergeSchema: false);

		Assert.That(included.Rows[0]["Text"], Is.EqualTo("é"));
		Assert.That(included.Rows[0].RowState, Is.EqualTo(DataRowState.Unchanged));
		Assert.That(destination.Tables, Has.Count.EqualTo(1));
		Assert.That(stream.Position, Is.EqualTo(stream.Length));
	}

	/// <summary>Verifies truncated values and invalid version fail and restore constraints.</summary>
	[Test]
	public void TruncatedValuesAndInvalidVersionFailAndRestoreConstraints() {
		using var source = new DataSet("Source");
		var table = source.Tables.Add("Values");
		table.Columns.Add("Text", typeof(string));
		table.Rows.Add("complete");
		using var complete = new MemoryStream();
		Tools.Data.WriteDataSet(complete, source, StreamDataSetFormat.Binary);
		var bytes = complete.ToArray();
		using var truncated = new MemoryStream(bytes, 0, bytes.Length - 1, writable: false);
		using var destination = new DataSet();
		Assert.That(() => Tools.Data.ReadDataSet(truncated, destination, StreamDataSetFormat.Binary, mergeSchema: true), Throws.InstanceOf<IOException>());
		Assert.That(destination.EnforceConstraints, Is.True);

		bytes[0] = 2;
		using var changedVersion = new MemoryStream(bytes, writable: false);
		Assert.That(
			() => Tools.Data.ReadDataSet(changedVersion, destination, StreamDataSetFormat.Binary, mergeSchema: true), Throws.TypeOf<BinaryDataSetVersionException>()
		);
		Assert.That(destination.EnforceConstraints, Is.True);
	}

	/// <summary>Verifies bound data views cancel edits and detach all notifications on disposal.</summary>
	[Test]
	public void BoundDataViewsCancelEditsAndDetachAllNotificationsOnDisposal() {
		using var table = new DataTable();
		table.Columns.Add("Name", typeof(string));
		table.Rows.Add("Before");
		table.AcceptChanges();
		var bound = new BoundDataView(table.DefaultView);
		var changes = 0;
		var cleared = 0;
		var deleted = 0;
		bound.ListChanged += (_, _) => changes++;
		bound.ListCleared += (_, _) => cleared++;
		bound.ItemDeleted += (_, _) => deleted++;
		bound.BeginEdit(0);
		bound.SetEditValue(bound.GetItemProperty("Name", StringComparison.Ordinal), "Changed");
		bound.EndEdit(cancel: true);
		Assert.That(table.Rows[0]["Name"], Is.EqualTo("Before"));
		Assert.That(changes, Is.GreaterThan(0));

		bound.Dispose();
		var observedChanges = changes;
		table.Rows[0]["Name"] = "After";
		table.Rows[0].Delete();
		table.Clear();
		Assert.That(changes, Is.EqualTo(observedChanges));
		Assert.That(cleared, Is.Zero);
		Assert.That(deleted, Is.Zero);
	}

	// The fixed values expose historical field encodings independently of a matching reader.
	private static DataSet CreateDataSet() {
		var dataSet = new DataSet("Source");
		var table = dataSet.Tables.Add("Values");
		table.Columns.Add("Int16", typeof(short));
		table.Columns.Add("Int32", typeof(int));
		table.Columns.Add("Int64", typeof(long));
		table.Columns.Add("Single", typeof(float));
		table.Columns.Add("Double", typeof(double));
		table.Columns.Add("Decimal", typeof(decimal));
		table.Columns.Add("Character", typeof(char));
		table.Columns.Add("Boolean", typeof(bool));
		table.Columns.Add("Byte", typeof(byte));
		table.Columns.Add("Text", typeof(string));
		table.Columns.Add("Bytes", typeof(byte[]));
		table.Columns.Add("Guid", typeof(Guid));
		table.Columns.Add("Date", typeof(DateTime));
		table.Columns.Add("Null", typeof(string));
		table.Rows.Add((short)-1234, 0x01020304, 0x0102030405060708L, 1.5f, -2.25d, -12.50m, '中', true, (byte)0xFE,
			"é", new byte[] { 0, 1, 2 }, Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), DateTime.FromOADate(1.5d), DBNull.Value);
		return dataSet;
	}

	// Expose the existing persistence boundary while the production abstraction supplies its own schema version.
	private sealed class SampleFileDataSet : FileDataSetBase {
		/// <summary>Configures the test file and payload format.</summary>
		public SampleFileDataSet(string path, StreamDataSetFormat format) {
			FileName = path;
			SaveDataFormat = format;
			MergeReadSchema = true;
		}

		/// <summary>Persists test values through the production file boundary.</summary>
		public void Save(DataSet dataSet) => SaveToFile(dataSet);

		/// <summary>Loads test values through the production file boundary.</summary>
		public DataSet Load() => LoadFromFile();

		/// <inheritdoc />
		protected override int GetDataVersion() => 1;

		/// <inheritdoc />
		protected override DataSet CreateData(int version) => new DataSet("Source");
	}
}
