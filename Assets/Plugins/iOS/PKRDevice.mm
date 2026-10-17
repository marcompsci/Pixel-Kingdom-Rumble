// Pixel Kingdom Rumble - device idiom. Called from DeviceIdiom.cs via [DllImport("__Internal")].
// An iPhone app running on an iPad in compatibility mode reports the phone idiom, which is what we want.
#import <UIKit/UIKit.h>

extern "C" bool PKR_IsPadIdiom()
{
    return [UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad;
}

// 0 nominal, 1 fair, 2 serious, 3 critical (NSProcessInfoThermalState). Read by PerfMonitor every few seconds.
extern "C" int PKR_ThermalState()
{
    return (int)[NSProcessInfo processInfo].thermalState;
}

// Low Power Mode (Settings > Battery): Auto graphics drops to 30 FPS while it's on.
extern "C" bool PKR_LowPowerMode()
{
    return [NSProcessInfo processInfo].isLowPowerModeEnabled;
}

// Settings > Accessibility > Motion > Reduce Motion: the game's own Reduced motion starts from it on first launch.
extern "C" bool PKR_ReduceMotion()
{
    return UIAccessibilityIsReduceMotionEnabled();
}
