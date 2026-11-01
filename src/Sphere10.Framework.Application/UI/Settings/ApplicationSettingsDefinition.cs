// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>Contributes application-root preferences to the same catalog as screen registrations.</summary>
public class ApplicationSettingsDefinition {
	/// <summary>Names one root preference profile independently of any screen or navigation command.</summary>
	public ApplicationSettingsDefinition(string stableKey, string title, object settings, Func<object> settingsFactory = null, Type settingsEditor = null) {
		Guard.ArgumentNotNullOrEmpty(stableKey, nameof(stableKey));
		Guard.ArgumentNotNull(settings, nameof(settings));
		StableKey = stableKey;
		Title = title ?? stableKey;
		Settings = ApplicationSettingsValueCloner.Copy(settings);
		var defaults = settingsFactory == null ? ApplicationSettingsValueCloner.Copy(settings) : null;
		SettingsFactory = settingsFactory ?? (() => ApplicationSettingsValueCloner.Copy(defaults));
		SettingsEditor = settingsEditor;
	}

	/// <summary>A caption-independent identity within the owning application.</summary>
	public string StableKey { get; }

	/// <summary>The title shown in Options navigation.</summary>
	public string Title { get; }

	/// <summary>The ordinary object edited by this application-root Options page.</summary>
	public object Settings { get; }

	/// <summary>Creates fresh defaults; an omitted factory copies the supplied object's initial values.</summary>
	public Func<object> SettingsFactory { get; }

	/// <summary>An optional platform editor type that edits the supplied settings object directly.</summary>
	public Type SettingsEditor { get; }

	/// <summary>Whether this contribution retains an explicitly supplied legacy provider ID, including null.</summary>
	public bool HasExplicitStorageId { get; private set; }

	/// <summary>The original provider ID when HasExplicitStorageId is true.</summary>
	public object StorageId { get; private set; }

	/// <summary>Retains an existing record identity while preserving any platform editor metadata.</summary>
	public virtual ApplicationSettingsDefinition WithStorageId(object storageId) {
		var definition = (ApplicationSettingsDefinition)MemberwiseClone();
		definition.HasExplicitStorageId = true;
		definition.StorageId = storageId;
		return definition;
	}
}
