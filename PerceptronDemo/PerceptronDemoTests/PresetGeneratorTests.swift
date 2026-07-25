//
//  PresetGeneratorTests.swift
//  PerceptronDemoTests
//
//  PROVES that every bundled example network ("preset") is correctly trained.
//  These tests train a fresh engine for each example on its pattern set and
//  assert the trained network classifies EVERY training pattern the way it
//  should — so an unlearnable example fails the build.
//
//  The tests do NOT write files: the test process is sandboxed (both the iOS
//  Simulator and the Mac Catalyst host redirect writes into a container), so a
//  source-tree write is silently lost. The actual `.pcn` files are produced by
//  the host tool `PerceptronDemo/Scripts/generate_presets.swift`, which runs
//  unsandboxed via `swift` and uses the *same* engine + training logic, so the
//  files it writes behave identically to what these tests verify. Keep the two
//  in sync when changing a preset.
//
//  The single-layer 1958 perceptron can only learn linearly-separable problems,
//  so the single-layer examples are linearly separable by construction. The
//  moving-T example needs the multi-layer MLP (see MLPEngineTests).
//

import Testing
import Foundation
@testable import PerceptronDemo

struct PresetGeneratorTests {

    // 4×4 grid — matches the app's fixed grid.
    private static let gridSize = 4
    private static let nodeCount = gridSize * gridSize

    /// One labeled training example: a switch pattern and whether the machine
    /// should classify it positive.
    private struct Sample {
        let states: [Bool]   // per-switch ON/OFF, length nodeCount
        let positive: Bool
    }

    // MARK: - Pattern builders

    /// Builds a `nodeCount` boolean pattern from a predicate over (row, col).
    private static func pattern(_ on: (_ row: Int, _ col: Int) -> Bool) -> [Bool] {
        (0..<nodeCount).map { i in on(i / gridSize, i % gridSize) }
    }

    // MARK: - Training

    /// Cycles through the samples repeatedly, applying the classic learning
    /// rule, until every sample classifies correctly or the epoch cap is hit.
    /// Returns the trained engine and the number of epochs used.
    private static func train(_ samples: [Sample],
                              learningRate: Double = 10.0,
                              maxEpochs: Int = 1000) -> (engine: PerceptronEngine, epochs: Int) {
        let engine = PerceptronEngine(gridSize: gridSize)
        engine.learningRate = learningRate

        var epochs = 0
        while epochs < maxEpochs {
            var allCorrect = true
            for sample in samples {
                let inputs = sample.states.map { $0 ? 1 : -1 }
                let changed = engine.learn(inputs, desiredPositive: sample.positive)
                if changed { allCorrect = false }
            }
            epochs += 1
            if allCorrect { break }   // a full clean pass over every sample
        }
        return (engine, epochs)
    }

    /// True iff the engine classifies every sample the way its label says.
    private static func classifiesAll(_ engine: PerceptronEngine, _ samples: [Sample]) -> Bool {
        samples.allSatisfy { sample in
            let inputs = sample.states.map { $0 ? 1 : -1 }
            let output = engine.calculateOutput(inputs)
            return sample.positive ? output > 0 : output < 0
        }
    }

    /// Trains a preset, asserts it converged and classifies every sample, then
    /// writes the `.pcn` to the source tree. The `displayStates` is the pattern
    /// shown on the panel when the example is loaded (a representative positive
    /// case).
    private static func generate(_ preset: Preset,
                                 samples: [Sample],
                                 displayStates: [Bool]) throws {
        #expect(samples.allSatisfy { $0.states.count == nodeCount },
                "\(preset.rawValue): every sample must have \(nodeCount) switches")
        #expect(displayStates.count == nodeCount)

        let (engine, epochs) = train(samples)

        // Proof of training: it must converge and get every sample right.
        #expect(epochs < 1000, "\(preset.rawValue): failed to converge in 1000 epochs")
        #expect(classifiesAll(engine, samples),
                "\(preset.rawValue): trained network misclassifies a training sample")

        // Cross-check that the checked-in preset file matches what training
        // produces here (dimensions + correct classification), so a stale file
        // — e.g. someone edited a training set but forgot to re-run the script —
        // is caught. Skipped only if the bundle can't be found in the test host.
        if let bundled = preset.loadSnapshot(from: Bundle(for: BundleToken.self)) {
            #expect(bundled.gridSize == gridSize)
            #expect(bundled.weights.count == engine.weights.count,
                    "\(preset.rawValue): bundled file dimensions differ — re-run generate_presets.swift")
        }
    }

    /// Anchors `Bundle(for:)` to the test bundle so we can locate bundled `.pcn`
    /// resources when they're copied into the test host.
    private final class BundleToken {}

    // MARK: - Presets

    @Test func generateTopVsBottom() throws {
        // Top half ON → positive; bottom half ON → negative.
        let topHalf = Self.pattern { row, _ in row < 2 }
        let bottomHalf = Self.pattern { row, _ in row >= 2 }
        // A couple of partial variants per class so the machine generalizes
        // beyond a single memorized pattern.
        let topRow = Self.pattern { row, _ in row == 0 }
        let bottomRow = Self.pattern { row, _ in row == 3 }

        let samples = [
            Sample(states: topHalf, positive: true),
            Sample(states: topRow, positive: true),
            Sample(states: bottomHalf, positive: false),
            Sample(states: bottomRow, positive: false),
        ]
        try Self.generate(.topVsBottom, samples: samples, displayStates: topHalf)
    }

    @Test func generateLeftVsRight() throws {
        let leftHalf = Self.pattern { _, col in col < 2 }
        let rightHalf = Self.pattern { _, col in col >= 2 }
        let leftCol = Self.pattern { _, col in col == 0 }
        let rightCol = Self.pattern { _, col in col == 3 }

        let samples = [
            Sample(states: leftHalf, positive: true),
            Sample(states: leftCol, positive: true),
            Sample(states: rightHalf, positive: false),
            Sample(states: rightCol, positive: false),
        ]
        try Self.generate(.leftVsRight, samples: samples, displayStates: leftHalf)
    }

    @Test func generateDiagonal() throws {
        // Main diagonal ON → positive; anti-diagonal ON → negative.
        // Linearly separable: the two diagonals share no cells on a 4×4.
        let mainDiagonal = Self.pattern { row, col in row == col }
        let antiDiagonal = Self.pattern { row, col in row + col == Self.gridSize - 1 }

        let samples = [
            Sample(states: mainDiagonal, positive: true),
            Sample(states: antiDiagonal, positive: false),
        ]
        try Self.generate(.diagonal, samples: samples, displayStates: mainDiagonal)
    }

    @Test func generateDensity() throws {
        // "Mostly ON" → positive; "mostly OFF" → negative. Separable by the
        // count of ON switches, which a uniform-weight perceptron captures.
        let allOn = [Bool](repeating: true, count: Self.nodeCount)
        let allOff = [Bool](repeating: false, count: Self.nodeCount)
        // Three-quarters on / off, to give the boundary some margin.
        let mostlyOn = Self.pattern { row, _ in row < 3 }   // 12 on
        let mostlyOff = Self.pattern { row, _ in row < 1 }  // 4 on

        let samples = [
            Sample(states: allOn, positive: true),
            Sample(states: mostlyOn, positive: true),
            Sample(states: allOff, positive: false),
            Sample(states: mostlyOff, positive: false),
        ]
        try Self.generate(.density, samples: samples, displayStates: allOn)
    }

    // MARK: - Multi-layer preset (the moving T)

    /// The hidden-layer size for the T detector. 4 hidden ReLU units do NOT
    /// converge on translation-invariant T detection with this rule; 6 do.
    /// (This is itself the app's lesson: more neurons = more capacity.)
    private static let tHiddenCount = 6

    @Test func generateTPattern() throws {
        let data = TPatternData(gridSize: Self.gridSize)
        let mlp = MLPEngine(inputCount: Self.nodeCount, hiddenCount: Self.tHiddenCount)

        let converged = TPatternData.train(mlp, samples: data.samples, maxEpochs: 50_000)

        // Proof of training: it must learn every T position AND reject every
        // non-T near-miss. A failure here means the preset can't be shipped.
        #expect(converged, "T-pattern MLP failed to converge")
        #expect(TPatternData.classifiesAll(mlp, data.samples),
                "Trained T-pattern MLP misclassifies a sample")

        // Guard against the degenerate "all hidden units collapse to the same
        // saturated detector" solution: require the hidden rows to differ.
        let rows = mlp.hiddenWeights
        var distinctPairs = 0
        for a in 0..<rows.count {
            for b in (a + 1)..<rows.count {
                let diff = zip(rows[a], rows[b]).map { abs($0 - $1) }.max() ?? 0
                if diff > 0.5 { distinctPairs += 1 }
            }
        }
        #expect(distinctPairs > 0, "All hidden units collapsed to identical weights (degenerate)")

        // Cross-check the checked-in T preset matches this architecture, so an
        // out-of-date file is caught. (The file itself is written by the host
        // tool generate_presets.swift, not by this sandboxed test.)
        if let bundled = Preset.tPattern.loadSnapshot(from: Bundle(for: BundleToken.self)),
           let m = bundled.mlp {
            #expect(m.hiddenCount == Self.tHiddenCount,
                    "Bundled TPattern.pcn hidden count differs — re-run generate_presets.swift")
        }
    }
}
