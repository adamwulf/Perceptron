//
//  NetworkView.swift
//  PerceptronDemo
//
//  The illuminated patch-panel that visualizes the multi-layer network as
//  physical hardware: neurons are light bulbs (brightness = activation) and
//  weights are wires strung between them (thickness = weight magnitude, colour
//  = sign, glow = live signal flowing through right now).
//
//  Three columns, left → right:
//    • input bulbs   — one per input, 100% (ON) or 0% (OFF)
//    • hidden bulbs  — one per hidden neuron, glowing at its ReLU activation
//    • output bulb   — the final neuron, glowing with the (clamped) output
//
//  Everything is drawn in one pass so the wires sit *behind* the bulbs. The
//  view is display-only; it renders whatever state the owning view controller
//  hands it via `update(...)`.
//

import UIKit

final class NetworkView: UIView {

    // MARK: - State (set by the owner each time the network is evaluated)

    /// Per-input activation: 1 (ON) or 0 (OFF).
    private var inputActivations: [Double] = []
    /// Per-hidden ReLU activation (>= 0), normalized against the current max for
    /// display brightness.
    private var hiddenActivations: [Double] = []
    /// W1[j][i] — input→hidden weights (hiddenCount × inputCount).
    private var hiddenWeights: [[Double]] = []
    /// W2[j] — hidden→output weights.
    private var outputWeights: [Double] = []
    /// The final network output (pre-clamped for the meter; sign drives the LED).
    private var output: Double = 0

    func update(inputActivations: [Double],
                hiddenActivations: [Double],
                hiddenWeights: [[Double]],
                outputWeights: [Double],
                output: Double) {
        self.inputActivations = inputActivations
        self.hiddenActivations = hiddenActivations
        self.hiddenWeights = hiddenWeights
        self.outputWeights = outputWeights
        self.output = output
        setNeedsDisplay()
    }

    // MARK: - Palette

    private enum Palette {
        static let positive = UIColor(red: 255/255, green: 190/255, blue: 70/255, alpha: 1)  // warm amber
        static let negative = UIColor(red: 80/255, green: 150/255, blue: 255/255, alpha: 1)   // cool blue
        static let bulbOff  = UIColor(white: 55/255, alpha: 1)
        static let socket   = UIColor(white: 22/255, alpha: 1)
        static let rail     = UIColor(white: 90/255, alpha: 1)
        static let label    = UIColor(white: 140/255, alpha: 1)
    }

    override init(frame: CGRect) {
        super.init(frame: frame)
        backgroundColor = .clear
        isOpaque = false
        contentMode = .redraw
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        backgroundColor = .clear
        isOpaque = false
        contentMode = .redraw
    }

    // MARK: - Layout math

    private struct Layout {
        var inputPoints: [CGPoint]
        var hiddenPoints: [CGPoint]
        var outputPoint: CGPoint
        var inputRadius: CGFloat
        var hiddenRadius: CGFloat
        var outputRadius: CGFloat
    }

    private func computeLayout() -> Layout {
        let inset: CGFloat = 24
        let topLabel: CGFloat = 26          // room for column captions
        let area = bounds.insetBy(dx: inset, dy: inset)
        let colX = { (frac: CGFloat) in area.minX + area.width * frac }

        let inputX = colX(0.06)
        let hiddenX = colX(0.52)
        let outputX = colX(0.94)

        let inputR: CGFloat = 9
        let hiddenR: CGFloat = 16
        let outputR: CGFloat = 24

        func column(count: Int, x: CGFloat, radius: CGFloat) -> [CGPoint] {
            guard count > 0 else { return [] }
            let top = area.minY + topLabel + radius
            let bottom = area.maxY - radius
            let span = bottom - top
            return (0..<count).map { i in
                let t = count == 1 ? 0.5 : CGFloat(i) / CGFloat(count - 1)
                return CGPoint(x: x, y: top + span * t)
            }
        }

        return Layout(
            inputPoints: column(count: inputActivations.count, x: inputX, radius: inputR),
            hiddenPoints: column(count: hiddenActivations.count, x: hiddenX, radius: hiddenR),
            outputPoint: CGPoint(x: outputX, y: area.midY + topLabel / 2),
            inputRadius: inputR, hiddenRadius: hiddenR, outputRadius: outputR)
    }

    // MARK: - Draw

    override func draw(_ rect: CGRect) {
        guard let ctx = UIGraphicsGetCurrentContext(),
              !inputActivations.isEmpty, !hiddenActivations.isEmpty else { return }

        let layout = computeLayout()

        // Normalizers so brightness/thickness map to [0,1] against the live max.
        let maxHidden = max(hiddenActivations.max() ?? 1, 0.0001)
        let maxHiddenWeight = max(hiddenWeights.flatMap { $0 }.map { abs($0) }.max() ?? 1, 0.0001)
        let maxOutputWeight = max(outputWeights.map { abs($0) }.max() ?? 1, 0.0001)

        // 1) Wires: input → hidden (behind everything).
        for (j, hp) in layout.hiddenPoints.enumerated() {
            guard j < hiddenWeights.count else { continue }
            for (i, ip) in layout.inputPoints.enumerated() {
                guard i < hiddenWeights[j].count else { continue }
                let w = hiddenWeights[j][i]
                let signal = (i < inputActivations.count ? inputActivations[i] : 0)
                drawWire(ctx, from: ip, to: hp,
                         weight: w, maxWeight: maxHiddenWeight, signal: signal)
            }
        }

        // 2) Wires: hidden → output.
        for (j, hp) in layout.hiddenPoints.enumerated() {
            guard j < outputWeights.count else { continue }
            let signal = hiddenActivations[j] / maxHidden
            drawWire(ctx, from: hp, to: layout.outputPoint,
                     weight: outputWeights[j], maxWeight: maxOutputWeight, signal: signal)
        }

        // 3) Bulbs on top of the wires.
        for (i, ip) in layout.inputPoints.enumerated() {
            drawBulb(ctx, at: ip, radius: layout.inputRadius,
                     brightness: inputActivations[i], tint: Palette.positive)
        }
        for (j, hp) in layout.hiddenPoints.enumerated() {
            drawBulb(ctx, at: hp, radius: layout.hiddenRadius,
                     brightness: hiddenActivations[j] / maxHidden, tint: Palette.positive)
        }
        // Output bulb: amber if firing positive, blue if negative; brightness by
        // magnitude (saturating around the ±10 target the net is trained to).
        let outBrightness = min(abs(output) / 10.0, 1.0)
        let outTint = output >= 0 ? Palette.positive : Palette.negative
        drawBulb(ctx, at: layout.outputPoint, radius: layout.outputRadius,
                 brightness: outBrightness, tint: outTint)

        // 4) Column captions.
        drawCaption("INPUTS", centeredX: layout.inputPoints.first?.x ?? 0, ctx: ctx)
        drawCaption("HIDDEN", centeredX: layout.hiddenPoints.first?.x ?? 0, ctx: ctx)
        drawCaption("OUT", centeredX: layout.outputPoint.x, ctx: ctx)
    }

    /// A wire whose thickness encodes |weight|, colour encodes sign, and glow
    /// encodes the live signal (|weight|-scaled source activation) flowing now.
    private func drawWire(_ ctx: CGContext, from: CGPoint, to: CGPoint,
                          weight: Double, maxWeight: Double, signal: Double) {
        let magnitude = abs(weight) / maxWeight                 // 0…1
        let width = 0.5 + magnitude * 4.0                       // thin…thick
        let base = weight >= 0 ? Palette.positive : Palette.negative

        // Dim resting wire (the wiring diagram) — always faintly visible.
        ctx.setLineWidth(width)
        ctx.setStrokeColor(base.withAlphaComponent(0.10 + 0.15 * magnitude).cgColor)
        ctx.move(to: from); ctx.addLine(to: to); ctx.strokePath()

        // Live glow proportional to signal × weight — this is what "lights up"
        // as the user flips inputs.
        let flow = magnitude * max(0, min(signal, 1))
        if flow > 0.02 {
            ctx.setLineWidth(width)
            ctx.setStrokeColor(base.withAlphaComponent(0.15 + 0.75 * flow).cgColor)
            ctx.setShadow(offset: .zero, blur: 4 + 6 * flow, color: base.withAlphaComponent(0.9).cgColor)
            ctx.move(to: from); ctx.addLine(to: to); ctx.strokePath()
            ctx.setShadow(offset: .zero, blur: 0, color: nil)
        }
    }

    /// A light-bulb neuron. `brightness` 0…1 drives the glow; a dim socket ring
    /// is always drawn so an off bulb still reads as a physical component.
    private func drawBulb(_ ctx: CGContext, at center: CGPoint, radius: CGFloat,
                          brightness: Double, tint: UIColor) {
        let b = max(0, min(brightness, 1))
        let rect = CGRect(x: center.x - radius, y: center.y - radius,
                          width: radius * 2, height: radius * 2)

        // Socket ring.
        ctx.setFillColor(Palette.socket.cgColor)
        ctx.fillEllipse(in: rect.insetBy(dx: -3, dy: -3))

        // Outer glow halo when lit.
        if b > 0.02 {
            ctx.setFillColor(tint.withAlphaComponent(0.25 * b).cgColor)
            ctx.fillEllipse(in: rect.insetBy(dx: -radius * 0.9, dy: -radius * 0.9))
            ctx.setFillColor(tint.withAlphaComponent(0.35 * b).cgColor)
            ctx.fillEllipse(in: rect.insetBy(dx: -radius * 0.4, dy: -radius * 0.4))
        }

        // Bulb body — interpolates from dark glass (off) to full tint (on).
        let body = blend(Palette.bulbOff, tint, t: b)
        ctx.setFillColor(body.cgColor)
        ctx.fillEllipse(in: rect)

        // Specular highlight.
        if b > 0.1 {
            ctx.setFillColor(UIColor(white: 1, alpha: 0.5 * b).cgColor)
            let hs = radius * 0.5
            ctx.fillEllipse(in: CGRect(x: center.x - radius * 0.4, y: center.y - radius * 0.5,
                                       width: hs, height: hs * 0.7))
        }

        // Rim.
        ctx.setStrokeColor(UIColor(white: 100/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.strokeEllipse(in: rect)
    }

    private func drawCaption(_ text: String, centeredX: CGFloat, ctx: CGContext) {
        let attrs: [NSAttributedString.Key: Any] = [
            .font: UIFont.monospacedSystemFont(ofSize: 11, weight: .semibold),
            .foregroundColor: Palette.label,
        ]
        let size = (text as NSString).size(withAttributes: attrs)
        (text as NSString).draw(at: CGPoint(x: centeredX - size.width / 2, y: bounds.minY + 6),
                                withAttributes: attrs)
    }

    private func blend(_ a: UIColor, _ b: UIColor, t: Double) -> UIColor {
        var ar: CGFloat = 0, ag: CGFloat = 0, ab: CGFloat = 0, aa: CGFloat = 0
        var br: CGFloat = 0, bg: CGFloat = 0, bb: CGFloat = 0, ba: CGFloat = 0
        a.getRed(&ar, green: &ag, blue: &ab, alpha: &aa)
        b.getRed(&br, green: &bg, blue: &bb, alpha: &ba)
        let f = CGFloat(max(0, min(t, 1)))
        return UIColor(red: ar + (br - ar) * f, green: ag + (bg - ag) * f,
                       blue: ab + (bb - ab) * f, alpha: 1)
    }
}
