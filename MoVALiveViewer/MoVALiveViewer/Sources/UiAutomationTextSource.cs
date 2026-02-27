using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MoVALiveViewer.Sources;

public sealed class UiAutomationTextSource : ITextSource
{
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private string _lastText = string.Empty;
    private IntPtr _targetHwnd;
    private int _pollIntervalMs = 200;

    public event Action<string>? ChunkReceived;
    public event Action<string>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;

    public string StatusText { get; private set; } = "Disconnected";
    public bool IsRunning { get; private set; }
    public string? TargetProcessName { get; set; }
    public string? TargetWindowTitle { get; set; }

    public int PollIntervalMs
    {
        get => _pollIntervalMs;
        set => _pollIntervalMs = Math.Max(50, Math.Min(2000, value));
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _lastText = string.Empty;

        if (!FindTargetWindow())
        {
            StatusText = "Target window not found";
            StatusChanged?.Invoke(StatusText);
            return Task.CompletedTask;
        }

        IsRunning = true;
        StatusText = "Connected";
        StatusChanged?.Invoke(StatusText);

        _pollTask = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
        IsRunning = false;
        _targetHwnd = IntPtr.Zero;
        StatusText = "Disconnected";
        StatusChanged?.Invoke(StatusText);
    }

    public bool FindTargetWindow()
    {
        if (string.IsNullOrEmpty(TargetProcessName))
            return false;

        try
        {
            var processes = Process.GetProcessesByName(TargetProcessName);
            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    if (string.IsNullOrEmpty(TargetWindowTitle) ||
                        (proc.MainWindowTitle?.Contains(TargetWindowTitle, StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        _targetHwnd = proc.MainWindowHandle;
                        return true;
                    }
                }
                proc.Dispose();
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }

        return false;
    }

    public static List<(int pid, string name, string title)> GetProcessesWithWindows()
    {
        var result = new List<(int, string, string)>();
        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                        result.Add((proc.Id, proc.ProcessName, proc.MainWindowTitle));
                }
                catch { }
                finally { proc.Dispose(); }
            }
        }
        catch { }
        return result.OrderBy(p => p.name).ToList();
    }

    private async Task PollLoop(CancellationToken ct)
    {
        int consecutiveErrors = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollIntervalMs, ct);

                if (_targetHwnd == IntPtr.Zero || !IsWindow(_targetHwnd))
                {
                    StatusText = "Target window lost";
                    StatusChanged?.Invoke(StatusText);
                    if (!FindTargetWindow())
                    {
                        await Task.Delay(1000, ct);
                        continue;
                    }
                }

                var editHwnd = FindEditControl(_targetHwnd);
                if (editHwnd == IntPtr.Zero)
                {
                    if (consecutiveErrors++ > 10)
                    {
                        StatusText = "No edit control found";
                        StatusChanged?.Invoke(StatusText);
                    }
                    continue;
                }

                var currentText = GetWindowText(editHwnd);
                if (currentText == null) continue;

                consecutiveErrors = 0;
                var delta = ComputeDelta(_lastText, currentText);

                if (!string.IsNullOrEmpty(delta))
                {
                    _lastText = currentText;
                    ChunkReceived?.Invoke(delta);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                consecutiveErrors++;
                if (consecutiveErrors <= 3)
                    ErrorOccurred?.Invoke(ex);
                else
                {
                    StatusText = $"Errors ({consecutiveErrors})";
                    StatusChanged?.Invoke(StatusText);
                }
            }
        }
    }

    private string ComputeDelta(string lastText, string currentText)
    {
        if (string.IsNullOrEmpty(lastText))
            return currentText;

        if (currentText.StartsWith(lastText))
            return currentText[lastText.Length..];

        int tailLen = Math.Min(4096, lastText.Length);
        var tail = lastText[^tailLen..];
        int idx = currentText.IndexOf(tail, StringComparison.Ordinal);

        if (idx >= 0)
            return currentText[(idx + tail.Length)..];

        return currentText;
    }

    private static IntPtr FindEditControl(IntPtr parentHwnd)
    {
        IntPtr found = IntPtr.Zero;
        EnumChildWindows(parentHwnd, (hwnd, _) =>
        {
            var className = GetClassName(hwnd);
            if (className != null &&
                (className.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                 className.Contains("RichEdit", StringComparison.OrdinalIgnoreCase) ||
                 className.Contains("TextBox", StringComparison.OrdinalIgnoreCase) ||
                 className.Contains("RICHEDIT", StringComparison.OrdinalIgnoreCase)))
            {
                found = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static string? GetWindowText(IntPtr hwnd)
    {
        int len = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        if (len <= 0) return string.Empty;

        var buffer = new char[len + 1];
        unsafe
        {
            fixed (char* p = buffer)
            {
                SendMessage(hwnd, WM_GETTEXT, (IntPtr)(len + 1), (IntPtr)p);
            }
        }
        return new string(buffer, 0, len);
    }

    private static string? GetClassName(IntPtr hwnd)
    {
        var buffer = new char[256];
        unsafe
        {
            fixed (char* p = buffer)
            {
                int len = GetClassName(hwnd, (IntPtr)p, 256);
                if (len > 0) return new string(buffer, 0, len);
            }
        }
        return null;
    }

    // Win32 Interop
    private const int WM_GETTEXT = 0x000D;
    private const int WM_GETTEXTLENGTH = 0x000E;

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parentHwnd, EnumChildProc callback, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, IntPtr lpClassName, int nMaxCount);

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
