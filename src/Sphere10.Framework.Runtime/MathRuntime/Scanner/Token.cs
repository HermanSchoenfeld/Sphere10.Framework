// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Maths.Compiler;

public class Token {
	public Token(TokenType type, string value, int line, int startPos, int endPos) {
		TokenType = type;
		Value = value;
		Line = line;
		StartPosition = startPos;
		EndPosition = endPos;
	}

	public string Value { get; set; }

	public TokenType TokenType { get; set; }

	public int Line { get; set; }

	public int StartPosition { get; set; }

	public int EndPosition { get; set; }


}

