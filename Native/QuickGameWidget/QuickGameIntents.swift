// Pixel Kingdom Rumble - Quick Game: the widget's buttons. Each tap runs one of these in the background (the app
// doesn't open), saves the new step, and WidgetKit redraws the widget.

import AppIntents
import WidgetKit

@available(iOS 17.0, *)
struct QGTapIntent: AppIntent {
    static var title: LocalizedStringResource = "Quick Game: move"
    static var isDiscoverable: Bool = false

    @Parameter(title: "Move")
    var move: String

    init() { move = QGInput.right.rawValue }
    init(_ input: QGInput) { move = input.rawValue }

    func perform() async throws -> some IntentResult {
        QGStore.apply(QGInput(rawValue: move) ?? .right)
        return .result()
    }
}

@available(iOS 17.0, *)
struct QGRestartIntent: AppIntent {
    static var title: LocalizedStringResource = "Quick Game: play again"
    static var isDiscoverable: Bool = false

    @Parameter(title: "Next stage")
    var next: Bool

    init() { next = false }
    init(next: Bool) { self.next = next }

    func perform() async throws -> some IntentResult {
        QGStore.restart(next: next)
        return .result()
    }
}
