// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CHMHelpProviderTests {
	[Test]
	public void ContextHelpUsesItsContainingFormInsteadOfThePrimaryWindow() {
		using var file = new TemporaryHelpFile();
		using var primary = new Form();
		using var options = new Form();
		using var context = new HelpControl { FileName = file.Path, HelpTopicID = 400 };
		options.Controls.Add(context);
		var provider = new RecordingProvider(primary, file.Path);
		provider.ShowContextHelp(context);
		Assert.That(provider.Owner, Is.SameAs(options));
		Assert.That(provider.File, Is.EqualTo(file.Path));
		Assert.That(provider.TopicId, Is.EqualTo(400));
	}

	[Test]
	public void NonControlContextFallsBackToThePrimaryContainingFormAndDefaultFile() {
		using var file = new TemporaryHelpFile();
		using var primary = new Form();
		using var primaryControl = new Control();
		primary.Controls.Add(primaryControl);
		var provider = new RecordingProvider(primaryControl, file.Path);
		provider.ShowContextHelp(new HelpObject { FileName = file.Path + ".missing", HelpTopicID = 700 });
		Assert.That(provider.Owner, Is.SameAs(primary));
		Assert.That(provider.File, Is.EqualTo(file.Path));
		Assert.That(provider.TopicId, Is.EqualTo(700));
	}

	[Test]
	public void DefaultHelpUsesThePrimaryFormWithoutAContextTopic() {
		using var file = new TemporaryHelpFile();
		using var primary = new Form();
		var provider = new RecordingProvider(primary, file.Path);
		provider.ShowHelp();
		Assert.That(provider.Owner, Is.SameAs(primary));
		Assert.That(provider.File, Is.EqualTo(file.Path));
		Assert.That(provider.TopicId, Is.Null);
	}

	[TestCase(-1920, 0, 1920, 1040, -1800, 100, 600, 500, 900, 700, -1920, 0, 900, 700)]
	[TestCase(0, 40, 3840, 2060, 1000, 500, 900, 750, 1200, 900, 850, 425, 1200, 900)]
	[TestCase(2560, -1200, 1920, 1160, 4000, -300, 600, 500, 900, 700, 3580, -740, 900, 700)]
	[TestCase(-1280, 0, 1280, 720, -1200, 100, 500, 500, 1800, 1000, -1280, 0, 1280, 720)]
	public void ViewerBoundsFitTheOwnersPhysicalMonitorWithNegativeOriginsAndDifferentDpiSizes(
		int workX, int workY, int workWidth, int workHeight, int ownerX, int ownerY, int ownerWidth, int ownerHeight,
		int helpWidth, int helpHeight, int expectedX, int expectedY, int expectedWidth, int expectedHeight
	) {
		var result = RecordingProvider.Place(new Rectangle(workX, workY, workWidth, workHeight), new Rectangle(ownerX, ownerY, ownerWidth, ownerHeight), new Size(helpWidth, helpHeight));
		Assert.That(result, Is.EqualTo(new Rectangle(expectedX, expectedY, expectedWidth, expectedHeight)));
	}


	private sealed class RecordingProvider : CHMHelpProvider {
		public RecordingProvider(Control primary, string file)
			: base(new TestUserInterfaceServices(primary), new TestProductInformationProvider(file)) {
		}

		public Control Owner { get; private set; }
		public string File { get; private set; }
		public int? TopicId { get; private set; }

		public static Rectangle Place(Rectangle workingArea, Rectangle ownerBounds, Size helpSize) => GetHelpWindowBounds(workingArea, ownerBounds, helpSize);

		protected override void ShowHelp(Control owner, string file, int? topicId) {
			Owner = owner;
			File = file;
			TopicId = topicId;
		}
	}

	private sealed class TestUserInterfaceServices : UserInterfaceServicesDecorator {
		public TestUserInterfaceServices(Control primary)
			: base(new ConsoleApplicationUserInterfaceServices()) {
			PrimaryUIController = primary;
		}

		public override object PrimaryUIController { get; }
	}

	private sealed class TestProductInformationProvider : IProductInformationProvider {
		public TestProductInformationProvider(string file) {
			ProductInformation = new ProductInformation { HelpResources = new[] { Tuple.Create(HelpType.CHM, file) } };
		}

		public ProductInformation ProductInformation { get; }
	}

	private sealed class TemporaryHelpFile : IDisposable {
		public TemporaryHelpFile() {
			Path = System.IO.Path.GetTempFileName();
		}

		public string Path { get; }

		public void Dispose() => System.IO.File.Delete(Path);
	}

	private sealed class HelpControl : Control, IHelpableObject {
		public HelpType Type => HelpType.CHM;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string FileName { get; set; }
		public string Url => null;
		public int? PageNumber => null;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int? HelpTopicID { get; set; }
		public int? HelpTopicAlias => null;
	}

	private sealed class HelpObject : IHelpableObject {
		public HelpType Type => HelpType.CHM;
		public string FileName { get; set; }
		public string Url => null;
		public int? PageNumber => null;
		public int? HelpTopicID { get; set; }
		public int? HelpTopicAlias => null;
	}
}
