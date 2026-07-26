# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Mark I Perceptron Simulator** that recreates Frank Rosenblatt's 1958 perceptron machine with a vintage 1950s industrial aesthetic. The UI features custom-drawn controls designed to look like physical hardware: toggle switches with LEDs, rotary knobs, analog meters, and backlit mechanical buttons.

The repository contains **two implementations** of the simulator:

1. **Windows Forms desktop app** (repo root, C# / .NET) — the original and most complete version. Everything in this document *below the iOS section* refers to this app unless stated otherwise.
2. **iOS / iPadOS / Mac Catalyst app** (`PerceptronDemo/`, Swift / UIKit) — a native port of the panel UI. See the [iOS App (PerceptronDemo)](#ios-app-perceptrondemo) section for its architecture, build commands, and how it differs from the desktop app.

Both share the same visual language and the same core perceptron math; the C# app is the source of truth that the Swift app is ported from.

**Key Insight**: The desktop program contains its own DNA — the complete prompts used to generate the codebase are embedded as resources. Feed `resources/complete_prompt.txt` into Claude Code to recreate the entire application.

## Build Commands

### Desktop (Windows Forms, C#)

```bash
# Build debug
dotnet build

# Run the application
dotnet run

# Build single-file release (outputs to c:\perceptron_release\)
dotnet publish -p:PublishProfile=SingleFileRelease
# Or use: publish.bat
```

### iOS / iPadOS / Mac Catalyst (Swift, UIKit)

The iOS app lives in `PerceptronDemo/` and builds with Xcode. Per the repo
guidance, build into a local DerivedData folder so package checkouts stay local.

**The app target is iPad-only** (`TARGETED_DEVICE_FAMILY = 2`), so use an **iPad**
simulator (or Mac Catalyst) — iPhone destinations are ineligible.

```bash
# Build for the iPad Simulator
xcodebuild -project PerceptronDemo/PerceptronDemo.xcodeproj \
  -scheme PerceptronDemo \
  -destination 'platform=iOS Simulator,name=iPad Pro 13-inch (M5),OS=26.2' \
  -derivedDataPath PerceptronDemo/DerivedData build

# Run tests
xcodebuild -project PerceptronDemo/PerceptronDemo.xcodeproj \
  -scheme PerceptronDemo \
  -destination 'platform=iOS Simulator,name=iPad Pro 13-inch (M5),OS=26.2' \
  -derivedDataPath PerceptronDemo/DerivedData test
```

Or open `PerceptronDemo/PerceptronDemo.xcodeproj` in Xcode and run. The app also
runs as a **Mac Catalyst** target. See the [iOS App](#ios-app-perceptrondemo)
section for details.

## iOS App (PerceptronDemo)

A native **UIKit** port of the perceptron panel, targeting **iPadOS 26.2+**
and **Mac Catalyst** (`SUPPORTS_MACCATALYST = YES`, app target
`TARGETED_DEVICE_FAMILY = 2` — iPad only, so iPhone simulators are ineligible;
Swift 5.0). It lives entirely under `PerceptronDemo/` and is a separate Xcode
project — it does **not** share code with the C# app, but mirrors its look and its
core math. The UI is built **programmatically** (no storyboard for the main
screen); `Main` storyboard usage is replaced by `SceneDelegate` wiring.

### Structure

```
PerceptronDemo/
  PerceptronDemo/
    AppDelegate.swift              # Standard UIKit app delegate + scene config
    SceneDelegate.swift            # Builds the window, presents the intro on launch
    ViewController.swift           # Empty template VC (not used by the running app)
    PerceptronPanelViewController.swift  # The main instrument panel + gear menu
    SignalFlowViewController.swift # 2nd screen: multi-layer "patch panel" (1986)
    IntroViewController.swift      # Launch "briefing card" / About screen
    PerceptronSnapshot.swift       # Codable panel state (.pcn) + Preset catalog
    SlideTransition.swift          # Horizontal "pan across the desk" modal transition
    Engine/
      PerceptronEngine.swift       # Single-layer (classic 1958 rule)
      MLPEngine.swift              # Multi-layer 16→6→1 (1986 backprop, ReLU)
    Controls/                      # Owner-drawn UIViews mirroring the desktop controls
      SwitchControl.swift          # Toggle switch + LED (isOn, value +1/-1)
      SwitchGrid.swift             # Shared switch-grid metrics + shift/toggle helpers
      KnobControl.swift            # Rotary dial (value, minValue/maxValue -30…30, step 0.05)
      ArrowButton.swift            # Triangular d-pad button (direction, arrowSize, arrowCenter)
      DPadControl.swift            # The joystick: 4 arrows + center button (onShift, onToggleAll)
      PushButton.swift             # Backlit mechanical button (labelText/symbolName, glowColor, isSquare, onTap, menu)
      AnalogMeterControl.swift     # Vintage needle gauge (value -100…100)
      OutputLedControl.swift       # Large output LED (isOn, label)
      NetworkView.swift            # Signal-flow diagram: bulbs (neurons) + wires (weights)
      MetalPlateView.swift         # Multi-line instruction plate (lines) + MetalLabelView (text)
    Presets/                       # Bundled, pre-trained example networks (*.pcn)
    Scripts/
      generate_presets.swift       # Host tool that trains + WRITES the Presets/*.pcn
    Assets.xcassets/               # App icon, accent color, Tutorial* screenshots (intro)
    Base.lproj/LaunchScreen.storyboard
  PerceptronDemoTests/             # Unit tests
    PerceptronDemoTests.swift      # Single-layer engine tests (pinned to the C# reference)
    MLPEngineTests.swift           # Multi-layer engine + "learns a moving T" proof
    TPatternData.swift             # Shared T-detection training set (tests + generator)
    PresetGeneratorTests.swift     # PROVES each preset trains (does not write files)
  PerceptronDemoUITests/           # UI tests (launch with -skipIntro to bypass the intro)
  ci_scripts/                      # Xcode Cloud pre/post build hooks
```

> **Note:** The Xcode project uses **file-system-synchronized groups**
> (`PBXFileSystemSynchronizedRootGroup`) for the app and test folders, so new
> files added under `PerceptronDemo/`, `PerceptronDemoTests/`, etc. are picked up
> automatically — no `project.pbxproj` editing needed. Unknown types like `.pcn`
> are bundled as resources.

### Key Components

- **SceneDelegate.swift** — Builds the `UIWindow` programmatically with a
  `PerceptronPanelViewController` root, then presents `IntroViewController` as a
  modal on **every launch** (deferred to the next runloop so the panel is in the
  hierarchy first). UI tests pass the `-skipIntro` launch argument to reach the
  panel directly.
- **PerceptronPanelViewController.swift** (~511 LOC) — The main screen. Three
  columns laid out manually in `viewDidLayoutSubviews` → `layoutPanel()`:
  switches + d-pad (left), weight knobs + BIAS/RATE (center), meter + LED +
  plates (right). A **LEARN + / RESET / LEARN − / ⚙** button strip sits along
  the bottom. Fixed **4×4 grid** (`gridSize = 4`).
- **IntroViewController.swift** (~457 LOC) — The launch "briefing card": what the
  machine does, 1958 history, an illustrated panel inventory (using the
  `Tutorial*` imageset screenshots), and Wikipedia / video links, all in a scroll
  view. Dismissed with a backlit **BEGIN** button (`isModalInPresentation = true`
  so it can't be swiped away). This is also the natural target for an **About**
  action from the panel.
- **PerceptronEngine.swift** (~84 LOC) — Ports only the **classic 1958 Rosenblatt
  rule** (`OUTPUT = Σ(input × weight) + bias`). `weights` is `private(set)`;
  mutate via `setWeight(_:_:)` / `learn(_:desiredPositive:)` / `resetWeights()`.
  Switch inputs are +1 (ON) / −1 (OFF); weights & bias clamp to −30…30; default
  `learningRate = 10.0`. A `Comparable.clamped(to:)` extension matches C#'s
  `Math.Clamp`.

### Gear Menu — Save / Load / Examples / About

The panel has a round **gear button** at the right end of the bottom
LEARN/RESET strip. It's a `PushButton` with a `menu`, so a **single tap opens
the menu** (`showsMenuAsPrimaryAction` on a transparent `UIButton` overlay — no
long press on iPad or Mac). The menu offers:

- **Save…** — serializes the full panel state to a `.pcn` file (JSON) and presents
  the system **export** document picker.
- **Load…** — presents the **import** document picker; the chosen file is decoded
  and pushed back into the engine and every control. Grid-size mismatches and
  invalid files are rejected with an alert (never partially applied).
- **Load Example ▸** — a submenu of bundled, pre-trained **single-layer** networks.
  Each item loads its `.pcn` directly (no picker).
- **Signal Flow (1986)** — opens the second screen (below).
- **About** — presents the same `IntroViewController` shown on launch.

**Snapshot / preset machinery** (`PerceptronSnapshot.swift`):

- `PerceptronSnapshot` — `Codable` state: `gridSize`, `weights`, `bias`,
  `learningRate`, `switchStates`, plus an optional `mlp` payload
  (`hiddenCount` / `hiddenWeights` / `hiddenBiases` / `outputWeights`) that is
  present only for multi-layer presets and `nil` for single-layer ones (so old
  files stay valid). `.fileExtension` is `"pcn"`; `jsonData()` emits
  pretty-printed, key-sorted JSON so regenerated presets diff cleanly.
- `Preset` — a `CaseIterable` enum cataloging the bundled examples. Each case has
  a `kind` (`.singleLayer` / `.multiLayer`); use `Preset.singleLayerCases` /
  `Preset.multiLayerCases` to pick the right set per screen. `loadSnapshot(from:)`
  reads and decodes the bundled `<rawValue>.pcn`.

**Presets are generated by a host script, and PROVEN by tests** — an important
two-part split forced by the sandbox:

- **Writing:** `PerceptronDemo/Scripts/generate_presets.swift` is a standalone
  `swift` tool (no Xcode/simulator). Run it to (re)generate every `.pcn`:
  ```bash
  swift PerceptronDemo/Scripts/generate_presets.swift
  ```
  It trains each network from scratch, asserts correct classification (exits
  non-zero otherwise), and writes `PerceptronDemo/Presets/*.pcn`.
  **Why a script and not the test:** the XCTest process is **sandboxed** on both
  the iOS Simulator and the Mac Catalyst host, so a test's `write(to:)` into the
  source tree is silently redirected into a container and never lands. The script
  runs unsandboxed and *can* write the repo. It uses the **same** engine/training
  logic as the app, so its output behaves identically when loaded.
- **Proving:** `PresetGeneratorTests` / `MLPEngineTests` re-train the same
  networks in-memory and assert they converge and classify correctly. They do
  **not** write files; they also cross-check that the bundled `.pcn` matches the
  expected dimensions, so an out-of-date file (training set changed but the
  script wasn't re-run) is caught.
- **Constraint:** the single-layer examples must be **linearly separable** (no
  XOR-style patterns) or their convergence assertion fails. Position-invariant
  shape detection (the moving T) needs the multi-layer `MLPEngine`.

### Second Screen — Signal Flow (multi-layer, 1986)

`SignalFlowViewController` presents the network as an **illuminated patch panel**
backed by `MLPEngine` (a `16 → 6 → 1` multi-layer perceptron). Left→right:
input switch grid + joystick → input bulbs → weight-wires → 6 hidden bulbs →
output bulb + meter/LED. It opens on the bundled, pre-trained **"T Anywhere"**
preset, which detects a T shape *at any position* in the grid — something the
single-layer panel provably cannot do.

It's reached from the gear menu and **slides in from the right** while the main
panel slides out to the left (`SlideTransitionDelegate`, reversed on dismiss),
as if panning your gaze across the bench. The switch grid uses the same
`SwitchGrid.Metrics` as the main panel, so the switches are the same size on
both screens, and the same `DPadControl` joystick sits below it — walking the T
around the grid while the output LED stays lit is the whole demonstration.

- **`MLPEngine.swift`** — ReLU hidden layer + backprop, ported from the desktop
  `BACKPROP` rule but generalized so `hiddenCount` is independent of
  `inputCount`. `learn(_:desiredPositive:)` is the sign-gated live rule (for the
  LEARN buttons); `trainStep(_:desiredPositive:margin:)` keeps pushing to a
  margin (used offline by the generator to avoid a fragile boundary). Weights
  seed deterministically (a fixed LCG) so training is reproducible; the seed
  spread deliberately **breaks symmetry** so the 6 hidden units specialize
  instead of collapsing into one saturated detector.
- **`NetworkView.swift`** — owner-drawn: **bulbs** = neurons (brightness =
  activation), **wires** = weights (thickness = |weight|, colour = sign
  (amber +, blue −), glow = live signal = |weight|·source-activation). Wires and
  neurons are **display-only**; training is via LEARN +/− or a loaded preset.
- **Why 6 hidden nodes, not 4:** 4 ReLU units do **not** converge on the moving-T
  with this rule; 6 do. `MLPEngineTests.learnsTranslationInvariantT` pins this
  (it *is* the app's lesson — more neurons = more capacity). If you add a
  multi-layer preset, keep it linearly separable *after* the hidden layer or add
  capacity until it converges.

### Differences From the Desktop App

The iOS app is an intentionally **scoped-down** port. Notable differences:

| Feature | Desktop (C#) | iOS (Swift) |
|---------|--------------|-------------|
| Grid size | 1–10, adjustable + linear mode | Fixed 4×4 |
| Math rules | 7 rules (1958–1986), Math dial | Classic 1958 (main panel) + 1986 MLP (Signal Flow screen) |
| Multi-layer / backprop | 1986 rule + BRAIN node graph | `MLPEngine` 16→6→1 + Signal Flow patch-panel view |
| Position-invariant shapes | (n/a) | "T Anywhere" preset on the Signal Flow screen |
| Save / Load | `.pcn` JSON files (SAVE/LOAD buttons) | `.pcn` JSON files via gear-menu (document picker) |
| Pre-trained examples | None | Bundled presets in gear-menu "Load Example ▸" + Signal Flow |
| Manual / Brain / Make dialogs | Present | Not ported |
| Intro / About | None (Manual has Credits/About page) | Full intro card on every launch |
| Teletype / debug output | PrinterDialog | Not ported |

### Conventions

- **Controls are owner-drawn `UIView` subclasses** using Core Graphics in
  `draw(_:)`, each exposing a small property/closure API (`onValueChanged`,
  `onTap`, etc.) rather than target/action. Follow this pattern for new controls.
- **Layout is manual** (frame-based in `viewDidLayoutSubviews`), not Auto Layout,
  for the panel — mirroring the desktop's absolute positioning. The intro screen
  *does* use Auto Layout (it's a scrollable document).
- **Palette** is duplicated as a private `Palette` enum where needed and should
  match the desktop color reference (black chassis, olive-grey brushed plates,
  engraved monospaced text, backlit glows).

## Architecture

> The rest of this document describes the **Windows Forms desktop app** (C#).
> For the Swift/UIKit app, see [iOS App (PerceptronDemo)](#ios-app-perceptrondemo)
> above.

### Core Components

- **MainForm.cs** (~1,680 LOC) - Main window with custom chrome, hosts all panels, handles layout/resize
- **PerceptronEngine.cs** (~480 LOC) - Neural network: single-layer perceptron + multi-layer with backpropagation
- **ManualDialog.cs** (~930 LOC) - Multi-page user manual with vintage paper styling
- **DebugDialog.cs** (~650 LOC) - Interactive neural network visualization ("THE BRAIN")

### Hardware Build Dialogs

- **PrintSchematicDialog.cs** (~560 LOC) - Circuit schematic with op-amp symbols, SPDT switches, resistors, LED circuit
- **POCBreadboardDialog.cs** (~1,360 LOC) - Interactive breadboard with `ComponentRegion` hover detection, component tooltips
- **BOMDialog.cs** (~370 LOC) - Bill of Materials with DataGridView, E24 resistor value calculations, CSV export
- **PartsDatabase.cs** (~320 LOC) - Centralized electronic component database with specs, prices, and part numbers

### PartsDatabase

Centralized database of electronic components used by BOM, Schematic, and Breadboard dialogs.

**Key Components:**
| Component ID | Description | Part Number | Price |
|--------------|-------------|-------------|-------|
| PWR-BAT-9V | 9V Battery | Duracell MN1604 | $3.00 |
| PWR-REG-7805 | +5V Regulator | LM7805CT | $0.65 |
| IC-OPAMP-LM358 | Dual Op-Amp | LM358N | $0.55 |
| SW-SPDT-TOGGLE | SPDT Switch | E-Switch 100SP1T1B4M2QE | $1.50 |
| RES-10K | Reference Resistor | Yageo CFR-25JB-52-10K | $0.10 |
| RES-4K7 | Pull-up Resistor | Yageo CFR-25JB-52-4K7 | $0.10 |
| RES-470R | LED Limiter | Yageo CFR-25JB-52-470R | $0.10 |
| LED-GRN-3MM | Input LED | Kingbright WP7113GD | $0.15 |
| LED-GRN-5MM | Output LED | Kingbright WP7113SGD | $0.20 |
| BB-830 | Breadboard | BusBoard BB830 | $6.00 |

**Helper Methods:**
```csharp
// Calculate input resistor: Rin = Rf/|weight|
PartsDatabase.CalculateInputResistance(weight)

// Round to nearest E24 value
PartsDatabase.GetNearestE24Value(ohms)

// Format: "10k", "470R", "1M"
PartsDatabase.FormatResistorValue(ohms)

// Get all fixed components for any perceptron
PartsDatabase.GetFixedComponents()

// Get per-input components (switches, LEDs, resistors)
PartsDatabase.GetPerInputComponents()
```

### Math Rules (Selectable via Math Dial)

The Math dial allows selecting different neural network computation rules spanning 1958-1986:

| Position | Label | Code Name | Description |
|----------|-------|-----------|-------------|
| 1 o'clock | **1958** | PERCEPTRON_CLASSIC | Original Rosenblatt (1-to-1 connectivity) |
| 2 o'clock | **1958+** | RULE_1958_SUM | Fully connected, sum: h[j] = Σ(input[i] × weight[j]) |
| 3 o'clock | **1958m** | RULE_1958_AVG | Fully connected, average: h[j] = (1/N) × Σ(input[i] × weight[j]) |
| 4 o'clock | **1958/+** | RULE_1958_DIV_SUM | Divide inputs then sum: h[j] = Σ((input[i]/N) × weight[j]) |
| 5 o'clock | **1958/m** | RULE_1958_DIV_AVG | Divide inputs then average |
| 6-8 o'clock | *(empty)* | - | Reserved for future expansion |
| 9 o'clock | **1960** | WIDROW_HOFF | Widrow-Hoff/LMS (1-to-1): w(t+1) = w(t) + η(d-y)x |
| 10 o'clock | **1986** | BACKPROP | ReLU + backpropagation MLP |

**Learning Rules:**
- 1958 variants: Update only when wrong (original perceptron rule)
- 1960: Continuous error, MSE gradient descent
- 1986: Full backpropagation through hidden layer

**Common:**
- Switches output **+1 (ON) or -1 (OFF)**, never 0
- Weights range from **-30 to +30**
- Default learning rate: **10.0**
- Bias is applied at output stage (not in hidden layer) for 1958 variants

### UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Mark I Perceptron Simulator by Frank Rosenblatt, 1958  [X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────┐  ┌─────────────┐  ┌───────────────────────┐   │
│  │ SWITCHES│  │    KNOBS    │  │   METER / LED /       │   │
│  │  (NxN)  │  │    (NxN)    │  │   FORMULA / INSTRUCT  │   │
│  │         │  │             │  │   VIDEO TUTORIAL      │   │
│  │  [D-PAD]│  │   [BIAS]    │  │                       │   │
│  └─────────┘  └─────────────┘  └───────────────────────┘   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ Grid  Math  LEARN+  RATE  LEARN-  RESET SAVE LOAD BRAIN MANUAL│
│ Size  Dial    ●           ●        ▢    □    □    □     □   │
└─────────────────────────────────────────────────────────────┘
```

### Special Modes

**Linear Mode (Easter Egg):**
When at grid size 1, turning the knob down further enters linear mode:
- Adds nodes one at a time (2, 3, 4... up to 25)
- Nodes wrap at 5 per row (creating non-square configurations)
- Allows testing arbitrary node counts without square grids

**Math Dial Modes:**
The Math dial (replaces the old MLP toggle) controls:
- **Connectivity**: 1958 uses 1-to-1; all others use fully-connected
- **Computation**: How hidden nodes process inputs
- **Learning**: Which weight update rule to use
See "Math Rules" section above for details.

### Brain Dialog ("THE BRAIN")

Interactive neural network visualization with:
- **Input node clicking**: Click any input node to toggle the corresponding switch in MainForm
- **Hidden node selection**: Click a hidden node to select it, then use arrow keys to adjust its weight
- **Red connections**: Connections from +1 (ON) inputs to hidden nodes draw in red
- **Real-time updates**: Weights and activations update as you adjust knobs

**Implementation Details:**
- `_inputNodePositions` / `_hiddenNodePositions` - PointF arrays for hit-testing
- `InputNodeClicked` event - raised with node index when input clicked
- `WeightChangeRequested` event - raised with (nodeIndex, delta) for weight adjustment
- Node radius: ~15px for hit detection
- Selected hidden node highlighted with contrasting border
- `UpdateVisualization(inputs, weights, hiddenActivations, output)` - full state update

### Hardware Build Features (Chapter X: Build It Yourself)

The application supports building a physical perceptron with real components:

**PrintSchematicDialog** - Professional circuit schematic with KiCad export:
- LM7805 voltage regulator (9V → 5V)
- LM358 dual op-amp IC in summing amplifier configuration
- SPDT toggle switches for +1/-1 input selection
- Input resistors calculated from weights: R = REF_RESISTOR / |weight| (where REF_RESISTOR = 10kΩ)
- 10kΩ feedback resistor (Rf)
- Output LED with 470Ω current-limiting resistor
- Decoupling capacitors (10µF electrolytic, 0.1µF ceramic)
- **Continuous vertical summing bus**: No gaps in wiring connections
- **Switch-to-resistor connections**: Direct wire connections eliminate gaps
- **Clean labeling**: Part numbers shown only once to avoid overlap (U1A shows LM358N, U1B omits duplicate)
- **KiCad Export**: "Export KiCad" button generates .kicad_sch files for professional EDA tools
- **SPICE simulation ready**: Exported schematics can be simulated in KiCad/ngspice

**POCBreadboardDialog** - Interactive breadboard with ELECTRICAL ACCURACY:
- `ComponentRegion` class for hit-testing and tooltips on hover
- **Hole-based component placement**: Components placed at specific (row, column) coordinates
- **Electrical connectivity modeling**: Holes represent actual breadboard connections
- **Scalable layout**: Shows 4 inputs in detail (configurable 3-6), with note about additional inputs
- **Realistic power rail holes**: Clustered in groups of 5 with 1-space gaps (dense power distribution)
- **Row/Column numbering**: Column numbers (5, 10, 15...) at top/bottom; Row letters (A-Z, then numbers) on left
- **Extra row above center channel**: Additional row of holes for better IC placement
- **Power supply selector**: Dropdown to choose power configuration (off-board, on-board battery, USB 5V, bench supply)
- **Thick, visible wires**: 4-5px wires for clarity, color-coded by input
- **Shared ground bus**: Realistic ground distribution at column 2
- **Accurate jumper routing**: All connections traced to actual power rail holes
- **2× larger hole spacing**: 20px spacing for detailed, buildable view (was 10px)
- **All wiring visible**: Complete circuit path traceable from input to output

**BOMDialog** - Bill of Materials:
- `GetStandardResistorValue()` - Rounds to nearest E24 standard value
- `GetResistorPartNumber()` - Generates Yageo CFR-25JB series part numbers
- DataGridView with columns: QTY, DESCRIPTION, VALUE/SPECS, PACKAGE, MFR PART NUMBER, SUPPLIER, UNIT $
- Copy to Clipboard and Export CSV functionality
- Real manufacturer part numbers (DigiKey/Mouser sourced)

### Custom Controls (Controls/ folder)

All controls are owner-drawn with GDI+ for the vintage aesthetic:

| Control | Description | Key Properties |
|---------|-------------|----------------|
| **SwitchControl** | Toggle switch + LED | `IsOn`, `Value` (+1/-1), scalable |
| **KnobControl** | Rotary dial (-30 to +30) | `Value`, `Step` (0.05) |
| **SettingsKnobControl** | Configurable range knob | `MinValue`, `MaxValue`, `MinValuePointsUp`, `BelowMinimumAttempted` event, `KnobClick` auto-repeat |
| **MathDialControl** | Math rule selector | `SelectedRule`, clock-position labels |
| **MechanicalPushButton** | Round/square button | `IsSquare`, `GlowColor`, `LabelText` |
| **ArrowButton** | Triangular d-pad button | `Direction`, `GlowColor` |
| **AnalogMeterControl** | Vintage gauge | `Value` (-100 to +100) |
| **OutputLedControl** | Large LED | `IsOn` |
| **MetalLabelControl** | Embossed plate | `LabelText`, `Subdued`, `CustomTextColor` |
| **MetalPlateControl** | Multi-line instruction plate | `InstructionLines` |
| **FormulaPlateControl** | Formula display | Shows perceptron equation |

### Key Event Flows

**Input Node Click → Switch Toggle:**
```
DebugDialog.InputNodeClicked(index)
  → MainForm handler toggles _switches[index].IsOn
  → UpdateOutput() recalculates
  → DebugDialog.UpdateVisualization() redraws
```

**Hidden Node Weight Adjustment:**
```
DebugDialog: Click hidden node → _selectedHiddenNodeIndex set
DebugDialog: Arrow keys → WeightChangeRequested(nodeIndex, delta)
  → MainForm handler adjusts _knobs[nodeIndex].Value
  → KnobValueChanged → UpdateOutput()
```

**Linear Mode Activation:**
```
GridSizeKnob at MinValue (1) → user drags down further
  → BelowMinimumAttempted event fires
  → MainForm._linearMode = true, _linearNodeCount = 2
  → Subsequent decrements: _linearNodeCount++ (up to 25)
```

**Math Dial Change:**
```
ConfigKnob.RuleChanged(newRule)
  → MainForm updates _engine.CurrentRule
  → UpdateOutput() uses new computation method
  → FormulaPlate updates formula text
```

### Key Implementation Details

1. **Switch Toggle Colors**: OFF = `Color.FromArgb(70, 70, 70)` (darker/shadowed), ON = `Color.FromArgb(100, 100, 100)` (medium gray)

2. **Glow Effects**: Use `PathGradientBrush` with offset center point for backlit plastic look
   - Center alpha: 60-100 depending on state
   - Edge alpha: 10
   - Offset center slightly down-left

3. **Conditional Glows**:
   - Learn+/Learn- buttons: Glow only when `_switches.Any(s => s.IsOn)`
   - Save button: Glows green only when `_weightsDirty == true`

4. **Learning Rate Knob**: Range -30 to +30, step 0.05, `MinValuePointsUp = false` (zero points up)

5. **Grid Scaling** (7×7 and larger):
   - Switch width: `Math.Max(28, 50 - (gridSize - 6) * 7)`
   - Knob width: `Math.Max(45, 70 - (gridSize - 6) * 8)`

6. **Custom Chrome**: `FormBorderStyle.None`, custom title bar, `WM_NCHITTEST` for resize edges

### ManualDialog Implementation

**PaperPanel Class** (nested in ManualDialog):
- Custom control that renders aged paper texture
- `PaperColor = Color.FromArgb(242, 238, 225)` - aged cream
- Adds noise texture for vintage paper look
- "DECLASSIFIED" watermark on certain pages

**Navigation:**
- Clickable Table of Contents with `_tocClickRegions` list
- Page buttons with arrow navigation
- Direct page jumping via TOC clicks

**Content Rendering:**
- `RenderPage(int pageNum)` - draws content based on page number
- Fixed-width font for technical sections
- Different styling for headers, body text, and code blocks

### Manual Dialog Chapters

| # | Chapter | Page |
|---|---------|------|
| I | Introduction | 3 |
| II | Press Release (1958 NYT) | 5 |
| III | [REDACTED] | 7 |
| IV | Operating Procedures | 12 |
| V | Selectable Math Dial | 19 |
| VI | The Algorithm | 24 |
| VII-VIII | [REDACTED] | 29, 34 |
| IX | Credits / About | 39 |
| X | Build It Yourself | 45 |
| XI | [REDACTED] | 51 |

### Circuit Validation

The circuit has been fully validated for electrical correctness:

**See CIRCUIT_AUDIT.md for complete validation:**
- ✅ **Phase 1**: Schematic validated as source of truth (inverting summing amplifier)
- ✅ **Phase 2**: Breadboard verified to match schematic (buildable)
- ✅ **Phase 3**: KiCad export validated with complete netlist (simulatable)
- ✅ **Phase 4**: Code refactored (clean, organized, maintainable)

**The circuit implements:** Vout = -Σ(INPUT × weight) using an LM358 op-amp as inverting summer.

**Buildability**: A person can follow the breadboard layout with exact hole coordinates and build a working physical perceptron that will function correctly.

### Embedded Resources

- `resources/perceptrons.png` - Reference image
- `resources/prompt.txt` - Original conversational build prompts
- `resources/complete_prompt.txt` - Comprehensive rebuild specification (V1)
- `resources/complete_prompt2.txt` - Full system with electrical validation (V2)
- `resources/Recreate_this_Program_AI_Prompt_BASE.txt` - Combined master prompt (all prompts concatenated)

Resource naming: `PerceptronSimulator.resources.{filename}`

## Design Guidelines

- **Aesthetic**: 1950s industrial/scientific equipment
- **Colors**: Muted dark grays, olive-greens, metallic tones
- **Paper**: Aged cream `Color.FromArgb(242, 238, 225)` with noise texture
- **Glows**: Subtle (alpha 60-100), simulating backlit translucent plastic
- **Feel**: Tactile, mechanical, physical

## Color Reference

```csharp
// Metal plates
Gradient top:    Color.FromArgb(70, 75, 70)
Gradient bottom: Color.FromArgb(50, 55, 50)
Text:            Color.FromArgb(130, 135, 130)
Subdued text:    Color.FromArgb(85, 90, 85)

// Buttons
Yellow glow:     Color.FromArgb(255, 220, 80)  // Brain, Manual
Green glow:      Color.FromArgb(120, 255, 120) // Learn+, Save (when dirty)
Red glow:        Color.FromArgb(200, 80, 60)   // Load

// Video Tutorial link
Green text:      Color.FromArgb(100, 180, 100)

// Dialog backgrounds
Dark gray:       Color.FromArgb(35, 35, 35)   // BOM, Print dialogs
Grid cell bg:    Color.FromArgb(45, 45, 45)   // DataGridView
Header bg:       Color.FromArgb(60, 60, 60)   // Column headers

// BOM Dialog
Title yellow:    Color.FromArgb(255, 220, 100)
Total label:     Color.LimeGreen

// Breadboard
Power rail red:  Color.FromArgb(200, 60, 60)
Power rail blue: Color.FromArgb(60, 60, 200)
Breadboard tan:  Color.FromArgb(230, 210, 180)
```

## PerceptronEngine Implementation

### Calculation Methods (by MathRule)

```csharp
// PERCEPTRON_CLASSIC (1958): 1-to-1 connectivity
hidden[j] = inputs[j] * weights[j]

// RULE_1958_SUM: Fully connected, sum
hidden[j] = Σ(inputs[i] * weights[j])  // all inputs to each hidden

// RULE_1958_AVG: Fully connected, average
hidden[j] = (1/N) * Σ(inputs[i] * weights[j])

// RULE_1958_DIV_SUM: Divide inputs first
hidden[j] = Σ((inputs[i]/N) * weights[j])

// RULE_1958_DIV_AVG: Divide inputs, then average
hidden[j] = (1/N) * Σ((inputs[i]/N) * weights[j])

// WIDROW_HOFF (1960): 1-to-1, continuous error
hidden[j] = inputs[j] * weights[j]
// Learning: w += learningRate * error * input

// BACKPROP (1986): Full MLP with hidden layer
// Uses HiddenWeights[i,j], HiddenBiases[j], OutputWeights[j]
// ReLU activation: max(0, x)
// Backpropagation through hidden layer
```

### Key Properties

- `HiddenWeights` - double[inputCount, hiddenCount] for backprop mode
- `HiddenBiases` - double[hiddenCount] for backprop mode
- `OutputWeights` - double[hiddenCount] for backprop mode
- `CurrentRule` - MathRule enum controlling computation

## Files

| File | LOC | Purpose |
|------|-----|---------|
| MainForm.cs | ~1,817 | Main UI, linear mode, Math dial, save/load |
| ManualDialog.cs | ~966 | User manual with Math Dial chapter, PaperPanel class |
| DebugDialog.cs | ~648 | Interactive brain visualization, node click events |
| PrintSchematicDialog.cs | ~882 | Circuit schematic drawing + KiCad export |
| POCBreadboardDialog.cs | ~2,174 | Hole-based breadboard with electrical modeling (cleaned) |
| BOMDialog.cs | ~337 | Bill of Materials (uses PartsDatabase) |
| PartsDatabase.cs | ~452 | Centralized electronic component data |
| BreadboardModel.cs | ~133 | Breadboard connectivity and coordinate system |
| KiCadExporter.cs | ~286 | Export circuits to KiCad with complete netlist |
| PrinterDialog.cs | ~485 | Vintage teletype debug output window |
| PerceptronEngine.cs | ~482 | 7 math rules (1958-1986), backprop hidden layer |
| DebugLogger.cs | ~35 | Global debug singleton routing to PrinterDialog |
| 15 Custom Controls | ~3,356 | UI components (incl. MathDialControl, TogglePlateControl) |
| **Total** | **~12,050** | Clean, validated, production-ready |

### Control Files Detail

| Control File | LOC | Key Features |
|--------------|-----|--------------|
| SwitchControl.cs | ~157 | `IsOn`, `Value` (+1/-1), scalable rendering |
| KnobControl.cs | ~325 | Drag/wheel/keyboard input, tick marks |
| SettingsKnobControl.cs | ~389 | Auto-repeat timer, `BelowMinimumAttempted` event |
| ConfigKnob.cs | ~326 | `MathRule` enum, clock-hour discrete positions |
| MathDialControl.cs | ~383 | Discrete-position rotary dial, 7 learning rules |
| MechanicalPushButton.cs | ~420 | Round/square variants, `GlowColor` effect |
| AnalogMeterControl.cs | ~194 | 16ms animation timer for needle movement |
| ArrowButton.cs | ~233 | Triangular d-pad with directional glow |
| MetalLabelControl.cs | ~124 | Brushed metal with screw details |
| MetalPlateControl.cs | ~128 | Multi-line instruction plates |
| MetalPlateButton.cs | ~157 | Clickable metal plate with hover/press states |
| OutputLedControl.cs | ~80 | Large LED with PathGradientBrush glow |
| FormulaPlateControl.cs | ~135 | Dynamic formula display based on math rule |
| TogglePlateControl.cs | ~197 | 1950s wall plate rocker switch (SETTINGS/TELETYPE) |
| FlatNumericUpDown.cs | ~108 | Dark-themed numeric spinner control |
