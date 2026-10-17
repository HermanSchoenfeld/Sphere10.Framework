// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sphere10.Framework.Windows;

/// <summary>Bounded streaming ASCII/UTF-16 extraction shared by process image and virtual-memory reads.</summary>
internal sealed class ProcessStringScanner {
	private const int MaximumStringLength = 4096;
	private readonly int _minimumLength;
	private readonly int _maximumResults;
	private readonly List<WindowsProcessString> _strings = new();
	private readonly Run _ascii = new();
	private readonly Run _evenUnicode = new();
	private readonly Run _oddUnicode = new();
	private byte _previous;
	private bool _hasPrevious;

	public ProcessStringScanner(int minimumLength, int maximumResults) {
		_minimumLength = minimumLength;
		_maximumResults = maximumResults;
	}

	public bool IsFull => _strings.Count >= _maximumResults;

	public WindowsProcessString[] Strings => _strings.ToArray();

	public void Feed(byte[] buffer, int count, ulong address, Action checkpoint) {
		for (var index = 0; index < count && !IsFull; index++) {
			if ((index & 4095) == 0)
				checkpoint?.Invoke();
			var value = buffer[index];
			var position = address + (uint)index;
			Append(_ascii, value >= 32 && value <= 126 ? (char)value : '\0', position, false);
			if (_hasPrevious) {
				var character = (char)(_previous | value << 8);
				var run = ((position - 1) & 1) == 0 ? _evenUnicode : _oddUnicode;
				var category = char.GetUnicodeCategory(character);
				var printable = character >= 32 && !char.IsControl(character) && !char.IsSurrogate(character)
					&& category is not UnicodeCategory.OtherNotAssigned and not UnicodeCategory.Format and not UnicodeCategory.PrivateUse;
				Append(run, printable ? character : '\0', position - 1, true);
			}
			_previous = value;
			_hasPrevious = true;
		}
	}

	public void EndRegion() {
		Flush(_ascii, false);
		Flush(_evenUnicode, true);
		Flush(_oddUnicode, true);
		_hasPrevious = false;
	}

	private void Append(Run run, char character, ulong address, bool unicode) {
		if (character == '\0') {
			Flush(run, unicode);
			return;
		}
		if (run.Length == 0)
			run.Address = address;
		run.Length++;
		if (run.Text.Length < MaximumStringLength)
			run.Text.Append(character);
	}

	private void Flush(Run run, bool unicode) {
		if (!IsFull && run.Length >= _minimumLength)
			_strings.Add(new WindowsProcessString(run.Address, run.Text.ToString(), unicode, run.Length > MaximumStringLength));
		run.Text.Clear();
		run.Length = 0;
	}

	private sealed class Run {
		public StringBuilder Text { get; } = new();
		public ulong Address { get; set; }
		public long Length { get; set; }
	}
}

