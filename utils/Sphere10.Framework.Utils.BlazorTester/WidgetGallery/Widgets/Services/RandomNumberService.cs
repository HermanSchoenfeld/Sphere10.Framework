// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Services;

public class RandomNumberService : IRandomNumberService {
	public int GetRandomNumber() => (int)(EndianBitConverter.Little.ToUInt32(Tools.Crypto.GenerateCryptographicallyRandomBytes(4), 0) & int.MaxValue);
}

public interface IRandomNumberService {
	int GetRandomNumber();
}
