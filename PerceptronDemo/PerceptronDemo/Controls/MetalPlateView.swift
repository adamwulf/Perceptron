//
//  MetalPlateView.swift
//  PerceptronDemo
//
//  Ports of MetalPlateControl.cs and MetalLabelControl.cs — brushed-metal
//  plates with beveled edges, corner screws, and engraved text. Used for
//  the OUTPUT formula, the OPERATING PROCEDURE instructions, and the small
//  "Off=-1v On=+1v" / "BIAS" labels. Owner-drawn with Core Graphics.
//

import UIKit

// MARK: - Shared drawing helpers

private enum MetalPlate {
    static func fillBrushedBackground(_ ctx: CGContext, rect: CGRect, corner: CGFloat,
                                      top: UIColor, bottom: UIColor) {
        let path = UIBezierPath(roundedRect: rect, cornerRadius: corner)
        ctx.saveGState()
        ctx.addPath(path.cgPath)
        ctx.clip()
        let colors = [top.cgColor, bottom.cgColor] as CFArray
        if let gradient = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(),
                                     colors: colors, locations: [0, 1]) {
            ctx.drawLinearGradient(gradient,
                                   start: CGPoint(x: rect.midX, y: rect.minY),
                                   end: CGPoint(x: rect.midX, y: rect.maxY),
                                   options: [])
        }
        ctx.restoreGState()
    }

    static func drawScrew(_ ctx: CGContext, x: CGFloat, y: CGFloat, size: CGFloat, slot: Bool) {
        ctx.setFillColor(UIColor(red: 25/255, green: 30/255, blue: 25/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: CGRect(x: x, y: y, width: size, height: size))
        ctx.setFillColor(UIColor(red: 55/255, green: 60/255, blue: 55/255, alpha: 1).cgColor)
        ctx.fillEllipse(in: CGRect(x: x + 1, y: y + 1, width: size - 2, height: size - 2))
        if slot {
            ctx.setStrokeColor(UIColor(red: 30/255, green: 35/255, blue: 30/255, alpha: 1).cgColor)
            ctx.setLineWidth(1)
            ctx.move(to: CGPoint(x: x + 1, y: y + size / 2))
            ctx.addLine(to: CGPoint(x: x + size - 1, y: y + size / 2))
            ctx.strokePath()
        }
    }
}

// MARK: - Multi-line instruction / formula plate

final class MetalPlateView: UIView {

    var lines: [String] = [] { didSet { setNeedsDisplay() } }

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
        let plate = CGRect(x: 2, y: 2, width: bounds.width - 4, height: bounds.height - 4)

        MetalPlate.fillBrushedBackground(
            ctx, rect: plate, corner: 4,
            top: UIColor(red: 75/255, green: 80/255, blue: 75/255, alpha: 1),
            bottom: UIColor(red: 55/255, green: 60/255, blue: 55/255, alpha: 1))

        // Brushed-metal horizontal striations.
        ctx.setStrokeColor(UIColor(white: 1, alpha: 15/255).cgColor)
        ctx.setLineWidth(1)
        var y = plate.minY + 4
        while y < plate.maxY - 4 {
            ctx.move(to: CGPoint(x: plate.minX + 4, y: y))
            ctx.addLine(to: CGPoint(x: plate.maxX - 4, y: y))
            ctx.strokePath()
            y += 3
        }

        // Border.
        ctx.setStrokeColor(UIColor(red: 40/255, green: 45/255, blue: 40/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.addPath(UIBezierPath(roundedRect: plate, cornerRadius: 4).cgPath)
        ctx.strokePath()

        // Corner screws.
        MetalPlate.drawScrew(ctx, x: plate.minX + 8, y: plate.minY + 8, size: 6, slot: true)
        MetalPlate.drawScrew(ctx, x: plate.maxX - 14, y: plate.minY + 8, size: 6, slot: true)
        MetalPlate.drawScrew(ctx, x: plate.minX + 8, y: plate.maxY - 14, size: 6, slot: true)
        MetalPlate.drawScrew(ctx, x: plate.maxX - 14, y: plate.maxY - 14, size: 6, slot: true)

        // Engraved text.
        let font = UIFont.monospacedSystemFont(ofSize: 11, weight: .bold)
        var textY = plate.minY + 20
        let textX = plate.minX + 20
        for line in lines {
            let shadow: [NSAttributedString.Key: Any] = [
                .font: font, .foregroundColor: UIColor(red: 25/255, green: 30/255, blue: 25/255, alpha: 1)]
            let light: [NSAttributedString.Key: Any] = [
                .font: font, .foregroundColor: UIColor(red: 180/255, green: 185/255, blue: 175/255, alpha: 1)]
            (line as NSString).draw(at: CGPoint(x: textX + 1, y: textY + 1), withAttributes: shadow)
            (line as NSString).draw(at: CGPoint(x: textX, y: textY), withAttributes: light)
            textY += 16
        }
    }
}

// MARK: - Small single-line label plate

final class MetalLabelView: UIView {

    var text = "" { didSet { setNeedsDisplay() } }
    var textColor: UIColor? = nil { didSet { setNeedsDisplay() } }

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
        let plate = CGRect(x: 0, y: 0, width: bounds.width - 1, height: bounds.height - 1)

        MetalPlate.fillBrushedBackground(
            ctx, rect: plate, corner: 3,
            top: UIColor(red: 70/255, green: 75/255, blue: 70/255, alpha: 1),
            bottom: UIColor(red: 50/255, green: 55/255, blue: 50/255, alpha: 1))

        // Border.
        ctx.setStrokeColor(UIColor(red: 40/255, green: 45/255, blue: 40/255, alpha: 1).cgColor)
        ctx.setLineWidth(1)
        ctx.addPath(UIBezierPath(roundedRect: plate, cornerRadius: 3).cgPath)
        ctx.strokePath()

        // Corner screws.
        MetalPlate.drawScrew(ctx, x: plate.minX + 3, y: plate.minY + 3, size: 3, slot: false)
        MetalPlate.drawScrew(ctx, x: plate.maxX - 6, y: plate.minY + 3, size: 3, slot: false)
        MetalPlate.drawScrew(ctx, x: plate.minX + 3, y: plate.maxY - 6, size: 3, slot: false)
        MetalPlate.drawScrew(ctx, x: plate.maxX - 6, y: plate.maxY - 6, size: 3, slot: false)

        // Centered engraved text.
        let font = UIFont.monospacedSystemFont(ofSize: 11, weight: .bold)
        let size = (text as NSString).size(withAttributes: [.font: font])
        let tx = (bounds.width - size.width) / 2
        let ty = (bounds.height - size.height) / 2
        let shadow: [NSAttributedString.Key: Any] = [
            .font: font, .foregroundColor: UIColor(red: 30/255, green: 35/255, blue: 30/255, alpha: 1)]
        let light: [NSAttributedString.Key: Any] = [
            .font: font,
            .foregroundColor: textColor ?? UIColor(red: 130/255, green: 135/255, blue: 130/255, alpha: 1)]
        (text as NSString).draw(at: CGPoint(x: tx + 1, y: ty + 1), withAttributes: shadow)
        (text as NSString).draw(at: CGPoint(x: tx, y: ty), withAttributes: light)
    }
}
