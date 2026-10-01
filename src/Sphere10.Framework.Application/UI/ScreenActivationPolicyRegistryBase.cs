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

public abstract class ScreenActivationPolicyRegistryBase : IScreenActivationPolicyRegistry {
	public abstract void RegisterDeclarations(IEnumerable<KeyValuePair<Type, ScreenActivationMode>> declarations, Action<Type> validateScreenType = null);

	public abstract bool TryGetPolicy(Type screenType, out ScreenActivationMode activationMode);

	public abstract bool IsExplicitlyDeclared(Type screenType);

	public abstract void Validate(Type screenType, ScreenActivationMode activationMode);

	public abstract void RegisterInstance(Type screenType, ScreenActivationMode activationMode);
}
