//
//  PushButton.swift
//  PerceptronDemo
//
//  A compact backlit mechanical push button — a minimal stand-in for the
//  desktop's MechanicalPushButton.cs. Round or square, with an optional
//  glow color that brightens while pressed. Owner-drawn with Core Graphics.
//

import UIKit

final class PushButton: UIView {

    var onTap: (() -> Void)?
    var labelText = "" { didSet { setNeedsDisplay() } }
    var isSquare = false { didSet { setNeedsDisplay() } }
    /// nil = no backlight; otherwise the button glows in this color.
    var glowColor: UIColor? { didSet { setNeedsDisplay() } }

    private var isPressed = false { didSet { setNeedsDisplay() } }

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

    override func draw(_ rect: CGRect) {
        guard let ctx = UIGraphicsGetCurrentContext() else { return }
        let inset: CGFloat = 3
        let body = bounds.insetBy(dx: inset, dy: inset)
        let corner: CGFloat = isSquare ? 6 : body.width / 2

        // Body.
        let bodyPath = UIBezierPath(roundedRect: body, cornerRadius: corner)
        ctx.setFillColor(UIColor(white: isPressed ? 55/255 : 45/255, alpha: 1).cgColor)
        ctx.addPath(bodyPath.cgPath)
        ctx.fillPath()

        // Backlight glow.
        if let glow = glowColor {
            let alpha: CGFloat = isPressed ? 0.85 : 0.4
            ctx.saveGState()
            ctx.addPath(bodyPath.cgPath)
            ctx.clip()
            let colors = [glow.withAlphaComponent(alpha).cgColor,
                          glow.withAlphaComponent(0.04).cgColor] as CFArray
            if let gradient = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(),
                                         colors: colors, locations: [0, 1]) {
                let c = CGPoint(x: body.midX - body.width * 0.15, y: body.midY + body.height * 0.15)
                ctx.drawRadialGradient(gradient, startCenter: c, startRadius: 0,
                                       endCenter: CGPoint(x: body.midX, y: body.midY),
                                       endRadius: body.width * 0.7, options: [])
            }
            ctx.restoreGState()
        }

        // Border.
        ctx.setStrokeColor(UIColor(white: 80/255, alpha: 1).cgColor)
        ctx.setLineWidth(1.5)
        ctx.addPath(bodyPath.cgPath)
        ctx.strokePath()

        // Label.
        if !labelText.isEmpty {
            let font = UIFont.monospacedSystemFont(ofSize: 12, weight: .bold)
            let attrs: [NSAttributedString.Key: Any] = [
                .font: font,
                .foregroundColor: UIColor(white: 210/255, alpha: 1)
            ]
            let size = (labelText as NSString).size(withAttributes: attrs)
            (labelText as NSString).draw(
                at: CGPoint(x: (bounds.width - size.width) / 2, y: (bounds.height - size.height) / 2),
                withAttributes: attrs)
        }
    }

    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        super.touchesBegan(touches, with: event)
        isPressed = true
    }

    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
        super.touchesEnded(touches, with: event)
        isPressed = false
        if let point = touches.first?.location(in: self), bounds.contains(point) {
            onTap?()
        }
    }

    override func touchesCancelled(_ touches: Set<UITouch>, with event: UIEvent?) {
        super.touchesCancelled(touches, with: event)
        isPressed = false
    }
}
