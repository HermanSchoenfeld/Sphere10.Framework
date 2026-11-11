// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Plain selected output values and the remembered enabled choice, without live output or storage ownership.</summary>
public sealed class LogOutputSelectorPreferences : SettingsObject {
	/// <summary>Indicates that an output was explicitly configured; constructing defaults never opens it.</summary>
	[DefaultValue(false)]
	[Browsable(false)]
	public bool HasConfiguration { get; set; }

	/// <summary>The remembered enabled choice; applying values does not activate an output.</summary>
	[DefaultValue(false)]
	[DisplayName("Enabled")]
	public bool Enabled { get; set; }

	/// <summary>The selected output's plain options, including Remember After Close.</summary>
	public LogOutputConfiguration Configuration { get; set; } = new();

	/// <summary>Restores disabled output and its configuration defaults without opening any output resource.</summary>
	public override void RestoreDefaultValues() {
		base.RestoreDefaultValues();
		Configuration ??= new LogOutputConfiguration();
		Tools.Object.SetDefaultValues(Configuration);
	}

	/// <summary>Checks value constraints without testing directories, opening files or creating logger resources.</summary>
	public override Result Validate() {
		var result = new Result();
		if (!HasConfiguration) {
			if (Enabled)
				result.AddError("Configure a logging output before enabling remembered logging.");
			return result;
		}
		if (Configuration == null) {
			result.AddError("A selected logging output requires configuration.");
			return result;
		}
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => Configuration.ValidateOptionsAndNormalize(), out var error);
		if (error != null)
			result.AddError(error.Message);
		return result;
	}

	/// <summary>Detaches the nested configuration so callers and background persistence cannot mutate live control values.</summary>
	public LogOutputSelectorPreferences Clone() {
		return new LogOutputSelectorPreferences {
			HasConfiguration = HasConfiguration,
			Enabled = Enabled,
			Configuration = Configuration?.Clone()
		};
	}

	/// <summary>Transient output choices remain available in this session without becoming remembered configuration.</summary>
	public bool ShouldSerializeHasConfiguration() => Configuration?.RememberAfterClose == true;

	/// <summary>Persists the enabled choice only when remembering this output was explicitly requested.</summary>
	public bool ShouldSerializeEnabled() => Configuration?.RememberAfterClose == true;

	/// <summary>Omits transient output paths and options from the owning root's persisted values.</summary>
	public bool ShouldSerializeConfiguration() => Configuration?.RememberAfterClose == true;
}
