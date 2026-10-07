# ScreenInk Studio / Windows PowerShell 5.1, no installation required.
[CmdletBinding()]
param([switch]$SelfTest, [string]$TestArtifactsPath, [switch]$ShortcutTest)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -Path @((Join-Path $PSScriptRoot 'ScreenInk.Core.cs'), (Join-Path $PSScriptRoot 'ScreenInk.Studio.cs'), (Join-Path $PSScriptRoot 'ScreenInk.Dock.cs'), (Join-Path $PSScriptRoot 'ScreenInk.ShortcutTests.cs')) -ReferencedAssemblies 'System.Windows.Forms.dll', 'System.Drawing.dll'
[ScreenInk.Native]::EnableDpi()
[System.Windows.Forms.Application]::EnableVisualStyles()
if ($ShortcutTest) { [ScreenInk.ShortcutTests]::Run(); return }

$script:pages = [System.Collections.Generic.List[ScreenInk.DrawingPage]]::new()
$script:preferences = [ScreenInk.InkPreferences]::new()
$script:pageIndex = -1
$script:overlay = $null
$script:toolbar = $null
$script:studio = $null
$script:canvas = $null
$script:tray = $null
$script:hotkey = $null
$script:appIcon = $null
$script:closing = $false
$script:toolbarPosition = $null
$script:captureTimer = $null

function Update-History {
    if ($null -ne $script:studio) { $script:studio.SetHistory($script:pageIndex, $script:pages.Count) }
}

function Show-Page([int]$index) {
    if ($index -lt 0 -or $index -ge $script:pages.Count -or $null -eq $script:canvas) { return }
    $script:canvas.FinishGesture()
    $script:pageIndex = $index
    $page = $script:pages[$index]
    $script:overlay.Location = $page.ScreenBounds.Location
    $script:overlay.Size = $page.ScreenBounds.Size
    $script:canvas.SetPage($page)
    Update-History
}

function Save-Canvas {
    $script:canvas.FinishGesture()
    $dialog = [System.Windows.Forms.SaveFileDialog]::new()
    $dialog.Title = 'Save annotated screen'
    $dialog.Filter = 'PNG image (*.png)|*.png'
    $dialog.FileName = 'ScreenInk-{0}.png' -f (Get-Date -Format 'yyyy-MM-dd-HHmmss')
    try {
        if ($dialog.ShowDialog($script:toolbar) -eq 'OK') {
            $image = $script:canvas.Page.Export()
            try { $image.Save($dialog.FileName, [System.Drawing.Imaging.ImageFormat]::Png) }
            finally { $image.Dispose() }
            $script:canvas.ShowMessage('Image saved')
        }
    } catch {
        $script:canvas.ShowMessage('Could not save the image. Please try another location.')
    } finally { $dialog.Dispose() }
}

function Copy-Canvas {
    $script:canvas.FinishGesture()
    $image = $script:canvas.Page.Export()
    try {
        [System.Windows.Forms.Clipboard]::SetImage($image)
        $script:canvas.ShowMessage('Image copied - ready to paste')
    } catch {
        $script:canvas.ShowMessage('Clipboard is busy. Try Copy again.')
    } finally { $image.Dispose() }
}

function Close-Overlay {
    if ($null -eq $script:overlay) { return }
    $script:canvas.FinishGesture()
    $script:toolbarPosition = $script:toolbar.Location
    $script:toolbar.Hide()
    $script:overlay.Hide()
    # Disposing the toolbar saves its brush preferences. Pages stay editable.
    $script:toolbar.Dispose()
    $script:overlay.Dispose()
    $script:toolbar = $null
    $script:overlay = $null
    $script:studio = $null
    $script:canvas = $null
}

function New-Screen {
    Close-Overlay
    if ($null -ne $script:captureTimer) { $script:captureTimer.Dispose() }
    $script:captureTimer = [System.Windows.Forms.Timer]::new()
    $script:captureTimer.Interval = 160
    $script:captureTimer.Add_Tick({
        $this.Stop()
        $this.Dispose()
        $script:captureTimer = $null
        if (-not $script:closing -and $null -eq $script:overlay) { Start-Overlay }
    })
    $script:captureTimer.Start()
}

function Connect-Shortcuts([ScreenInk.InkForm]$form) {
    $form.add_Done({ Close-Overlay })
    $form.add_PreviousPage({ Show-Page ($script:pageIndex - 1) })
    $form.add_NextPage({ Show-Page ($script:pageIndex + 1) })
    $form.add_SaveRequested({ Save-Canvas })
    $form.add_CopyRequested({ Copy-Canvas })
    $form.add_CompactRequested({ $script:studio.ToggleCompact() })
}

function Place-Toolbar([System.Drawing.Rectangle]$area) {
    $script:toolbar.Bounds = $script:studio.DockBounds($area)
    [ScreenInk.Native]::Round($script:toolbar, 16)
}

function Start-Overlay([switch]$Resume, [switch]$BuildOnly) {
    if ($null -ne $script:overlay) { Close-Overlay; return }
    if (-not $Resume -or $script:pages.Count -eq 0) {
        $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
        $screenshot = [System.Drawing.Bitmap]::new($bounds.Width, $bounds.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($screenshot)
        try { $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bounds.Size, [System.Drawing.CopyPixelOperation]::SourceCopy) }
        catch { $screenshot.Dispose(); throw }
        finally { $graphics.Dispose() }
        $script:pages.Add([ScreenInk.DrawingPage]::new($screenshot, $bounds))
        $script:pageIndex = $script:pages.Count - 1
    }
    $page = $script:pages[$script:pageIndex]
    $script:overlay = [ScreenInk.InkForm]::new()
    $script:overlay.FormBorderStyle = 'None'
    $script:overlay.StartPosition = 'Manual'
    $script:overlay.Location = $page.ScreenBounds.Location
    $script:overlay.Size = $page.ScreenBounds.Size
    $script:overlay.TopMost = $true
    $script:overlay.ShowInTaskbar = $false
    $script:canvas = [ScreenInk.InkCanvas]::new()
    $script:canvas.Dock = 'Fill'
    $script:canvas.SetPage($page)
    $script:overlay.Canvas = $script:canvas
    $script:overlay.Controls.Add($script:canvas)
    Connect-Shortcuts $script:overlay
    $script:overlay.Add_FormClosing({
        if (-not $script:closing) { $_.Cancel = $true; Close-Overlay }
    })

    $script:toolbar = [ScreenInk.InkForm]::new()
    $script:toolbar.Canvas = $script:canvas
    $script:toolbar.FormBorderStyle = 'None'
    $script:toolbar.StartPosition = 'Manual'
    $script:toolbar.TopMost = $true
    $script:toolbar.ShowInTaskbar = $false
    $script:toolbar.BackColor = [ScreenInk.StudioTheme]::Background
    Connect-Shortcuts $script:toolbar
    $script:studio = [ScreenInk.StudioToolbar]::new($script:canvas, $script:preferences)
    $script:studio.Dock = 'Fill'
    $script:toolbar.Controls.Add($script:studio)
    $working = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    if ($null -ne $script:toolbarPosition) { $working = [System.Windows.Forms.Screen]::FromPoint($script:toolbarPosition).WorkingArea }
    Place-Toolbar $working
    $script:studio.add_PreviousRequested({ Show-Page ($script:pageIndex - 1) })
    $script:studio.add_NextRequested({ Show-Page ($script:pageIndex + 1) })
    $script:studio.add_NewRequested({ New-Screen })
    $script:studio.add_SaveRequested({ Save-Canvas })
    $script:studio.add_CopyRequested({ Copy-Canvas })
    $script:studio.add_DoneRequested({ Close-Overlay })
    $script:studio.add_SizeRequested({
        $area = [System.Windows.Forms.Screen]::FromRectangle($script:toolbar.Bounds).WorkingArea
        Place-Toolbar $area
    })
    Update-History
    [ScreenInk.Native]::Round($script:toolbar, 16)
    if ($BuildOnly) { return }
    $script:overlay.Show()
    $script:toolbar.Show($script:overlay)
    $script:canvas.Focus() | Out-Null
}

function Stop-ScreenInk {
    $script:closing = $true
    if ($null -ne $script:captureTimer) { $script:captureTimer.Dispose(); $script:captureTimer = $null }
    Close-Overlay
    foreach ($page in $script:pages) { $page.Dispose() }
    $script:pages.Clear()
    if ($null -ne $script:tray) { $script:tray.Visible = $false; $script:tray.Dispose(); $script:tray = $null }
    if ($null -ne $script:appIcon) { $script:appIcon.Dispose(); $script:appIcon = $null }
    if ($null -ne $script:hotkey) { $script:hotkey.Dispose(); $script:hotkey = $null }
    $script:launcher.Close()
    [System.Windows.Forms.Application]::Exit()
}

if ($SelfTest) {
    [ScreenInk.ShortcutTests]::Run()
    [ScreenInk.CoreTests]::Run()
    [ScreenInk.StudioTests]::Run($TestArtifactsPath)
    [ScreenInk.DockTests]::Run($TestArtifactsPath)
    $script:pages.Add([ScreenInk.DrawingPage]::new([System.Drawing.Bitmap]::new(640, 480), [System.Drawing.Rectangle]::new(0, 0, 640, 480)))
    $stroke = [ScreenInk.Stroke]::new([ScreenInk.InkTool]::Pen, [System.Drawing.Color]::Blue, 5, [System.Drawing.PointF]::new(30, 40), 0)
    $script:pages[0].Strokes.Add($stroke)
    $script:pages[0].Commit([System.Collections.Generic.List[ScreenInk.Stroke]]::new())
    $script:pageIndex = 0
    Start-Overlay -Resume -BuildOnly
    $script:canvas.Tool = [ScreenInk.InkTool]::Highlighter
    if (-not $script:studio.ToolButtons[[ScreenInk.InkTool]::Highlighter].Selected) { throw 'Tool selection must be visible.' }
    $script:studio.ToggleCompact()
    $script:studio.SetDockSide([ScreenInk.DockSide]::Right)
    Close-Overlay
    Start-Overlay -Resume -BuildOnly
    if ($script:canvas.Page.Strokes.Count -ne 1 -or $script:canvas.Page.Undo.Count -ne 1) { throw 'Closing/reopening must preserve ink and undo.' }
    if ($script:canvas.Tool -ne [ScreenInk.InkTool]::Highlighter) { throw 'Reopening must remember the selected tool.' }
    if (-not $script:studio.Collapsed -or $script:studio.Placement -ne [ScreenInk.DockSide]::Right) { throw 'Reopening must retain compact mode and dock edge.' }
    $script:canvas.UndoInk()
    if ($script:canvas.Page.Strokes.Count -ne 0) { throw 'Retained undo must remain usable.' }
    Close-Overlay
    foreach ($page in $script:pages) { $page.Dispose() }
    'PASS: close/reopen retains drawing, undo, selected tool, compact mode, and dock edge.'
    return
}

$script:launcher = [System.Windows.Forms.Form]::new()
$script:launcher.ShowInTaskbar = $false
$script:launcher.Opacity = 0
$script:launcher.Add_Shown({ $this.Hide() })
$script:launcher.Add_FormClosing({ if (-not $script:closing) { $_.Cancel = $true; $this.Hide() } })
$script:tray = [System.Windows.Forms.NotifyIcon]::new()
$script:tray.Text = 'ScreenInk - F8 to draw'
$script:appIcon = [ScreenInk.Native]::CreateAppIcon()
$script:tray.Icon = $script:appIcon
$script:tray.Visible = $true
$menu = [System.Windows.Forms.ContextMenuStrip]::new()
$newItem = $menu.Items.Add('New screen (F8)')
$newItem.Add_Click({ Start-Overlay })
$resumeItem = $menu.Items.Add('Resume previous drawing')
$resumeItem.Add_Click({ if ($null -eq $script:overlay) { Start-Overlay -Resume } })
[void]$menu.Items.Add('-')
$exitItem = $menu.Items.Add('Exit')
$exitItem.Add_Click({ Stop-ScreenInk })
$script:tray.ContextMenuStrip = $menu
$script:tray.Add_DoubleClick({ Start-Overlay })
try {
    $script:hotkey = [ScreenInk.HotkeyWindow]::new()
    $script:hotkey.add_Pressed({ Start-Overlay })
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'ScreenInk', 'OK', 'Warning') | Out-Null
    Stop-ScreenInk
    $menu.Dispose()
    $script:launcher.Dispose()
    return
}
try { [System.Windows.Forms.Application]::Run($script:launcher) }
finally {
    if (-not $script:closing) { Stop-ScreenInk }
    $menu.Dispose()
    $script:launcher.Dispose()
}
