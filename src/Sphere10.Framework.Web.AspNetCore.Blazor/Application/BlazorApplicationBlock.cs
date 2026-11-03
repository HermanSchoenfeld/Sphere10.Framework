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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A web block owning its menus alongside portable screen-registration metadata.</summary>
public class BlazorApplicationBlock : IBlazorApplicationBlock {
	public const string DefaultIconUrl = "";

	private readonly BlockMetadata _definition;
	private readonly List<IBlazorApplicationMenu> _menus = new();
	private readonly BlazorApplicationMenu _toolbarOwner;
	private IBlazorApplicationMenuItem[] _toolBarItems = Array.Empty<IBlazorApplicationMenuItem>();
	private string _id;
	private Type _defaultScreen;

	/// <summary>Initializes independent block metadata and toolbar ownership.</summary>
	public BlazorApplicationBlock() {
		_definition = new BlockMetadata();
		_toolbarOwner = new BlazorApplicationMenu { Parent = this, Text = "Toolbar" };
	}

	/// <inheritdoc />
	public IBlazorApplication Parent { get; set; }

	/// <inheritdoc />
	public IBlazorPlugin Source { get; set; }

	/// <inheritdoc />
	public string Id {
		get {
			return _id ?? Title;
		}
		init {
			_id = value;
		}
	}

	/// <inheritdoc />
	public int Position {
		get {
			return _definition.Position;
		}
		init {
			_definition.Position = value;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return _definition.Name;
		}
		init {
			_definition.Name = value;
		}
	}

	/// <inheritdoc />
	public string Name {
		get {
			return _definition.Name;
		}
	}

	/// <summary>The block's ordinary settings defaults, copied into each circuit's runtime profile.</summary>
	public SettingsObject BlockSettings {
		get {
			return _definition.BlockSettings;
		}
		set {
			_definition.BlockSettings = value;
		}
	}

	/// <inheritdoc />
	public string IconUrl { get; init; }

	/// <inheritdoc />
	public string Tooltip { get; init; }

	/// <summary>The web menus owned by this block.</summary>
	public IBlazorApplicationMenu[] Menus {
		get {
			return _menus.ToArray();
		}
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var menu in value) {
				Guard.ArgumentNotNull(menu, nameof(value));
				Guard.ArgumentNotNull(menu.Items, nameof(value));
				Guard.Argument(!_menus.Any(existing => ReferenceEquals(existing, menu)), nameof(value), "The menu is already in this block.");
				Guard.Argument(menu.Parent == null || ReferenceEquals(menu.Parent, this), nameof(value), "The menu already belongs to another block.");
				menu.Parent = this;
				_menus.Add(menu);
			}
		}
	}

	/// <summary>The web toolbar commands owned by this block.</summary>
	public IBlazorApplicationMenuItem[] ToolBarItems {
		get {
			return _toolBarItems.ToArray();
		}
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_toolBarItems = value.ToArray();
			foreach (var item in _toolBarItems.Where(item => item.Parent == null))
				_toolbarOwner.AddItem(item);
		}
	}

	/// <inheritdoc />
	public Type DefaultScreen {
		get {
			return _defaultScreen ?? _definition.DefaultScreenDefinition?.ScreenType;
		}
		init {
			_defaultScreen = value;
		}
	}

	/// <inheritdoc />
	public ScreenActivationMode? DefaultScreenActivationMode {
		get {
			return DefaultScreenDefinition?.ActivationMode;
		}
	}

	/// <inheritdoc />
	public ScreenKind DefaultScreenKind {
		get {
			return DefaultScreenDefinition?.ScreenKind ?? ScreenKind.Normal;
		}
	}

	/// <inheritdoc />
	public string DefaultScreenTitle {
		get {
			return DefaultScreenDefinition?.Title;
		}
	}

	/// <inheritdoc />
	public ApplicationScreenDefinition[] ScreenDefinitions {
		get {
			return _definition.ScreenDefinitions;
		}
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var screen in value)
				_definition.AddScreenDefinition(screen);
		}
	}

	/// <inheritdoc />
	public ApplicationScreenDefinition DefaultScreenDefinition {
		get {
			if (_definition.DefaultScreenDefinition == null && _defaultScreen != null)
				_definition.DefaultScreenDefinition = Tools.UI.ResolveScreenRegistration(_definition.ScreenDefinitions, ApplicationScreenRegistration.ForType(_defaultScreen));
			return _definition.DefaultScreenDefinition;
		}
		init {
			_definition.DefaultScreenDefinition = value;
		}
	}

	/// <summary>Allows a block to veto exit using the running application's circuit context.</summary>
	public virtual Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(application, nameof(application));
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(true);
	}

	#region Inner Types

	// Shares only registration metadata with the portable application layer.
	private sealed class BlockMetadata : ApplicationBlockBase {
	}

	#endregion
}
