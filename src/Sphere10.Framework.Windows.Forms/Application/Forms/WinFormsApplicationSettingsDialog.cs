// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits the actual plugin, block and screen settings owned by the running application.</summary>
public partial class WinFormsApplicationSettingsDialog : Form {
	private readonly Dictionary<SettingsObject, string> _backups = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<SettingsObject> _dirty = new(ReferenceEqualityComparer.Instance);
	private readonly IWinFormsApplication _application;
	private readonly IServiceProvider _services;
	private object _selectedOwner;
	private ICrudEntityEditor<object> _editor;
	private Control _editorControl;
	private bool _applying;

	/// <summary>Creates the native designer surface without resolving application services or settings.</summary>
	public WinFormsApplicationSettingsDialog() {
		InitializeComponent();
		UpdateButtons();
	}

	/// <summary>Enumerates existing owners and prefers the active screen's settings unless an owner is supplied.</summary>
	public WinFormsApplicationSettingsDialog(IWinFormsApplication application, IServiceProvider services = null, object selectedOwner = null)
		: this() {
		_application = application;
		_services = services;
		PopulateNavigation();

		// Follow the visible screen rather than the block currently browsed in navigation.
		if (selectedOwner is ApplicationScreenDefinition { Settings: null })
			selectedOwner = null;
		var activeScreen = _application?.ScreenHost?.ActiveScreen;
		selectedOwner ??= activeScreen?.ScreenSettings ?? activeScreen?.Block?.BlockSettings;
		if (selectedOwner != null)
			SelectOwner(selectedOwner);
		else {
			var node = FindFirstSettingsNode(_navigation.Nodes);
			if (node != null)
				_navigation.SelectedNode = node;
		}
	}

	/// <summary>The selected existing plugin, block or screen definition.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedOwner {
		get {
			return _selectedOwner;
		}
	}

	/// <summary>Whether any visited settings object contains unapplied dialog edits.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasUnappliedChanges {
		get {
			return _dirty.Count > 0 || _editor?.HasChanges == true;
		}
	}

	/// <summary>Selects an owner already present in the running application's graph.</summary>
	public void SelectOwner(object owner) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		var node = FindOwnerNode(_navigation.Nodes, owner);
		Guard.Argument(node != null, nameof(owner), "The owner does not belong to this application's Options tree.");
		_navigation.SelectedNode = node;
		node.EnsureVisible();
	}

	/// <summary>Validates and saves the selected actual settings object, which then notifies its listeners.</summary>
	public async Task<bool> ApplySelectedAsync(CancellationToken cancellationToken = default) {
		var settings = GetSettings(_selectedOwner);
		if (!CanEdit(_selectedOwner))
			return true;
		Guard.Ensure(!_applying, "Settings are already being saved.");
		CaptureEditorChanges();
		_applying = true;
		UpdateButtons();
		_status.ForeColor = SystemColors.ControlText;
		_status.Text = "Saving settings...";
		try {
			// Use the settings object's existing validation and ordinary data annotations.
			var result = settings.Validate();
			var annotations = new List<ValidationResult>();
			Validator.TryValidateObject(settings, new ValidationContext(settings), annotations, true);
			if (result.IsFailure || annotations.Count > 0) {
				ShowError(string.Join(Environment.NewLine, result.ErrorMessages.Concat(annotations.Select(error => error.ErrorMessage))));
				return false;
			}

			// A successful explicit save is the only boundary that publishes OnChanged.
			await settings.SaveAsync(cancellationToken);
			_backups[settings] = SettingsObjectJsonSerializer.SerializeValues(settings);
			_dirty.Remove(settings);
			RefreshPageCaption(_selectedOwner);
			CreateEditor();
			_status.ForeColor = SystemColors.ControlText;
			_status.Text = "Settings saved.";
			return true;
		} catch (Exception error) {
			ShowError(error.Message);
			return false;
		} finally {
			_applying = false;
			UpdateButtons();
		}
	}

	/// <summary>Restores the selected object's declared defaults without saving them.</summary>
	public void ResetSelectedDefaults() {
		Guard.Ensure(!_applying, "Wait for the current settings save to complete.");
		if (!CanEdit(_selectedOwner))
			return;
		var settings = GetSettings(_selectedOwner);
		settings.RestoreDefaultValues();
		_dirty.Add(settings);
		CreateEditor();
		RefreshPageCaption(_selectedOwner);
		_status.ForeColor = SystemColors.ControlText;
		_status.Text = "Defaults are provisional until Apply or OK succeeds.";
	}

	/// <summary>Restores local backups for unapplied edits while retaining earlier successful Apply operations.</summary>
	public void CancelEdits() {
		CaptureEditorChanges();
		foreach (var settings in _dirty)
			SettingsObjectJsonSerializer.PopulateValues(_backups[settings], settings);
		_dirty.Clear();
		ClearEditor();
	}

	// Do not dispose an editor while its explicitly accepted save is still running.
	protected override void OnFormClosing(FormClosingEventArgs e) {
		if (_applying) {
			e.Cancel = true;
			return;
		}
		if (DialogResult != DialogResult.OK)
			CancelEdits();
		base.OnFormClosing(e);
	}

	// Navigation retains only dialog-local backups; the owner always keeps the actual settings object.
	private void NavigationAfterSelect(object sender, TreeViewEventArgs e) {
		CaptureEditorChanges();
		ClearEditor();
		_selectedOwner = e.Node.Tag;
		_status.Clear();
		_title.Text = e.Node.Text;
		_emptyPage.Visible = true;
		var settings = GetSettings(_selectedOwner);
		if (settings == null) {
			_emptyPage.Text = "This item has no configurable settings.";
			UpdateButtons();
			return;
		}
		_backups.TryAdd(settings, SettingsObjectJsonSerializer.SerializeValues(settings));
		try {
			CreateEditor();
		} catch (Exception error) {
			ShowError(error.Message);
		}
		UpdateButtons();
	}

	// Factories and attributed editors receive the actual object, rather than an edit-session surrogate.
	private void CreateEditor() {
		ClearEditor();
		if (!CanEdit(_selectedOwner)) {
			_emptyPage.Text = "These settings do not have a user editor.";
			_emptyPage.Visible = true;
			return;
		}
		var settings = GetSettings(_selectedOwner);
		var factory = (_selectedOwner as WinFormsApplicationScreenDefinition)?.SettingsEditor;
		var editorType = factory == null ? GetDeclaredEditorType(settings.GetType()) : null;
		var context = new WinFormsApplicationSettingsEditorContext(settings, _application, _services, this);
		ICrudEntityEditor<object> editor;
		if (factory != null)
			editor = factory(context);
		else if (editorType != null) {
			Guard.Argument(!editorType.IsAbstract && !editorType.ContainsGenericParameters && typeof(ICrudEntityEditor<object>).IsAssignableFrom(editorType),
				nameof(editorType), "A settings editor must implement ICrudEntityEditor<object>.");
			var constructor = editorType.GetConstructor(new[] { typeof(WinFormsApplicationSettingsEditorContext) });
			editor = (ICrudEntityEditor<object>)(constructor == null ? Tools.Object.Create(editorType) : constructor.Invoke(new object[] { context }));
		} else
			editor = new DefaultCrudEntityEditor {
				PropertyFilter = (owner, property) => Tools.UI.IsSettingsMember(property.PropertyType, property.Attributes.Cast<Attribute>())
					&& (owner is not SettingsObject || property.Name is not (nameof(SettingsObject.ID) or nameof(SettingsObject.Provider))),
				PropertySort = PropertySort.CategorizedAlphabetical
			};

		// The dialog owns the fresh editor control; the settings and application services are borrowed.
		Guard.Ensure(editor != null, "A settings editor factory must return an editor.");
		var control = editor.AsControl();
		Guard.Ensure(control != null && !control.IsDisposed && control.Parent == null, "A settings editor must return a new unattached control.");
		var accepted = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!accepted)
				control.Dispose();
		});
		editor.SetEntity(DataSourceCapabilities.CanUpdate, settings, false);
		control.Dock = DockStyle.Fill;
		_editorHost.Controls.Add(control);
		_editor = editor;
		_editorControl = control;
		_editor.PropertyChanged += EditorPropertyChanged;
		_emptyPage.Visible = false;
		accepted = true;
	}

	// Editors keep the supplied settings root; dirty state belongs only to this modal dialog.
	private void CaptureEditorChanges() {
		if (_editor == null)
			return;
		var settings = GetSettings(_selectedOwner);
		Guard.Ensure(ReferenceEquals(_editor.GetEntityWithChanges(), settings), "A settings editor must edit the actual settings object supplied by SetEntity.");
		if (_editor.HasChanges) {
			_dirty.Add(settings);
			RefreshPageCaption(_selectedOwner);
		}
	}

	// Property changes remain provisional until the user explicitly saves the actual settings object.
	private void EditorPropertyChanged(CrudEntityPropertyChangedEventArgs change) {
		var settings = GetSettings(_selectedOwner);
		if (settings == null)
			return;
		_dirty.Add(settings);
		RefreshPageCaption(_selectedOwner);
	}

	// Release only the native editor and its removable event subscription.
	private void ClearEditor() {
		if (_editor != null)
			_editor.PropertyChanged -= EditorPropertyChanged;
		_editor = null;
		if (_editorControl == null)
			return;
		_editorHost.Controls.Remove(_editorControl);
		_editorControl.Dispose();
		_editorControl = null;
	}

	// Project the existing application graph directly into the native navigation tree.
	private void PopulateNavigation() {
		var productName = _services?.GetService<IProductInformationProvider>()?.ProductInformation?.ProductName;
		var root = _navigation.Nodes.Add(string.IsNullOrWhiteSpace(productName) ? "Application" : productName);
		if (_application == null)
			return;
		foreach (var plugin in _application.LoadedPlugins.Cast<IApplicationPlugin>().Concat(_application.Blocks.Select(block => block.Source).Where(plugin => plugin != null))
			.Distinct<IApplicationPlugin>(ReferenceEqualityComparer.Instance)) {
			var node = new TreeNode(plugin.Name) { Tag = plugin };
			root.Nodes.Add(node);
			foreach (var block in _application.Blocks.Where(block => ReferenceEquals(block.Source, plugin)))
				AddBlock(node.Nodes, block);
		}
		foreach (var block in _application.Blocks.Where(block => block.Source == null))
			AddBlock(root.Nodes, block);
		root.Expand();
	}

	// Only settings-bearing screen definitions belong in Options, including registrations not yet opened.
	private static void AddBlock(TreeNodeCollection nodes, IApplicationBlock block) {
		var node = new TreeNode(block.Name) { Tag = block };
		nodes.Add(node);
		foreach (var definition in block.ScreenDefinitions.Where(definition => definition.Settings != null)) {
			var duplicate = block.ScreenDefinitions.Count(other => other.Settings != null && other.Title == definition.Title) > 1;
			var caption = duplicate ? $"{definition.Title} ({Tools.UI.GetScreenRegistrationKey(block, definition)})" : definition.Title;
			node.Nodes.Add(new TreeNode(caption) { Tag = definition });
		}
	}

	// Resolve values through their existing owners rather than a parallel settings registry.
	private static SettingsObject GetSettings(object owner) {
		return owner switch {
			IApplicationPlugin plugin => plugin.PluginSettings,
			IApplicationBlock block => block.BlockSettings,
			ApplicationScreenDefinition definition => definition.Settings,
			_ => null
		};
	}

	// Select the first actual settings page when the caller did not choose an owner.
	private static TreeNode FindFirstSettingsNode(TreeNodeCollection nodes) {
		foreach (TreeNode node in nodes) {
			if (GetSettings(node.Tag) != null)
				return node;
			var child = FindFirstSettingsNode(node.Nodes);
			if (child != null)
				return child;
		}
		return null;
	}

	// Find an existing node by owner identity without creating another settings model.
	private static TreeNode FindOwnerNode(TreeNodeCollection nodes, object owner) {
		foreach (TreeNode node in nodes) {
			if (ReferenceEquals(node.Tag, owner) || ReferenceEquals(GetSettings(node.Tag), owner))
				return node;
			var child = FindOwnerNode(node.Nodes, owner);
			if (child != null)
				return child;
		}
		return null;
	}

	// Dirty markers affect presentation only, preserving each owner's registration identity.
	private void RefreshPageCaption(object owner) {
		var node = FindOwnerNode(_navigation.Nodes, owner);
		if (node == null)
			return;
		node.Name = string.IsNullOrEmpty(node.Name) ? node.Text : node.Name;
		node.Text = node.Name + (_dirty.Contains(GetSettings(owner)) ? " *" : string.Empty);
		if (ReferenceEquals(owner, _selectedOwner))
			_title.Text = node.Text;
	}

	// Keep the existing native layout responsive while an explicit settings save is pending.
	private void UpdateButtons() {
		_navigation.Enabled = !_applying;
		_editorHost.Enabled = !_applying;
		_apply.Enabled = !_applying && CanEdit(_selectedOwner);
		_reset.Enabled = _apply.Enabled;
		_ok.Enabled = !_applying;
		_cancel.Enabled = !_applying;
	}

	// Automatic editing is opt-in on the settings class; explicit editors take precedence.
	private static bool CanEdit(object owner) {
		var settings = GetSettings(owner);
		return settings != null && ((owner as WinFormsApplicationScreenDefinition)?.SettingsEditor != null
			|| GetDeclaredEditorType(settings.GetType()) != null || Attribute.IsDefined(settings.GetType(), typeof(UserEditableAttribute), true));
	}

	// The standard component-model attribute names a custom editor shared by any owner of that settings type.
	private static Type GetDeclaredEditorType(Type settingsType) {
		var editor = TypeDescriptor.GetAttributes(settingsType).OfType<EditorAttribute>()
			.FirstOrDefault(attribute => Type.GetType(attribute.EditorBaseTypeName, false) == typeof(ICrudEntityEditor<object>));
		return editor == null ? null : Type.GetType(editor.EditorTypeName, true);
	}

	// Failed validation or storage leaves the editor available for correction and retry.
	private void ShowError(string message) {
		_status.ForeColor = Color.Firebrick;
		_status.Text = message;
	}

	// Apply only the selected actual owner while retaining other dialog-local edits.
	private async void ApplyClick(object sender, EventArgs e) {
		await ApplySelectedAsync();
	}

	// Close only after the selected save succeeds and other visited pages have been resolved.
	private async void OkClick(object sender, EventArgs e) {
		if (!await ApplySelectedAsync())
			return;
		if (HasUnappliedChanges) {
			ShowError("Other pages have unapplied changes. Apply them or Cancel to discard the remaining edits.");
			return;
		}
		DialogResult = DialogResult.OK;
		Close();
	}

	// Use the settings object's existing declared-default lifecycle.
	private void ResetClick(object sender, EventArgs e) {
		ResetSelectedDefaults();
	}

	// Discard only unapplied local edits; previously accepted saves remain authoritative.
	private void CancelClick(object sender, EventArgs e) {
		CancelEdits();
		DialogResult = DialogResult.Cancel;
		Close();
	}
}
