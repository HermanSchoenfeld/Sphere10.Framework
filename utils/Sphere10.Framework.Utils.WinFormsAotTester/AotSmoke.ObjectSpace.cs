// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static string _objectSpaceFixtures;

	public static void ConfigureObjectSpaceFixtures(string[] args) {
		var argument = Array.IndexOf(args, "--objectspace-fixtures");
		if (argument < 0)
			return;
		var directory = args.ElementAtOrDefault(argument + 1);
		Guard.ArgumentNotNullOrEmpty(directory, nameof(args));
		_objectSpaceFixtures = Path.GetFullPath(directory);
		Guard.Ensure(Directory.Exists(_objectSpaceFixtures), "The supplied ObjectSpace fixture directory does not exist.");
	}

	private static async Task VerifyObjectSpace(BlockMainForm main) {
		var host = main.ScreenHost;
		var block = host.Blocks.Single(item => item.ScreenDefinitions.Any(definition => definition.ScreenType == typeof(ObjectSpaceScreen)));
		var definition = block.ScreenDefinitions.Single(item => item.ScreenType == typeof(ObjectSpaceScreen));
		Record("ObjectSpaceActivate");
		var screen = (ObjectSpaceScreen)await host.OpenScreenAsync(block, definition);
		await Task.Yield();
		var name = screen.Controls.Find("accountName", true).OfType<TextBox>().Single();
		var quantity = screen.Controls.Find("accountQuantity", true).OfType<NumericUpDown>().Single();
		var samples = screen.Controls.Find("sampleAccounts", true).OfType<NumericUpDown>().Single();
		var endianness = screen.Controls.Find("storeEndianness", true).OfType<ComboBox>().Single();
		var rows = screen.Controls.Find("accountsGrid", true).OfType<DataGridView>().Single();
		var status = screen.Controls.Find("objectSpaceStatus", true).OfType<Label>().Single();
		var output = screen.Controls.Find("objectSpaceLog", true).OfType<TextBox>().Single();
		var create = screen.Controls.Find("_buildButton", true).OfType<Button>().Single();
		var add = screen.Controls.Find("_addButton", true).OfType<Button>().Single();
		var save = screen.Controls.Find("_updateButton", true).OfType<Button>().Single();
		var delete = screen.Controls.Find("_deleteButton", true).OfType<Button>().Single();
		var reload = screen.Controls.Find("_reloadButton", true).OfType<Button>().Single();
		var close = screen.Controls.Find("_closeButton", true).OfType<Button>().Single();
		for (var attempt = 0; !create.Enabled && attempt < 500; attempt++)
			await Task.Delay(10);
		Guard.Ensure(create.Enabled && endianness.Enabled && rows.Rows.Count == 0 && !add.Enabled, "The ObjectSpace screen did not finish loading its preferences into an empty workbench.");
		if (_objectSpaceFixtures != null) {
			// This happens before any schema creation in this process; same-process reopens can hide missing factory activation code.
			for (var byteOrder = 0; byteOrder < 2; byteOrder++) {
				var suffix = byteOrder == 0 ? "little" : "big";
				var prefix = Path.Combine(_objectSpaceFixtures, "objectspace-fixture-" + suffix);
				Guard.Ensure(File.Exists(prefix + ".s10space") && File.Exists(prefix + ".tsv"), "The supplied ObjectSpace fixture is incomplete: " + prefix);
				var expected = ReadObjectSpaceFixture(prefix + ".tsv");
				Guard.Ensure(expected.Count == 3, "The cold ObjectSpace fixture must contain the edited three-account sample.");
				endianness.SelectedIndex = byteOrder;
				screen.OpenObjectSpaceSmokeSnapshot(File.ReadAllBytes(prefix + ".s10space"), byteOrder == 0 ? Endianness.LittleEndian : Endianness.BigEndian);
				VerifyObjectSpaceRowsEqual(expected, ReadObjectSpaceRows(rows));
				Guard.Ensure(status.Text.Contains("3 accounts", StringComparison.Ordinal) && status.Text.Contains("3 identities", StringComparison.Ordinal)
					&& !endianness.Enabled && reload.Enabled && close.Enabled, status.Text);
				CaptureControl(screen, "objectspace-cold-" + suffix + ".png");
				Record("ObjectSpaceColdOpened=" + suffix);
				close.PerformClick();
				Guard.Ensure(status.Text == "Store closed. Existing snapshot files are unchanged." && rows.Rows.Count == 0 && endianness.Enabled, status.Text);
			}
		}
		for (var byteOrder = 0; byteOrder < 2; byteOrder++) {
			var suffix = byteOrder == 0 ? "little" : "big";
			samples.Value = 2;
			endianness.SelectedIndex = byteOrder;
			create.PerformClick();
			Guard.Ensure(status.Text == "Created a real two-dimension metadata store. Only public identity bytes are persisted.", status.Text);
			var initial = ReadObjectSpaceRows(rows);
			Guard.Ensure(initial.Count == 2 && initial["Sample account 1"].Quantity == 125M && initial["Sample account 2"].Quantity == 250M);
			Guard.Ensure(add.Enabled && reload.Enabled && close.Enabled && !endianness.Enabled);
			Record("ObjectSpaceCreated=" + suffix + ";Rows=" + initial.Count);
			CaptureControl(screen, "objectspace-created-" + suffix + ".png");
			var addedName = "AOT account " + suffix;
			var updatedName = "AOT renamed " + suffix;
			name.Text = addedName;
			quantity.Value = 12.5M;
			add.PerformClick();
			Guard.Ensure(status.Text == "Account and public identity saved.", status.Text);
			var added = ReadObjectSpaceRows(rows);
			Guard.Ensure(added.Count == 3 && added[addedName].Quantity == 12.5M);
			var fingerprint = added[addedName].Fingerprint;
			SelectObjectSpaceRow(rows, addedName);
			Guard.Ensure(save.Enabled);
			name.Text = updatedName;
			quantity.Value = 99.25M;
			save.PerformClick();
			Guard.Ensure(status.Text == "The selected tracked account was updated and explicitly saved.", status.Text);
			var updated = ReadObjectSpaceRows(rows);
			Guard.Ensure(updated.Count == 3 && !updated.ContainsKey(addedName) && updated[updatedName] == (99.25M, fingerprint));
			Record("ObjectSpaceUpdated=" + suffix + ";Rows=" + updated.Count);
			var fixturePrefix = Path.Combine(_directory, "objectspace-fixture-" + suffix);
			File.WriteAllBytes(fixturePrefix + ".s10space", screen.CaptureObjectSpaceSmokeSnapshot());
			File.WriteAllLines(fixturePrefix + ".tsv", updated.OrderBy(pair => pair.Key, StringComparer.Ordinal)
				.Select(pair => pair.Key + "\t" + pair.Value.Quantity.ToString(CultureInfo.InvariantCulture) + "\t" + pair.Value.Fingerprint));
			Record("ObjectSpaceFixtureWritten=" + suffix);
			// The real reload command flushes, closes both owners and rehydrates from metadata without a supplied definition.
			reload.PerformClick();
			Guard.Ensure(status.Text == "Reloaded directly with ObjectSpace.TryOpenFromStream. The definition and factory came from metadata.", status.Text);
			var reopened = ReadObjectSpaceRows(rows);
			VerifyObjectSpaceRowsEqual(updated, reopened);
			Guard.Ensure(!endianness.Enabled && endianness.SelectedIndex == byteOrder);
			Record("ObjectSpaceMetadataReopened=" + suffix + ";Rows=" + reopened.Count);
			CaptureControl(screen, "objectspace-reopened-" + suffix + ".png");
			SelectObjectSpaceRow(rows, updatedName);
			Guard.Ensure(delete.Enabled);
			delete.PerformClick();
			Guard.Ensure(status.Text == "Selected account and its public identity deleted.", status.Text);
			var deleted = ReadObjectSpaceRows(rows);
			VerifyObjectSpaceRowsEqual(initial, deleted);
			reload.PerformClick();
			Guard.Ensure(status.Text == "Reloaded directly with ObjectSpace.TryOpenFromStream. The definition and factory came from metadata.", status.Text);
			VerifyObjectSpaceRowsEqual(initial, ReadObjectSpaceRows(rows));
			Record("ObjectSpaceDeletedAndReopened=" + suffix + ";Rows=" + rows.Rows.Count);
			close.PerformClick();
			Guard.Ensure(status.Text == "Store closed. Existing snapshot files are unchanged.", status.Text);
			Guard.Ensure(rows.Rows.Count == 0 && !add.Enabled && !save.Enabled && !delete.Enabled && !reload.Enabled && !close.Enabled && endianness.Enabled);
			Guard.Ensure(!output.Text.Contains("Could not complete the operation:", StringComparison.Ordinal));
			Record("ObjectSpaceClosed=" + suffix);
		}
		Record("ObjectSpaceVerified");
	}

	private static Dictionary<string, (decimal Quantity, string Fingerprint)> ReadObjectSpaceRows(DataGridView grid) {
		var result = grid.Rows.Cast<DataGridViewRow>().ToDictionary(
			row => (string)row.Cells[0].Value,
			row => (Convert.ToDecimal(row.Cells[1].Value), (string)row.Cells[2].Value),
			StringComparer.Ordinal);
		Guard.Ensure(result.Values.All(value => value.Item2 is { Length: > 2 }
			&& value.Item2.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			&& value.Item2.Length % 2 == 0
			&& value.Item2.Skip(2).All(Uri.IsHexDigit)), "Every displayed account must retain public identity bytes.");
		Guard.Ensure(result.Values.Select(value => value.Item2).Distinct(StringComparer.Ordinal).Count() == result.Count, "Independent accounts must have distinct public identities.");
		return result;
	}

	private static void SelectObjectSpaceRow(DataGridView grid, string name) {
		var row = grid.Rows.Cast<DataGridViewRow>().Single(item => (string)item.Cells[0].Value == name);
		grid.ClearSelection();
		grid.CurrentCell = row.Cells[0];
		row.Selected = true;
		Guard.Ensure(grid.SelectedRows.Count == 1 && ReferenceEquals(grid.SelectedRows[0], row));
	}

	private static void VerifyObjectSpaceRowsEqual(Dictionary<string, (decimal Quantity, string Fingerprint)> expected, Dictionary<string, (decimal Quantity, string Fingerprint)> actual)
		=> Guard.Ensure(expected.Count == actual.Count && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && pair.Value == value), "ObjectSpace metadata reload or mutation changed retained account values/public identities.");

	private static Dictionary<string, (decimal Quantity, string Fingerprint)> ReadObjectSpaceFixture(string path) {
		var result = new Dictionary<string, (decimal Quantity, string Fingerprint)>(StringComparer.Ordinal);
		foreach (var line in File.ReadLines(path)) {
			var columns = line.Split('\t');
			Guard.Ensure(columns.Length == 3 && !string.IsNullOrEmpty(columns[0]), "Invalid ObjectSpace fixture row: " + path);
			result.Add(columns[0], (decimal.Parse(columns[1], NumberStyles.Number, CultureInfo.InvariantCulture), columns[2]));
		}
		return result;
	}
}

partial class ObjectSpaceScreen {
	internal byte[] CaptureObjectSpaceSmokeSnapshot() {
		Guard.Ensure(AotSmoke.IsActive, "ObjectSpace diagnostic snapshots require an active smoke run.");
		RequireStore();
		_space.Flush();
		return _rawStream.ToArray();
	}

	internal void OpenObjectSpaceSmokeSnapshot(byte[] bytes, Endianness endianness) {
		Guard.Ensure(AotSmoke.IsActive, "ObjectSpace diagnostic opening requires an active smoke run.");
		OpenBytes(bytes, endianness);
	}
}
