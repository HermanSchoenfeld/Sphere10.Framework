// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.AppointmentBook;
using AppointmentBookControl = Sphere10.Framework.Windows.Forms.AppointmentBook.AppointmentBook;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Renders the tester's real appointment screen over a fixed schedule and checks its visual contract.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class AppointmentBookAppearanceTests {
	[Test]
	public void AppointmentScreenCaptureUsesAFixedSchedule() {
		RunWithMessageLoop(async owner => {
			using var screen = new AppointmentBookScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var source = CreateSchedule();
			var book = Find<AppointmentBookControl>(screen, "AppointmentBook");
			var unallocated = Find<UnallocatedBook>(screen, "UnallocatedBook");
			book.DataSource = source;
			unallocated.DataSource = source;
			book.SelectAppointment(source.GetAppointments(source.GetColumns().ElementAt(1)).First());
			await Task.Yield();
			var path = SaveCapture(screen, "appointment-book-screen");
			Assert.That(File.Exists(path), Is.True);

			// Sub-hour slots show the hour beside its minutes, as the planning grid does.
			book.SetTimePeriodType(TimePeriodType.DailyHalfHourly);
			unallocated.SetTimePeriodType(TimePeriodType.DailyHalfHourly);
			await Task.Yield();
			Assert.That(File.Exists(SaveCapture(screen, "appointment-book-half-hourly")), Is.True);

			// Both books keep font-based rows after binding and resizing, including the tray's empty rows.
			foreach (var grid in new[] { book, unallocated }.Select(control => control.Controls.OfType<SourceGrid.Grid>().Single()))
				Assert.That(Enumerable.Range(1, grid.RowsCount - 1).Select(row => grid.Rows[row].Height), Is.All.EqualTo(grid.Font.Height + 8));
		});
	}

	[Test]
	public void AppointmentsUseThePlanningGridStyle() {
		RunWithMessageLoop(async owner => {
			var source = CreateSchedule();
			using var book = new AppointmentBookControl { Dock = DockStyle.Fill };
			owner.Controls.Add(book);
			book.DataSource = source;
			book.SelectAppointment(source.GetAppointments(source.GetColumns().ElementAt(1)).First());
			await Task.Yield();
			var grid = book.Controls.OfType<SourceGrid.Grid>().Single();

			// Model cell (column 0, row 2) is the magenta "Site inspection"; the grid adds one header row and two time columns.
			var appointment = View(grid, 3, 2);
			Assert.That(appointment.BackColor.GetBrightness(), Is.GreaterThanOrEqualTo(0.85f));
			Assert.That(appointment.BackColor.GetHue(), Is.EqualTo(Color.Magenta.GetHue()).Within(2f));
			Assert.That(appointment.ForeColor.GetBrightness(), Is.LessThanOrEqualTo(0.3f));
			Assert.That(appointment.ForeColor.GetHue(), Is.EqualTo(Color.Magenta.GetHue()).Within(2f));
			Assert.That(appointment.Font.Bold, Is.True);
			Assert.That(appointment.Font.Underline, Is.False);
			var outline = ((Sphere10.Framework.Drawing.RectangleBorder)appointment.Border).Top;
			Assert.That(outline.Width, Is.EqualTo(1));
			Assert.That(outline.Color.ToArgb(), Is.Not.EqualTo(Color.Black.ToArgb()));

			// The selected "Fleet servicing" uses the SourceGrid selection outline.
			var selected = (Sphere10.Framework.Drawing.RectangleBorder)View(grid, 3, 3).Border;
			Assert.That(selected.Top, Is.EqualTo(((SourceGrid.Selection.SelectionBase)grid.Selection).Border.Top));

			// Free slots draw the planning grid's cell lines, and the time column reads as whole hours under a dated caption.
			var free = (Sphere10.Framework.Drawing.RectangleBorder)View(grid, 1, 2).Border;
			Assert.That(free.Right, Is.EqualTo(SourceGrid.Cells.Views.ViewBase.DefaultBorder.Right));
			Assert.That(free.Bottom, Is.EqualTo(SourceGrid.Cells.Views.ViewBase.DefaultBorder.Bottom));
			Assert.That(grid[1, 0].DisplayText, Is.EqualTo("00:00"));
			Assert.That(grid[0, 0].DisplayText, Does.Contain(source.StartTime.ToString("dddd")));
			Assert.That(grid.Rows[0].Height, Is.EqualTo(2 * grid.Font.Height + 8));
		});
	}

	[Test]
	public void AppointmentColorsCanBeDrawnUnchanged() {
		RunWithMessageLoop(async owner => {
			var source = CreateSchedule();
			using var book = new AppointmentBookControl { Dock = DockStyle.Fill, SoftenAppointmentColors = false };
			owner.Controls.Add(book);
			book.DataSource = source;
			await Task.Yield();
			var grid = book.Controls.OfType<SourceGrid.Grid>().Single();
			Assert.That(View(grid, 3, 2).BackColor.ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
			Assert.That(View(grid, 3, 2).ForeColor.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
		});
	}

	private static SourceGrid.Cells.Views.Cell View(SourceGrid.Grid grid, int row, int column) => (SourceGrid.Cells.Views.Cell)grid[row, column].View;

	// A schedule with the saturated colours, multi-line details and lengths that the sample generator produces.
	private static MyAppointmentBookDataSource CreateSchedule() {
		var source = new MyAppointmentBookDataSource();
		var day = source.StartTime;
		var colors = new[] { Color.Magenta, Color.Red, Color.Gold, Color.OrangeRed, Color.DeepPink, Color.DodgerBlue, Color.LimeGreen, Color.DarkOrange };
		var columns = Enumerable.Range(1, 8).Select(index => source.NewColumn($"Resource {index}", null)).ToArray();
		var bookings = new (int Column, int Start, int Hours, string Subject, string Street, string City, string Notes)[] {
			(0, 2, 1, "Site inspection", "", "", ""),
			(0, 12, 1, "Client call", "", "", ""),
			(0, 14, 4, "Installation", "87 Milky Way Rd", "Hyades, VIC 9531", ""),
			(1, 2, 9, "Fleet servicing", "25 Phi Ct", "Ceres, SA 8683", "Bring the diagnostic kit"),
			(1, 16, 1, "Handover", "", "", ""),
			(1, 18, 2, "Quote review", "", "", ""),
			(2, 1, 5, "Warehouse audit", "51 Beta Pde", "Mercury, QLD 4122", ""),
			(3, 8, 8, "Training day", "26 Delta St", "Hyades, SA 4919", "Projector booked"),
			(4, 0, 3, "Night shift", "", "", ""),
			(5, 2, 8, "Roof repair", "24 Psi Pde", "Taurus, WA 7005", ""),
			(5, 11, 3, "Safety briefing", "", "", ""),
			(6, 7, 2, "Delivery", "", "", ""),
			(6, 11, 3, "Fit-out", "9 Gamma Way", "Vega, NSW 2150", ""),
			(7, 1, 1, "Stand-up", "", "", ""),
			(7, 8, 3, "Planning", "14 Alpha St", "Pleides, TAS 7000", "")
		};
		for (var index = 0; index < bookings.Length; index++) {
			var booking = bookings[index];
			var start = day.AddHours(booking.Start);
			source.NewAppointment(columns[booking.Column], $"booking-{index}", start, start.AddHours(booking.Hours), booking.Subject, booking.Street, booking.City, booking.Notes, colors[index % colors.Length], null);
		}
		source.NewAppointment(new UnallocatedColumn(), "unallocated-0", day.AddHours(9), day.AddHours(11), "Unassigned callout", "3 Omega Rd", "Woodridge, QLD 4114", "", Color.Red, null);
		source.NewAppointment(new UnallocatedColumn(), "unallocated-1", day.AddHours(13), day.AddHours(14), "Follow-up visit", "", "", "", Color.Gold, null);
		return source;
	}

	private static string SaveCapture(Control control, string name) {
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "appointment-book-captures");
		Directory.CreateDirectory(directory);
		using var image = new Bitmap(control.Width, control.Height);
		control.DrawToBitmap(image, new Rectangle(Point.Empty, control.Size));
		var path = Path.Combine(directory, name + ".png");
		image.Save(path, ImageFormat.Png);
		TestContext.Progress.WriteLine("Appointment book capture: " + path);
		return path;
	}

	// Named native controls exercise the real Designer-owned surface without production test accessors.
	private static T Find<T>(Control parent, string name)
		where T : Control {
		return (T)parent.Controls.Find(name, true).Single();
	}

	// Each case owns a real UI dispatcher off-screen, closes stray dialogs and restores the caller's context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1500, 1000)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The appointment book test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				foreach (var dialog in System.Windows.Forms.Application.OpenForms.Cast<Form>().Where(form => form != owner).ToArray())
					dialog.Dispose();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
