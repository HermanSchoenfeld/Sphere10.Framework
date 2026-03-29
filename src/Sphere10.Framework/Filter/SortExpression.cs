// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Describes a single sort criterion: a property name and a direction.
/// </summary>
public class SortExpression {

	public string Property { get; set; }

	public SortDirection Direction { get; set; }

	public SortExpression() {
	}

	public SortExpression(string property, SortDirection direction) {
		Property = property;
		Direction = direction;
	}
}
