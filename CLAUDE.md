# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Mark I Perceptron Simulator** - a Windows Forms application that recreates Frank Rosenblatt's 1958 perceptron machine with a vintage 1950s industrial aesthetic. The UI features 12 custom-drawn controls designed to look like physical hardware: toggle switches with LEDs, rotary knobs, analog meters, and backlit mechanical buttons.

**Key Insight**: This program contains its own DNA — the complete prompts used to generate the codebase are embedded as resources. Feed `resources/complete_prompt.txt` into Claude Code to recreate the entire application.

## Build Commands

```bash
# Build debug
dotnet build

# Run the application
dotnet run

# Build single-file release (outputs to c:\perceptron_release\)
dotnet publish -p:PublishProfile=SingleFileRelease
# Or use: publish.bat
```

## Architecture

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
