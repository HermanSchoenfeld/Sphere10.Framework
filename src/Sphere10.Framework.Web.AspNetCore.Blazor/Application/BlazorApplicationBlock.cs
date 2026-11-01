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
	private IBlazorApplicationMenuItem[] _toolBarItems = Array.Empty<IBlazorApplicationMenuItem>();
	private string _id;
	private Type _defaultScreen;

	public BlazorApplicationBlock() {
		_definition = new BlockMetadata(this);
		_toolbarOwner = new BlazorApplicationMenu { Parent = this, Text = "Toolbar" };
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
		}
	}

	public Type DefaultScreen {
		get => _defaultScreen ?? _definition.DefaultScreenDefinition?.ScreenType;
		init => _defaultScreen = value;
	}

	public ScreenActivationMode? DefaultScreenActivationMode {
		get => DefaultScreenDefinition?.ActivationMode;
	}

	public ScreenKind DefaultScreenKind {
		get => DefaultScreenDefinition?.ScreenKind ?? ScreenKind.Normal;
	}

	public string DefaultScreenTitle {
		get => DefaultScreenDefinition?.Title;
	}

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

	public ApplicationScreenDefinition DefaultScreenDefinition {
		get {
			if (_definition.DefaultScreenDefinition == null && _defaultScreen != null)
				_definition.DefaultScreenDefinition = Tools.UI.ResolveScreenRegistration(_definition.ScreenDefinitions, ApplicationScreenRegistration.ForType(_defaultScreen));
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

	private sealed class BlockMetadata : ApplicationBlockBase {
		private readonly IBlazorApplicationBlock _owner;

		public BlockMetadata(IBlazorApplicationBlock owner) => _owner = owner;

		protected override IApplicationBlock MenuOwner => _owner;
	}
}
