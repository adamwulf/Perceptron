//
//  SwitchGrid.swift
//  PerceptronDemo
//
//  Shared geometry and pattern helpers for the N×N input switch grid. Both the
//  main panel and the Signal Flow screen use this so the switches are drawn at
//  the same size on both screens and the joystick shifts them the same way.
//

import UIKit

enum SwitchGrid {

    /// Sizes for one grid, derived from the column width it has to fit into.
    /// The caps (`maxCell` / `maxSwitchWidth`) are what keep the switches the
    /// same size on every screen once there's enough room for them.
    struct Metrics {
        static let maxCell: CGFloat = 72
        static let maxSwitchWidth: CGFloat = 56
        static let rowPitchFactor: CGFloat = 1.15
        /// Switch height as a multiple of its width (the toggle plus its LED).
        static let switchAspect: CGFloat = 1.4

        let gridSize: Int
        let cell: CGFloat
        let rowPitch: CGFloat
        let switchSize: CGSize

        init(columnWidth: CGFloat, gridSize: Int) {
            self.gridSize = gridSize
            cell = min(columnWidth / CGFloat(gridSize), Metrics.maxCell)
            rowPitch = cell * Metrics.rowPitchFactor
            let width = min(cell - 6, Metrics.maxSwitchWidth)
            switchSize = CGSize(width: width, height: width * Metrics.switchAspect)
        }

        var width: CGFloat { cell * CGFloat(gridSize) }
        /// Full height, including the last row's overhang past the row pitch.
        var height: CGFloat { rowPitch * CGFloat(gridSize - 1) + switchSize.height }
    }

    /// Frames `switches` in a `gridSize`×`gridSize` grid whose top-left corner
    /// is (`x`, `y`), using `metrics` for the cell sizes.
    static func layout(_ switches: [SwitchControl], metrics: Metrics, x: CGFloat, y: CGFloat) {
        for (i, sw) in switches.enumerated() {
            let r = i / metrics.gridSize, c = i % metrics.gridSize
            sw.frame = CGRect(
                x: x + CGFloat(c) * metrics.cell + (metrics.cell - metrics.switchSize.width) / 2,
                y: y + CGFloat(r) * metrics.rowPitch,
                width: metrics.switchSize.width,
                height: metrics.switchSize.height)
        }
    }

    /// Shifts the ON pattern by (`dx`, `dy`) cells. Anything pushed off an edge
    /// is dropped — the pattern does not wrap.
    static func shift(_ switches: [SwitchControl], gridSize: Int, dx: Int, dy: Int) {
        var grid = Array(repeating: Array(repeating: false, count: gridSize), count: gridSize)
        for (i, sw) in switches.enumerated() {
            grid[i / gridSize][i % gridSize] = sw.isOn
        }
        var newGrid = Array(repeating: Array(repeating: false, count: gridSize), count: gridSize)
        for r in 0..<gridSize {
            for c in 0..<gridSize {
                let sr = r - dy, sc = c - dx
                if sr >= 0, sr < gridSize, sc >= 0, sc < gridSize {
                    newGrid[r][c] = grid[sr][sc]
                }
            }
        }
        for (i, sw) in switches.enumerated() {
            sw.isOn = newGrid[i / gridSize][i % gridSize]
        }
    }

    /// If any switch is off, turns them all on; otherwise turns them all off.
    static func toggleAll(_ switches: [SwitchControl]) {
        let turnOn = switches.contains { !$0.isOn }
        switches.forEach { $0.isOn = turnOn }
    }
}
