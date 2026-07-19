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

    /// The drawn triangle keeps this size regardless of how large the view's
    /// frame is, so the touch target can grow (easier to hit on iPad) without
    /// the arrow itself getting bigger. Set to the visual size the layout wants.
    var arrowSize = CGSize(width: 40, height: 16) { didSet { setNeedsDisplay() } }

    /// Where the triangle is drawn, in this view's own coordinates. When nil the
    /// triangle is centered in `bounds`. The layout sets this when the frame is
    /// padded asymmetrically (e.g. clamped away from the center button) so the
    /// triangle still renders at its intended on-screen position.
    var arrowCenter: CGPoint? { didSet { setNeedsDisplay() } }

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
        // Draw the triangle at a fixed visual size, positioned at `arrowCenter`
        // (or the bounds center when unset). The extra bounds beyond this rect
        // are hit area only.
        let drawW = min(arrowSize.width, bounds.width) - inset * 2
        let drawH = min(arrowSize.height, bounds.height) - inset * 2
        let center = arrowCenter ?? CGPoint(x: bounds.midX, y: bounds.midY)
        let r = CGRect(x: center.x - drawW / 2,
                       y: center.y - drawH / 2,
                       width: drawW, height: drawH)

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
