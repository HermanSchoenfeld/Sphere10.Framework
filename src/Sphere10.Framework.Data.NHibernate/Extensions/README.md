# Extensions

Provides extension methods that add the auditing listener to a FluentNHibernate configuration and attach detached entities to a session.

`AddBusinessObjectAuditor()` registers the internal pre-insert and pre-update listener from [Listeners](../Listeners/README.md). On insert it sets `CreatedOnUtc` and `LastUpdatedOnUtc` to the current UTC time and `Active` to `true`. On update it sets `LastUpdatedOnUtc`. Each property is set only when the entity has a property of that name. `ISession.Attach(entity)` adds a detached entity to the session's persistence context with the loaded status, without reading it from the database. Its `mode` argument is ignored.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FluentConfigurationExtensions`](FluentConfigurationExtensions.cs) | static class | Adds `AddBusinessObjectAuditor()` to `FluentConfiguration`, which appends the audit listener for pre-insert and pre-update events |
| [`SessionExtensions`](SessionExtensions.cs) | static class | Adds `Attach<T>()` to `ISession`, which registers a detached entity as loaded in the persistence context |
