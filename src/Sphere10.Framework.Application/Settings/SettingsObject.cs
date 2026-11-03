// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Sphere10.Framework.Application;

/// <summary>Owns typed settings values, default-value restoration and persistence through their attached provider.</summary>
[XmlRoot]
[Serializable]
public abstract class SettingsObject : ObjectWithDefaultValues {

	/// <summary>Signals that these settings have been successfully saved.</summary>
	[field: NonSerialized]
	public event EventHandlerEx OnChanged;

	/// <summary>The identity of this settings record within its declared type.</summary>
	[Browsable(false)]
	[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
	public object ID { get; set; } = null;

	/// <summary>The existing provider used to load, save or delete this record.</summary>
	[Browsable(false)]
	[XmlIgnore]
	[JsonIgnore]
	public ISettingsProvider Provider { get; set; }

	/// <summary>Reloads this settings instance through its attached provider.</summary>
	public virtual void Load() {
		CheckProvider();
		Provider.ReloadSetting(this);
	}

	/// <summary>Saves these settings and notifies their consumers after persistence succeeds.</summary>
	public virtual void Save() {
		CheckProvider();
		Provider.SaveSetting(this);
		NotifyChanged();
	}

	/// <summary>Awaits persistence before notifying consumers of these settings.</summary>
	public virtual async Task SaveAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		CheckProvider();
		await Provider.SaveSettingAsync(this, cancellationToken);
		NotifyChanged();
	}

	/// <summary>Deletes this settings record through its attached provider.</summary>
	public virtual void Delete() {
		CheckProvider();
		Provider.DeleteSetting(this);
	}

	/// <summary>Checks settings-specific constraints before a caller accepts an edit.</summary>
	public virtual Result Validate() {
		return Result.Success;
	}

	// Publish changes on the actual shared settings instance after its provider accepts the save.
	internal void NotifyChanged() {
		OnChanged?.Invoke();
	}

	// Settings values must be attached to their persistence owner before storage operations.
	private void CheckProvider() {
		if (Provider == null)
			throw new SoftwareException("Setting cannot be saved as it is not registered with a settings provider.");
	}
}
