# TODO: Create Comprehensive Reproduction Prompt

## Current Status

**Completed:**
- ✅ Combined existing prompts (prompt.txt + complete_prompt.txt + complete_prompt2.txt)
- ✅ Created base file: Recreate_this_Program_AI_Prompt_BASE.txt (1,353 lines)
- ✅ Added electrical validation requirements
- ✅ Added circuit topology and principles
- ✅ Added architecture and code organization

**File:** resources/Recreate_this_Program_AI_Prompt_BASE.txt

---

## Remaining Work to Create "One Prompt to Rule Them All"

### 1. Custom Controls Implementation Details (~500 lines)

Add complete specifications for all 13 controls:

**Per Control, specify:**
- Exact properties and their purposes
- Paint method with complete GDI+ code
- Event handling (mouse, keyboard, value changes)
- Scaling logic
- Color gradients and effects
- Hit testing and interaction

**Controls to document:**
1. SwitchControl - Toggle mechanics, LED rendering, +1/-1 value
2. KnobControl - Rotation math, mouse drag, tick marks, -30 to +30 range
3. SettingsKnobControl - Min/max config, BelowMinimumAttempted event
4. ConfigKnob (Math Dial) - 7 discrete positions, clock-hour placement
5. AnalogMeterControl - Needle animation, arc drawing, -100 to +100 scale
6. OutputLedControl - PathGradientBrush glow, on/off states
7. MechanicalPushButton - Round/square variants, glow effects, press states
8. ArrowButton - Triangular geometry, directional glow
9. MetalLabelControl - Brushed metal gradient, embossed text, corner screws
10. MetalPlateControl - Multi-line text, engraved effect
11. MetalPlateButton - Clickable plate variant
12. FormulaPlateControl - Dynamic formula display based on math rule
13. FlatNumericUpDown - Styled numeric input

### 2. MainForm Complete Implementation (~800 lines)

**Layout & Chrome:**
- Custom window chrome (borderless, draggable title bar)
- WM_NCHITTEST for resize edge detection
- Three-column panel layout (switches, knobs, display)
- Dynamic grid scaling (1×1 to 10×10)
- Grid size selector (dial with BelowMinimumAttempted for linear mode)

**State Management:**
- Weight array management
- Dirty state tracking (_weightsDirty)
- Conditional button glows (Learn buttons only when switches ON, Save when dirty)
- Grid resize logic (control sizing, spacing calculations)

**Event Handling:**
- Switch value change → UpdateOutput
- Knob value change → UpdateOutput
- Learn+/Learn- button logic with conditional execution
- Save/Load with JSON serialization
- Brain button → open DebugDialog
- Manual button → open ManualDialog
- Math dial change → update formula plate

**Linear Mode Easter Egg:**
- Trigger at grid size 1, turn dial further down
- Increment node count (2, 3, 4... up to 25)
- Wrap at 5 per row
- Non-square grid layout logic

### 3. ManualDialog Implementation (~600 lines)

**PaperPanel Class:**
- Aged cream paper texture: Color.FromArgb(242, 238, 225)
- Noise texture rendering for vintage look
- "DECLASSIFIED" watermark on specific pages
- Custom Paint method

**Navigation:**
- Table of contents with clickable regions (_tocClickRegions list)
- Page buttons (prev/next with arrow labels)
- Direct page jumping via TOC clicks
- Current page highlighting

**Content Rendering:**
- RenderPage(int pageNum) switch statement
- 11+ chapters with specific content
- Different fonts for headers, body, code blocks
- Fixed-width font for technical sections
- [REDACTED] sections
- 1958 NYT press release text
- Operating procedures
- Algorithm explanation
- Build It Yourself chapter
- Credits

### 4. DebugDialog (Brain) Implementation (~400 lines)

**Network Visualization:**
- Calculate node positions (input and hidden layers)
- Draw connections with color-coding (green pos, red neg, RED for active inputs)
- Weight value labels on connections
- Activation value labels on nodes

**Interactive Features:**
- Input node clicking → raise InputNodeClicked event → MainForm toggles switch
- Hidden node selection → click to select, arrow keys to adjust weight
- WeightChangeRequested event with (nodeIndex, delta)
- Selected node highlight with contrasting border
- Real-time updates via UpdateVisualization method

**Hit Testing:**
- _inputNodePositions and _hiddenNodePositions arrays (PointF[])
- Node radius ~15px for click detection
- Find closest node to click location

### 5. Hardware Build Dialogs (~1,000 lines)

**PrintSchematicDialog:**
- Complete schematic drawing with op-amp symbols
- SPDT switch representation (3 terminals)
- Resistor symbols with value labels
- Continuous summing bus (no gaps!)
- Feedback resistor path
- Power supply section
- LED circuit
- Voltmeter symbol
- Ground symbols
- Gap-free connections verified
- "Export KiCad" button with complete netlist generation

**POCBreadboardDialog - CRITICAL ELECTRICAL DETAILS:**
- Hole-based coordinate system: GetHolePos(bbLeft, bbTop, row, col)
- Power rail calculations: GetTopPowerRailY(), GetBottomPowerRailY(), GetPowerRailHole()
- Component placement at exact row/column coordinates
- All jumper wire connections drawn
- RegisterComponent with EXACT hit rectangles matching visual positions
- Power supply selector dropdown
- Debug toggle showing hit regions
- Click selection (not hover)
- Shared ground bus at column 2
- 2× hole spacing (20px) for detailed view
- Shows 4 inputs in detail, note about additional

**Electrical Requirements:**
- Every component must have power
- All grounds connect to ground rail
- Summing bus connects ALL inputs
- Feedback path Pin 1 → Rf → Pin 2 complete
- No wires to nowhere
- Circuit traceable

**BOMDialog:**
- DataGridView with component list
- E24 resistor value rounding
- Per-input and fixed component separation
- Total cost calculation
- Copy to clipboard
- CSV export

**PartsDatabase:**
- All component specs, prices, part numbers
- CalculateInputResistance(weight) = Rf / |weight|
- GetNearestE24Value(ohms)
- FormatResistorValue(ohms)

### 6. PerceptronEngine - 7 Math Rules (~300 lines)

**Complete implementation for each rule:**
1. PERCEPTRON_CLASSIC (1958) - 1-to-1 connectivity
2. RULE_1958_SUM - Fully connected sum
3. RULE_1958_AVG - Fully connected average
4. RULE_1958_DIV_SUM - Divide inputs then sum
5. RULE_1958_DIV_AVG - Divide inputs then average
6. WIDROW_HOFF (1960) - LMS rule, continuous error
7. BACKPROP (1986) - Multi-layer with ReLU, backpropagation

**For each:**
- Forward propagation formula
- Learning rule (when to update, how to update)
- Hidden layer vs output layer handling
- Weight clamping logic

### 7. Exact Visual Details (~500 lines)

**Colors (every value):**
- All button glows (yellow, green, red - exact RGB)
- Metal plate gradients (top/bottom colors)
- Subdued vs normal text colors
- Dialog backgrounds
- Paper colors
- Breadboard tan, power rail colors

**Dimensions:**
- Grid scaling formulas for 7×7+
- Control sizing at different grid sizes
- Panel spacing and margins
- Title bar height (30px)
- All button sizes and positions

**GDI+ Techniques:**
- PathGradientBrush for glows (center alpha, edge alpha, offset)
- LinearGradientBrush for metallic surfaces
- How to draw corner screws, embossed text, shadows
- Needle animation for meter (16ms timer)

### 8. File I/O Formats (~200 lines)

**JSON Save Format:**
- weights array
- bias value
- gridSize
- isLinearMode, linearNodeCount
- currentRule

**KiCad Format:**
- S-expression syntax
- Component symbols
- Properties (Reference, Value, position)
- Wire definitions
- Power symbols
- Complete netlist structure

**CSV Export:**
- BOM format with headers
- Column structure

---

## Estimated Effort

**To create truly comprehensive prompt:**
- Read and analyze all ~9,615 LOC
- Extract key implementation patterns
- Document every detail systematically
- Organize into logical sections
- **Estimated result: 3,000-4,000 line prompt**
- **Time required: 10-15 hours traditional work**
- **With AI assistance: 2-4 hours of careful specification**

---

## Approach

**Option A: Detailed Manual Creation**
- Manually read each file
- Extract critical implementation details
- Write comprehensive specifications
- Most accurate, most time-consuming

**Option B: AI-Assisted Extraction**
- Use AI to analyze code files
- Extract patterns and implementations
- Generate detailed specifications
- Faster, might miss nuances

**Option C: Hybrid**
- AI extracts structure and patterns
- Manual review and enhancement
- Add critical details and validation
- Best balance of speed and accuracy

---

## When to Complete

**Recommended: After app is finalized**

Since you want to make more changes first, complete this prompt AFTER:
- All features are stable
- Circuit is final and validated
- No major refactors planned
- Ready for long-term reproduction

Then create the comprehensive prompt as final documentation.

---

## Next Steps (When Ready)

1. Finalize all app features and circuits
2. Run complete validation (all phases)
3. Choose approach (A, B, or C above)
4. Create comprehensive Recreate_this_Program_AI_Prompt.txt
5. Test by feeding to fresh Claude instance
6. Verify reproduction accuracy
7. Iterate until reproduction succeeds
