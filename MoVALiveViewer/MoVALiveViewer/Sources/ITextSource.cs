namespace MoVALiveViewer.Sources;

public interface ITextSource : IDisposable
{
    event Action<string>? ChunkReceived;
    event Action<string>? StatusChanged;
    event Action<Exception>? ErrorOccurred;

    string StatusText { get; }
    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct);
    void Stop();
}
