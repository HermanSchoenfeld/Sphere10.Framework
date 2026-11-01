// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>Edits catalog pages through detached sessions with explicit validation and persist-before-publish acceptance.</summary>
public partial class BlazorApplicationSettingsDialog : ModalComponent {
	private readonly Dictionary<ApplicationSettingsProfile, ApplicationSettingsEditSession> _edits = new();
	private readonly HashSet<ApplicationSettingsProfile> _dirty = new();
	private ApplicationSettingsProfile _selected;
	private DynamicComponent _editor;
	private string[] _errors = Array.Empty<string>();
	private bool _saving;

	/// <summary>The owning application's catalog, including unopened and menu-free registrations.</summary>
	[Parameter] public ApplicationSettingsCatalog Catalog { get; set; }

	/// <summary>The readable application heading, separate from its persistence key.</summary>
	[Parameter] public string ApplicationTitle { get; set; } = "Application";

	// A settings value remains persistent even when it has not opted into the automatic editor.
	private bool CanEditSelected {
		get {
			return CanEdit(_selected);
		}
	}

	// Editors are renderer-owned components; the dialog alone owns each edit session and its acceptance boundary.
	private Type EditorType {
		get {
			var editor = (_selected?.ScreenDefinition as BlazorApplicationScreenDefinition)?.SettingsEditor
				?? _selected?.ApplicationDefinition?.SettingsEditor ?? typeof(BlazorApplicationSettingsEditor);
			Guard.Argument(!editor.IsAbstract && !editor.ContainsGenericParameters && typeof(IComponent).IsAssignableFrom(editor)
				&& typeof(IBlazorApplicationSettingsEditor).IsAssignableFrom(editor), nameof(EditorType),
				"A settings editor must be a concrete component implementing IBlazorApplicationSettingsEditor.");
			return editor;
		}
	}

	// Capture the page in its callback so an unmounted editor cannot dirty a newly selected registration.
	private IDictionary<string, object> EditorParameters {
		get {
			var profile = _selected;
			return new Dictionary<string, object> {
				[nameof(IBlazorApplicationSettingsEditor.Value)] = _edits[profile].Draft,
				[nameof(IBlazorApplicationSettingsEditor.Changed)] = EventCallback.Factory.Create(this, () => MarkDirty(profile))
			};
		}
	}

	/// <summary>Applies the selected page and retains the dialog and all other provisional pages.</summary>
	public async Task ApplyAsync() {
		if (_selected != null && _dirty.Contains(_selected))
			await SaveSelectedPageAsync();
	}

	/// <summary>Accepts the selected page and closes once no other page has unapplied edits.</summary>
	public async Task AcceptAsync() {
		if (_saving)
			return;
		if (!await SaveSelectedPageAsync())
			return;
		if (_dirty.Count != 0) {
			_errors = new[] { "Other pages still have unapplied changes. Apply those pages, or choose Cancel to discard unapplied changes." };
			return;
		}
		await Ok();
	}

	/// <summary>Replaces the selected draft with its authoritative fresh defaults without saving.</summary>
	public void ResetDefaults() {
		if (_saving || !CanEditSelected)
			return;
		_edits[_selected].Reset();
		_dirty.Add(_selected);
		_errors = Array.Empty<string>();
	}

	/// <summary>Discards edits since the last successful Apply; accepted preferences remain published.</summary>
	public void CancelEditing() {
		if (_saving)
			return;
		foreach (var session in _edits.Values)
			session.Cancel();
		Cancel();
	}

	/// <summary>Escape, the close button and backdrop follow the same cancellation boundary.</summary>
	public override Task<bool> RequestCloseAsync() {
		if (_saving)
			return Task.FromResult(false);
		CancelEditing();
		return Task.FromResult(true);
	}

	// Restoration normally completed at circuit startup; direct hosts still use that same catalog initialization.
	protected override async Task OnInitializedAsync() {
		Guard.ArgumentNotNull(Catalog, nameof(Catalog));
		await Catalog.InitializeAsync();
		await SelectPageAsync(Catalog.Profiles.FirstOrDefault(CanEdit) ?? Catalog.Profiles.FirstOrDefault());
	}

	// Valid pending input is retained per page; invalid text must be corrected before its editor is unmounted.
	private async Task SelectPageAsync(ApplicationSettingsProfile profile) {
		if (_saving || ReferenceEquals(profile, _selected))
			return;
		_saving = true;
		var editor = _editor?.Instance as IBlazorApplicationSettingsEditor;
		try {
			if (!await ValidateEditorAsync(editor))
				return;
			_selected = profile;
			_editor = null;
			_errors = Array.Empty<string>();
			if (CanEdit(profile) && !_edits.ContainsKey(profile))
				_edits.Add(profile, profile.BeginEdit());
		} finally {
			_saving = false;
		}
	}

	// A save failure leaves this detached page dirty and visible; no other page is implicitly accepted.
	private async Task<bool> SaveSelectedPageAsync() {
		if (_saving)
			return false;
		// Keep this page and its editor together while custom validation yields to the renderer.
		_saving = true;
		var profile = _selected;
		var editor = _editor?.Instance as IBlazorApplicationSettingsEditor;
		try {
			if (!await ValidateEditorAsync(editor))
				return false;
			if (profile == null || !_dirty.Contains(profile))
				return true;
			_errors = Array.Empty<string>();
			var validation = await _edits[profile].ApplyAsync();
			if (validation.IsFailure) {
				_errors = validation.ErrorMessages.ToArray();
				return false;
			}
			_dirty.Remove(profile);
			return true;
		} catch (Exception error) {
			_errors = new[] { "Preferences could not be saved. Correct the problem and try again. " + error.Message };
			return false;
		} finally {
			_saving = false;
		}
	}

	// Pending text belongs to the active component; declared value validation is invoked once by ApplyAsync.
	private async Task<bool> ValidateEditorAsync(IBlazorApplicationSettingsEditor editor) {
		if (editor == null)
			return true;
		var validation = await editor.ValidateAsync();
		if (validation.IsSuccess)
			return true;
		_errors = validation.ErrorMessages.ToArray();
		return false;
	}

	// The callback captures its own page so an old component cannot mark another registration's draft dirty.
	private void MarkDirty(ApplicationSettingsProfile profile) {
		_dirty.Add(profile);
	}

	// An explicit editor owns its object contract; automatic editing requires the class-level opt-in.
	private static bool CanEdit(ApplicationSettingsProfile profile) {
		return profile?.HasOptions == true && ((profile.ScreenDefinition as BlazorApplicationScreenDefinition)?.SettingsEditor != null
			|| profile.ApplicationDefinition?.SettingsEditor != null || Attribute.IsDefined(profile.SettingsType, typeof(UserEditableAttribute), true));
	}

	// Selection and unsaved markers are presentation only and never become profile identity.
	private string PageClass(ApplicationSettingsProfile profile) {
		return ReferenceEquals(profile, _selected) ? "sphere10-options-page-link selected" : "sphere10-options-page-link";
	}
}
