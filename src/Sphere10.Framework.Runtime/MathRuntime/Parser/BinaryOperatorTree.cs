// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics;

namespace Sphere10.Framework.Maths.Compiler;

public class BinaryOperatorTree : SyntaxTree {
	public BinaryOperatorTree(Token token)
		: base(token) {
	}

	public Operator Operator { get; set; }

	public SyntaxTree LeftHandSide { get; set; }

	public SyntaxTree RightHandSide { get; set; }

	public override string ToString() {
		Debug.Assert(LeftHandSide != null);
		Debug.Assert(RightHandSide != null);
		return string.Format("{0}({1},{2})", Operator, LeftHandSide.ToString(), RightHandSide.ToString());
	}
}

