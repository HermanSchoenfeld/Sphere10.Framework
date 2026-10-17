// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// TextWriter which appends to a file.
/// </summary>
/// <remarks></remarks>
public class FileAppendTextWriter : TextWriterBase {

	/// <summary>
	/// This is the default encoding used by StreamWriter, which File.AppendAllText uses internally.
	/// </summary>
	private static readonly Encoding _defaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
	private readonly Encoding _encoding;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileAppendTextWriter"/> class.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <remarks></remarks>
	public FileAppendTextWriter(string filePath, bool createIfMissing = false)
		: this(filePath, _defaultEncoding, createIfMissing) {
	}

	public FileAppendTextWriter(string filePath, Encoding encoding, bool createIfMissing) {
		Guard.ArgumentNotNull(encoding, nameof(encoding));
		_encoding = encoding;
		FilePath = filePath;
		if (createIfMissing && !File.Exists(FilePath))
			Tools.FileSystem.CreateBlankFile(FilePath, true);
	}

	public string FilePath { get; set; }

	public override Encoding Encoding => _encoding;

	protected override void InternalWrite(string value)
		=> File.AppendAllText(FilePath, value, _encoding);

	protected override Task InternalWriteAsync(string value)
		=> File.AppendAllTextAsync(FilePath, value, _encoding);

}

