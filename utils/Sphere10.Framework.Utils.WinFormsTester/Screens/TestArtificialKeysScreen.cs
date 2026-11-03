// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Demonstrates artificial database key descriptions and their XML persistence.</summary>
public partial class TestArtificialKeysScreen : ApplicationScreen {
	/// <summary>Creates the local schema example without opening a database.</summary>
	public TestArtificialKeysScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		textBox1.Text = "Customers: primary key Id; unique key Email." + Environment.NewLine
			+ "Orders: primary key Id; CustomerId references Customers.Id." + Environment.NewLine
			+ "Serialize and verify displays the exact XML and confirms that reserialization is identical.";
	}

	// The sample describes valid relationships; the demonstration itself performs no database mutation.
	private void _testButton_Click(object sender, EventArgs e) {
		try {
			var keys = new ArtificialKeys {
				Tables = new[] {
					new ArtificialKeys.Table {
						Name = "Customers",
						PrimaryKey = new ArtificialKeys.PrimaryKey {
							Name = "PK_Customers", AutoIncrement = true,
							Columns = new[] { new ArtificialKeys.Column { Name = "Id" } }
						},
						UniqueConstraints = new[] {
							new ArtificialKeys.UniqueConstraint { Name = "UQ_Customers_Email", Columns = new[] { new ArtificialKeys.Column { Name = "Email" } } }
						}
					},
					new ArtificialKeys.Table {
						Name = "Orders",
						PrimaryKey = new ArtificialKeys.PrimaryKey {
							Name = "PK_Orders", Sequence = "OrdersSequence",
							Columns = new[] { new ArtificialKeys.Column { Name = "Id" } }
						},
						ForeignKeys = new[] {
							new ArtificialKeys.ForeignKey {
								Name = "FK_Orders_Customers", ReferenceTable = "Customers",
								Columns = new[] { new ArtificialKeys.Column { Name = "CustomerId", References = "Id" } }
							}
						}
					}
				}
			};
			var xml = Tools.Xml.WriteToString(keys);
			var restored = Tools.Xml.ReadFromString<ArtificialKeys>(xml);
			var identical = string.Equals(xml, Tools.Xml.WriteToString(restored), StringComparison.Ordinal);
			textBox1.Text = xml;
			_status.Text = identical ? "Passed: two tables and all key descriptions round-tripped unchanged." : "Failed: the reserialized XML differs.";
		} catch (Exception error) {
			_status.Text = "Schema round trip failed: " + error.Message;
		}
	}
}
//<ArtificialKeys>
//    <Table name = "Table1">
//        <PrimaryKey name="PK1" autoIncrement="true">
//            <Column name="ID"/>
//        </PrimaryKey>
//
//        <ForeignKey name="FK1" foreignKeyTable="Table2">
//            <Column name="A" linksTo="U">
//            <Column name="B" linksTo="V">
//            <Column name="C" linksTo="W">
//        </ForeignKey>
//
//        <UniqueConstraint name="UC1">
//            <Column>X</Column>
//            <Column>Y</Column>
//        </UniqueConstraint>
//    </Table>
//
//    <Table name="Table2">
//        <PrimaryKey name="PK1" sequence="Sequence1">
//            <Column name="A"/>
//        </PrimaryKey>
//    </Table>
//
//</AritificialKeys>
