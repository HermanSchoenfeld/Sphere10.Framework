// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application;

/// <summary>Rejects settings access while retaining the standard synchronous provider direction.</summary>
public class ProhibitedSettingsProvider : SyncSettingsProviderBase {
	/// <inheritdoc />
	public override event EventHandlerEx Changed {
		add {
			throw new NotSupportedException(_exceptionMessage);
		}
		remove {
			throw new NotSupportedException(_exceptionMessage);
		}
	}

	private const string _exceptionMessage = "Settings are prohibited in this application";

	/// <inheritdoc />
	public override bool AutoSaveNewSettings {
		get {
			throw new NotSupportedException(_exceptionMessage);
		}
	}

	/// <inheritdoc />
	public override bool EncryptSettings {
		get {
			throw new NotSupportedException(_exceptionMessage);
		}
	}

	/// <inheritdoc />
	public override SettingsObject NewSetting(Type settingsObjectType, object id = null) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override bool ContainsSetting(Type settingsObjectType, object id = null) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(Type settingsObjectType, object id = null) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(SettingsObject defaults) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override void SaveSetting(SettingsObject settings) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override void DeleteSetting(SettingsObject settings) {
		throw new NotSupportedException(_exceptionMessage);
	}

	/// <inheritdoc />
	public override void ClearSettings() {
		throw new NotSupportedException(_exceptionMessage);
	}

	// No persistence primitive is available when the host prohibits settings.
	protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
		throw new NotSupportedException(_exceptionMessage);
	}

	// Reject writes through the primitive contract as well as the public operation.
	protected override void SaveInternal(SettingsObject settings) {
		throw new NotSupportedException(_exceptionMessage);
	}
}
