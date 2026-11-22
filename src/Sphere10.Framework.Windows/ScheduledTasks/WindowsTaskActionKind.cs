// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>What a scheduled task does when it runs.</summary>
public enum WindowsTaskActionKind {
	/// <summary>Starts a program.</summary>
	Execute,

	/// <summary>Calls a registered COM handler.</summary>
	ComHandler,

	/// <summary>An action this library does not model. It is kept exactly as Windows defines it.</summary>
	Other
}
