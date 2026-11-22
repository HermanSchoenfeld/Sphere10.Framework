// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>
/// One command a screen offers in the shell while it is the active screen: an icon, a caption that is also its tooltip, when it
/// is enabled and visible, and its action. A screen declares each command once, usually in <c>OnInitialized</c>, with
/// <see cref="ApplicationScreen.Command"/> and places it with a <see cref="ScreenCommandLayout"/> through
/// <see cref="ApplicationScreen.UseCommands"/>; the screen turns them into the shell's menu items and icon toolbar buttons and
/// runs the action through its <see cref="ApplicationScreen.RunCommandAsync"/> hook. The predicates read the screen's state, so
/// a command is offered only where it can run. A group box that the command acts on also shows it as an icon in its caption with
/// <c>ScreenCommandIcon</c> or <c>GroupBoxCaption</c>, which share the toolbar's state and action.
/// </summary>
public sealed class ScreenCommand {
	private readonly Func<Task> _action;
	private Func<bool> _enabled = () => true;
	private Func<bool> _visible = () => true;
	private Func<string> _blocker = () => string.Empty;

	internal ScreenCommand(ApplicationScreen screen, string id, string text, string icon, Func<Task> action) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		Guard.ArgumentNotNullOrWhitespace(id, nameof(id));
		Guard.ArgumentNotNullOrWhitespace(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Screen = screen;
		Id = id;
		Text = text;
		Icon = icon;
		_action = action;
	}

	/// <summary>The command's stable identity in the shell's menus and toolbar.</summary>
	public string Id { get; }

	/// <summary>The command's name, such as "Lock Product", in the menu and as the toolbar button's tooltip.</summary>
	public string Text { get; }

	/// <summary>The icon's CSS classes, such as "fas fa-lock".</summary>
	public string Icon { get; }

	/// <summary>Why the command cannot run now, such as "Product: Locked"; empty when nothing blocks it.</summary>
	public string Blocker => _blocker() ?? string.Empty;

	/// <summary>The caption the shell shows: the name, followed by what blocks the command when something does.</summary>
	public string Title => Blocker.Length == 0 ? Text : $"{Text} ({Blocker})";

	/// <summary>Whether the command is shown; a hidden command is left out of its menu and the toolbar.</summary>
	public bool IsVisible => _visible();

	/// <summary>Whether the command can run now, apart from the screen being busy.</summary>
	public bool CanExecute => Blocker.Length == 0 && _enabled();

	/// <summary>Whether the command is enabled where the shell shows it: it can run and its screen is not busy.</summary>
	public bool IsEnabled => Screen.CanRunCommand(this);

	// The screen that declared the command, whose busy state and command hook it shares wherever it is shown.
	internal ApplicationScreen Screen { get; }

	/// <summary>Enables the command only while <paramref name="enabled"/> holds, for example while a record is selected.</summary>
	public ScreenCommand EnabledWhen(Func<bool> enabled) {
		Guard.ArgumentNotNull(enabled, nameof(enabled));
		_enabled = enabled;
		return this;
	}

	/// <summary>
	/// Disables the command while <paramref name="blocker"/> gives a reason, which the caption and tooltip then name, for example
	/// a service flag's refusal such as "Product: Locked".
	/// </summary>
	public ScreenCommand BlockedBy(Func<string> blocker) {
		Guard.ArgumentNotNull(blocker, nameof(blocker));
		_blocker = blocker;
		return this;
	}

	/// <summary>Shows the command only while <paramref name="visible"/> holds, for example only for the kind of record selected.</summary>
	public ScreenCommand VisibleWhen(Func<bool> visible) {
		Guard.ArgumentNotNull(visible, nameof(visible));
		_visible = visible;
		return this;
	}

	/// <summary>
	/// Runs the command as the shell's menu and toolbar do: through its screen's <see cref="ApplicationScreen.RunCommandAsync"/> hook,
	/// and only while it is visible and <see cref="IsEnabled"/>. The screen renders afterwards, which publishes any changed command state.
	/// </summary>
	/// <returns>Whether the command ran.</returns>
	public Task<bool> RunAsync() => Screen.RunScreenCommandAsync(this);

	internal Task ExecuteAsync() => _action();
}
