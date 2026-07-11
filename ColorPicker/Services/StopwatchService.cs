using System.Diagnostics;

namespace ColorPicker.Services;

public static class StopwatchService
{
    private static Stopwatch? _stopwatch;
    private static double _sum = 0;
    private static int _counter = 0;
    private static int _sampleSize = 100;
    private static string _txt = "";
    private static int _callCount;
    private static DateTime _callStart = DateTime.UtcNow;
    private static int _renderFrameCount;
    private static TimeSpan _renderWindowStart;
    private static bool _renderWindowInitialized;


    [Conditional("DEBUG")]
    public static void Start(int sampleSize = 100, string txt = "")
    {
        _txt = txt;
        _sampleSize = sampleSize;
        _stopwatch ??= new Stopwatch();
        _stopwatch.Restart();
    }

    [Conditional("DEBUG")]
    public static void Stop()
    {
        if (_stopwatch == null) return;

        _stopwatch.Stop();

        _sum += _stopwatch.Elapsed.TotalMilliseconds;
        _counter++;

        if (_counter >= _sampleSize)
        {
            var avg = _sum / _counter;
            Console.WriteLine($"{_txt}: {_counter} | Enabled: {State.IsEnabled} | AVG: {avg:F2}ms");
            _counter = 0;
            _sum = 0;
        }
    }

    [Conditional("DEBUG")]
    public static void TrackFunctionCallRate([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        _callCount++;

        if ((DateTime.UtcNow - _callStart).TotalSeconds >= 1)
        {
            var methodText = string.IsNullOrEmpty(caller) ? "" : $" {caller}()";
            Console.WriteLine($"Calls/sec{methodText}: {_callCount}");

            _callCount = 0;
            _callStart = DateTime.UtcNow;
        }
    }

    [Conditional("DEBUG")]
    public static void TrackRenderFps(TimeSpan renderingTime)
    {
        if (!_renderWindowInitialized)
        {
            _renderWindowInitialized = true;
            _renderWindowStart = renderingTime;
        }

        _renderFrameCount++;

        var elapsed = renderingTime - _renderWindowStart;
        if (elapsed.TotalSeconds >= 1)
        {
            double fps = _renderFrameCount / elapsed.TotalSeconds;
            Console.WriteLine($"WPF render fps: {fps:F1}");

            _renderFrameCount = 0;
            _renderWindowStart = renderingTime;
        }
    }
}
