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
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

// Screen commands: the commands a screen declares with Command and places with UseCommands, published as the shell's menus
// and icon toolbar through the Menus and ToolBarItems defaults.
public abstract partial class ApplicationScreen {
	// Raised by NotifyScreenChanged, a change that no render of the screen follows, so command icons outside the shell refresh too.
	internal event EventHandlerEx CommandsChanged;

	private ScreenCommandLayout _commandLayout;
	private string _commandShape;
	private IApplicationMenu[] _commandMenus = [];
	private IApplicationMenuItem[] _commandToolBarItems = [];

	/// <summary>
	/// Whether the screen is running work that its commands must wait for; every command placed by <see cref="UseCommands"/> is
	/// disabled while it is true. A screen with a busy guard overrides it.
	/// </summary>
	protected virtual bool IsBusy => false;

	// Whether one of the screen's commands is enabled wherever it is shown: the toolbar, the menus and group box caption icons.
	internal bool CanRunCommand(ScreenCommand command) => !IsBusy && command.CanExecute;

	// Runs one of the screen's commands from outside the shell, such as a group box caption icon, exactly as the shell's item would,
	// then renders a rendered screen so its changed state, and the command state it publishes to the shell, is shown.
	internal async Task<bool> RunScreenCommandAsync(ScreenCommand command) {
		if (_disposed || !command.IsVisible || !CanRunCommand(command))
			return false;
		await RunCommandAsync(command.ExecuteAsync);
		if (_initialized && !_disposed)
			await InvokeAsync(StateHasChanged);
		return true;
	}

	/// <summary>
	/// Declares one of the screen's commands. Its action runs through <see cref="RunCommandAsync"/> when the shell invokes it, and
	/// the command is disabled while <see cref="IsBusy"/> holds.
	/// </summary>
	/// <example>
	/// <code>
	/// protected override void OnInitialized() {
	/// 	var newProduct = Command("product-new", "New Product", "fas fa-plus", NewProductAsync);
	/// 	var lockProduct = Command("product-lock", "Lock Product", "fas fa-lock", LockAsync)
	/// 		.EnabledWhen(() => _product != null)
	/// 		.BlockedBy(() => _product?.CanLock == false ? "Product: Locked" : string.Empty);
	/// 	UseCommands(new ScreenCommandLayout {
	/// 		File = [newProduct],
	/// 		MenuId = "product", MenuText = () => "Product",
	/// 		Menu = [[lockProduct]],
	/// 		ToolBar = [[newProduct], [lockProduct]]
	/// 	});
	/// }
	/// </code>
	/// </example>
	protected ScreenCommand Command(string id, string text, string icon, Func<Task> action) => new(this, id, text, icon, action);

	/// <summary>
	/// Places the screen's commands in the shell's File menu, its record-type menu and its icon toolbar. The default
	/// <see cref="Menus"/> and <see cref="ToolBarItems"/> publish them; a screen that overrides either publishes its own instead.
	/// </summary>
	protected void UseCommands(ScreenCommandLayout layout) {
		Guard.ArgumentNotNull(layout, nameof(layout));
		_commandLayout = layout;
		_commandShape = null;
	}

	/// <summary>
	/// Runs a command's action when the shell invokes it. The default runs it directly; a screen overrides this to run every
	/// command under its own guard, for example one that waits for a reload, reports failures in a status line and sets
	/// <see cref="IsBusy"/> meanwhile.
	/// </summary>
	protected virtual Task RunCommandAsync(Func<Task> action) => action();

	// The File menu items and the record-type menu of the commands placed by UseCommands.
	private IApplicationMenu[] GetCommandMenus() {
		RefreshCommands();
		return Tools.Array.Clone(_commandMenus);
	}

	// The icon toolbar of the commands placed by UseCommands.
	private IApplicationMenuItem[] GetCommandToolBarItems() {
		RefreshCommands();
		return Tools.Array.Clone(_commandToolBarItems);
	}

	// The shell's menu items carry a fixed caption and visibility, so they are built again only when a command's caption (which
	// names what blocks it), its visibility or the menu's caption has changed; enabled states are read live from the commands.
	private void RefreshCommands() {
		if (_commandLayout == null)
			return;
		var layout = _commandLayout;
		var commands = layout.File.Concat(layout.Menu.SelectMany(group => group)).Concat(layout.ToolBar.SelectMany(group => group)).Distinct().ToArray();
		var menuText = layout.MenuText();
		var shape = menuText + "\n" + string.Join("\n", commands.Select(command => $"{command.Id}|{command.Title}|{command.IsVisible}"));
		if (shape == _commandShape)
			return;
		_commandShape = shape;
		// A hidden command is left out, so a group of hidden commands leaves no separator behind.
		var items = commands.Where(command => command.IsVisible).ToDictionary(command => command, command => (IApplicationMenuItem)new ApplicationMenuItemBuilder()
			.WithId(command.Id)
			.WithText(command.Title)
			.WithToolBarCaption(command.Text)
			.WithIcon(command.Icon)
			.EnabledWhen(() => CanRunCommand(command))
			.WithAction((_, _) => RunCommandAsync(command.ExecuteAsync))
			.Build());
		var menus = new List<IApplicationMenu>();
		var fileItems = Grouped([layout.File], items).ToArray();
		if (fileItems.Length > 0) {
			var file = new ApplicationMenuBuilder().WithId(Application.FileMenuId).WithText("File");
			foreach (var item in fileItems)
				file.AddItem(item);
			menus.Add(file.Build());
		}
		var menuItems = Grouped(layout.Menu, items).ToArray();
		if (menuItems.Length > 0) {
			var menu = new ApplicationMenuBuilder().WithId(layout.MenuId).WithText(menuText);
			foreach (var item in menuItems)
				menu.AddItem(item);
			menus.Add(menu.Build());
		}
		_commandMenus = menus.ToArray();
		_commandToolBarItems = Grouped(layout.ToolBar, items).ToArray();
	}

	// The visible commands of each group, with a separator between groups that have any.
	private static IEnumerable<IApplicationMenuItem> Grouped(ScreenCommand[][] groups, Dictionary<ScreenCommand, IApplicationMenuItem> items) {
		var first = true;
		foreach (var group in groups) {
			var shown = group.Where(items.ContainsKey).ToArray();
			if (shown.Length == 0)
				continue;
			if (!first)
				yield return new BlazorMenuSeparator();
			first = false;
			foreach (var command in shown)
				yield return items[command];
		}
	}
}
