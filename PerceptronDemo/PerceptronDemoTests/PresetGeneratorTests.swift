//
//  PresetGeneratorTests.swift
//  PerceptronDemoTests
//
//  Generates the bundled pre-trained example networks ("presets") that the
//  gear menu's "Load Example" submenu offers, AND proves each one is correctly
//  trained. Running these tests:
//
//    1. Trains a fresh PerceptronEngine on a small, linearly-separable set of
//       patterns for each example (top-vs-bottom, left-vs-right, etc.).
//    2. Asserts the trained network classifies EVERY training pattern the way
//       it should — so a preset that can't be learned fails the build instead of
//       shipping a broken file.
//    3. Writes the trained state to `PerceptronDemo/Presets/<Name>.pcn` in the
//       source tree (the path is derived from this file's own location, so it
//       hits the real repo regardless of where DerivedData lives).
//
//  The `.pcn` files are checked in and bundled in the app. Re-running the tests
//  regenerates them from scratch — they are provably-trained fixtures, not
//  hand-tuned magic numbers.
//
//  The single-layer 1958 perceptron can only learn linearly-separable
//  problems, so every example here is linearly separable by construction
//  (no XOR-style patterns).
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

        let snapshot = PerceptronSnapshot(
            gridSize: gridSize,
            weights: engine.weights,
            bias: engine.bias,
            learningRate: engine.learningRate,
            switchStates: displayStates)

        try write(snapshot, for: preset)
    }

    // MARK: - Writing to the source tree

    /// The `PerceptronDemo/PerceptronDemo/Presets` directory in the source tree,
    /// derived from this test file's own path:
    ///   .../PerceptronDemo/PerceptronDemoTests/PresetGeneratorTests.swift
    ///   -> .../PerceptronDemo/PerceptronDemo/Presets
    private static var presetsDirectory: URL {
        URL(fileURLWithPath: #filePath)            // .../PresetGeneratorTests.swift
            .deletingLastPathComponent()            // .../PerceptronDemoTests
            .deletingLastPathComponent()            // .../PerceptronDemo (project dir)
            .appendingPathComponent("PerceptronDemo")   // app sources
            .appendingPathComponent("Presets")
    }

    private static func write(_ snapshot: PerceptronSnapshot, for preset: Preset) throws {
        let dir = presetsDirectory
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        let url = dir.appendingPathComponent("\(preset.rawValue).\(PerceptronSnapshot.fileExtension)")
        try snapshot.jsonData().write(to: url, options: .atomic)
    }

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
}
