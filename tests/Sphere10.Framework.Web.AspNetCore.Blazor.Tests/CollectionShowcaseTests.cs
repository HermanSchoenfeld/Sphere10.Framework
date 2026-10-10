// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Cards;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CollectionShowcaseTests {
	[Test]
	public void CatalogueReadsDetachBothMembershipAndEditableValues() {
		var catalogue = new CollectionCatalogue();
		var first = catalogue.Read("coastal-notes");
		var second = catalogue.Read("coastal-notes");
		var all = catalogue.ReadAll();
		first.Name = "Uncommitted draft";
		all.Single(item => item.Id == "coastal-notes").Owner = "Uncommitted owner";
		all[0] = null;

		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(second.Name, Is.EqualTo("Coastal notes"));
		Assert.That(catalogue.Read("coastal-notes").Owner, Is.EqualTo("Alex Morgan"));
		Assert.That(catalogue.ReadAll(), Has.Length.EqualTo(9).And.None.Null);
	}

	[Test]
	public void SaveCopiesTheDraftAndRaisesOneNotificationAfterNormalizingItsNames() {
		var catalogue = new CollectionCatalogue();
		var draft = catalogue.Read("coastal-notes");
		var notifications = 0;
		CreativeCollection observed = null;
		catalogue.Changed += () => {
			notifications++;
			observed = catalogue.Read(draft.Id);
		};
		draft.Name = "  Saved coastal notes  ";
		draft.Owner = "  Morgan Lee  ";
		draft.Progress = 83;
		catalogue.Save(draft);
		draft.Name = "Another unsaved change";
		draft.Progress = 10;

		var saved = catalogue.Read("coastal-notes");
		Assert.That(saved.Name, Is.EqualTo("Saved coastal notes"));
		Assert.That(saved.Owner, Is.EqualTo("Morgan Lee"));
		Assert.That(saved.Progress, Is.EqualTo(83));
		Assert.That(saved.Updated, Is.EqualTo(DateTime.Today));
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(observed.HasSameEdits(saved), Is.True, "Observers must read the saved state after the write scope has ended.");
	}

	[Test]
	public void ActualDemoRegistrationKeepsCatalogueChangesWithinTheOwningScope() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		Program.ConfigureServices(services);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var first = firstScope.ServiceProvider.GetRequiredService<CollectionCatalogue>();
		var second = secondScope.ServiceProvider.GetRequiredService<CollectionCatalogue>();
		var draft = first.Read("coastal-notes");
		draft.Name = "First session only";
		first.Save(draft);

		Assert.That(firstScope.ServiceProvider.GetRequiredService<CollectionCatalogue>(), Is.SameAs(first));
		Assert.That(second, Is.Not.SameAs(first));
		Assert.That(first.Read("coastal-notes").Name, Is.EqualTo("First session only"));
		Assert.That(second.Read("coastal-notes").Name, Is.EqualTo("Coastal notes"));
	}

	[TestCase("  aLeX  ", "COAST", "Active", "Photography", "coastal-notes")]
	[TestCase("material", "Taylor", "Active", "Research", "material-library")]
	[TestCase("material", "Alex", "Active", "Research", "")]
	[TestCase("", "reference", "Active", "Research", "material-library")]
	[TestCase("", "", "Archived", "Photography", "quiet-interiors|summer-archive")]
	[TestCase("sam", "", "Draft", "Illustration", "orbital-studies|sound-and-shape")]
	public void SearchStatusAndCategoryComposeBeforePaging(string filter, string search, string status, string category, string expectedIds) {
		var source = new CollectionDataSource(new CollectionCatalogue()) { FilterText = filter, StatusFilter = status, CategoryFilter = category };
		var result = source.ReadRange(search, 9, 0, nameof(CreativeCollection.Name), SortDirection.Ascending);
		var expected = expectedIds.Split('|', StringSplitOptions.RemoveEmptyEntries);

		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(expected));
		Assert.That(result.TotalCount, Is.EqualTo(expected.Length));
	}

	[TestCase(nameof(CreativeCollection.Progress), SortDirection.Descending, "quiet-interiors|summer-archive|city-in-colour")]
	[TestCase(nameof(CreativeCollection.Owner), SortDirection.Ascending, "coastal-notes|quiet-interiors|summer-archive")]
	[TestCase(nameof(CreativeCollection.Name), SortDirection.Descending, "weekend-sketches|summer-archive|sound-and-shape")]
	[TestCase(nameof(CreativeCollection.Updated), SortDirection.Descending, "coastal-notes|orbital-studies|city-in-colour")]
	[TestCase(nameof(CreativeCollection.ItemCount), SortDirection.Ascending, "sound-and-shape|orbital-studies|weekend-sketches")]
	[TestCase(nameof(CreativeCollection.Description), SortDirection.Descending, "quiet-interiors|weekend-sketches|coastal-notes")]
	public void SortingUsesTypedValuesAndStableIdTieBreaks(string property, SortDirection direction, string expectedIds) {
		var source = new CollectionDataSource(new CollectionCatalogue());
		var result = source.ReadRange(null, 3, 0, property, direction);

		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(expectedIds.Split('|')));
		Assert.That(result.TotalCount, Is.EqualTo(9));
	}

	[TestCase(3, -500, 0, "city-in-colour|coastal-notes|field-guide")]
	[TestCase(3, 1, 1, "material-library|orbital-studies|quiet-interiors")]
	[TestCase(3, 500, 2, "sound-and-shape|summer-archive|weekend-sketches")]
	[TestCase(4, 500, 2, "weekend-sketches")]
	[TestCase(6, 500, 1, "sound-and-shape|summer-archive|weekend-sketches")]
	public void PagingClampsToAvailablePagesWithoutRepeatingOrDroppingItems(int pageLength, int page, int expectedPage, string expectedIds) {
		var source = new CollectionDataSource(new CollectionCatalogue());
		var result = source.ReadRange(null, pageLength, page, nameof(CreativeCollection.Name), SortDirection.Ascending);

		Assert.That(result.Page, Is.EqualTo(expectedPage));
		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(expectedIds.Split('|')));
		Assert.That(result.TotalCount, Is.EqualTo(9));
	}

	[Test]
	public void EmptySearchResultsReturnAnEmptyFirstPage() {
		var source = new CollectionDataSource(new CollectionCatalogue());
		var result = source.ReadRange("A collection that does not exist", 3, 500, null, SortDirection.Ascending);

		Assert.That(result.Page, Is.Zero);
		Assert.That(result.TotalCount, Is.Zero);
		Assert.That(result.Items, Is.Empty);
	}

	[Test]
	public void InvalidationReloadsSavedValuesWithoutChangingAnExistingSnapshot() {
		var catalogue = new CollectionCatalogue();
		var source = new CollectionDataSource(catalogue);
		var first = source.ReadRange("coast", 3, 0, null, SortDirection.Ascending).Items.Single();
		var draft = catalogue.Read("coastal-notes");
		draft.Owner = "New owner";
		catalogue.Save(draft);

		Assert.That(source.ReadRange("coast", 3, 0, null, SortDirection.Ascending).Items.Single().Owner, Is.EqualTo("Alex Morgan"));
		source.Invalidate();
		Assert.That(source.ReadRange("coast", 3, 0, null, SortDirection.Ascending).Items.Single().Owner, Is.EqualTo("New owner"));
		Assert.That(first.Owner, Is.EqualTo("Alex Morgan"), "An existing presentation snapshot must not change behind its owner.");
	}

	[TestCase("null item")]
	[TestCase("empty ID")]
	[TestCase("unknown ID")]
	[TestCase("null name")]
	[TestCase("empty name")]
	[TestCase("whitespace name")]
	[TestCase("null owner")]
	[TestCase("empty owner")]
	[TestCase("whitespace owner")]
	[TestCase("negative progress")]
	[TestCase("excess progress")]
	[TestCase("unknown status")]
	public void InvalidSavesRejectTheWholeChangeWithoutMutationOrNotifications(string invalidCase) {
		var catalogue = new CollectionCatalogue();
		var before = catalogue.ReadAll().ToDictionary(item => item.Id);
		var notifications = 0;
		catalogue.Changed += () => notifications++;
		var draft = catalogue.Read("coastal-notes");
		draft.Description = "This valid edit must also be rejected with the invalid field.";
		switch (invalidCase) {
			case "null item": draft = null; break;
			case "empty ID": draft.Id = string.Empty; break;
			case "unknown ID": draft.Id = "unknown-collection"; break;
			case "null name": draft.Name = null; break;
			case "empty name": draft.Name = string.Empty; break;
			case "whitespace name": draft.Name = " \t "; break;
			case "null owner": draft.Owner = null; break;
			case "empty owner": draft.Owner = string.Empty; break;
			case "whitespace owner": draft.Owner = " \t "; break;
			case "negative progress": draft.Progress = -1; break;
			case "excess progress": draft.Progress = 101; break;
			case "unknown status": draft.Status = (CollectionStatus)99; break;
		}

		Assert.That(() => catalogue.Save(draft), Throws.InstanceOf<ArgumentException>());
		var after = catalogue.ReadAll();
		Assert.That(after.Select(item => item.Id), Is.EquivalentTo(before.Keys));
		Assert.That(after.All(item => item.HasSameEdits(before[item.Id]) && item.Updated == before[item.Id].Updated), Is.True);
		Assert.That(notifications, Is.Zero);
	}

	[TestCase(0)]
	[TestCase(-1)]
	public void NonPositivePageLengthsAreRejected(int pageLength) {
		var source = new CollectionDataSource(new CollectionCatalogue());

		Assert.That(() => source.ReadRange(null, pageLength, 0, null, SortDirection.Ascending), Throws.InstanceOf<ArgumentException>());
	}
}
