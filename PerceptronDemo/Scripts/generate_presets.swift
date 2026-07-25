#!/usr/bin/env swift
//
//  generate_presets.swift
//  PerceptronDemo — preset generator (host tool)
//
//  Trains the bundled example networks and writes their `.pcn` files into
//  `PerceptronDemo/Presets/`. Run it directly on the host (no Xcode, no
//  simulator, no sandbox):
//
//      swift PerceptronDemo/Scripts/generate_presets.swift
//
//  It re-derives every preset from scratch, ASSERTS each one classifies its
//  training set correctly (exits non-zero otherwise), and overwrites the
//  checked-in files. The Swift unit tests (MLPEngineTests / PresetGenerator
//  Tests) independently prove the same training converges; this script is the
//  reliable *writer*, because a sandboxed test process cannot write the source
//  tree.
//
//  The engine/training logic here is kept byte-compatible with the app's
//  MLPEngine so a script-generated preset behaves identically when loaded.
//

import Foundation

// MARK: - Clamp

extension Comparable {
    func clamped(to range: ClosedRange<Self>) -> Self {
        min(max(self, range.lowerBound), range.upperBound)
    }
}

// MARK: - MLP (mirror of the app's MLPEngine)

final class MLP {
    let inputCount: Int
    let hiddenCount: Int
    var hiddenWeights: [[Double]]
    var hiddenBiases: [Double]
    var outputWeights: [Double]
    var bias = 0.0
    var hiddenActivations: [Double]
    var learningRate = 10.0

    init(inputCount: Int, hiddenCount: Int) {
        self.inputCount = inputCount
        self.hiddenCount = hiddenCount
        hiddenWeights = Array(repeating: Array(repeating: 0, count: inputCount), count: hiddenCount)
        hiddenBiases = Array(repeating: 0, count: hiddenCount)
        outputWeights = Array(repeating: 0, count: hiddenCount)
        hiddenActivations = Array(repeating: 0, count: hiddenCount)
        seed()
    }

    private func seed() {
        var state: UInt64 = 0x9E3779B97F4A7C15
        func next() -> Double {
            state ^= state >> 12; state ^= state << 25; state ^= state >> 27
            let bits = state &* 0x2545F4914F6CDD1D
            return Double(bits >> 11) * (1.0 / 9007199254740992.0)
        }
        for j in 0..<hiddenCount {
            hiddenBiases[j] = 0
            outputWeights[j] = (next() - 0.5) * 0.5
            for i in 0..<inputCount { hiddenWeights[j][i] = (next() - 0.5) * 2.0 }
        }
    }

    @discardableResult
    func calculateOutput(_ inputs: [Int]) -> Double {
        for j in 0..<hiddenCount {
            var z = hiddenBiases[j]
            let row = hiddenWeights[j]
            for i in 0..<inputCount { z += row[i] * Double(inputs[i]) }
            hiddenActivations[j] = max(0, z)
        }
        var out = bias
        for j in 0..<hiddenCount { out += outputWeights[j] * hiddenActivations[j] }
        return out
    }

    @discardableResult
    func trainStep(_ inputs: [Int], desiredPositive: Bool, margin: Double) -> Bool {
        let out = calculateOutput(inputs)
        if desiredPositive && out >= margin { return false }
        if !desiredPositive && out <= -margin { return false }
        let target = desiredPositive ? 10.0 : -10.0
        let error = target - out
        let lr = learningRate * 0.01
        for j in 0..<hiddenCount {
            outputWeights[j] = (outputWeights[j] + lr * error * hiddenActivations[j]).clamped(to: -30...30)
        }
        bias = (bias + lr * error).clamped(to: -30...30)
        for j in 0..<hiddenCount {
            let d = hiddenActivations[j] > 0 ? 1.0 : 0.0
            let delta = outputWeights[j] * error * d
            for i in 0..<inputCount {
                hiddenWeights[j][i] = (hiddenWeights[j][i] + lr * delta * Double(inputs[i])).clamped(to: -30...30)
            }
            hiddenBiases[j] = (hiddenBiases[j] + lr * delta).clamped(to: -30...30)
        }
        return true
    }
}

// MARK: - Single-layer perceptron (mirror of PerceptronEngine)

final class SLP {
    var weights: [Double]
    var bias = 0.0
    var learningRate = 10.0
    init(nodeCount: Int) { weights = Array(repeating: 0, count: nodeCount) }

    func output(_ inputs: [Int]) -> Double {
        var s = 0.0
        for i in 0..<inputs.count { s += Double(inputs[i]) * weights[i] }
        return s + bias
    }
    @discardableResult
    func learn(_ inputs: [Int], desiredPositive: Bool) -> Bool {
        let o = output(inputs)
        if desiredPositive && o > 0 { return false }
        if !desiredPositive && o < 0 { return false }
        for i in 0..<inputs.count {
            weights[i] = (weights[i] + (desiredPositive ? 1 : -1) * learningRate * Double(inputs[i])).clamped(to: -30...30)
        }
        bias = (bias + (desiredPositive ? 1 : -1) * learningRate).clamped(to: -30...30)
        return true
    }
}

// MARK: - Grid helpers

let N = 4
func idx(_ r: Int, _ c: Int) -> Int { r * N + c }
func gridBools(_ on: (Int, Int) -> Bool) -> [Bool] {
    (0..<(N * N)).map { on($0 / N, $0 % N) }
}

struct Sample { let states: [Bool]; let positive: Bool }
func inputs(_ states: [Bool]) -> [Int] { states.map { $0 ? 1 : -1 } }

// MARK: - Snapshot encoding (matches PerceptronSnapshot's JSON)

func encode(gridSize: Int, weights: [Double], bias: Double, learningRate: Double,
            switchStates: [Bool], mlp: MLP?) -> Data {
    var dict: [String: Any] = [
        "gridSize": gridSize,
        "weights": weights,
        "bias": bias,
        "learningRate": learningRate,
        "switchStates": switchStates,
    ]
    if let mlp {
        dict["mlp"] = [
            "hiddenCount": mlp.hiddenCount,
            "hiddenWeights": mlp.hiddenWeights,
            "hiddenBiases": mlp.hiddenBiases,
            "outputWeights": mlp.outputWeights,
        ]
    }
    return try! JSONSerialization.data(withJSONObject: dict,
                                       options: [.prettyPrinted, .sortedKeys])
}

// Resolve Presets/ relative to this script file.
let presetsDir = URL(fileURLWithPath: #filePath)      // .../Scripts/generate_presets.swift
    .deletingLastPathComponent()                       // .../Scripts
    .deletingLastPathComponent()                       // .../PerceptronDemo (project dir)
    .appendingPathComponent("PerceptronDemo")          // app sources
    .appendingPathComponent("Presets")
try! FileManager.default.createDirectory(at: presetsDir, withIntermediateDirectories: true)

func writePreset(_ name: String, _ data: Data) {
    let url = presetsDir.appendingPathComponent("\(name).pcn")
    try! data.write(to: url, options: .atomic)
    print("  wrote \(url.lastPathComponent) (\(data.count) bytes)")
}

func fail(_ msg: String) -> Never {
    FileHandle.standardError.write(Data(("ERROR: " + msg + "\n").utf8))
    exit(1)
}

// MARK: - Single-layer presets

func trainSLP(_ name: String, samples: [Sample], display: [Bool]) {
    let slp = SLP(nodeCount: N * N)
    var epochs = 0
    while epochs < 1000 {
        var clean = true
        for s in samples where slp.learn(inputs(s.states), desiredPositive: s.positive) { clean = false }
        epochs += 1
        if clean { break }
    }
    for s in samples {
        let o = slp.output(inputs(s.states))
        if (s.positive && o <= 0) || (!s.positive && o >= 0) { fail("\(name) misclassifies a sample") }
    }
    writePreset(name, encode(gridSize: N, weights: slp.weights, bias: slp.bias,
                             learningRate: slp.learningRate, switchStates: display, mlp: nil))
}

print("Single-layer presets:")
trainSLP("TopVsBottom",
         samples: [
            Sample(states: gridBools { r, _ in r < 2 }, positive: true),
            Sample(states: gridBools { r, _ in r == 0 }, positive: true),
            Sample(states: gridBools { r, _ in r >= 2 }, positive: false),
            Sample(states: gridBools { r, _ in r == 3 }, positive: false),
         ],
         display: gridBools { r, _ in r < 2 })
trainSLP("LeftVsRight",
         samples: [
            Sample(states: gridBools { _, c in c < 2 }, positive: true),
            Sample(states: gridBools { _, c in c == 0 }, positive: true),
            Sample(states: gridBools { _, c in c >= 2 }, positive: false),
            Sample(states: gridBools { _, c in c == 3 }, positive: false),
         ],
         display: gridBools { _, c in c < 2 })
trainSLP("Diagonal",
         samples: [
            Sample(states: gridBools { r, c in r == c }, positive: true),
            Sample(states: gridBools { r, c in r + c == N - 1 }, positive: false),
         ],
         display: gridBools { r, c in r == c })
trainSLP("Density",
         samples: [
            Sample(states: [Bool](repeating: true, count: N * N), positive: true),
            Sample(states: gridBools { r, _ in r < 3 }, positive: true),
            Sample(states: [Bool](repeating: false, count: N * N), positive: false),
            Sample(states: gridBools { r, _ in r < 1 }, positive: false),
         ],
         display: [Bool](repeating: true, count: N * N))

// MARK: - T-pattern (multi-layer) preset

func tShape(top: Int, left: Int) -> Set<[Int]> {
    var cells = Set<[Int]>()
    for c in left..<(left + 3) { cells.insert([top, c]) }
    let mid = left + 1
    cells.insert([top + 1, mid]); cells.insert([top + 2, mid])
    return cells
}
func fromCells(_ cells: Set<[Int]>) -> [Bool] { gridBools { r, c in cells.contains([r, c]) } }

func tSamples() -> ([Sample], [Bool]) {
    var pos: [Set<[Int]>] = []
    for top in 0...(N - 3) { for left in 0...(N - 3) { pos.append(tShape(top: top, left: left)) } }

    var neg: [Set<[Int]>] = []
    func tDown(_ top: Int, _ left: Int) -> Set<[Int]> {
        var s = Set<[Int]>(); let mid = left + 1
        s.insert([top, mid]); s.insert([top + 1, mid])
        for c in left..<(left + 3) { s.insert([top + 2, c]) }; return s
    }
    func tLeft(_ top: Int, _ left: Int) -> Set<[Int]> {
        var s = Set<[Int]>(); for r in top..<(top + 3) { s.insert([r, left]) }
        let mid = top + 1; s.insert([mid, left + 1]); s.insert([mid, left + 2]); return s
    }
    func plus(_ top: Int, _ left: Int) -> Set<[Int]> {
        var s = Set<[Int]>(); let mr = top + 1, mc = left + 1
        s.insert([top, mc]); s.insert([top + 2, mc])
        s.insert([mr, left]); s.insert([mr, left + 2]); s.insert([mr, mc]); return s
    }
    for top in 0...(N - 3) { for left in 0...(N - 3) {
        neg.append(tDown(top, left)); neg.append(tLeft(top, left)); neg.append(plus(top, left))
    } }
    neg.append([])
    neg.append(Set((0..<N).flatMap { r in (0..<N).map { [r, $0] } }))
    for r in 0..<N { neg.append(Set((0..<N).map { [r, $0] })) }
    for c in 0..<N { neg.append(Set((0..<N).map { [$0, c] })) }
    neg.append(Set((0..<N).map { [$0, $0] }))
    neg.append(Set((0..<N).map { [$0, N - 1 - $0] }))

    let samples = pos.map { Sample(states: fromCells($0), positive: true) } +
                  neg.map { Sample(states: fromCells($0), positive: false) }
    return (samples, fromCells(tShape(top: 0, left: 0)))
}

print("Multi-layer preset:")
let (tsamples, tdisplay) = tSamples()
let mlp = MLP(inputCount: N * N, hiddenCount: 6)
mlp.learningRate = 0.5
let margin = 1.0
var converged = false
for _ in 0..<50_000 {
    for s in tsamples { mlp.trainStep(inputs(s.states), desiredPositive: s.positive, margin: margin) }
    if tsamples.allSatisfy({ s in
        let o = mlp.calculateOutput(inputs(s.states))
        return s.positive ? o >= margin : o <= -margin
    }) { converged = true; break }
}
if !converged { fail("TPattern MLP did not converge") }
// Reject the degenerate all-identical-hidden-units solution.
var distinct = false
for a in 0..<mlp.hiddenCount { for b in (a + 1)..<mlp.hiddenCount {
    if zip(mlp.hiddenWeights[a], mlp.hiddenWeights[b]).map({ abs($0 - $1) }).max() ?? 0 > 0.5 { distinct = true }
} }
if !distinct { fail("TPattern hidden units collapsed (degenerate)") }

writePreset("TPattern", encode(gridSize: N, weights: mlp.outputWeights, bias: mlp.bias,
                               learningRate: mlp.learningRate, switchStates: tdisplay, mlp: mlp))
print("Done. All presets trained and written.")
