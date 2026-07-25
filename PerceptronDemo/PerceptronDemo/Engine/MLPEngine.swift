//
//  MLPEngine.swift
//  PerceptronDemo
//
//  A small multi-layer perceptron — the 1986 backpropagation network — ported
//  from the desktop PerceptronEngine.cs BACKPROP rule, but generalized so the
//  hidden layer size is independent of the input count (the desktop used a
//  square hidden layer). This is what powers the "signal-flow" patch-panel
//  screen and, unlike the single-layer PerceptronEngine, it can learn
//  translation-invariant shapes (e.g. a T anywhere in the grid).
//
//  Architecture:  inputs → hidden (ReLU) → single output
//    h[j]   = ReLU( Σᵢ W1[j,i]·x[i] + b1[j] )
//    output = Σⱼ W2[j]·h[j] + bias
//
//  Learning (backprop, only when the current classification is wrong — matching
//  the desktop's "update only when wrong" gate):
//    target  = desiredPositive ? +10 : −10
//    error   = target − output
//    W2[j]  += lr·error·h[j];              bias += lr·error
//    δ[j]    = W2[j]·error·ReLU'(z[j])
//    W1[j,i]+= lr·δ[j]·x[i];               b1[j] += lr·δ[j]
//  where lr = learningRate · 0.01 (same scaling as the desktop backprop rule).
//
//  Inputs are +1 (ON) / −1 (OFF). All weights/biases clamp to −30…30.
//

import Foundation

final class MLPEngine {

    let inputCount: Int
    let hiddenCount: Int

    /// W1[j][i] — input→hidden weights (hiddenCount × inputCount).
    private(set) var hiddenWeights: [[Double]]
    /// b1[j] — hidden biases (hiddenCount).
    private(set) var hiddenBiases: [Double]
    /// W2[j] — hidden→output weights (hiddenCount).
    private(set) var outputWeights: [Double]
    /// Output bias.
    var bias: Double = 0

    /// h[j] — the ReLU activations from the most recent `calculateOutput`. Drives
    /// the hidden-neuron bulbs on the patch-panel screen.
    private(set) var hiddenActivations: [Double]

    var learningRate: Double = 10.0

    init(inputCount: Int, hiddenCount: Int) {
        self.inputCount = inputCount
        self.hiddenCount = hiddenCount
        self.hiddenWeights = Array(repeating: Array(repeating: 0, count: inputCount), count: hiddenCount)
        self.hiddenBiases = Array(repeating: 0, count: hiddenCount)
        self.outputWeights = Array(repeating: 0, count: hiddenCount)
        self.hiddenActivations = Array(repeating: 0, count: hiddenCount)
        seedWeights()
    }

    // MARK: - Initialization

    /// Deterministically seeds the hidden weights with small, varied values.
    /// A symmetric all-zero start would keep every hidden node identical forever
    /// (they'd all receive the same gradient), so backprop could never
    /// differentiate them — the classic symmetry-breaking problem. We use a
    /// fixed LCG instead of `Double.random` so training is fully reproducible:
    /// the generator test regenerates identical preset files every run.
    private func seedWeights() {
        var state: UInt64 = 0x9E3779B97F4A7C15   // fixed seed
        func next() -> Double {
            // xorshift* — deterministic pseudo-random in [0,1)
            state ^= state >> 12
            state ^= state << 25
            state ^= state >> 27
            let bits = state &* 0x2545F4914F6CDD1D
            return Double(bits >> 11) * (1.0 / 9007199254740992.0)
        }
        // A meaningful spread (±1) so the hidden units start genuinely
        // *different* from one another. A tiny spread lets the crude
        // "update-when-wrong" rule feed all units near-identical gradients, so
        // they collapse into the same detector and rail to the clamps — a
        // degenerate solution. Distinct starts break that symmetry and let each
        // hidden neuron specialize.
        for j in 0..<hiddenCount {
            hiddenBiases[j] = 0
            outputWeights[j] = (next() - 0.5) * 0.5
            for i in 0..<inputCount {
                hiddenWeights[j][i] = (next() - 0.5) * 2.0
            }
        }
    }

    // MARK: - Forward pass

    /// Runs the network and caches the hidden activations. Inputs are ±1.
    @discardableResult
    func calculateOutput(_ inputs: [Int]) -> Double {
        precondition(inputs.count == inputCount, "Input size must match inputCount")

        for j in 0..<hiddenCount {
            var z = hiddenBiases[j]
            let row = hiddenWeights[j]
            for i in 0..<inputCount {
                z += row[i] * Double(inputs[i])
            }
            hiddenActivations[j] = max(0, z)   // ReLU
        }

        var output = bias
        for j in 0..<hiddenCount {
            output += outputWeights[j] * hiddenActivations[j]
        }
        return output
    }

    // MARK: - Learning

    /// One backprop step for the live LEARN +/− buttons. Mirrors the desktop
    /// rule: no-op when the current sign already matches the desired sign;
    /// otherwise gradient-descend toward a ±10 target. Returns true if weights
    /// were adjusted.
    @discardableResult
    func learn(_ inputs: [Int], desiredPositive: Bool) -> Bool {
        let output = calculateOutput(inputs)
        if desiredPositive && output > 0 { return false }
        if !desiredPositive && output < 0 { return false }
        applyBackprop(inputs, desiredPositive: desiredPositive, currentOutput: output)
        return true
    }

    /// One backprop step for offline *training* (the preset generator). Unlike
    /// `learn`, this keeps pushing until the output clears `margin` on the
    /// correct side — not merely until the sign is right — which is what lets
    /// the network find a distributed solution with a real decision margin
    /// instead of stopping at a fragile boundary. Returns true if it updated.
    @discardableResult
    func trainStep(_ inputs: [Int], desiredPositive: Bool, margin: Double) -> Bool {
        let output = calculateOutput(inputs)
        if desiredPositive && output >= margin { return false }
        if !desiredPositive && output <= -margin { return false }
        applyBackprop(inputs, desiredPositive: desiredPositive, currentOutput: output)
        return true
    }

    /// The shared gradient update: target ±10, error = target − output,
    /// output-layer then hidden-layer weights, all clamped to ±30.
    private func applyBackprop(_ inputs: [Int], desiredPositive: Bool, currentOutput: Double) {
        let target = desiredPositive ? 10.0 : -10.0
        let error = target - currentOutput
        let lr = learningRate * 0.01

        // Output layer: W2[j] += lr·error·h[j];  bias += lr·error
        for j in 0..<hiddenCount {
            outputWeights[j] = (outputWeights[j] + lr * error * hiddenActivations[j]).clamped(to: -30...30)
        }
        bias = (bias + lr * error).clamped(to: -30...30)

        // Hidden layer: δ[j] = W2[j]·error·ReLU'(z[j]); W1[j,i] += lr·δ[j]·x[i]
        for j in 0..<hiddenCount {
            let reluDerivative = hiddenActivations[j] > 0 ? 1.0 : 0.0
            let delta = outputWeights[j] * error * reluDerivative
            for i in 0..<inputCount {
                hiddenWeights[j][i] = (hiddenWeights[j][i] + lr * delta * Double(inputs[i])).clamped(to: -30...30)
            }
            hiddenBiases[j] = (hiddenBiases[j] + lr * delta).clamped(to: -30...30)
        }
    }

    func reset() {
        for j in 0..<hiddenCount {
            hiddenBiases[j] = 0
            outputWeights[j] = 0
            hiddenActivations[j] = 0
        }
        bias = 0
        seedWeights()
    }

    // MARK: - Snapshot bridging

    /// Restores all learned parameters from stored arrays (used when loading a
    /// preset / saved file). Dimensions must match this engine.
    func load(hiddenWeights: [[Double]], hiddenBiases: [Double],
              outputWeights: [Double], bias: Double) {
        guard hiddenWeights.count == hiddenCount,
              hiddenWeights.allSatisfy({ $0.count == inputCount }),
              hiddenBiases.count == hiddenCount,
              outputWeights.count == hiddenCount else {
            return
        }
        self.hiddenWeights = hiddenWeights
        self.hiddenBiases = hiddenBiases
        self.outputWeights = outputWeights
        self.bias = bias
    }
}
