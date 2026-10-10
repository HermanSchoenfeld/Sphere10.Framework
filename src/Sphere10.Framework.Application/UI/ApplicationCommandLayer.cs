// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>A provider's command contribution at an explicit application, block or screen level.</summary>
public class ApplicationCommandLayer {
	public ApplicationCommandLayer(ApplicationCommandLevel level, IApplicationCommandProvider provider) {
		Guard.Argument(level is ApplicationCommandLevel.Application or ApplicationCommandLevel.Block or ApplicationCommandLevel.Screen,
			nameof(level), "Unknown application command level.");
		Guard.ArgumentNotNull(provider, nameof(provider));
		Level = level;
		Provider = provider;
	}

	public ApplicationCommandLevel Level { get; }

	public IApplicationCommandProvider Provider { get; }
}
