//
//  TPatternData.swift
//  PerceptronDemoTests
//
//  The training set for translation-invariant "T" detection, shared by the
//  MLP unit test and the preset generator. A T is:
//
//      X X X        (top bar — 3 across)
//      . X .        (stem — 2 down from the middle of the bar)
//      . X .
//
//  a 3×3 shape. On a 4×4 grid it fits at 4 positions (2 vertical × 2
//  horizontal offsets). Positives = a T at each position. Negatives = a broad
//  set of non-T patterns, deliberately including near-misses (rotated/flipped
//  Ts, bars, plus-signs) so the network learns the *shape*, not just "some
//  cells are on."
//
//  Inputs are ±1 (ON/OFF); switch states are stored as Bool.
//

import Foundation
@testable import PerceptronDemo

struct TPatternData {

    let gridSize: Int
    var nodeCount: Int { gridSize * gridSize }

    struct Sample {
        let states: [Bool]
        let positive: Bool
    }

    let samples: [Sample]
    /// A representative positive pattern (a T at the top-left) to show on the
    /// panel when the preset loads.
    let displayStates: [Bool]

    init(gridSize: Int = 4) {
        self.gridSize = gridSize
        let n = gridSize

        // Build a grid of Bools from a set of ON (row,col) cells.
        func grid(_ cells: Set<[Int]>) -> [Bool] {
            (0..<(n * n)).map { i in cells.contains([i / n, i % n]) }
        }

        // A T with its top-left at (top,left): top bar spans 3 cols, stem drops
        // 2 rows down the middle column.
        func tShape(top: Int, left: Int) -> Set<[Int]> {
            var cells = Set<[Int]>()
            for c in left..<(left + 3) { cells.insert([top, c]) }        // top bar
            let mid = left + 1
            cells.insert([top + 1, mid])                                  // stem
            cells.insert([top + 2, mid])
            return cells
        }

        // Positives: T at every valid position on the grid.
        var positives: [Set<[Int]>] = []
        for top in 0...(n - 3) {
            for left in 0...(n - 3) {
                positives.append(tShape(top: top, left: left))
            }
        }

        // Negatives: near-misses and unrelated shapes at various positions.
        var negatives: [Set<[Int]>] = []

        // Rotated / flipped "T"s (still 3×3) — these must read as NOT a T.
        func tUpsideDown(top: Int, left: Int) -> Set<[Int]> {
            var cells = Set<[Int]>()
            let mid = left + 1
            cells.insert([top, mid]); cells.insert([top + 1, mid])        // stem up
            for c in left..<(left + 3) { cells.insert([top + 2, c]) }     // bottom bar
            return cells
        }
        func tLeft(top: Int, left: Int) -> Set<[Int]> {   // ⊢ rotated
            var cells = Set<[Int]>()
            for r in top..<(top + 3) { cells.insert([r, left]) }          // left bar
            let mid = top + 1
            cells.insert([mid, left + 1]); cells.insert([mid, left + 2])  // stem right
            return cells
        }
        func plus(top: Int, left: Int) -> Set<[Int]> {
            var cells = Set<[Int]>()
            let mr = top + 1, mc = left + 1
            cells.insert([top, mc]); cells.insert([top + 2, mc])
            cells.insert([mr, left]); cells.insert([mr, left + 2]); cells.insert([mr, mc])
            return cells
        }
        for top in 0...(n - 3) {
            for left in 0...(n - 3) {
                negatives.append(tUpsideDown(top: top, left: left))
                negatives.append(tLeft(top: top, left: left))
                negatives.append(plus(top: top, left: left))
            }
        }

        // Simple structural negatives.
        negatives.append([])                                             // all off
        negatives.append(Set((0..<n).flatMap { r in (0..<n).map { c in [r, c] } })) // all on
        for r in 0..<n { negatives.append(Set((0..<n).map { [r, $0] })) } // each full row
        for c in 0..<n { negatives.append(Set((0..<n).map { [$0, c] })) } // each full column
        // Diagonals.
        negatives.append(Set((0..<n).map { [$0, $0] }))
        negatives.append(Set((0..<n).map { [$0, n - 1 - $0] }))

        self.samples =
            positives.map { Sample(states: grid($0), positive: true) } +
            negatives.map { Sample(states: grid($0), positive: false) }
        self.displayStates = grid(tShape(top: 0, left: 0))
    }

    // MARK: - Training / evaluation

    /// A comfortable decision margin. Requiring |output| ≥ margin (not just the
    /// right sign) stops training from settling on a fragile boundary and, in
    /// practice, prevents the "everything rails to the ±30 clamp" degenerate
    /// solution — the net has to find a *distributed* answer, not a saturated one.
    static let margin = 1.0

    /// Trains with backprop until every sample clears the margin, or the epoch
    /// cap is hit. A deliberately gentle learning rate keeps weights away from
    /// the clamps so the hidden units stay distinct instead of collapsing.
    static func train(_ mlp: MLPEngine, samples: [Sample], maxEpochs: Int) -> Bool {
        mlp.learningRate = 0.5   // effective step = 0.5 × 0.01 = 0.005
        for epoch in 0..<maxEpochs {
            // Rotate the presentation order each epoch so no single sample
            // dominates (avoids Math.random, which is unavailable / non-repro).
            let offset = epoch % samples.count
            for k in 0..<samples.count {
                let sample = samples[(k + offset) % samples.count]
                let inputs = sample.states.map { $0 ? 1 : -1 }
                mlp.trainStep(inputs, desiredPositive: sample.positive, margin: margin)
            }
            if classifiesAll(mlp, samples) { return true }
        }
        return classifiesAll(mlp, samples)
    }

    /// Every sample must clear the margin with the correct sign.
    static func classifiesAll(_ mlp: MLPEngine, _ samples: [Sample]) -> Bool {
        samples.allSatisfy { sample in
            let inputs = sample.states.map { $0 ? 1 : -1 }
            let out = mlp.calculateOutput(inputs)
            return sample.positive ? out >= margin : out <= -margin
        }
    }
}
