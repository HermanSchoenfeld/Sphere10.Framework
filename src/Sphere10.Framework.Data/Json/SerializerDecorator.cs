// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data;

public class SerializerDecorator : IJsonSerializer {
	protected IJsonSerializer InternalSerializer;

	public SerializerDecorator(IJsonSerializer internalSerializer) {
		InternalSerializer = internalSerializer;
	}

	public virtual string Serialize<T>(T value) => InternalSerializer.Serialize<T>(value);

	public virtual T Deserialize<T>(string value) => InternalSerializer.Deserialize<T>(value);
}

