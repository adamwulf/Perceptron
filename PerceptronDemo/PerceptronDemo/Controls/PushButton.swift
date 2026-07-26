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
    /// Optional SF Symbol engraved in the button face instead of `labelText`.
    var symbolName: String? { didSet { setNeedsDisplay() } }

    /// When set, a single tap opens this menu (the menu *is* the primary
    /// action — no long press). Implemented with a transparent `UIButton`
    /// overlay because `showsMenuAsPrimaryAction` is a `UIButton` feature and
    /// this control is owner-drawn; the overlay drives our pressed state so the
    /// backlight still brightens while the menu is up. `onTap` is not called
    /// while a menu is set.
    var menu: UIMenu? { didSet { updateMenuOverlay() } }

    private var isPressed = false { didSet { setNeedsDisplay() } }
    private var menuOverlay: MenuOverlayButton?

    override var accessibilityLabel: String? {
        didSet { menuOverlay?.accessibilityLabel = accessibilityLabel }
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

        // Face: an engraved symbol if one is set, otherwise the text label.
        if let symbolName,
           let symbol = UIImage(
               systemName: symbolName,
               withConfiguration: UIImage.SymbolConfiguration(
                   pointSize: min(body.width, body.height) * 0.45, weight: .regular))?
               .withTintColor(UIColor(white: 210/255, alpha: 1), renderingMode: .alwaysOriginal) {
            symbol.draw(at: CGPoint(x: bounds.midX - symbol.size.width / 2,
                                    y: bounds.midY - symbol.size.height / 2))
        } else if !labelText.isEmpty {
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

    // MARK: - Menu

    /// A `UIButton` that reports its highlight state, so the owner-drawn body
    /// underneath can light up while the menu is being pressed/shown.
    private final class MenuOverlayButton: UIButton {
        var onHighlightChanged: ((Bool) -> Void)?
        override var isHighlighted: Bool {
            didSet { onHighlightChanged?(isHighlighted) }
        }
    }

    private func updateMenuOverlay() {
        guard let menu else {
            menuOverlay?.removeFromSuperview()
            menuOverlay = nil
            isPressed = false
            return
        }

        let overlay: MenuOverlayButton
        if let existing = menuOverlay {
            overlay = existing
        } else {
            overlay = MenuOverlayButton(type: .custom)
            overlay.showsMenuAsPrimaryAction = true
            overlay.onHighlightChanged = { [weak self] pressed in self?.isPressed = pressed }
            overlay.frame = bounds
            addSubview(overlay)
            menuOverlay = overlay
        }
        overlay.menu = menu
        overlay.accessibilityLabel = accessibilityLabel
    }

    override func layoutSubviews() {
        super.layoutSubviews()
        menuOverlay?.frame = bounds
    }

    // MARK: - Touches

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
