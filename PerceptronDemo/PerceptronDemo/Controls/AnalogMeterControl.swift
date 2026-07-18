//
//  AnalogMeterControl.swift
//  PerceptronDemo
//
//  Port of AnalogMeterControl.cs — a vintage gauge (-100…100) with a red
//  needle that eases toward its target value. Owner-drawn with Core
//  Graphics; animated via a display-link.
//

import UIKit

final class AnalogMeterControl: UIView {

    var minValue: Double = -100
    var maxValue: Double = 100

    private var _value: Double = 0
    var value: Double {
        get { _value }
        set {
            _value = newValue.clamped(to: minValue...maxValue)
            startAnimation()
        }
    }

    private var displayValue: Double = 0
    private var displayLink: CADisplayLink?

    override init(frame: CGRect) {
        super.init(frame: frame)
        backgroundColor = .clear
        isOpaque = false
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        backgroundColor = .clear
        isOpaque = false
    }

    deinit { displayLink?.invalidate() }

    private func startAnimation() {
        if displayLink == nil {
            let link = CADisplayLink(target: self, selector: #selector(tick))
            link.add(to: .main, forMode: .common)
            displayLink = link
        }
    }

    @objc private func tick() {
        let diff = _value - displayValue
        if abs(diff) < 0.5 {
            displayValue = _value
            displayLink?.invalidate()
            displayLink = nil
        } else {
            displayValue += diff * 0.15
        }
        setNeedsDisplay()
    }

    override func draw(_ rect: CGRect) {
        guard let ctx = UIGraphicsGetCurrentContext() else { return }
        let w = bounds.width
        let h = bounds.height

        // Meter background.
        let meterRect = CGRect(x: 5, y: 5, width: w - 10, height: h - 10)
        let bgPath = UIBezierPath(roundedRect: meterRect, cornerRadius: 8)
        ctx.setFillColor(UIColor(white: 25/255, alpha: 1).cgColor)
        ctx.addPath(bgPath.cgPath)
        ctx.fillPath()

        // Inner face.
        let faceRect = CGRect(x: 15, y: 15, width: w - 30, height: h - 50)
        let facePath = UIBezierPath(roundedRect: faceRect, cornerRadius: 6)
        ctx.setFillColor(UIColor(white: 35/255, alpha: 1).cgColor)
        ctx.addPath(facePath.cgPath)
        ctx.fillPath()

        // Border.
        ctx.setStrokeColor(UIColor(white: 70/255, alpha: 1).cgColor)
        ctx.setLineWidth(2)
        ctx.addPath(bgPath.cgPath)
        ctx.strokePath()

        let center = CGPoint(x: w / 2, y: h - 35)
        let radius = min(w, h) - 70

        drawScale(ctx, center: center, radius: radius)
        drawNeedle(ctx, center: center, radius: radius)

        // Center pivot.
        ctx.setFillColor(UIColor(white: 60/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: CGRect(x: center.x - 6, y: center.y - 6, width: 12, height: 12))
        ctx.setFillColor(UIColor(white: 90/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: CGRect(x: center.x - 3, y: center.y - 3, width: 6, height: 6))
    }

    private func drawScale(_ ctx: CGContext, center: CGPoint, radius: CGFloat) {
        // Arc: -150° through +120° sweep.
        ctx.setStrokeColor(UIColor(white: 80/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.addArc(center: center, radius: radius,
                   startAngle: -150 * .pi / 180, endAngle: -30 * .pi / 180, clockwise: false)
        ctx.strokePath()

        for i in stride(from: -100, through: 100, by: 10) {
            let normalized = (Double(i) - minValue) / (maxValue - minValue)
            let angle = CGFloat(-150 + normalized * 120) * .pi / 180
            let isMajor = i % 50 == 0
            let innerR = radius - (isMajor ? 15 : 10)
            let p1 = CGPoint(x: center.x + cos(angle) * innerR, y: center.y + sin(angle) * innerR)
            let p2 = CGPoint(x: center.x + cos(angle) * radius, y: center.y + sin(angle) * radius)
            ctx.setStrokeColor(UIColor(white: isMajor ? 140/255 : 100/255, alpha: 1).cgColor)
            ctx.setLineWidth(isMajor ? 2 : 1)
            ctx.move(to: p1)
            ctx.addLine(to: p2)
            ctx.strokePath()

            if isMajor {
                let text = "\(i)"
                let attrs: [NSAttributedString.Key: Any] = [
                    .font: UIFont.monospacedSystemFont(ofSize: 10, weight: .regular),
                    .foregroundColor: UIColor(white: 160/255, alpha: 1)
                ]
                let size = (text as NSString).size(withAttributes: attrs)
                let textR = innerR - 12
                let tx = center.x + cos(angle) * textR - size.width / 2
                let ty = center.y + sin(angle) * textR - size.height / 2
                (text as NSString).draw(at: CGPoint(x: tx, y: ty), withAttributes: attrs)
            }
        }
    }

    private func drawNeedle(_ ctx: CGContext, center: CGPoint, radius: CGFloat) {
        let normalized = (displayValue - minValue) / (maxValue - minValue)
        let angle = CGFloat(-150 + normalized * 120) * .pi / 180
        let needleLength = radius - 5
        let end = CGPoint(x: center.x + cos(angle) * needleLength,
                          y: center.y + sin(angle) * needleLength)

        // Needle body (red).
        ctx.setStrokeColor(UIColor(red: 200/255, green: 60/255, blue: 60/255, alpha: 1).cgColor)
        ctx.setLineWidth(2)
        ctx.setLineCap(.round)
        ctx.move(to: center)
        ctx.addLine(to: end)
        ctx.strokePath()

        // Counter-weight tail.
        let tailLength: CGFloat = 15
        let tail = CGPoint(x: center.x - cos(angle) * tailLength,
                           y: center.y - sin(angle) * tailLength)
        ctx.setLineWidth(4)
        ctx.move(to: center)
        ctx.addLine(to: tail)
        ctx.strokePath()
    }
}
