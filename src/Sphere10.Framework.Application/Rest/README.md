# REST client

A JSON-over-HTTP client with typed GET, POST, PATCH and DELETE calls relative to a base URL.

`RestClient` creates one `HttpClient` on first use and logs each request and response at debug level through the `ILogger` it is given. Request bodies are serialized with Newtonsoft.Json using camel-case names and omitted nulls, unless the constructor receives other settings. Responses are deserialized with the settings passed to each call, or Newtonsoft.Json defaults. A response without a success status throws `InvalidOperationException` with the status code and body. Override `PrepareHttpRequestInternal` to add headers such as authorization, and `BuildException` to raise a different error. Dispose the client to release its `HttpClient`. `ParseStreamAsync<T>` in [Extension methods](../Extensions/README.md) reads the responses.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IRestClient`](IRestClient.cs) | interface | Typed `GetAsync`, `PostAsync`, `PatchAsync` and `DeleteAsync` with query parameters, headers and serializer settings, declared in the `Sphere10.Framework` namespace |
| [`RestClient`](RestClient.cs) | class | Disposable `IRestClient` over a lazily created `HttpClient` with request logging, overridable request preparation and error mapping |
