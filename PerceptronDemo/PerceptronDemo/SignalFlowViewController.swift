//
//  SignalFlowViewController.swift
//  PerceptronDemo
//
//  The second screen: a multi-layer network shown as an illuminated patch
//  panel. Input switches on the left drive a live signal-flow diagram
//  (NetworkView) — input bulbs → glowing weight-wires → hidden bulbs → output
//  bulb — backed by the multi-layer MLPEngine. This is where the app can show a
//  network detecting a "T anywhere in the grid," which a single-layer
//  perceptron cannot do.
//
//  Wires and neurons are display-only. Training happens through LEARN + /
//  LEARN − (and by loading the bundled, pre-trained "T Anywhere" preset).
//

import UIKit

final class SignalFlowViewController: UIViewController {

    // MARK: - Model

    private let gridSize = 4
    private var nodeCount: Int { gridSize * gridSize }
    private let hiddenCount = 6
    private lazy var engine = MLPEngine(inputCount: nodeCount, hiddenCount: hiddenCount)

    // MARK: - Controls

    private var switches: [SwitchControl] = []
    private let dpad = DPadControl()
    private let networkView = NetworkView()
    private let meter = AnalogMeterControl()
    private let outputLed = OutputLedControl()
    private let titleLabel = MetalLabelView()

    private let learnPlusButton = PushButton()
    private let learnMinusButton = PushButton()
    private let resetButton = PushButton()

    // Nameplate at the left end of the strip — walks back to the main panel.
    private let mainPanelPlate = MetalPlateButton()

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black
        buildControls()
        loadTPresetIfAvailable()
        refresh()
    }

    override func viewDidLayoutSubviews() {
        super.viewDidLayoutSubviews()
        layoutPanel()
    }

    // MARK: - Build

    private func buildControls() {
        titleLabel.text = "SIGNAL FLOW · 16 → \(hiddenCount) → 1"
        view.addSubview(titleLabel)

        for _ in 0..<nodeCount {
            let sw = SwitchControl()
            sw.onStateChanged = { [weak self] in self?.refresh() }
            switches.append(sw)
            view.addSubview(sw)
        }

        // Same joystick as the main panel: arrows shift the pattern, the center
        // button toggles all/none. Moving a shape around the grid is the point
        // of this screen — the network keeps firing wherever the T lands.
        dpad.onShift = { [weak self] dx, dy in self?.shiftPattern(dx: dx, dy: dy) }
        dpad.onToggleAll = { [weak self] in self?.toggleAllSwitches() }
        view.addSubview(dpad)

        view.addSubview(networkView)

        view.addSubview(meter)
        outputLed.label = "OUTPUT"
        view.addSubview(outputLed)

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
        resetButton.onTap = { [weak self] in self?.resetNetwork() }
        view.addSubview(resetButton)

        // Navigation plate — back to the panel on the left of the bench. The
        // slide transition reverses itself on dismiss.
        mainPanelPlate.labelText = "◀ MAIN PANEL"
        mainPanelPlate.onTap = { [weak self] in self?.dismiss(animated: true) }
        view.addSubview(mainPanelPlate)
    }

    // MARK: - Layout

    private func layoutPanel() {
        // Same margin and switch-column proportion as the main panel, so the
        // shared `SwitchGrid.Metrics` produces identically sized switches on
        // both screens.
        let safe = view.safeAreaLayoutGuide.layoutFrame
        let content = safe.insetBy(dx: 24, dy: 24)

        titleLabel.frame = CGRect(x: content.minX, y: content.minY, width: 260, height: 22)

        let buttonStrip: CGFloat = 56
        let bodyTop = content.minY + 34
        let bodyHeight = content.height - 34 - buttonStrip - 16

        // Left column: 4×4 switch grid with the joystick below it, the pair
        // centered vertically in the body.
        let switchColWidth = content.width * 0.26
        let metrics = SwitchGrid.Metrics(columnWidth: switchColWidth, gridSize: gridSize)
        let dpadSize = DPadControl.preferredSize
        let dpadDrop = metrics.rowPitch * CGFloat(gridSize) + 60  // grid top → joystick center
        let blockHeight = dpadDrop + dpadSize.height / 2
        let gridX = content.minX + (switchColWidth - metrics.width) / 2
        let gridTop = bodyTop + max(0, (bodyHeight - blockHeight) / 2)
        SwitchGrid.layout(switches, metrics: metrics, x: gridX, y: gridTop)

        dpad.frame = CGRect(x: content.minX + (switchColWidth - dpadSize.width) / 2,
                            y: gridTop + dpadDrop - dpadSize.height / 2,
                            width: dpadSize.width, height: dpadSize.height)

        // Right edge: meter + output LED, stacked.
        let rightColWidth: CGFloat = min(content.width * 0.22, 200)
        let rightX = content.maxX - rightColWidth
        let meterH = min(rightColWidth * 0.7, 130)
        meter.frame = CGRect(x: rightX, y: bodyTop, width: rightColWidth, height: meterH)
        outputLed.frame = CGRect(x: rightX + rightColWidth / 2 - 30, y: bodyTop + meterH + 10,
                                 width: 60, height: 44)

        // Middle: the network diagram fills the space between switches and meter.
        let netX = content.minX + switchColWidth + 8
        let netWidth = rightX - 8 - netX
        networkView.frame = CGRect(x: netX, y: bodyTop, width: max(netWidth, 0), height: bodyHeight)

        // Bottom: ◀ MAIN PANEL / LEARN + / RESET / LEARN − strip. The navigation
        // plate is at the far left because that's the direction it takes you —
        // mirroring the main panel, where it sits at the far right.
        let gap: CGFloat = 16
        let plateWidth = min(180, content.width * 0.2)
        let bw = (content.width - gap * 3 - plateWidth) / 3
        let by = content.maxY - buttonStrip
        mainPanelPlate.frame = CGRect(x: content.minX, y: by, width: plateWidth, height: buttonStrip)
        let buttonsX = content.minX + plateWidth + gap
        learnPlusButton.frame = CGRect(x: buttonsX, y: by, width: bw, height: buttonStrip)
        resetButton.frame = CGRect(x: buttonsX + bw + gap, y: by, width: bw, height: buttonStrip)
        learnMinusButton.frame = CGRect(x: buttonsX + (bw + gap) * 2, y: by, width: bw, height: buttonStrip)
    }

    // MARK: - Behavior

    private func refresh() {
        let inputs = switches.map { $0.value }
        let output = engine.calculateOutput(inputs)
        meter.value = output.clamped(to: -100...100)
        outputLed.isOn = output > 0
        networkView.update(
            inputActivations: inputs.map { $0 > 0 ? 1.0 : 0.0 },
            hiddenActivations: engine.hiddenActivations,
            hiddenWeights: engine.hiddenWeights,
            outputWeights: engine.outputWeights,
            output: output)
    }

    private func learn(desiredPositive: Bool) {
        let inputs = switches.map { $0.value }
        engine.learn(inputs, desiredPositive: desiredPositive)
        refresh()
    }

    private func resetNetwork() {
        engine.reset()
        refresh()
    }

    private func shiftPattern(dx: Int, dy: Int) {
        SwitchGrid.shift(switches, gridSize: gridSize, dx: dx, dy: dy)
        refresh()
    }

    private func toggleAllSwitches() {
        SwitchGrid.toggleAll(switches)
        refresh()
    }

    /// Loads the bundled, pre-trained "T Anywhere" MLP if it's present so the
    /// screen opens on a working detector rather than a random net.
    private func loadTPresetIfAvailable() {
        guard let snapshot = Preset.tPattern.loadSnapshot(),
              let mlp = snapshot.mlp,
              mlp.hiddenCount == hiddenCount else { return }
        engine.load(hiddenWeights: mlp.hiddenWeights,
                    hiddenBiases: mlp.hiddenBiases,
                    outputWeights: mlp.outputWeights,
                    bias: snapshot.bias)
        engine.learningRate = snapshot.learningRate
        // Show the preset's display pattern (a T at top-left).
        for (i, sw) in switches.enumerated() where i < snapshot.switchStates.count {
            sw.isOn = snapshot.switchStates[i]
        }
    }
}
