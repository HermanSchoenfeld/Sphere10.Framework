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
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Reusable block metadata and canonical screen definitions.</summary>
public abstract class ApplicationBlockBase : IApplicationBlock {
	private readonly List<ApplicationScreenDefinition> _screenDefinitions = new();
	private ApplicationScreenDefinition _defaultScreenDefinition;

	public virtual IApplication Parent { get; set; }

	public virtual IApplicationPlugin Source { get; set; }

	public virtual string Name { get; set; } = string.Empty;

	public virtual int Position { get; set; }

	/// <summary>The actual settings instance shared by this owner and its screens.</summary>
	public virtual SettingsObject BlockSettings { get; set; }

	public virtual ApplicationScreenDefinition[] ScreenDefinitions => _screenDefinitions.ToArray();

	public virtual ApplicationScreenDefinition DefaultScreenDefinition {
		get => _defaultScreenDefinition;
		set {
			Guard.Argument(value == null || ContainsScreenDefinition(value), nameof(value), "The default screen must already be registered by this block.");
			_defaultScreenDefinition = value;
		}
	}

	public virtual Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(application, nameof(application));
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(true);
	}

	public virtual void AddScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		if (!ContainsScreenDefinition(definition))
			_screenDefinitions.Add(definition);
	}

	public virtual bool ContainsScreenDefinition(ApplicationScreenDefinition definition) =>
		_screenDefinitions.Any(candidate => ReferenceEquals(candidate, definition));

	/// <summary>Removes a definition using reference identity; platform adapters validate their navigation references.</summary>
	public virtual void RemoveScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.Argument(!ReferenceEquals(DefaultScreenDefinition, definition), nameof(definition), "The default screen definition cannot be removed.");
		_screenDefinitions.RemoveAll(candidate => ReferenceEquals(candidate, definition));
	}
}
