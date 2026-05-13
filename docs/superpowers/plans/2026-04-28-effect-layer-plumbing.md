# Effect Layer Plumbing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Add two layer interfaces, seven built-in effect layer stubs (all using Debug_Red.fx as placeholder), and a rewritten ping-pong pipeline in ScreenFXComponent — so every effect can be triggered and the pipeline can be visually validated before real shaders are written.

**Architecture:** `IDistortionLayer` instances are chained through ping-pong render targets; `IOverlayLayer` instances draw additively on top of the back buffer. `ScreenFXComponent` owns all three render targets, iterates both lists in `EndCapture`, and exposes public shortcut properties and `Trigger*()` convenience methods for each built-in layer.

**Tech Stack:** C# 12, .NET 8, MonoGame 3.8 DesktopGL

> **Testing note:** MonoGame requires a GPU — no headless unit tests. Each task ends with `dotnet build` to catch compile errors. The final task adds keyboard triggers to the example app and verifies visually.

---

## File Map

**Create:**
- `ScreenFXBuddy/Effects/IDistortionLayer.cs`
- `ScreenFXBuddy/Effects/IOverlayLayer.cs`
- `ScreenFXBuddy/Effects/ForceRippleLayer.cs`
- `ScreenFXBuddy/Effects/GravityWaveLayer.cs`
- `ScreenFXBuddy/Effects/ScreenShakeLayer.cs`
- `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs`
- `ScreenFXBuddy/Effects/HeatHazeLayer.cs`
- `ScreenFXBuddy/Effects/HitFlashLayer.cs`
- `ScreenFXBuddy/Effects/AnimeSuperLayer.cs`

**Modify:**
- `ScreenFXBuddy/ScreenFXComponent.cs` — add ping-pong targets, layer lists, pipeline loop
- `ScreenFXBuddy.Example/Game1.cs` — add keyboard triggers for visual testing

---

## Task 1: Create IDistortionLayer and IOverlayLayer interfaces

**Files:**
- Create: `ScreenFXBuddy/Effects/IDistortionLayer.cs`
- Create: `ScreenFXBuddy/Effects/IOverlayLayer.cs`

- [x] **Step 1: Create IDistortionLayer**

`ScreenFXBuddy/Effects/IDistortionLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public interface IDistortionLayer
{
    bool IsActive { get; }
    void LoadContent(ContentManager content);
    void Update(GameTime gameTime);
    void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination);
}
```

- [x] **Step 2: Create IOverlayLayer**

`ScreenFXBuddy/Effects/IOverlayLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public interface IOverlayLayer
{
    bool IsActive { get; }
    void LoadContent(ContentManager content);
    void Update(GameTime gameTime);
    void Apply(SpriteBatch spriteBatch);
}
```

- [x] **Step 3: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded, 0 errors.

- [x] **Step 4: Commit**

```bash
git add ScreenFXBuddy/Effects/IDistortionLayer.cs ScreenFXBuddy/Effects/IOverlayLayer.cs
git commit -m "feat: add IDistortionLayer and IOverlayLayer interfaces"
```

---

## Task 2: Rewrite ScreenFXComponent for the ping-pong pipeline

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`

- [x] **Step 1: Replace ScreenFXComponent.cs entirely**

`ScreenFXBuddy/ScreenFXComponent.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy;

public class ScreenFXComponent : DrawableGameComponent
{
    private SpriteBatch _spriteBatch;
    private RenderTarget2D _sceneTarget;
    private RenderTarget2D _pingTarget;
    private RenderTarget2D _pongTarget;

    public List<IDistortionLayer> DistortionLayers { get; } = new();
    public List<IOverlayLayer> OverlayLayers { get; } = new();

    public ForceRippleLayer ForceRipple { get; private set; }
    public GravityWaveLayer GravityWave { get; private set; }
    public ScreenShakeLayer ScreenShake { get; private set; }
    public ChromaticAberrationLayer ChromaticAberration { get; private set; }
    public HeatHazeLayer HeatHaze { get; private set; }
    public HitFlashLayer HitFlash { get; private set; }
    public AnimeSuperLayer AnimeSuper { get; private set; }

    public ScreenFXComponent(Game game) : base(game) { }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var pp = GraphicsDevice.PresentationParameters;
        _sceneTarget = CreateTarget(pp);
        _pingTarget  = CreateTarget(pp);
        _pongTarget  = CreateTarget(pp);

        ForceRipple          = new ForceRippleLayer(GraphicsDevice);
        GravityWave          = new GravityWaveLayer(GraphicsDevice);
        ScreenShake          = new ScreenShakeLayer(GraphicsDevice);
        ChromaticAberration  = new ChromaticAberrationLayer(GraphicsDevice);
        HeatHaze             = new HeatHazeLayer(GraphicsDevice);
        HitFlash             = new HitFlashLayer(GraphicsDevice);
        AnimeSuper           = new AnimeSuperLayer(GraphicsDevice);

        DistortionLayers.AddRange(new IDistortionLayer[]
            { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze });
        OverlayLayers.AddRange(new IOverlayLayer[]
            { HitFlash, AnimeSuper });

        foreach (var layer in DistortionLayers) layer.LoadContent(Game.Content);
        foreach (var layer in OverlayLayers)    layer.LoadContent(Game.Content);
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var layer in DistortionLayers) layer.Update(gameTime);
        foreach (var layer in OverlayLayers)    layer.Update(gameTime);
        base.Update(gameTime);
    }

    public void BeginCapture()
    {
        GraphicsDevice.SetRenderTarget(_sceneTarget);
    }

    public void EndCapture()
    {
        // Ping-pong through distortion layers.
        // source starts at _sceneTarget; we alternate writing to _pingTarget / _pongTarget.
        var source  = _sceneTarget;
        bool usePing = true;

        foreach (var layer in DistortionLayers)
        {
            if (!layer.IsActive) continue;
            var dest = usePing ? _pingTarget : _pongTarget;
            layer.Apply(_spriteBatch, source, dest);
            source  = dest;
            usePing = !usePing;
        }

        // Blit final result (or unmodified scene) to back buffer.
        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(source, GraphicsDevice.Viewport.Bounds, Color.White);
        _spriteBatch.End();

        // Additive overlay layers on top of the back buffer.
        foreach (var layer in OverlayLayers)
        {
            if (!layer.IsActive) continue;
            layer.Apply(_spriteBatch);
        }
    }

    public void TriggerForceRipple(Vector2 position, float strength = 1f)
        => ForceRipple.Trigger(position, strength);

    public void TriggerGravityWave(Vector2 position, float strength = 1f)
        => GravityWave.Trigger(position, strength);

    public void TriggerScreenShake(float trauma)
        => ScreenShake.Trigger(trauma);

    public void TriggerChromaticAberration(float intensity, float duration)
        => ChromaticAberration.Trigger(intensity, duration);

    public void TriggerHeatHaze(float intensity, float duration)
        => HeatHaze.Trigger(intensity, duration);

    public void TriggerHitFlash(Color color, float duration)
        => HitFlash.Trigger(color, duration);

    public void TriggerAnimeSuper(Color color, float duration)
        => AnimeSuper.Trigger(color, duration);

    private RenderTarget2D CreateTarget(PresentationParameters pp) =>
        new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight,
            false, pp.BackBufferFormat, pp.DepthStencilFormat);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sceneTarget?.Dispose();
            _pingTarget?.Dispose();
            _pongTarget?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

- [x] **Step 2: Build (will fail — layer classes don't exist yet, that's expected)**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: errors referencing `ForceRippleLayer`, `GravityWaveLayer`, etc. — confirms the component is wired correctly and layer classes are the only thing missing.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs
git commit -m "feat: rewrite ScreenFXComponent with ping-pong pipeline and layer lists"
```

---

## Task 3: ForceRippleLayer (Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/ForceRippleLayer.cs`

- [x] **Step 1: Create ForceRippleLayer**

`ScreenFXBuddy/Effects/ForceRippleLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ForceRippleLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private readonly List<RippleInstance> _instances = new();

    private const int MaxInstances = 16;

    public bool IsActive => _instances.Count > 0;

    public ForceRippleLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(Vector2 position, float strength = 1f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new RippleInstance(position, strength, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Age = inst.Age + dt };
            if (inst.Age >= 1f)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct RippleInstance(Vector2 Position, float Strength, float Age);
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded (one fewer unresolved type).

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/ForceRippleLayer.cs
git commit -m "feat: add ForceRippleLayer stub with Debug_Red placeholder"
```

---

## Task 4: GravityWaveLayer (Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/GravityWaveLayer.cs`

- [x] **Step 1: Create GravityWaveLayer**

`ScreenFXBuddy/Effects/GravityWaveLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class GravityWaveLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private readonly List<WaveInstance> _instances = new();

    private const int MaxInstances = 8;

    public bool IsActive => _instances.Count > 0;

    public GravityWaveLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(Vector2 position, float strength = 1f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new WaveInstance(position, strength, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Age = inst.Age + dt };
            if (inst.Age >= 1f)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct WaveInstance(Vector2 Position, float Strength, float Age);
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/GravityWaveLayer.cs
git commit -m "feat: add GravityWaveLayer stub with Debug_Red placeholder"
```

---

## Task 5: ScreenShakeLayer (Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/ScreenShakeLayer.cs`

- [x] **Step 1: Create ScreenShakeLayer**

`ScreenFXBuddy/Effects/ScreenShakeLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ScreenShakeLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private float _trauma;

    private const float DecayRate = 1.5f;

    public bool IsActive => _trauma > 0.001f;

    public ScreenShakeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(float trauma)
    {
        _trauma = Math.Min(_trauma + trauma, 1f);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _trauma = Math.Max(0f, _trauma - DecayRate * dt);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/ScreenShakeLayer.cs
git commit -m "feat: add ScreenShakeLayer stub with Debug_Red placeholder"
```

---

## Task 6: ChromaticAberrationLayer (Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs`

- [x] **Step 1: Create ChromaticAberrationLayer**

`ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ChromaticAberrationLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private float _intensity;
    private float _remaining;

    public bool IsActive => _remaining > 0f;

    public ChromaticAberrationLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(float intensity, float duration)
    {
        _intensity = intensity;
        _remaining = duration;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _remaining = Math.Max(0f, _remaining - dt);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs
git commit -m "feat: add ChromaticAberrationLayer stub with Debug_Red placeholder"
```

---

## Task 7: HeatHazeLayer (Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/HeatHazeLayer.cs`

- [x] **Step 1: Create HeatHazeLayer**

`ScreenFXBuddy/Effects/HeatHazeLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class HeatHazeLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private float _intensity;
    private float _remaining;

    public bool IsActive => _remaining > 0f;

    public HeatHazeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(float intensity, float duration)
    {
        _intensity = intensity;
        _remaining = duration;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _remaining = Math.Max(0f, _remaining - dt);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/HeatHazeLayer.cs
git commit -m "feat: add HeatHazeLayer stub with Debug_Red placeholder"
```

---

## Task 8: HitFlashLayer (additive overlay, Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/HitFlashLayer.cs`

- [x] **Step 1: Create HitFlashLayer**

`ScreenFXBuddy/Effects/HitFlashLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class HitFlashLayer : IOverlayLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private readonly List<FlashInstance> _instances = new();

    private const int MaxInstances = 4;

    public bool IsActive => _instances.Count > 0;

    public HitFlashLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Color color, float duration)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new FlashInstance(color, duration, duration));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Remaining = inst.Remaining - dt };
            if (inst.Remaining <= 0f)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        foreach (var flash in _instances)
        {
            float alpha = flash.Remaining / flash.Duration;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                _effect);
            spriteBatch.Draw(_whitePixel, _graphicsDevice.Viewport.Bounds,
                flash.Color * alpha);
            spriteBatch.End();
        }
    }

    private record struct FlashInstance(Color Color, float Duration, float Remaining);
}
```

- [x] **Step 2: Build**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```
Expected: Build succeeded.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/HitFlashLayer.cs
git commit -m "feat: add HitFlashLayer stub with Debug_Red placeholder"
```

---

## Task 9: AnimeSuperLayer (additive overlay, Debug_Red placeholder)

**Files:**
- Create: `ScreenFXBuddy/Effects/AnimeSuperLayer.cs`

- [x] **Step 1: Create AnimeSuperLayer**

`ScreenFXBuddy/Effects/AnimeSuperLayer.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class AnimeSuperLayer : IOverlayLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private Color _color;
    private float _remaining;
    private float _duration;

    public bool IsActive => _remaining > 0f;

    public AnimeSuperLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Color color, float duration)
    {
        _color     = color;
        _duration  = duration;
        _remaining = duration;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _remaining = Math.Max(0f, _remaining - dt);
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float alpha = _remaining / _duration;
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(_whitePixel, _graphicsDevice.Viewport.Bounds, _color * alpha);
        spriteBatch.End();
    }
}
```

- [x] **Step 2: Build (full solution — all layer classes now exist)**

```bash
dotnet build ScreenFXBuddy.sln
```
Expected: Build succeeded, 0 errors.

- [x] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/AnimeSuperLayer.cs
git commit -m "feat: add AnimeSuperLayer stub with Debug_Red placeholder"
```

---

## Task 10: Wire keyboard triggers in Example and run visual test

**Files:**
- Modify: `ScreenFXBuddy.Example/Game1.cs`

- [x] **Step 1: Update Game1.cs with keyboard triggers**

`ScreenFXBuddy.Example/Game1.cs`:
```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ScreenFXComponent _screenFX;
    private KeyboardState _prevKeys;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _screenFX = new ScreenFXComponent(this);
        Components.Add(_screenFX);
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (keys.IsKeyDown(Keys.Escape)) Exit();

        var center = new Vector2(0.5f, 0.5f);

        if (keys.IsKeyDown(Keys.D1) && !_prevKeys.IsKeyDown(Keys.D1))
            _screenFX.TriggerForceRipple(center);

        if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
            _screenFX.TriggerGravityWave(center);

        if (keys.IsKeyDown(Keys.D3) && !_prevKeys.IsKeyDown(Keys.D3))
            _screenFX.TriggerScreenShake(0.8f);

        if (keys.IsKeyDown(Keys.D4) && !_prevKeys.IsKeyDown(Keys.D4))
            _screenFX.TriggerChromaticAberration(1f, 2f);

        if (keys.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5))
            _screenFX.TriggerHeatHaze(1f, 2f);

        if (keys.IsKeyDown(Keys.D6) && !_prevKeys.IsKeyDown(Keys.D6))
            _screenFX.TriggerHitFlash(Color.White, 0.5f);

        if (keys.IsKeyDown(Keys.D7) && !_prevKeys.IsKeyDown(Keys.D7))
            _screenFX.TriggerAnimeSuper(Color.White, 1f);

        _prevKeys = keys;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _screenFX.BeginCapture();

        GraphicsDevice.Clear(Color.CornflowerBlue);
        // TODO: draw game scene here

        _screenFX.EndCapture();

        base.Draw(gameTime);
    }
}
```

- [x] **Step 2: Build the solution**

```bash
dotnet build ScreenFXBuddy.sln
```
Expected: Build succeeded, 0 errors.

- [x] **Step 3: Run and test each key**

```bash
dotnet run --project ScreenFXBuddy.Example/ScreenFXBuddy.Example.csproj
```

Expected results:
- No key pressed: cornflower blue (pipeline inactive, scene blitted directly)
- `1` key: screen turns solid red (ForceRipple Debug_Red active, fades after ~1s)
- `2` key: screen turns solid red (GravityWave)
- `3` key: screen turns solid red, fades within ~0.5s (ScreenShake trauma decay)
- `4` key: screen turns solid red, fades over 2s (ChromaticAberration)
- `5` key: screen turns solid red, fades over 2s (HeatHaze)
- `6` key: red tint additively blended on top, fades over 0.5s (HitFlash overlay)
- `7` key: red tint additively blended on top, fades over 1s (AnimeSuper overlay)

- [x] **Step 4: Commit**

```bash
git add ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: add keyboard triggers to example app for pipeline testing"
```
