# AppStore

A legacy Xamarin.iOS StoreKit wrapper that fetches App Store product data and purchases products asynchronously.

`iTunesAppStoreManager` caches `SKProduct` objects by product ID in a synchronized dictionary. `PrefetchProductData` and `GetProductData` request only the IDs that are not cached yet and retry a failed request up to three times. `GetProductData(string)` throws when the App Store returns no product for the ID. `PurchaseProduct` adds a payment to the StoreKit payment queue and completes with a `PurchaseResult` once the transaction is purchased, restored or failed. It finishes each completed transaction straight away, before the caller records the purchase, which a comment in the source flags as a risk. Check `CanMakePayments` before offering a purchase. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`iTunesAppStoreManager`](iTunesAppStoreManager.cs) | class | Fetches and caches StoreKit product data and purchases a product by ID or `SKProduct`. |
| [`PurchaseResult`](PurchaseResult.cs) | class | Outcome of a purchase with its `TransactionResult`, product ID, purchase date and error. |
| [`TransactionResult`](TransactionResult.cs) | enum | `Purchased`, `Failed` or `Restored`. |
