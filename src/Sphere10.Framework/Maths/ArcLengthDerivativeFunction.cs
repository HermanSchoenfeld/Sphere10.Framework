// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Maths;

namespace Sphere10.Framework;

public class ArcLengthDerivativeFunction : IFunction {
	private readonly IFunction _derivativeFunction = null;

	public ArcLengthDerivativeFunction(IFunction function) {
		_derivativeFunction = new FunctionDerivative(function);
	}

	public double Eval(double x) {
		return Math.Sqrt(1 + Math.Pow(_derivativeFunction.Eval(x), 2));

	}
}

