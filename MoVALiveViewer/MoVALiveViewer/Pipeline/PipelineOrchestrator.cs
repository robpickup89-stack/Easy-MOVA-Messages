using System.Threading.Channels;
using MoVALiveViewer.Models;
using MoVALiveViewer.Parsing;
using MoVALiveViewer.Sources;
using MoVALiveViewer.State;

namespace MoVALiveViewer.Pipeline;

public sealed class PipelineOrchestrator : IDisposable
{
    private readonly AppState _state;
    private readonly MoVAParser _parser = new();
    private readonly LineSplitter _splitter = new();
    private readonly Channel<string> _lineChannel;
    private CancellationTokenSource? _cts;
    private Task? _parserTask;
    private ITextSource? _source;
    private StreamWriter? _recordWriter;
    private readonly object _recordLock = new();

    public AppState State => _state;
    public bool IsRecording { get; private set; }
    public string? RecordingPath { get; private set; }

    public event Action<string>? StatusChanged;

    public PipelineOrchestrator(AppState state)
    {
        _state = state;
        _lineChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task StartAsync(ITextSource source)
    {
        Stop();

        _source = source;
        _cts = new CancellationTokenSource();
        _splitter.Reset();

        _source.ChunkReceived += OnChunkReceived;
        _source.StatusChanged += s => StatusChanged?.Invoke(s);

        _parserTask = Task.Run(() => ParserLoop(_cts.Token), _cts.Token);

        await _source.StartAsync(_cts.Token);
        StatusChanged?.Invoke("Running");
    }

    public void Stop()
    {
        _cts?.Cancel();

        if (_source != null)
        {
            _source.ChunkReceived -= OnChunkReceived;
            _source.Stop();
            _source.Dispose();
            _source = null;
        }

        StopRecording();
        _splitter.Reset();
        StatusChanged?.Invoke("Stopped");
    }

    public void StartRecording(string filePath)
    {
        lock (_recordLock)
        {
            StopRecording();
            _recordWriter = new StreamWriter(filePath, append: true) { AutoFlush = false };
            RecordingPath = filePath;
            IsRecording = true;
        }
    }

    public void StopRecording()
    {
        lock (_recordLock)
        {
            _recordWriter?.Flush();
            _recordWriter?.Dispose();
            _recordWriter = null;
            IsRecording = false;
            RecordingPath = null;
        }
    }

    private void OnChunkReceived(string chunk)
    {
        foreach (var line in _splitter.Feed(chunk))
        {
            _lineChannel.Writer.TryWrite(line);
        }
    }

    private async Task ParserLoop(CancellationToken ct)
    {
        await foreach (var line in _lineChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                _state.EnqueueRawLine(line);

                // Record if active
                lock (_recordLock)
                {
                    _recordWriter?.WriteLine(line);
                    if (_state.TotalLinesProcessed % 50 == 0)
                        _recordWriter?.Flush();
                }

                var record = _parser.Parse(line);

                if (record.Type == RecordType.StageHeader && record.Stage.HasValue)
                {
                    _state.StartNewSnapshot(
                        record.Stage.Value,
                        record.TimeOfDay,
                        record);
                }
                else if (_state.CurrentSnapshot != null)
                {
                    _state.AddRecordToCurrentSnapshot(record);
                }

                int seqId = _state.CurrentSnapshot?.SequenceId ?? 0;
                _state.EnqueueEvent(record, seqId);
            }
            catch (Exception)
            {
                // Don't crash the parser loop
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
