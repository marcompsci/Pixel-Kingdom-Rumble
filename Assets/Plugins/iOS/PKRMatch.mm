// Pixel Kingdom Rumble - online co-op over Game Center (GameKit real-time matches, 2-4 players, peer to peer).
// Called from GameCenterTransport.cs / PKRMatchService.cs via [DllImport("__Internal")]. No server: Game Center
// finds or invites the players, then the phones talk directly. Incoming data is queued here and polled by C#.
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController(void);

// 0 idle, 1 matchmaking, 2 ready (everyone connected), 3 cancelled or failed, 4 disconnected
static int pkrMpState = 0;
static GKMatch *pkrMatch = nil;
static NSMutableArray *pkrInbox = nil;     // of @[fromId, NSData]
static NSMutableArray *pkrLeft = nil;      // of fromId
static NSString *pkrMpError = @"";

static NSString *PKRMpId(GKPlayer *p) { return p.gamePlayerID ?: @""; }

@interface PKRMatchDelegate : NSObject <GKMatchmakerViewControllerDelegate, GKMatchDelegate, GKLocalPlayerListener>
@end

@implementation PKRMatchDelegate

- (void)useMatch:(GKMatch *)match
{
    pkrMatch = match;
    pkrMatch.delegate = self;
    if (pkrMatch.expectedPlayerCount == 0) pkrMpState = 2;
}

- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFindMatch:(GKMatch *)match
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    [self useMatch:match];
}

- (void)matchmakerViewControllerWasCancelled:(GKMatchmakerViewController *)vc
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    pkrMpState = 3;
    pkrMpError = @"cancelled";
}

- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFailWithError:(NSError *)error
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    pkrMpState = 3;
    pkrMpError = error.localizedDescription ?: @"failed";
    NSLog(@"[PKR Match] matchmaking failed: %@", pkrMpError);
}

- (void)match:(GKMatch *)match didReceiveData:(NSData *)data fromRemotePlayer:(GKPlayer *)player
{
    @synchronized (pkrInbox) { [pkrInbox addObject:@[PKRMpId(player), data]]; }
}

- (void)match:(GKMatch *)match player:(GKPlayer *)player didChangeConnectionState:(GKPlayerConnectionState)state
{
    if (state == GKPlayerStateConnected)
    {
        if (match.expectedPlayerCount == 0) pkrMpState = 2;
    }
    else if (state == GKPlayerStateDisconnected)
    {
        @synchronized (pkrLeft) { [pkrLeft addObject:PKRMpId(player)]; }
        if (match.players.count == 0) pkrMpState = 4;
    }
}

// Two-player matches only (GameKit's rule): try to bring a dropped player back into the same match. The host keeps
// their seat (an AI plays it meanwhile) and hands it back when their controls arrive again.
- (BOOL)match:(GKMatch *)match shouldReinviteDisconnectedPlayer:(GKPlayer *)player
{
    NSLog(@"[PKR Match] reinviting a dropped player");
    return YES;
}

- (void)match:(GKMatch *)match didFailWithError:(NSError *)error
{
    NSLog(@"[PKR Match] match error: %@", error.localizedDescription);
    pkrMpState = 4;
}

// A friend's invite was accepted from the Game Center banner or Messages.
- (void)player:(GKPlayer *)player didAcceptInvite:(GKInvite *)invite
{
    GKMatchmakerViewController *vc = [[GKMatchmakerViewController alloc] initWithInvite:invite];
    if (vc == nil) return;
    vc.matchmakerDelegate = self;
    pkrMpState = 1;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

@end

static PKRMatchDelegate *pkrMatchDelegate = nil;

static void PKRMpSetup()
{
    if (pkrMatchDelegate == nil)
    {
        pkrMatchDelegate = [[PKRMatchDelegate alloc] init];
        pkrInbox = [NSMutableArray array];
        pkrLeft = [NSMutableArray array];
    }
}

static int PKRCopy(NSString *s, char *buf, int cap)
{
    if (buf == NULL || cap <= 0) return 0;
    const char *u = [s UTF8String] ?: "";
    int n = (int)strlen(u);
    if (n > cap - 1) n = cap - 1;
    memcpy(buf, u, n);
    buf[n] = 0;
    return n;
}

// Call once signed in: listens for accepted invites.
extern "C" void PKR_MP_Init()
{
    PKRMpSetup();
    if ([GKLocalPlayer localPlayer].isAuthenticated) [[GKLocalPlayer localPlayer] registerListener:pkrMatchDelegate];
}

// Opens Game Center's own screen: invite friends or find players (playerGroup keeps modes apart).
extern "C" void PKR_MP_Find(int minPlayers, int maxPlayers, int playerGroup)
{
    PKRMpSetup();
    if (![GKLocalPlayer localPlayer].isAuthenticated) { pkrMpState = 3; pkrMpError = @"Sign in to Game Center first"; return; }
    [[GKLocalPlayer localPlayer] registerListener:pkrMatchDelegate];
    GKMatchRequest *req = [[GKMatchRequest alloc] init];
    req.minPlayers = minPlayers;
    req.maxPlayers = maxPlayers;
    req.playerGroup = playerGroup;
    GKMatchmakerViewController *vc = [[GKMatchmakerViewController alloc] initWithMatchRequest:req];
    if (vc == nil) { pkrMpState = 3; pkrMpError = @"matchmaking unavailable"; return; }
    vc.matchmakerDelegate = pkrMatchDelegate;
    pkrMpState = 1;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

extern "C" int PKR_MP_State() { return pkrMpState; }

extern "C" int PKR_MP_Error(char *buf, int cap) { return PKRCopy(pkrMpError, buf, cap); }

extern "C" int PKR_MP_LocalId(char *buf, int cap) { return PKRCopy(PKRMpId([GKLocalPlayer localPlayer]), buf, cap); }

extern "C" int PKR_MP_LocalName(char *buf, int cap) { return PKRCopy([GKLocalPlayer localPlayer].alias ?: @"", buf, cap); }

extern "C" int PKR_MP_PeerCount() { return pkrMatch != nil ? (int)pkrMatch.players.count : 0; }

extern "C" int PKR_MP_PeerId(int i, char *buf, int cap)
{
    if (pkrMatch == nil || i < 0 || i >= (int)pkrMatch.players.count) return 0;
    return PKRCopy(PKRMpId(pkrMatch.players[i]), buf, cap);
}

extern "C" int PKR_MP_PeerName(int i, char *buf, int cap)
{
    if (pkrMatch == nil || i < 0 || i >= (int)pkrMatch.players.count) return 0;
    return PKRCopy(pkrMatch.players[i].alias ?: @"", buf, cap);
}

extern "C" void PKR_MP_SendAll(const unsigned char *data, int len, int reliable)
{
    if (pkrMatch == nil || data == NULL || len <= 0) return;
    NSError *err = nil;
    [pkrMatch sendDataToAllPlayers:[NSData dataWithBytes:data length:len]
                      withDataMode:(reliable ? GKMatchSendDataReliable : GKMatchSendDataUnreliable) error:&err];
    if (err != nil) NSLog(@"[PKR Match] send: %@", err.localizedDescription);
}

extern "C" void PKR_MP_SendTo(const char *peerId, const unsigned char *data, int len, int reliable)
{
    if (pkrMatch == nil || data == NULL || len <= 0) return;
    NSString *pid = peerId ? [NSString stringWithUTF8String:peerId] : @"";
    for (GKPlayer *p in pkrMatch.players)
    {
        if (![PKRMpId(p) isEqualToString:pid]) continue;
        NSError *err = nil;
        [pkrMatch sendData:[NSData dataWithBytes:data length:len] toPlayers:@[p]
              dataMode:(reliable ? GKMatchSendDataReliable : GKMatchSendDataUnreliable) error:&err];
        if (err != nil) NSLog(@"[PKR Match] send to: %@", err.localizedDescription);
        return;
    }
}

// Copies the next incoming message into buf (and its sender into fromBuf). Returns its length, 0 if there was
// nothing, or -(length) if buf was too small (the message stays queued).
extern "C" int PKR_MP_Receive(unsigned char *buf, int cap, char *fromBuf, int fromCap)
{
    if (pkrInbox == nil) return 0;
    @synchronized (pkrInbox)
    {
        if (pkrInbox.count == 0) return 0;
        NSArray *msg = pkrInbox[0];
        NSData *data = msg[1];
        int n = (int)data.length;
        if (n > cap) return -n;
        memcpy(buf, data.bytes, n);
        PKRCopy(msg[0], fromBuf, fromCap);
        [pkrInbox removeObjectAtIndex:0];
        return n;
    }
}

extern "C" int PKR_MP_TakeLeft(char *buf, int cap)
{
    if (pkrLeft == nil) return 0;
    @synchronized (pkrLeft)
    {
        if (pkrLeft.count == 0) return 0;
        NSString *s = pkrLeft[0];
        [pkrLeft removeObjectAtIndex:0];
        return PKRCopy(s, buf, cap) + 1;
    }
}

extern "C" void PKR_MP_Disconnect()
{
    if (pkrMatch != nil) { [pkrMatch disconnect]; pkrMatch.delegate = nil; }
    pkrMatch = nil;
    pkrMpState = 0;
    if (pkrInbox != nil) @synchronized (pkrInbox) { [pkrInbox removeAllObjects]; }
    if (pkrLeft != nil) @synchronized (pkrLeft) { [pkrLeft removeAllObjects]; }
}
