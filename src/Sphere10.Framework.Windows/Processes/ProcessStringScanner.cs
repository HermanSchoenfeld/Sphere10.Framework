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

/// <summary>Bounded streaming ASCII/UTF-16/UTF-8 extraction shared by process image and virtual-memory reads.</summary>
internal sealed class ProcessStringScanner {
	private const int MaximumStringLength = 4096;
	private readonly int _minimumLength;
	private readonly int _maximumResults;
	private readonly List<WindowsProcessString> _strings = new();
	private readonly Run _ascii = new();
	private readonly Run _evenUnicode = new();
	private readonly Run _oddUnicode = new();
	private readonly Run _utf8 = new();
	private bool _utf8HasMultibyte;
	private bool _utf8Truncated;
	private int _utf8Continuation;
	private int _utf8Scalar;
	private int _utf8MinimumScalar;
	private ulong _utf8SequenceStart;
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
			AppendUtf8(value, position);
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
		FlushUtf8();
		_utf8Continuation = 0;
		_hasPrevious = false;
	}

	private void AppendUtf8(byte value, ulong address) {
		if (_utf8Continuation > 0) {
			if ((value & 0xc0) == 0x80) {
				_utf8Scalar = (_utf8Scalar << 6) | (value & 0x3f);
				if (--_utf8Continuation != 0)
					return;
				if (_utf8Scalar < _utf8MinimumScalar || !Rune.IsValid(_utf8Scalar)) {
					FlushUtf8();
					return;
				}
				var rune = new Rune(_utf8Scalar);
				var category = Rune.GetUnicodeCategory(rune);
				if (category is UnicodeCategory.Control or UnicodeCategory.OtherNotAssigned or UnicodeCategory.Format or UnicodeCategory.PrivateUse) {
					FlushUtf8();
					return;
				}
				if (_utf8.Length == 0)
					_utf8.Address = _utf8SequenceStart;
				_utf8HasMultibyte = true;
				_utf8.Length++;
				if (_utf8.Text.Length + rune.Utf16SequenceLength <= MaximumStringLength)
					_utf8.Text.Append(rune.ToString());
				else
					_utf8Truncated = true;
				return;
			}
			// An invalid continuation terminates the previous run; this byte may start a new one.
			_utf8Continuation = 0;
			FlushUtf8();
		}
		if (value < 0x80) {
			if (value < 32 || value > 126) {
				FlushUtf8();
				return;
			}
			if (_utf8.Length == 0)
				_utf8.Address = address;
			_utf8.Length++;
			if (_utf8.Text.Length < MaximumStringLength)
				_utf8.Text.Append((char)value);
			else
				_utf8Truncated = true;
			return;
		}
		_utf8SequenceStart = address;
		if (value is >= 0xc2 and <= 0xdf) {
			_utf8Scalar = value & 0x1f;
			_utf8Continuation = 1;
			_utf8MinimumScalar = 0x80;
		} else if (value is >= 0xe0 and <= 0xef) {
			_utf8Scalar = value & 0xf;
			_utf8Continuation = 2;
			_utf8MinimumScalar = 0x800;
		} else if (value is >= 0xf0 and <= 0xf4) {
			_utf8Scalar = value & 7;
			_utf8Continuation = 3;
			_utf8MinimumScalar = 0x10000;
		} else
			FlushUtf8();
	}

	private void FlushUtf8() {
		// Pure ASCII already has one canonical result; emit UTF-8 only when a complete non-ASCII scalar occurred.
		if (_utf8HasMultibyte && !IsFull && _utf8.Length >= _minimumLength)
			_strings.Add(new WindowsProcessString(_utf8.Address, _utf8.Text.ToString(), false, _utf8Truncated) { Encoding = "UTF-8" });
		_utf8.Text.Clear();
		_utf8.Length = 0;
		_utf8HasMultibyte = false;
		_utf8Truncated = false;
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

	#region Inner Types

	private sealed class Run {
		public StringBuilder Text { get; } = new();
		public ulong Address { get; set; }
		public long Length { get; set; }
	}

	#endregion
}

