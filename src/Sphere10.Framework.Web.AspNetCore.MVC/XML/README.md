# XML

An action result that returns serialized objects or existing XML as `application/xml`.

`XmlResult` is a `ContentResult` with status 200 unless a status code is passed. An object argument is serialized with `Tools.Xml.WriteToString` in UTF-8, a string argument is sent as given, and the parameterless constructor sends an empty body. It does not need the Framework to be started. The [Processing](../Processing/README.md) domain holds the image result.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`XmlResult`](XmlResult.cs) | class | `ContentResult` with the `application/xml` content type whose body is a serialized object, a given XML string or empty. |
