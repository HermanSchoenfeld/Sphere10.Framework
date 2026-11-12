// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Serialization;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Groups native list operations and explicit Framework utility experiments.</summary>
public partial class MiscTestScreen : ApplicationScreen {
	private CancellationTokenSource _compressionCancellation;

	public MiscTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	#region Sqlite

	public string CreateTempSqliteDB() {
		var table = "CREATE TABLE IF NOT EXISTS TTC (id INTEGER PRIMARY KEY, Route_ID TEXT, Branch_Code TEXT, Version INTEGER, Stop INTEGER, Vehicle_Index INTEGER, Day Integer, Time TEXT)";
		var tempFile = string.Format("{0}{1}{2}", Path.GetTempPath(), Path.DirectorySeparatorChar, Guid.NewGuid());
		var dac = Tools.Sqlite.Create(tempFile);
		using (dac.EnterScope()) {
			dac.ExecuteNonQuery(table);
		}
		return tempFile;
	}

	public void SqliteDACInsertTest(string path, int numInserts) {
		var dac = Tools.Sqlite.Open(path);
		using (dac.EnterScope(true)) {
			for (var index = 0; index < numInserts; index++)
				dac.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
		}
	}

	public void SqliteDACExecuteBatch(string path, int numInserts) {
		var dac = Tools.Sqlite.Open(path);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
			dac.ExecuteBatch(builder);
		}
	}

	public void SqliteDACExecuteBatch2(string path, int numInserts) {
		var dac = Tools.Sqlite.Open(path);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });

			dac.ExecuteNonQuery(builder.ToString());
		}
	}

	public void SqliteDACExecuteBatchManualTransaction(string path, int numInserts) {
		var dac = Tools.Sqlite.Open(path);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			builder.BeginTransaction();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
			builder.CommitTransaction();
			dac.ExecuteBatch(builder);
		}
	}

	#endregion

	#region SQL Server

	public string CreateTempMSSQLDB() {
		var table = "CREATE TABLE TTC (id INTEGER PRIMARY KEY IDENTITY, Route_ID VARCHAR(255), Branch_Code VARCHAR(255), Version INTEGER, Stop INTEGER, Vehicle_Index INTEGER, Day Integer, Time VARCHAR(255))";
		var dbName = Guid.NewGuid().ToStrictAlphaString();
		Tools.MSSQL.CreateDatabase("localhost", dbName, "sa", null, false);
		var dac = Tools.MSSQL.Open("localhost", dbName, "sa", null);
		using (dac.EnterScope()) {
			dac.ExecuteNonQuery(table);
		}
		return dbName;
	}

	public void SqlServerDACInsertTest(string dbName, int numInserts) {
		var dac = Tools.MSSQL.Open("localhost", dbName, "sa", null);
		;
		using (dac.EnterScope(true)) {
			for (var index = 0; index < numInserts; index++)
				dac.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
		}
	}

	public void SqlServerDACExecuteBatch(string dbName, int numInserts) {
		var dac = Tools.MSSQL.Open("localhost", dbName, "sa", null);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
			dac.ExecuteBatch(builder);
		}
	}

	public void SqlServerDACExecuteBatch2(string dbName, int numInserts) {
		var dac = Tools.MSSQL.Open("localhost", dbName, "sa", null);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });

			dac.ExecuteNonQuery(builder.ToString());
		}
	}

	public void SqlServerDACExecuteBatchManualTransaction(string dbName, int numInserts) {
		var dac = Tools.MSSQL.Open("localhost", dbName, "sa", null);
		using (dac.EnterScope()) {
			var builder = dac.CreateSQLBuilder();
			builder.BeginTransaction();
			for (var index = 0; index < numInserts; index++)
				builder.Insert("TTC", new[] { new ColumnValue("Route_ID", Guid.NewGuid().ToString()), });
			builder.CommitTransaction();
			dac.ExecuteBatch(builder);
		}
	}

	#endregion

	/// <summary>Loads a small merger example without starting any optional utility or database operation.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode)
			Button1_Click(this, EventArgs.Empty);
	}

	/// <summary>Stops the screen-owned comparison on actual destruction, including direct disposal.</summary>
	protected override void OnDestroyScreen() {
		_compressionCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	private void Button1_Click(object sender, EventArgs args) {
		var objs = new[] { new TestObject { ID = 1, Name = "One" }, new TestObject { ID = 2, Name = "Two" }, new TestObject { ID = 3, Name = "Three" } };
		_listMerger1.DisplayMember = "Name";
		_listMerger1.ValueMember = "ID";
		_listMerger1.LeftItems = objs;
	}

	private void ClipTestButton_Click(object sender, EventArgs args) {
		var teststring = "01234567890123456789012345678901234567890123456789";
		var writer = new TextBoxWriter(_outputTextBox);

		writer.WriteLine(teststring);
		for (var index = 0; index < teststring.Length + 10; index++)
			writer.WriteLine(teststring.Clip(index));
	}

	private void ScopedContextTestButton_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);

		TestScopePolicy(writer, ContextScopePolicy.None, ContextScopePolicy.None);
		TestScopePolicy(writer, ContextScopePolicy.None, ContextScopePolicy.MustBeNested);
		TestScopePolicy(writer, ContextScopePolicy.None, ContextScopePolicy.MustBeRoot);

		TestScopePolicy(writer, ContextScopePolicy.MustBeNested, ContextScopePolicy.None);
		TestScopePolicy(writer, ContextScopePolicy.MustBeNested, ContextScopePolicy.MustBeNested);
		TestScopePolicy(writer, ContextScopePolicy.MustBeNested, ContextScopePolicy.MustBeRoot);

		TestScopePolicy(writer, ContextScopePolicy.MustBeRoot, ContextScopePolicy.None);
		TestScopePolicy(writer, ContextScopePolicy.MustBeRoot, ContextScopePolicy.MustBeNested);
		TestScopePolicy(writer, ContextScopePolicy.MustBeRoot, ContextScopePolicy.MustBeRoot);
	}

	private static void TestScopePolicy(TextBoxWriter writer, ContextScopePolicy rootPolicy, ContextScopePolicy childPolicy) {
		try {
			ScopeContextTest.TextWriter = writer;
			writer.WriteLine("Testing Scoped Context: RootPolicy={0}, ChildPolicy={1}", rootPolicy, childPolicy);
			using (new ScopeContextTest(rootPolicy)) {
				using (new ScopeContextTest(childPolicy)) {
					using (new ScopeContextTest(childPolicy)) {
					}
				}
			}
		} catch (Exception error) {
			writer.WriteLine("Exception: {0}", error.ToDisplayString());
		} finally {
			writer.WriteLine();
			writer.WriteLine();
		}
	}

	// The original compression comparison runs on detached values, then publishes one UI-owned result.
	private async void CompressTestButton_Click(object sender, EventArgs args) {
		if (_compressionCancellation != null) {
			_compressionCancellation.Cancel();
			return;
		}

		using var cancellation = new CancellationTokenSource();
		_compressionCancellation = cancellation;
		var cancellationToken = cancellation.Token;

		try {
			// Keep Cancel and the rest of the workspace usable while the detached worker runs.
			_compressTestButton.Text = "Cancel comparison";
			_outputTextBox.AppendText("Comparing four sample sizes. Click Cancel comparison to stop." + Environment.NewLine);
			var text = await Task.Run(() => CompareCompression(cancellationToken), cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText(text);
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText("Compression comparison cancelled." + Environment.NewLine);
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText("Compression comparison failed: " + error.Message + Environment.NewLine);
		} finally {
			_compressionCancellation = null;
			if (!IsDisposed && !Disposing)
				_compressTestButton.Text = "Compression comparison";
		}
	}

	private void PathTemplatesButton_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);
		try {
			var templates = new[] { "{Personal}{/}Alpha{/}Beta{/}Gamma" };

			foreach (var template in templates) {
				writer.Write(template);
				writer.Write(" -> ");
				writer.WriteLine(Tools.FileSystem.ResolvePathTemplate(template));
			}
		} catch (Exception error) {
			writer.WriteLine(error.ToDiagnosticString());
		}
	}

	private void SqliteTestButton_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);
		try {

			for (var index = 100; index <= 10000; index = index * 10) {

				var db = CreateTempSqliteDB();
				var start = DateTime.Now;
				SqliteDACInsertTest(db, index);
				writer.WriteLine("{0} unbatched inserts no transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				File.Delete(db);

				db = CreateTempSqliteDB();
				start = DateTime.Now;
				SqliteDACExecuteBatch(db, index);
				writer.WriteLine("{0} batched inserts no transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				File.Delete(db);

				db = CreateTempSqliteDB();
				start = DateTime.Now;
				SqliteDACExecuteBatch2(db, index);
				writer.WriteLine("{0} batched inserts 2 no transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				File.Delete(db);

				db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {

					start = DateTime.Now;
					SqliteDACInsertTest(db, index);
					t.Complete();
					writer.WriteLine("{0} unbatched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);

				db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqliteDACExecuteBatch(db, index);
					t.Complete();
					writer.WriteLine("{0} optimized batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);

				db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqliteDACExecuteBatch2(db, index);
					t.Complete();
					writer.WriteLine("{0} standard batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);
			}
		} catch (Exception error) {
			writer.WriteLine(error.ToDiagnosticString());
		}
	}

	private void Button2_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);
		try {

			for (var index = 100; index <= 10000; index = index * 10) {

				DateTime start;
				var db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {

					start = DateTime.Now;
					SqliteDACInsertTest(db, index);
					t.Complete();
					writer.WriteLine("{0} unbatched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);

				db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqliteDACExecuteBatch(db, index);
					t.Complete();
					writer.WriteLine("{0} optimized batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);

				db = CreateTempSqliteDB();
				start = DateTime.Now;
				SqliteDACExecuteBatchManualTransaction(db, index);
				writer.WriteLine("{0} optimized batched inserts with manual transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				File.Delete(db);

				db = CreateTempSqliteDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqliteDACExecuteBatch2(db, index);
					t.Complete();
					writer.WriteLine("{0} standard batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				File.Delete(db);
			}
		} catch (Exception error) {
			writer.WriteLine(error.ToDiagnosticString());
		}
	}

	private void SqlServerTest_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);
		try {

			for (var index = 100; index <= 10000; index = index * 10) {

				DateTime start;
				var db = CreateTempMSSQLDB();
				using (var t = new TransactionScope()) {

					start = DateTime.Now;
					SqlServerDACInsertTest(db, index);
					t.Complete();
					writer.WriteLine("{0} unbatched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				Tools.MSSQL.DropDatabase("localhost", db, "sa", null, false);

				db = CreateTempMSSQLDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqlServerDACExecuteBatch(db, index);
					t.Complete();
					writer.WriteLine("{0} optimized batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				Tools.MSSQL.DropDatabase("localhost", db, "sa", null, false);

				db = CreateTempMSSQLDB();
				start = DateTime.Now;
				SqlServerDACExecuteBatchManualTransaction(db, index);
				writer.WriteLine("{0} optimized batched inserts with manual transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				Tools.MSSQL.DropDatabase("localhost", db, "sa", null, false);

				db = CreateTempMSSQLDB();
				using (var t = new TransactionScope()) {
					start = DateTime.Now;
					SqlServerDACExecuteBatch2(db, index);
					t.Complete();
					writer.WriteLine("{0} standard batched inserts with transaction = {1}", index, DateTime.Now.Subtract(start).TotalMilliseconds);
				}
				Tools.MSSQL.DropDatabase("localhost", db, "sa", null, false);
			}
		} catch (Exception error) {
			writer.WriteLine(error.ToDiagnosticString());
		}
	}

	private async void SystemPathsButton_Click(object sender, EventArgs args) {
		var writer = new TextBoxWriter(_outputTextBox);
		try {
			foreach (var specialFolder in Tools.Enums.GetValues<Environment.SpecialFolder>())
				await writer.WriteLineAsync($"{specialFolder} -> {Environment.GetFolderPath(specialFolder)}");
		} catch (Exception error) {
			writer.WriteLine(error.ToDiagnosticString());
		}
	}

	private async void Button3_Click(object sender, EventArgs args) {
		try {
			//var writer = new TextBoxWriter(_clipTestTextBox);
			var xxx =
				@"<?xml version=""1.0"" encoding=""utf-8""?>
<object name="""" type=""TK0"" assembly="""">
  <!-- Data section : Don't edit any attributes ! -->
  <properties>
    <property name=""Keys"" type=""TK1"" assembly="""">
      <items />
    </property>
    <property name=""Values"" type=""TK1"" assembly="""">
      <items />
    </property>
  </properties>
  <!-- TypeDictionary : Don't edit anything in this section at all ! -->
  <typedictionary name="""" type=""System.Collections.Hashtable"" assembly=""mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"">
    <items>
      <item>
        <properties />
      </item>
      <item>
        <properties />
      </item>
    </items>
  </typedictionary>
</object>";
			var x = new XmlDeepDeserializer();
			x.Deserialize(new StringReader(xxx));
			_outputTextBox.AppendText("XML sample deserialized successfully." + Environment.NewLine);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void MacAddressesButton_Click(object sender, EventArgs args) {
		try {
			var writer = new TextBoxWriter(_outputTextBox);
			foreach (var macAddr in Tools.Network.GetMacAddresses())
				await writer.WriteLineAsync(macAddr);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void MappingTests_Click(object sender, EventArgs args) {
		try {

			var writer = new TextBoxWriter(_outputTextBox);

			var member = Tools.Mapping.GetMember<TestClass>(x => x.Name);

			writer.WriteLine(member);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// Four representative sizes demonstrate the same codecs without a thousand repeated pipeline runs.
	private static string CompareCompression(CancellationToken cancellationToken) {
		var output = new StringBuilder();
		const string seed = "A repeated demonstration record makes compression and encryption costs comparable. ";
		const string password = "password";
		const string salt = "asdfasasdfasdfasdfasdfasdfasdf";

		foreach (var sampleSize in new[] { 1, 10, 100, 1000 }) {
			cancellationToken.ThrowIfCancellationRequested();
			var value = new StringBuilder(seed);
			for (var item = 0; item < sampleSize; item++)
				value.Append(Tools.Maths.RNG.Next(0, 10000));
			var plain = value.ToString();

			// Cancellation is observed between synchronous codecs; they borrow no native controls.
			var compressedEncrypted = Tools.Text.CompressText(plain, password);
			cancellationToken.ThrowIfCancellationRequested();
			var compressed = Tools.Text.CompressText(plain);
			cancellationToken.ThrowIfCancellationRequested();
			var encrypted = Tools.Crypto.EncryptStringAES(plain, password, salt);
			cancellationToken.ThrowIfCancellationRequested();

			var decompressed = Tools.Text.DecompressText(compressed);
			cancellationToken.ThrowIfCancellationRequested();
			var decryptedDecompressed = Tools.Text.DecompressText(compressedEncrypted, password);
			cancellationToken.ThrowIfCancellationRequested();
			var decrypted = Tools.Crypto.DecryptStringAES(encrypted, password, salt);
			cancellationToken.ThrowIfCancellationRequested();
			var roundTrip = plain == decompressed && plain == decryptedDecompressed && plain == decrypted;
			output.AppendLine(
				$"Original: {plain.Length:N0} · Compressed: {compressed.Length:N0} · Compressed/encrypted: {compressedEncrypted.Length:N0} · Encrypted: {encrypted.Length:N0} · Round trip: {roundTrip}"
			);
		}
		return output.ToString();
	}

	public class TestObject {
		public int ID {
			get; set;
		}

		public string Name {
			get; set;
		}
	}

	public class ScopeContextTest : SyncContextScope {
		public static TextWriter TextWriter;

		public ScopeContextTest(ContextScopePolicy policy)
			: base(policy, "myTlsSlotName") {
			if (IsRootScope)
				TextWriter.WriteLine("[Begin Root Scope]");
			else
				TextWriter.WriteLine("[Begin Child Scope]");
		}

		protected override void OnScopeEndInternal() => TextWriter.WriteLine(RootScope != this ? "[End Child Scope]" : "[End Root Scope]");

		protected override void OnContextEnd() => TextWriter.WriteLine("Context end");
	}

	[XmlRoot("Menu")]
	public class Menu {
		public Menu() {
			SubMenus = new Menu[0];
			Url = "#";
		}

		public Menu(string text, string url, string glyph = null)
			: this() {
			Text = text;
			Url = url;
			Glyph = glyph;
		}

		[XmlAttribute("text")]
		public string Text {
			get; set;
		}

		[XmlAttribute("url")]
		public string Url {
			get; set;
		}

		[XmlAttribute("open")]
		public bool Open {
			get; set;
		}

		[XmlAttribute("icon")]
		public string FontAwesomeIcon {
			get; set;
		}

		[XmlAttribute("glyph")]
		public string Glyph {
			get; set;
		}

		[XmlElement("Menu")]
		public Menu[] SubMenus {
			get; set;
		}
	}

	public class SideBarModelBuilder {
		public Menu CraeteDefaultSideBarModel() {
			return new Menu {
				SubMenus =
					new[] { CreateDashboardMenu() }
						.Concat(CreateAssetsMenu())
						.Concat(CreateUsersMenu())
						.Concat(CreateAccountsMenu())
						.Concat(CreateTransactionsMenu())
						.Concat(CreateCardMenu())
						.Concat(CreateTransformersMenu())
						.Concat(CreateAuditMenu())
						.Concat(CreateReportsMenu())
						.Concat(CreateConfigurationMenu())
						.ToArray()
			};
		}

		protected Menu CreateDashboardMenu() {
			return new Menu {
				Text = "Dashboard",
				FontAwesomeIcon = "fa-home",
				Url = "/Dashboard"
			};
		}

		protected Menu CreateAssetsMenu() {
			return new Menu {
				Text = "Assets",
				FontAwesomeIcon = "fa-btc",
				SubMenus = new[] {
					new Menu("Asset Types", "/Assets"),
					new Menu("Issue Asset", "/Assets/Issue"),
				}
			};
		}

		protected Menu CreateUsersMenu() {
			return new Menu {
				Text = "Users",
				FontAwesomeIcon = "fa-user",
				SubMenus = new[] {
					new Menu("Persons", "/Persons"),
					new Menu("{Agents}", "/Agents"),
					new Menu("Auditors", "/Auditors"),
				}
			};
		}

		protected Menu CreateAccountsMenu() {
			return new Menu {
				Text = "Accounts",
				FontAwesomeIcon = "fa-money",
				SubMenus = new[] {
					new Menu("Accounts", "/Accounts"),
					new Menu("Endpoints", "/Endpoints"),
					new Menu("Transactions", "/Transactions"),
				}
			};
		}

		protected Menu CreateTransactionsMenu() {
			return new Menu {
				Text = "Transactions",
				FontAwesomeIcon = "fa-money",
				SubMenus = new[] {
					new Menu("Transactions", "/Transactions"),
					new Menu("Redeem Codes", "/RedeemCodes"),
				}
			};
		}

		protected Menu CreateCardMenu() {
			return new Menu {
				Text = "Cards",
				FontAwesomeIcon = "fa-credit-card",
				SubMenus = new[] {
					new Menu("View Cards", "/Cards"),
					new Menu("New Card", "/Cards/New"),
				}
			};
		}

		protected Menu CreateTransformersMenu() {
			return new Menu {
				Text = "Transformers",
				FontAwesomeIcon = "fa-balance-scale",
				SubMenus = new[] {
					new Menu("View Transformers", "/Transformers"),
					new Menu("New Transformer", "/Transformers/New"),
				}
			};
		}

		protected Menu CreateAuditMenu() {
			return new Menu {
				Text = "Audit",
				FontAwesomeIcon = "fa-shield",
				SubMenus = new[] {
					new Menu("Audits", "/Audits"),
				}
			};
		}

		protected Menu CreateReportsMenu() {
			return new Menu {
				Text = "Reports",
				FontAwesomeIcon = "fa-area-chart",
				SubMenus = new[] {
					new Menu("AML Compliance", "/Reports/AML"),
					new Menu("Agent Reconcilliation", "/Reports/AgentReconcilliation"),
				}
			};
		}

		protected Menu CreateConfigurationMenu() {
			return new Menu {
				Text = "Configuration",
				FontAwesomeIcon = "fa-gear",
				SubMenus = new[] {
					new Menu("Settings", "/Settings"),
					new Menu("Blockchain Engine", "/Engine"),
					new Menu("Communications", "/Communications")
				}
			};
		}
	}
}
