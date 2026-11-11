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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Keeps a changing set of global shortcuts registered, for example the <see cref="HotKeyCommand"/>s an options screen edits.
/// Each <see cref="Apply(IEnumerable{GlobalHotKeyBinding})"/> registers the bindings it is given, re-registers those whose
/// shortcut changed, and releases the bindings that are gone or set to <see cref="Keys.None"/>. Own it and use it on one UI
/// thread. Override <see cref="CreateHotKey"/> to supply another <see cref="IGlobalHotKey"/>, for example in tests.
/// </summary>
public class GlobalHotKeySet : IDisposable {
	private readonly Dictionary<string, Registration> _registrations = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _errors = new(StringComparer.OrdinalIgnoreCase);
	private bool _disposed;

	/// <summary>Registration problems from the latest apply, as "Caption (Shortcut): reason", one per binding.</summary>
	public string[] Errors => _errors.Values.ToArray();

	/// <summary>The latest registration problem of each failed binding, keyed by binding key or command name.</summary>
	public IReadOnlyDictionary<string, string> ErrorsByKey => new Dictionary<string, string>(_errors, StringComparer.OrdinalIgnoreCase);

	/// <summary>The keys of the bindings currently holding a registration.</summary>
	public string[] RegisteredKeys => _registrations.Keys.ToArray();

	/// <summary>Registers each command at its resolved shortcut (the user's assignment or the command's default).</summary>
	public void Apply(IEnumerable<HotKeyCommand> commands, IEnumerable<HotKeyAssignment> assignments) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		Apply(commands.Select(command => new GlobalHotKeyBinding(command?.Name, command?.Caption, command == null ? Keys.None : HotKeyAssignment.Resolve(assignments, command), command?.Execute)));
	}

	/// <summary>Makes the registered shortcuts match the bindings; a failed binding keeps its previous registration.</summary>
	public void Apply(IEnumerable<GlobalHotKeyBinding> bindings) {
		Guard.ArgumentNotNull(bindings, nameof(bindings));
		ObjectDisposedException.ThrowIf(_disposed, this);
		var wanted = new Dictionary<string, GlobalHotKeyBinding>(StringComparer.OrdinalIgnoreCase);
		foreach (var binding in bindings) {
			Guard.ArgumentNotNull(binding, nameof(bindings));
			Guard.Argument(!string.IsNullOrWhiteSpace(binding.Key), nameof(bindings), "Every binding needs a key.");
			Guard.Argument(binding.Action != null, nameof(bindings), $"The binding '{binding.Key}' needs an action.");
			Guard.Argument(wanted.TryAdd(binding.Key, binding), nameof(bindings), $"The key '{binding.Key}' is bound twice.");
		}
		_errors.Clear();
		foreach (var key in _registrations.Keys.Where(key => !wanted.TryGetValue(key, out var binding) || binding.Shortcut == Keys.None).ToArray())
			Release(key);
		foreach (var binding in wanted.Values.Where(binding => binding.Shortcut != Keys.None)) {
			var created = !_registrations.TryGetValue(binding.Key, out var registration);
			if (created) {
				registration = new Registration(CreateHotKey());
				registration.HotKey.Pressed += (_, _) => Tools.Exceptions.ExecuteIgnoringException(registration.Action);
			}
			registration.Action = binding.Action;
			if (registration.Shortcut != binding.Shortcut) {
				if (registration.HotKey.TryRegister(binding.Shortcut, out var error))
					registration.Shortcut = binding.Shortcut;
				else
					_errors[binding.Key] = $"{binding.Caption ?? binding.Key} ({HotKeyEditor.Describe(binding.Shortcut)}): {error}";
			}
			if (created && registration.Shortcut == Keys.None) {
				// A new binding that failed to register holds nothing worth keeping.
				Tools.Exceptions.ExecuteIgnoringException(registration.HotKey.Dispose);
				continue;
			}
			_registrations[binding.Key] = registration;
		}
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		foreach (var key in _registrations.Keys.ToArray())
			Release(key);
		GC.SuppressFinalize(this);
	}

	/// <summary>Creates an unregistered hot key owned by this set; the default is a <see cref="GlobalHotKey"/>.</summary>
	protected virtual IGlobalHotKey CreateHotKey() => new GlobalHotKey();

	private void Release(string key) {
		var registration = _registrations[key];
		_registrations.Remove(key);
		Tools.Exceptions.ExecuteIgnoringException(registration.HotKey.Dispose);
	}

	#region Inner Types

	private sealed class Registration {
		public Registration(IGlobalHotKey hotKey) => HotKey = hotKey;

		public IGlobalHotKey HotKey { get; }

		public Keys Shortcut { get; set; }

		public Action Action { get; set; }
	}

	#endregion
}
