// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data;

public class Sphere10FrameworkJsonSerializer : IJsonSerializer {


	public string Serialize<T>(T obj) => Tools.Json.WriteToString(obj);

	public T Deserialize<T>(string json) => Tools.Json.ReadFromString<T>(json);

}

