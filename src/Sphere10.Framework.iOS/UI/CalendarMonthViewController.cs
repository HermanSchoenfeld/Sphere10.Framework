// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Foundation;
using UIKit;
using System.Collections;
using System.Collections.Generic;
using CoreGraphics;
namespace Sphere10.Framework.iOS
{
	public class CalendarMonthViewController : UIViewController
    {

        public CalendarMonthView MonthView;

        public override void ViewDidLoad()
        {
            MonthView = new CalendarMonthView();
			MonthView.OnDateSelected += (date) => {
				Console.WriteLine(String.Format("Selected {0}", date.ToShortDateString()));
			};
			MonthView.OnFinishedDateSelection = (date) => {
				Console.WriteLine(String.Format("Finished selecting {0}", date.ToShortDateString()));
			};
			MonthView.IsDayMarkedDelegate = (date) => {
				return (date.Day % 2==0) ? true : false;
			};
			MonthView.IsDateAvailable = (date)=>{
				return (date > DateTime.Today);
			};
			
            View.AddSubview(MonthView);
        }
		
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            return false;
        }

    }


}

