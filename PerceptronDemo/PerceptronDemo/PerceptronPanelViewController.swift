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

        // Right column.
        view.addSubview(meter)
        outputLed.label = "OUTPUT"
        view.addSubview(outputLed)

        voltageLabel.text = "Off=-1v  On=+1v"
        view.addSubview(voltageLabel)

        biasLabel.text = "BIAS"
        view.addSubview(biasLabel)

        formulaPlate.lines = ["OUTPUT =", "  SUM(Switch x Weight) + Bias"]
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
        let buttonStripHeight: CGFloat = 92 // taller: holds the RATE knob + its value text
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

        // D-pad below the switch grid.
        let dpadCenterY = gridTop + CGFloat(gridSize) * (cell * 1.15) + 60
        let dpadCenterX = x + width / 2
        centerToggle.frame = CGRect(x: dpadCenterX - 23, y: dpadCenterY - 23, width: 46, height: 46)
        arrowUp.frame = CGRect(x: dpadCenterX - 20, y: dpadCenterY - 23 - 20, width: 40, height: 16)
        arrowDown.frame = CGRect(x: dpadCenterX - 20, y: dpadCenterY + 23 + 4, width: 40, height: 16)
        arrowLeft.frame = CGRect(x: dpadCenterX - 23 - 20, y: dpadCenterY - 20, width: 16, height: 40)
        arrowRight.frame = CGRect(x: dpadCenterX + 23 + 4, y: dpadCenterY - 20, width: 16, height: 40)
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

        // Bias knob centered below the grid.
        let biasY = top + CGFloat(gridSize) * cell + 20
        let biasX = x + width / 2 - knobSize / 2
        biasLabel.frame = CGRect(x: biasX - 54, y: biasY + knobSize / 2 - 12, width: 48, height: 22)
        biasKnob.frame = CGRect(x: biasX, y: biasY, width: knobSize, height: knobSize)
    }

    private func layoutRightColumn(x: CGFloat, top: CGFloat, width: CGFloat, height: CGFloat) {
        var y = top
        let meterHeight = min(width * 0.72, 150)
        meter.frame = CGRect(x: x, y: y, width: width, height: meterHeight)
        y += meterHeight + 14

        outputLed.frame = CGRect(x: x + width / 2 - 30, y: y, width: 60, height: 44)
        y += 44 + 12

        let formulaHeight: CGFloat = 44
        formulaPlate.frame = CGRect(x: x, y: y, width: width, height: formulaHeight)
        y += formulaHeight + 12

        let procedureHeight = min(height - (y - top), 170)
        procedurePlate.frame = CGRect(x: x, y: y, width: width, height: max(procedureHeight, 150))
    }

    private func layoutButtonStrip(x: CGFloat, y: CGFloat, width: CGFloat, height: CGFloat) {
        // Strip order mirrors the desktop: [Learn +] [RATE] [Learn -] [Reset].
        // The RATE column is a knob (with a small label above it); the other
        // three are push buttons vertically centered on the knob body.
        let gap: CGFloat = 16
        let rateColWidth: CGFloat = min(height + 8, 100) // knob is roughly square
        let buttonWidth = (width - rateColWidth - gap * 3) / 3
        let buttonHeight: CGFloat = 52
        let buttonY = y + (height - buttonHeight) / 2

        var cx = x
        learnPlusButton.frame = CGRect(x: cx, y: buttonY, width: buttonWidth, height: buttonHeight)
        cx += buttonWidth + gap

        // RATE: label on top, knob filling the rest of the column.
        rateLabel.frame = CGRect(x: cx + (rateColWidth - 44) / 2, y: y, width: 44, height: 18)
        rateKnob.frame = CGRect(x: cx, y: y + 16, width: rateColWidth, height: height - 16)
        cx += rateColWidth + gap

        learnMinusButton.frame = CGRect(x: cx, y: buttonY, width: buttonWidth, height: buttonHeight)
        cx += buttonWidth + gap

        resetButton.frame = CGRect(x: cx, y: buttonY, width: buttonWidth, height: buttonHeight)
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
