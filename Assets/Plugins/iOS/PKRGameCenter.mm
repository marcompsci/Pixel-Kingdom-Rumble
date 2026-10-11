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
