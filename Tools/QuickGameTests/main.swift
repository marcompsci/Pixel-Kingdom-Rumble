// Quick Game (widget) engine tests. Run on the Mac (unity_check.command does it):
//   xcrun swiftc -O Native/QuickGameWidget/QuickGameEngine.swift Tools/QuickGameTests/main.swift -o /tmp/pkr_qg && /tmp/pkr_qg
// Prints PASS/FAIL lines and "QUICKGAME TESTS: n passed, m failed".

import Foundation

var passed = 0, failed = 0
func check(_ ok: Bool, _ name: String) {
    if ok { passed += 1 } else { failed += 1; print("FAIL \(name)") }
}

// Every stage: 7 rows, one width, one start, one goal.
for (i, st) in QGStages.all.enumerated() {
    let widths = Set(st.rows.map { $0.count })
    check(st.rows.count == 7 && widths.count == 1, "stage \(st.id) shape")
    let joined = st.rows.joined()
    check(joined.filter { $0 == "P" }.count == 1, "stage \(st.id) has one start")
    check(joined.filter { $0 == "G" }.count == 1, "stage \(st.id) has one goal")
    check(QGEngine.totalShards(i) > 0, "stage \(st.id) has shards")
}

// Rules on a flat test stage.
let flat = QGEngine.start(stageIndex: 3) // Spike Meadow: flat ground with spikes
var s = flat
s = QGEngine.step(s, .right)
check(s.x == flat.x + 1 && s.y == flat.y, "step right moves one tile")
s = QGEngine.step(s, .left)
check(s.x == flat.x && s.facing == -1, "step left moves back and faces left")
s = QGEngine.step(s, .right)
let before = s
s = QGEngine.step(s, .jump)
check(s.y == before.y - 1 && s.x == before.x + 1 && s.jumps == 1, "jump goes up and forward")
s = QGEngine.step(s, .jump)
check(s.jumps == 2 && s.event == "double", "second jump in the air is a double jump")
let peak = s.y
s = QGEngine.step(s, .jump)
check(s.jumps == 2 && s.y <= peak, "no third jump")
var guardSteps = 0
while QGGrid(QGEngine.stage(3)).solid(s.x, s.y + 1) == false && guardSteps < 10 { s = QGEngine.step(s, .right); guardSteps += 1 }
check(s.jumps == 0, "landing resets the jumps")

// Pits cost a heart and return to the checkpoint.
var p = QGEngine.start(stageIndex: 0) // Meadow Hop: pit at x 6-7
for _ in 0..<5 { p = QGEngine.step(p, .right) }
p = QGEngine.step(p, .right) // walk off the ledge
for _ in 0..<4 { if p.event != "hurt" { p = QGEngine.step(p, .right) } }
check(p.hearts == QGEngine.maxHearts - 1 && p.x == p.checkX && p.y == p.checkY, "falling in a pit costs a heart and respawns")

// Out of hearts.
var l = QGEngine.start(stageIndex: 3)
var n = 0
while l.status == .playing && n < 200 { l = QGEngine.step(l, .right); n += 1 }
check(l.status == .lost && l.event == "lost", "walking into spikes three times loses")
let frozen = QGEngine.step(l, .right)
check(frozen == l, "a finished run ignores taps")

// Stats.
var stats = QGStats()
stats.played(day: "2026-11-01", yesterday: "2026-10-31")
stats.played(day: "2026-11-02", yesterday: "2026-11-01")
check(stats.streak == 2 && stats.bestStreak == 2, "streak counts days in a row")
stats.played(day: "2026-11-05", yesterday: "2026-11-04")
check(stats.streak == 1 && stats.bestStreak == 2, "a missed day restarts the streak")
var won = QGEngine.start(stageIndex: 0)
var wonAfter = won
wonAfter.status = .won; wonAfter.steps = 40; wonAfter.shards = 3; wonAfter.event = "won"
stats.record(won, wonAfter)
wonAfter.steps = 33
stats.record(won, wonAfter)
check(stats.clears == 2 && stats.shards == 6 && stats.bestSteps("meadow_hop") == 33, "clears, shards and best taps")
won.status = .won
stats.record(won, wonAfter)
check(stats.clears == 2, "an already-won run doesn't count twice")
let json = try! JSONEncoder().encode(stats)
let back = try! JSONDecoder().decode(QGStats.self, from: json)
check(back == stats, "stats round-trip through JSON")
let old = try! JSONDecoder().decode(QGStats.self, from: "{\"clears\":4}".data(using: .utf8)!)
check(old.clears == 4 && old.runs == 0 && old.best.isEmpty, "older stats with missing fields still load")

// Season tiles follow the game's seasons.
let cal = Calendar(identifier: .gregorian)
func day(_ m: Int, _ d: Int) -> Date { cal.date(from: DateComponents(year: 2026, month: m, day: d))! }
check(QGDay.theme(day(11, 26), seasons: true, calendar: cal) == "harvest", "Thanksgiving is harvest")
check(QGDay.theme(day(12, 25), seasons: true, calendar: cal) == "winter", "Christmas is winter")
check(QGDay.theme(day(5, 1), seasons: true, calendar: cal) == "meadow", "spring is meadow")
check(QGDay.theme(day(12, 25), seasons: false, calendar: cal) == "meadow", "seasons off is meadow")
check(QGDay.key(day(11, 1), calendar: cal) == "2026-11-01", "day key")

// Every stage can be cleared (breadth-first search over taps), within 80 taps.
struct Key: Hashable {
    let x, y, facing, rise, jumps, hearts, parity, cx, cy: Int
    let foes: [Int]
}
func key(_ s: QGState) -> Key {
    Key(x: s.x, y: s.y, facing: s.facing, rise: s.rise, jumps: s.jumps, hearts: s.hearts, parity: s.steps % 2,
        cx: s.checkX, cy: s.checkY, foes: s.foes.flatMap { [$0.x, $0.dir, $0.alive ? 1 : 0] })
}
for i in 0..<QGStages.all.count {
    let start = QGEngine.start(stageIndex: i)
    var queue = [start]
    var head = 0
    var seen: Set<Key> = [key(start)]
    var best = -1
    while head < queue.count && best < 0 {
        let cur = queue[head]; head += 1
        if cur.steps > 80 { continue }
        for input in [QGInput.right, .jump, .left] {
            let nx = QGEngine.step(cur, input)
            if nx.status == .won { best = nx.steps; break }
            if nx.status == .lost { continue }
            let k = key(nx)
            if !seen.contains(k) { seen.insert(k); queue.append(nx) }
        }
    }
    check(best > 0, "stage \(QGStages.all[i].id) can be cleared")
    if best > 0 { print("  \(QGStages.all[i].id): fewest taps \(best)") }
}

print("QUICKGAME TESTS: \(passed) passed, \(failed) failed")
exit(failed == 0 ? 0 : 1)
