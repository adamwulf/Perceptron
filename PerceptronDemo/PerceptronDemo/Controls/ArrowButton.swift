//
//  ArrowButton.swift
//  PerceptronDemo
//
//  Port of ArrowButton.cs — a flat triangular d-pad button used to shift
//  the switch pattern. Owner-drawn with Core Graphics; taps fire onTap.
//

import UIKit

enum ArrowDirection {
    case up, down, left, right
}

final class ArrowButton: UIView {

    var onTap: (() -> Void)?
    var direction: ArrowDirection = .up { didSet { setNeedsDisplay() } }
    var glowColor: UIColor = UIColor(red: 1, green: 220/255, blue: 80/255, alpha: 1)

    private var isPressed = false { didSet { setNeedsDisplay() } }

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
        let inset: CGFloat = 3
        let r = bounds.insetBy(dx: inset, dy: inset)

        // Triangle pointing in `direction`.
        let path = UIBezierPath()
        switch direction {
        case .up:
            path.move(to: CGPoint(x: r.midX, y: r.minY))
            path.addLine(to: CGPoint(x: r.maxX, y: r.maxY))
            path.addLine(to: CGPoint(x: r.minX, y: r.maxY))
        case .down:
            path.move(to: CGPoint(x: r.midX, y: r.maxY))
            path.addLine(to: CGPoint(x: r.maxX, y: r.minY))
            path.addLine(to: CGPoint(x: r.minX, y: r.minY))
        case .left:
            path.move(to: CGPoint(x: r.minX, y: r.midY))
            path.addLine(to: CGPoint(x: r.maxX, y: r.minY))
            path.addLine(to: CGPoint(x: r.maxX, y: r.maxY))
        case .right:
            path.move(to: CGPoint(x: r.maxX, y: r.midY))
            path.addLine(to: CGPoint(x: r.minX, y: r.minY))
            path.addLine(to: CGPoint(x: r.minX, y: r.maxY))
        }
        path.close()

        // Glow behind the triangle when pressed.
        if isPressed {
            ctx.setFillColor(glowColor.withAlphaComponent(0.35).cgColor)
            ctx.addPath(path.cgPath)
            ctx.fillPath()
        }

        let fill = isPressed
            ? glowColor.withAlphaComponent(0.9)
            : UIColor(white: 110/255, alpha: 1)
        ctx.setFillColor(fill.cgColor)
        ctx.addPath(path.cgPath)
        ctx.fillPath()

        ctx.setStrokeColor(UIColor(white: 70/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.addPath(path.cgPath)
        ctx.strokePath()
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
