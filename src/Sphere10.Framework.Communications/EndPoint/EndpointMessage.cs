// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Polyminer
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;

namespace Sphere10.Framework.Communications.RPC;

//Implement a communication endpoint message with bytes or strings
public class EndpointMessage {
	public byte[] MessageData;
	public IEndPoint Stream;

	public EndpointMessage(string message, IEndPoint targetStream = null) {
		FromString(message);
		Stream = targetStream;
	}
	public EndpointMessage(byte[] message, IEndPoint targetStream = null) {
		MessageData = message;
		Stream = targetStream;
	}
	public string ToSafeString() {
		ASCIIEncoding encoder = new ASCIIEncoding();
		// Keep tab, line feed and carriage return, which separate requests, and drop every other control character
		var safeArray = Array.FindAll(MessageData, c => c >= 32 || c == 9 || c == 10 || c == 13);
		var trimed = encoder.GetString(safeArray, 0, safeArray.Length).Trim();
		return trimed;
	}
	public override string ToString() {
		ASCIIEncoding encoder = new ASCIIEncoding();
		return encoder.GetString(MessageData, 0, MessageData.Length);
	}
	public void FromString(string message) {
		ASCIIEncoding encoder = new ASCIIEncoding();
		MessageData = encoder.GetBytes(message);
	}
}

