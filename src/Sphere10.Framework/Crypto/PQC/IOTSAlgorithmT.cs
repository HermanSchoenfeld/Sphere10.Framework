// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>Defines a one-time signature algorithm with a specific configuration type.</summary>
public interface IOTSAlgorithm<out TConfig> : IOTSAlgorithm
	where TConfig : OTSConfig {
	new TConfig Config { get; }

	#region Base Interface Bridges

	OTSConfig IOTSAlgorithm.Config => Config;

	#endregion

}
