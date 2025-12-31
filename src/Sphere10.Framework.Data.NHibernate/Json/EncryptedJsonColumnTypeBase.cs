// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data.NHibernate;

public class EncryptedJsonColumnTypeBase<T> : JsonColumnTypeBase<T> where T : class {
	public EncryptedJsonColumnTypeBase(string password)
		: base(new EncryptedJsonSerializer(new Sphere10FrameworkJsonSerializer(), password)) {
	}

}

