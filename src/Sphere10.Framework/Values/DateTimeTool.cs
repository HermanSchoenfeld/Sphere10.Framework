// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

public static class Time {
	/// <summary>Returns the complete calendar years in dateA minus dateB.</summary>
	public static int YearsDifference(DateTime dateA, DateTime dateB) => MonthsDifference(dateA, dateB) / 12;

	/// <summary>Returns complete calendar months, using AddMonths for month-end and leap-year boundaries.</summary>
	public static int MonthsDifference(DateTime dateA, DateTime dateB) {
		// Compare in one direction so negative differences remain symmetric.
		if (dateA == dateB)
			return 0;
		if (dateA < dateB)
			return -MonthsDifference(dateB, dateA);

		// A calendar anniversary is complete only after its shifted date and time.
		var months = (dateA.Year - dateB.Year) * 12 + dateA.Month - dateB.Month;
		if (months == 0)
			return 0;
		return dateA >= dateB.AddMonths(months) ? months : months - 1;
	}

	public static DateTime CalculateNextDayOfWeek(DateTime fromDate, DayOfWeek dayOfWeek) {
		//Sunday    1  0
		//Monday    2  1
		//Tuesday   3  2
		//Wednesday 4  3
		//Thursday  5  4
		//Friday    6  5
		//Saturday  7  6
		var startDayOfWeek = (int)(fromDate.DayOfWeek) - 1;
		var destinationDayOfWeek = (int)dayOfWeek - 1;

		return destinationDayOfWeek > startDayOfWeek ? fromDate.AddDays(destinationDayOfWeek - startDayOfWeek).ToMidnight() : fromDate.AddDays((7 + destinationDayOfWeek) - startDayOfWeek).ToMidnight();
	}

	public static DateTime CalculateNextDayOfWeek(DateTime fromDate, DayOfWeek dayOfWeek, TimeSpan timeOfDay) {
		DateTime date;
		if (fromDate.DayOfWeek == dayOfWeek && fromDate.TimeOfDay < timeOfDay)
			date = fromDate;
		else
			date = CalculateNextDayOfWeek(fromDate, dayOfWeek);

		return date.ToMidnight().Add(timeOfDay);
	}

	public static DateTime CalculateNextDayOfMonth(DateTime fromDate, int dayOfMonth) {
		if (fromDate.Day < dayOfMonth)
			return new DateTime(fromDate.Year, fromDate.Month, dayOfMonth, fromDate.Hour, fromDate.Minute, fromDate.Second, fromDate.Millisecond, fromDate.Kind);

		return fromDate.ToEndOfMonth().AddDays(1).ToBeginningOfMonth().AddDays(dayOfMonth - 1).ToMidnight().Add(fromDate.TimeOfDay);
	}

	public static DateTime CalculateNextDayOfMonth(DateTime fromDate, int dayOfMonth, TimeSpan timeOfDay) {
		DateTime date;
		if (fromDate.Day < dayOfMonth || (fromDate.Day == dayOfMonth && fromDate.TimeOfDay < timeOfDay))
			date = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, 0);
		else
			date = CalculateNextDayOfMonth(fromDate, dayOfMonth).ToMidnight();

		return date.Add(timeOfDay);
	}

	public static DateTime DateFrom1979Jan1(uint milliseconds) => new DateTime(1979, 1, 1).AddMilliseconds(milliseconds);

	public static DateTime FromUnixTime(uint unixTimeStamp) {
		// Unix timestamp is seconds past epoch
		var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
		return dateTime;
	}

	/// <summary>
	/// Return the ordinal suffix for the specified day of the month.
	/// Such as 'st' for 1st or 'rd' for 3rd.
	/// </summary>
	public static string GetOrdinalDaySuffix(int dayNumber) {
		// Return: st, nd, rd, th, ...

		if (((dayNumber % 100) / 10) != 1) {
			var lastDigit = dayNumber % 10;

			if (lastDigit == 1)
				return "st";
			if (lastDigit == 2)
				return "nd";
			if (lastDigit == 3)
				return "rd";
		}

		return "th";
	}

	public static string ConvertMonth(int month) {
		switch (month) {
			case 1:
				return "January";
			case 2:
				return "February";
			case 3:
				return "March";
			case 4:
				return "April";
			case 5:
				return "May";
			case 6:
				return "June";
			case 7:
				return "July";
			case 8:
				return "August";
			case 9:
				return "September";
			case 10:
				return "October";
			case 11:
				return "November";
			case 12:
				return "December";
		}
		throw new ArgumentOutOfRangeException(nameof(month), "Must  be between 1 and 12 (inclusive)");
	}

	public static DateTime Max(DateTime date1, DateTime date2) => date1 >= date2 ? date1 : date2;

	public static DateTime? Max(DateTime? date1, DateTime? date2) {
		if (date1.HasValue && !date2.HasValue)
			return date1;

		if (!date1.HasValue && date2.HasValue)
			return date2;

		if (!date1.HasValue)
			return null;

		return date1.Value >= date2.Value ? date1.Value : date2.Value;
	}

	public static DateTime Min(DateTime date1, DateTime date2) => date1 <= date2 ? date1 : date2;
}
