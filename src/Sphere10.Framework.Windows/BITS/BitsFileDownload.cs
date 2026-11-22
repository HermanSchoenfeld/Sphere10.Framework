// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.BITS;

/// <summary>
/// Downloads one address to one local file with the Background Intelligent Transfer Service. Create it with the job's name, the
/// address and the destination, optionally subscribe to <see cref="ProgressChanged"/>, then await <see cref="DownloadAsync"/>,
/// which completes when the file is at its destination and fails with <see cref="BitsDownloadException"/> otherwise.
/// </summary>
/// <remarks>
/// An instance downloads once. Starting it cancels any existing job of the current user with the same name, as every
/// <see cref="JobEx"/> does, so a name identifies one download at a time. Cancelling the token cancels the job. The job is started
/// and observed on worker threads, and <see cref="ProgressChanged"/> is raised there with the job's progress and state, so a handler
/// dispatches to its UI thread itself and returns promptly. BITS waits in <see cref="JobState.Queued"/> and retries from
/// <see cref="JobState.TransientError"/> for as long as the job exists, so the download fails once no byte has arrived for
/// <see cref="NoProgressTimeout"/>. A failed or cancelled download cancels its job and leaves no file at the destination.
/// </remarks>
public class BitsFileDownload : JobEx {

	public event EventHandlerEx<JobProgress, JobState> ProgressChanged;

	private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly string _name;
	private int _started;
	private int _finished;
	private ulong _reportedBytesTransferred = ulong.MaxValue;
	private ulong _reportedBytesTotal = ulong.MaxValue;
	private JobState? _reportedState;
	private long _lastProgressTick;

	public BitsFileDownload(string name, string url, string destinationPath) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		Guard.ArgumentNotNullOrWhitespace(url, nameof(url));
		Guard.ArgumentNotNullOrWhitespace(destinationPath, nameof(destinationPath));
		var isAbsolute = Uri.TryCreate(url, UriKind.Absolute, out var address);
		Guard.Argument(isAbsolute && address.Scheme is "http" or "https", nameof(url), "Supply an absolute http or https address.");
		_name = name;
		Url = address.AbsoluteUri;
		DestinationPath = Path.GetFullPath(destinationPath);
	}

	public override JobType JobType => JobType.Download;

	public override string Name => _name;

	public override JobOwner Owner => JobOwner.CurrentUser;

	public override bool DeleteOnFailure => true;

	public string Url { get; }

	/// <summary>The full path of the downloaded file. Its folder is created when missing, and an existing file is replaced.</summary>
	public string DestinationPath { get; }

	/// <summary>The job's priority, read when the download starts. The default is foreground, for a download the user waits for.</summary>
	public JobPriority Priority { get; set; } = JobPriority.ForeGround;

	/// <summary>How often the job's state and progress are read. BITS notifications arrive on COM threads and can be missed, so the state is polled as well.</summary>
	public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);

	/// <summary>
	/// How long the job may go without transferring a byte before the download fails. The default is two minutes, and
	/// <see cref="Timeout.InfiniteTimeSpan"/> leaves a stalled job to BITS and the caller's token.
	/// </summary>
	public TimeSpan NoProgressTimeout { get; set; } = TimeSpan.FromMinutes(2);

	/// <summary>Starts the job and completes when the file has been transferred to <see cref="DestinationPath"/>.</summary>
	public Task DownloadAsync(CancellationToken cancellationToken = default) {
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);
		Guard.Ensure(PollInterval > TimeSpan.Zero, "The poll interval must be positive.");
		Guard.Ensure(NoProgressTimeout > TimeSpan.Zero || NoProgressTimeout == Timeout.InfiniteTimeSpan, "The no-progress timeout must be positive or infinite.");
		Guard.Ensure(Interlocked.Exchange(ref _started, 1) == 0, "A BITS file download runs once.");
		// Every BITS call stays on worker threads, away from a caller's single-threaded apartment.
		return Task.Run(() => TransferAsync(cancellationToken));
	}

	public override void OnCreate(BitsJob jobToConfigure) {
		jobToConfigure.Description = $"Download of {Url}";
		jobToConfigure.Priority = Priority;
		jobToConfigure.NotificationFlags = NotificationFlags.JobTransferred | NotificationFlags.JobErrorOccured;
		jobToConfigure.AddFile(Url, DestinationPath);
	}

	public override void OnCompleted() {
		if (Interlocked.Exchange(ref _finished, 1) != 0)
			return;
		// Complete releases the transferred file to its destination name. The wrapper reports its failures elsewhere, so the file decides.
		UnderlyingJob.Complete();
		if (File.Exists(DestinationPath))
			_completion.TrySetResult();
		else
			_completion.TrySetException(new BitsDownloadException($"The download of '{Url}' finished but '{DestinationPath}' was not written."));
	}

	public override void OnError(string error) {
		if (Interlocked.Exchange(ref _finished, 1) != 0)
			return;
		base.OnError(error);
		_completion.TrySetException(new BitsDownloadException(error));
	}

	protected void NotifyProgressChanged(JobProgress progress, JobState state) {
		OnProgressChanged(progress, state);
		ProgressChanged?.Invoke(progress, state);
	}

	protected virtual void OnProgressChanged(JobProgress progress, JobState state) {
	}

	private async Task TransferAsync(CancellationToken cancellationToken) {
		try {
			Tools.FileSystem.CreateDirectory(Path.GetDirectoryName(DestinationPath));
			if (File.Exists(DestinationPath))
				Tools.FileSystem.DeleteFile(DestinationPath);
			_lastProgressTick = Environment.TickCount64;
			Start();
			while (!_completion.Task.IsCompleted) {
				try {
					await _completion.Task.WaitAsync(PollInterval, cancellationToken).ConfigureAwait(false);
				} catch (TimeoutException) {
					Poll();
				}
			}
			await _completion.Task.ConfigureAwait(false);
		} catch (Exception error) {
			// Whatever ended the wait, the job must not keep transferring for an owner that has gone, and nothing partial stays behind.
			Abandon();
			Tools.Exceptions.ExecuteIgnoringException(() => {
				if (File.Exists(DestinationPath))
					Tools.FileSystem.DeleteFile(DestinationPath);
			});
			if (error is OperationCanceledException or BitsDownloadException)
				throw;
			throw new BitsDownloadException($"The download of '{Url}' failed. {error.Message}", error);
		}
	}

	private void Poll() {
		var job = UnderlyingJob;
		if (job == null)
			return;
		var state = job.State;
		var progress = job.Progress;
		if (progress != null && (progress.BytesTransferred != _reportedBytesTransferred || progress.BytesTotal != _reportedBytesTotal || state != _reportedState)) {
			if (progress.BytesTransferred != _reportedBytesTransferred)
				_lastProgressTick = Environment.TickCount64;
			_reportedBytesTransferred = progress.BytesTransferred;
			_reportedBytesTotal = progress.BytesTotal;
			_reportedState = state;
			NotifyProgressChanged(progress, state);
		}
		switch (state) {
			case JobState.Transferred:
				OnCompleted();
				return;
			case JobState.Error:
				OnError(job.Error?.Description is { Length: > 0 } description ? description.Trim() : $"The download of '{Url}' failed.");
				return;
			case JobState.Cancelled:
			case JobState.Acknowledged:
				// Only another owner ends the job this way, for example a newer download started under the same name.
				if (Interlocked.Exchange(ref _finished, 1) == 0)
					_completion.TrySetException(new BitsDownloadException($"The download of '{Url}' was ended outside this download."));
				return;
		}
		// A queued job and a job retrying a transient error would otherwise wait for as long as BITS keeps the job.
		if (NoProgressTimeout != Timeout.InfiniteTimeSpan && Environment.TickCount64 - _lastProgressTick >= NoProgressTimeout.TotalMilliseconds) {
			var reason = state == JobState.TransientError && job.Error?.Description is { Length: > 0 } transientError
				? transientError.Trim()
				: $"The job's last state was {state}.";
			OnError($"The download of '{Url}' made no progress for {NoProgressTimeout.TotalSeconds:0} seconds. {reason}");
		}
	}

	private void Abandon() {
		if (Interlocked.Exchange(ref _finished, 1) != 0)
			return;
		// The job may already be gone; nothing further can be released.
		Tools.Exceptions.ExecuteIgnoringException(() => UnderlyingJob?.Cancel());
		_completion.TrySetCanceled();
	}
}
