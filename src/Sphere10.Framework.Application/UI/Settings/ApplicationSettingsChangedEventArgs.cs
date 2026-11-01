// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>Identifies accepted edits or in-memory section initialization without persisting runtime ownership.</summary>
public sealed class ApplicationSettingsChangedEventArgs {
	private readonly object _initialization;
	private readonly Action<object, object> _copySection;

	// Origins coordinate live instances only; they never enter a persisted settings payload.
	internal ApplicationSettingsChangedEventArgs(long revision, object source, long sourceVersion,
		object initialization = null, Action<object, object> copySection = null) {
		Revision = revision;
		Source = source;
		SourceVersion = sourceVersion;
		_initialization = initialization == null ? null : ApplicationSettingsValueCloner.Copy(initialization);
		_copySection = copySection;
	}

	/// <summary>The accepted or initialized in-memory value revision.</summary>
	public long Revision { get; }

	/// <summary>A small producer token, an explicit edit session, or null for section initialization.</summary>
	public object Source { get; }

	/// <summary>The producer's local capture version; zero identifies Apply or section initialization.</summary>
	public long SourceVersion { get; }

	/// <summary>Whether this notification copies one initialized section without accepting other working edits.</summary>
	public bool IsSectionInitialization => _copySection != null;

	/// <summary>Copies only the initialized section into consumer-owned working values, with an isolated source per consumer.</summary>
	public void ApplyInitialization(object target) {
		Guard.ArgumentNotNull(target, nameof(target));
		Guard.Ensure(IsSectionInitialization, "This change is not a section initialization.");
		_copySection(ApplicationSettingsValueCloner.Copy(_initialization), target);
	}
}
