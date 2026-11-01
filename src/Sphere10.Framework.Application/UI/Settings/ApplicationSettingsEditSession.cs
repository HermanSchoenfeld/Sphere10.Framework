// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>One detached Options page whose provisional edits never mutate accepted values or provider caches.</summary>
public sealed class ApplicationSettingsEditSession {
	// Start from a detached accepted snapshot; merely opening a page does not persist anything.
	internal ApplicationSettingsEditSession(ApplicationSettingsProfile profile) {
		Profile = profile;
		Cancel();
	}

	/// <summary>The profile whose values are being edited.</summary>
	public ApplicationSettingsProfile Profile { get; }

	/// <summary>Mutable detached scalar, nested and collection values for the editor.</summary>
	public object Draft { get; private set; }

	/// <summary>The last accepted revision rebased into this session.</summary>
	public long AcceptedRevision { get; private set; }

	/// <summary>Checks the settings object's validation or standard annotations before persistence.</summary>
	public Result Validate() {
		return Profile.Validate(Draft);
	}

	/// <summary>Restores provisional defaults from the authoritative factory without writing or losing migration identity.</summary>
	public void Reset() {
		Draft = Profile.CreateDefaults();
	}

	/// <summary>Discards unapplied edits and rebases on the latest successfully accepted values.</summary>
	public void Cancel() {
		Draft = Profile.GetValues(out var revision);
		AcceptedRevision = revision;
	}

	/// <summary>Validates and snapshots now, then persists before accepting and rebasing the page.</summary>
	public async Task<Result> ApplyAsync(CancellationToken cancellationToken = default) {
		var values = ApplicationSettingsValueCloner.Copy(Draft);
		var validation = Profile.Validate(values);
		if (validation.IsFailure)
			return validation;
		await Profile.ApplyAsync(values, this, cancellationToken);
		Cancel();
		return validation;
	}
}
