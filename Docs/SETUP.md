# Setup: Unity project and iOS export

## One-time: create the Unity project around this repo

The repo contains scripts, packages and (soon) generated content, but not Unity's `ProjectSettings/`.
The cleanest way to get a correctly configured 2D URP project is to start from Unity's template and layer
the repo on top.

1. **Unity Hub > New project > Universal 2D** template, Unity 6 LTS. Name it `Pixel-Kingdom-Rumble`.
   Close Unity once it finishes opening.
2. In Terminal, inside that new project folder:
   ```bash
   git init -b main
   git remote add origin https://github.com/marcompsci/Pixel-Kingdom-Rumble.git
   git fetch origin
   git reset origin/main          # point at the repo without touching your files
   git checkout -- .              # bring in the repo's tracked files (overwrites Packages/manifest.json)
   git branch --set-upstream-to=origin/main
   ```
   Note: your home folder is itself a git repo, so always check `git rev-parse --show-toplevel` prints the
   project folder before committing. Alternatively point Unity Hub at the existing clone in `~/GameDev/Pixel-Kingdom-Rumble`
   and copy in the template's `ProjectSettings/` and `Assets/Settings/` folders instead.
3. Open the project in Unity Hub. Unity resolves packages (a few minutes).
   - If asked **"enable the new Input System backends?"** choose **Yes** (Unity restarts).
   - Set **Edit > Project Settings > Player > Other Settings > Active Input Handling** to **Input System Package (New)** if it was not set.
4. Run **PKR > Configure iOS Player Settings**.
5. **Window > General > Test Runner**: run **EditMode** (133 tests) and **PlayMode** (28 tests).
5b. **PKR > Build All Scenes**, open `Scenes/00_Boot`, press Play. Open **Window > General > Device Simulator**
    and pick an iPhone to see and use the touch controls with the mouse.
6. Commit Unity's generated files so the repo becomes complete:
   ```bash
   git add -A
   git commit -m "Add Unity ProjectSettings and template assets"
   git push -u origin main
   ```

After setup, work through [FIRST_RUN.md](FIRST_RUN.md) to check each feature.

## iOS build

Requirements: a Mac with Xcode (current release), Unity iOS Build Support module installed via Unity Hub,
and your paid Apple Developer account signed in to Xcode.

1. **File > Build Profiles** (Unity 6) > **iOS** > **Switch Platform**.
2. Run **PKR > Build All Scenes** so every scene is in the list with `00_Boot` first.
3. **Build** to a folder named `iOSBuild/` at the repo root (it is git-ignored).
4. Open `iOSBuild/Unity-iPhone.xcodeproj` in Xcode.
5. Target **Unity-iPhone > Signing & Capabilities**: tick *Automatically manage signing*, choose your Team.
   The bundle ID defaults to `com.marcompsci.pixelkingdomrumble`; change it in Unity Player Settings if you want another.
6. Plug in your iPhone, select it as the run destination, press **Run**.

### Performance check (manual)
- Build with **Development Build** off for real numbers.
- In Xcode, use the FPS gauge in the Debug navigator, or Instruments > Time Profiler.
- Target: steady 60 FPS on a modern iPhone. No benchmark has been run yet.

## Troubleshooting
- **Scene X is not in Build Settings**: run **PKR > Build All Scenes**.
- **Orientation does not change in the Editor**: expected. Use the Device Simulator or a real device.
- **No haptics**: they only work on a real iPhone, and only when *Haptics* is on in Settings.
