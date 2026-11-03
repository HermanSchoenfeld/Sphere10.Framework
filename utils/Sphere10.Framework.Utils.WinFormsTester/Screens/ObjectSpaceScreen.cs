// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.CryptoEx.EC;
using Sphere10.Framework.ObjectSpaces;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Creates, edits and directly reloads real account/public-identity ObjectSpaces.</summary>
public partial class ObjectSpaceScreen : ObjectSpaceScreenBase {
	private ObjectSpace _space;
	private MemoryStream _rawStream;
	private bool _fileOperation;
	private Task _preferencesLoading = Task.CompletedTask;

	/// <summary>Creates the design surface without opening files, creating keys or constructing a store.</summary>
	public ObjectSpaceScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		UpdateAvailability();
	}

	// The selected byte order remains fixed while its store is open.
	private Endianness SelectedEndianness => _endianness.SelectedIndex == 1 ? Endianness.BigEndian : Endianness.LittleEndian;

	/// <summary>Joins optional legacy restoration before the normal accepted-close capture.</summary>
	public override async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		await _preferencesLoading.WaitAsync(cancellationToken);
		if (!IsDisposed && !Disposing)
			await base.SaveSettingsAsync(cancellationToken);
	}

	// The host attaches ordinary preferences before native Load.
	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		_preferencesLoading = InitializePreferencesAsync();
		UpdateAvailability();
		await _preferencesLoading;
		if (!IsDisposed && !Disposing)
			UpdateAvailability();
	}

	// Options affects the next store and remembered path, never an open store's schema or byte order.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	// Stores and their borrowed raw stream stay on this native owner for their full lifetime.
	protected override void OnDestroyScreen() {
		CloseStore();
		base.OnDestroyScreen();
	}

	// The explicit create command is the only path that generates new demonstration identities.
	private void CreateClicked(object sender, EventArgs args) {
		Run(() => {
			CloseStore();
			_rawStream = new MemoryStream();
			try {
				_space = new ObjectSpaceBuilder()
					.UseMemoryStream(_rawStream)
					.WithEndianness(SelectedEndianness)
					.StoreMetaData()
					.AddDimension<Identity>(ignoreAnnotations: true)
						.WithUniqueIndexOn(item => item.Key)
						.Done()
					.AddDimension<Account>(ignoreAnnotations: true)
						.WithUniqueIndexOn(item => item.Name)
						.Done()
					.Build();
				for (var index = 1; index <= (int)_sampleCount.Value; index++)
					AddAccount($"Sample account {index}", index * 125M);
				RefreshAccounts();
				Report("Created a real two-dimension metadata store. Only public identity bytes are persisted.");
			} catch {
				CloseStore();
				throw;
			}
		});
	}

	// New records use the same unique indexes and tracked save path as later edits.
	private void AddClicked(object sender, EventArgs args) {
		Run(() => {
			RequireStore();
			AddAccount(ValidateName(), _quantity.Value);
			RefreshAccounts();
			Report("Account and public identity saved.");
		});
	}

	// Validate unique membership before modifying the identity-tracked object.
	private void UpdateClicked(object sender, EventArgs args) {
		Run(() => {
			RequireStore();
			var originalName = SelectedName();
			var name = ValidateName();
			var account = _space.Get((Account item) => item.Name, originalName);
			if (_space.TryGet((Account item) => item.Name, name, out var existing) && !ReferenceEquals(existing, account))
				throw new InvalidOperationException("Another account already uses this name.");
			account.Name = name;
			account.Quantity = _quantity.Value;
			_space.Save(account);
			RefreshAccounts();
			Report("The selected tracked account was updated and explicitly saved.");
		});
	}

	// Delete both independently stored records, retaining no stale row/entity snapshot.
	private void DeleteClicked(object sender, EventArgs args) {
		Run(() => {
			RequireStore();
			var account = _space.Get((Account item) => item.Name, SelectedName());
			var identity = _space.Get((Identity item) => item.Key, account.Identity.Key);
			_space.Delete(account);
			_space.Delete(identity);
			RefreshAccounts();
			Report("Selected account and its public identity deleted.");
		});
	}

	// A complete byte snapshot is reopened through metadata, without rebuilding the definition in client code.
	private void ReloadClicked(object sender, EventArgs args) {
		Run(() => {
			RequireStore();
			_space.Flush();
			var bytes = _rawStream.ToArray();
			var endianness = _space.Traits.HasFlag(ObjectSpaceTraits.BigEndian) ? Endianness.BigEndian : Endianness.LittleEndian;
			OpenBytes(bytes, endianness);
			Report("Reloaded directly with ObjectSpace.TryOpenFromStream. The definition and factory came from metadata.");
		});
	}

	// Snapshot writes occur only after the user's explicit Save dialog acceptance.
	private async void SaveSnapshotClicked(object sender, EventArgs args) {
		if (_fileOperation)
			return;
		try {
			RequireStore();
			using var dialog = new SaveFileDialog { Title = "Save ObjectSpace snapshot", Filter = "ObjectSpace snapshots (*.s10space)|*.s10space|All files (*.*)|*.*", FileName = _objectSpacePathControl.Path };
			if (dialog.ShowDialog(this) != DialogResult.OK)
				return;
			_fileOperation = true;
			UpdateAvailability();
			_space.Flush();
			var bytes = _rawStream.ToArray();
			await File.WriteAllBytesAsync(dialog.FileName, bytes);
			if (IsDisposed || Disposing)
				return;
			_objectSpacePathControl.Path = dialog.FileName;
			Report($"Saved {bytes.Length:N0} bytes. The snapshot includes ObjectSpace and dimension metadata.");
		} catch (Exception error) {
			ReportError(error);
		} finally {
			_fileOperation = false;
			if (!IsDisposed && !Disposing)
				UpdateAvailability();
		}
	}

	// File IO completes before the store is hydrated on its one native owner thread.
	private async void OpenSnapshotClicked(object sender, EventArgs args) {
		if (_fileOperation)
			return;
		try {
			using var dialog = new OpenFileDialog { Title = "Open ObjectSpace snapshot", Filter = "ObjectSpace snapshots (*.s10space)|*.s10space|All files (*.*)|*.*", FileName = _objectSpacePathControl.Path };
			if (dialog.ShowDialog(this) != DialogResult.OK)
				return;
			_fileOperation = true;
			UpdateAvailability();
			if (new FileInfo(dialog.FileName).Length > 16 * 1024 * 1024)
				throw new InvalidOperationException("This interactive demonstration opens snapshots up to 16 MiB.");
			var endianness = SelectedEndianness;
			var bytes = await File.ReadAllBytesAsync(dialog.FileName);
			if (IsDisposed || Disposing)
				return;
			if (bytes.Length > 16 * 1024 * 1024)
				throw new InvalidOperationException("This interactive demonstration opens snapshots up to 16 MiB.");
			OpenBytes(bytes, endianness);
			_objectSpacePathControl.Path = dialog.FileName;
			Report("Snapshot opened from its metadata. Selected byte order is supplied by this client.");
		} catch (Exception error) {
			ReportError(error);
		} finally {
			_fileOperation = false;
			if (!IsDisposed && !Disposing)
				UpdateAvailability();
		}
	}

	// Closing is explicit and never writes a snapshot implicitly.
	private void CloseClicked(object sender, EventArgs args) {
		Run(() => {
			CloseStore();
			_accountsGrid.Rows.Clear();
			Report("Store closed. Existing snapshot files are unchanged.");
		});
	}

	// Selection edits only the input controls; persistence still requires Save account.
	private void AccountSelected(object sender, EventArgs args) {
		if (_accountsGrid.SelectedRows.Count == 0)
			return;
		var row = _accountsGrid.SelectedRows[0];
		_accountName.Text = (string)row.Cells[0].Value;
		_quantity.Value = Math.Clamp(Convert.ToDecimal(row.Cells[1].Value), _quantity.Minimum, _quantity.Maximum);
		UpdateAvailability();
	}

	// UI choices, not operational account data or key material, belong to the registration preferences.
	private void PreferencesEdited(object sender, EventArgs args) {
		if (!HasSettings || Updating)
			return;
		var settings = base.ScreenSettings;
		settings.SampleAccounts = (int)_sampleCount.Value;
		settings.Endianness = SelectedEndianness;
		MarkSettingsChanged();
	}

	// The path selector uses its existing semantic event.
	private void SnapshotPathChanged() {
		if (!HasSettings || Updating)
			return;
		base.ScreenSettings.SnapshotPath = _objectSpacePathControl.Path;
		MarkSettingsChanged();
	}

	// Create independent public identities; private keys are never serialized into either dimension.
	private void AddAccount(string name, decimal quantity) {
		if (_space.TryGet((Account item) => item.Name, name, out _))
			throw new InvalidOperationException("An account with this name already exists.");
		var signer = new ECDSA(ECDSAKeyType.SECP256K1);
		var privateKey = signer.GeneratePrivateKey();
		Identity identity;
		try {
			identity = new Identity { DSS = DSS.ECDSA_SECP256k1, Key = signer.DerivePublicKey(privateKey, 0).RawBytes };
		} finally {
			Tools.Crypto.SecureErase(privateKey.RawBytes);
		}
		_space.Save(identity);
		try {
			_space.Save(new Account { Name = name, Quantity = quantity, Identity = identity });
		} catch {
			_space.Delete(identity);
			throw;
		}
	}

	// The current store closes before a replacement begins; failed hydration releases the new raw stream.
	private void OpenBytes(byte[] bytes, Endianness endianness) {
		CloseStore();
		_rawStream = new MemoryStream(bytes.Length);
		_rawStream.Write(bytes);
		_rawStream.Position = 0;
		try {
			if (!ObjectSpace.TryOpenFromStream(_rawStream, endianness, out _space))
				throw new InvalidOperationException("No compatible ObjectSpace metadata was found for the supplied byte order.");
			if (!_space.Definition.Dimensions.Any(dimension => dimension.ObjectType == typeof(Account))
				|| !_space.Definition.Dimensions.Any(dimension => dimension.ObjectType == typeof(Identity)))
				throw new InvalidOperationException("This snapshot does not contain the account/public-identity demonstration dimensions.");
			RefreshAccounts();
		} catch {
			CloseStore();
			throw;
		}
	}

	// Native rows contain value snapshots, while every later mutation resolves its tracked record through the store.
	private void RefreshAccounts() {
		_space.Flush();
		_accountsGrid.Rows.Clear();
		foreach (var account in _space.GetAll<Account>()) {
			var key = account.Identity?.Key ?? Array.Empty<byte>();
			_accountsGrid.Rows.Add(account.Name, account.Quantity, key.ToHexString());
		}
		_status.Text = $"{_space.Count<Account>():N0} accounts · {_space.Count<Identity>():N0} identities · {_rawStream.Length:N0} bytes · traits {_space.Traits}";
		UpdateAvailability();
	}

	// Dispose the store before its borrowed backing stream, even if flushing fails.
	private void CloseStore() {
		try {
			_space?.Dispose();
		} finally {
			_space = null;
			_rawStream?.Dispose();
			_rawStream = null;
		}
		if (!IsDisposed && !Disposing)
			UpdateAvailability();
	}

	// Import the old standalone path only before the actual screen has its first typed settings record.
	private async Task InitializePreferencesAsync() {
		if (!HasSettings || Tools.Runtime.IsDesignMode)
			return;
		try {
			var settings = base.ScreenSettings;
			var provider = settings.Provider;
			if (provider != null && string.IsNullOrEmpty(settings.SnapshotPath)
				&& !await provider.ContainsSettingAsync(settings.GetType(), settings.ID)
				&& await provider.ContainsSettingAsync(typeof(ScreenSettings))) {
				var legacy = (ScreenSettings)await provider.LoadSettingAsync(typeof(ScreenSettings));
				if (!IsDisposed && !Disposing)
					settings.SnapshotPath = legacy.FilePath ?? string.Empty;
			}
		} catch (Exception error) {
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error, "Could not import the existing ObjectSpace demonstration path."));
		}
		if (!IsDisposed && !Disposing)
			ApplyPreferences();
	}

	// Keep both ownership and validation failures visible without escaping native event dispatch.
	private void Run(Action action) {
		try {
			action();
		} catch (Exception error) {
			ReportError(error);
		} finally {
			if (!IsDisposed && !Disposing)
				UpdateAvailability();
		}
	}

	// Commands operate only on an existing store and an actual selected row.
	private void UpdateAvailability() {
		var open = _space != null;
		var selected = open && _accountsGrid.SelectedRows.Count > 0;
		var busy = _fileOperation || !_preferencesLoading.IsCompleted;
		_inputs.Enabled = !busy;
		_objectSpacePathControl.Enabled = !busy;
		_buildButton.Enabled = !busy;
		_openSnapshotButton.Enabled = !busy;
		_addButton.Enabled = !busy && open;
		_updateButton.Enabled = !busy && selected;
		_deleteButton.Enabled = !busy && selected;
		_reloadButton.Enabled = !busy && open;
		_saveSnapshotButton.Enabled = !busy && open;
		_closeButton.Enabled = !busy && open;
		_endianness.Enabled = !busy && !open;
	}

	// Retain existing store settings when Options changes the next-create defaults.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		var settings = base.ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_sampleCount.Value = Math.Clamp(settings.SampleAccounts, 1, 25);
			_objectSpacePathControl.Path = settings.SnapshotPath ?? string.Empty;
			if (_space == null)
				_endianness.SelectedIndex = settings.Endianness == Endianness.BigEndian ? 1 : 0;
		}
	}

	// An empty name cannot represent a useful unique account record.
	private string ValidateName() {
		var name = _accountName.Text.Trim();
		if (name.Length == 0 || name.Length > 80)
			throw new InvalidOperationException("Enter an account name between 1 and 80 characters.");
		return name;
	}

	// Resolve the original selected identity rather than an edited input value.
	private string SelectedName() {
		if (_accountsGrid.SelectedRows.Count == 0)
			throw new InvalidOperationException("Select an account first.");
		return (string)_accountsGrid.SelectedRows[0].Cells[0].Value;
	}

	// Make the required lifecycle action explicit before a record command runs.
	private void RequireStore() {
		if (_space == null)
			throw new InvalidOperationException("Create or open an ObjectSpace first.");
	}

	// Failures retain the existing screen and are visible beside the actions.
	private void ReportError(Exception error) {
		SystemLog.Exception(error, "The ObjectSpace demonstration failed.");
		if (!IsDisposed && !Disposing)
			Report("Could not complete the operation: " + error.Message);
	}

	// The bounded log describes completed actions without exposing secrets.
	private void Report(string message) {
		if (_outputTextBox.Lines.Length > 100)
			_outputTextBox.Clear();
		_outputTextBox.AppendText(message + Environment.NewLine);
		_status.Text = message;
	}

	#region Inner Types

	/// <summary>A uniquely named account with a persisted public identity value.</summary>
	public class Account {
		/// <summary>The unique account name.</summary>
		[UniqueIndex]
		public string Name {
			get; set;
		}

		/// <summary>The sample balance represented by this record.</summary>
		public decimal Quantity {
			get; set;
		}

		/// <summary>The public identity associated with the account.</summary>
		public Identity Identity {
			get; set;
		}
	}

	/// <summary>The signature-scheme identifier and public key bytes; no private key is stored.</summary>
	public class Identity {
		/// <summary>The scheme that produced the public key.</summary>
		public DSS DSS {
			get; set;
		}

		/// <summary>The uniquely indexed public key bytes.</summary>
		[UniqueIndex]
		public byte[] Key {
			get; set;
		}
	}

	/// <summary>Retains the old standalone path-setting type for existing provider records.</summary>
	public new class ScreenSettings : SettingsObject {
		/// <summary>The former standalone snapshot path.</summary>
		[DefaultValue(null)]
		public string FilePath {
			get; set;
		}
	}

	/// <summary>Creation and snapshot-location preferences, excluding the store's records and key material.</summary>
	[UserEditable]
	public class ObjectSpaceScreenSettings : SettingsObject {
		/// <summary>The bounded number of accounts generated by New sample store.</summary>
		[DefaultValue(5)]
		[DisplayName("Sample accounts"), Range(1, 25)]
		public int SampleAccounts {
			get; set;
		} = 5;

		/// <summary>The byte order supplied by the client when creating or opening a snapshot.</summary>
		[DefaultValue(Endianness.LittleEndian)]
		[DisplayName("Byte order")]
		public Endianness Endianness {
			get; set;
		} = Endianness.LittleEndian;

		/// <summary>The most recently selected snapshot path; no file opens merely by restoring it.</summary>
		[DefaultValue("")]
		[DisplayName("Snapshot path")]
		public string SnapshotPath {
			get; set;
		} = string.Empty;
	}
	#endregion
}
