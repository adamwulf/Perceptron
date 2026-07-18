//
//  KnobControl.swift
//  PerceptronDemo
//
//  Port of KnobControl.cs — a rotary dial (-30…30). Zero points straight
//  up; the indicator sweeps -225°…+45°. Turned with a pan gesture:
//  drag up (or right) to increase, down (or left) to decrease, matching
//  the desktop drag math. Owner-drawn with Core Graphics.
//

import UIKit

final class KnobControl: UIView {

    var onValueChanged: (() -> Void)?

    var minValue: Double = -30
    var maxValue: Double = 30
    var step: Double = 0.05
    var valueFormat = "%.2f"

    private var _value: Double = 0
    var value: Double {
        get { _value }
        set {
            let clamped = newValue.clamped(to: minValue...maxValue)
            if abs(_value - clamped) > 0.0001 {
                _value = clamped
                setNeedsDisplay()
                onValueChanged?()
            }
        }
    }

    private var lastPanPoint: CGPoint = .zero

    override init(frame: CGRect) {
        super.init(frame: frame)
        commonInit()
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        commonInit()
    }

    private func commonInit() {
        backgroundColor = .clear
        isOpaque = false
        let pan = UIPanGestureRecognizer(target: self, action: #selector(handlePan(_:)))
        addGestureRecognizer(pan)
    }

    // MARK: - Interaction

    @objc private func handlePan(_ gr: UIPanGestureRecognizer) {
        switch gr.state {
        case .began:
            lastPanPoint = gr.location(in: self)
        case .changed:
            let p = gr.location(in: self)
            // Desktop: delta = (dragUp + dragRight) * 0.3, where the value
            // then advances by `delta` weight units directly.
            let deltaY = lastPanPoint.y - p.y   // up is positive
            let deltaX = p.x - lastPanPoint.x   // right is positive
            let delta = Double(deltaY + deltaX) * 0.3
            value += delta
            lastPanPoint = p
        default:
            break
        }
    }

    // MARK: - Drawing

    override func draw(_ rect: CGRect) {
        guard let ctx = UIGraphicsGetCurrentContext() else { return }
        let w = bounds.width
        let h = bounds.height

        let knobY: CGFloat = 5
        let knobSize = min(w - 10, h - 30)
        let knobX = (w - knobSize) / 2
        let center = CGPoint(x: knobX + knobSize / 2, y: knobY + knobSize / 2)
        let knobRect = CGRect(x: knobX, y: knobY, width: knobSize, height: knobSize)

        drawTickMarks(ctx, center: center, radius: knobSize / 2 + 2)

        // Knob shadow.
        ctx.setFillColor(UIColor(white: 20/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: knobRect.offsetBy(dx: 2, dy: 2))

        // Knob body — radial gradient with an off-center highlight (backlit look).
        ctx.saveGState()
        ctx.addEllipse(in: knobRect)
        ctx.clip()
        let colors = [
            UIColor(white: 90/255, alpha: 1).cgColor,
            UIColor(white: 40/255, alpha: 1).cgColor
        ] as CFArray
        if let gradient = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(),
                                     colors: colors, locations: [0, 1]) {
            let highlight = CGPoint(x: knobX + knobSize * 0.4, y: knobY + knobSize * 0.3)
            ctx.drawRadialGradient(gradient,
                                   startCenter: highlight, startRadius: 0,
                                   endCenter: center, endRadius: knobSize / 2,
                                   options: [])
        }
        ctx.restoreGState()

        // Knob border.
        ctx.setStrokeColor(UIColor(white: 80/255, alpha: 1).cgColor)
        ctx.setLineWidth(2)
        ctx.strokeEllipse(in: knobRect)

        // Indicator line — 0 points straight up.
        let normalized = (_value - minValue) / (maxValue - minValue)
        let angle = CGFloat(-225 + normalized * 270) * .pi / 180
        let lineLength = knobSize / 2 - 6
        let lineEnd = CGPoint(x: center.x + cos(angle) * lineLength,
                              y: center.y + sin(angle) * lineLength)
        ctx.setStrokeColor(UIColor(white: 220/255, alpha: 1).cgColor)
        ctx.setLineWidth(3)
        ctx.setLineCap(.round)
        ctx.move(to: center)
        ctx.addLine(to: lineEnd)
        ctx.strokePath()

        // Center dot.
        let dotSize: CGFloat = 8
        ctx.setFillColor(UIColor(white: 60/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: CGRect(x: center.x - dotSize / 2, y: center.y - dotSize / 2,
                                   width: dotSize, height: dotSize))

        // Value text below the knob.
        let text = String(format: valueFormat, _value)
        let attrs: [NSAttributedString.Key: Any] = [
            .font: UIFont.monospacedSystemFont(ofSize: 11, weight: .regular),
            .foregroundColor: UIColor(white: 180/255, alpha: 1)
        ]
        let size = (text as NSString).size(withAttributes: attrs)
        (text as NSString).draw(at: CGPoint(x: (w - size.width) / 2, y: knobY + knobSize + 5),
                                withAttributes: attrs)
    }

    private func drawTickMarks(_ ctx: CGContext, center: CGPoint, radius: CGFloat) {
        for i in stride(from: -30, through: 30, by: 10) {
            let normalized = (Double(i) - minValue) / (maxValue - minValue)
            let angle = CGFloat(-225 + normalized * 270) * .pi / 180
            let isMajor = i % 30 == 0
            let innerR = radius + 4
            let outerR = radius + (isMajor ? 10 : 7)
            let p1 = CGPoint(x: center.x + cos(angle) * innerR, y: center.y + sin(angle) * innerR)
            let p2 = CGPoint(x: center.x + cos(angle) * outerR, y: center.y + sin(angle) * outerR)
            ctx.setStrokeColor(UIColor(white: isMajor ? 100/255 : 80/255, alpha: 1).cgColor)
            ctx.setLineWidth(isMajor ? 2 : 1)
            ctx.move(to: p1)
            ctx.addLine(to: p2)
            ctx.strokePath()
        }
    }
}
