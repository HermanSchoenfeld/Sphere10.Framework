# Configuration-file logging

A rolling-file logger configured from a `LoggingConfiguration` section of an `app.config` or `web.config` file.

`ApplicationLogger` is a `LoggerDecorator` over the logger that `ApplicationLogger.CreateLogger` builds. `CreateLogger` reads the configuration section named `LoggingConfiguration` whatever section name it is given. It returns a `NoOpLogger` when the section is missing, and otherwise a `RollingFileLogger` with the configured path, file limits and enabled levels. The section needs a configuration section handler, such as `GenericSectionHandler` from [Settings](../Settings/README.md#configuration-files). `Sphere10Framework.CreateApplicationLogger` builds a rolling-file logger in the product's `logs` data folder without a configuration file.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationLogger`](ApplicationLogger.cs) | class | Logger decorator over a `RollingFileLogger` configured by the `LoggingConfiguration` section, or a `NoOpLogger` without one |
| [`LoggingConfiguration`](LoggingConfiguration.cs) | class | Log file path, maximum file count and size, and debug, info, warning and error switches |
