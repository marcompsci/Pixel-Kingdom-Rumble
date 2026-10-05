// Pixel Kingdom Rumble - iOS haptics bridge.
// Called from HapticsService.cs via [DllImport("__Internal")].
// style: 0 = light, 1 = medium, 2 = heavy.
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *pkrGenerators[3];

extern "C" void PKR_HapticImpact(int style)
{
    if (style < 0) style = 0;
    if (style > 2) style = 2;

    if (pkrGenerators[style] == nil)
    {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleLight;
        if (style == 1) s = UIImpactFeedbackStyleMedium;
        if (style == 2) s = UIImpactFeedbackStyleHeavy;
        pkrGenerators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    }
    [pkrGenerators[style] impactOccurred];
    [pkrGenerators[style] prepare]; // warm up for the next tap
}
