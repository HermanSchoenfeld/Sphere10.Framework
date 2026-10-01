// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

/// <summary>Validates complete declaration batches before installing any policy. Callers serialize host mutations.</summary>
public class ScreenActivationPolicyRegistry : ScreenActivationPolicyRegistryBase {
	private readonly Dictionary<Type, ScreenActivationMode> _policies = new();
	private readonly HashSet<Type> _declarations = new();

	public override void RegisterDeclarations(IEnumerable<KeyValuePair<Type, ScreenActivationMode>> declarations, Action<Type> validateScreenType = null) {
		Guard.ArgumentNotNull(declarations, nameof(declarations));
		var pending = new Dictionary<Type, ScreenActivationMode>();
		foreach (var declaration in declarations) {
			Tools.UI.ValidateScreenType(declaration.Key);
			validateScreenType?.Invoke(declaration.Key);
			Validate(declaration.Key, declaration.Value);
			Guard.Argument(!pending.TryGetValue(declaration.Key, out var mode) || mode == declaration.Value,
				nameof(declarations), $"Menu entries for {declaration.Key.Name} declare conflicting activation modes.");
			pending[declaration.Key] = declaration.Value;
		}

		foreach (var declaration in pending) {
			_policies[declaration.Key] = declaration.Value;
			_declarations.Add(declaration.Key);
		}
	}

	public override bool TryGetPolicy(Type screenType, out ScreenActivationMode activationMode) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		return _policies.TryGetValue(screenType, out activationMode);
	}

	public override bool IsExplicitlyDeclared(Type screenType) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		return _declarations.Contains(screenType);
	}

	public override void Validate(Type screenType, ScreenActivationMode activationMode) {
		Tools.UI.ValidateScreenType(screenType);
		Tools.UI.ValidateActivationMode(activationMode);
		Guard.Argument(!_policies.TryGetValue(screenType, out var registeredMode) || registeredMode == activationMode,
			nameof(activationMode), $"The activation mode for {screenType.Name} is already registered and cannot change.");
	}

	public override void RegisterInstance(Type screenType, ScreenActivationMode activationMode) {
		Validate(screenType, activationMode);
		_policies[screenType] = activationMode;
	}
}
