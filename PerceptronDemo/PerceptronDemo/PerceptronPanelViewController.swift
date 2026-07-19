//
//  PerceptronPanelViewController.swift
//  PerceptronDemo
//
//  The main instrument panel — a UIKit port of the desktop MainForm's
//  central UI. Three columns: switches + d-pad (left), weight knobs + bias
//  (center), meter + output LED + plates (right). A Learn+/Learn-/Reset
//  strip sits below. Fixed 4×4 grid for this first pass.
//

import UIKit

final class PerceptronPanelViewController: UIViewController {

    // MARK: - Model

    private let gridSize = 4
    private lazy var engine = PerceptronEngine(gridSize: gridSize)
    private var nodeCount: Int { gridSize * gridSize }

    // MARK: - Controls

    private var switches: [SwitchControl] = []
    private var knobs: [KnobControl] = []
    private let biasKnob = KnobControl()

    private let arrowUp = ArrowButton()
    private let arrowDown = ArrowButton()
    private let arrowLeft = ArrowButton()
    private let arrowRight = ArrowButton()
    private let centerToggle = PushButton()

    private let meter = AnalogMeterControl()
    private let outputLed = OutputLedControl()
    private let voltageLabel = MetalLabelView()
    private let biasLabel = MetalLabelView()
    private let formulaPlate = MetalPlateView()
    private let procedurePlate = MetalPlateView()

    private let learnPlusButton = PushButton()
    private let learnMinusButton = PushButton()
    private let resetButton = PushButton()
    private let rateKnob = KnobControl()
    private let rateLabel = MetalLabelView()

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black
        buildControls()
        updateOutput()
    }

    override func viewDidLayoutSubviews() {
        super.viewDidLayoutSubviews()
        layoutPanel()
    }

    // MARK: - Build

    private func buildControls() {
        // Switches (4×4) — flipping one recomputes the output.
        for _ in 0..<nodeCount {
            let sw = SwitchControl()
            sw.onStateChanged = { [weak self] in
                self?.updateOutput()
            }
            switches.append(sw)
            view.addSubview(sw)
        }

        // Weight knobs (4×4) — turning one writes into the engine.
        for i in 0..<nodeCount {
            let knob = KnobControl()
            knob.step = 0.05
            knob.onValueChanged = { [weak self] in
                guard let self else { return }
                self.engine.setWeight(i, knob.value)
                self.updateOutput()
            }
            knobs.append(knob)
            view.addSubview(knob)
        }

        // Bias knob.
        biasKnob.step = 0.05
        biasKnob.onValueChanged = { [weak self] in
            guard let self else { return }
            self.engine.bias = self.biasKnob.value
            self.updateOutput()
        }
        view.addSubview(biasKnob)

        // D-pad — shifts the switch pattern.
        arrowUp.direction = .up
        arrowUp.onTap = { [weak self] in self?.shiftPattern(dx: 0, dy: -1) }
        arrowDown.direction = .down
        arrowDown.onTap = { [weak self] in self?.shiftPattern(dx: 0, dy: 1) }
        arrowLeft.direction = .left
        arrowLeft.onTap = { [weak self] in self?.shiftPattern(dx: -1, dy: 0) }
        arrowRight.direction = .right
        arrowRight.onTap = { [weak self] in self?.shiftPattern(dx: 1, dy: 0) }
        [arrowUp, arrowDown, arrowLeft, arrowRight].forEach { view.addSubview($0) }

        centerToggle.isSquare = true
        centerToggle.onTap = { [weak self] in self?.toggleAllSwitches() }
        view.addSubview(centerToggle)

        // Swipe the center button like a joystick to shift the pattern, in
        // addition to the arrow buttons. A plain tap still toggles all/none.
        for dir: UISwipeGestureRecognizer.Direction in [.up, .down, .left, .right] {
            let swipe = UISwipeGestureRecognizer(target: self, action: #selector(handleCenterSwipe(_:)))
            swipe.direction = dir
            centerToggle.addGestureRecognizer(swipe)
        }

        // Right column.
        view.addSubview(meter)
        outputLed.label = "OUTPUT"
        view.addSubview(outputLed)

        voltageLabel.text = "Off=-1v  On=+1v"
        view.addSubview(voltageLabel)

        biasLabel.text = "BIAS"
        view.addSubview(biasLabel)

        formulaPlate.lines = ["OUTPUT = SUM(Switch x Weight) + Bias"]
        view.addSubview(formulaPlate)

        procedurePlate.lines = [
            "  OPERATING PROCEDURE",
            "",
            "1. Set switch pattern",
            "2. If output should be +",
            "   press [Learn +]",
            "3. If output should be -",
            "   press [Learn -]",
            "4. Repeat with patterns"
        ]
        view.addSubview(procedurePlate)

        // Learn / Reset strip.
        learnPlusButton.labelText = "LEARN +"
        learnPlusButton.glowColor = UIColor(red: 120/255, green: 1, blue: 120/255, alpha: 1)
        learnPlusButton.onTap = { [weak self] in self?.learn(desiredPositive: true) }
        view.addSubview(learnPlusButton)

        learnMinusButton.labelText = "LEARN -"
        learnMinusButton.glowColor = UIColor(red: 120/255, green: 1, blue: 120/255, alpha: 1)
        learnMinusButton.onTap = { [weak self] in self?.learn(desiredPositive: false) }
        view.addSubview(learnMinusButton)

        resetButton.labelText = "RESET"
        resetButton.glowColor = UIColor(red: 200/255, green: 80/255, blue: 60/255, alpha: 1)
        resetButton.onTap = { [weak self] in self?.resetAll() }
        view.addSubview(resetButton)

        // RATE knob — controls how much each Learn press moves the weights.
        // Matches the desktop: range -30…30, starts at 10.0 (the default that
        // makes a single press jump each weight by ±10).
        rateKnob.step = 0.05
        rateKnob.value = engine.learningRate   // 10.0 by default
        rateKnob.onValueChanged = { [weak self] in
            guard let self else { return }
            self.engine.learningRate = self.rateKnob.value
        }
        view.addSubview(rateKnob)

        rateLabel.text = "RATE"
        view.addSubview(rateLabel)
    }

    // MARK: - Layout

    private func layoutPanel() {
        let safe = view.safeAreaLayoutGuide.layoutFrame
        let margin: CGFloat = 24
        let content = safe.insetBy(dx: margin, dy: margin)

        // Reserve a strip at the bottom for Learn/Reset.
        let buttonStripHeight: CGFloat = 56
        let panelHeight = content.height - buttonStripHeight - 16
        let panelTop = content.minY

        // Three columns.
        let columnGap: CGFloat = 24
        let switchColWidth = content.width * 0.26
        let knobColWidth = content.width * 0.40
        let rightColWidth = content.width - switchColWidth - knobColWidth - columnGap * 2

        let switchColX = content.minX
        let knobColX = switchColX + switchColWidth + columnGap
        let rightColX = knobColX + knobColWidth + columnGap

        layoutSwitchColumn(x: switchColX, top: panelTop, width: switchColWidth, height: panelHeight)
        layoutKnobColumn(x: knobColX, top: panelTop, width: knobColWidth, height: panelHeight)
        layoutRightColumn(x: rightColX, top: panelTop, width: rightColWidth, height: panelHeight)
        layoutButtonStrip(x: content.minX, y: content.maxY - buttonStripHeight,
                          width: content.width, height: buttonStripHeight)
    }

    private func layoutSwitchColumn(x: CGFloat, top: CGFloat, width: CGFloat, height: CGFloat) {
        voltageLabel.frame = CGRect(x: x + (width - 130) / 2, y: top, width: 130, height: 18)

        let gridTop = top + 28
        let cell = min(width / CGFloat(gridSize), 72)
        let gridWidth = cell * CGFloat(gridSize)
        let gridX = x + (width - gridWidth) / 2
        let swSize = min(cell - 6, 56)

        for (i, sw) in switches.enumerated() {
            let r = i / gridSize, c = i % gridSize
            let cx = gridX + CGFloat(c) * cell + (cell - swSize) / 2
            let cy = gridTop + CGFloat(r) * (cell * 1.15)
            sw.frame = CGRect(x: cx, y: cy, width: swSize, height: swSize * 1.4)
        }

        // D-pad below the switch grid. Each arrow's triangle is drawn at its
        // original visual size and in its original position (its center sits
        // `arrowOffset` from the d-pad center). The tappable frame is inflated
        // by `hitPad` so it's easy to hit on iPad — the drawn triangle stays
        // put, only the invisible touch target grows. The edge facing the
        // center button is clamped so the enlarged area never overlaps it.
        let dpadCenterY = gridTop + CGFloat(gridSize) * (cell * 1.15) + 60
        let dpadCenterX = x + width / 2
        let hArrow = CGSize(width: 40, height: 16)          // up / down triangle
        let vArrow = CGSize(width: hArrow.height, height: hArrow.width) // left / right
        let arrowOffset: CGFloat = 35   // distance from d-pad center to arrow center
        let hitPad: CGFloat = 16        // touch padding added on each side
        let centerHalf: CGFloat = 23

        centerToggle.frame = CGRect(x: dpadCenterX - centerHalf, y: dpadCenterY - centerHalf,
                                    width: centerHalf * 2, height: centerHalf * 2)

        // Draws `size` centered on the arrow's on-screen point; pads the frame
        // by `hitPad`, but clamps the inner edge (the one pointing at the d-pad
        // center along `dir`) to the center button's boundary so the two never
        // overlap. `arrowCenter` keeps the triangle at its true position even
        // though the clamped frame is asymmetric.
        func padded(_ view: ArrowButton, size: CGSize, dir: ArrowDirection) {
            view.arrowSize = size
            let cx: CGFloat, cy: CGFloat
            var frame: CGRect
            switch dir {
            case .up:
                cx = dpadCenterX; cy = dpadCenterY - arrowOffset
                let bottom = dpadCenterY - centerHalf            // clamp to top of button
                let top = cy - size.height / 2 - hitPad
                frame = CGRect(x: cx - size.width / 2 - hitPad, y: top,
                               width: size.width + hitPad * 2, height: bottom - top)
            case .down:
                cx = dpadCenterX; cy = dpadCenterY + arrowOffset
                let top = dpadCenterY + centerHalf               // clamp to bottom of button
                let bottom = cy + size.height / 2 + hitPad
                frame = CGRect(x: cx - size.width / 2 - hitPad, y: top,
                               width: size.width + hitPad * 2, height: bottom - top)
            case .left:
                cx = dpadCenterX - arrowOffset; cy = dpadCenterY
                let right = dpadCenterX - centerHalf             // clamp to left of button
                let left = cx - size.width / 2 - hitPad
                frame = CGRect(x: left, y: cy - size.height / 2 - hitPad,
                               width: right - left, height: size.height + hitPad * 2)
            case .right:
                cx = dpadCenterX + arrowOffset; cy = dpadCenterY
                let left = dpadCenterX + centerHalf              // clamp to right of button
                let right = cx + size.width / 2 + hitPad
                frame = CGRect(x: left, y: cy - size.height / 2 - hitPad,
                               width: right - left, height: size.height + hitPad * 2)
            }
            view.frame = frame
            view.arrowCenter = CGPoint(x: cx - frame.minX, y: cy - frame.minY)
        }
        padded(arrowUp,    size: hArrow, dir: .up)
        padded(arrowDown,  size: hArrow, dir: .down)
        padded(arrowLeft,  size: vArrow, dir: .left)
        padded(arrowRight, size: vArrow, dir: .right)
    }

    private func layoutKnobColumn(x: CGFloat, top: CGFloat, width: CGFloat, height: CGFloat) {
        let cell = min(width / CGFloat(gridSize), 96)
        let gridWidth = cell * CGFloat(gridSize)
        let gridX = x + (width - gridWidth) / 2
        let knobSize = min(cell - 8, 88)

        for (i, knob) in knobs.enumerated() {
            let r = i / gridSize, c = i % gridSize
            let cx = gridX + CGFloat(c) * cell + (cell - knobSize) / 2
            let cy = top + CGFloat(r) * cell
            knob.frame = CGRect(x: cx, y: cy, width: knobSize, height: knobSize)
        }

        // BIAS and RATE knobs, side by side, centered below the grid.
        // Each has its label plate directly above the knob.
        let extraKnobSize = min(knobSize, 78)
        let labelHeight: CGFloat = 20
        let labelWidth: CGFloat = 48
        let pairGap: CGFloat = 40
        let pairWidth = extraKnobSize * 2 + pairGap
        let pairX = x + (width - pairWidth) / 2
        let knobsY = top + CGFloat(gridSize) * cell + 20 + labelHeight + 4

        let biasX = pairX
        biasLabel.frame = CGRect(x: biasX + (extraKnobSize - labelWidth) / 2,
                                 y: top + CGFloat(gridSize) * cell + 20,
                                 width: labelWidth, height: labelHeight)
        biasKnob.frame = CGRect(x: biasX, y: knobsY, width: extraKnobSize, height: extraKnobSize)

        let rateX = pairX + extraKnobSize + pairGap
        rateLabel.frame = CGRect(x: rateX + (extraKnobSize - labelWidth) / 2,
                                 y: top + CGFloat(gridSize) * cell + 20,
                                 width: labelWidth, height: labelHeight)
        rateKnob.frame = CGRect(x: rateX, y: knobsY, width: extraKnobSize, height: extraKnobSize)
    }

    private func layoutRightColumn(x: CGFloat, top: CGFloat, width: CGFloat, height: CGFloat) {
        var y = top
        let meterHeight = min(width * 0.72, 150)
        meter.frame = CGRect(x: x, y: y, width: width, height: meterHeight)
        y += meterHeight + 14

        outputLed.frame = CGRect(x: x + width / 2 - 30, y: y, width: 60, height: 44)
        y += 44 + 12

        let formulaHeight: CGFloat = 48 // single engraved line + padding
        formulaPlate.frame = CGRect(x: x, y: y, width: width, height: formulaHeight)
        y += formulaHeight + 12

        // Procedure plate is sized to its content, not stretched to fill the
        // column. Its 9 engraved lines (16pt each) plus top/bottom padding need
        // ~176pt; letting it grow with the window just adds empty brushed metal
        // around the vertically-centered text. Cap it at whatever space is left
        // so it never overflows the strip below on a short window.
        let procedureContentHeight: CGFloat = 176
        let remaining = height - (y - top)
        procedurePlate.frame = CGRect(x: x, y: y, width: width,
                                      height: min(procedureContentHeight, max(remaining, 0)))
    }

    private func layoutButtonStrip(x: CGFloat, y: CGFloat, width: CGFloat, height: CGFloat) {
        let gap: CGFloat = 16
        let buttonWidth = (width - gap * 2) / 3
        learnPlusButton.frame = CGRect(x: x, y: y, width: buttonWidth, height: height)
        resetButton.frame = CGRect(x: x + buttonWidth + gap, y: y, width: buttonWidth, height: height)
        learnMinusButton.frame = CGRect(x: x + (buttonWidth + gap) * 2, y: y, width: buttonWidth, height: height)
    }

    // MARK: - Behavior

    private func updateOutput() {
        guard switches.count == nodeCount, engine.weights.count == nodeCount else { return }
        let inputs = switches.map { $0.value }
        let output = engine.calculateOutput(inputs)
        meter.value = output.clamped(to: -100...100)
        outputLed.isOn = output > 0
    }

    private func learn(desiredPositive: Bool) {
        let inputs = switches.map { $0.value }
        engine.learn(inputs, desiredPositive: desiredPositive)
        // Reflect the new weights on the knobs.
        for (i, knob) in knobs.enumerated() where i < engine.weights.count {
            knob.value = engine.weights[i]
        }
        biasKnob.value = engine.bias
        updateOutput()
    }

    private func resetAll() {
        switches.forEach { $0.isOn = false }
        knobs.forEach { $0.value = 0 }
        biasKnob.value = 0
        engine.resetWeights()
        updateOutput()
    }

    private func toggleAllSwitches() {
        // If any switch is off, turn all on; otherwise turn all off.
        let turnOn = switches.contains { !$0.isOn }
        switches.forEach { $0.isOn = turnOn }
        updateOutput()
    }

    @objc private func handleCenterSwipe(_ gesture: UISwipeGestureRecognizer) {
        switch gesture.direction {
        case .up:    shiftPattern(dx: 0, dy: -1)
        case .down:  shiftPattern(dx: 0, dy: 1)
        case .left:  shiftPattern(dx: -1, dy: 0)
        case .right: shiftPattern(dx: 1, dy: 0)
        default:     break
        }
    }

    private func shiftPattern(dx: Int, dy: Int) {
        var grid = Array(repeating: Array(repeating: false, count: gridSize), count: gridSize)
        for (i, sw) in switches.enumerated() {
            grid[i / gridSize][i % gridSize] = sw.isOn
        }
        var newGrid = Array(repeating: Array(repeating: false, count: gridSize), count: gridSize)
        for r in 0..<gridSize {
            for c in 0..<gridSize {
                let sr = r - dy, sc = c - dx
                if sr >= 0, sr < gridSize, sc >= 0, sc < gridSize {
                    newGrid[r][c] = grid[sr][sc]
                }
            }
        }
        for (i, sw) in switches.enumerated() {
            sw.isOn = newGrid[i / gridSize][i % gridSize]
        }
        updateOutput()
    }
}
