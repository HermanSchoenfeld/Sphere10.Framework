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
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Enumerates an application's declared screen and root preferences without constructing operational screens.</summary>
public sealed class ApplicationSettingsCatalog {
	private readonly object _initializationLock = new();
	private readonly ApplicationSettingsProfile[] _profiles;
	private Task _initialization;

	/// <summary>Projects existing block definitions and root contributions; the selector supplies a platform's stable block identity.</summary>
	public ApplicationSettingsCatalog(string applicationKey, IApplicationSettingsStore store, IEnumerable<IApplicationBlock> blocks,
		IEnumerable<ApplicationSettingsDefinition> applicationSettings = null, ILogger logger = null, Func<IApplicationBlock, string> blockKeySelector = null) {
		Guard.ArgumentNotNullOrEmpty(applicationKey, nameof(applicationKey));
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		ApplicationKey = applicationKey;
		blockKeySelector ??= block => block.Name;
		var profiles = new List<ApplicationSettingsProfile>();
		foreach (var root in applicationSettings ?? Array.Empty<ApplicationSettingsDefinition>()) {
			Guard.ArgumentNotNull(root, nameof(applicationSettings));
			profiles.Add(new ApplicationSettingsProfile(Key(applicationKey, "application", root.StableKey), null, null, root, store, logger));
		}
		foreach (var block in blocks.Distinct<IApplicationBlock>(ReferenceEqualityComparer.Instance)) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			Tools.UI.ValidateScreenRegistrations(block.ScreenDefinitions);
			foreach (var definition in block.ScreenDefinitions) {
				var profileKey = definition.Settings == null ? null : Key(applicationKey, "screen", block.Source?.Name ?? "standalone",
					blockKeySelector(block), Tools.UI.GetScreenRegistrationKey(block, definition));
				profiles.Add(new ApplicationSettingsProfile(profileKey, block, definition, null, store, logger));
			}
		}
		Guard.Argument(!profiles.Where(profile => profile.HasOptions).GroupBy(profile => profile.ProfileKey, StringComparer.Ordinal).Any(group => group.Count() > 1),
			nameof(blocks), "Settings profile keys must be unique. Configure stable application, block and registration keys.");
		_profiles = profiles.ToArray();
	}

	/// <summary>The stable application namespace, independent of its displayed caption.</summary>
	public string ApplicationKey { get; }

	/// <summary>Whether restoration has completed before screen data work is allowed.</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>Root and screen pages, including unopened and no-options registrations.</summary>
	public ApplicationSettingsProfile[] Profiles => Tools.Array.Clone(_profiles);

	/// <summary>Restores declared values once; cancelled or failed startup can retry without creating preference files.</summary>
	public Task InitializeAsync(CancellationToken cancellationToken = default) {
		lock (_initializationLock) {
			if (IsInitialized)
				return Task.CompletedTask;
			if (_initialization == null || _initialization.IsCompleted)
				_initialization = InitializeProfilesAsync(cancellationToken);
			return _initialization.WaitAsync(cancellationToken);
		}
	}

	/// <summary>Awaits queued accepted writes without acquiring UI state or starting new saves.</summary>
	public async Task FlushAsync(CancellationToken cancellationToken = default) {
		await Task.WhenAll(_profiles.Select(profile => profile.FlushAsync(cancellationToken))).ConfigureAwait(false);
	}

	/// <summary>Resolves a source moniker inside its owning block and returns that graph's profile.</summary>
	public ApplicationSettingsProfile GetProfile(IApplicationBlock block, ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(block, nameof(block));
		var owned = _profiles.Where(profile => ReferenceEquals(profile.Block, block)).ToArray();
		var definition = Tools.UI.ResolveScreenRegistration(owned.Select(profile => profile.ScreenDefinition), registration);
		return owned.Single(profile => ReferenceEquals(profile.ScreenDefinition, definition));
	}

	/// <summary>Finds one application-root contribution by its stable local key.</summary>
	public ApplicationSettingsProfile GetProfile(string applicationSettingsKey) {
		Guard.ArgumentNotNullOrEmpty(applicationSettingsKey, nameof(applicationSettingsKey));
		var profile = _profiles.SingleOrDefault(profile => profile.ApplicationDefinition?.StableKey == applicationSettingsKey);
		Guard.Argument(profile != null, nameof(applicationSettingsKey), "The application settings key is not registered.");
		return profile;
	}

	// Startup enumerates metadata only; individual profiles own optional-load errors and their validated defaults.
	private async Task InitializeProfilesAsync(CancellationToken token) {
		foreach (var profile in _profiles) {
			token.ThrowIfCancellationRequested();
			await profile.InitializeAsync(token).ConfigureAwait(false);
		}
		IsInitialized = true;
	}

	// Escaping each identity segment prevents different application/block keys from colliding through separators.
	private static string Key(params string[] segments) {
		Guard.Argument(segments.All(segment => !string.IsNullOrWhiteSpace(segment)), nameof(segments), "Persistent settings identities cannot contain an empty segment.");
		return string.Join("/", segments.Select(Uri.EscapeDataString));
	}
}
