// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable Blazor facade over shared application-block metadata.</summary>
public class BlazorApplicationBlock : IBlazorApplicationBlock {
	public const string DefaultIconUrl = "";

	private readonly BlockMetadata _definition;
	private readonly BlazorApplicationMenu _toolbarOwner;
	private readonly BlazorApplicationMenu _generatedOwner;
	private readonly Dictionary<ApplicationScreenDefinition, BlazorScreenMenuItem> _definitionCommands = new(ReferenceEqualityComparer.Instance);
	private BlazorScreenMenuItem _defaultCommand;
	private IBlazorApplicationMenuItem[] _toolBarItems = Array.Empty<IBlazorApplicationMenuItem>();
	private string _id;
	private Type _defaultScreen;
	private string _defaultScreenTitle;
	private ScreenActivationMode? _defaultScreenActivationMode;
	private ScreenKind _defaultScreenKind;

	public BlazorApplicationBlock() {
		_definition = new BlockMetadata(this);
		_toolbarOwner = new BlazorApplicationMenu { Parent = this, Text = "Toolbar" };
		_generatedOwner = new BlazorApplicationMenu { Parent = this, Text = "Screen activators" };
	}

	public IBlazorApplication Parent { get; set; }

	public IBlazorPlugin Source { get; set; }

	public string Id {
		get => _id ?? Title;
		init => _id = value;
	}

	public int Position {
		get => _definition.Position;
		init => _definition.Position = value;
	}

	public string Title {
		get => _definition.Name;
		init => _definition.Name = value;
	}

	public string Name => _definition.Name;

	public string IconUrl { get; init; }

	public string Tooltip { get; init; }

	public IBlazorApplicationMenu[] Menus {
		get => _definition.Menus.Cast<IBlazorApplicationMenu>().ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var menu in value)
				_definition.AddMenu(menu);
		}
	}

	public IBlazorApplicationMenuItem[] ToolBarItems {
		get => _toolBarItems.ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_toolBarItems = value.ToArray();
			foreach (var item in _toolBarItems.Where(item => item.Parent == null))
				_toolbarOwner.AddItem(item);
			foreach (var screen in _toolBarItems.OfType<BlazorScreenMenuItem>())
				_definition.AddScreenDefinition(screen.ScreenDefinition);
		}
	}

	public Type DefaultScreen {
		get => _defaultScreen ?? _definition.DefaultScreenDefinition?.ScreenType;
		init => _defaultScreen = value;
	}

	public ScreenActivationMode? DefaultScreenActivationMode {
		get => _defaultScreenActivationMode ?? _definition.DefaultScreenDefinition?.ActivationMode;
		init => _defaultScreenActivationMode = value;
	}

	public ScreenKind DefaultScreenKind {
		get => _definition.DefaultScreenDefinition?.ScreenKind ?? _defaultScreenKind;
		init => _defaultScreenKind = value;
	}

	public string DefaultScreenTitle {
		get => _defaultScreenTitle ?? _definition.DefaultScreenDefinition?.Title;
		init => _defaultScreenTitle = value;
	}

	public ApplicationScreenDefinition[] ScreenDefinitions {
		get {
			_ = DefaultScreenDefinition;
			return _definition.ScreenDefinitions;
		}
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var screen in value)
				_definition.AddScreenDefinition(screen);
		}
	}

	public ApplicationScreenDefinition DefaultScreenDefinition {
		get {
			if (_definition.DefaultScreenDefinition == null && _defaultScreen != null) {
				var matching = Menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>().FirstOrDefault(item => item.ScreenType == _defaultScreen);
				_definition.DefaultScreenDefinition = matching != null && _defaultScreenTitle == null && !_defaultScreenActivationMode.HasValue && _defaultScreenKind == ScreenKind.Normal
					? matching.ScreenDefinition : new ApplicationScreenDefinition(_defaultScreen, _defaultScreenTitle ?? matching?.ScreenTitle ?? matching?.Title,
					_defaultScreenActivationMode ?? matching?.ActivationMode ?? ScreenActivationMode.MultiInstance,
					_defaultScreenKind == ScreenKind.Normal ? matching?.ScreenKind ?? ScreenKind.Normal : _defaultScreenKind);
			}
			if (_definition.DefaultScreenDefinition == null)
				_definition.DefaultScreenDefinition = Menus.SelectMany(menu => menu.Items).Concat(ToolBarItems).OfType<BlazorScreenMenuItem>()
					.FirstOrDefault(item => item.IsDefault)?.ScreenDefinition;
			return _definition.DefaultScreenDefinition;
		}
		init => _definition.DefaultScreenDefinition = value;
	}

	/// <summary>Allows a block to veto exit using the running application's circuit context.</summary>
	public virtual Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(application, nameof(application));
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(true);
	}

	internal BlazorScreenMenuItem GetGeneratedScreenCommand(ApplicationScreenDefinition definition, string id, BlazorScreenMenuItem matching = null, bool isDefault = false) {
		if (isDefault && _defaultCommand != null)
			return _defaultCommand;
		if (!isDefault && _definitionCommands.TryGetValue(definition, out var command))
			return command;
		command = new BlazorScreenMenuItem {
			Id = id, Title = definition.Title, ScreenDefinition = definition,
			ActivationMode = definition.ActivationMode ?? matching?.ActivationMode ?? ScreenActivationMode.MultiInstance,
			Icon = matching?.Icon, IsVisible = matching?.IsVisible ?? true, CanExecute = matching?.CanExecute, IsDefault = isDefault,
			Parameters = matching?.Parameters ?? new Dictionary<string, object>()
		};
		if (matching != null)
			command.CopySubscriptionsFrom(matching);
		_generatedOwner.AddItem(command);
		if (isDefault)
			_defaultCommand = command;
		else
			_definitionCommands.Add(definition, command);
		return command;
	}

	private sealed class BlockMetadata : ApplicationBlockBase {
		private readonly IBlazorApplicationBlock _owner;

		public BlockMetadata(IBlazorApplicationBlock owner) => _owner = owner;

		protected override IApplicationBlock MenuOwner => _owner;
	}
}
