// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.ScheduledTasks;

namespace Tools;

public static partial class Windows {

	/// <summary>
	/// Reads and changes this computer's scheduled tasks through Task Scheduler: lists tasks with their full definitions, registers
	/// a new or changed definition, enables, disables, runs, stops and deletes a task. Every change takes the task as it was read
	/// and is refused when the task has changed since, so nothing is overwritten unseen.
	/// </summary>
	/// <remarks>Each operation connects to Task Scheduler on the calling worker thread and releases it before returning. A task the
	/// caller's account may not read is left out of a listing; one it may not change makes the change fail with Windows' refusal.</remarks>
	public static class ScheduledTasks {
		public const string RootFolder = @"\";
		private const int Create = 2;
		private const int Update = 4;
		private const int ChangedMode = unchecked((int)0x80010106);

		/// <summary>Reads the tasks of a folder and, by default, of every folder beneath it.</summary>
		public static WindowsScheduledTask[] ReadTasks(string folder = RootFolder, bool recursive = true) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrWhitespace(folder, nameof(folder));
			return Execute(service => {
				var tasks = new List<WindowsScheduledTask>();
				using var root = service.GetFolder(folder);
				ReadFolder(root, recursive, tasks);
				return tasks.ToArray();
			});
		}

		public static Task<WindowsScheduledTask[]> ReadTasksAsync(string folder = RootFolder, bool recursive = true, CancellationToken cancellationToken = default)
			=> Task.Run(() => ReadTasks(folder, recursive), cancellationToken);

		/// <summary>Reads one task by its full path, or returns null when no such task exists.</summary>
		public static WindowsScheduledTask ReadTask(string path) {
			Debug.AssertNotMainThread();
			var (folder, name) = Split(path);
			return Execute(service => {
				using var parent = OpenFolder(service, folder);
				using var task = parent == null ? null : OpenTask(parent, name);
				return task == null ? null : Describe(task);
			});
		}

		public static Task<WindowsScheduledTask> ReadTaskAsync(string path, CancellationToken cancellationToken = default) => Task.Run(() => ReadTask(path), cancellationToken);

		/// <summary>Registers a definition at a path and returns the task as Task Scheduler now holds it. With <paramref name="expected"/>
		/// it replaces that task, provided it is unchanged; without, it creates a task and fails when one already exists there.</summary>
		/// <remarks>A task that signs in with a stored password cannot be re-registered here, because Windows requires the password again.</remarks>
		public static WindowsScheduledTask RegisterTask(string path, WindowsTaskDefinition definition, WindowsScheduledTask expected = null) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(definition, nameof(definition));
			Guard.Argument(expected == null || string.Equals(expected.Path, path, StringComparison.OrdinalIgnoreCase), nameof(expected), "The expected task is at another path.");
			var validation = definition.Validate();
			Guard.Argument(validation.IsSuccess, nameof(definition), string.Join(" ", validation.ErrorMessages));
			Guard.Argument(!string.Equals(definition.LogonType, "Password", StringComparison.OrdinalIgnoreCase), nameof(definition),
				"This task signs in with a stored password, which Windows requires again to change it. Change it in Task Scheduler.");
			var (folder, name) = Split(path);
			var xml = definition.ToXml();
			return Execute(service => {
				using var parent = service.GetFolder(folder);
				if (expected != null)
					EnsureUnchanged(parent, name, expected);
				using var registered = parent.RegisterTask(name, xml, expected == null ? Create : Update, LogonTypeOf(definition));
				Guard.Ensure(registered != null, $"Task Scheduler did not register '{path}'.");
				return Describe(registered);
			});
		}

		public static Task<WindowsScheduledTask> RegisterTaskAsync(string path, WindowsTaskDefinition definition, WindowsScheduledTask expected = null, CancellationToken cancellationToken = default)
			=> Task.Run(() => RegisterTask(path, definition, expected), cancellationToken);

		/// <summary>Deletes a task, provided it is unchanged since it was read.</summary>
		public static void DeleteTask(WindowsScheduledTask expected) => Change(expected, (folder, _) => folder.DeleteTask(expected.Name));

		public static Task DeleteTaskAsync(WindowsScheduledTask expected, CancellationToken cancellationToken = default) => Task.Run(() => DeleteTask(expected), cancellationToken);

		/// <summary>Enables or disables a task, provided it is unchanged since it was read, and returns it as it now stands.</summary>
		public static WindowsScheduledTask SetEnabled(WindowsScheduledTask expected, bool enabled) {
			WindowsScheduledTask result = null;
			Change(expected, (folder, task) => {
				task.Enabled = enabled;
				// The task object keeps the definition it was opened with, so the changed one is read from the folder again.
				using var changed = OpenTask(folder, expected.Name);
				Guard.Ensure(changed != null, $"The task '{expected.Path}' no longer exists.");
				result = Describe(changed);
			});
			return result;
		}

		public static Task<WindowsScheduledTask> SetEnabledAsync(WindowsScheduledTask expected, bool enabled, CancellationToken cancellationToken = default)
			=> Task.Run(() => SetEnabled(expected, enabled), cancellationToken);

		/// <summary>Asks Task Scheduler to start a task now, provided it is unchanged since it was read. Windows owns the instance it starts.</summary>
		public static void RunTask(WindowsScheduledTask expected) => Change(expected, (_, task) => task.Run());

		public static Task RunTaskAsync(WindowsScheduledTask expected, CancellationToken cancellationToken = default) => Task.Run(() => RunTask(expected), cancellationToken);

		/// <summary>Asks Task Scheduler to stop every running instance of a task.</summary>
		public static void StopTask(WindowsScheduledTask task) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(task, nameof(task));
			Execute(service => {
				using var parent = service.GetFolder(task.Folder);
				using var registered = OpenTask(parent, task.Name);
				Guard.Ensure(registered != null, $"The task '{task.Path}' no longer exists.");
				registered.Stop();
				return true;
			});
		}

		public static Task StopTaskAsync(WindowsScheduledTask task, CancellationToken cancellationToken = default) => Task.Run(() => StopTask(task), cancellationToken);

		private static void Change(WindowsScheduledTask expected, Action<TaskFolderComObject, RegisteredTaskComObject> change) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(expected, nameof(expected));
			Execute(service => {
				using var parent = service.GetFolder(expected.Folder);
				using var task = EnsureUnchanged(parent, expected.Name, expected);
				change(parent, task);
				return true;
			});
		}

		// The definition is the task's identity for a change: the same path with other XML is a task someone has since changed.
		private static RegisteredTaskComObject EnsureUnchanged(TaskFolderComObject folder, string name, WindowsScheduledTask expected) {
			var task = OpenTask(folder, name);
			Guard.Ensure(task != null, $"The task '{expected.Path}' no longer exists.");
			if (!string.Equals(task.Xml, expected.Xml, StringComparison.Ordinal)) {
				task.Dispose();
				throw new InvalidOperationException($"The task '{expected.Path}' has changed since it was read. Read it again before changing it.");
			}
			return task;
		}

		private static void ReadFolder(TaskFolderComObject folder, bool recursive, List<WindowsScheduledTask> tasks) {
			using (var collection = folder.GetTasks()) {
				var count = collection?.Count ?? 0;
				for (var index = 1; index <= count; index++) {
					using var task = collection.GetItem(index);
					if (task != null)
						tasks.Add(Describe(task));
				}
			}
			if (!recursive)
				return;
			using var folders = folder.GetFolders();
			var folderCount = folders?.Count ?? 0;
			for (var index = 1; index <= folderCount; index++) {
				using var child = folders.GetItem(index);
				if (child != null)
					ReadFolder(child, true, tasks);
			}
		}

		private static WindowsScheduledTask Describe(RegisteredTaskComObject task) {
			var path = task.Path ?? RootFolder;
			var (folder, name) = Split(path);
			// A task's definition can be withheld from an account that may still see the task; the rest is reported with the reason.
			string xml = null;
			WindowsTaskDefinition definition = null;
			Tools.Exceptions.ExecuteCapturingException(() => {
				xml = task.Xml;
				definition = WindowsTaskDefinition.Parse(xml);
			}, out var error);
			return new WindowsScheduledTask(path, name, folder, Enum.IsDefined((WindowsTaskState)task.State) ? (WindowsTaskState)task.State : WindowsTaskState.Unknown, task.Enabled,
				task.LastRunTime, task.NextRunTime, task.LastTaskResult, task.NumberOfMissedRuns, xml, definition, error?.Message);
		}

		private static TaskFolderComObject OpenFolder(TaskServiceComObject service, string folder) {
			TaskFolderComObject opened = null;
			Tools.Exceptions.ExecuteCapturingException<COMException>(() => opened = service.GetFolder(folder), out _, error => IsNotFound(error));
			return opened;
		}

		private static RegisteredTaskComObject OpenTask(TaskFolderComObject folder, string name) {
			RegisteredTaskComObject opened = null;
			Tools.Exceptions.ExecuteCapturingException<COMException>(() => opened = folder.GetTask(name), out _, error => IsNotFound(error));
			return opened;
		}

		// File-not-found and path-not-found, as Task Scheduler reports a missing task or folder.
		private static bool IsNotFound(COMException error) => error.HResult is unchecked((int)0x80070002) or unchecked((int)0x80070003);

		private static (string Folder, string Name) Split(string path) {
			Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
			Guard.Argument(path.StartsWith('\\') && !path.EndsWith('\\'), nameof(path), @"A task path starts at the root folder, such as \Folder\Task Name.");
			var separator = path.LastIndexOf('\\');
			return (separator == 0 ? RootFolder : path[..separator], path[(separator + 1)..]);
		}

		// Task Scheduler numbers the logon types; the definition names its own.
		private static int LogonTypeOf(WindowsTaskDefinition definition) => definition.LogonType?.ToUpperInvariant() switch {
			"S4U" => 2,
			"INTERACTIVETOKEN" => 3,
			"GROUP" => 4,
			"SERVICEACCOUNT" => 5,
			"INTERACTIVETOKENORPASSWORD" => 6,
			// A definition that names a service account without a logon type runs as that service.
			_ => definition.RunAs is "S-1-5-18" or "S-1-5-19" or "S-1-5-20" ? 5 : 3
		};

		// Connects on the calling thread for one operation. A thread already in a single-threaded apartment is used as it is.
		private static T Execute<T>(Func<TaskServiceComObject, T> operation) {
			var initialized = WinAPI.OLE32.CoInitializeEx(IntPtr.Zero, 0);
			if (initialized != ChangedMode)
				Marshal.ThrowExceptionForHR(initialized);
			try {
				using var service = COM.CreateTaskService();
				service.Connect();
				return operation(service);
			} finally {
				if (initialized >= 0)
					WinAPI.OLE32.CoUninitialize();
			}
		}
	}
}
