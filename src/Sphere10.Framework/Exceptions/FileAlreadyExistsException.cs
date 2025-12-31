// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public class FileAlreadyExistsException : SoftwareException {
	public FileAlreadyExistsException(string filename)
		: this($"File already exists", filename) {

	}

	public FileAlreadyExistsException(string message, string filename)
		: base(message) {
		Path = filename;
	}

	public string Path { get; }
}

