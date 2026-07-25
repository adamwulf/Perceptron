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
    private let networkView = NetworkView()
    private let meter = AnalogMeterControl()
    private let outputLed = OutputLedControl()
    private let titleLabel = MetalLabelView()

    private let learnPlusButton = PushButton()
    private let learnMinusButton = PushButton()
    private let resetButton = PushButton()
    private let closeButton = UIButton(type: .system)

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

        var config = UIButton.Configuration.plain()
        config.image = UIImage(systemName: "xmark.circle.fill",
                               withConfiguration: UIImage.SymbolConfiguration(pointSize: 24))
        config.baseForegroundColor = UIColor(white: 150/255, alpha: 1)
        closeButton.configuration = config
        closeButton.accessibilityLabel = "Close"
        closeButton.addAction(UIAction { [weak self] _ in self?.dismiss(animated: true) },
                              for: .touchUpInside)
        closeButton.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(closeButton)

        NSLayoutConstraint.activate([
            closeButton.trailingAnchor.constraint(equalTo: view.safeAreaLayoutGuide.trailingAnchor, constant: -8),
            closeButton.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 4),
        ])
    }

    // MARK: - Layout

    private func layoutPanel() {
        let safe = view.safeAreaLayoutGuide.layoutFrame
        let content = safe.insetBy(dx: 20, dy: 20)

        titleLabel.frame = CGRect(x: content.minX, y: content.minY, width: 260, height: 22)

        let buttonStrip: CGFloat = 56
        let bodyTop = content.minY + 34
        let bodyHeight = content.height - 34 - buttonStrip - 16

        // Left column: 4×4 switch grid.
        let switchColWidth = min(content.width * 0.24, 220)
        let cell = min(switchColWidth / CGFloat(gridSize), 52)
        let gridWidth = cell * CGFloat(gridSize)
        let gridX = content.minX + (switchColWidth - gridWidth) / 2
        let swSize = min(cell - 6, 42)
        let gridHeight = cell * 1.2 * CGFloat(gridSize)
        let gridTop = bodyTop + max(0, (bodyHeight - gridHeight) / 2)
        for (i, sw) in switches.enumerated() {
            let r = i / gridSize, c = i % gridSize
            let x = gridX + CGFloat(c) * cell + (cell - swSize) / 2
            let y = gridTop + CGFloat(r) * (cell * 1.2)
            sw.frame = CGRect(x: x, y: y, width: swSize, height: swSize * 1.35)
        }

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

        // Bottom: LEARN + / RESET / LEARN − strip.
        let gap: CGFloat = 16
        let bw = (content.width - gap * 2) / 3
        let by = content.maxY - buttonStrip
        learnPlusButton.frame = CGRect(x: content.minX, y: by, width: bw, height: buttonStrip)
        resetButton.frame = CGRect(x: content.minX + bw + gap, y: by, width: bw, height: buttonStrip)
        learnMinusButton.frame = CGRect(x: content.minX + (bw + gap) * 2, y: by, width: bw, height: buttonStrip)
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
