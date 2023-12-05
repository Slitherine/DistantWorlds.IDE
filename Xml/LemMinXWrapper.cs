using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DW2IDE.Xml;

/// <summary>
/// A wrapper around a native image build of the LemMinX XML Language Server.
/// </summary>
/// <remarks>
/// Communicates over stdin/stdout.
/// Reads from stderr for debugging.
/// Automatically restarts as needed.
/// </remarks>
public class LemMinXWrapper : IDuplexPipe, IDisposable {

  private Process? _proc;

  private int _procId;

  private bool _shouldExit = false;

  public LemMinXWrapper(string pathToExe) {
    var processStartInfo = new ProcessStartInfo {
#if DEBUG
      EnvironmentVariables = { ["LEMMINX_DEBUG"] = "1" },
#endif
      FileName = pathToExe,
      Arguments = "",
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };
    _proc = new() { StartInfo = processStartInfo };

    _proc.Exited += (_, _) => {
      if (_shouldExit) return;

      var oldProc = _proc;
      var oldPid = _procId;

      if (Interlocked.CompareExchange(ref _proc!, null, oldProc)
          == oldProc) {
        StartShutdownProcess(oldProc, oldPid);
        oldProc.Dispose();
      }
    };
    _proc.EnableRaisingEvents = true;

#if DEBUG
    _proc.ErrorDataReceived += (_, args) => {
      if (args.Data is null) return;

      Console.Error.WriteLine($"{DateTime.Now:s} LEMMINX: {args.Data}");
    };
#endif
    _proc.Start();
    _procId = _proc.Id;
#if DEBUG
    _proc.BeginErrorReadLine();
#endif
  }

  [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
  private static void StartShutdownProcess(Process oldProc, int pid) {
    ThreadPool.QueueUserWorkItem(static o => {
      if (o is null) return;

      var (oldProc, pid) = ((Process, int))o;

      // give a chance for the process to exit on its own
      try {
        if (oldProc.WaitForExit(1000))
          return;
      }
      catch {
        // oof
        try {
          oldProc = Process.GetProcessById(pid);
        }
        catch {
          return; // already dead
        }
      }

      try {
        oldProc.StandardInput.Close();
      }
      catch {
        // oof
        try {
          oldProc = Process.GetProcessById(pid);
        }
        catch {
          return; // already dead
        }
      }

      try {
        if (!oldProc.HasExited && !oldProc.WaitForExit(250))
          oldProc.Kill();
      }
      catch {
        // oof
        try {
          try {
            oldProc = Process.GetProcessById(pid);
          }
          catch {
            return; // already dead or oof
          }

          if (!oldProc.HasExited && !oldProc.WaitForExit(250))
            oldProc.Kill();
        }
        catch {
          // oof
        }
      }

      try {
        oldProc.Dispose();
      }
      catch {
        // oof
      }
    }, (oldProc, pid));
  }

  public void Dispose() {
    _shouldExit = true;
    try {
      var proc = _proc;
      if (proc is not null) {
        if (!proc.HasExited)
          StartShutdownProcess(proc, proc.Id);
        else
          proc.Dispose();
      }
    }
    catch {
      // oof
    }

    _proc = null!;
  }

  public Stream? InputStream => _proc?.StandardInput.BaseStream;

  public Stream? OutputStream => _proc?.StandardOutput.BaseStream;

  public PipeReader? CreateReader() {
    var s = OutputStream;
    if (s is null) return null;

    return PipeReader.Create(s, new(leaveOpen: true));
  }

  PipeReader IDuplexPipe.Input => CreateReader() ?? throw new ObjectDisposedException(nameof(LemMinXWrapper));

  public PipeWriter? CreateWriter() {
    var s = InputStream;
    if (s is null) return null;

    return PipeWriter.Create(s, new(leaveOpen: true));
  }

  PipeWriter IDuplexPipe.Output => CreateWriter() ?? throw new ObjectDisposedException(nameof(LemMinXWrapper));

}