// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

public class TestClass {
	private static readonly string[] Names = { "Bitcoin", "Ethereum", "Polkadot", "Litecoin" };

	public int Id { get; set; }

	public string Name { get; set; } = "New item";

	public TestEnum Color { get; set; }

	public DateTime CreationDate { get; set; }

	public decimal Age { get; set; }

	public string Details { get; set; } = "Test Details";

	public string Note { get; set; } = "Test Note";

	public void FillWithTestData(int id) {
		Id = id;
		Name = Names[id % Names.Length];
		Color = (TestEnum)(id % 6);
		CreationDate = new DateTime(2020 + id % 5, id % 12 + 1, 1);
		Age = id % 100;
	}

	public override string ToString() => $"Id: {Id} Name: {Name} Color: {Color} CreationDate: {CreationDate:d} Age: {Age} Details: {Details} Note: {Note}";

	public enum TestEnum {
		Black,
		White,
		Yellow,
		Purple,
		Brown,
		Blue
	}
}
