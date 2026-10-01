// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

namespace Tools;

public static class UI {
	public static void ValidateScreenType(Type screenType, Type screenContract = null) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Guard.Argument(typeof(IApplicationScreen).IsAssignableFrom(screenType) && !screenType.IsAbstract && !screenType.ContainsGenericParameters,
			nameof(screenType), "A concrete application screen type is required.");
		if (screenContract != null)
			Guard.Argument(screenContract.IsAssignableFrom(screenType), nameof(screenType), $"The screen must implement or derive from {screenContract.Name}.");
	}

	public static void ValidateActivationMode(ScreenActivationMode activationMode) =>
		Guard.Argument(activationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.MultiInstance, nameof(activationMode), "Unknown activation mode.");
}
