// Pixel Kingdom Rumble - device idiom. Called from DeviceIdiom.cs via [DllImport("__Internal")].
// An iPhone app running on an iPad in compatibility mode reports the phone idiom, which is what we want.
#import <UIKit/UIKit.h>

extern "C" bool PKR_IsPadIdiom()
{
    return [UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad;
}
