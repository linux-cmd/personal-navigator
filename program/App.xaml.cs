using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PersonalNavigator;

public partial class App : System.Windows.Application
{
    private const string MutexName = "PersonalNavigator.Singleton.v1";
    private const string PipeName = "PersonalNavigator.Activation.v1";
    private Mutex? _mutex;
    private bool _ownsMutex;
    private CancellationTokenSource? _pipeCancellation;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, MutexName, out bool isFirst);
        _ownsMutex = isFirst;
        if (!isFirst)
        {
            SendActivation(e.Args.FirstOrDefault() ?? string.Empty);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _pipeCancellation = new CancellationTokenSource();
        _ = ListenForActivationsAsync(_pipeCancellation.Token);

        string? screenshot = e.Args.FirstOrDefault(a => a.StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("--screenshot=".Length).Trim('"');
        string? query = e.Args.FirstOrDefault(a => a.StartsWith("--query=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("--query=".Length).Trim('"');
        if (!string.IsNullOrWhiteSpace(screenshot)) _mainWindow.EnableScreenshotMode(screenshot, query);

        bool background = e.Args.Any(a => a.Equals("--background", StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrWhiteSpace(screenshot);
        string? target = e.Args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (!background)
        {
            _mainWindow.Show();
            _mainWindow.ActivateFromExternal(target);
        }
        else
        {
            _mainWindow.InitializeHidden();
        }
    }

    private static void SendActivation(string argument)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(1200);
            byte[] payload = Encoding.UTF8.GetBytes(argument);
            pipe.Write(payload, 0, payload.Length);
        }
        catch
        {
            // The existing instance may still be finishing startup. The global hotkey remains available.
        }
    }

    private async Task ListenForActivationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(cancellationToken);
                using var memory = new MemoryStream();
                await pipe.CopyToAsync(memory, cancellationToken);
                string argument = Encoding.UTF8.GetString(memory.ToArray());
                await Dispatcher.InvokeAsync(() => _mainWindow?.ActivateFromExternal(argument));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(250, cancellationToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pipeCancellation?.Cancel();
        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
