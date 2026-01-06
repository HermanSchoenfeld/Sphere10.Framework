// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Newtonsoft.Json;
using Sphere10.Framework.Communications;

namespace Sphere10.Framework.DApp.Node.RPC;

[Serializable]
public class MiningBlockUpdates {
	public uint MicroBlockNumber { get; set; }
	public uint TimeStamp { get; set; }

	[JsonConverter(typeof(ByteArrayHexConverter))]
	public byte[] TargetPOW { get; set; }
}

