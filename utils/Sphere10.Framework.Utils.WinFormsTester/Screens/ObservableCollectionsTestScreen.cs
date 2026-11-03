// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates individual and range notifications alongside the resulting collection values.</summary>
public partial class ObservableCollectionsTestScreen : WinFormsApplicationScreen {
	/// <summary>Creates the collection laboratory without mutating data or reading storage.</summary>
	public ObservableCollectionsTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_textBoxEx1.Font);
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ObservableCollectionsScreenSettings ScreenSettings {
		get {
			return (ObservableCollectionsScreenSettings)base.ScreenSettings;
		}
	}

	// Restore presentation choices once the host has attached its ordinary settings object.

	// Options changes event verbosity without rerunning either sequence.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	// Exercise the original single/range insert/remove sequence with checkpoints before final Clear.
	private void ListClicked(object sender, EventArgs args) {
		using var output = new StringWriter();
		var list = new ObservableExtendedList<string>();
		var includeAccess = _includeAccess.Checked;
		output.WriteLine("OBSERVABLE LIST");
		list.Added += (_, args) => output.WriteLine($"Added {args.CallArgs.Items.Count()}: {args.CallArgs.Items.ToDelimittedString(", ")}");
		list.RemovedRange += (_, args) => output.WriteLine($"Removed {args.CallArgs.Count} at index {args.CallArgs.Index}");
		list.Accessed += (_, traits) => {
			if (includeAccess)
				output.WriteLine($"Access: {traits}");
		};

		list.Add("string 1");
		list.Add("string 2");
		list.RemoveAt(1);
		list.Remove("string 1");
		list.AddRange(new[] { "bulk 1", "bulk 2" });
		list.AddRange(new[] { "bulk 3", "bulk 4" });
		list.RemoveRange(2, 2);
		list.RemoveAt(1);
		list.RemoveAt(0);
		list.AddRange(new[] { "bulk 5", "bulk 6" });
		list.Clear();

		list.Add("padding 1");
		list.Add("padding 2");
		list.AddRange(new[] { "padding 6", "padding 7" });
		list.Insert(2, "single insert 3");
		list.InsertRange(3, new[] { "bulk insert 4", "bulk insert 5" });
		output.WriteLine();
		output.WriteLine("After Insert / InsertRange — expected order 1, 2, 3, 4, 5, 6, 7:");
		output.WriteLine(list.ToDelimittedString(", "));
		list.RemoveRange(1, 5);
		output.WriteLine("After removing the middle five — expected 1, 7:");
		output.WriteLine(list.ToDelimittedString(", "));
		list.Clear();
		output.WriteLine($"After Clear: {list.Count} items.");
		_textBoxEx1.Text = output.ToString();
		_status.Text = "List sequence complete. Single and range event ordering is shown above.";
	}

	// Retain dictionary add/update/remove/missing-remove/Clear behavior and describe its membership.
	private void DictionaryClicked(object sender, EventArgs args) {
		using var output = new StringWriter();
		var dictionary = new ObservableDictionary<string, int>();
		var includeAccess = _includeAccess.Checked;
		output.WriteLine("OBSERVABLE DICTIONARY");
		dictionary.Added += (_, args) => output.WriteLine($"Added: {args.CallArgs.Items.Select(item => $"{item.Key} = {item.Value}").ToDelimittedString(", ")}");
		dictionary.RemovedKeys += (_, args) => output.WriteLine($"Removed keys: {args.CallArgs.Items.ToDelimittedString(", ")}");
		dictionary.Accessed += (_, traits) => {
			if (includeAccess)
				output.WriteLine($"Access: {traits}");
		};

		dictionary.Add("string 1", 1);
		dictionary.Remove("string 1");
		for (var index = 2; index <= 6; index++)
			dictionary.Add($"string {index}", index);
		dictionary["string 6"] = -6;
		output.WriteLine($"Remove missing key: {dictionary.Remove("string 1")}");
		dictionary.Remove(dictionary.ElementAt(0));
		output.WriteLine();
		output.WriteLine("Membership before Clear:");
		foreach (var item in dictionary)
			output.WriteLine($"{item.Key} = {item.Value}");
		dictionary.Clear();
		output.WriteLine($"After Clear: {dictionary.Count} items.");
		_textBoxEx1.Text = output.ToString();
		_status.Text = "Dictionary sequence complete. Removing a missing key leaves membership unchanged.";
	}

	// Clear only the generated event trace.
	private void ClearClicked(object sender, EventArgs args) {
		_textBoxEx1.Clear();
		_status.Text = "Results cleared. Run either sequence again to compare its notifications.";
	}

	// Retain deliberate presentation changes at the screen's accepted-save boundary.
	private void PreferencesEdited(object sender, EventArgs args) {
		if (!HasSettings || Updating)
			return;
		ScreenSettings.IncludeAccessEvents = _includeAccess.Checked;
		MarkSettingsChanged();
	}

	// Standalone/designer construction keeps its default without requiring application services.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			_includeAccess.Checked = ScreenSettings.IncludeAccessEvents;
	}

	#region Inner Types

	/// <summary>Presentation preferences for the observable collection event laboratory.</summary>
	[UserEditable]
	public class ObservableCollectionsScreenSettings : SettingsObject {
		/// <summary>Includes access notifications alongside the mutation events.</summary>
		[DefaultValue(false)]
		[DisplayName("Include access events")]
		public bool IncludeAccessEvents {
			get; set;
		}
	}
	#endregion
}
