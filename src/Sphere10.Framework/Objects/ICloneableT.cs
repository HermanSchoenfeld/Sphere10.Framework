// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>Defines typed cloning and supplies the System.ICloneable adapter.</summary>
public interface ICloneable<out T> : System.ICloneable {
	new T Clone();

	#region Base Interface Bridges

	object System.ICloneable.Clone() => Clone();

	#endregion

}
