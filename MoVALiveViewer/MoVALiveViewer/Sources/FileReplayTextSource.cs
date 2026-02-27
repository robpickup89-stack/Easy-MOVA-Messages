namespace MoVALiveViewer.Sources;

public enum ReplaySpeed
{
    RealTime,
    Fast50,
    Fast200,
    Fast500,
    Instant,
    StepByStep
}

public sealed class FileReplayTextSource : ITextSource
{
    private readonly string _filePath;
    private CancellationTokenSource? _cts;
    private Task? _replayTask;
    private bool _paused;

    public event Action<string>? ChunkReceived;
    public event Action<string>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;

    public string StatusText { get; private set; } = "Idle";
    public bool IsRunning { get; private set; }
    public ReplaySpeed Speed { get; set; } = ReplaySpeed.Fast200;

    public bool IsPaused
    {
        get => _paused;
        set
        {
            _paused = value;
            StatusText = value ? "Paused" : "Replaying";
            StatusChanged?.Invoke(StatusText);
        }
    }

    private readonly ManualResetEventSlim _stepGate = new(false);
    private bool _stepRequested;

    public FileReplayTextSource(string filePath)
    {
        _filePath = filePath;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        StatusText = "Replaying";
        StatusChanged?.Invoke(StatusText);

        _replayTask = Task.Run(() => ReplayLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _stepGate.Set();
        IsRunning = false;
        StatusText = "Stopped";
        StatusChanged?.Invoke(StatusText);
    }

    public void StepNext()
    {
        _stepRequested = true;
        _stepGate.Set();
    }

    private async Task ReplayLoop(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(_filePath);
            int lineCount = 0;

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                while (_paused && !ct.IsCancellationRequested)
                {
                    if (Speed == ReplaySpeed.StepByStep)
                    {
                        _stepGate.Wait(ct);
                        _stepGate.Reset();
                        if (!_stepRequested) continue;
                        _stepRequested = false;
                        break;
                    }
                    await Task.Delay(50, ct);
                }

                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                ChunkReceived?.Invoke(line + "\n");
                lineCount++;

                if (lineCount % 100 == 0)
                {
                    StatusText = $"Replaying ({lineCount} lines)";
                    StatusChanged?.Invoke(StatusText);
                }

                int delayMs = Speed switch
                {
                    ReplaySpeed.RealTime => 1000,
                    ReplaySpeed.Fast50 => 20,
                    ReplaySpeed.Fast200 => 5,
                    ReplaySpeed.Fast500 => 2,
                    ReplaySpeed.Instant => 0,
                    ReplaySpeed.StepByStep => 0,
                    _ => 5
                };

                if (Speed == ReplaySpeed.StepByStep)
                {
                    _paused = true;
                }
                else if (delayMs > 0)
                {
                    await Task.Delay(delayMs, ct);
                }
            }

            StatusText = $"Complete ({lineCount} lines)";
            StatusChanged?.Invoke(StatusText);
            IsRunning = false;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            StatusText = $"Error: {ex.Message}";
            StatusChanged?.Invoke(StatusText);
            IsRunning = false;
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _stepGate.Dispose();
    }
}
