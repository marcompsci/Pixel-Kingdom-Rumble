// Pixel Kingdom Rumble - Quick Game (the Story Quest widget game).
//
// A tiny step-based platformer. Every tap on the widget is one step of game time:
//   LEFT / RIGHT  step one tile that way (and face it)
//   JUMP          jump up and forward (2 tiles up); tap JUMP again in the air for a DOUBLE JUMP
// Each step the hero also falls one tile if nothing is under them, foes walk every other step, and touching a
// foe from above stomps it. Pits and spikes cost a heart and send the hero back to the last checkpoint.
//
// Pure Swift + Foundation: the widget, the app intents and the Mac tests (Tools/widget_tests.swift) all share it.
// Stages are 7 rows tall, top row first:  # ground   * Star Shard   F foe   ^ spikes   C checkpoint   P start   G goal

import Foundation

public enum QGInput: String, Codable, CaseIterable { case left, right, jump }
public enum QGStatus: String, Codable { case playing, won, lost }

public struct QGFoe: Codable, Equatable {
    public var x: Int
    public var y: Int
    public var dir: Int
    public var alive: Bool
}

public struct QGStage {
    public let id: String
    public let name: String
    public let rows: [String]
    public var width: Int { rows.first?.count ?? 0 }
    public var height: Int { rows.count }
}

/// One stage's tiles, with the rules for what is solid.
public struct QGGrid {
    public let width: Int
    public let height: Int
    private let cells: [[Character]]

    public init(_ stage: QGStage) {
        cells = stage.rows.map { Array($0) }
        width = cells.first?.count ?? 0
        height = cells.count
    }

    public func cell(_ x: Int, _ y: Int) -> Character {
        guard x >= 0, x < width, y >= 0, y < height else { return "." }
        return cells[y][x]
    }

    /// The left and right edges are walls; above the top is open sky; below the bottom is a pit.
    public func solid(_ x: Int, _ y: Int) -> Bool {
        if x < 0 || x >= width { return true }
        if y < 0 || y >= height { return false }
        return cells[y][x] == "#"
    }
}

public struct QGState: Codable, Equatable {
    public var stageIndex: Int
    public var stageId: String
    public var x: Int
    public var y: Int
    public var facing: Int
    public var rise: Int
    public var jumps: Int
    public var hearts: Int
    public var shards: Int
    public var stomps: Int
    public var steps: Int
    public var checkX: Int
    public var checkY: Int
    public var taken: [Int]
    public var foes: [QGFoe]
    public var status: QGStatus
    /// What just happened, for the widget's one-line message: shard, stomp, hurt, checkpoint, double, won, lost.
    public var event: String
}

public enum QGEngine {
    public static let maxHearts = 3

    public static func stage(_ index: Int) -> QGStage {
        let all = QGStages.all
        return all[((index % all.count) + all.count) % all.count]
    }

    public static func start(stageIndex: Int) -> QGState {
        let st = stage(stageIndex)
        let g = QGGrid(st)
        var px = 1, py = g.height - 2
        var foes: [QGFoe] = []
        for y in 0..<g.height {
            for x in 0..<g.width {
                let c = g.cell(x, y)
                if c == "P" { px = x; py = y }
                if c == "F" { foes.append(QGFoe(x: x, y: y, dir: -1, alive: true)) }
            }
        }
        let wrapped = ((stageIndex % QGStages.all.count) + QGStages.all.count) % QGStages.all.count
        return QGState(stageIndex: wrapped, stageId: st.id, x: px, y: py, facing: 1, rise: 0, jumps: 0,
                       hearts: maxHearts, shards: 0, stomps: 0, steps: 0, checkX: px, checkY: py,
                       taken: [], foes: foes, status: .playing, event: "")
    }

    public static func totalShards(_ stageIndex: Int) -> Int {
        var n = 0
        for row in stage(stageIndex).rows {
            for ch in row where ch == "*" { n += 1 }
        }
        return n
    }

    private static func hurt(_ s: inout QGState) {
        s.hearts -= 1
        if s.hearts <= 0 {
            s.status = .lost
            s.event = "lost"
        } else {
            s.x = s.checkX; s.y = s.checkY
            s.rise = 0; s.jumps = 0; s.facing = 1
            s.event = "hurt"
        }
    }

    /// One tap = one step of game time.
    public static func step(_ state: QGState, _ input: QGInput) -> QGState {
        var s = state
        guard s.status == .playing else { return s }
        let g = QGGrid(stage(s.stageIndex))
        s.event = ""
        s.steps += 1

        let grounded = g.solid(s.x, s.y + 1)
        var dx = 0
        switch input {
        case .left:
            s.facing = -1; dx = -1
        case .right:
            s.facing = 1; dx = 1
        case .jump:
            if grounded { s.rise = 2; s.jumps = 1 }
            else if s.jumps < 2 { s.rise = 2; s.jumps = 2; s.event = "double" }
            dx = s.facing
        }

        let fromX = s.x, fromY = s.y
        if dx != 0 && !g.solid(s.x + dx, s.y) { s.x += dx }
        var fell = false
        if s.rise > 0 {
            if s.y - 1 < 0 || g.solid(s.x, s.y - 1) { s.rise = 0 } else { s.y -= 1; s.rise -= 1 }
        } else if !g.solid(s.x, s.y + 1) {
            s.y += 1; fell = true
        }
        if s.rise == 0 && g.solid(s.x, s.y + 1) { s.jumps = 0 }

        if s.y >= g.height { hurt(&s); return s }          // fell into a pit
        let c = g.cell(s.x, s.y)
        if c == "^" { hurt(&s); return s }                  // spikes
        let idx = s.y * g.width + s.x
        if c == "*" && !s.taken.contains(idx) { s.taken.append(idx); s.shards += 1; s.event = "shard" }
        if c == "C" && (s.checkX != s.x || s.checkY != s.y) { s.checkX = s.x; s.checkY = s.y; s.event = "checkpoint" }
        if c == "G" { s.status = .won; s.event = "won"; return s }

        // Foes walk every other step and turn at walls and ledges.
        var movedFrom: [Int] = []
        for i in s.foes.indices {
            var f = s.foes[i]
            movedFrom.append(f.x)
            if f.alive && s.steps % 2 == 0 {
                var nx = f.x + f.dir
                if g.solid(nx, f.y) || !g.solid(nx, f.y + 1) {
                    f.dir = -f.dir
                    nx = f.x + f.dir
                    if g.solid(nx, f.y) || !g.solid(nx, f.y + 1) { nx = f.x }
                }
                f.x = nx
            }
            s.foes[i] = f
        }
        for i in s.foes.indices {
            let f = s.foes[i]
            guard f.alive else { continue }
            let same = f.x == s.x && f.y == s.y
            let crossed = f.y == s.y && f.y == fromY && f.x == fromX && movedFrom[i] == s.x
            if same || crossed {
                if fell || fromY < f.y {
                    s.foes[i].alive = false
                    s.stomps += 1
                    s.rise = 1; s.jumps = 1
                    s.event = "stomp"
                } else {
                    hurt(&s)
                    return s
                }
            }
        }
        return s
    }
}

/// The Quick Game's stages (all original). The day picks where the run starts; clearing one leads to the next.
public enum QGStages {
    public static let all: [QGStage] = [
        QGStage(id: "meadow_hop", name: "Meadow Hop", rows: [
            "....................................",
            "....................................",
            "...............*....................",
            "..........*.........*.......**......",
            "............................##....G.",
            ".P...*.......C.....F....*.........#.",
            "######..########.##########..#######",
        ]),
        QGStage(id: "acorn_steps", name: "Acorn Steps", rows: [
            "....................................",
            ".......................*............",
            "...................*..###...........",
            "..............*...###.......*.......",
            "..........*..###........F..###....G.",
            ".P.....C.###.........######.......#.",
            "#########...##########....##########",
        ]),
        QGStage(id: "beetle_bridge", name: "Beetle Bridge", rows: [
            "....................................",
            "....................................",
            "...........*...*...*................",
            "....................................",
            "..........F.........F......*.....G..",
            ".P...C..#############...C..###...#..",
            "#########...........######...#######",
        ]),
        QGStage(id: "spike_meadow", name: "Spike Meadow", rows: [
            "....................................",
            "....................................",
            ".........*.........*........*.......",
            "....................................",
            "........###.......###.......##...G..",
            ".P...C.......^^^.......^^^.......#..",
            "####################################",
        ]),
        QGStage(id: "high_road", name: "High Road", rows: [
            "....................................",
            "..............*..............*......",
            "...........######.........######....",
            "......*............................G",
            "....####.....................C...###",
            ".P...........F..........F.......####",
            "######....##########..#######..#####",
        ]),
        QGStage(id: "double_gap", name: "Double Gap", rows: [
            "....................................",
            "...........*.........*..............",
            "....................................",
            "....................................",
            "..............................*..G..",
            ".P...C........C.........C........#..",
            "#######......####......######.######",
        ]),
        QGStage(id: "tower_climb", name: "Tower Climb", rows: [
            ".......................*...........G",
            "...................######.....######",
            "..............*...................#.",
            "..........######.........F...######.",
            ".....*..............######..........",
            ".P..####....C.......................",
            "####################################",
        ]),
        QGStage(id: "last_light", name: "Last Light", rows: [
            "....................................",
            "..........*.............*...........",
            "......................####........G.",
            ".......F.####..^^..........F......#.",
            "....######....######...#######..###.",
            ".P...........................C......",
            "#####...####...........###########..",
        ]),
    ]

    /// Today's first stage (changes at local midnight, same for everyone on that day).
    public static func dailyIndex(_ date: Date = Date(), calendar: Calendar = .current) -> Int {
        let c = calendar.dateComponents([.year, .month, .day], from: date)
        let n = (c.year ?? 2026) * 372 + (c.month ?? 1) * 31 + (c.day ?? 1)
        return n % all.count
    }
}

/// Fewest taps to clear one stage.
public struct QGBest: Codable, Equatable {
    public var id: String
    public var steps: Int
}

/// Lifetime Quick Game stats. Saved by the widget in the App Group; the game reads them for Story Quest > STATS and
/// pays Star Shards for new clears. Plain fields and an array (no dictionaries) so Unity's JsonUtility can read it.
public struct QGStats: Codable, Equatable {
    public var runs = 0
    public var clears = 0
    public var losses = 0
    public var shards = 0
    public var stomps = 0
    public var taps = 0
    public var streak = 0
    public var bestStreak = 0
    public var lastDay = ""
    public var lastStage = ""
    public var best: [QGBest] = []

    public init() {}

    enum CodingKeys: String, CodingKey { case runs, clears, losses, shards, stomps, taps, streak, bestStreak, lastDay, lastStage, best }

    /// Missing fields (an older save) read as zero instead of throwing the whole record away.
    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        runs = try c.decodeIfPresent(Int.self, forKey: .runs) ?? 0
        clears = try c.decodeIfPresent(Int.self, forKey: .clears) ?? 0
        losses = try c.decodeIfPresent(Int.self, forKey: .losses) ?? 0
        shards = try c.decodeIfPresent(Int.self, forKey: .shards) ?? 0
        stomps = try c.decodeIfPresent(Int.self, forKey: .stomps) ?? 0
        taps = try c.decodeIfPresent(Int.self, forKey: .taps) ?? 0
        streak = try c.decodeIfPresent(Int.self, forKey: .streak) ?? 0
        bestStreak = try c.decodeIfPresent(Int.self, forKey: .bestStreak) ?? 0
        lastDay = try c.decodeIfPresent(String.self, forKey: .lastDay) ?? ""
        lastStage = try c.decodeIfPresent(String.self, forKey: .lastStage) ?? ""
        best = try c.decodeIfPresent([QGBest].self, forKey: .best) ?? []
    }

    public func bestSteps(_ id: String) -> Int? { best.first(where: { $0.id == id })?.steps }

    /// A day played (yyyy-MM-dd): keeps the streak of days in a row.
    public mutating func played(day: String, yesterday: String) {
        if lastDay == day { return }
        streak = lastDay == yesterday ? streak + 1 : 1
        bestStreak = max(bestStreak, streak)
        lastDay = day
    }

    /// Record what one tap did (before -> after).
    public mutating func record(_ before: QGState, _ after: QGState) {
        taps += 1
        lastStage = after.stageId
        if after.event == "stomp" { stomps += 1 }
        if before.status == .playing && after.status == .won {
            clears += 1
            shards += after.shards
            if let i = best.firstIndex(where: { $0.id == after.stageId }) {
                best[i].steps = min(best[i].steps, after.steps)
            } else {
                best.append(QGBest(id: after.stageId, steps: after.steps))
            }
        }
        if before.status == .playing && after.status == .lost { losses += 1 }
    }
}

public enum QGDay {
    public static func key(_ date: Date, calendar: Calendar = .current) -> String {
        let c = calendar.dateComponents([.year, .month, .day], from: date)
        return String(format: "%04d-%02d-%02d", c.year ?? 0, c.month ?? 0, c.day ?? 0)
    }

    public static func yesterday(_ date: Date, calendar: Calendar = .current) -> String {
        key(calendar.date(byAdding: .day, value: -1, to: date) ?? date, calendar: calendar)
    }

    /// The live-season tiles, matching the game's Story Quest seasons: harvest in autumn, snow in winter.
    public static func theme(_ date: Date, seasons: Bool, calendar: Calendar = .current) -> String {
        guard seasons else { return "meadow" }
        let c = calendar.dateComponents([.month, .day], from: date)
        let m = c.month ?? 1, d = c.day ?? 1
        if (m == 9 && d >= 22) || m == 10 || m == 11 { return "harvest" }
        if m == 12 || m == 1 || m == 2 { return "winter" }
        return "meadow"
    }
}
