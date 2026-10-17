// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Instance-based utility class for Windows shell operations such as creating shortcuts
/// and managing startup items. Access via <c>Tools.Windows.Shell</c>.
/// </summary>
public class ShellUtility {
	private const int AssociationBufferError = unchecked((int)0x80004003);
	private const uint MaximumAssociationStringLength = 1048576;

	/// <summary>Resolves the Shell's current association strings without changing registrations or verifying executable files.</summary>
	/// <remarks>Extension lookups retain current-user default resolution. Set isProgId only to inspect a specific registered ProgID.
	/// Missing or unsupported fields return empty strings and their native HRESULTs. Strings are limited to 1,048,576 characters and four reads;
	/// an oversized or repeatedly growing value returns E_POINTER without partial output. No program is launched.</remarks>
	public virtual ShellFileAssociationInfo QueryFileAssociation(string association, string verb = null, bool isProgId = false) {
		Guard.ArgumentNotNullOrWhitespace(association, nameof(association));
		Guard.Argument(association.Length <= 32767 && !association.Contains('\0'), nameof(association), "An association must be a NUL-free Shell identifier of at most 32,767 characters.");
		Guard.Argument(verb == null || !string.IsNullOrWhiteSpace(verb) && verb.Length <= 32767 && !verb.Contains('\0'), nameof(verb),
			"A verb must be null or a NUL-free Shell verb of at most 32,767 characters.");
		var flags = WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOTRUNCATE | WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOFIXUPS;
		if (isProgId)
			flags |= WinAPI.SHLWAPI.ASSOCF.ASSOCF_INIT_FIXED_PROGID | WinAPI.SHLWAPI.ASSOCF.ASSOCF_INIT_FOR_FILE;
		var results = new Dictionary<string, int>(StringComparer.Ordinal);
		string Read(WinAPI.SHLWAPI.ASSOCSTR value, string property) {
			var result = ReadAssociationString((StringBuilder output, ref uint length) => WinAPI.SHLWAPI.AssocQueryStringW(flags, value, association, verb, output, ref length));
			results.Add(property, result.HResult);
			return result.Value;
		}
		return new ShellFileAssociationInfo {
			Association = association, Verb = verb, IsProgId = isProgId,
			ProgId = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_PROGID, nameof(ShellFileAssociationInfo.ProgId)),
			FriendlyDocumentName = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_FRIENDLYDOCNAME, nameof(ShellFileAssociationInfo.FriendlyDocumentName)),
			FriendlyApplicationName = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, nameof(ShellFileAssociationInfo.FriendlyApplicationName)),
			ExecutablePath = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_EXECUTABLE, nameof(ShellFileAssociationInfo.ExecutablePath)),
			Command = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_COMMAND, nameof(ShellFileAssociationInfo.Command)),
			DefaultIcon = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_DEFAULTICON, nameof(ShellFileAssociationInfo.DefaultIcon)),
			DelegateExecute = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_DELEGATEEXECUTE, nameof(ShellFileAssociationInfo.DelegateExecute)),
			ApplicationId = Read(WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_APPID, nameof(ShellFileAssociationInfo.ApplicationId)),
			HResults = results
		};
	}

	/// <summary>Notifies Shell clients that file or protocol registrations changed. It neither writes registrations nor waits for recipients to refresh.</summary>
	public virtual void NotifyFileAssociationsChanged()
		=> WinAPI.SHELL32.SHChangeNotify(WinAPI.SHELL32.SHCNE_ASSOCCHANGED, WinAPI.SHELL32.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

	/// <summary>Reads real items in a Shell namespace, including virtual folders such as shell:RecycleBinFolder.</summary>
	/// <remarks>The synchronous method requires an STA caller. It returns detached data and keeps no COM objects.</remarks>
	public virtual WindowsShellItemEnumeration GetShellFolderItems(string namespaceName, int maximumItems = 10000)
		=> ReadShellFolderItems(namespaceName, maximumItems, null);

	public virtual Task<WindowsShellItemEnumeration> GetShellFolderItemsAsync(string namespaceName, int maximumItems = 10000, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => ReadShellFolderItems(namespaceName, maximumItems, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Returns the selected native item's currently offered, localized command names.</summary>
	public virtual string[] GetShellItemVerbs(WindowsShellItemInfo expected)
		=> WithShellItem(expected, item => {
			using var verbs = new ShellComObjectScope(((dynamic)item).Verbs());
			var names = new List<string>();
			for (var index = 0; index < (int)((dynamic)verbs.Item).Count; index++) {
				using var verb = new ShellComObjectScope(((dynamic)verbs.Item).Item(index));
				var name = (string)((dynamic)verb.Item).Name;
				if (!string.IsNullOrWhiteSpace(name))
					names.Add(name);
			}
			return names.ToArray();
		});

	public virtual Task<string[]> GetShellItemVerbsAsync(WindowsShellItemInfo expected, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => GetShellItemVerbs(expected), cancellationToken);

	/// <summary>Requests permanent deletion of one exactly revalidated Recycle Bin item. The Shell owns confirmation and asynchronous completion.</summary>
	public virtual void DeleteRecycleBinItem(WindowsShellItemInfo expected, IntPtr ownerWindow = default) => DeleteRecycleBinItemCore(expected, ownerWindow, null);

	public virtual Task DeleteRecycleBinItemAsync(WindowsShellItemInfo expected, IntPtr ownerWindow = default, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { DeleteRecycleBinItemCore(expected, ownerWindow, cancellationToken.ThrowIfCancellationRequested); return true; }, cancellationToken, false);

	private static void DeleteRecycleBinItemCore(WindowsShellItemInfo expected, IntPtr ownerWindow, Action checkpoint) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(string.Equals(expected.Namespace, "shell:RecycleBinFolder", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(expected.Namespace, "::{645FF040-5081-101B-9F08-00AA002F954E}", StringComparison.OrdinalIgnoreCase), nameof(expected), "Select an item from the Recycle Bin namespace.");
		WithShellItem(expected, item => {
			checkpoint?.Invoke();
			Marshal.ThrowExceptionForHR(WinAPI.SHELL32.SHGetIDListFromObject(item, out var identifier));
			using var identifierScope = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeCoTaskMem(identifier));
			Guard.Ensure(identifier != IntPtr.Zero, "The selected Shell item has no native identity.");
			var request = new WinAPI.SHELL32.SHELLEXECUTEINFO {
				Size = (uint)Marshal.SizeOf<WinAPI.SHELL32.SHELLEXECUTEINFO>(), Mask = WinAPI.SHELL32.SEE_MASK_INVOKEIDLIST | WinAPI.SHELL32.SEE_MASK_NOASYNC,
				Window = ownerWindow, Verb = "delete", ItemIdList = identifier, ShowCommand = 1
			};
			checkpoint?.Invoke();
			if (!WinAPI.SHELL32.ShellExecuteExW(ref request))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return true;
		});
	}

	/// <summary>Requests an exact currently offered Shell verb. Windows owns any dialogs and asynchronous operation lifetime.</summary>
	/// <remarks>Call on a COM-initialized STA, or use the async counterpart. A successful request is not proof that a later shell operation completed.</remarks>
	public virtual void InvokeShellItemVerb(WindowsShellItemInfo expected, string verbName) {
		Guard.ArgumentNotNullOrWhitespace(verbName, nameof(verbName));
		WithShellItem(expected, item => {
			using var verbs = new ShellComObjectScope(((dynamic)item).Verbs());
			for (var index = 0; index < (int)((dynamic)verbs.Item).Count; index++) {
				using var verb = new ShellComObjectScope(((dynamic)verbs.Item).Item(index));
				if (!string.Equals((string)((dynamic)verb.Item).Name, verbName, StringComparison.Ordinal))
					continue;
				((dynamic)verb.Item).DoIt();
				return true;
			}
			throw new InvalidOperationException("The selected Shell command is no longer offered by this item. Refresh its commands.");
		});
	}

	/// <summary>Invokes an admitted Shell command on a dedicated STA; cancellation does not discard an already dispatched operation.</summary>
	public virtual Task InvokeShellItemVerbAsync(WindowsShellItemInfo expected, string verbName, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { InvokeShellItemVerb(expected, verbName); return true; }, cancellationToken, false);

	internal static (string Value, int HResult) ReadAssociationString(AssociationStringQuery query) {
		uint length = 0;
		var error = query(null, ref length);
		if (error < 0 && error != AssociationBufferError)
			return (string.Empty, error);
		for (var attempt = 0; attempt < 4; attempt++) {
			if (length == 0 || length > MaximumAssociationStringLength)
				return (string.Empty, AssociationBufferError);
			var capacity = length;
			var output = new StringBuilder((int)capacity);
			error = query(output, ref length);
			if (error == 0)
				return length <= capacity ? (output.ToString(), 0) : (string.Empty, AssociationBufferError);
			if (error != AssociationBufferError || length <= capacity)
				return (string.Empty, error);
		}
		return (string.Empty, AssociationBufferError);
	}

	private static WindowsShellItemEnumeration ReadShellFolderItems(string namespaceName, int maximumItems, Action checkpoint) {
		Guard.ArgumentNotNullOrWhitespace(namespaceName, nameof(namespaceName));
		Guard.Argument(maximumItems is > 0 and <= 100000, nameof(maximumItems), "Choose a bound from 1 to 100,000 Shell items.");
		Guard.Ensure(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Shell namespace operations require an STA thread.");
		using var shell = CreateShell();
		using var folder = new ShellComObjectScope(((dynamic)shell.Item).NameSpace(namespaceName));
		Guard.Ensure(folder.Item != null, "Windows could not open the selected Shell namespace.");
		using var items = new ShellComObjectScope(((dynamic)folder.Item).Items());
		var count = (int)((dynamic)items.Item).Count;
		Guard.Ensure(count >= 0, "Windows returned an invalid Shell item count.");
		var rows = new List<WindowsShellItemInfo>();
		for (var index = 0; index < Math.Min(count, maximumItems); index++) {
			checkpoint?.Invoke();
			using var item = new ShellComObjectScope(((dynamic)items.Item).Item(index));
			if (item.Item != null)
				rows.Add(ReadShellItem(item.Item, namespaceName));
		}
		return new WindowsShellItemEnumeration(rows.ToArray(), count, count > maximumItems);
	}

	private static T WithShellItem<T>(WindowsShellItemInfo expected, Func<object, T> action) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.ArgumentNotNullOrWhitespace(expected.Namespace, nameof(expected.Namespace));
		Guard.ArgumentNotNullOrWhitespace(expected.ParsingName, nameof(expected.ParsingName));
		Guard.Ensure(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Shell commands require an STA thread.");
		using var shell = CreateShell();
		using var folder = new ShellComObjectScope(((dynamic)shell.Item).NameSpace(expected.Namespace));
		Guard.Ensure(folder.Item != null, "The selected Shell namespace is no longer available.");
		using var items = new ShellComObjectScope(((dynamic)folder.Item).Items());
		for (var index = 0; index < (int)((dynamic)items.Item).Count; index++) {
			using var item = new ShellComObjectScope(((dynamic)items.Item).Item(index));
			if (item.Item == null || !string.Equals((string)((dynamic)item.Item).Path, expected.ParsingName, StringComparison.OrdinalIgnoreCase))
				continue;
			var current = ReadShellItem(item.Item, expected.Namespace);
			Guard.Ensure(current.Name == expected.Name && current.IsFolder == expected.IsFolder && current.SizeBytes == expected.SizeBytes
				&& current.DateDeleted == expected.DateDeleted && current.OriginalDirectory == expected.OriginalDirectory,
				"The selected Shell item changed. Refresh before requesting a command.");
			return action(item.Item);
		}
		throw new InvalidOperationException("The selected Shell item is no longer present. Refresh the inventory.");
	}

	private static WindowsShellItemInfo ReadShellItem(object native, string namespaceName) {
		dynamic item = native;
		var errors = new List<string>();
		object Property(string name) {
			try { return item.ExtendedProperty(name); }
			catch (Exception error) { errors.Add(name + ": " + error.Message); return null; }
		}
		var size = Property("System.Size");
		var deleted = Property("System.Recycle.DateDeleted");
		return new WindowsShellItemInfo {
			Namespace = namespaceName, ParsingName = (string)item.Path, Name = (string)item.Name, Type = (string)item.Type, IsFolder = (bool)item.IsFolder,
			SizeBytes = size is ulong unsigned && unsigned <= long.MaxValue ? (long)unsigned : size is long signed && signed >= 0 ? signed : null,
			OriginalDirectory = Property("System.Recycle.DeletedFrom") as string, DateDeleted = deleted is DateTime date ? date : null,
			Availability = string.Join("; ", errors)
		};
	}

	private static ShellComObjectScope CreateShell() => new(Activator.CreateInstance(Type.GetTypeFromCLSID(WinAPI.SHELL32.CLSID_Shell, true)));

	private static Task<T> RunShellQueryAsync<T>(Func<T> query, CancellationToken cancellationToken, bool checkCompletionCancellation = true) {
		cancellationToken.ThrowIfCancellationRequested();
		var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		var worker = new Thread(() => {
			try {
				cancellationToken.ThrowIfCancellationRequested();
				Marshal.ThrowExceptionForHR(WinAPI.OLE32.CoInitializeEx(IntPtr.Zero, WinAPI.OLE32.COINIT_APARTMENTTHREADED | WinAPI.OLE32.COINIT_DISABLE_OLE1DDE));
				using var apartment = Tools.Scope.ExecuteOnDispose(WinAPI.OLE32.CoUninitialize);
				var result = query();
				if (checkCompletionCancellation)
					cancellationToken.ThrowIfCancellationRequested();
				completion.TrySetResult(result);
				PumpVisibleShellWindows();
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { completion.TrySetCanceled(cancellationToken); }
			catch (Exception error) { completion.TrySetException(error); }
		}) { IsBackground = true, Name = "Windows Shell Query" };
		worker.SetApartmentState(ApartmentState.STA);
		worker.Start();
		return completion.Task;
	}

	private static void PumpVisibleShellWindows() {
		// A native extension can return after creating a modeless sheet on this STA. Keep its apartment and dispatcher alive until it closes.
		var windows = new List<IntPtr>();
		var threadId = WinAPI.KERNEL32.GetCurrentThreadId();
		Exception enumerationFailure = null;
		bool CollectWindow(IntPtr window, IntPtr parameter) {
			try {
				if (WinAPI.USER32.IsWindowVisible(window))
					windows.Add(window);
				return true;
			} catch (Exception error) {
				enumerationFailure = error;
				return false;
			}
		}
		while (true) {
			windows.Clear();
			if (!WinAPI.USER32.EnumThreadWindows(threadId, CollectWindow, IntPtr.Zero)) {
				if (enumerationFailure != null)
					throw enumerationFailure;
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The Shell worker could not enumerate its own windows.");
			}
			while (WinAPI.USER32.PeekMessageW(out var message, IntPtr.Zero, 0, 0, WinAPI.USER32.PM_REMOVE)) {
				if (message.Message == WinAPI.USER32.WM_QUIT)
					return;
				var handled = false;
				foreach (var window in windows)
					if (WinAPI.USER32.IsDialogMessageW(window, ref message)) {
						handled = true;
						break;
					}
				if (!handled) {
					WinAPI.USER32.TranslateMessage(ref message);
					WinAPI.USER32.DispatchMessageW(ref message);
				}
			}
			// Recheck after dispatch: a queued callback can create or close the last visible sheet.
			windows.Clear();
			if (!WinAPI.USER32.EnumThreadWindows(threadId, CollectWindow, IntPtr.Zero)) {
				if (enumerationFailure != null)
					throw enumerationFailure;
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The Shell worker could not enumerate its own windows.");
			}
			if (windows.Count == 0)
				return;
			if (WinAPI.USER32.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, uint.MaxValue, WinAPI.USER32.QS_ALLINPUT, WinAPI.USER32.MWMO_INPUTAVAILABLE) == uint.MaxValue)
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The Shell worker could not wait for native dialog messages.");
		}
	}

	/// <summary>Launches a shell target. The caller owns the returned observation handle; disposing it does not terminate the launched program.</summary>
	/// <remarks>Invoke on a COM-initialized STA thread for shell extension and UAC dialogs, or use LaunchAsync from a UI caller.</remarks>
	public virtual Process Launch(string target, string arguments = null, string workingDirectory = null, string verb = null,
		IntPtr ownerWindow = default, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal) {
		Guard.ArgumentNotNullOrWhitespace(target, nameof(target));
		Guard.Argument(Enum.IsDefined(windowStyle), nameof(windowStyle), "Supply a defined window style.");
		if (!string.IsNullOrEmpty(workingDirectory))
			Guard.DirectoryExists(workingDirectory);
		return Process.Start(new ProcessStartInfo {
			FileName = target, Arguments = arguments ?? string.Empty, WorkingDirectory = workingDirectory ?? string.Empty,
			Verb = verb ?? string.Empty, UseShellExecute = true, WindowStyle = windowStyle,
			ErrorDialog = true, ErrorDialogParentHandle = ownerWindow
		});
	}

	/// <summary>Launches on a dedicated STA. The caller owns the returned observation, including when cancellation arrives after dispatch.</summary>
	public virtual Task<Process> LaunchAsync(string target, string arguments = null, string workingDirectory = null, string verb = null,
		IntPtr ownerWindow = default, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => Launch(target, arguments, workingDirectory, verb, ownerWindow, windowStyle), cancellationToken, false);

	/// <summary>Opens a Windows shell parsing name, including virtual folders without a filesystem path.</summary>
	public virtual void OpenShellItem(string parsingName, IntPtr ownerWindow = default) {
		Guard.ArgumentNotNullOrWhitespace(parsingName, nameof(parsingName));
		using var observation = Launch(parsingName, ownerWindow: ownerWindow);
	}

	public virtual Task OpenShellItemAsync(string parsingName, IntPtr ownerWindow = default, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { OpenShellItem(parsingName, ownerWindow); return true; }, cancellationToken, false);

	/// <summary>Returns the current user's path for an explicit known-folder ID without creating or redirecting the folder.</summary>
	public virtual string GetKnownFolderPath(Guid folderId) {
		Guard.Argument(folderId != Guid.Empty, nameof(folderId), "Supply a known-folder ID.");
		var pointer = IntPtr.Zero;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeCoTaskMem(pointer));
		Marshal.ThrowExceptionForHR(WinAPI.SHELL32.SHGetKnownFolderPath(ref folderId, 0, IntPtr.Zero, out pointer));
		return Marshal.PtrToStringUni(pointer) ?? string.Empty;
	}

	/// <summary>Queries the caller's Recycle Bin on an explicit local drive, or all drives when omitted.</summary>
	public virtual WindowsRecycleBinInfo QueryRecycleBin(string driveRoot = null) {
		driveRoot = NormalizeRecycleBinRoot(driveRoot);
		var information = new WinAPI.SHELL32.SHQUERYRBINFO { Size = (uint)Marshal.SizeOf<WinAPI.SHELL32.SHQUERYRBINFO>() };
		Marshal.ThrowExceptionForHR(WinAPI.SHELL32.SHQueryRecycleBinW(driveRoot, ref information));
		return new WindowsRecycleBinInfo(driveRoot, information.SizeBytes, information.ItemCount);
	}

	public virtual Task<WindowsRecycleBinInfo> QueryRecycleBinAsync(string driveRoot = null, CancellationToken cancellationToken = default)
		=> Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return QueryRecycleBin(driveRoot);
		}, cancellationToken);

	/// <summary>Empties the caller's selected Recycle Bin through the shell. Use the async counterpart to keep native confirmation/progress off the UI thread.</summary>
	public virtual void EmptyRecycleBin(string driveRoot = null, IntPtr ownerWindow = default, bool showConfirmation = true,
		bool showProgress = true, bool playSound = false) {
		driveRoot = NormalizeRecycleBinRoot(driveRoot);
		var flags = (showConfirmation ? 0u : WinAPI.SHELL32.SHERB_NOCONFIRMATION)
			| (showProgress ? 0u : WinAPI.SHELL32.SHERB_NOPROGRESSUI) | (playSound ? 0u : WinAPI.SHELL32.SHERB_NOSOUND);
		Marshal.ThrowExceptionForHR(WinAPI.SHELL32.SHEmptyRecycleBinW(ownerWindow, driveRoot, flags));
	}

	/// <summary>Runs native confirmation and deletion on a dedicated STA, retaining completion after dispatch even if the caller cancels.</summary>
	public virtual Task EmptyRecycleBinAsync(string driveRoot = null, IntPtr ownerWindow = default, bool showConfirmation = true,
		bool showProgress = true, bool playSound = false, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { EmptyRecycleBin(driveRoot, ownerWindow, showConfirmation, showProgress, playSound); return true; }, cancellationToken, false);

	private static string NormalizeRecycleBinRoot(string driveRoot) {
		if (driveRoot == null)
			return null;
		Guard.Argument(driveRoot.Length == 3 && char.IsAsciiLetter(driveRoot[0]) && driveRoot[1] == ':' && driveRoot[2] is '\\' or '/',
			nameof(driveRoot), "Select an explicit local drive root, such as C:\\.");
		return char.ToUpperInvariant(driveRoot[0]) + ":\\";
	}

	/// <summary>Opens the registered shell property sheet for an existing file or directory.</summary>
	/// <remarks>Call on a COM-initialized STA thread when shell extensions require that apartment.</remarks>
	public virtual void ShowProperties(string path, IntPtr ownerWindow = default) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		path = Path.GetFullPath(path);
		Guard.Ensure(File.Exists(path) || Directory.Exists(path), "The file or directory is no longer accessible.");
		var info = new WinAPI.SHELL32.SHELLEXECUTEINFO {
			Size = (uint)Marshal.SizeOf<WinAPI.SHELL32.SHELLEXECUTEINFO>(), Mask = WinAPI.SHELL32.SEE_MASK_INVOKEIDLIST | WinAPI.SHELL32.SEE_MASK_NOASYNC,
			Window = ownerWindow, Verb = "properties", File = path, ShowCommand = 1
		};
		if (!WinAPI.SHELL32.ShellExecuteExW(ref info))
			throw new Win32Exception(Marshal.GetLastWin32Error());
	}

	public virtual Task ShowPropertiesAsync(string path, IntPtr ownerWindow = default, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { ShowProperties(path, ownerWindow); return true; }, cancellationToken, false);

	/// <summary>Opens an existing directory through the Windows shell.</summary>
	public virtual void OpenDirectory(string path) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		Guard.Ensure(Directory.Exists(path), "The directory is no longer accessible.");
		using var process = Process.Start(new ProcessStartInfo { FileName = Path.GetFullPath(path), UseShellExecute = true });
	}

	public virtual Task OpenDirectoryAsync(string path, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { OpenDirectory(path); return true; }, cancellationToken, false);

	/// <summary>Launches a named tool from the Windows system directory with separate arguments.</summary>
	public void OpenSystemTool(string executable, params string[] arguments) {
		Guard.ArgumentNotNullOrWhitespace(executable, nameof(executable));
		Guard.Argument(Path.GetFileName(executable) == executable, nameof(executable), "Supply a system tool filename, without a directory path.");
		Guard.ArgumentNotNull(arguments, nameof(arguments));
		var info = new ProcessStartInfo { FileName = Path.Combine(Environment.SystemDirectory, executable), UseShellExecute = true };
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		using var process = Process.Start(info);
	}

	public Task OpenSystemToolAsync(string executable, string[] arguments, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(arguments, nameof(arguments));
		var capturedArguments = (string[])arguments.Clone();
		return RunShellQueryAsync(() => { OpenSystemTool(executable, capturedArguments); return true; }, cancellationToken, false);
	}

	/// <summary>Opens an absolute URI with its Windows-registered protocol handler.</summary>
	public void OpenUri(Uri uri) {
		Guard.ArgumentNotNull(uri, nameof(uri));
		Guard.Argument(uri.IsAbsoluteUri, nameof(uri), "Supply an absolute URI for the registered protocol handler.");
		using var process = Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
	}

	public Task OpenUriAsync(Uri uri, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { OpenUri(uri); return true; }, cancellationToken, false);

	public void CreateShortcutForApplication(string executablePath, string shortcutPath, string arguments = null, bool overwrite = true, ShellLink.LinkDisplayMode displayMode = ShellLink.LinkDisplayMode.Normal) {
		using (var Shortcut = new ShellLink()) {
			Shortcut.Target = executablePath;
			Shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
			Shortcut.Description = "My Shorcut Name Here";
			Shortcut.DisplayMode = displayMode;
			Shortcut.Arguments = arguments;
			if (File.Exists(shortcutPath) && overwrite)
				File.Delete(shortcutPath);
			Shortcut.Save(shortcutPath);
		}
	}

	public string DetermineStartupShortcutFilename(string applicationName) {
		return string.Format(
			"{1}{0}{2}.LNK",
			Path.DirectorySeparatorChar,
			Environment.GetFolderPath(Environment.SpecialFolder.Startup),
			Tools.FileSystem.ToWellFormedPath(applicationName)
		);
	}

	/// <summary>
	/// Opens a file in explorer with the file highlighted.
	/// </summary>
	public virtual void HighlightFileInExplorer(string filePath) {
		Guard.ArgumentNotNullOrWhitespace(filePath, nameof(filePath));
		Guard.Ensure(File.Exists(filePath) || Directory.Exists(filePath), "The file or directory is no longer accessible.");
		using var process = Launch(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
			"/select,\"" + Path.GetFullPath(filePath) + "\"");
	}

	public virtual Task HighlightFileInExplorerAsync(string filePath, CancellationToken cancellationToken = default)
		=> RunShellQueryAsync(() => { HighlightFileInExplorer(filePath); return true; }, cancellationToken, false);

	internal delegate int AssociationStringQuery(StringBuilder output, ref uint length);
}

