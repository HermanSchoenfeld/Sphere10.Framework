// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace Sphere10.Framework.Data;

/// <summary>
/// Persists a DataSet with its schema version and selected payload format.
/// </summary>
public abstract class FileDataSetBase {
	private StreamDataSetFormat _saveDataFormat = StreamDataSetFormat.Binary;
	private StreamDataSetFormat _fileDataFormat;
	private bool _mergeReadSchema = false;
	private string _fileName;

	// File framing uses the current Sphere10 namespace and accepts the original DevAge namespace.
	private const string _dataNamespace = "https://sphere10.com/FileDataSet";

	// Existing files retain their original namespace until explicitly saved again.
	private const string _legacyDataNamespace = "http://www.devage.com/FileDataSet";
	private const int _fileVersionNumber = 1;
	private const string _fileVersionAttribute = "fileversion";
	private const string _dataVersionAttribute = "dataversion";
	private const string _dataFormatAttribute = "dataformat";

	// Construction and file configuration.
	/// <summary>Initializes the file-backed DataSet persistence boundary.</summary>
	protected FileDataSetBase() {
	}

	/// <summary>Gets or sets the path used by load and save operations.</summary>
	public string FileName {
		get => _fileName;
		set => _fileName = value;
	}

	/// <summary>
	/// Gets or Sets the format used to save the DataSet
	/// </summary>
	protected StreamDataSetFormat SaveDataFormat {
		get => _saveDataFormat;
		set => _saveDataFormat = value;
	}

	/// <summary>
	/// Gets the current format of the File where the data are loaded.
	/// </summary>
	protected StreamDataSetFormat FileDataFormat => _fileDataFormat;

	/// <summary>
	/// Gets or Sets if merge the schema of the file with the schema of the DataSet specified. Use true if the DataSet doesn't have a schema definition. Default is false.
	/// </summary>
	protected bool MergeReadSchema {
		get => _mergeReadSchema;
		set => _mergeReadSchema = value;
	}

	/// <summary>Writes the DataSet payload and its format/version attributes to the configured file.</summary>
	protected virtual void SaveToFile(DataSet dataSet) {
		if (_fileName == null)
			throw new ApplicationException("ExecutableFileName is null");

		// Assemble the complete document before replacing the destination file.
		byte[] completeByteArray;
		using (var fileMemStream = new MemoryStream()) {
			var xmlWriter = new XmlTextWriter(fileMemStream, Encoding.UTF8);

			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("filedataset", _dataNamespace);

			xmlWriter.WriteStartElement("header", _dataNamespace);

			// File version.
			xmlWriter.WriteAttributeString(_fileVersionAttribute, _fileVersionNumber.ToString());

			// Data version.
			xmlWriter.WriteAttributeString(_dataVersionAttribute, GetDataVersion().ToString());

			// Data format.
			xmlWriter.WriteAttributeString(_dataFormatAttribute, ((int)_saveDataFormat).ToString());
			xmlWriter.WriteEndElement();

			xmlWriter.WriteStartElement("data", _dataNamespace);

			byte[] xmlByteArray;
			using (var xmlMemStream = new MemoryStream()) {
				Tools.Data.WriteDataSet(xmlMemStream, dataSet, _saveDataFormat);

				//dataSet.WriteXml(xmlMemStream);

				xmlByteArray = xmlMemStream.ToArray();
				xmlMemStream.Close();
			}

			xmlWriter.WriteBase64(xmlByteArray, 0, xmlByteArray.Length);
			xmlWriter.WriteEndElement();

			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();

			xmlWriter.Flush();

			completeByteArray = fileMemStream.ToArray();
			fileMemStream.Close();
		}

		//se tutto è andato a buon fine scrivo effettivamente il file
		Tools.FileSystem.WriteAllBytes(_fileName, completeByteArray);
	}

	/// <summary>Reads the configured file and hydrates a DataSet created for its stored schema version.</summary>
	protected virtual DataSet LoadFromFile() {
		if (_fileName == null)
			throw new ApplicationException("ExecutableFileName is null");

		// Select the namespace used by the existing document before reading its header.
		var xmlDoc = new XmlDocument();
		xmlDoc.Load(_fileName);

		var namespaceMan = new XmlNamespaceManager(xmlDoc.NameTable);
		namespaceMan.AddNamespace("fileds", xmlDoc.DocumentElement.NamespaceURI == _legacyDataNamespace ? _legacyDataNamespace : _dataNamespace);

		var temporary = xmlDoc.DocumentElement.SelectSingleNode("fileds:header", namespaceMan);
		if (temporary == null)
			throw new ApplicationException("File header not found");
		var header = (XmlElement)temporary;

		// File version.
		var strFileVersion = header.GetAttribute(_fileVersionAttribute);
		var fileVersion = int.Parse(strFileVersion);
		if (fileVersion == _fileVersionNumber) {
			var strDataFormat = header.GetAttribute(_dataFormatAttribute);
			_fileDataFormat = (StreamDataSetFormat)int.Parse(strDataFormat);
		} else if (fileVersion == 0)
			_fileDataFormat = StreamDataSetFormat.XML;
		else if (fileVersion > _fileVersionNumber)
			throw new ApplicationException("File Version not supported, expected: " + _fileVersionNumber.ToString());

		// Data version.
		var strDataVersion = header.GetAttribute(_dataVersionAttribute);
		var dataVersion = int.Parse(strDataVersion);
		var dataSet = CreateData(dataVersion);

		// Data payload.
		temporary = xmlDoc.DocumentElement.SelectSingleNode("fileds:data", namespaceMan);
		if (temporary == null)
			throw new ApplicationException("File data not found");
		var xmlDataNode = (XmlElement)temporary;

		var xmlBuffer = Convert.FromBase64String(xmlDataNode.InnerText);
		using (var memStream = new MemoryStream(xmlBuffer)) {
			Tools.Data.ReadDataSet(memStream, dataSet, _fileDataFormat, MergeReadSchema);

			//dataSet.ReadXml(memStream);
		}

		return dataSet;
	}

	/// <summary>Returns the schema version to store with the DataSet payload.</summary>
	protected abstract int GetDataVersion();

	/// <summary>Creates the target DataSet for the stored schema version.</summary>
	protected abstract DataSet CreateData(int version);
}
