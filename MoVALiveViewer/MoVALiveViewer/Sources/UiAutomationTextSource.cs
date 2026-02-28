using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MoVALiveViewer.Sources;

public sealed partial class UiAutomationTextSource : ITextSource
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
    public int TargetControlIndex { get; set; }

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

    /// <summary>
    /// Returns only processes whose main window contains at least one Edit/RichEdit/TextBox child control.
    /// </summary>
    public static List<(int pid, string name, string title)> GetProcessesWithWindows()
    {
        var result = new List<(int pid, string name, string title)>();
        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                    {
                        if (HasEditControl(proc.MainWindowHandle))
                            result.Add((proc.Id, proc.ProcessName, proc.MainWindowTitle));
                    }
                }
                catch { }
                finally { proc.Dispose(); }
            }
        }
        catch { }
        return result.OrderBy(p => p.name).ToList();
    }

    /// <summary>
    /// Enumerates all edit controls within a given window, returning their class name and text preview.
    /// </summary>
    public static List<(int index, string className, string textPreview)> GetEditControlsForWindow(IntPtr parentHwnd)
    {
        var controls = new List<(int index, string className, string textPreview)>();
        int idx = 0;
        EnumChildWindows(parentHwnd, (hwnd, _) =>
        {
            var className = GetClassName(hwnd);
            if (className != null && IsEditClassName(className))
            {
                var text = GetWindowText(hwnd);
                var preview = string.IsNullOrEmpty(text) ? "(empty)" :
                    text.Length > 50 ? text[..50].Replace("\r", "").Replace("\n", " ") + "..." : text.Replace("\r", "").Replace("\n", " ");
                controls.Add((idx, className, preview));
                idx++;
            }
            return true;
        }, IntPtr.Zero);
        return controls;
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

                var editHwnd = FindEditControlByIndex(_targetHwnd, TargetControlIndex);
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

    private static string ComputeDelta(string lastText, string currentText)
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

    private static bool IsEditClassName(string className) =>
        className.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
        className.Contains("RichEdit", StringComparison.OrdinalIgnoreCase) ||
        className.Contains("TextBox", StringComparison.OrdinalIgnoreCase) ||
        className.Contains("RICHEDIT", StringComparison.OrdinalIgnoreCase);

    private static bool HasEditControl(IntPtr parentHwnd)
    {
        bool found = false;
        EnumChildWindows(parentHwnd, (hwnd, _) =>
        {
            var className = GetClassName(hwnd);
            if (className != null && IsEditClassName(className))
            {
                found = true;
                return false; // stop enumeration
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static IntPtr FindEditControl(IntPtr parentHwnd)
    {
        return FindEditControlByIndex(parentHwnd, 0);
    }

    private static IntPtr FindEditControlByIndex(IntPtr parentHwnd, int targetIndex)
    {
        IntPtr found = IntPtr.Zero;
        int currentIndex = 0;
        EnumChildWindows(parentHwnd, (hwnd, _) =>
        {
            var className = GetClassName(hwnd);
            if (className != null && IsEditClassName(className))
            {
                if (currentIndex == targetIndex)
                {
                    found = hwnd;
                    return false;
                }
                currentIndex++;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static string? GetWindowText(IntPtr hwnd)
    {
        int len = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        if (len <= 0) return string.Empty;

        IntPtr buffer = Marshal.AllocHGlobal((len + 1) * sizeof(char));
        try
        {
            SendMessage(hwnd, WM_GETTEXT, (IntPtr)(len + 1), buffer);
            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? GetClassName(IntPtr hwnd)
    {
        IntPtr buffer = Marshal.AllocHGlobal(256 * sizeof(char));
        try
        {
            int len = GetClassName(hwnd, buffer, 256);
            if (len > 0) return Marshal.PtrToStringUni(buffer, len);
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    // Win32 Interop
    private const int WM_GETTEXT = 0x000D;
    private const int WM_GETTEXTLENGTH = 0x000E;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindow(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parentHwnd, EnumChildProc callback, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial int GetClassName(IntPtr hwnd, IntPtr lpClassName, int nMaxCount);

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
