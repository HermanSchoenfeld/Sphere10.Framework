// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>One action of a scheduled task.</summary>
/// <param name="Kind">Whether the action starts a program, calls a COM handler or is of an unmodelled kind.</param>
/// <param name="Command">The program an Execute action starts, as Windows stores it, unexpanded.</param>
/// <param name="Arguments">The program's arguments, or null.</param>
/// <param name="WorkingDirectory">The folder the program starts in, or null.</param>
/// <param name="ClassId">The class identifier of a COM handler action, or null.</param>
public sealed record WindowsTaskAction(WindowsTaskActionKind Kind, string Command = null, string Arguments = null, string WorkingDirectory = null, string ClassId = null) {
	/// <summary>The action in words, such as the command line it runs.</summary>
	public string Describe() => Kind switch {
		WindowsTaskActionKind.Execute => string.IsNullOrEmpty(Arguments) ? Command : Command + " " + Arguments,
		WindowsTaskActionKind.ComHandler => "COM Handler " + ClassId,
		_ => "Other Action"
	};
}
