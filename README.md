# Top Down Shooter Demo

Open this project with Unity 6000.6.3f1. The **Start** scene is first in Build Settings. Press **Play** to begin a three-stage run; clearing every enemy reveals the portal, and interacting beside it advances directly to the next arena. The last portal opens the completion screen.

## Controls

| Platform | Move | Aim and fire | Portal |
| --- | --- | --- | --- |
| Android landscape | Left stick | Hold and drag right stick | Tap **Interact** when it appears |
| Unity Editor | WASD or arrow keys | Mouse aim, hold left button to fire | E near portal |

Health refills at the start of each stage. On death, **Retry** restarts that stage. **Replay** starts again at stage 1 after completion.

## Project and build

The project uses the Universal 2D template and generated colored shapes. All game logic is in `Assets/Scripts`. The four scenes are in `Assets/Scenes`. To regenerate them from the editor scripts, choose **Top Down Shooter > Generate Demo**. To repeat the automated Editor playthrough, choose **Top Down Shooter > Run Play Mode Smoke Test** and check the Console for `[SMOKE] PASS`.

The Android build targets landscape orientation and uses Unity's Android Build Support, SDK, NDK, and OpenJDK. The included APK is a development build.

## Unity asset import

Unity 6.6 can report `Host type is not matching any asset type` for the URP package's `TraceRenderingLayerMask.urtshader`. This error persisted after moving the project to a short path. In this project, **Assets > Reimport All** rebuilt Unity's cached `Library` data and cleared the error. A short project path is still useful on Windows, but moving the folder alone does not reset old import data.

You may press Play directly from any stage scene while testing in the Editor. Stage scenes create the game flow and HUD automatically when the Start scene was not loaded first.
