# BITS

Creates, enumerates and monitors Background Intelligent Transfer Service (BITS) download and upload jobs through its COM interfaces.

`BitsManager` is the entry point. It wraps the BITS `IBackgroundCopyManager`, creates jobs with `CreateJob(displayName, JobType)`, enumerates the current user's or all users' jobs with `EnumJobs`, and raises job added, removed, modified, transferred and error events. Each `BitsJob` wraps one queued job. Use it to add files, set priority, credentials and proxy settings, and to `Resume`, `Suspend`, `Cancel` or `Complete` the transfer. A job's files are `BitsFile` objects with `FileProgress`, and its progress and timestamps are read as `JobProgress` and `JobTimes`.

`BitsFileDownload` downloads one address to one local file and is the entry point for an ordinary download. Create it with a job name, an absolute http or https address and the destination path, then await `DownloadAsync`. The task completes when the file is at its destination and fails with `BitsDownloadException`, which carries the description BITS gave. Cancelling the token cancels the job. The download creates the destination folder when it is missing and replaces a file already there. The job is started and observed on worker threads, and `ProgressChanged` is raised there with the job's `JobProgress` and `JobState`, so a handler dispatches to its UI thread itself. The state shows why nothing is arriving, such as `Queued`, `Connecting` or `TransientError`. BITS waits and retries in those states for as long as the job exists, so the download fails once no byte has arrived for `NoProgressTimeout`, which is two minutes by default and can be `Timeout.InfiniteTimeSpan`. A failed or cancelled download cancels its job and leaves no file at the destination. A download runs once, and because it is a `JobEx`, starting it cancels any job of the current user that has the same name, so give concurrent downloads different names.

```csharp
using System;
using System.IO;
using System.Threading;
using Sphere10.Framework.Windows.BITS;

using var cancellation = new CancellationTokenSource();
var destination = Path.Combine(Path.GetTempPath(), "Example", "setup.exe");
var download = new BitsFileDownload("Example Update", "https://updates.example.com/setup.exe", destination);
download.ProgressChanged += (progress, state) => Console.WriteLine($"{state}: {progress.BytesTransferred} of {progress.BytesTotal} bytes");
await download.DownloadAsync(cancellation.Token);
```

`JobEx` is a template for a named job that survives application restarts. `Initialize` cancels any existing job with the same name, creates a new one and calls `OnCreate`. A completed transfer calls `OnCompleted` and a job error calls `OnError`. `BitsManager`, `BitsJob`, `BitsFile`, `BitsFiles` and `BitsJobs` are disposable because they hold COM objects. `AutoUpdater` and `IPatch` are a legacy patch downloader that compiles and runs a downloaded C# file with hard-coded assembly references, so new code should not use them.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AuthenticationScheme`](_Enums.cs) | enum | Authentication scheme (Basic, Digest, NTLM, Negotiate, Passport) for credentials a job supplies to a proxy or server |
| [`AuthenticationTarget`](_Enums.cs) | enum | Whether job credentials answer server or proxy authentication requests |
| [`AutoUpdater`](AutoUpdateManager.cs) | class | Legacy patch downloader that queues a BITS job for a patch file, then compiles and executes the downloaded C# patch |
| [`BG_AUTH_SCHEME`](BITSInterop.cs) | enum | Native BITS authentication-scheme constants that `BitsJob` maps from `AuthenticationScheme` |
| [`BG_AUTH_TARGET`](BITSInterop.cs) | enum | Native BITS authentication-target constants that `BitsJob` maps from `AuthenticationTarget` |
| [`BitsCredentials`](BitsCredentials.cs) | class | User name, password, scheme and target passed to `BitsJob.AddCredentials` |
| [`BitsDownloadException`](BitsDownloadException.cs) | class | Failure of a `BitsFileDownload`, with the description BITS gave for it |
| [`BitsError`](BitsError.cs) | class | A job's current error: description, context, protocol, error code and the failing `BitsFile` |
| [`BitsFile`](BitsFile.cs) | class | Disposable wrapper of one job file with its local and remote names and `FileProgress` |
| [`BitsFileDownload`](BitsFileDownload.cs) | class | `JobEx` that downloads one address to one file, with an awaited `DownloadAsync`, cancellation, `ProgressChanged` with progress and state, a `Priority`, a `PollInterval` and a `NoProgressTimeout` |
| [`BitsFileInfo`](BitsFileInfo.cs) | class | Remote and local name pair used to add a file to a job |
| [`BitsFiles`](BitsFiles.cs) | class | Disposable `List<BitsFile>` returned by `BitsJob.EnumFiles` |
| [`BitsInterfaceNotificationEventArgs`](BitsNotification.cs) | class | Event data for a COM interface failure, with message, description and HRESULT |
| [`BitsJob`](BitsJob.cs) | class | Disposable wrapper of one BITS job: properties, files, credentials, proxy settings, state control and modified, transferred and error events |
| [`BitsJobs`](BitsJobs.cs) | class | Disposable `Dictionary<Guid, BitsJob>` of jobs keyed by job ID |
| [`BitsManager`](BitsManager.cs) | class | Disposable entry point that creates and enumerates jobs, describes HRESULTs and raises queue-wide job events |
| [`ErrorContext`](_Enums.cs) | enum | Where a job error occurred, such as the local file, remote file or transport |
| [`ErrorNotificationEventArgs`](BitsNotification.cs) | class | Manager-level job error event data carrying the job and its `BitsError` |
| [`FileProgress`](FileProgress.cs) | class | Bytes total, bytes transferred and completion flag of one job file |
| [`IPatch`](AutoUpdateManager.cs) | interface | Legacy contract with one `Execute` method for patches that `AutoUpdater` compiles |
| [`JobErrorNotificationEventArgs`](BitsNotification.cs) | class | Job-level error event data carrying the `BitsError` |
| [`JobEx`](JobEx.cs) | abstract class | Template for a named job that replaces any job with the same name on `Initialize` and reports `OnCompleted` or `OnError` |
| [`JobNotificationEventArgs`](BitsNotification.cs) | class | Base event data for job notifications |
| [`JobOwner`](_Enums.cs) | enum | Enumeration scope for `EnumJobs`: the current user's jobs or all users' jobs |
| [`JobPriority`](_Enums.cs) | enum | Job priority from foreground to low |
| [`JobProgress`](JobProgress.cs) | class | Bytes and files total and transferred for a job |
| [`JobState`](_Enums.cs) | enum | Job lifecycle state from queued through transferred, acknowledged or cancelled |
| [`JobTimes`](JobTimes.cs) | class | Creation, modification and transfer-completion times of a job |
| [`JobType`](_Enums.cs) | enum | Download, upload or upload-reply job |
| [`NotificationEventArgs`](BitsNotification.cs) | class | Manager-level job event data carrying the affected `BitsJob` |
| [`NotificationFlags`](_Enums.cs) | enum | Flags selecting which job events (transferred, error, modified) BITS reports, or none |
| [`ProxySettings`](ProxySettings.cs) | class | A job's proxy usage, proxy list and bypass list, applied with `Update` |
| [`ProxyUsage`](_Enums.cs) | enum | Proxy selection: preconfigured, none, override or auto-detect |
