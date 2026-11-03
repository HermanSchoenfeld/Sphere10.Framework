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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>Edits actual application owner settings, retaining local serialized backups only for Cancel.</summary>
public partial class ApplicationSettingsDialog : ModalComponent {
	private readonly Dictionary<SettingsObject, string> _backups = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<SettingsObject> _dirty = new(ReferenceEqualityComparer.Instance);
	private object _selected;
	private object _editorKey = new();
	private DynamicComponent _editor;
	private string[] _errors = Array.Empty<string>();
	private bool _saving;

	/// <summary>The application supplying its actual plugin, block and screen settings owners.</summary>
	[Parameter] public IApplication Application { get; set; }

	/// <summary>The readable application heading, separate from persistent settings identity.</summary>
	[Parameter] public string ApplicationTitle { get; set; } = "Application";

	/// <summary>The optional owner to select when the dialog opens.</summary>
	[Parameter] public object SelectedOwner { get; set; }

	// Settings remain persistent even when their classes have not enabled automatic editing.
	private bool CanEditSelected {
		get {
			return CanEdit(_selected);
		}
	}

	// Custom editors belong to the settings class or the existing screen registration.
	private Type EditorType {
		get {
			var editor = (_selected as ApplicationScreenDefinition)?.SettingsEditor
				?? GetDeclaredEditorType(GetSettings(_selected).GetType()) ?? typeof(ApplicationSettingsEditor);
			Guard.Argument(!editor.IsAbstract && !editor.ContainsGenericParameters && typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(editor)
				&& typeof(IApplicationSettingsEditor).IsAssignableFrom(editor), nameof(EditorType),
				"A settings editor must be a concrete component implementing IApplicationSettingsEditor.");
			return editor;
		}
	}

	// A stale editor callback can only dirty the actual settings object that it was given.
	private IDictionary<string, object> EditorParameters {
		get {
			var settings = GetSettings(_selected);
			return new Dictionary<string, object> {
				[nameof(IApplicationSettingsEditor.Value)] = settings,
				[nameof(IApplicationSettingsEditor.Changed)] = EventCallback.Factory.Create(this, () => _dirty.Add(settings))
			};
		}
	}

	/// <summary>Saves the selected changed owner while retaining other unapplied edits.</summary>
	public async Task ApplyAsync() {
		if (GetSettings(_selected) is { } settings && _dirty.Contains(settings))
			await SaveSelectedPageAsync();
	}

	/// <summary>Accepts the selected owner and closes after all other edits have been applied.</summary>
	public async Task AcceptAsync() {
		if (_saving || !await SaveSelectedPageAsync())
			return;
		if (_dirty.Count != 0) {
			_errors = new[] { "Other pages still have unapplied changes. Apply those pages, or choose Cancel to discard unapplied changes." };
			return;
		}
		await Ok();
	}

	/// <summary>Restores the actual object through its existing default-value lifecycle without saving.</summary>
	public void ResetDefaults() {
		if (_saving || !CanEditSelected)
			return;
		var settings = GetSettings(_selected);
		settings.RestoreDefaultValues();
		_dirty.Add(settings);
		_editorKey = new object();
		_errors = Array.Empty<string>();
	}

	/// <summary>Restores local backups of unapplied values; successfully saved edits remain accepted.</summary>
	public void CancelEditing() {
		if (_saving)
			return;
		foreach (var settings in _dirty)
			SettingsObjectJsonSerializer.PopulateValues(_backups[settings], settings);
		_dirty.Clear();
		Cancel();
	}

	/// <summary>Escape, the close button and backdrop use the same Cancel boundary.</summary>
	public override Task<bool> RequestCloseAsync() {
		if (_saving)
			return Task.FromResult(false);
		CancelEditing();
		return Task.FromResult(true);
	}

	/// <summary>Selects an existing owner after validating pending editor input.</summary>
	public async Task SelectOwnerAsync(object owner) {
		if (_saving || ReferenceEquals(owner, _selected))
			return;
		_saving = true;
		try {
			if (!await ValidateEditorAsync())
				return;
			_selected = owner;
			_editor = null;
			_editorKey = new object();
			_errors = Array.Empty<string>();
			if (GetSettings(owner) is { } settings && !_backups.ContainsKey(settings))
				_backups.Add(settings, SettingsObjectJsonSerializer.SerializeValues(settings));
		} finally {
			_saving = false;
		}
	}

	/// <summary>Uses the application's existing initialization before exposing any settings owner.</summary>
	protected override async Task OnInitializedAsync() {
		Guard.ArgumentNotNull(Application, nameof(Application));
		await Application.InitializeAsync();
		var owners = GetOwners().ToArray();
		var selected = owners.FirstOrDefault(owner => ReferenceEquals(owner, SelectedOwner)
			|| SelectedOwner != null && ReferenceEquals(GetSettings(owner), SelectedOwner));

		// The active session knows its registration even before its operational component is rendered.
		var activeScreen = Application.ScreenHost.ActiveScreen;
		if (selected == null && activeScreen?.Definition.Settings != null)
			selected = activeScreen.Definition;
		if (selected == null && activeScreen?.Block?.BlockSettings != null)
			selected = activeScreen.Block;
		await SelectOwnerAsync(selected ?? owners.FirstOrDefault(CanEdit) ?? owners.FirstOrDefault(owner => GetSettings(owner) != null));
	}

	// Saving the actual object publishes its existing OnChanged event only after the provider succeeds.
	private async Task<bool> SaveSelectedPageAsync() {
		if (_saving)
			return false;
		_saving = true;
		try {
			if (!await ValidateEditorAsync())
				return false;
			var settings = GetSettings(_selected);
			if (settings == null || !_dirty.Contains(settings))
				return true;
			var validation = settings.Validate();
			var failures = new List<ValidationResult>();
			Validator.TryValidateObject(settings, new ValidationContext(settings), failures, true);
			foreach (var failure in failures)
				validation.AddError(failure.ErrorMessage ?? "The settings value is invalid.");
			_errors = validation.ErrorMessages.ToArray();
			if (validation.IsFailure)
				return false;
			await settings.SaveAsync();
			_backups[settings] = SettingsObjectJsonSerializer.SerializeValues(settings);
			_dirty.Remove(settings);
			return true;
		} catch (Exception error) {
			_errors = new[] { "Preferences could not be saved. Correct the problem and try again. " + error.Message };
			return false;
		} finally {
			_saving = false;
		}
	}

	// The current component owns any invalid pending text that has not reached the settings object.
	private async Task<bool> ValidateEditorAsync() {
		if (_editor?.Instance is not IApplicationSettingsEditor editor)
			return true;
		var validation = await editor.ValidateAsync();
		if (validation.IsSuccess)
			return true;
		_errors = validation.ErrorMessages.ToArray();
		return false;
	}

	// Enumerate existing owners and only settings-bearing screens, including unopened and menu-free registrations.
	private IEnumerable<object> GetOwners() {
		foreach (var plugin in Application.LoadedPlugins)
			yield return plugin;
		foreach (var block in Application.ScreenHost.AvailableBlocks) {
			yield return block;
			foreach (var definition in block.ScreenDefinitions.Where(definition => definition.Settings != null))
				yield return definition;
		}
	}

	// The same actual owner references drive navigation, editing and persistence.
	private static SettingsObject GetSettings(object owner) {
		return owner switch {
			SharedUI.ApplicationScreenDefinition screen => screen.Settings,
			SharedUI.IApplicationBlock block => block.BlockSettings,
			IApplicationPlugin plugin => plugin.PluginSettings,
			_ => null
		};
	}

	// Display text remains independent of provider IDs and registration identity.
	private static string GetTitle(object owner) {
		return owner switch {
			SharedUI.ApplicationScreenDefinition screen => screen.Title,
			SharedUI.IApplicationBlock block => block.Name,
			IApplicationPlugin plugin => plugin.Name,
			_ => string.Empty
		};
	}

	// Automatic editing is opt-in; explicit editors may edit an otherwise unmarked settings class.
	private static bool CanEdit(object owner) {
		return GetSettings(owner) is { } settings && ((owner as ApplicationScreenDefinition)?.SettingsEditor != null
			|| GetDeclaredEditorType(settings.GetType()) != null || Attribute.IsDefined(settings.GetType(), typeof(UserEditableAttribute), true));
	}

	// Ordinary component-model editor metadata belongs to the settings class.
	private static Type GetDeclaredEditorType(Type settingsType) {
		var editor = TypeDescriptor.GetAttributes(settingsType).OfType<EditorAttribute>()
			.FirstOrDefault(attribute => Type.GetType(attribute.EditorBaseTypeName, false) == typeof(IApplicationSettingsEditor));
		return editor == null ? null : Type.GetType(editor.EditorTypeName, true);
	}

	// The selected owner and unapplied marker are presentation state only.
	private string PageClass(object owner) {
		return ReferenceEquals(owner, _selected) ? "sphere10-options-page-link selected" : "sphere10-options-page-link";
	}
}
