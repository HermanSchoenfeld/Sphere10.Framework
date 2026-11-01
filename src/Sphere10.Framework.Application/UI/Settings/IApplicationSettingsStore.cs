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

/// <summary>Persists detached declared values in the owning application and user scope.</summary>
/// <remarks>Implementations serialize and perform storage IO off the model's owner thread. They do not retain or edit live UI values.</remarks>
public interface IApplicationSettingsStore {
	/// <summary>Loads known typed values and migration presence, or returns null when no profile exists.</summary>
	Task<ApplicationSettingsStoredProfile> LoadAsync(string profileKey, object defaults, bool hasExplicitStorageId = false, object storageId = null, CancellationToken cancellationToken = default);

	/// <summary>Persists an exclusively owned snapshot before it can become the accepted profile revision.</summary>
	Task SaveAsync(string profileKey, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId = false, object storageId = null, CancellationToken cancellationToken = default);
}
