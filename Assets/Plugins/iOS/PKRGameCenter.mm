// Pixel Kingdom Rumble - Game Center bridge (GameKit, iOS 14+).
// Called from GameCenterService.cs via [DllImport("__Internal")]. Every call is safe before sign-in: GameKit
// simply ignores scores/achievements from a player who isn't authenticated, and the C# side queues them.
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController(void);

static int pkrAuthState = 0; // 0 = unknown/pending, 1 = signed in, 2 = unavailable/declined

@interface PKRGameCenterDelegate : NSObject <GKGameCenterControllerDelegate>
@end
@implementation PKRGameCenterDelegate
- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController *)gameCenterViewController
{
    [gameCenterViewController dismissViewControllerAnimated:YES completion:nil];
}
@end
static PKRGameCenterDelegate *pkrDelegate;

static NSString *PKRString(const char *s) { return s ? [NSString stringWithUTF8String:s] : @""; }

extern "C" void PKR_GC_Authenticate()
{
    GKLocalPlayer *player = [GKLocalPlayer localPlayer];
    player.authenticateHandler = ^(UIViewController *vc, NSError *error) {
        if (vc != nil)
        {
            [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
            return;
        }
        pkrAuthState = [GKLocalPlayer localPlayer].isAuthenticated ? 1 : 2;
        if (error != nil) NSLog(@"[PKR GameCenter] sign-in: %@", error.localizedDescription);
        else NSLog(@"[PKR GameCenter] signed in: %d", pkrAuthState == 1);
    };
}

extern "C" int PKR_GC_AuthState() { return pkrAuthState; }

extern "C" void PKR_GC_ReportScore(const char *leaderboardId, long long score)
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) return;
    NSString *board = PKRString(leaderboardId);
    [GKLeaderboard submitScore:(NSInteger)score context:0 player:[GKLocalPlayer localPlayer]
                leaderboardIDs:@[board] completionHandler:^(NSError *error) {
        if (error != nil) NSLog(@"[PKR GameCenter] score %@: %@", board, error.localizedDescription);
    }];
}

extern "C" void PKR_GC_ReportAchievement(const char *achievementId, double percent)
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) return;
    GKAchievement *a = [[GKAchievement alloc] initWithIdentifier:PKRString(achievementId)];
    a.percentComplete = percent;
    a.showsCompletionBanner = YES;
    [GKAchievement reportAchievements:@[a] withCompletionHandler:^(NSError *error) {
        if (error != nil) NSLog(@"[PKR GameCenter] achievement %@: %@", a.identifier, error.localizedDescription);
    }];
}

extern "C" void PKR_GC_ShowDashboard()
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { PKR_GC_Authenticate(); return; }
    if (pkrDelegate == nil) pkrDelegate = [[PKRGameCenterDelegate alloc] init];
    GKGameCenterViewController *vc = [[GKGameCenterViewController alloc] initWithState:GKGameCenterViewControllerStateDefault];
    vc.gameCenterDelegate = pkrDelegate;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

// A single leaderboard, friends-only or everyone (iOS 14+ GameKit view).
extern "C" void PKR_GC_ShowLeaderboard(const char *leaderboardId, int friendsOnly)
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { PKR_GC_Authenticate(); return; }
    if (pkrDelegate == nil) pkrDelegate = [[PKRGameCenterDelegate alloc] init];
    GKGameCenterViewController *vc = [[GKGameCenterViewController alloc]
        initWithLeaderboardID:PKRString(leaderboardId)
                  playerScope:(friendsOnly ? GKLeaderboardPlayerScopeFriendsOnly : GKLeaderboardPlayerScopeGlobal)
                    timeScope:GKLeaderboardTimeScopeAllTime];
    vc.gameCenterDelegate = pkrDelegate;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

// Where you stand among your Game Center friends on a leaderboard. Loaded in the background; poll PKR_GC_FriendRank.
static NSMutableDictionary<NSString *, NSArray<NSNumber *> *> *pkrFriendRanks; // board -> @[rank, count]

extern "C" void PKR_GC_LoadFriendRank(const char *leaderboardId)
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) return;
    if (pkrFriendRanks == nil) pkrFriendRanks = [NSMutableDictionary dictionary];
    NSString *board = PKRString(leaderboardId);
    pkrFriendRanks[board] = @[@(-1), @(0)]; // loading
    [GKLeaderboard loadLeaderboardsWithIDs:@[board] completionHandler:^(NSArray<GKLeaderboard *> *boards, NSError *error) {
        GKLeaderboard *lb = boards.firstObject;
        if (lb == nil || error != nil)
        {
            dispatch_async(dispatch_get_main_queue(), ^{ pkrFriendRanks[board] = @[@(0), @(0)]; });
            return;
        }
        [lb loadEntriesForPlayerScope:GKLeaderboardPlayerScopeFriendsOnly timeScope:GKLeaderboardTimeScopeAllTime
                                range:NSMakeRange(1, 100)
                    completionHandler:^(GKLeaderboardEntry *local, NSArray<GKLeaderboardEntry *> *entries, NSInteger total, NSError *err) {
            NSInteger rank = local != nil ? local.rank : 0;
            NSInteger count = err != nil ? 0 : total;
            dispatch_async(dispatch_get_main_queue(), ^{ pkrFriendRanks[board] = @[@(rank), @(count)]; });
        }];
    }];
}

// -1 = still loading, 0 = no score / unavailable, otherwise your rank; *count = players on the friends board (you included).
extern "C" int PKR_GC_FriendRank(const char *leaderboardId, int *count)
{
    NSArray<NSNumber *> *r = pkrFriendRanks != nil ? pkrFriendRanks[PKRString(leaderboardId)] : nil;
    if (count != NULL) *count = r != nil ? r[1].intValue : 0;
    return r != nil ? r[0].intValue : 0;
}
