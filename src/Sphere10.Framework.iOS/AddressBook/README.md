# AddressBook

A legacy Xamarin.iOS helper that asks the user for address-book access.

`AddressBookTool.RequestAddressBookAuthorization` shows the `ABAddressBook` access prompt only while the authorization status is not yet determined. It waits for the answer on a thread-pool thread and then returns whether the status is `Authorized`. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AddressBookTool`](AddressBookTool.cs) | static class | `RequestAddressBookAuthorization` requests address-book access when needed and returns `true` when the status is `Authorized`. |
