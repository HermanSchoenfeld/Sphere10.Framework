// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.Text.FixedLength;

/// <summary>
/// Class for define a field, implements IField interface.
/// </summary>
public class Field : IField {
	public Field(int index, string name, int length, Type type)
		: this(index, name, length, new Sphere10.Framework.ComponentModel.TypeConverterValidator(type)) {
	}

	public Field(int index, string name, int length, Sphere10.Framework.ComponentModel.IValidator validator) {
		Index = index;
		Name = name;
		Length = length;
		Validator = validator;
	}

	public int Index { get; }

	public string Name { get; }

	public int Length {
		get {
			return field;
		}
		set {
			if (value <= 0)
				throw new InvalidFieldLengthException(value);
			field = value;
		}
	}

	public bool TrimBeforeParse { get; set; } = true;

	public Sphere10.Framework.ComponentModel.IValidator Validator {
		get {
			return field;
		}
		set {
			if (value == null)
				throw new ArgumentNullException("Validator");
			field = value;
		}
	}

	public virtual string RegularExpressionPattern {
		get {
			var pattern = "(?<{0}>.{{{1}}})"; //sample:  (?<NOME>.{LENGTH})
			return string.Format(pattern, Name, Length.ToString());
		}
	}

	public virtual string ValueToString(object val) {
		try {
			var temporary = Validator.ValueToString(val);
			if (temporary == null)
				temporary = string.Empty;

			if (temporary.Length > Length)
				throw new ValueNotValidLengthException(temporary, Length);
			if (temporary.Length < Length) {
				if (Validator.AllowNull)
					temporary = temporary.PadRight(Length, ' ');
				else {
					if (Validator.ValueType == typeof(string))
						temporary = temporary.PadRight(Length, ' ');
					else if (Validator.ValueType == typeof(int))
						temporary = temporary.PadLeft(Length, '0'); //N.B. Questo sarebbe da cambiare nel caso in cui l'int abbia il segno!!
					else if (Validator.ValueType == typeof(double))
						throw new ValueNotValidLengthException(temporary, Length);
					else if (Validator.ValueType == typeof(decimal))
						throw new ValueNotValidLengthException(temporary, Length);
					else
						throw new ValueNotSupportedException(temporary, Validator.ValueType);
				}
			}

			return temporary;
		} catch (Exception exception) {
			throw new FieldStringConvertException(Name, val, exception);
		}
	}

	public virtual object StringToValue(string str) {
		try {
			if (TrimBeforeParse && str != null)
				str = str.Trim();

			return Validator.StringToValue(str);
		} catch (Exception exception) {
			throw new FieldParseException(Name, str, exception);
		}
	}
}

//	public class FieldFiller : Field
//	{
//		public FieldFiller(int index, string name, int length):base(index, name, length, typeof(string))
//		{
//		}
//
//		public override string RegularExpressionPattern
//		{
//			get
//			{
//				string pattern = "(?<{0}>.{{1}}){2}";
//				return string.Format(pattern, Name, "*", Tools.FixedLength.ValidateRegExpSeparator(Separator));
//			}
//		}
//	}
