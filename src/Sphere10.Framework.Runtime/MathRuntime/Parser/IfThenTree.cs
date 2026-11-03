// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Maths.Compiler;

public class IfThenTree : SyntaxTree {
	public IfThenTree()
		: base() {
	}

	public SyntaxTree Condition { get; set; }

	public SyntaxTree Expression { get; set; }

	public override string ToString() {
		return string.Format("IFTHEN({0},{1})", Condition, Expression);
	}
}

