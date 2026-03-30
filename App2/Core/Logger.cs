using System;
using System.IO;

public static class Logger
{
    static StreamWriter _writer;
    static string       _path;

    public static void Init()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDir  = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);

            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _path = Path.Combine(logDir, $"game_{stamp}.log");
            _writer = new StreamWriter(_path, append: false) { AutoFlush = true };

            Log("=== BRAWLHAVEN LOG ===");
            Log($"Zeit:       {DateTime.Now}");
            Log($"OS:         {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            Log($"Arch:       {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}");
            Log($"Runtime:    {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Log($"Log-Datei:  {_path}");
            Log("=====================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logger konnte nicht initialisiert werden: {ex.Message}");
        }
    }

    public static void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try { _writer?.WriteLine(line); } catch { }
        Console.WriteLine(line);
    }

    public static void LogException(string context, Exception ex)
    {
        Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}");
        Log($"  StackTrace: {ex.StackTrace?.Replace("\n", "\n             ")}");
        if (ex.InnerException != null)
            Log($"  Inner: {ex.InnerException.Message}");
    }

    public static void Close()
    {
        Log("=== Spiel beendet ===");
        _writer?.Close();
        _writer = null;
    }
}
