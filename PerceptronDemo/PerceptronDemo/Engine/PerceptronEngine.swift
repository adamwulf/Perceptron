//
//  PerceptronEngine.swift
//  PerceptronDemo
//
//  Port of PerceptronEngine.cs — the neural-network logic.
//  This first pass ports the classic 1958 Rosenblatt rule, which is
//  what the main-screen formula plate ("SUM(Switch x Weight) + Bias")
//  displays. Other math rules from the desktop app can be added later.
//

import Foundation

/// Single-layer perceptron using the original 1958 Rosenblatt learning rule.
/// Switch inputs are +1 (ON) or -1 (OFF); weights and bias range -30…30.
final class PerceptronEngine {

    private(set) var weights: [Double]
    var bias: Double = 0
    var learningRate: Double = 10.0

    /// `gridSize` is the NxN edge length; weight count is gridSize².
    init(gridSize: Int) {
        weights = Array(repeating: 0, count: gridSize * gridSize)
    }

    /// PERCEPTRON_CLASSIC: OUTPUT = Σ(input[i] × weight[i]) + bias
    func calculateOutput(_ inputs: [Int]) -> Double {
        precondition(inputs.count == weights.count, "Input size must match weight count")
        var sum = 0.0
        for i in 0..<inputs.count {
            sum += Double(inputs[i]) * weights[i]
        }
        return sum + bias
    }

    /// Original perceptron learning rule (update only when wrong).
    /// - Parameters:
    ///   - inputs: current switch states (-1 or +1)
    ///   - desiredPositive: true if the output should be positive
    /// - Returns: true if weights were adjusted, false if already correct
    @discardableResult
    func learn(_ inputs: [Int], desiredPositive: Bool) -> Bool {
        let currentOutput = calculateOutput(inputs)

        // Already classified correctly — no adjustment needed.
        if desiredPositive && currentOutput > 0 { return false }
        if !desiredPositive && currentOutput < 0 { return false }

        for i in 0..<inputs.count {
            if desiredPositive {
                weights[i] += learningRate * Double(inputs[i])
            } else {
                weights[i] -= learningRate * Double(inputs[i])
            }
            weights[i] = weights[i].clamped(to: -30...30)
        }

        bias += desiredPositive ? learningRate : -learningRate
        bias = bias.clamped(to: -30...30)

        return true
    }

    func resetWeights() {
        for i in 0..<weights.count { weights[i] = 0 }
        bias = 0
    }

    func setWeight(_ index: Int, _ value: Double) {
        guard weights.indices.contains(index) else { return }
        weights[index] = value.clamped(to: -30...30)
    }

    func weight(at index: Int) -> Double {
        weights.indices.contains(index) ? weights[index] : 0
    }
}

extension Comparable {
    /// Matches C#'s Math.Clamp semantics.
    func clamped(to range: ClosedRange<Self>) -> Self {
        min(max(self, range.lowerBound), range.upperBound)
    }
}
