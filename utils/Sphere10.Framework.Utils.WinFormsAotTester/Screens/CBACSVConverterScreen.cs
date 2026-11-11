// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Sphere10.Framework.Data.Csv;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Converts bank CSV dates using the Framework CSV reader without losing field boundaries.</summary>
public partial class CBACSVConverterScreen : ApplicationScreen {
	/// <summary>Creates an in-memory conversion example with no file or network access.</summary>
	public CBACSVConverterScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		LoadSample();
	}

	// Convert the complete draft first, so invalid input never leaves a partial accepted result.
	private void _convertButton_Click(object sender, EventArgs e) {
		try {
			using var reader = new CsvReader(new StringReader(_CBACSVTextBox.Text), false);
			var output = new StringBuilder();
			var count = 0;
			while (reader.ReadNextRecord()) {
				if (reader.FieldCount < 4)
					throw new FormatException($"Record {count + 1} requires at least four columns.");
				var fields = Enumerable.Range(0, reader.FieldCount).Select(index => reader[index]).ToArray();
				if (!DateTime.TryParseExact(fields[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
					throw new FormatException($"Record {count + 1} has an invalid date. Expected dd/MM/yyyy.");
				fields[0] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				output.AppendLine(string.Join(",", fields.Select(field => field.EscapeCSV())));
				count++;
			}
			_outputTextBox.Text = output.ToString();
			_status.Text = $"Converted {count} record(s). All columns are retained.";
		} catch (Exception error) {
			_status.Text = "Conversion failed: " + error.Message;
		}
	}

	// Provide quoted and empty fields as immediately usable boundary examples.
	private void LoadSampleClicked(object sender, EventArgs e) => LoadSample();

	private void LoadSample() {
		_CBACSVTextBox.Text = "01/11/2026,-25.50,\"Coffee, lunch\",1200.00" + Environment.NewLine + "02/11/2026,100.00,,1300.00";
		_outputTextBox.Clear();
		_status.Text = "Input above; converted CSV appears below. Dates must use dd/MM/yyyy.";
	}
}
