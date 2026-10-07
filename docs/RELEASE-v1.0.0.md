# ScreenInk 1.0.0

Turn your screen into a teaching canvas: press **F8**, draw or highlight your explanation, and press **F8**, **Esc**, or **Done** to return to your presentation.

## Download and start

1. Download **ScreenInk-v1.0.0-Windows-Portable.zip** from the assets below.
2. Right-click the ZIP and choose **Extract All**.
3. Open **ScreenInk Portable** and double-click **Start ScreenInk.bat**.
4. Press **F8** to draw. ScreenInk's tray menu also has **New screen (F8)** and **Exit**.

Windows 10/11 with the built-in Windows PowerShell 5.1 and .NET Framework / Windows Forms components is required. Keep all files together. No installer, account, internet connection, Python, or Node.js is needed to use the app.

## Included

- Pen, highlighter, eraser, laser pointer, straight line, freehand arrow, and rectangle.
- Undo/redo, per-tool settings, movable full toolbar, and compact dock.
- Previous/next drawings retained while the app is running.
- Save PNG and clipboard Copy without toolbars or temporary laser trails.
- An offline blank whiteboard page, detailed README, banner, and a 26-second scripted tool demo on generated lesson content.
- Readable source files and an MIT license.

## Validation and limits

The complete existing self-test passed locally, including the same test run against the extracted portable ZIP. The demo also verifies whole-stroke erasing and undo/redo before exporting its annotated PNG. Windows GitHub Actions runs the regression suite from the published source.

The drawing surface freezes a screenshot; it is not a live click-through overlay. History and preferences last until Exit, so export important drawings first. Touchscreen/stylus input depends on Windows mouse-event compatibility; pressure, tilt, palm rejection, and multi-touch are not implemented. Device-specific and meeting-app behavior still need broader testing. Managed computers may restrict PowerShell or Add-Type.

All repository demo images use generated content. Normal app captures include all visible content on all monitors: prepare your screens before recording or sharing. For meeting annotations, share the full display and check the preview.

`SHA256SUMS.txt` contains the portable ZIP's SHA-256 checksum. On Windows you can compare it with `Get-FileHash -Algorithm SHA256`.

Read the [full guide](https://github.com/FarzamFattahi/ScreenInk#readme) or [report a problem](https://github.com/FarzamFattahi/ScreenInk/issues).
