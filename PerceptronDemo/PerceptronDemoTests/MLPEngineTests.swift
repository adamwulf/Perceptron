//
//  MLPEngineTests.swift
//  PerceptronDemoTests
//
//  Pins the multi-layer (1986 backprop) engine's forward pass and learning
//  gate to hand-derived values, then proves it can learn a translation-
//  invariant "T anywhere" — the thing a single-layer perceptron provably
//  cannot do.
//

import Testing
import Foundation
@testable import PerceptronDemo

struct MLPEngineTests {

    private let tol = 1e-9

    // MARK: - Forward pass

    @Test func forwardPassAppliesReLUThenOutputLayer() {
        let mlp = MLPEngine(inputCount: 2, hiddenCount: 2)
        // Overwrite the seeded weights with known values.
        mlp.load(
            hiddenWeights: [[1.0, 0.0], [-1.0, 2.0]],
            hiddenBiases: [0.0, 0.0],
            outputWeights: [1.0, 1.0],
            bias: 0.0)
        // inputs = [+1, -1]
        // h0 = ReLU(1·1 + 0·(-1)) = ReLU(1) = 1
        // h1 = ReLU(-1·1 + 2·(-1)) = ReLU(-3) = 0
        // out = 1·1 + 1·0 + 0 = 1
        #expect(abs(mlp.calculateOutput([1, -1]) - 1.0) < tol)
        #expect(abs(mlp.hiddenActivations[0] - 1.0) < tol)
        #expect(abs(mlp.hiddenActivations[1] - 0.0) < tol)
    }

    @Test func reluClampsNegativePreactivationsToZero() {
        let mlp = MLPEngine(inputCount: 1, hiddenCount: 1)
        mlp.load(hiddenWeights: [[-5.0]], hiddenBiases: [0.0], outputWeights: [1.0], bias: 0.0)
        // input +1 => z = -5 => ReLU 0 => output = bias 0
        #expect(abs(mlp.calculateOutput([1]) - 0.0) < tol)
        #expect(mlp.hiddenActivations[0] == 0.0)
    }

    // MARK: - Learning gate

    @Test func learnIsNoOpWhenAlreadyCorrectSign() {
        let mlp = MLPEngine(inputCount: 1, hiddenCount: 1)
        mlp.load(hiddenWeights: [[5.0]], hiddenBiases: [0.0], outputWeights: [1.0], bias: 0.0)
        // input +1 => h = 5, output = 5 > 0; want positive => already correct.
        #expect(mlp.learn([1], desiredPositive: true) == false)
    }

    @Test func learnAdjustsWhenWrong() {
        let mlp = MLPEngine(inputCount: 1, hiddenCount: 1)
        mlp.load(hiddenWeights: [[5.0]], hiddenBiases: [0.0], outputWeights: [1.0], bias: 0.0)
        // input +1 => output 5 > 0, but we want negative => wrong => adjusts.
        #expect(mlp.learn([1], desiredPositive: false) == true)
    }

    // MARK: - The headline capability: learn a moving T

    @Test func learnsTranslationInvariantT() {
        let data = TPatternData(gridSize: 4)
        let mlp = MLPEngine(inputCount: data.nodeCount, hiddenCount: 6)
        let converged = TPatternData.train(mlp, samples: data.samples, maxEpochs: 50_000)
        #expect(converged, "MLP with 6 hidden nodes should learn the moving-T within the epoch cap")
        #expect(TPatternData.classifiesAll(mlp, data.samples),
                "Trained MLP must classify every T position (and every non-T) correctly")
    }
}
