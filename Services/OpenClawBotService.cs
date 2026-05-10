using System.Diagnostics;

namespace StokBarangMAUI.Services
{
    /// <summary>
    /// Service untuk mengelola bot WhatsApp OPENCLAW
    /// Bot berjalan sebagai proses Node.js eksternal
    /// </summary>
    public class OpenClawBotService
    {
        private Process? _botProcess;
        private string _botPath;
        private bool _isRunning;

        public bool IsRunning => _isRunning && _botProcess != null && !_botProcess.HasExited;
        public event Action<string>? OnOutput;
        public event Action<string>? OnError;
        public event Action<bool>? OnStatusChanged;

        public OpenClawBotService()
        {
            _botPath = ResolveBotPath();
        }

        /// <summary>
        /// Resolve the OPENCLAW folder path by trying multiple strategies.
        /// </summary>
        private string ResolveBotPath()
        {
            // Strategy 1: Relative to the project source directory (dev-time)
            var candidates = new List<string>
            {
                // From bin/Debug/netX.0/platform, go up to project root
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "OPENCLAW")),
                // From bin output directly
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OPENCLAW")),
                // Alongside the app (Android / published)
                Path.Combine(FileSystem.AppDataDirectory, "OPENCLAW"),
            };

            // Also try Environment.CurrentDirectory for desktop runs
            var cwd = Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(cwd))
            {
                candidates.Insert(0, Path.Combine(cwd, "OPENCLAW"));
                // If cwd is inside bin, also go up
                candidates.Insert(1, Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", "OPENCLAW")));
            }

            foreach (var candidate in candidates)
            {
                try
                {
                    var full = Path.GetFullPath(candidate);
                    if (Directory.Exists(full))
                    {
                        Debug.WriteLine($"[OpenClawBot] Found OPENCLAW at: {full}");
                        return full;
                    }
                }
                catch { /* skip invalid paths */ }
            }

            // Fallback: first candidate (will fail gracefully later)
            var fallback = Path.GetFullPath(candidates[0]);
            Debug.WriteLine($"[OpenClawBot] OPENCLAW not found, using fallback: {fallback}");
            return fallback;
        }

        /// <summary>
        /// Start bot WhatsApp OPENCLAW
        /// </summary>
        public async Task<bool> StartBotAsync()
        {
            if (IsRunning)
            {
                Debug.WriteLine("[OpenClawBot] Bot sudah berjalan");
                return true;
            }

            try
            {
                // Re-resolve path in case folder was created after init
                _botPath = ResolveBotPath();

                // Cek apakah folder OPENCLAW ada
                if (!Directory.Exists(_botPath))
                {
                    OnError?.Invoke($"Folder OPENCLAW tidak ditemukan di: {_botPath}");
                    return false;
                }

                // Cek file index.js
                var indexJs = Path.Combine(_botPath, "src", "index.js");
                if (!File.Exists(indexJs))
                {
                    OnError?.Invoke($"File src/index.js tidak ditemukan di: {_botPath}");
                    return false;
                }

                // Cek apakah npm/node tersedia
                var npmCheck = await CheckNpmInstalledAsync();
                if (!npmCheck)
                {
                    OnError?.Invoke("Node.js/npm tidak terinstall. Install Node.js terlebih dahulu dari nodejs.org");
                    return false;
                }

                // Cek apakah node_modules sudah ada
                var nodeModulesPath = Path.Combine(_botPath, "node_modules");
                if (!Directory.Exists(nodeModulesPath))
                {
                    OnOutput?.Invoke("Installing dependencies...");
                    var installSuccess = await RunNpmInstallAsync();
                    if (!installSuccess)
                    {
                        OnError?.Invoke("Gagal install dependencies. Jalankan 'npm install' manual di folder OPENCLAW.");
                        return false;
                    }
                    OnOutput?.Invoke("Dependencies berhasil diinstall.");
                }

                // Cek apakah .env ada
                var envPath = Path.Combine(_botPath, ".env");
                if (!File.Exists(envPath))
                {
                    // Try to copy .env.example as starter
                    var examplePath = Path.Combine(_botPath, ".env.example");
                    if (File.Exists(examplePath))
                    {
                        File.Copy(examplePath, envPath);
                        OnOutput?.Invoke("File .env dibuat dari .env.example. Silakan edit konfigurasi.");
                    }
                    else
                    {
                        OnError?.Invoke("File .env tidak ditemukan. Copy .env.example ke .env dan isi konfigurasi.");
                        return false;
                    }
                }

                // Determine the correct command for the OS
                var isWindows = OperatingSystem.IsWindows();
                var fileName = isWindows ? "cmd.exe" : "npm";
                var arguments = isWindows ? "/c npm start" : "start";

                // Start bot process
                _botProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = _botPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _botProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.WriteLine($"[OpenClawBot] {e.Data}");
                        OnOutput?.Invoke(e.Data);
                    }
                };

                _botProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // npm often writes to stderr for non-error messages, filter noise
                        if (e.Data.Contains("npm WARN") || e.Data.Contains("npm notice"))
                        {
                            Debug.WriteLine($"[OpenClawBot WARN] {e.Data}");
                            OnOutput?.Invoke(e.Data);
                        }
                        else
                        {
                            Debug.WriteLine($"[OpenClawBot ERROR] {e.Data}");
                            OnError?.Invoke(e.Data);
                        }
                    }
                };

                _botProcess.Exited += (sender, e) =>
                {
                    _isRunning = false;
                    OnStatusChanged?.Invoke(false);
                    Debug.WriteLine("[OpenClawBot] Process exited");
                };

                _botProcess.EnableRaisingEvents = true;
                _botProcess.Start();
                _botProcess.BeginOutputReadLine();
                _botProcess.BeginErrorReadLine();

                _isRunning = true;
                OnStatusChanged?.Invoke(true);
                OnOutput?.Invoke($"Bot WhatsApp OPENCLAW dimulai dari: {_botPath}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OpenClawBot] Error starting bot: {ex.Message}");
                OnError?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop bot WhatsApp OPENCLAW
        /// </summary>
        public void StopBot()
        {
            if (_botProcess != null && !_botProcess.HasExited)
            {
                try
                {
                    _botProcess.Kill(true); // Kill process tree
                    _botProcess.WaitForExit(5000);
                    _botProcess.Dispose();
                    _botProcess = null;
                    _isRunning = false;
                    OnStatusChanged?.Invoke(false);
                    OnOutput?.Invoke("Bot WhatsApp OPENCLAW dihentikan.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OpenClawBot] Error stopping bot: {ex.Message}");
                    OnError?.Invoke($"Error stopping bot: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Cek apakah npm terinstall
        /// </summary>
        private async Task<bool> CheckNpmInstalledAsync()
        {
            try
            {
                var isWindows = OperatingSystem.IsWindows();
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = isWindows ? "cmd.exe" : "npm",
                        Arguments = isWindows ? "/c npm --version" : "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    OnOutput?.Invoke($"npm version: {output.Trim()}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OpenClawBot] npm check error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Install npm dependencies
        /// </summary>
        private async Task<bool> RunNpmInstallAsync()
        {
            try
            {
                var isWindows = OperatingSystem.IsWindows();
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = isWindows ? "cmd.exe" : "npm",
                        Arguments = isWindows ? "/c npm install" : "install",
                        WorkingDirectory = _botPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OnOutput?.Invoke(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OpenClawBot] npm install error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get bot status info
        /// </summary>
        public string GetStatusInfo()
        {
            if (IsRunning)
            {
                return $"✅ Bot aktif (PID: {_botProcess?.Id})";
            }
            return "⭕ Bot tidak aktif";
        }

        /// <summary>
        /// Get the resolved OPENCLAW path
        /// </summary>
        public string GetBotPath() => _botPath;
    }
}
