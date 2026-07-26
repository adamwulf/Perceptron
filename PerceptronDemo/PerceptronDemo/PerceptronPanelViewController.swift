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
import UniformTypeIdentifiers

final class PerceptronPanelViewController: UIViewController {

    // MARK: - Model

    private let gridSize = 4
    private lazy var engine = PerceptronEngine(gridSize: gridSize)
    private var nodeCount: Int { gridSize * gridSize }

    // MARK: - Controls

    private var switches: [SwitchControl] = []
    private var knobs: [KnobControl] = []
    private let biasKnob = KnobControl()

    private let dpad = DPadControl()

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

    // Gear button (bottom strip) opens a Save / Load / About menu.
    private let gearButton = PushButton()

    // Held strongly — `transitioningDelegate` is a weak reference.
    private let slideTransition = SlideTransitionDelegate()

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

        // D-pad — shifts the switch pattern; its center toggles all/none.
        dpad.onShift = { [weak self] dx, dy in self?.shiftPattern(dx: dx, dy: dy) }
        dpad.onToggleAll = { [weak self] in self?.toggleAllSwitches() }
        view.addSubview(dpad)

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

        buildGearButton()
    }

    /// A round backlit gear button at the right end of the bottom Learn/Reset
    /// strip — the same mechanical treatment as its neighbors. A single tap
    /// pops the Save / Load / Examples / Signal Flow / About menu (the menu is
    /// the button's primary action, so there's no long press on Mac or iPad).
    private func buildGearButton() {
        gearButton.symbolName = "gearshape.fill"
        gearButton.glowColor = UIColor(red: 255/255, green: 220/255, blue: 80/255, alpha: 1)
        gearButton.accessibilityLabel = "Settings"
        gearButton.menu = makeGearMenu()
        view.addSubview(gearButton)
    }

    private func makeGearMenu() -> UIMenu {
        let save = UIAction(title: "Save…",
                            image: UIImage(systemName: "square.and.arrow.down")) { [weak self] _ in
            self?.presentSave()
        }
        let load = UIAction(title: "Load…",
                            image: UIImage(systemName: "square.and.arrow.up")) { [weak self] _ in
            self?.presentLoad()
        }
        let about = UIAction(title: "About",
                             image: UIImage(systemName: "info.circle")) { [weak self] _ in
            self?.presentAbout()
        }

        // "Load Example ▸" — a submenu of bundled, pre-trained networks. Each
        // item loads its `.pcn` directly (no picker).
        let exampleActions = Preset.singleLayerCases.map { preset in
            UIAction(title: preset.menuTitle,
                     image: UIImage(systemName: preset.symbolName)) { [weak self] _ in
                self?.loadPreset(preset)
            }
        }
        let examples = UIMenu(title: "Load Example",
                              image: UIImage(systemName: "square.stack.3d.up"),
                              children: exampleActions)

        // The second screen: the multi-layer "signal flow" patch panel, which
        // can detect a shape anywhere in the grid (the single-layer panel can't).
        let signalFlow = UIAction(title: "Signal Flow (1986)",
                                  image: UIImage(systemName: "point.3.filled.connected.trianglepath.dotted")) { [weak self] _ in
            self?.presentSignalFlow()
        }

        return UIMenu(children: [signalFlow, examples, save, load, about])
    }

    /// Slides the Signal Flow screen in from the right (this panel slides out to
    /// the left) — as if panning your gaze across the bench to the next machine.
    private func presentSignalFlow() {
        let vc = SignalFlowViewController()
        vc.modalPresentationStyle = .fullScreen
        vc.transitioningDelegate = slideTransition
        present(vc, animated: true)
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
        let metrics = SwitchGrid.Metrics(columnWidth: width, gridSize: gridSize)
        let gridX = x + (width - metrics.width) / 2
        SwitchGrid.layout(switches, metrics: metrics, x: gridX, y: gridTop)

        // Joystick below the switch grid.
        let dpadCenterY = gridTop + metrics.rowPitch * CGFloat(gridSize) + 60
        let dpadSize = DPadControl.preferredSize
        dpad.frame = CGRect(x: x + (width - dpadSize.width) / 2,
                            y: dpadCenterY - dpadSize.height / 2,
                            width: dpadSize.width, height: dpadSize.height)
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
        // The gear sits at the right end as a circle (width == height), the
        // three wide buttons split what's left.
        let gearSize = height
        let buttonWidth = (width - gap * 3 - gearSize) / 3
        learnPlusButton.frame = CGRect(x: x, y: y, width: buttonWidth, height: height)
        resetButton.frame = CGRect(x: x + buttonWidth + gap, y: y, width: buttonWidth, height: height)
        learnMinusButton.frame = CGRect(x: x + (buttonWidth + gap) * 2, y: y, width: buttonWidth, height: height)
        gearButton.frame = CGRect(x: x + width - gearSize, y: y, width: gearSize, height: gearSize)
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
        SwitchGrid.toggleAll(switches)
        updateOutput()
    }

    private func shiftPattern(dx: Int, dy: Int) {
        SwitchGrid.shift(switches, gridSize: gridSize, dx: dx, dy: dy)
        updateOutput()
    }

    // MARK: - Save / Load / About

    // `PerceptronSnapshot` and the file extension live in PerceptronSnapshot.swift
    // so the loader, the Save/Load flow, and the preset-generator test all share
    // one definition.

    private func currentSnapshot() -> PerceptronSnapshot {
        PerceptronSnapshot(
            gridSize: gridSize,
            weights: engine.weights,
            bias: engine.bias,
            learningRate: engine.learningRate,
            switchStates: switches.map { $0.isOn })
    }

    /// Serializes the current state to a temp `.pcn` file and presents the
    /// system export picker so the user can save it anywhere (Files, iCloud,
    /// etc.). Works on iPhone, iPad, and Mac Catalyst.
    private func presentSave() {
        let snapshot = currentSnapshot()
        do {
            let data = try snapshot.jsonData()

            let filename = "perceptron_settings.\(PerceptronSnapshot.fileExtension)"
            let url = FileManager.default.temporaryDirectory.appendingPathComponent(filename)
            try data.write(to: url, options: .atomic)

            let picker = UIDocumentPickerViewController(forExporting: [url], asCopy: true)
            picker.delegate = self
            present(picker, animated: true)
        } catch {
            presentError("Could not save settings.", error)
        }
    }

    /// Presents the system import picker; the chosen file is decoded in
    /// `documentPicker(_:didPickDocumentsAt:)`.
    private func presentLoad() {
        // A `.pcn` file is JSON with a custom extension. We deliberately don't
        // register a UTI (keeps Info.plist untouched), so `.pcn` has no declared
        // type and `UTType(filenameExtension:)` returns nil. Accepting `.json`
        // *and* `.data` means both plain `.json` files and our unregistered
        // `.pcn` files are selectable in the picker. The actual content is
        // validated on decode.
        var types: [UTType] = [.json, .data]
        if let pcn = UTType(filenameExtension: PerceptronSnapshot.fileExtension) {
            types.insert(pcn, at: 0)
        }
        let picker = UIDocumentPickerViewController(forOpeningContentTypes: types)
        picker.delegate = self
        picker.allowsMultipleSelection = false
        present(picker, animated: true)
    }

    /// Presents the launch intro card as an About screen, matching how
    /// `SceneDelegate` presents it on launch.
    private func presentAbout() {
        let intro = IntroViewController()
        intro.modalPresentationStyle = .formSheet
        intro.modalTransitionStyle = .coverVertical
        intro.isModalInPresentation = true // require the BEGIN button to dismiss
        present(intro, animated: true)
    }

    /// Decodes a snapshot and pushes it into the engine and every on-screen
    /// control. This grid is a fixed 4×4, so a file saved from a different grid
    /// size is rejected rather than partially applied.
    private func applySnapshot(_ snapshot: PerceptronSnapshot) {
        guard snapshot.gridSize == gridSize else {
            presentError(
                "This file was saved for a \(snapshot.gridSize)×\(snapshot.gridSize) grid, "
                + "but this app uses a \(gridSize)×\(gridSize) grid.", nil)
            return
        }

        // Weights → engine + knobs.
        for i in 0..<nodeCount where i < snapshot.weights.count {
            engine.setWeight(i, snapshot.weights[i])
            if i < knobs.count { knobs[i].value = engine.weight(at: i) }
        }

        // Bias and rate.
        engine.bias = snapshot.bias.clamped(to: -30...30)
        biasKnob.value = engine.bias
        engine.learningRate = snapshot.learningRate
        rateKnob.value = snapshot.learningRate

        // Switch pattern.
        for (i, sw) in switches.enumerated() where i < snapshot.switchStates.count {
            sw.isOn = snapshot.switchStates[i]
        }

        updateOutput()
    }

    private func loadSnapshot(from url: URL) {
        // Files returned by the picker are security-scoped on iOS.
        let scoped = url.startAccessingSecurityScopedResource()
        defer { if scoped { url.stopAccessingSecurityScopedResource() } }

        do {
            let data = try Data(contentsOf: url)
            let snapshot = try PerceptronSnapshot.decode(from: data)
            applySnapshot(snapshot)
        } catch {
            presentError("Could not load settings — the file may be invalid.", error)
        }
    }

    /// Loads a bundled pre-trained example straight into the panel — no document
    /// picker. Backed by a `.pcn` file generated by `PresetGeneratorTests`.
    private func loadPreset(_ preset: Preset) {
        guard let snapshot = preset.loadSnapshot() else {
            presentError("Example \"\(preset.menuTitle)\" is unavailable.", nil)
            return
        }
        applySnapshot(snapshot)
    }

    private func presentError(_ message: String, _ error: Error?) {
        let detail = error.map { " (\($0.localizedDescription))" } ?? ""
        let alert = UIAlertController(
            title: "Error", message: message + detail, preferredStyle: .alert)
        alert.addAction(UIAlertAction(title: "OK", style: .default))
        present(alert, animated: true)
    }
}

// MARK: - UIDocumentPickerDelegate

extension PerceptronPanelViewController: UIDocumentPickerDelegate {
    func documentPicker(_ controller: UIDocumentPickerViewController,
                        didPickDocumentsAt urls: [URL]) {
        guard controller.documentPickerMode == .open, let url = urls.first else { return }
        loadSnapshot(from: url)
    }
}
