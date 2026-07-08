using System.Diagnostics;

namespace ColorPicker.Services;

public static class StopwatchService
{
    private static Stopwatch? _stopwatch;
    private static double _sum = 0;
    private static int _counter = 0;
    private static int _sampleSize = 50;

    public static void Start(int sampleSize = 50)
    {
        _sampleSize = sampleSize;
        _stopwatch ??= new Stopwatch();
        _stopwatch.Restart();
    }

    public static void Stop()
    {
        if (_stopwatch == null) return;

        _stopwatch.Stop();

        _sum += _stopwatch.Elapsed.TotalMilliseconds;
        _counter++;

        if (_counter >= _sampleSize)
        {
            var avg = _sum / _counter;
            Console.WriteLine($"Samples: {_counter}, AVG: {avg:F2} ms");
            _counter = 0;
            _sum = 0;
        }
    }
}
