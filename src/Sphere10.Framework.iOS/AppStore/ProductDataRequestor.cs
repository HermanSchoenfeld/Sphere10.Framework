// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using StoreKit;

namespace Sphere10.Framework.iOS {

	internal class ProductDataRequestor : SKProductsRequestDelegate {
		private readonly ManualResetEventSlim _waiter;
		private SKProductsResponse _response;

		public ProductDataRequestor() {
			_waiter = new ManualResetEventSlim(false);
		}

		public override void ReceivedResponse(SKProductsRequest request, SKProductsResponse response) {
			_response = response;
			_waiter.Set();
		}

		public async Task<SKProductsResponse> GetProductData(params string[] appStoreProductIDs) {
			if (appStoreProductIDs.Length == 0)
				throw new ArgumentException("No app store product IDs passed", "appStoreProductIDs");
			var array = new NSString[appStoreProductIDs.Length];
			for (var i = 0; i < appStoreProductIDs.Length; i++) {
				array[i] = new NSString(appStoreProductIDs[i]);
			}
			var productIdentifiers = NSSet.MakeNSObjectSet<NSString>(array);

			//set up product request for in-app purchase
			var productsRequest = new SKProductsRequest(productIdentifiers);
			productsRequest.Delegate = this;
			productsRequest.Start();
			await Task.Run(() => _waiter.Wait());
			Debug.Assert(_response != null);
			return _response;
		}
	}

}

