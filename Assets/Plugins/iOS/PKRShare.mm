// Pixel Kingdom Rumble - iOS share sheet bridge.
// Called from ShareService.cs via [DllImport("__Internal")]: shares text and/or a PNG (or animated GIF highlight clip) file with the system sheet
// (Messages, social apps, Save Image, Copy...). The app's Info.plist declares NSPhotoLibraryAddUsageDescription
// (IOSPostBuild) because "Save Image" writes to the photo library.
#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController();

extern "C" void PKR_Share(const char* text, const char* imagePath)
{
    NSMutableArray* items = [NSMutableArray array];
    if (imagePath != NULL && strlen(imagePath) > 0)
    {
        NSString* path = [NSString stringWithUTF8String:imagePath];
        if ([[path.pathExtension lowercaseString] isEqualToString:@"gif"])
        {
            // Animated highlight clip: share the file itself so it stays animated (a UIImage would keep one frame).
            if ([[NSFileManager defaultManager] fileExistsAtPath:path]) [items addObject:[NSURL fileURLWithPath:path]];
        }
        else
        {
            UIImage* image = [UIImage imageWithContentsOfFile:path];
            if (image != nil) [items addObject:image];
        }
    }
    if (text != NULL && strlen(text) > 0) [items addObject:[NSString stringWithUTF8String:text]];
    if (items.count == 0) return;

    UIViewController* root = UnityGetGLViewController();
    if (root == nil) return;
    UIActivityViewController* sheet = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
    UIPopoverPresentationController* pop = sheet.popoverPresentationController;
    if (pop != nil) // iPad: anchor the popover in the middle of the screen
    {
        pop.sourceView = root.view;
        pop.sourceRect = CGRectMake(CGRectGetMidX(root.view.bounds), CGRectGetMidY(root.view.bounds), 1, 1);
        pop.permittedArrowDirections = 0;
    }
    [root presentViewController:sheet animated:YES completion:nil];
}
