# Speed Lines Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `SpeedLinesLayer` overlay effect that renders radial burst lines from a pixel-space origin, supporting multiple simultaneous instances with configurable fade modes and expand animation.

**Architecture:** `SpeedLinesInstance` holds per-burst state and computes current alpha/inner-radius each frame. `SpeedLinesLayer` maintains a list of instances (identical pattern to `HitFlashLayer`), converts pixel coords to UV in `Apply()`, and issues one additive `SpriteBatch` draw call per instance. The HLSL shader generates line patterns via an angular hash — no textures needed.

**Tech Stack:** MonoGame/DesktopGL, HLSL (ps_3_0 / ps_4_0_level_9_1), `GameTimer.CountdownTimer` (already used by `HitFlashInstance`)

---

## File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `ScreenFXBuddy/Effects/SpeedLinesInstance.cs` | `SpeedLinesMode` enum + per-burst state |
| Create | `ScreenFXBuddy/Content/Overlay_SpeedLines.fx` | Radial line shader |
| Create | `ScreenFXBuddy/Effects/SpeedLinesLayer.cs` | `IOverlayLayer` managing instance list |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Property + registration + `TriggerSpeedLines()` |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register the new `.fx` asset |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Test keybindings |

---

### Task 1: SpeedLinesInstance

**Files:**
- Create: `ScreenFXBuddy/Effects/SpeedLinesInstance.cs`

- [ ] **Step 1: Create `SpeedLinesInstance.cs`**

```csharp
using System;
using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

public enum SpeedLinesMode
{
    Static,  // lines appear at full intensity immediately
    Expand   // lines expand outward from center over the lifetime
}

public class SpeedLinesInstance
{
    public Vector2 PixelPosition { get; }
    public Color Color { get; }
    public SpeedLinesMode LinesMode { get; }
    public FadeMode FadeMode { get; }
    public FadeCurve FadeCurve { get; }
    public int LineCount { get; }
    public float MaxRadius { get; }

    public CountdownTimer Timer { get; } = new CountdownTimer();
    public bool IsAlive => Timer.HasTimeRemaining;

    public SpeedLinesInstance(Vector2 pixelPosition, Color color,
        SpeedLinesMode linesMode, FadeMode fadeMode, FadeCurve fadeCurve,
        int lineCount, float maxRadius, float duration)
    {
        PixelPosition = pixelPosition;
        Color         = color;
        LinesMode     = linesMode;
        FadeMode      = fadeMode;
        FadeCurve     = fadeCurve;
        LineCount     = lineCount;
        MaxRadius     = maxRadius;
        Timer.Start(duration);
    }

    public void Update(GameTime gameTime) => Timer.Update(gameTime);

    // 0–1 intensity for the current frame, derived from FadeMode + FadeCurve + timer.
    public float CurrentAlpha => FadeMode switch
    {
        FadeMode.FadeIn  => ApplyCurve(1f - Timer.Lerp),
        FadeMode.FadeOut => ApplyCurve(Timer.Lerp),
        _                => 1f
    };

    // UV-space inner radius. Static = 0 always. Expand = grows from 0 → MaxRadius.
    // Timer.Lerp is 1.0 at start and 0.0 at expiry.
    public float CurrentInnerRadius => LinesMode == SpeedLinesMode.Expand
        ? MaxRadius * (1f - Timer.Lerp)
        : 0f;

    private float ApplyCurve(float t) => FadeCurve switch
    {
        FadeCurve.Logarithmic => MathF.Log(1f + t * (MathF.E - 1f)),
        FadeCurve.Exponential => t * t,
        _                     => t
    };
}
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/SpeedLinesInstance.cs
git commit -m "feat: add SpeedLinesInstance with SpeedLinesMode enum"
```

---

### Task 2: Shader

**Files:**
- Create: `ScreenFXBuddy/Content/Overlay_SpeedLines.fx`

- [ ] **Step 1: Create `Overlay_SpeedLines.fx`**

```hlsl
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Center;      // UV-space origin of the burst
float4 LineColor;   // RGBA tint including current alpha
float  LineCount;   // number of angular segments
float  InnerRadius; // UV-radius below which pixels are transparent (expand cutoff)
float  MaxRadius;   // UV-radius above which pixels are transparent

static const float PI = 3.14159265;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv   = input.TexCoord;
    float2 dir  = uv - Center;
    float  dist = length(dir);

    // Expand cutoff: guard against atan2(0,0) singularity when InnerRadius == 0
    if (dist < max(InnerRadius, 0.0001) || dist > MaxRadius)
        return float4(0, 0, 0, 0);

    // Map angle [-π, π] → segment index [0, LineCount)
    float angle   = atan2(dir.y, dir.x);
    float segment = floor((angle / (2.0 * PI) + 0.5) * LineCount);

    // Hash the segment — roughly half of segments become lines
    float hash = frac(sin(segment * 127.1 + 311.7) * 43758.5453);
    if (hash < 0.5)
        return float4(0, 0, 0, 0);

    // Fade to zero as pixels approach the outer radius
    float edgeFade = 1.0 - smoothstep(MaxRadius * 0.7, MaxRadius, dist);

    return float4(LineColor.rgb, LineColor.a * edgeFade);
}

technique SpeedLines
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ScreenFXBuddy/Content/Overlay_SpeedLines.fx
git commit -m "feat: add Overlay_SpeedLines.fx radial line shader"
```

---

### Task 3: SpeedLinesLayer

**Files:**
- Create: `ScreenFXBuddy/Effects/SpeedLinesLayer.cs`

- [ ] **Step 1: Create `SpeedLinesLayer.cs`**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class SpeedLinesLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private readonly List<SpeedLinesInstance> _instances = new();

    private EffectParameter _pCenter;
    private EffectParameter _pLineColor;
    private EffectParameter _pLineCount;
    private EffectParameter _pInnerRadius;
    private EffectParameter _pMaxRadius;

    public bool IsActive => _instances.Count > 0;

    public SpeedLinesLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Overlay_SpeedLines");
        _pCenter      = _effect.Parameters["Center"];
        _pLineColor   = _effect.Parameters["LineColor"];
        _pLineCount   = _effect.Parameters["LineCount"];
        _pInnerRadius = _effect.Parameters["InnerRadius"];
        _pMaxRadius   = _effect.Parameters["MaxRadius"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Vector2 pixelPosition, Color color,
        SpeedLinesMode linesMode = SpeedLinesMode.Expand,
        FadeMode fadeMode        = FadeMode.FadeOut,
        FadeCurve fadeCurve      = FadeCurve.Logarithmic,
        int lineCount            = 24,
        float maxRadius          = 1.0f,
        float duration           = 1f)
    {
        _instances.Add(new SpeedLinesInstance(
            pixelPosition, color, linesMode, fadeMode, fadeCurve, lineCount, maxRadius, duration));
    }

    public void Update(GameTime gameTime)
    {
        int i = 0;
        while (i < _instances.Count)
        {
            _instances[i].Update(gameTime);
            if (!_instances[i].IsAlive)
                _instances.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        var vp = _graphicsDevice.Viewport;
        foreach (var inst in _instances)
        {
            var uvCenter = new Vector2(
                inst.PixelPosition.X / vp.Width,
                inst.PixelPosition.Y / vp.Height);

            _pCenter.SetValue(uvCenter);
            _pLineColor.SetValue((inst.Color * inst.CurrentAlpha).ToVector4());
            _pLineCount.SetValue((float)inst.LineCount);
            _pInnerRadius.SetValue(inst.CurrentInnerRadius);
            _pMaxRadius.SetValue(inst.MaxRadius);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                _effect);
            spriteBatch.Draw(_whitePixel, vp.Bounds, Color.White);
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/SpeedLinesLayer.cs
git commit -m "feat: add SpeedLinesLayer IOverlayLayer"
```

---

### Task 4: Wire up ScreenFXComponent

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`

- [ ] **Step 1: Add the `SpeedLines` property** after the existing `AnimeSuperLayer AnimeSuper` property declaration (around line 27):

```csharp
public SpeedLinesLayer SpeedLines { get; private set; }
```

- [ ] **Step 2: Initialize and register in `LoadContent`**

After `AnimeSuper = new AnimeSuperLayer(GraphicsDevice);` (around line 46), add:

```csharp
SpeedLines = new SpeedLinesLayer(GraphicsDevice);
```

Change the `OverlayLayers.AddRange` call to include `SpeedLines`:

```csharp
OverlayLayers.AddRange(new IOverlayLayer[]
    { HitFlash, AnimeSuper, SpeedLines });
```

- [ ] **Step 3: Add the convenience trigger method** after `TriggerAnimeSuper` (around line 127):

```csharp
public void TriggerSpeedLines(
    Vector2 position,
    Color color,
    SpeedLinesMode linesMode = SpeedLinesMode.Expand,
    FadeMode fadeMode        = FadeMode.FadeOut,
    FadeCurve fadeCurve      = FadeCurve.Logarithmic,
    int lineCount            = 24,
    float maxRadius          = 1.0f,
    float duration           = 1f)
    => SpeedLines.Trigger(position, color, linesMode, fadeMode, fadeCurve, lineCount, maxRadius, duration);
```

- [ ] **Step 4: Build to confirm it compiles**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs
git commit -m "feat: register SpeedLinesLayer in ScreenFXComponent"
```

---

### Task 5: Register shader in Content Pipeline

**Files:**
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

- [ ] **Step 1: Add the shader entry** after the `Distorter_ScreenShake.fx` block (after line 32):

```
#begin ../../ScreenFXBuddy/Content/Overlay_SpeedLines.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Overlay_SpeedLines.fx;Overlay_SpeedLines.fx
```

- [ ] **Step 2: Build the example project** (this compiles the `.mgcb` content and the shader)

```bash
dotnet build ScreenFXBuddy.Example/ScreenFXBuddy.Example.csproj
```

Expected: `Build succeeded` with 0 errors and no MGCB errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: register Overlay_SpeedLines.fx in content pipeline"
```

---

### Task 6: Add example keybindings and visual test

**Files:**
- Modify: `ScreenFXBuddy.Example/Game1.cs`

- [ ] **Step 1: Add test bindings in `Update()`** after the `TriggerAnimeSuper` block (after line 70):

```csharp
var screenCenter = new Vector2(ScreenWidth / 2f, ScreenHeight / 2f);

// Basic expand + fade-out (default)
if (keys.IsKeyDown(Keys.D8) && !_prevKeys.IsKeyDown(Keys.D8))
    _screenFX.TriggerSpeedLines(screenCenter, Color.White);

// Static lines, fade-in, linear curve
if (keys.IsKeyDown(Keys.D9) && !_prevKeys.IsKeyDown(Keys.D9))
    _screenFX.TriggerSpeedLines(screenCenter, Color.Yellow,
        SpeedLinesMode.Static, FadeMode.FadeIn, FadeCurve.Linear, 32, 1.0f, 1.5f);

// Off-center burst, tight radius, many lines
if (keys.IsKeyDown(Keys.D0) && !_prevKeys.IsKeyDown(Keys.D0))
    _screenFX.TriggerSpeedLines(new Vector2(300, 200), Color.Cyan,
        SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Exponential, 48, 0.6f, 0.8f);
```

- [ ] **Step 2: Add the `ScreenFXBuddy.Effects` using if not already present** at the top of `Game1.cs` (it already imports `ScreenFXBuddy.Effects` for `FadeMode` etc., so this should already be there).

- [ ] **Step 3: Build the full solution**

```bash
dotnet build
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Run the example and visually verify**

```bash
dotnet run --project ScreenFXBuddy.Example/ScreenFXBuddy.Example.csproj
```

- Press **8** — white lines should burst from screen center, expanding outward and fading over ~1 second.
- Press **9** — yellow static lines should appear at full screen, fading in over 1.5 seconds.
- Press **0** — cyan lines from top-left quadrant, tight radius, faster fade.
- Press **8** rapidly several times — multiple simultaneous bursts should stack additively.
- Verify lines look organic/irregular (not perfectly regular like a wagon wheel).

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: add SpeedLines example keybindings (8, 9, 0)"
```
