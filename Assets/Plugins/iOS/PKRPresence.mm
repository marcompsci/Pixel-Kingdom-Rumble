// Pixel Kingdom Rumble - live player counts for the Kingdom Plaza, from Game Center.
// Each place (the plaza, each world building, each party quest) has a Game Center player group. The game announces
// itself in the place it's in with a short matchmaking request (cancelled after a few seconds, nobody is matched into
// anything) and asks Game Center how many players it saw in each group recently (queryPlayerGroupActivity).
// No server of ours, nothing else is sent. Called from LivePresence.cs via [DllImport("__Internal")].
#import <GameKit/GameKit.h>

static NSMutableDictionary<NSNumber *, NSNumber *> *pkrPresenceCounts;
static BOOL pkrPresenceAnnouncing;

static BOOL PKRPresenceReady()
{
    return [GKLocalPlayer localPlayer].isAuthenticated;
}

extern "C" bool PKR_Presence_Ready() { return PKRPresenceReady(); }

extern "C" void PKR_Presence_Query(int group)
{
    if (!PKRPresenceReady()) return;
    if (pkrPresenceCounts == nil) pkrPresenceCounts = [NSMutableDictionary dictionary];
    [[GKMatchmaker sharedMatchmaker] queryPlayerGroupActivity:(NSUInteger)group withCompletionHandler:^(NSInteger activity, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error == nil) pkrPresenceCounts[@(group)] = @(activity);
        });
    }];
}

// -1 = not known yet (or Game Center unavailable).
extern "C" int PKR_Presence_Count(int group)
{
    NSNumber *n = pkrPresenceCounts[@(group)];
    return n != nil ? n.intValue : -1;
}

extern "C" void PKR_Presence_Announce(int group)
{
    if (!PKRPresenceReady() || pkrPresenceAnnouncing) return;
    pkrPresenceAnnouncing = YES;
    GKMatchRequest *req = [[GKMatchRequest alloc] init];
    req.minPlayers = 2;
    req.maxPlayers = 2;
    req.playerGroup = (NSUInteger)group;
    [[GKMatchmaker sharedMatchmaker] findPlayersForHostedRequest:req withCompletionHandler:^(NSArray<GKPlayer *> *players, NSError *error) {
        // Ignored on purpose: this request only marks "someone is here" for the counts.
    }];
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(6 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        [[GKMatchmaker sharedMatchmaker] cancel];
        pkrPresenceAnnouncing = NO;
    });
}
