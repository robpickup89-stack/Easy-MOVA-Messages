using System.Collections.Concurrent;
using MoVALiveViewer.Models;

namespace MoVALiveViewer.State;

public sealed class AppState
{
    private readonly object _snapshotLock = new();

    public Snapshot? CurrentSnapshot { get; private set; }
    public Snapshot? PinnedSnapshot { get; private set; }
    public bool IsPinned { get; private set; }

    public RingBuffer<Snapshot> Snapshots { get; }
    public ConcurrentQueue<EventRow> PendingEvents { get; } = new();
    public ConcurrentQueue<string> PendingRawLines { get; } = new();
    public AlertManager Alerts { get; } = new();

    public int TotalLinesProcessed { get; private set; }
    public int TotalSnapshots { get; private set; }

    public event Action<Snapshot>? SnapshotFinalized;

    public AppState(int ringBufferSize = 1000)
    {
        Snapshots = new RingBuffer<Snapshot>(ringBufferSize);
    }

    public void StartNewSnapshot(int stage, TimeSpan time, ParsedRecord headerRecord)
    {
        lock (_snapshotLock)
        {
            if (CurrentSnapshot != null)
            {
                Snapshots.Add(CurrentSnapshot);
                TotalSnapshots++;
                SnapshotFinalized?.Invoke(CurrentSnapshot);
                Alerts.OnStageChanged(CurrentSnapshot);
            }

            CurrentSnapshot = new Snapshot
            {
                Stage = stage,
                TimeOfDay = time,
                Time = DateTime.Today.Add(time),
                SequenceId = TotalSnapshots + 1
            };

            if (headerRecord.Fields.Count > 0)
            {
                foreach (var kv in headerRecord.Fields)
                {
                    if (kv.Key != "Stage" && kv.Key != "Time")
                        CurrentSnapshot.StageFields[kv.Key] = kv.Value;
                }
            }

            CurrentSnapshot.Records.Add(headerRecord);
        }
    }

    public void AddRecordToCurrentSnapshot(ParsedRecord record)
    {
        lock (_snapshotLock)
        {
            if (CurrentSnapshot == null) return;

            CurrentSnapshot.Records.Add(record);

            switch (record.Type)
            {
                case RecordType.StageDetail:
                case RecordType.StageMinLine:
                    foreach (var kv in record.Fields)
                    {
                        if (kv.Key != "Age")
                            CurrentSnapshot.StageFields[kv.Key] = kv.Value;
                    }
                    break;

                case RecordType.NXHeader:
                    if (record.Link.HasValue)
                    {
                        var ls = CurrentSnapshot.GetOrCreateLink(record.Link.Value);
                        if (record.Fields.TryGetValue("ESLI", out var esli))
                            ls.ESLI = esli?.ToString();
                        if (record.Fields.TryGetValue("LAs", out var las) && las is List<LAEntry> laList)
                            ls.LAs = laList;
                        ls.LastUpdated = DateTime.Now;
                    }
                    break;

                case RecordType.NXBDR:
                    if (record.Link.HasValue)
                    {
                        var ls = CurrentSnapshot.GetOrCreateLink(record.Link.Value);
                        if (record.Fields.TryGetValue("BDR", out var bdr) && bdr is BDREntry bdrEntry)
                            ls.BDRs.Add(bdrEntry);
                        ls.LastUpdated = DateTime.Now;
                    }
                    break;

                case RecordType.NXOPT:
                    if (record.Link.HasValue)
                    {
                        var ls = CurrentSnapshot.GetOrCreateLink(record.Link.Value);
                        if (record.Fields.TryGetValue("CF", out var cf) && cf is int cfVal)
                            ls.CF = cfVal;
                        if (record.Fields.TryGetValue("DEM", out var dem))
                        {
                            ls.DEM = dem?.ToString();
                            Alerts.OnDEMUpdated(record.Link.Value, ls.DEM);
                        }
                        if (record.Fields.TryGetValue("DEMRaw", out var demRaw))
                            ls.DEMRaw = demRaw?.ToString();
                        if (record.Fields.TryGetValue("RCX", out var rcx) && rcx is int[] rcxArr)
                            ls.RCX = rcxArr;
                        if (record.Fields.TryGetValue("OptBDR_A", out var a) && a is int av)
                            ls.OptBDR_A = av;
                        if (record.Fields.TryGetValue("OptBDR_B", out var b) && b is int bv)
                            ls.OptBDR_B = bv;
                        if (record.Fields.TryGetValue("OptBDR_C", out var c) && c is int cv)
                            ls.OptBDR_C = cv;
                        ls.LastUpdated = DateTime.Now;
                        Alerts.OnOptReceived(record.Link.Value);
                    }
                    break;

                case RecordType.NXContinuation:
                    if (record.Link.HasValue)
                    {
                        var ls = CurrentSnapshot.GetOrCreateLink(record.Link.Value);
                        if (record.Fields.TryGetValue("IG", out var ig) && ig is int igVal)
                            ls.IG = igVal;
                        if (record.Fields.TryGetValue("SDEM", out var sdem))
                            ls.SDEM = sdem?.ToString();
                        if (record.Fields.TryGetValue("RCIN", out var rcin) && rcin is int[] rcinArr)
                            ls.RCIN = rcinArr;
                        if (record.Fields.TryGetValue("BON", out var bon) && bon is int[] bonArr)
                            ls.BON = bonArr;
                        ls.LastUpdated = DateTime.Now;
                    }
                    break;
            }

            // SAT alert check
            var sat = CurrentSnapshot.SAT;
            if (!string.IsNullOrEmpty(sat))
                Alerts.OnSATUpdated(sat, CurrentSnapshot);
        }
    }

    public void EnqueueEvent(ParsedRecord record, int snapshotSeqId)
    {
        PendingEvents.Enqueue(new EventRow
        {
            Time = record.Timestamp,
            TypeLabel = record.Type.ToString(),
            Stage = record.Stage ?? (CurrentSnapshot?.Stage),
            Link = record.Link,
            Summary = record.Summary,
            SnapshotSeqId = snapshotSeqId,
            RawLine = record.RawLine
        });
    }

    public void EnqueueRawLine(string line)
    {
        PendingRawLines.Enqueue(line);
        TotalLinesProcessed++;
        Alerts.OnDataReceived();
    }

    public Snapshot? GetDisplaySnapshot()
    {
        lock (_snapshotLock)
        {
            return IsPinned ? PinnedSnapshot : CurrentSnapshot;
        }
    }

    public void Pin(int? snapshotSeqId = null)
    {
        lock (_snapshotLock)
        {
            if (snapshotSeqId.HasValue)
            {
                PinnedSnapshot = Snapshots.ToArray()
                    .FirstOrDefault(s => s.SequenceId == snapshotSeqId.Value)
                    ?? CurrentSnapshot?.Clone();
            }
            else
            {
                PinnedSnapshot = CurrentSnapshot?.Clone();
            }
            IsPinned = true;
        }
    }

    public void Unpin()
    {
        lock (_snapshotLock)
        {
            IsPinned = false;
            PinnedSnapshot = null;
        }
    }

    public void Reset()
    {
        lock (_snapshotLock)
        {
            CurrentSnapshot = null;
            PinnedSnapshot = null;
            IsPinned = false;
            Snapshots.Clear();
            TotalLinesProcessed = 0;
            TotalSnapshots = 0;
            Alerts.Reset();

            while (PendingEvents.TryDequeue(out _)) { }
            while (PendingRawLines.TryDequeue(out _)) { }
        }
    }
}
