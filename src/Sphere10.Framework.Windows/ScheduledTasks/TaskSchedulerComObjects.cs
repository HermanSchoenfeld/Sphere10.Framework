// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>Owns one Task Scheduler automation interface. Members are called by name through <see cref="DispatchComObject"/>, which
/// supports Native AOT. Use and dispose each wrapper on the thread that created it.</summary>
internal abstract class TaskSchedulerComObject : DispatchComObject {
	protected TaskSchedulerComObject(IntPtr dispatch)
		: base(dispatch) {
	}

	protected override string Subject => "Task Scheduler";

	// Automation returns dates as OLE dates; the scheduler's "never" is the OLE zero date, 30 December 1899.
	protected DateTime? GetDate(string member) {
		var value = GetValue(member);
		var date = value switch {
			DateTime time => time,
			double oleDate => DateTime.FromOADate(oleDate),
			_ => (DateTime?)null
		};
		return date.HasValue && date.Value.Year > 1900 ? date : null;
	}

	protected int GetInteger(string member) => Convert.ToInt32(GetValue(member) ?? 0);
}

/// <summary>The Task Scheduler service connected to this computer, created through <c>Tools.Windows.COM</c>.</summary>
internal sealed class TaskServiceComObject(IntPtr dispatch) : TaskSchedulerComObject(dispatch) {
	/// <summary>Connects to this computer's scheduler as the calling user. Nothing else works before this.</summary>
	public void Connect() => Call("Connect");

	/// <summary>Opens a task folder, which the caller disposes. It fails when the folder does not exist.</summary>
	public TaskFolderComObject GetFolder(string path) {
		var folder = GetObject("GetFolder", pointer => new TaskFolderComObject(pointer), path);
		Guard.Ensure(folder != null, $"Task Scheduler returned no folder for '{path}'.");
		return folder;
	}
}

/// <summary>One folder of registered tasks.</summary>
internal sealed class TaskFolderComObject(IntPtr dispatch) : TaskSchedulerComObject(dispatch) {
	private const int IncludeHidden = 1;

	public string Path => GetValue("Path") as string;

	/// <summary>The folder's direct sub-folders, which the caller disposes.</summary>
	public TaskCollectionComObject<TaskFolderComObject> GetFolders()
		=> GetObject("GetFolders", pointer => new TaskCollectionComObject<TaskFolderComObject>(pointer, item => new TaskFolderComObject(item)), 0);

	/// <summary>The folder's own tasks, hidden ones included, which the caller disposes.</summary>
	public TaskCollectionComObject<RegisteredTaskComObject> GetTasks()
		=> GetObject("GetTasks", pointer => new TaskCollectionComObject<RegisteredTaskComObject>(pointer, item => new RegisteredTaskComObject(item)), IncludeHidden);

	/// <summary>Opens one task by its name within this folder, which the caller disposes.</summary>
	public RegisteredTaskComObject GetTask(string name) => GetObject("GetTask", pointer => new RegisteredTaskComObject(pointer), name);

	/// <summary>Registers a task from its XML and returns it, which the caller disposes.</summary>
	/// <param name="flags">2 creates, 4 updates, 6 does either.</param>
	/// <param name="logonType">How the task signs in, as Task Scheduler numbers it.</param>
	public RegisteredTaskComObject RegisterTask(string name, string xml, int flags, int logonType)
		=> GetObject("RegisterTask", pointer => new RegisteredTaskComObject(pointer), name, xml, flags, null, null, logonType, null);

	public void DeleteTask(string name) => Call("DeleteTask", name, 0);
}

/// <summary>A collection of folders or tasks at the time of the query. Its items are numbered from one.</summary>
internal sealed class TaskCollectionComObject<T>(IntPtr dispatch, Func<IntPtr, T> wrap) : TaskSchedulerComObject(dispatch) where T : TaskSchedulerComObject {
	public int Count => GetInteger("Count");

	/// <summary>Returns one item, which the caller disposes.</summary>
	public T GetItem(int index) => GetObject("Item", wrap, index);
}

/// <summary>One registered task.</summary>
internal sealed class RegisteredTaskComObject(IntPtr dispatch) : TaskSchedulerComObject(dispatch) {
	public string Name => GetValue("Name") as string;

	public string Path => GetValue("Path") as string;

	public int State => GetInteger("State");

	public bool Enabled {
		get => GetValue("Enabled") is true;
		set => SetValue("Enabled", value);
	}

	public DateTime? LastRunTime => GetDate("LastRunTime");

	public DateTime? NextRunTime => GetDate("NextRunTime");

	public int LastTaskResult => GetInteger("LastTaskResult");

	public int NumberOfMissedRuns => GetInteger("NumberOfMissedRuns");

	public string Xml => GetValue("Xml") as string;

	/// <summary>Asks Task Scheduler to start the task now. Windows owns the instance it starts.</summary>
	public void Run() => Call("Run", null);

	/// <summary>Asks Task Scheduler to stop every running instance of the task.</summary>
	public void Stop() => Call("Stop", 0);
}
