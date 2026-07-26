//
//  DPadControl.swift
//  PerceptronDemo
//
//  The switch-pattern "joystick": four triangular arrow buttons around a
//  square center button. Arrows shift the pattern one cell; the center button
//  toggles every switch on/off, and can also be swiped like a stick to shift.
//
//  Extracted so the main panel and the Signal Flow screen get the exact same
//  control rather than two hand-laid-out copies.
//

import UIKit

final class DPadControl: UIView {

    /// Shift the pattern by one cell. `dx`/`dy` are -1, 0, or +1 (screen axes,
    /// so +dy is down).
    var onShift: ((Int, Int) -> Void)?
    /// The center button was tapped — toggle all switches on (or all off).
    var onToggleAll: (() -> Void)?

    /// Smallest frame that fits the arrows plus their padded touch targets.
    /// Larger frames just add slack around the same drawn control.
    static let preferredSize = CGSize(width: 118, height: 118)

    // Fixed geometry, measured from the control's center.
    private static let arrowOffset: CGFloat = 35   // center → arrow center
    private static let hitPad: CGFloat = 16        // touch padding on each side
    private static let centerHalf: CGFloat = 23    // half the center button
    private static let hArrow = CGSize(width: 40, height: 16)          // up / down
    private static let vArrow = CGSize(width: 16, height: 40)          // left / right

    private let arrowUp = ArrowButton()
    private let arrowDown = ArrowButton()
    private let arrowLeft = ArrowButton()
    private let arrowRight = ArrowButton()
    private let centerToggle = PushButton()

    override init(frame: CGRect) {
        super.init(frame: frame)
        build()
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        build()
    }

    private func build() {
        backgroundColor = .clear

        arrowUp.direction = .up
        arrowUp.onTap = { [weak self] in self?.onShift?(0, -1) }
        arrowDown.direction = .down
        arrowDown.onTap = { [weak self] in self?.onShift?(0, 1) }
        arrowLeft.direction = .left
        arrowLeft.onTap = { [weak self] in self?.onShift?(-1, 0) }
        arrowRight.direction = .right
        arrowRight.onTap = { [weak self] in self?.onShift?(1, 0) }
        [arrowUp, arrowDown, arrowLeft, arrowRight].forEach { addSubview($0) }

        centerToggle.isSquare = true
        centerToggle.onTap = { [weak self] in self?.onToggleAll?() }
        addSubview(centerToggle)

        // Swipe the center button like a joystick to shift the pattern, in
        // addition to the arrow buttons. A plain tap still toggles all/none.
        for dir: UISwipeGestureRecognizer.Direction in [.up, .down, .left, .right] {
            let swipe = UISwipeGestureRecognizer(target: self, action: #selector(handleCenterSwipe(_:)))
            swipe.direction = dir
            centerToggle.addGestureRecognizer(swipe)
        }
    }

    override var intrinsicContentSize: CGSize { DPadControl.preferredSize }

    // MARK: - Layout

    override func layoutSubviews() {
        super.layoutSubviews()

        let centerX = bounds.midX
        let centerY = bounds.midY
        let centerHalf = DPadControl.centerHalf

        centerToggle.frame = CGRect(x: centerX - centerHalf, y: centerY - centerHalf,
                                    width: centerHalf * 2, height: centerHalf * 2)

        // Each arrow's triangle is drawn at its original visual size and in its
        // original position (its center sits `arrowOffset` from the d-pad
        // center). The tappable frame is inflated by `hitPad` so it's easy to
        // hit on iPad — the drawn triangle stays put, only the invisible touch
        // target grows. The edge facing the center button is clamped so the
        // enlarged area never overlaps it; `arrowCenter` keeps the triangle at
        // its true position even though the clamped frame is asymmetric.
        func padded(_ view: ArrowButton, size: CGSize, dir: ArrowDirection) {
            let arrowOffset = DPadControl.arrowOffset
            let hitPad = DPadControl.hitPad
            view.arrowSize = size
            let cx: CGFloat, cy: CGFloat
            var frame: CGRect
            switch dir {
            case .up:
                cx = centerX; cy = centerY - arrowOffset
                let bottom = centerY - centerHalf                // clamp to top of button
                let top = cy - size.height / 2 - hitPad
                frame = CGRect(x: cx - size.width / 2 - hitPad, y: top,
                               width: size.width + hitPad * 2, height: bottom - top)
            case .down:
                cx = centerX; cy = centerY + arrowOffset
                let top = centerY + centerHalf                   // clamp to bottom of button
                let bottom = cy + size.height / 2 + hitPad
                frame = CGRect(x: cx - size.width / 2 - hitPad, y: top,
                               width: size.width + hitPad * 2, height: bottom - top)
            case .left:
                cx = centerX - arrowOffset; cy = centerY
                let right = centerX - centerHalf                 // clamp to left of button
                let left = cx - size.width / 2 - hitPad
                frame = CGRect(x: left, y: cy - size.height / 2 - hitPad,
                               width: right - left, height: size.height + hitPad * 2)
            case .right:
                cx = centerX + arrowOffset; cy = centerY
                let left = centerX + centerHalf                  // clamp to right of button
                let right = cx + size.width / 2 + hitPad
                frame = CGRect(x: left, y: cy - size.height / 2 - hitPad,
                               width: right - left, height: size.height + hitPad * 2)
            }
            view.frame = frame
            view.arrowCenter = CGPoint(x: cx - frame.minX, y: cy - frame.minY)
        }
        padded(arrowUp,    size: DPadControl.hArrow, dir: .up)
        padded(arrowDown,  size: DPadControl.hArrow, dir: .down)
        padded(arrowLeft,  size: DPadControl.vArrow, dir: .left)
        padded(arrowRight, size: DPadControl.vArrow, dir: .right)
    }

    // MARK: - Gestures

    @objc private func handleCenterSwipe(_ gesture: UISwipeGestureRecognizer) {
        switch gesture.direction {
        case .up:    onShift?(0, -1)
        case .down:  onShift?(0, 1)
        case .left:  onShift?(-1, 0)
        case .right: onShift?(1, 0)
        default:     break
        }
    }
}
