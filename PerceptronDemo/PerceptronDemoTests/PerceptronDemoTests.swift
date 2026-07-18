//
//  PerceptronDemoTests.swift
//  PerceptronDemoTests
//
//  These tests pin the Swift PerceptronEngine to the behavior of the
//  original C# PerceptronEngine.cs (the desktop reference implementation).
//  The C# project ships no tests, so every expected value below is derived
//  by hand from the C# formulas for the classic 1958 rule:
//
//    calculateOutput:  OUTPUT = Σ(input[i] × weight[i]) + bias
//    learn (classic):  update only when currently wrong;
//                      weights[i] += ±learningRate × input[i], clamp ±30;
//                      bias       += ±learningRate,            clamp ±30.
//
//  Switch inputs are +1 (ON) or -1 (OFF), never 0.
//

import Testing
@testable import PerceptronDemo

struct PerceptronDemoTests {

    private let tol = 1e-9

    // MARK: - calculateOutput

    @Test func outputIsBiasWhenWeightsAreZero() {
        let engine = PerceptronEngine(gridSize: 2) // 4 weights, all 0
        engine.bias = 3.5
        // Σ(input × 0) + 3.5 = 3.5 for any inputs.
        #expect(abs(engine.calculateOutput([1, -1, 1, -1]) - 3.5) < tol)
    }

    @Test func outputSumsWeightedInputsPlusBias() {
        let engine = PerceptronEngine(gridSize: 2)
        engine.setWeight(0, 2.0)
        engine.setWeight(1, -1.5)
        engine.setWeight(2, 4.0)
        engine.setWeight(3, 0.5)
        engine.bias = 1.0
        // inputs = [+1, -1, +1, -1]
        // = (1·2.0) + (-1·-1.5) + (1·4.0) + (-1·0.5) + 1.0
        // = 2.0 + 1.5 + 4.0 - 0.5 + 1.0 = 8.0
        #expect(abs(engine.calculateOutput([1, -1, 1, -1]) - 8.0) < tol)
    }

    @Test func outputWithAllInputsOff() {
        let engine = PerceptronEngine(gridSize: 2)
        engine.setWeight(0, 1.0)
        engine.setWeight(1, 2.0)
        engine.setWeight(2, 3.0)
        engine.setWeight(3, 4.0)
        // all OFF => all inputs -1
        // = -(1+2+3+4) + 0 = -10.0
        #expect(abs(engine.calculateOutput([-1, -1, -1, -1]) - (-10.0)) < tol)
    }

    // MARK: - setWeight clamping

    @Test func setWeightClampsToRange() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.setWeight(0, 999)
        #expect(engine.weight(at: 0) == 30)
        engine.setWeight(0, -999)
        #expect(engine.weight(at: 0) == -30)
    }

    @Test func setWeightIgnoresOutOfBoundsIndex() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.setWeight(5, 10) // no crash, no effect
        #expect(engine.weight(at: 5) == 0)
    }

    // MARK: - learn: no-op when already correct

    @Test func learnReturnsFalseWhenAlreadyPositive() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.setWeight(0, 5) // input +1 => output 5 > 0
        let changed = engine.learn([1], desiredPositive: true)
        #expect(changed == false)
        #expect(engine.weight(at: 0) == 5) // unchanged
        #expect(engine.bias == 0)
    }

    @Test func learnReturnsFalseWhenAlreadyNegative() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.setWeight(0, 5) // input -1 => output -5 < 0
        let changed = engine.learn([-1], desiredPositive: false)
        #expect(changed == false)
        #expect(engine.weight(at: 0) == 5)
    }

    // MARK: - learn: classic update rule

    @Test func learnPositiveNudgesWeightsAndBiasByRateTimesInput() {
        let engine = PerceptronEngine(gridSize: 1)
        // learningRate default 10.0. Start at 0 => output 0, not > 0, so wrong.
        let changed = engine.learn([1], desiredPositive: true)
        #expect(changed == true)
        // weight += 10 * (+1) = 10;  bias += 10 = 10
        #expect(abs(engine.weight(at: 0) - 10.0) < tol)
        #expect(abs(engine.bias - 10.0) < tol)
    }

    @Test func learnNegativeNudgesOppositeDirection() {
        let engine = PerceptronEngine(gridSize: 1)
        // Start at 0 => output 0, not < 0, so wrong for desiredNegative.
        let changed = engine.learn([1], desiredPositive: false)
        #expect(changed == true)
        // weight -= 10 * (+1) = -10;  bias -= 10 = -10
        #expect(abs(engine.weight(at: 0) - (-10.0)) < tol)
        #expect(abs(engine.bias - (-10.0)) < tol)
    }

    @Test func learnWithOffInputMovesWeightUpForPositiveTarget() {
        let engine = PerceptronEngine(gridSize: 1)
        // input OFF (-1), want positive, currently output 0 (wrong).
        engine.learn([-1], desiredPositive: true)
        // weight += 10 * (-1) = -10;  bias += 10 = 10
        #expect(abs(engine.weight(at: 0) - (-10.0)) < tol)
        #expect(abs(engine.bias - 10.0) < tol)
    }

    @Test func learnClampsWeightAndBiasAt30() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.setWeight(0, 25)
        engine.bias = 25
        // input +1, want positive, but output = 25 + 25 = 50 > 0 => already correct, no-op.
        // Force a "wrong" state: make output <= 0 while wanting positive.
        engine.setWeight(0, -25)
        engine.bias = 0
        // output with input +1 = -25 (wrong for positive) => update:
        // weight += 10 => -15; bias += 10 => 10. Not yet clamped.
        engine.learn([1], desiredPositive: true)
        #expect(abs(engine.weight(at: 0) - (-15.0)) < tol)

        // Drive weight above +30 to confirm clamp.
        engine.setWeight(0, 25)
        engine.bias = -100 // ensure output negative => wrong for positive target
        engine.learn([1], desiredPositive: true)
        // weight += 10 => 35, clamped to 30
        #expect(engine.weight(at: 0) == 30)
    }

    // MARK: - learningRate honored

    @Test func learnUsesConfiguredLearningRate() {
        let engine = PerceptronEngine(gridSize: 1)
        engine.learningRate = 3.0
        engine.learn([1], desiredPositive: true) // output 0 => wrong
        #expect(abs(engine.weight(at: 0) - 3.0) < tol)
        #expect(abs(engine.bias - 3.0) < tol)
    }

    // MARK: - resetWeights

    @Test func resetZeroesWeightsAndBias() {
        let engine = PerceptronEngine(gridSize: 2)
        engine.setWeight(0, 10)
        engine.setWeight(3, -7)
        engine.bias = 4
        engine.resetWeights()
        #expect(engine.weights.allSatisfy { $0 == 0 })
        #expect(engine.bias == 0)
    }

    // MARK: - end-to-end: teach a simple pattern

    @Test func repeatedLearningConvergesToCorrectClassification() {
        // Teach: input pattern [+1, +1] should produce positive output.
        let engine = PerceptronEngine(gridSize: 1) // 1 weight
        // Use a 2-input engine instead:
        let e2 = PerceptronEngine(gridSize: 2) // 4 weights; use pattern below
        let pattern = [1, 1, -1, -1]

        // Repeatedly teach "positive" until it classifies correctly.
        var iterations = 0
        while e2.calculateOutput(pattern) <= 0 && iterations < 100 {
            e2.learn(pattern, desiredPositive: true)
            iterations += 1
        }
        #expect(e2.calculateOutput(pattern) > 0)
        #expect(iterations < 100) // must converge
        _ = engine
    }
}
