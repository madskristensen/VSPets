using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using VSPets.Models;

namespace VSPets.Benchmarks;

/// <summary>
/// Benchmarks for cache key generation strategies.
/// The renderer generates a string key via interpolation on every RenderFrame call.
/// This benchmark compares that approach against value-type composite key alternatives.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class CacheKeyBenchmarks
{
    private PetType _petType;
    private PetColor _color;
    private PetState _state;
    private int _frame;
    private int _size;

    [GlobalSetup]
    public void Setup()
    {
        _petType = PetType.Cat;
        _color = PetColor.Brown;
        _state = PetState.Walking;
        _frame = 2;
        _size = (int)PetSize.Small;
    }

    /// <summary>
    /// Current approach: string interpolation to build cache key.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Key: String interpolation")]
    public string StringInterpolation()
    {
        return $"{_petType}_{_color}_{_state}_{_frame}_{_size}";
    }

    /// <summary>
    /// Alternative: String.Concat with pre-converted parts.
    /// </summary>
    [Benchmark(Description = "Key: String.Concat")]
    public string StringConcat()
    {
        return string.Concat(
            _petType.ToString(), "_",
            _color.ToString(), "_",
            _state.ToString(), "_",
            _frame.ToString(), "_",
            _size.ToString());
    }

    /// <summary>
    /// Alternative: Composite value-type key using a struct with GetHashCode.
    /// Zero allocations for lookup.
    /// </summary>
    [Benchmark(Description = "Key: ValueTuple hashcode")]
    public int ValueTupleHashCode()
    {
        return (_petType, _color, _state, _frame, _size).GetHashCode();
    }

    /// <summary>
    /// Alternative: Manual packed int key.
    /// PetType (4 bits) + PetColor (5 bits) + PetState (4 bits) + Frame (3 bits) + Size (16 bits) = 32 bits.
    /// </summary>
    [Benchmark(Description = "Key: Packed int")]
    public int PackedIntKey()
    {
        return ((int)_petType & 0xF)
             | (((int)_color & 0x1F) << 4)
             | (((int)_state & 0xF) << 9)
             | ((_frame & 0x7) << 13)
             | ((_size & 0xFFFF) << 16);
    }

    /// <summary>
    /// String interpolation including dictionary lookup (simulating full cache check).
    /// </summary>
    [Benchmark(Description = "Key + Dictionary.TryGetValue (string)")]
    public bool StringKeyWithLookup()
    {
        var key = $"{_petType}_{_color}_{_state}_{_frame}_{_size}";
        return _stringCache.TryGetValue(key, out _);
    }

    /// <summary>
    /// Packed int key including dictionary lookup.
    /// </summary>
    [Benchmark(Description = "Key + Dictionary.TryGetValue (packed int)")]
    public bool PackedKeyWithLookup()
    {
        int key = ((int)_petType & 0xF)
                | (((int)_color & 0x1F) << 4)
                | (((int)_state & 0xF) << 9)
                | ((_frame & 0x7) << 13)
                | ((_size & 0xFFFF) << 16);
        return _intCache.TryGetValue(key, out _);
    }

    private readonly Dictionary<string, object> _stringCache = new(StringComparer.OrdinalIgnoreCase)
    {
        [$"{PetType.Cat}_{PetColor.Brown}_{PetState.Walking}_{2}_{(int)PetSize.Small}"] = new object()
    };

    private readonly Dictionary<int, object> _intCache = new()
    {
        [((int)PetType.Cat & 0xF)
       | (((int)PetColor.Brown & 0x1F) << 4)
       | (((int)PetState.Walking & 0xF) << 9)
       | ((2 & 0x7) << 13)
       | (((int)PetSize.Small & 0xFFFF) << 16)] = new object()
    };
}
