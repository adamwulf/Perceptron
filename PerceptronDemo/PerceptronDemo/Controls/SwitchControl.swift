//
//  SwitchControl.swift
//  PerceptronDemo
//
//  Port of SwitchControl.cs — a vintage toggle switch with an indicator
//  LED. Tap to flip. ON = +1, OFF = -1. Owner-drawn with Core Graphics.
//

import UIKit

final class SwitchControl: UIView {

    var onStateChanged: (() -> Void)?

    var isOn: Bool = false {
        didSet {
            guard isOn != oldValue else { return }
            setNeedsDisplay()
            onStateChanged?()
        }
    }

    /// +1 when ON, -1 when OFF (never 0) — matches the perceptron input convention.
    var value: Int { isOn ? 1 : -1 }

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

    override func draw(_ rect: CGRect) {
        guard let ctx = UIGraphicsGetCurrentContext() else { return }
        let w = bounds.width
        let h = bounds.height

        // Scale relative to the desktop's 50×80 design size.
        let scale = min(w / 50, h / 80)

        let ledSize = max(6, 12 * scale)
        let ledX = (w - ledSize) / 2
        let ledY = max(2, 4 * scale)
        let ledRect = CGRect(x: ledX, y: ledY, width: ledSize, height: ledSize)

        // LED glow when on (drawn under the LED body).
        if isOn {
            let glowPad = max(2, 3 * scale)
            ctx.setFillColor(UIColor(red: 1, green: 100/255, blue: 100/255, alpha: 40/255).cgColor)
            ctx.fillEllipse(in: ledRect.insetBy(dx: -glowPad, dy: -glowPad))
        }

        // LED body.
        let ledColor = isOn
            ? UIColor(red: 1, green: 60/255, blue: 60/255, alpha: 1)
            : UIColor(red: 60/255, green: 20/255, blue: 20/255, alpha: 1)
        ctx.setFillColor(ledColor.cgColor)
        ctx.fillEllipse(in: ledRect)

        // LED border.
        ctx.setStrokeColor(UIColor(white: 100/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.strokeEllipse(in: ledRect)

        // Switch track.
        let trackWidth = max(12, 24 * scale)
        let trackHeight = max(22, 44 * scale)
        let trackX = (w - trackWidth) / 2
        let trackY = ledY + ledSize + max(4, 8 * scale)
        let corner = max(2, 4 * scale)
        let trackRect = CGRect(x: trackX, y: trackY, width: trackWidth, height: trackHeight)

        let trackPath = UIBezierPath(roundedRect: trackRect, cornerRadius: corner)
        ctx.setFillColor(UIColor(white: 40/255, alpha: 1).cgColor)
        ctx.addPath(trackPath.cgPath)
        ctx.fillPath()

        ctx.setStrokeColor(UIColor(white: 70/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.addPath(trackPath.cgPath)
        ctx.strokePath()

        // Switch toggle — sits at top when ON, bottom when OFF.
        let togglePad = max(2, 3 * scale)
        let toggleHeight = max(9, 18 * scale)
        let toggleY = isOn
            ? trackY + togglePad
            : trackY + trackHeight - toggleHeight - togglePad
        let toggleRect = CGRect(x: trackX + togglePad,
                                y: toggleY,
                                width: trackWidth - togglePad * 2,
                                height: toggleHeight)
        let toggleColor: UIColor = isOn
            ? UIColor(white: 100/255, alpha: 1)
            : UIColor(white: 70/255, alpha: 1)
        let togglePath = UIBezierPath(roundedRect: toggleRect, cornerRadius: max(1, corner - 1))
        ctx.setFillColor(toggleColor.cgColor)
        ctx.addPath(togglePath.cgPath)
        ctx.fillPath()

        // Subtle highlight strip on the toggle.
        let hlPad = max(1, 2 * scale)
        let highlightAlpha: CGFloat = isOn ? 20/255 : 40/255
        ctx.setFillColor(UIColor(white: 1, alpha: highlightAlpha).cgColor)
        ctx.fill(CGRect(x: trackX + togglePad + hlPad,
                        y: toggleY + hlPad,
                        width: trackWidth - togglePad * 2 - hlPad * 2,
                        height: max(2, 3 * scale)))
    }

    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
        super.touchesEnded(touches, with: event)
        // Only toggle if the touch ended inside the view (standard tap semantics).
        if let point = touches.first?.location(in: self), bounds.contains(point) {
            isOn.toggle()
        }
    }
}
