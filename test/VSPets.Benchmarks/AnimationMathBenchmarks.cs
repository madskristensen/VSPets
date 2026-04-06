using System.Windows.Media;
using System.Windows.Media.Imaging;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using VSPets.Animation;
using VSPets.Models;

namespace VSPets.Benchmarks;

/// <summary>
/// Benchmarks for per-frame animation math and cache eviction — hot-path code
/// that runs every tick (~30 FPS) for every pet on screen.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class AnimationMathBenchmarks
{
    // ----- Leg position math (called once per Draw* method per pet per frame) -----

    [Params(PetState.Idle, PetState.Walking, PetState.Running, PetState.Happy)]
    public PetState State { get; set; }

    [Benchmark(Description = "GetLegPositions (per-frame math)")]
    public (double, double, double) GetLegPositions()
    {
        // Mirrors ProceduralSpriteRenderer.GetLegPositions — private, so reproduced here
        int frame = 2; // Mid-cycle frame
        switch (State)
        {
            case PetState.Walking:
            case PetState.Exiting:
            case PetState.Entering:
                var walkPhase = (frame % 4) * (Math.PI / 2);
                return (
                    Math.Sin(walkPhase) * 2,
                    Math.Sin(walkPhase + Math.PI) * 2,
                    Math.Abs(Math.Sin(walkPhase * 2)) * 0.5);

            case PetState.Running:
            case PetState.Chasing:
                var runPhase = (frame % 4) * (Math.PI / 2);
                return (
                    Math.Sin(runPhase) * 3,
                    Math.Sin(runPhase + Math.PI) * 3,
                    Math.Abs(Math.Sin(runPhase * 2)) * 1.5);

            case PetState.Idle:
            case PetState.Sleeping:
                var breathPhase = (frame % 2) * Math.PI;
                return (0, 0, Math.Sin(breathPhase) * 0.3);

            case PetState.Happy:
                var happyPhase = (frame % 2) * Math.PI;
                return (0, 0, Math.Abs(Math.Sin(happyPhase)) * 2);

            default:
                return (0, 0, 0);
        }
    }

    // ----- Breathing animation (runs every tick for idle/sleeping pets) -----

    private double _breathingPhase;
    private const double _breathingSpeed = 1.5;
    private const double _breathingAmplitude = 0.03;

    [Benchmark(Description = "UpdateBreathingAnimation (per tick)")]
    public double UpdateBreathingAnimation()
    {
        double deltaTime = 0.033; // ~30 FPS
        _breathingPhase += deltaTime * _breathingSpeed * 2 * Math.PI;
        if (_breathingPhase > Math.PI * 2)
        {
            _breathingPhase -= Math.PI * 2;
        }

        return 1.0 + Math.Sin(_breathingPhase) * _breathingAmplitude;
    }

    // ----- Brush/Pen creation (currently created per Draw* call, not cached) -----

    [Benchmark(Description = "CreateBrush + Freeze (per Draw call)")]
    public Brush CreateAndFreezeBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(139, 90, 43));
        brush.Freeze();
        return brush;
    }

    [Benchmark(Description = "CreatePen + Freeze (per Draw call)")]
    public Pen CreateAndFreezePen()
    {
        var brush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
        brush.Freeze();
        var pen = new Pen(brush, 0.8125);
        pen.Freeze();
        return pen;
    }
}

/// <summary>
/// Benchmarks for LRU cache eviction strategies in ProceduralSpriteRenderer.
/// Compares the LINQ-based EvictOldestEntries vs the threshold-based EvictOldestEntriesFast.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class CacheEvictionBenchmarks
{
    private Dictionary<string, BitmapSource> _frameCache;
    private Dictionary<string, long> _accessOrder;
    private long _accessCounter;
    private const int _maxCacheSize = 500;

    [Params(100, 300, 500)]
    public int CacheSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _frameCache = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        _accessOrder = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        _accessCounter = 0;

        // Fill cache to desired size
        for (int i = 0; i < CacheSize; i++)
        {
            var key = $"Pet_{i % 12}_{i % 6}_{i % 7}_{i % 4}_{26}";
            _frameCache[key] = null; // Null is fine; we're benchmarking the eviction lookup, not bitmap memory
            _accessOrder[key] = ++_accessCounter;
        }
    }

    /// <summary>
    /// LINQ-based eviction: OrderBy + Take + ToList. Current fallback path.
    /// </summary>
    [Benchmark(Baseline = true, Description = "EvictOldest: LINQ OrderBy")]
    public void EvictWithLinq()
    {
        int count = _maxCacheSize / 4;
        var keysToRemove = _accessOrder
            .OrderBy(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _frameCache.Remove(key);
            _accessOrder.Remove(key);
        }
    }

    /// <summary>
    /// Threshold-based eviction: single-pass with counter threshold. Current fast path.
    /// </summary>
    [Benchmark(Description = "EvictOldest: Threshold scan")]
    public void EvictWithThreshold()
    {
        int targetCount = _maxCacheSize / 4;
        var threshold = _accessCounter - (_maxCacheSize * 2);
        int removed = 0;

        var keysToRemove = new List<string>(targetCount);
        foreach (var kvp in _accessOrder)
        {
            if (kvp.Value < threshold)
            {
                keysToRemove.Add(kvp.Key);
                if (++removed >= targetCount)
                    break;
            }
        }

        foreach (var key in keysToRemove)
        {
            _frameCache.Remove(key);
            _accessOrder.Remove(key);
        }
    }
}
