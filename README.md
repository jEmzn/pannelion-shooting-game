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

## Windows project path

Open this project from a short folder path, such as the included `TDS` working copy or `C:\Unity\TDS` after extracting the source archive. Unity's URP package contains deeply nested files. In the original long workspace folder, one package asset resolves to a 261-character path and the Editor can report `Host type is not matching any asset type` for `TraceRenderingLayerMask.urtshader`. The same project imported cleanly from the shorter `TDS` path. The in-game product name remains **Top Down Shooter Demo**.

You may press Play directly from any stage scene while testing in the Editor. Stage scenes create the game flow and HUD automatically when the Start scene was not loaded first.
