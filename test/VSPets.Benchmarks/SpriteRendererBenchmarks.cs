using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using VSPets.Animation;
using VSPets.Models;

namespace VSPets.Benchmarks;

/// <summary>
/// Benchmarks for ProceduralSpriteRenderer — the most expensive per-frame operation.
/// Measures sprite creation cost per pet type, cache hit/miss behavior, and eviction overhead.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class SpriteRendererBenchmarks
{
    private ProceduralSpriteRenderer _renderer;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = ProceduralSpriteRenderer.Instance;
        _renderer.ClearCache();
    }

    [IterationSetup(Target = nameof(RenderFrame_CacheHit))]
    public void WarmCacheForHitBenchmark()
    {
        // Pre-populate the cache entry that the CacheHit benchmark will read
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Idle, 0, (int)PetSize.Small);
    }

    // ----- Per-pet-type sprite creation (cache miss) -----

    [Benchmark(Description = "CreateSprite: Cat")]
    [IterationSetup(Target = nameof(CreateSprite_Cat))]
    public void CreateSprite_Cat_Setup() => _renderer.ClearCache();

    [Benchmark(Description = "CreateSprite: Cat (Small)")]
    public void CreateSprite_Cat()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Dog (Small)")]
    public void CreateSprite_Dog()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Dog, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Fox (Small)")]
    public void CreateSprite_Fox()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Fox, PetColor.Red, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Bear (Small)")]
    public void CreateSprite_Bear()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Bear, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Axolotl (Small)")]
    public void CreateSprite_Axolotl()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Axolotl, PetColor.Pink, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Clippy (Small)")]
    public void CreateSprite_Clippy()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Clippy, PetColor.Original, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: RubberDuck (Small)")]
    public void CreateSprite_RubberDuck()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.RubberDuck, PetColor.Yellow, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Turtle (Small)")]
    public void CreateSprite_Turtle()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Turtle, PetColor.Green, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Bunny (Small)")]
    public void CreateSprite_Bunny()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Bunny, PetColor.White, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Raccoon (Small)")]
    public void CreateSprite_Raccoon()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Raccoon, PetColor.Gray, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: TRex (Small)")]
    public void CreateSprite_TRex()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.TRex, PetColor.Green, PetState.Walking, 0, (int)PetSize.Small);
    }

    [Benchmark(Description = "CreateSprite: Wolf (Small)")]
    public void CreateSprite_Wolf()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Wolf, PetColor.Gray, PetState.Walking, 0, (int)PetSize.Small);
    }

    // ----- Size comparison -----

    [Benchmark(Description = "CreateSprite: Cat (Tiny 20px)")]
    public void CreateSprite_Cat_Tiny()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Tiny);
    }

    [Benchmark(Description = "CreateSprite: Cat (Medium 36px)")]
    public void CreateSprite_Cat_Medium()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Medium);
    }

    [Benchmark(Description = "CreateSprite: Cat (Large 48px)")]
    public void CreateSprite_Cat_Large()
    {
        _renderer.ClearCache();
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Walking, 0, (int)PetSize.Large);
    }

    // ----- Cache scenarios -----

    [Benchmark(Baseline = true, Description = "RenderFrame: Cache hit")]
    public void RenderFrame_CacheHit()
    {
        // Entry was pre-populated in IterationSetup
        _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Idle, 0, (int)PetSize.Small);
    }

    // ----- Full walk cycle (4 frames) -----

    [Benchmark(Description = "RenderFrame: Full 4-frame walk cycle (Cat)")]
    public void RenderFrame_FullWalkCycle()
    {
        _renderer.ClearCache();
        for (int frame = 0; frame < 4; frame++)
        {
            _renderer.RenderFrame(PetType.Cat, PetColor.Brown, PetState.Walking, frame, (int)PetSize.Small);
        }
    }
}
