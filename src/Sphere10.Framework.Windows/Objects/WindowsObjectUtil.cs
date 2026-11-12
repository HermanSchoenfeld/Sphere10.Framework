// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Read-only NT object-directory, symbolic-link and handle-name queries; no object is changed or followed automatically.</summary>
	public static class Objects {
		public const int MaximumDirectoryCharacters = 2 * 1024 * 1024;

		/// <summary>Describes known standard and type-specific rights, retaining every undecoded bit as hexadecimal.</summary>
		public static string DecodeAccessMask(string objectType, uint mask) {
			var rights = new List<(uint Mask, string Name)> {
				(0x00010000, "Delete"), (0x00020000, "Read Control"), (0x00040000, "Write DACL"), (0x00080000, "Write Owner"),
				(0x00100000, "Synchronize"), (0x01000000, "Access System Security"), (0x02000000, "Maximum Allowed"),
				(0x10000000, "Generic All"), (0x20000000, "Generic Execute"), (0x40000000, "Generic Write"), (0x80000000, "Generic Read")
			};
			var specific = objectType switch {
				"File" => new[] { "Read Data / List Directory", "Write Data / Add File", "Append Data / Add Subdirectory", "Read EA", "Write EA", "Execute / Traverse", "Delete Child", "Read Attributes", "Write Attributes" },
				"Key" => new[] { "Query Value", "Set Value", "Create Subkey", "Enumerate Subkeys", "Notify", "Create Link" },
				"Process" => new[] { "Terminate", "Create Thread", "Set Session ID", "VM Operation", "VM Read", "VM Write", "Duplicate Handle", "Create Process", "Set Quota", "Set Information", "Query Information", "Suspend / Resume", "Query Limited Information", "Set Limited Information" },
				"Thread" => new[] { "Terminate", "Suspend / Resume", "Alert", "Get Context", "Set Context", "Set Information", "Query Information", "Set Token", "Impersonate", "Direct Impersonation", "Set Limited Information", "Query Limited Information", "Resume" },
				"Token" => new[] { "Assign Primary", "Duplicate", "Impersonate", "Query", "Query Source", "Adjust Privileges", "Adjust Groups", "Adjust Default", "Adjust Session ID" },
				"Section" => new[] { "Query", "Map Write", "Map Read", "Map Execute", "Extend Size", "Map Execute Explicit" },
				"Job" => new[] { "Assign Process", "Set Attributes", "Query", "Terminate", "Set Security Attributes", "Impersonate" },
				"Directory" => new[] { "Query", "Traverse", "Create Object", "Create Subdirectory" },
				"Event" or "Semaphore" or "Timer" => new[] { "Query State", "Modify State" },
				"Mutant" or "SymbolicLink" => new[] { "Query State" },
				_ => System.Array.Empty<string>()
			};
			for (var bit = 0; bit < specific.Length; bit++)
				rights.Add((1U << bit, specific[bit]));
			var selected = rights.Where(right => (mask & right.Mask) != 0).ToArray();
			var unknown = mask & ~selected.Aggregate(0U, (value, right) => value | right.Mask);
			return string.Join(", ", selected.Select(right => right.Name).Concat(unknown == 0 ? System.Array.Empty<string>() : new[] { $"Unknown 0x{unknown:X8}" }));
		}

		public static Task<WindowsObjectDirectory> ReadDirectoryAsync(string path, int maximumEntries = 4096, CancellationToken cancellationToken = default)
			=> ReadDirectoryAsync(path, maximumEntries, MaximumDirectoryCharacters, cancellationToken);

		/// <summary>Bounds retained UTF-16 name, type and full-path characters independently of row count and the five-second native query budget.</summary>
		public static Task<WindowsObjectDirectory> ReadDirectoryAsync(string path, int maximumEntries, int maximumCharacters, CancellationToken cancellationToken = default) {
			ValidatePath(path);
			Guard.ArgumentInRange(maximumEntries, 1, 65536, nameof(maximumEntries));
			Guard.ArgumentInRange(maximumCharacters, 1, MaximumDirectoryCharacters, nameof(maximumCharacters));
			Guard.ArgumentGTE(maximumCharacters, path.Length, nameof(maximumCharacters));
			return Task.Run(() => ReadDirectoryCore(path, maximumEntries, maximumCharacters, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
		}

		public static Task<string> ReadSymbolicLinkAsync(string path, CancellationToken cancellationToken = default) {
			ValidatePath(path);
			return Task.Run(() => ReadSymbolicLinkCore(path, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
		}

		/// <summary>Conservative UTF-16 payload cost; repeated names in a full path are charged independently.</summary>
		public static long GetRetainedCharacterCount(WindowsObjectEntry entry) => CountRetainedCharacters(entry);

		/// <summary>Uses the existing conservative payload accounting for a captured directory and its entries.</summary>
		public static long GetRetainedCharacterCount(WindowsObjectDirectory directory) => CountRetainedCharacters(directory);

		/// <summary>Conservative UTF-16 payload cost; repeated names in a full path are charged independently.</summary>
		public static long CountRetainedCharacters(WindowsObjectEntry entry) {
			Guard.ArgumentNotNull(entry, nameof(entry));
			return (long)(entry.Name?.Length ?? 0) + (entry.Type?.Length ?? 0) + (entry.Path?.Length ?? 0);
		}

		public static long CountRetainedCharacters(WindowsObjectDirectory directory) {
			Guard.ArgumentNotNull(directory, nameof(directory));
			return (long)(directory.Path?.Length ?? 0) + directory.Entries.Sum(CountRetainedCharacters);
		}

		internal static string QueryHandleString(WinAPI.KERNEL32.KernelHandle handle, bool typeName) {
			Debug.AssertNotMainThread();
			var size = 1024;
			for (var attempt = 0; attempt < 8; attempt++) {
				using var buffer = new UnmanagedMemoryScope(size);
				var status = WinAPI.NTDLL.NtQueryObject(handle, typeName ? 2 : 1, buffer.Item, (uint)size, out var returned);
				if (status is WinAPI.NTDLL.StatusBufferTooSmall or WinAPI.NTDLL.StatusInfoLengthMismatch or unchecked((int)0x80000005)) {
					size = checked((int)Math.Max((uint)size * 2, returned));
					Guard.Ensure(size <= 1024 * 1024, "The NT Object Name Exceeds The Query Budget");
					continue;
				}
				CheckStatus(status, "Query Object " + (typeName ? "Type" : "Name"));
				Guard.Ensure(returned >= Marshal.SizeOf<WinAPI.NTDLL.UNICODE_STRING>() && returned <= size, "Windows Returned An Invalid Object Information Length");
				return ReadString(Marshal.PtrToStructure<WinAPI.NTDLL.UNICODE_STRING>(buffer.Item), buffer.Item, checked((int)returned));
			}
			throw new InvalidOperationException("The NT Object Name Kept Growing During Capture");
		}

		private static WindowsObjectDirectory ReadDirectoryCore(string path, int maximumEntries, int maximumCharacters, Action checkpoint) {
			Debug.AssertNotMainThread();
			using var name = new ObjectNameScope(path);
			var attributes = name.Attributes;
			var status = WinAPI.NTDLL.NtOpenDirectoryObject(out var directory, 3, ref attributes);
			using (directory) {
				CheckStatus(status, "Open Object Directory");
				var entries = new List<WindowsObjectEntry>();
				var retainedCharacters = (long)path.Length;
				var elapsed = Stopwatch.StartNew();
				uint context = 0;
				var restart = true;
				var size = 4096;
				while (entries.Count < maximumEntries && elapsed.Elapsed < TimeSpan.FromSeconds(5)) {
					checkpoint();
					using var buffer = new UnmanagedMemoryScope(size);
					var savedContext = context;
					status = WinAPI.NTDLL.NtQueryDirectoryObject(directory, buffer.Item, (uint)size, true, restart, ref context, out var returned);
					if (status == unchecked((int)0x8000001A)) {
						checkpoint();
						return new(path, entries.ToArray(), false);
					}
					if (status is WinAPI.NTDLL.StatusBufferTooSmall or WinAPI.NTDLL.StatusInfoLengthMismatch or unchecked((int)0x80000005)) {
						context = savedContext;
						size = checked((int)Math.Max((uint)size * 2, returned));
						Guard.Ensure(size <= 131072, "The NT Directory Entry Exceeds The Query Budget");
						continue;
					}
					CheckStatus(status, "Read Object Directory");
					var stringSize = Marshal.SizeOf<WinAPI.NTDLL.UNICODE_STRING>();
					Guard.Ensure(returned >= stringSize * 2 && returned <= size, "Windows Returned An Invalid Object Directory Entry");
					var entryName = ReadString(Marshal.PtrToStructure<WinAPI.NTDLL.UNICODE_STRING>(buffer.Item), buffer.Item, (int)returned);
					var entryType = ReadString(Marshal.PtrToStructure<WinAPI.NTDLL.UNICODE_STRING>(IntPtr.Add(buffer.Item, stringSize)), buffer.Item, (int)returned);
					Guard.Ensure(entryName.Length > 0 && !entryName.Contains('\\'), "Windows Returned An Invalid Object Entry Name");
					var entryPath = path.TrimEnd('\\') + "\\" + entryName;
					var entry = new WindowsObjectEntry(entryName, entryType, entryPath);
					var characters = CountRetainedCharacters(entry);
					if (characters > maximumCharacters - retainedCharacters) {
						checkpoint();
						return new(path, entries.ToArray(), true, "Retained Text Limit Reached");
					}
					retainedCharacters += characters;
					entries.Add(entry);
					restart = false;
				}
				checkpoint();
				return new(path, entries.ToArray(), true, entries.Count >= maximumEntries ? "Entry Limit Reached" : "Five-Second Query Limit Reached");
			}
		}

		private static string ReadSymbolicLinkCore(string path, Action checkpoint) {
			Debug.AssertNotMainThread();
			checkpoint();
			using var name = new ObjectNameScope(path);
			var attributes = name.Attributes;
			var status = WinAPI.NTDLL.NtOpenSymbolicLinkObject(out var link, 1, ref attributes);
			using (link) {
				CheckStatus(status, "Open Object Symbolic Link");
				using var buffer = new UnmanagedMemoryScope(65534);
				var target = new WinAPI.NTDLL.UNICODE_STRING { Buffer = buffer.Item, MaximumLength = 65534 };
				status = WinAPI.NTDLL.NtQuerySymbolicLinkObject(link, ref target, out _);
				CheckStatus(status, "Read Object Symbolic Link");
				checkpoint();
				return ReadString(target, buffer.Item, 65534);
			}
		}

		private static string ReadString(WinAPI.NTDLL.UNICODE_STRING text, IntPtr allocation, int size) {
			Guard.Ensure(text.Length % 2 == 0 && text.Length <= text.MaximumLength, "Windows Returned An Invalid Unicode String Length");
			if (text.Length == 0)
				return string.Empty;
			var start = unchecked((ulong)allocation.ToInt64());
			var address = unchecked((ulong)text.Buffer.ToInt64());
			Guard.Ensure(address >= start && address - start <= (ulong)size && text.Length <= (ulong)size - (address - start), "Windows Returned An Object String Outside Its Buffer");
			return Marshal.PtrToStringUni(text.Buffer, text.Length / 2);
		}

		private static void ValidatePath(string path) {
			Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
			Guard.Argument(path.StartsWith('\\') && path.Length <= 32766 && !path.Contains('\0'), nameof(path), "Use An Absolute NT Object Path Of At Most 32766 Characters");
		}

		private static void CheckStatus(int status, string operation) {
			if (status < 0)
				throw new Win32Exception((int)WinAPI.NTDLL.RtlNtStatusToDosError(status), operation + $" Failed (NTSTATUS 0x{status:X8})");
		}

		#region Inner Types

		private sealed class ObjectNameScope : IDisposable {
			private readonly UnmanagedMemoryScope _allocation;

			public ObjectNameScope(string path) {
				var bytes = Encoding.Unicode.GetBytes(path + '\0');
				var stringSize = Marshal.SizeOf<WinAPI.NTDLL.UNICODE_STRING>();
				_allocation = new UnmanagedMemoryScope(stringSize + bytes.Length);
				var text = IntPtr.Add(_allocation.Item, stringSize);
				Marshal.Copy(bytes, 0, text, bytes.Length);
				Marshal.StructureToPtr(new WinAPI.NTDLL.UNICODE_STRING { Buffer = text, Length = checked((ushort)(bytes.Length - 2)), MaximumLength = checked((ushort)bytes.Length) }, _allocation.Item, false);
				Attributes = new WinAPI.NTDLL.OBJECT_ATTRIBUTES { Length = (uint)Marshal.SizeOf<WinAPI.NTDLL.OBJECT_ATTRIBUTES>(), ObjectName = _allocation.Item, Attributes = 0x40 };
			}

			public WinAPI.NTDLL.OBJECT_ATTRIBUTES Attributes { get; }

			public void Dispose() => _allocation.Dispose();
		}

		#endregion
	}
}
