using MoVALiveViewer.Models;

namespace MoVALiveViewer.Parsing;

public sealed class MoVAParser
{
    private static readonly HashSet<string> KnownKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "S", "SMF", "SAT", "LAM", "CUT", "SMCYC", "DMX", "SUS",
        "SMIN", "LMIN", "RCX", "NX", "ESLI", "BDR", "OPT", "CF",
        "DEM", "SDEM", "BON", "RCIN"
    };

    private int _currentLinkNo = -1;
    private int _sequenceId;

    public ParsedRecord Parse(string line)
    {
        var trimmed = line.TrimStart();
        var tokens = Tokenize(line);

        if (trimmed.StartsWith("S ") && tokens.Length >= 3)
            return ParseStageHeader(tokens, line);

        if (trimmed.StartsWith("SMCYC"))
            return ParseStageDetail(tokens, line);

        if (tokens.Length >= 2 && IsInt(tokens[0]) && tokens[1] == "SMIN")
            return ParseStageMinLine(tokens, line);

        if (tokens.Length >= 2 && tokens[0] == "SMIN")
            return ParseStageMinLine(tokens, line);

        var nxIdx = Array.IndexOf(tokens, "NX");
        if (nxIdx >= 0 && nxIdx + 1 < tokens.Length)
        {
            if (int.TryParse(tokens[nxIdx + 1], out var linkNo))
            {
                _currentLinkNo = linkNo;
                return ParseNXLine(tokens, nxIdx, linkNo, line);
            }
        }

        if (trimmed.Contains("IG:") || trimmed.StartsWith("SDEM") || trimmed.StartsWith("BON") || trimmed.StartsWith("RCIN"))
            return ParseContinuation(tokens, line);

        return new ParsedRecord
        {
            Type = RecordType.Other,
            RawLine = line,
            Timestamp = DateTime.Now
        };
    }

    public bool IsSnapshotBoundary(string line)
    {
        return line.TrimStart().StartsWith("S ") && Tokenize(line).Length >= 3;
    }

    public int NextSequenceId() => Interlocked.Increment(ref _sequenceId);

    private ParsedRecord ParseStageHeader(string[] tokens, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.StageHeader,
            RawLine = line,
            Timestamp = DateTime.Now
        };

        int idx = 0;
        if (tokens[idx] == "S") idx++;

        if (idx < tokens.Length && int.TryParse(tokens[idx], out var stage))
        {
            record.Stage = stage;
            record.Fields["Stage"] = stage;
            idx++;
        }

        if (idx < tokens.Length && TryParseTime(tokens[idx], out var tod))
        {
            record.TimeOfDay = tod;
            record.Timestamp = DateTime.Today.Add(tod);
            record.Fields["Time"] = tod;
            idx++;
        }

        ParseKeyValueGroups(tokens, idx, record.Fields);
        return record;
    }

    private ParsedRecord ParseStageDetail(string[] tokens, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.StageDetail,
            RawLine = line,
            Timestamp = DateTime.Now
        };

        ParseKeyValueGroups(tokens, 0, record.Fields);
        return record;
    }

    private ParsedRecord ParseStageMinLine(string[] tokens, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.StageMinLine,
            RawLine = line,
            Timestamp = DateTime.Now
        };

        int startIdx = 0;
        if (tokens.Length > 0 && IsInt(tokens[0]) && tokens[0] != "SMIN")
        {
            record.Fields["Age"] = int.Parse(tokens[0]);
            startIdx = 1;
        }

        ParseKeyValueGroups(tokens, startIdx, record.Fields);
        return record;
    }

    private ParsedRecord ParseNXLine(string[] tokens, int nxIdx, int linkNo, string line)
    {
        int afterLink = nxIdx + 2;

        bool hasESLI = Array.IndexOf(tokens, "ESLI", afterLink) >= 0;
        bool hasOPT = Array.IndexOf(tokens, "OPT", afterLink) >= 0;
        bool hasBDR = Array.IndexOf(tokens, "BDR", afterLink) >= 0;

        int ageValue = -1;
        if (nxIdx > 0 && IsInt(tokens[0]))
            int.TryParse(tokens[0], out ageValue);

        if (hasOPT)
            return ParseNXOPT(tokens, afterLink, linkNo, ageValue, line);

        if (hasBDR && !hasESLI)
            return ParseNXBDR(tokens, afterLink, linkNo, ageValue, line);

        return ParseNXHeader(tokens, afterLink, linkNo, ageValue, line);
    }

    private ParsedRecord ParseNXHeader(string[] tokens, int startIdx, int linkNo, int age, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.NXHeader,
            Link = linkNo,
            RawLine = line,
            Timestamp = DateTime.Now
        };
        if (age >= 0) record.Fields["Age"] = age;

        int idx = startIdx;
        while (idx < tokens.Length)
        {
            if (tokens[idx] == "ESLI" && idx + 1 < tokens.Length)
            {
                record.Fields["ESLI"] = tokens[idx + 1];
                idx += 2;
                continue;
            }

            if (IsLAToken(tokens[idx], out int laneIdx))
            {
                var la = new LAEntry { LaneIndex = laneIdx };
                if (idx + 1 < tokens.Length && int.TryParse(tokens[idx + 1], out var v1)) la.V1 = v1;
                if (idx + 2 < tokens.Length && int.TryParse(tokens[idx + 2], out var v2)) la.V2 = v2;
                if (idx + 3 < tokens.Length && int.TryParse(tokens[idx + 3], out var v3)) la.V3 = v3;

                if (!record.Fields.ContainsKey("LAs"))
                    record.Fields["LAs"] = new List<LAEntry>();
                ((List<LAEntry>)record.Fields["LAs"]).Add(la);
                idx += 4;
                continue;
            }

            idx++;
        }

        return record;
    }

    private ParsedRecord ParseNXBDR(string[] tokens, int startIdx, int linkNo, int age, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.NXBDR,
            Link = linkNo,
            RawLine = line,
            Timestamp = DateTime.Now
        };
        if (age >= 0) record.Fields["Age"] = age;

        int idx = startIdx;

        // May have a time token before BDR
        if (idx < tokens.Length && TryParseTime(tokens[idx], out var tod))
        {
            record.TimeOfDay = tod;
            idx++;
        }

        var bdr = new BDREntry();

        while (idx < tokens.Length)
        {
            if (tokens[idx] == "BDR")
            {
                if (idx + 1 < tokens.Length && int.TryParse(tokens[idx + 1], out var a)) bdr.A = a;
                if (idx + 2 < tokens.Length && int.TryParse(tokens[idx + 2], out var b)) bdr.B = b;
                if (idx + 3 < tokens.Length && int.TryParse(tokens[idx + 3], out var c)) bdr.C = c;
                idx += 4;
                continue;
            }

            if (IsLKToken(tokens[idx], out int laneIdx))
            {
                var lk = new LKEntry { LaneIndex = laneIdx };
                if (idx + 1 < tokens.Length && int.TryParse(tokens[idx + 1], out var a)) lk.A = a;
                if (idx + 2 < tokens.Length && int.TryParse(tokens[idx + 2], out var b)) lk.B = b;
                if (idx + 3 < tokens.Length && int.TryParse(tokens[idx + 3], out var c)) lk.C = c;
                if (idx + 4 < tokens.Length && int.TryParse(tokens[idx + 4], out var d)) lk.D = d;
                bdr.LKEntries.Add(lk);
                idx += 5;
                continue;
            }

            idx++;
        }

        record.Fields["BDR"] = bdr;
        return record;
    }

    private ParsedRecord ParseNXOPT(string[] tokens, int startIdx, int linkNo, int age, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.NXOPT,
            Link = linkNo,
            RawLine = line,
            Timestamp = DateTime.Now
        };
        if (age >= 0) record.Fields["Age"] = age;

        int idx = startIdx;

        // Time token before OPT
        if (idx < tokens.Length && TryParseTime(tokens[idx], out var tod))
        {
            record.TimeOfDay = tod;
            record.Timestamp = DateTime.Today.Add(tod);
            idx++;
        }

        while (idx < tokens.Length)
        {
            if (tokens[idx] == "OPT") { idx++; continue; }

            if (tokens[idx] == "BDR")
            {
                if (idx + 1 < tokens.Length && int.TryParse(tokens[idx + 1], out var a)) record.Fields["OptBDR_A"] = a;
                if (idx + 2 < tokens.Length && int.TryParse(tokens[idx + 2], out var b)) record.Fields["OptBDR_B"] = b;
                if (idx + 3 < tokens.Length && int.TryParse(tokens[idx + 3], out var c)) record.Fields["OptBDR_C"] = c;
                idx += 4;
                continue;
            }

            if (tokens[idx] == "CF" && idx + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[idx + 1], out var cf))
                    record.Fields["CF"] = cf;
                idx += 2;
                continue;
            }

            if (tokens[idx] == "DEM")
            {
                idx++;
                var demParts = new List<string>();
                while (idx < tokens.Length && !KnownKeywords.Contains(tokens[idx]) && !tokens[idx].StartsWith("IG:"))
                {
                    demParts.Add(tokens[idx]);
                    idx++;
                }
                record.Fields["DEM"] = string.Join(" ", demParts);
                record.Fields["DEMRaw"] = string.Join(" ", demParts);
                continue;
            }

            if (tokens[idx] == "RCX")
            {
                idx++;
                var rcx = new List<int>();
                while (idx < tokens.Length && int.TryParse(tokens[idx], out var v))
                {
                    rcx.Add(v);
                    idx++;
                }
                record.Fields["RCX"] = rcx.ToArray();
                continue;
            }

            idx++;
        }

        return record;
    }

    private ParsedRecord ParseContinuation(string[] tokens, string line)
    {
        var record = new ParsedRecord
        {
            Type = RecordType.NXContinuation,
            Link = _currentLinkNo >= 0 ? _currentLinkNo : null,
            RawLine = line,
            Timestamp = DateTime.Now
        };

        int idx = 0;
        while (idx < tokens.Length)
        {
            if (tokens[idx].StartsWith("IG:"))
            {
                var igStr = tokens[idx][3..];
                if (int.TryParse(igStr, out var ig))
                    record.Fields["IG"] = ig;
                idx++;
                continue;
            }

            if (tokens[idx] == "SDEM" && idx + 1 < tokens.Length)
            {
                record.Fields["SDEM"] = tokens[idx + 1];
                idx += 2;
                continue;
            }

            if (tokens[idx] == "BON")
            {
                idx++;
                var bon = new List<int>();
                while (idx < tokens.Length && int.TryParse(tokens[idx], out var v))
                {
                    bon.Add(v);
                    idx++;
                }
                record.Fields["BON"] = bon.ToArray();
                continue;
            }

            if (tokens[idx] == "RCIN")
            {
                idx++;
                var rcin = new List<int>();
                while (idx < tokens.Length && int.TryParse(tokens[idx], out var v))
                {
                    rcin.Add(v);
                    idx++;
                }
                record.Fields["RCIN"] = rcin.ToArray();
                continue;
            }

            idx++;
        }

        return record;
    }

    private void ParseKeyValueGroups(string[] tokens, int startIdx, Dictionary<string, object> fields)
    {
        int idx = startIdx;
        while (idx < tokens.Length)
        {
            var token = tokens[idx];

            if (token == "SMF")
            {
                idx++;
                var vals = CollectInts(tokens, ref idx);
                fields["SMF"] = vals;
                continue;
            }

            if (token == "SAT")
            {
                idx++;
                var parts = new List<string>();
                while (idx < tokens.Length && !KnownKeywords.Contains(tokens[idx]))
                {
                    parts.Add(tokens[idx]);
                    idx++;
                }
                fields["SAT"] = string.Join(" ", parts);
                continue;
            }

            if (token == "LAM" && idx + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[idx + 1], out var v))
                    fields["LAM"] = v;
                idx += 2;
                continue;
            }

            if (token == "CUT" && idx + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[idx + 1], out var v))
                    fields["CUT"] = v;
                idx += 2;
                continue;
            }

            if (token == "SMCYC" && idx + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[idx + 1], out var v))
                    fields["SMCYC"] = v;
                idx += 2;
                continue;
            }

            if (token == "DMX")
            {
                idx++;
                var vals = CollectInts(tokens, ref idx);
                fields["DMX"] = vals;
                continue;
            }

            if (token == "SUS")
            {
                idx++;
                var parts = new List<string>();
                while (idx < tokens.Length && !KnownKeywords.Contains(tokens[idx]))
                {
                    parts.Add(tokens[idx]);
                    idx++;
                }
                fields["SUS"] = parts.ToArray();
                continue;
            }

            if (token == "SMIN" && idx + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[idx + 1], out var v))
                    fields["SMIN"] = v;
                idx += 2;
                continue;
            }

            if (token == "LMIN")
            {
                idx++;
                var vals = CollectInts(tokens, ref idx);
                fields["LMIN"] = vals;
                continue;
            }

            if (token == "RCX")
            {
                idx++;
                var vals = CollectInts(tokens, ref idx);
                fields["RCX"] = vals;
                continue;
            }

            idx++;
        }
    }

    private static int[] CollectInts(string[] tokens, ref int idx)
    {
        var list = new List<int>();
        while (idx < tokens.Length && int.TryParse(tokens[idx], out var v))
        {
            list.Add(v);
            idx++;
        }
        return list.ToArray();
    }

    private static string[] Tokenize(string line)
    {
        return line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool IsInt(string s) => int.TryParse(s, out _);

    private static bool TryParseTime(string s, out TimeSpan result)
    {
        result = default;
        if (s.Length >= 7 && s.Contains(':'))
            return TimeSpan.TryParse(s, out result);
        return false;
    }

    private static bool IsLAToken(string token, out int laneIndex)
    {
        laneIndex = 0;
        if (token.Length >= 3 && token.EndsWith("LA") && int.TryParse(token[..^2], out laneIndex))
            return true;
        return false;
    }

    private static bool IsLKToken(string token, out int laneIndex)
    {
        laneIndex = 0;
        if (token.Length >= 3 && token.EndsWith("LK") && int.TryParse(token[..^2], out laneIndex))
            return true;
        return false;
    }
}
