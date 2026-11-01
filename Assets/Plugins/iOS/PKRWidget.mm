// Pixel Kingdom Rumble - the Quick Game widget's shared App Group (group.com.marcompsci.pixelkingdomrumble).
// The widget saves its stats there; the game reads them (Story Quest > STATS, Star Shards for widget clears) and
// tells the widget which hero to draw and whether live seasons are on. Nothing leaves the phone.
// Called from QuickGameBridge.cs via [DllImport("__Internal")].
#import <Foundation/Foundation.h>
#include <string.h>

static NSUserDefaults* PKRWidgetGroup()
{
    return [[NSUserDefaults alloc] initWithSuiteName:@"group.com.marcompsci.pixelkingdomrumble"];
}

// Copies the widget's stats JSON into buf (UTF-8, zero-terminated). Returns its length, 0 if none,
// or -needed if buf is too small.
extern "C" int PKR_Widget_ReadStats(char* buf, int cap)
{
    NSString* s = [PKRWidgetGroup() stringForKey:@"qg.stats"];
    if (s == nil) return 0;
    const char* utf8 = [s UTF8String];
    int n = (int)strlen(utf8);
    if (n + 1 > cap) return -(n + 1);
    memcpy(buf, utf8, n);
    buf[n] = 0;
    return n;
}

extern "C" void PKR_Widget_WriteProfile(const char* json)
{
    if (json == NULL) return;
    [PKRWidgetGroup() setObject:[NSString stringWithUTF8String:json] forKey:@"qg.profile"];
}
