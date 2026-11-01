// Pixel Kingdom Rumble - Quick Game widget. A small Story Quest stage on the Home Screen, played with taps:
// the left half steps left, the right half steps right, JUMP jumps (tap it again in the air to double jump).
// Small, Medium and Large sizes play; the Lock Screen one shows today's run and your stats.

import SwiftUI
import UIKit
import WidgetKit

struct QGEntry: TimelineEntry {
    let date: Date
    let state: QGState
    let stats: QGStats
    let hero: String
    let theme: String
}

struct QGProvider: TimelineProvider {
    static func sample() -> QGEntry {
        QGEntry(date: Date(), state: QGEngine.start(stageIndex: 0), stats: QGStats(), hero: "hero_nova", theme: "meadow")
    }

    static func current(_ date: Date = Date()) -> QGEntry {
        let p = QGStore.profile()
        let hero = UIImage(named: "hero_" + p.hero) != nil ? "hero_" + p.hero : "hero_nova"
        return QGEntry(date: date, state: QGStore.loadState(), stats: QGStore.loadStats(), hero: hero,
                       theme: QGDay.theme(date, seasons: p.seasons))
    }

    func placeholder(in context: Context) -> QGEntry { QGProvider.sample() }

    func getSnapshot(in context: Context, completion: @escaping (QGEntry) -> Void) {
        completion(context.isPreview ? QGProvider.sample() : QGProvider.current())
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<QGEntry>) -> Void) {
        let now = Date()
        // Taps redraw the widget straight away; otherwise refresh at midnight (season tiles, streak day).
        let tomorrow = Calendar.current.startOfDay(for: now.addingTimeInterval(86_400))
        completion(Timeline(entries: [QGProvider.current(now)], policy: .after(tomorrow)))
    }
}

// MARK: - Look

enum QGLook {
    static func sky(_ theme: String) -> [Color] {
        switch theme {
        case "harvest": return [Color(red: 0.95, green: 0.62, blue: 0.36), Color(red: 1.0, green: 0.86, blue: 0.62)]
        case "winter": return [Color(red: 0.16, green: 0.2, blue: 0.42), Color(red: 0.56, green: 0.62, blue: 0.86)]
        default: return [Color(red: 0.36, green: 0.6, blue: 0.95), Color(red: 0.74, green: 0.88, blue: 1.0)]
        }
    }

    static let ink = Color(red: 0.13, green: 0.09, blue: 0.15)
    static let gold = Color(red: 1.0, green: 0.84, blue: 0.35)

    static func font(_ size: CGFloat) -> Font { .system(size: size, weight: .heavy, design: .monospaced) }

    static func message(_ s: QGState) -> String {
        switch s.event {
        case "shard": return "+1 STAR SHARD"
        case "stomp": return "STOMP!"
        case "hurt": return "OUCH! BACK TO THE FLAG"
        case "double": return "DOUBLE JUMP!"
        case "checkpoint": return "CHECKPOINT!"
        default:
            return s.steps == 0 ? "TAP LEFT / RIGHT TO STEP · JUMP x2 = DOUBLE" : ""
        }
    }
}

struct QGSprite: View {
    let name: String
    let w: CGFloat
    let h: CGFloat
    var body: some View {
        Image(name).resizable().interpolation(.none).aspectRatio(contentMode: .fit).frame(width: w, height: h)
    }
}

/// The stage around the hero: tiles, shards, spikes, flags, foes and the hero, scrolled to keep the hero in view.
struct QGBoard: View {
    let state: QGState
    let hero: String
    let theme: String
    let columns: Int

    var body: some View {
        GeometryReader { geo in
            let g = QGGrid(QGEngine.stage(state.stageIndex))
            let cols = min(columns, g.width)
            let tile = min(geo.size.width / CGFloat(cols), geo.size.height / CGFloat(g.height))
            let cam = max(0, min(g.width - cols, state.x - cols / 3))
            let offX = (geo.size.width - tile * CGFloat(cols)) / 2
            let offY = geo.size.height - tile * CGFloat(g.height)
            ZStack(alignment: .topLeading) {
                ForEach(0..<g.height, id: \.self) { y in
                    ForEach(cam..<(cam + cols), id: \.self) { x in
                        cell(g, x, y, tile)
                            .position(x: offX + (CGFloat(x - cam) + 0.5) * tile, y: offY + (CGFloat(y) + 0.5) * tile)
                    }
                }
                ForEach(Array(state.foes.enumerated()), id: \.offset) { item in
                    let f = item.element
                    if f.alive && f.x >= cam && f.x < cam + cols {
                        QGSprite(name: "foe", w: tile * 0.95, h: tile * 0.95)
                            .scaleEffect(x: f.dir > 0 ? -1 : 1, y: 1)
                            .position(x: offX + (CGFloat(f.x - cam) + 0.5) * tile, y: offY + (CGFloat(f.y) + 0.55) * tile)
                    }
                }
                QGSprite(name: hero, w: tile * 1.25, h: tile * 1.25)
                    .scaleEffect(x: state.facing < 0 ? -1 : 1, y: 1)
                    .position(x: offX + (CGFloat(state.x - cam) + 0.5) * tile, y: offY + (CGFloat(state.y) + 0.42) * tile)
            }
        }
    }

    @ViewBuilder
    func cell(_ g: QGGrid, _ x: Int, _ y: Int, _ t: CGFloat) -> some View {
        let c = g.cell(x, y)
        if c == "#" {
            QGSprite(name: g.solid(x, y - 1) ? "\(theme)_fill" : "\(theme)_top", w: t + 0.5, h: t + 0.5)
        } else if c == "*" && !state.taken.contains(y * g.width + x) {
            QGSprite(name: "shard", w: t * 0.7, h: t * 0.7)
        } else if c == "^" {
            QGSprite(name: "spikes", w: t, h: t * 0.5).offset(y: t * 0.25)
        } else if c == "C" {
            QGSprite(name: "checkpoint", w: t * 0.6, h: t * 1.2).offset(y: -t * 0.1)
                .opacity(state.checkX == x && state.checkY == y ? 1 : 0.6)
        } else if c == "G" {
            QGSprite(name: "goal", w: t * 1.3, h: t * 1.9).offset(y: -t * 0.45)
        } else {
            Color.clear.frame(width: 1, height: 1)
        }
    }
}

// MARK: - Play views

struct QGHeader: View {
    let s: QGState
    let size: CGFloat
    var body: some View {
        HStack(spacing: 3) {
            ForEach(0..<QGEngine.maxHearts, id: \.self) { i in
                Image(systemName: i < s.hearts ? "heart.fill" : "heart")
                    .font(.system(size: size))
                    .foregroundColor(Color(red: 0.95, green: 0.25, blue: 0.3))
            }
            Spacer(minLength: 4)
            Text(QGEngine.stage(s.stageIndex).name.uppercased()).font(QGLook.font(size)).lineLimit(1).minimumScaleFactor(0.6)
            Spacer(minLength: 4)
            QGSprite(name: "shard", w: size + 2, h: size + 2)
            Text("\(s.shards)/\(QGEngine.totalShards(s.stageIndex))").font(QGLook.font(size)).monospacedDigit()
        }
        .foregroundColor(.white)
        .shadow(color: QGLook.ink, radius: 0, x: 1, y: 1)
    }
}

struct QGControls: View {
    let s: QGState
    let compact: Bool

    var body: some View {
        HStack(spacing: 0) {
            Button(intent: QGTapIntent(.left)) { zone("chevron.left", .leading) }.buttonStyle(.plain)
            Button(intent: QGTapIntent(.right)) { zone("chevron.right", .trailing) }.buttonStyle(.plain)
        }
        .overlay(alignment: .bottomTrailing) {
            Button(intent: QGTapIntent(.jump)) {
                Text(s.jumps == 1 ? "DOUBLE" : "JUMP")
                    .font(QGLook.font(compact ? 11 : 13))
                    .foregroundColor(QGLook.ink)
                    .padding(.horizontal, compact ? 8 : 12)
                    .padding(.vertical, compact ? 5 : 7)
                    .background(Capsule().fill(QGLook.gold))
                    .overlay(Capsule().stroke(QGLook.ink, lineWidth: 2))
            }
            .buttonStyle(.plain)
            .padding(compact ? 4 : 6)
        }
    }

    func zone(_ symbol: String, _ edge: Alignment) -> some View {
        ZStack(alignment: edge) {
            Rectangle().fill(Color.white.opacity(0.001))
            Image(systemName: symbol)
                .font(.system(size: compact ? 14 : 18, weight: .black))
                .foregroundColor(.white.opacity(0.45))
                .padding(.horizontal, 4)
        }
        .contentShape(Rectangle())
    }
}

struct QGEndCard: View {
    let s: QGState
    let compact: Bool
    var body: some View {
        let won = s.status == .won
        VStack(spacing: compact ? 3 : 6) {
            Text(won ? "STAGE CLEAR!" : "OUT OF HEARTS").font(QGLook.font(compact ? 13 : 17)).foregroundColor(won ? QGLook.gold : .white)
            Text("\(s.steps) TAPS · \(s.shards) SHARDS · \(s.stomps) STOMPS").font(QGLook.font(compact ? 8 : 10)).foregroundColor(.white.opacity(0.9))
                .lineLimit(1).minimumScaleFactor(0.6)
            Button(intent: QGRestartIntent(next: won)) {
                Text(won ? "NEXT STAGE" : "TRY AGAIN")
                    .font(QGLook.font(compact ? 11 : 13)).foregroundColor(QGLook.ink)
                    .padding(.horizontal, 12).padding(.vertical, 6)
                    .background(Capsule().fill(QGLook.gold))
                    .overlay(Capsule().stroke(QGLook.ink, lineWidth: 2))
            }
            .buttonStyle(.plain)
        }
        .padding(compact ? 6 : 10)
        .background(RoundedRectangle(cornerRadius: 10).fill(QGLook.ink.opacity(0.82)))
    }
}

struct QGPlayView: View {
    let entry: QGEntry
    let columns: Int
    let compact: Bool
    var large = false

    var body: some View {
        let s = entry.state
        VStack(spacing: 2) {
            QGHeader(s: s, size: compact ? 9 : 11)
                .padding(.horizontal, compact ? 8 : 12)
                .padding(.top, compact ? 7 : 9)
            if !compact {
                Text(QGLook.message(s)).font(QGLook.font(9)).foregroundColor(.white.opacity(0.9))
                    .shadow(color: QGLook.ink, radius: 0, x: 1, y: 1)
                    .frame(height: 11)
            }
            ZStack {
                QGBoard(state: s, hero: entry.hero, theme: entry.theme, columns: columns)
                if s.status == .playing {
                    QGControls(s: s, compact: compact)
                } else {
                    QGEndCard(s: s, compact: compact)
                }
            }
            if large {
                let best = entry.stats.bestSteps(s.stageId).map { "\($0) TAPS" } ?? "--"
                Text("CLEARS \(entry.stats.clears) · STREAK \(entry.stats.streak)d · BEST \(best)")
                    .font(QGLook.font(10)).foregroundColor(.white)
                    .shadow(color: QGLook.ink, radius: 0, x: 1, y: 1)
                    .padding(.bottom, 8)
            }
        }
    }
}

struct QGLockView: View {
    let entry: QGEntry
    var body: some View {
        let s = entry.state
        VStack(alignment: .leading, spacing: 1) {
            Text("QUICK GAME").font(.system(size: 13, weight: .heavy))
            Text("\(QGEngine.stage(s.stageIndex).name) · \(s.shards)/\(QGEngine.totalShards(s.stageIndex)) shards")
                .font(.system(size: 12, weight: .semibold)).lineLimit(1)
            Text("\(entry.stats.clears) clears · \(entry.stats.streak)-day streak").font(.system(size: 12)).lineLimit(1)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

struct QGWidgetView: View {
    @Environment(\.widgetFamily) var family
    let entry: QGEntry

    var body: some View {
        Group {
            switch family {
            case .accessoryRectangular: QGLockView(entry: entry)
            case .systemSmall: QGPlayView(entry: entry, columns: 7, compact: true)
            case .systemLarge: QGPlayView(entry: entry, columns: 12, compact: false, large: true)
            default: QGPlayView(entry: entry, columns: 15, compact: false)
            }
        }
        .containerBackground(for: .widget) {
            if family == .accessoryRectangular {
                Color.clear
            } else {
                LinearGradient(colors: QGLook.sky(entry.theme), startPoint: .top, endPoint: .bottom)
            }
        }
    }
}

@main
struct QGWidgetBundle: WidgetBundle {
    var body: some Widget { QGWidget() }
}

struct QGWidget: Widget {
    let kind = "PKRQuickGame"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: QGProvider()) { entry in
            QGWidgetView(entry: entry)
        }
        .configurationDisplayName("Quick Game")
        .description("A tiny Story Quest stage on your Home Screen. Tap left or right to step, JUMP to jump, and JUMP again in the air to double jump.")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge, .accessoryRectangular])
        .contentMarginsDisabled()
    }
}
