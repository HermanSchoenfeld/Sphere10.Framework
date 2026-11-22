// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>One task as Task Scheduler holds it at the time it was read: where it is, its state and history, and its full definition.
/// Pass it back to change, enable, run or delete the task, so that a task changed meanwhile is never overwritten.</summary>
/// <param name="Path">The task's full path, such as \Microsoft\Windows\Defrag\ScheduledDefrag.</param>
/// <param name="Name">The task's name, the last part of its path.</param>
/// <param name="Folder">The folder that holds the task; \ is the root.</param>
/// <param name="State">The state Task Scheduler reported.</param>
/// <param name="Enabled">Whether the task may run.</param>
/// <param name="LastRunTime">When the task last ran, or null when it never has.</param>
/// <param name="NextRunTime">When the task will next run by its triggers, or null when no trigger is due.</param>
/// <param name="LastTaskResult">The result code of the last run; zero is success.</param>
/// <param name="MissedRuns">How many scheduled runs were missed.</param>
/// <param name="Xml">The task's definition exactly as Task Scheduler returned it.</param>
/// <param name="Definition">The parsed definition, or null when <paramref name="DefinitionError"/> says why it could not be read.</param>
/// <param name="DefinitionError">Why the definition could not be read, or null.</param>
public sealed record WindowsScheduledTask(string Path, string Name, string Folder, WindowsTaskState State, bool Enabled, DateTime? LastRunTime, DateTime? NextRunTime, int LastTaskResult,
	int MissedRuns, string Xml, WindowsTaskDefinition Definition, string DefinitionError);
