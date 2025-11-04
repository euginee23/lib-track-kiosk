using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace lib_track_kiosk.configs
{
    /// <summary>
    /// Lightweight reusable helper to open/close an on-screen keyboard executable.
    /// Default path is provided by FileLocations.OnScreenKeyboardExecutable.
    /// You can override the path via the constructor or Open(path).
    /// When the keyboard is open this class installs a low-level keyboard hook;
    /// pressing Enter (Return) will automatically close the keyboard.
    /// </summary>
    internal class OnScreenKeyboard : IDisposable
    {
        private readonly object _sync = new object();
        private string _exePath;
        private Process _process; // the process we started (if any)
        private bool _disposed;

        // keyboard hook related
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _procDelegate;

        /// <summary>
        /// Default constructor uses the path defined in FileLocations.OnScreenKeyboardExecutable.
        /// </summary>
        public OnScreenKeyboard()
            : this(FileLocations.OnScreenKeyboardExecutable)
        {
        }

        /// <summary>
        /// Constructor that accepts a custom exe path.
        /// </summary>
        public OnScreenKeyboard(string exePath)
        {
            _exePath = exePath ?? throw new ArgumentNullException(nameof(exePath));
            _procDelegate = HookCallback;
        }

        /// <summary>
        /// Full path to the on-screen keyboard executable used by this helper.
        /// Can be changed at runtime by calling Open(customPath) or setting via constructor.
        /// </summary>
        public string ExecutablePath
        {
            get
            {
                lock (_sync) { return _exePath; }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Path cannot be empty.", nameof(value));
                lock (_sync) { _exePath = value; }
            }
        }

        /// <summary>
        /// Returns true if an instance of the keyboard is currently running (either started by this helper or found on the system by name).
        /// </summary>
        public bool IsOpen
        {
            get
            {
                lock (_sync)
                {
                    if (_process != null && !_process.HasExited)
                        return true;

                    // fallback: check any running processes with matching file name
                    var name = Path.GetFileNameWithoutExtension(_exePath);
                    if (string.IsNullOrEmpty(name)) return false;
                    return Process.GetProcessesByName(name).Any(p => !p.HasExited);
                }
            }
        }

        /// <summary>
        /// Open the on-screen keyboard using the configured executable path.
        /// If it's already running this returns true and does nothing.
        /// </summary>
        /// <returns>true if started or already running; false otherwise</returns>
        public bool Open()
        {
            return Open(_exePath);
        }

        /// <summary>
        /// Open the on-screen keyboard using a path provided by the caller.
        /// </summary>
        /// <param name="exePath">Full path to the keyboard exe</param>
        /// <returns>true if started or already running; false otherwise</returns>
        public bool Open(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath)) throw new ArgumentNullException(nameof(exePath));

            lock (_sync)
            {
                _exePath = exePath;

                try
                {
                    if (IsOpen)
                    {
                        // ensure hook installed if keyboard already open
                        EnsureHookInstalled();
                        return true;
                    }

                    if (!File.Exists(_exePath))
                        return false;

                    var psi = new ProcessStartInfo
                    {
                        FileName = _exePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(_exePath) ?? Environment.CurrentDirectory
                    };

                    _process = Process.Start(psi);

                    // sometimes the process starts and exits quickly; give brief time to ensure it's alive
                    Thread.Sleep(150);

                    if (_process != null && !_process.HasExited)
                    {
                        EnsureHookInstalled();
                        return true;
                    }

                    // fallback: check if process exists by name
                    var name = Path.GetFileNameWithoutExtension(_exePath);
                    var found = Process.GetProcessesByName(name).Any(p => !p.HasExited);
                    if (found)
                        EnsureHookInstalled();

                    return found;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Asynchronous version of Open that returns a Task of bool.
        /// </summary>
        public Task<bool> OpenAsync()
        {
            return Task.Run(() => Open());
        }

        /// <summary>
        /// Close the on-screen keyboard. Attempts a polite CloseMainWindow first, then kills remaining processes if necessary.
        /// Returns true if there is no running keyboard after the call.
        /// </summary>
        /// <returns>true if closed (or not running), false on failure</returns>
        public bool Close()
        {
            lock (_sync)
            {
                try
                {
                    // uninstall hook first to avoid race between hook callback and process shutdown
                    UninstallHook();

                    var name = Path.GetFileNameWithoutExtension(_exePath);
                    // If we have a tracked process that we started, try to close it first.
                    if (_process != null && !_process.HasExited)
                    {
                        try
                        {
                            if (_process.CloseMainWindow())
                            {
                                if (_process.WaitForExit(1500))
                                {
                                    _process.Dispose();
                                    _process = null;
                                }
                            }

                            if (_process != null && !_process.HasExited)
                            {
                                _process.Kill(true);
                                _process.WaitForExit(1000);
                                _process.Dispose();
                                _process = null;
                            }
                        }
                        catch
                        {
                            try { _process?.Kill(true); } catch { }
                            try { _process?.Dispose(); } catch { }
                            _process = null;
                        }
                    }

                    // Ensure we also try to close any other running instances with same exe name.
                    if (!string.IsNullOrEmpty(name))
                    {
                        var procs = Process.GetProcessesByName(name);
                        foreach (var p in procs)
                        {
                            try
                            {
                                if (!p.HasExited)
                                {
                                    if (p.CloseMainWindow())
                                    {
                                        p.WaitForExit(1200);
                                    }

                                    if (!p.HasExited)
                                    {
                                        p.Kill(true);
                                        p.WaitForExit(1000);
                                    }
                                }
                            }
                            catch
                            {
                                try { if (!p.HasExited) p.Kill(true); } catch { }
                            }
                            finally
                            {
                                try { p.Dispose(); } catch { }
                            }
                        }
                    }

                    // final check
                    return !IsOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Asynchronous version of Close.
        /// </summary>
        public Task<bool> CloseAsync()
        {
            return Task.Run(() => Close());
        }

        /// <summary>
        /// Toggle the keyboard open/close state.
        /// </summary>
        public bool Toggle()
        {
            if (IsOpen) return Close();
            return Open();
        }

        /// <summary>
        /// Dispose pattern to ensure any started process/hook is cleaned up.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            lock (_sync)
            {
                try
                {
                    UninstallHook();
                    Close();
                }
                catch { }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        // ------------------- Keyboard hook implementation -------------------

        private void EnsureHookInstalled()
        {
            // Install hook only if we haven't already
            if (_hookId != IntPtr.Zero)
                return;

            try
            {
                _hookId = SetHook(_procDelegate);
            }
            catch
            {
                _hookId = IntPtr.Zero;
            }
        }

        private void UninstallHook()
        {
            try
            {
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
            }
            catch
            {
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // nCode >= 0 indicates we should process the message
            if (nCode >= 0)
            {
                int wParamInt = wParam.ToInt32();
                if (wParamInt == WM_KEYDOWN || wParamInt == WM_SYSKEYDOWN)
                {
                    try
                    {
                        int vkCode = Marshal.ReadInt32(lParam);
                        // VK_RETURN = 0x0D, VK_SEPARATOR / Numpad Enter = 0x6E
                        if (vkCode == VK_RETURN || vkCode == VK_SEPARATOR)
                        {
                            // Close keyboard asynchronously to avoid doing heavy work inside the hook callback
                            Task.Run(() =>
                            {
                                try
                                {
                                    Close();
                                }
                                catch { /* swallow */ }
                            });
                        }
                    }
                    catch
                    {
                        // swallow exceptions in hook
                    }
                }
            }

            // pass along to next hook
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ------------------- Win32 interop -------------------

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_RETURN = 0x0D;
        private const int VK_SEPARATOR = 0x6E; // Numpad Enter on some keyboards

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}