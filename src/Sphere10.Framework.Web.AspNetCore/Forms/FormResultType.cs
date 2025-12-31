// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Runtime.Serialization;

namespace Sphere10.Framework.Web.AspNetCore;

public enum FormResultType {

	[EnumMember(Value = "message")] ShowMessage,

	[EnumMember(Value = "redirect")] Redirect,

	[EnumMember(Value = "replace_page")] ReplacePage,

	[EnumMember(Value = "replace_form")] ReplaceForm
}

