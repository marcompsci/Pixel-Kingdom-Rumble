// Pixel Kingdom Rumble - Quick Game: the run in progress and the lifetime stats, kept in the App Group so the widget,
// its tap intents and the game itself all see the same thing. Nothing leaves the phone.

import Foundation

public enum QGStore {
    public static let group = "group.com.marcompsci.pixelkingdomrumble"
    static let stateKey = "qg.state"
    static let statsKey = "qg.stats"
    static let profileKey = "qg.profile"

    static var defaults: UserDefaults { UserDefaults(suiteName: group) ?? .standard }

    /// What the game tells the widget: which hero to draw and whether live seasons are on.
    public struct Profile: Codable {
        public var hero: String = "nova"
        public var seasons: Bool = true
    }

    public static func profile() -> Profile {
        guard let s = defaults.string(forKey: profileKey), let d = s.data(using: .utf8),
              let p = try? JSONDecoder().decode(Profile.self, from: d) else { return Profile() }
        return p
    }

    public static func loadState() -> QGState {
        if let s = defaults.string(forKey: stateKey), let d = s.data(using: .utf8),
           let st = try? JSONDecoder().decode(QGState.self, from: d) {
            return st
        }
        return QGEngine.start(stageIndex: QGStages.dailyIndex())
    }

    public static func loadStats() -> QGStats {
        guard let s = defaults.string(forKey: statsKey), let d = s.data(using: .utf8),
              let st = try? JSONDecoder().decode(QGStats.self, from: d) else { return QGStats() }
        return st
    }

    static func save(_ state: QGState, _ stats: QGStats) {
        let enc = JSONEncoder()
        if let d = try? enc.encode(state), let s = String(data: d, encoding: .utf8) { defaults.set(s, forKey: stateKey) }
        if let d = try? enc.encode(stats), let s = String(data: d, encoding: .utf8) { defaults.set(s, forKey: statsKey) }
    }

    /// One tap on the widget.
    public static func apply(_ input: QGInput, now: Date = Date()) {
        let before = loadState()
        guard before.status == .playing else { return }
        var stats = loadStats()
        if before.steps == 0 && stats.runs == 0 { stats.runs = 1 } // the very first run ever
        let after = QGEngine.step(before, input)
        stats.played(day: QGDay.key(now), yesterday: QGDay.yesterday(now))
        stats.record(before, after)
        save(after, stats)
    }

    /// TRY AGAIN (same stage) or NEXT STAGE (after a clear).
    public static func restart(next: Bool) {
        let cur = loadState()
        var stats = loadStats()
        stats.runs += 1
        save(QGEngine.start(stageIndex: next ? cur.stageIndex + 1 : cur.stageIndex), stats)
    }
}
