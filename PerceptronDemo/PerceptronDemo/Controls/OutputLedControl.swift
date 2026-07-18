//
//  OutputLedControl.swift
//  PerceptronDemo
//
//  Port of OutputLedControl.cs — the large green OUTPUT indicator LED
//  with a soft glow when lit. Owner-drawn with Core Graphics.
//

import UIKit

final class OutputLedControl: UIView {

    var isOn: Bool = false {
        didSet {
            guard isOn != oldValue else { return }
            setNeedsDisplay()
        }
    }

    /// Caption drawn beneath the LED.
    var label = "OUTPUT" { didSet { setNeedsDisplay() } }

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
        let ledSize: CGFloat = 20
        let ledX = (bounds.width - ledSize) / 2
        let ledY: CGFloat = 5
        let ledRect = CGRect(x: ledX, y: ledY, width: ledSize, height: ledSize)

        // Layered glow when on.
        if isOn {
            ctx.setFillColor(UIColor(red: 0, green: 1, blue: 0, alpha: 30/255).cgColor)
            ctx.fillEllipse(in: ledRect.insetBy(dx: -8, dy: -8))
            ctx.setFillColor(UIColor(red: 0, green: 1, blue: 0, alpha: 50/255).cgColor)
            ctx.fillEllipse(in: ledRect.insetBy(dx: -4, dy: -4))
        }

        // LED body.
        let ledColor = isOn
            ? UIColor(red: 0, green: 220/255, blue: 0, alpha: 1)
            : UIColor(red: 0, green: 60/255, blue: 0, alpha: 1)
        ctx.setFillColor(ledColor.cgColor)
        ctx.fillEllipse(in: ledRect)

        // Specular highlight.
        if isOn {
            ctx.setFillColor(UIColor(white: 1, alpha: 100/255).cgColor)
            ctx.fillEllipse(in: CGRect(x: ledX + 4, y: ledY + 3, width: 6, height: 4))
        }

        // Border.
        ctx.setStrokeColor(UIColor(white: 100/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.strokeEllipse(in: ledRect)

        // Label.
        let attrs: [NSAttributedString.Key: Any] = [
            .font: UIFont.monospacedSystemFont(ofSize: 11, weight: .regular),
            .foregroundColor: UIColor(white: 140/255, alpha: 1)
        ]
        let size = (label as NSString).size(withAttributes: attrs)
        (label as NSString).draw(at: CGPoint(x: (bounds.width - size.width) / 2, y: ledY + ledSize + 5),
                                 withAttributes: attrs)
    }
}
