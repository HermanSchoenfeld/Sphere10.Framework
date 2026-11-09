// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web;

/// <summary>
/// Describes a message to show a user, with its <see cref="UserMessageType"/>. The ASP.NET Core integration collects these per request through <c>HttpContext.GetUserMessages()</c>.
/// </summary>
public sealed class UserMessage {
	public string Message { get; set; }
	public UserMessageType MessageType { get; set; }

	public UserMessage() {
		Message = string.Empty;
		MessageType = UserMessageType.Information;
	}

	public UserMessage(string message, UserMessageType messageType) {
		Message = message;
		MessageType = messageType;
	}

}

