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

/// <summary>Captures accepted dirty UI preferences before an approved close or shutdown releases their owner.</summary>
public interface IApplicationSettingsParticipant {
	/// <summary>Captures values on their owner thread and awaits persistence; clean participants perform no writes.</summary>
	Task SaveSettingsAsync(CancellationToken cancellationToken = default);
}
