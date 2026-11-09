# Security

Manages local users, groups and shares through the NetAPI, adjusts token privileges, impersonates accounts and reads or translates security descriptors.

`NTHost` is the entry point for account administration. `NTHost.CurrentMachine` or `new NTHost(name)` represents a local or remote machine, and its methods list, look up and create `NTLocalUser`, `NTLocalGroup` and `NTShare` objects. Every object derives `NTObject`, which carries a host, name, SID and SID type. Local objects (`NTLocalObject`) push edits with `Update`, reload with `Refresh` and remove themselves with `Delete`. Group members are `NTLocalObject`, `NTRemoteObject` for domain accounts, or `NTDanglingObject` for references whose SID or name cannot be resolved. Remote hosts use the caller's Windows credentials, and each native query checks access separately. `NetApiBufferScope` frees NetAPI output buffers, and `NetApiException` reports `NET_API_STATUS` failures.

`PrivilegeUtil` (`Tools.Windows.Security`) enables privileges such as `SeBackupPrivilege` for one operation through `EnterScope` or `TryEnterScope`, which return a `TokenPrivilegeScope` over a duplicated thread token. Acquire and dispose that scope synchronously on the same worker thread, because it restores the previous thread token on disposal. `Impersonator.RunAs` logs on with supplied credentials and runs a synchronous action under that identity. `SecurityDescriptorUtil` (`Tools.Windows.SecurityDescriptors`) lists the SIDs a descriptor references and rewrites them without changing any other descriptor data. `SecurityTool` reads and writes file, directory and registry security, changes owners and looks up account names and SIDs. `ShareSecurity` with `ShareAccessRule` and `ShareAuditRule` is a `NativeObjectSecurity` model of share permissions.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Impersonator`](Impersonator.cs) | class | Static `RunAs` that logs on with a user name, domain and password and runs an action impersonated, then restores the caller's identity |
| [`NetApiBufferScope`](NetApiBufferScope.cs) | struct | Readonly scope that frees a NetAPI output buffer with `NetApiBufferFree` |
| [`NetApiException`](NetApiException.cs) | class | `WindowsException` for a failed NetAPI call with its `NET_API_STATUS` |
| [`NTDanglingObject`](NTDanglingObject.cs) | class | Group member or reference that has a SID without a name or a name without a SID |
| [`NTHost`](NTHost.cs) | class | A local or remote machine that lists, finds and creates local users, groups and shares and resolves UNC paths to local paths |
| [`NTLocalGroup`](NTLocalGroup.cs) | class | A local group with member listing by kind and add, remove, update, refresh and delete operations |
| [`NTLocalObject`](NTLocalObject.cs) | abstract class | Base of host-local users, groups and shares with a description and abstract `Update`, `Refresh` and `Delete` |
| [`NTLocalUser`](NTLocalUser.cs) | class | A local user with NetAPI account fields, password setting, group membership and update, refresh and delete operations |
| [`NTObject`](NTObject.cs) | abstract class | Base of security principals with host, name, full name, SID and SID type |
| [`NTRemoteObject`](NTRemoteObject.cs) | class | A domain account referenced from a host, such as a domain member of a local group |
| [`NTShare`](NTShare.cs) | class | A network share with server path, type, permissions, use counts and security descriptor, and access-control get and set operations |
| [`PrivilegeUtil`](PrivilegeUtil.cs) | class | `Tools.Windows.Security` token-privilege lookup, enabling and disabling, and operation-local privilege scopes |
| [`SecurityDescriptorSidReference`](SecurityDescriptorSidReference.cs) | struct | Record struct of one SID and the owner, group, DACL and SACL sections that reference it |
| [`SecurityDescriptorUtil`](SecurityDescriptorUtil.cs) | class | `Tools.Windows.SecurityDescriptors` SID reference listing and SID translation of binary security descriptors |
| [`SecurityTool`](SecurityTool.cs) | class | Static file, directory and registry security reads and writes, owner changes, current-user SID and account and SID lookups |
| [`ShareAccessRule`](ShareAuditRule.cs) | class | `AccessRule` granting or denying `ShareRights` to an identity |
| [`ShareAuditRule`](ShareAuditRule.cs) | class | `AuditRule` auditing `ShareRights` use by an identity |
| [`ShareRights`](ShareSecurity.cs) | enum | Share permission bits for read, write and create |
| [`ShareSecurity`](ShareSecurity.cs) | class | `NativeObjectSecurity` for a share, with share access and audit rule management |
| [`TokenPrivilegeScope`](TokenPrivilegeScope.cs) | struct | Synchronous scope that enables privileges on a duplicated thread token and restores the previous token on disposal |
| [`UserFlags`](UserFlags.cs) | enum | NetAPI user account flags such as disabled, locked out, password not required and password never expires |
| [`UserOperatorPriviliges`](UserOperatorPrivileges.cs) | enum | NetAPI operator privilege flags for print, communication, server and accounts operators |
| [`UserPrivilege`](UserPrivilege.cs) | enum | NetAPI user privilege level: guest, user or administrator |

### LocalAccounts

`LocalAccountsService` serializes local-user and local-group workflows over an `ILocalAccountsBackend`. Updates and deletes take the `LocalUserInfo` or `LocalGroupInfo` the caller last read and check it against the current state before changing anything. `WindowsLocalAccountsBackend` is the production backend over `NTHost`, and tests can supply their own. `LocalUserDataSourceBase` and `LocalGroupDataSourceBase` are [`PollingDataSource`](../../Sphere10.Framework/DataSource/README.md) templates with CRUD support. Subclasses implement `ConfirmAsync` and may override `Logger`, while `ActionLocalUserDataSource` and `ActionLocalGroupDataSource` take both as delegates. Passwords are never part of the info records.

| Type | Kind | Purpose |
|---|---|---|
