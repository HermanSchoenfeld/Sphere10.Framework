// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows.Files;

/// <summary>A named file stream and the edit baseline owned by one <see cref="AlternateDataStreamDataSource"/>.</summary>
public sealed class AlternateDataStreamEntry {
	[DisplayName("File"), Description("The primary file owning this named stream.")]
	public string FilePath { get; set; }

	[DisplayName("Stream"), Description("The named data stream, excluding the primary file contents.")]
	public string Name { get; set; } = string.Empty;

	[DisplayName("Size"), Description("The stream's current size in bytes.")]
	public long Length { get; set; }

	[Browsable(false)]
	public string Text { get; set; } = string.Empty;

	[Browsable(false), JsonIgnore]
	public AlternateDataStreamDataSource Owner { get; set; }

	[Browsable(false), JsonIgnore]
	public AlternateDataStreamEntry Origin { get; set; }

	/// <summary>The bytes the editor loaded; writes verify the stream still holds them.</summary>
	[Browsable(false), JsonIgnore]
	public byte[] Baseline { get; set; }

	/// <summary>Binary content the editor supplies instead of <see cref="Text"/>.</summary>
	[Browsable(false), JsonIgnore]
	public byte[] RawBytes { get; set; }

	[Browsable(false), JsonIgnore]
	public string ValidationError { get; set; }

	[Browsable(false), JsonIgnore]
	public bool HasUtf8Bom { get; set; }

	[Browsable(false), JsonIgnore]
	public bool IsPersisted { get; set; }

	[Browsable(false), JsonIgnore]
	public string OriginalName { get; set; }

	public AlternateDataStreamEntry Copy() => new() {
		FilePath = FilePath, Name = Name, Length = Length, Text = Text, Owner = Owner, Origin = Origin ?? this,
		Baseline = Baseline, RawBytes = RawBytes, HasUtf8Bom = HasUtf8Bom, IsPersisted = IsPersisted, OriginalName = OriginalName
	};
}
