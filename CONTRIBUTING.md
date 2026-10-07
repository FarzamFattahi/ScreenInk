# Contributing to ScreenInk

Keep the portable launcher simple and preserve the app's offline operation. Avoid introducing runtime downloads, uploads, or telemetry. Keep example assets based on generated content; never commit desktop captures with personal information.

Before submitting a drawing or toolbar change, run:

```powershell
powershell.exe -STA -NoProfile -ExecutionPolicy Bypass -File .\ScreenInk.ps1 -SelfTest -TestArtifactsPath .\verification
```

For changes involving keyboard tool switching, run the focused regression:

```powershell
powershell.exe -STA -NoProfile -ExecutionPolicy Bypass -File .\ScreenInk.ps1 -ShortcutTest
```

Verify the actual tray/F8 lifecycle on a Windows desktop when it is affected. Use an unshared clean background and save your work first. Confirm that closing the overlay preserves drawings, Exit shuts down the app, and a fresh launch can register F8.

Describe the user-visible problem, the resulting behavior, and how you verified it. Add meaningful regression coverage when changing drawing behavior. For device reports, include your Windows version and mouse/touchpad/touchscreen/stylus model, but omit serial numbers and private screenshots.

The demo is a scripted replay of actual canvas mouse handlers on a generated fraction lesson. Rebuild it with `tools/Record-Demo.ps1` and `tools/Encode-Demo.py` (requires Pillow). The banner in `docs/assets/banner.png` was generated with OpenAI's built-in image-generation tool; its prompt and provenance are recorded in `docs/assets/BANNER.md`. It is conceptual artwork, while the GIF demonstrates the real drawing code.
