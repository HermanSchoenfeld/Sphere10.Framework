// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows;

/// <summary>A raw environment value and its edit baseline, owned by one <see cref="EnvironmentVariableDataSource"/>.</summary>
public sealed class EnvironmentVariableEntry {
	[DisplayName("Variable"), Description("The exact Windows environment variable name.")]
	public string Name { get; set; } = string.Empty;

	[DisplayName("Value"), Description("The unexpanded value; references such as %TEMP% are retained literally.")]
	public string Value { get; set; } = string.Empty;

	[DisplayName("Scope"), Description("Process changes affect System Expert; User and Machine values are persistent.")]
	public EnvironmentVariableTarget Scope { get; set; } = EnvironmentVariableTarget.Process;

	[DisplayName("Availability"), Description("An explanation when Windows could not supply an editable value."), ReadOnly(true)]
	public string Availability { get; internal set; } = string.Empty;

	[Browsable(false), JsonIgnore]
	public EnvironmentVariableDataSourceBase Owner { get; set; }

	[Browsable(false), JsonIgnore]
	public EnvironmentVariableEntry Origin { get; set; }

	[Browsable(false), JsonIgnore]
	public string OriginalName { get; set; }

	[Browsable(false), JsonIgnore]
	public string OriginalValue { get; set; }

	[Browsable(false), JsonIgnore]
	public EnvironmentVariableTarget OriginalScope { get; set; }

	[Browsable(false), JsonIgnore]
	public bool IsPersisted { get; set; }

	public EnvironmentVariableEntry Copy() => new() {
		Name = Name, Value = Value, Scope = Scope, Availability = Availability,
		Owner = Owner, Origin = Origin ?? this, OriginalName = OriginalName, OriginalValue = OriginalValue,
		OriginalScope = OriginalScope, IsPersisted = IsPersisted
	};

	public void Accept(EnvironmentVariableEntry changes) {
		Name = OriginalName = changes.Name;
		Value = OriginalValue = changes.Value;
		Scope = OriginalScope = changes.Scope;
		Availability = string.Empty;
		IsPersisted = true;
	}
}
