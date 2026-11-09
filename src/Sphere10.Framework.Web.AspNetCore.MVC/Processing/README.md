# Processing

A Windows-only action result that writes a `System.Drawing` image to the response.

`ImageResult` encodes its `Image` as JPEG by default, or as PNG or GIF, and throws for any other `ImageFormat`. It disposes the image after writing, so do not reuse the image. Asynchronous execution encodes the image in memory, sets the content length and copies the bytes with asynchronous writes, so `AllowSynchronousIO` is not needed. Only a direct call to the synchronous `ExecuteResult` saves straight to the response body. The [XML](../XML/README.md) domain holds the other result type of this library.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ImageResult`](ImageResult.cs) | class | `ActionResult` that writes and then disposes a `System.Drawing.Image` as JPEG, PNG or GIF with the matching content type. |
