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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits selected application and screen preferences on detached pages without creating their operational screens.</summary>
public partial class WinFormsApplicationSettingsDialog : Form {
	private readonly Dictionary<ApplicationSettingsProfile, PageState> _pages = new(ReferenceEqualityComparer.Instance);
	private readonly ApplicationSettingsCatalog _catalog;
	private readonly IWinFormsApplication _application;
	private readonly IServiceProvider _services;
	private ApplicationSettingsProfile _selected;
	private ICrudEntityEditor<object> _editor;
	private Control _editorControl;
	private bool _applying;

	/// <summary>Creates only the native designer surface, without resolving providers or preference data.</summary>
	public WinFormsApplicationSettingsDialog() {
		InitializeComponent();
		UpdateButtons();
	}

	/// <summary>Projects an initialized catalog and optionally selects the active registration's page.</summary>
	public WinFormsApplicationSettingsDialog(ApplicationSettingsCatalog catalog, IWinFormsApplication application = null,
		IServiceProvider services = null, ApplicationSettingsProfile selectedProfile = null)
		: this() {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		Guard.Ensure(catalog.IsInitialized, "Initialize application preferences before displaying Options.");
		_catalog = catalog;
		_application = application;
		_services = services;
		PopulateNavigation();
		var profile = selectedProfile ?? catalog.Profiles.FirstOrDefault(item => item.HasOptions) ?? catalog.Profiles.FirstOrDefault();
		if (profile != null)
			SelectProfile(profile);
	}

	/// <summary>The selected root or canonical screen registration, including no-options pages.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ApplicationSettingsProfile SelectedProfile => _selected;

	/// <summary>Whether any visited page has unapplied changes, independently of successful earlier Apply operations.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasUnappliedChanges => _pages.Values.Any(page => page.Dirty);

	/// <summary>Selects an existing catalog page without reconstructing its owning screen.</summary>
	public void SelectProfile(ApplicationSettingsProfile profile) {
		Guard.ArgumentNotNull(profile, nameof(profile));
		var node = FindProfileNode(_navigation.Nodes, profile);
		Guard.Argument(node != null, nameof(profile), "The profile does not belong to this Options catalog.");
		_navigation.SelectedNode = node;
		node.EnsureVisible();
	}

	/// <summary>Validates and persists only the selected detached page before accepting its new revision.</summary>
	public async Task<bool> ApplySelectedAsync(CancellationToken cancellationToken = default) {
		if (!CanEditProfile(_selected))
			return true;
		Guard.Ensure(!_applying, "A preference page is already being saved.");
		CaptureEditorChanges();
		var profile = _selected;
		var page = _pages[profile];
		_applying = true;
		UpdateButtons();
		_status.ForeColor = SystemColors.ControlText;
		_status.Text = "Saving preferences...";
		try {
			var validation = await page.Session.ApplyAsync(cancellationToken);
			if (validation.IsFailure) {
				ShowError(string.Join(Environment.NewLine, validation.ErrorMessages));
				return false;
			}
			page.Dirty = false;
			RefreshPageCaption(profile);
			CreateEditor(page);
			_status.ForeColor = SystemColors.ControlText;
			_status.Text = "Preferences saved.";
			return true;
		} catch (Exception error) {
			ShowError(error.Message);
			return false;
		} finally {
			_applying = false;
			UpdateButtons();
		}
	}

	/// <summary>Restores provisional defaults for the selected page without saving or clearing migration presence.</summary>
	public void ResetSelectedDefaults() {
		Guard.Ensure(!_applying, "Wait for the current preference save to complete.");
		if (!CanEditProfile(_selected))
			return;
		var page = _pages[_selected];
		page.Session.Reset();
		page.Dirty = true;
		CreateEditor(page);
		RefreshPageCaption(_selected);
		_status.ForeColor = SystemColors.ControlText;
		_status.Text = "Defaults are provisional until Apply or OK succeeds.";
	}

	/// <summary>Discards all unapplied page drafts while preserving preferences already accepted by Apply.</summary>
	public void CancelEdits() {
		foreach (var pair in _pages) {
			pair.Value.Session.Cancel();
			pair.Value.Dirty = false;
		}
	}

	// An accepted write must finish before closing; disposing a draft never queues another save.
	protected override void OnFormClosing(FormClosingEventArgs e) {
		if (_applying) {
			e.Cancel = true;
			return;
		}
		if (DialogResult != DialogResult.OK)
			CancelEdits();
		base.OnFormClosing(e);
	}

	// Each profile keeps its own detached draft while navigation groups remain metadata-only nodes.
	private void NavigationAfterSelect(object sender, TreeViewEventArgs e) {
		CaptureEditorChanges();
		ClearEditor();
		_selected = e.Node.Tag as ApplicationSettingsProfile;
		_status.Clear();
		_title.Text = _selected?.Title ?? e.Node.Text;
		_emptyPage.Visible = true;
		if (_selected?.HasOptions != true) {
			_emptyPage.Text = _selected == null ? "Select an application or screen to inspect its preferences."
				: "This screen has no configurable preferences.";
			UpdateButtons();
			return;
		}
		if (!_pages.TryGetValue(_selected, out var page)) {
			page = new PageState(_selected.BeginEdit());
			_pages.Add(_selected, page);
		} else if (!page.Dirty)
			page.Session.Cancel();
		try {
			CreateEditor(page);
			if (_selected.LastLoadError != null)
				ShowError("Saved preferences could not be restored; validated defaults are shown. " + _selected.LastLoadError.Message);
		} catch (Exception error) {
			ShowError(error.Message);
		}
		UpdateButtons();
	}

	// Only the selected GUI is instantiated; factories borrow the detached session and existing application context.
	private void CreateEditor(PageState page) {
		ClearEditor();
		var profile = page.Session.Profile;
		if (!CanEditProfile(profile)) {
			_emptyPage.Text = "These preferences do not have a user editor.";
			_emptyPage.Visible = true;
			return;
		}
		var factory = (profile.ScreenDefinition as WinFormsApplicationScreenDefinition)?.SettingsEditor
			?? (profile.ApplicationDefinition as WinFormsApplicationSettingsDefinition)?.EditorFactory;
		var editor = factory != null ? factory(new WinFormsApplicationSettingsEditorContext(page.Session.Draft, _application, _services, this))
			: new DefaultCrudEntityEditor {
				PropertyFilter = (owner, property) => owner is not SettingsObject
					|| property.Name is not (nameof(SettingsObject.ID) or nameof(SettingsObject.Provider)),
				PropertySort = PropertySort.CategorizedAlphabetical
			};
		Guard.Ensure(editor != null, "A settings editor factory must return an editor.");
		var control = editor.AsControl();
		Guard.Ensure(control != null && !control.IsDisposed && control.Parent == null, "A settings editor must return a new unattached control.");
		var accepted = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!accepted)
				control.Dispose();
		});
		editor.SetEntity(DataSourceCapabilities.CanUpdate, page.Session.Draft, false);
		control.Dock = DockStyle.Fill;
		_editorHost.Controls.Add(control);
		_editor = editor;
		_editorControl = control;
		_editor.PropertyChanged += EditorPropertyChanged;
		_emptyPage.Visible = false;
		accepted = true;
	}

	// Editors modify the supplied detached root in place; the shared session remains the sole Apply boundary.
	private void CaptureEditorChanges() {
		if (_editor == null || _selected == null || !_pages.TryGetValue(_selected, out var page))
			return;
		Guard.Ensure(ReferenceEquals(_editor.GetEntityWithChanges(), page.Session.Draft), "A settings editor must edit the detached session root supplied by SetEntity.");
		if (_editor.HasChanges) {
			page.Dirty = true;
			RefreshPageCaption(_selected);
		}
	}

	// Native editor notifications only mark the draft; they do not serialize or persist it.
	private void EditorPropertyChanged(CrudEntityPropertyChangedEventArgs change) {
		if (_selected == null || !_pages.TryGetValue(_selected, out var page))
			return;
		page.Dirty = true;
		RefreshPageCaption(_selected);
	}

	// Editor controls belong to this dialog; borrowed factories and sessions have no disposal side effects.
	private void ClearEditor() {
		if (_editor != null)
			_editor.PropertyChanged -= EditorPropertyChanged;
		_editor = null;
		if (_editorControl != null) {
			_editorHost.Controls.Remove(_editorControl);
			_editorControl.Dispose();
			_editorControl = null;
		}
	}

	// Group by the actual application/plugin/block metadata, retaining distinct registration leaves.
	private void PopulateNavigation() {
		var application = _navigation.Nodes.Add(_catalog.ApplicationKey);
		var roots = _catalog.Profiles.Where(profile => profile.ApplicationDefinition != null).ToArray();
		if (roots.Length != 0) {
			var preferences = application.Nodes.Add("Application Preferences");
			foreach (var profile in roots)
				AddProfileNode(preferences.Nodes, profile, profile.Title);
		}
		foreach (var plugin in _catalog.Profiles.Where(profile => profile.ScreenDefinition != null).GroupBy(profile => profile.Plugin)) {
			var pluginNode = application.Nodes.Add(plugin.Key?.Name ?? "Application Screens");
			foreach (var block in plugin.GroupBy(profile => profile.Block)) {
				var blockNode = pluginNode.Nodes.Add(block.Key.Name);
				foreach (var profile in block) {
					var duplicates = block.Count(other => string.Equals(other.Title, profile.Title, StringComparison.Ordinal)) > 1;
					var caption = duplicates ? $"{profile.Title} ({profile.ScreenDefinition.StableKey ?? (block.ToList().IndexOf(profile) + 1).ToString()})" : profile.Title;
					AddProfileNode(blockNode.Nodes, profile, caption);
				}
			}
		}
		application.Expand();
	}

	// Node names retain their readable base captions when dirty markers are refreshed.
	private static void AddProfileNode(TreeNodeCollection nodes, ApplicationSettingsProfile profile, string caption) {
		nodes.Add(new TreeNode(caption) { Name = caption, Tag = profile });
	}

	// Lookup stays in this dialog's projected tree rather than creating another settings registry.
	private static TreeNode FindProfileNode(TreeNodeCollection nodes, ApplicationSettingsProfile profile) {
		foreach (TreeNode node in nodes) {
			if (ReferenceEquals(node.Tag, profile))
				return node;
			var child = FindProfileNode(node.Nodes, profile);
			if (child != null)
				return child;
		}
		return null;
	}

	// Caption changes are presentation only and never alter the profile's persistent identity.
	private void RefreshPageCaption(ApplicationSettingsProfile profile) {
		var dirty = _pages.TryGetValue(profile, out var page) && page.Dirty;
		var node = FindProfileNode(_navigation.Nodes, profile);
		if (node != null)
			node.Text = node.Name + (dirty ? " *" : string.Empty);
		if (ReferenceEquals(profile, _selected))
			_title.Text = profile.Title + (dirty ? " *" : string.Empty);
	}

	// Keep the form responsive while an accepted snapshot is persisted off the UI thread.
	private void UpdateButtons() {
		_navigation.Enabled = !_applying;
		_editorHost.Enabled = !_applying;
		_apply.Enabled = !_applying && CanEditProfile(_selected);
		_reset.Enabled = _apply.Enabled;
		_ok.Enabled = !_applying && _catalog != null;
		_cancel.Enabled = !_applying;
	}

	// Automatic editing is explicitly allowed by the value class; a product-supplied editor remains independent of that marker.
	private static bool CanEditProfile(ApplicationSettingsProfile profile) {
		return profile?.HasOptions == true && ((profile.ScreenDefinition as WinFormsApplicationScreenDefinition)?.SettingsEditor != null
			|| (profile.ApplicationDefinition as WinFormsApplicationSettingsDefinition)?.EditorFactory != null
			|| Attribute.IsDefined(profile.SettingsType, typeof(UserEditableAttribute), true));
	}

	// Validation/storage failures leave the draft and dialog available for a deliberate retry.
	private void ShowError(string message) {
		_status.ForeColor = Color.Firebrick;
		_status.Text = message;
	}

	// Apply the selected page while preserving every other visited draft.
	private async void ApplyClick(object sender, EventArgs e) {
		await ApplySelectedAsync();
	}

	// OK resolves the selected page and keeps the dialog open while another page has unapplied work.
	private async void OkClick(object sender, EventArgs e) {
		if (!await ApplySelectedAsync())
			return;
		if (HasUnappliedChanges) {
			ShowError("Other pages have unapplied changes. Apply those pages or Cancel to discard the remaining drafts.");
			return;
		}
		DialogResult = DialogResult.OK;
		Close();
	}

	// Reset only the selected provisional page.
	private void ResetClick(object sender, EventArgs e) {
		ResetSelectedDefaults();
	}

	// Discard remaining drafts without undoing previously accepted preferences.
	private void CancelClick(object sender, EventArgs e) {
		CancelEdits();
		DialogResult = DialogResult.Cancel;
		Close();
	}

	// Each visited page retains its own detached session until this dialog closes.
	private sealed class PageState(ApplicationSettingsEditSession session) {
		public ApplicationSettingsEditSession Session { get; } = session;

		public bool Dirty { get; set; }
	}
}
