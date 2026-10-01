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

public abstract class ScreenActivationPolicyRegistryDecorator<TRegistry> : IScreenActivationPolicyRegistry where TRegistry : IScreenActivationPolicyRegistry {
	protected readonly TRegistry InternalRegistry;

	protected ScreenActivationPolicyRegistryDecorator(TRegistry internalRegistry) {
		Guard.ArgumentNotNull(internalRegistry, nameof(internalRegistry));
		InternalRegistry = internalRegistry;
	}

	public virtual void RegisterDeclarations(IEnumerable<KeyValuePair<Type, ScreenActivationMode>> declarations, Action<Type> validateScreenType = null) =>
		InternalRegistry.RegisterDeclarations(declarations, validateScreenType);

	public virtual bool TryGetPolicy(Type screenType, out ScreenActivationMode activationMode) => InternalRegistry.TryGetPolicy(screenType, out activationMode);

	public virtual bool IsExplicitlyDeclared(Type screenType) => InternalRegistry.IsExplicitlyDeclared(screenType);

	public virtual void Validate(Type screenType, ScreenActivationMode activationMode) => InternalRegistry.Validate(screenType, activationMode);

	public virtual void RegisterInstance(Type screenType, ScreenActivationMode activationMode) => InternalRegistry.RegisterInstance(screenType, activationMode);
}

public abstract class ScreenActivationPolicyRegistryDecorator : ScreenActivationPolicyRegistryDecorator<IScreenActivationPolicyRegistry> {
	protected ScreenActivationPolicyRegistryDecorator(IScreenActivationPolicyRegistry internalRegistry)
		: base(internalRegistry) {
	}
}
