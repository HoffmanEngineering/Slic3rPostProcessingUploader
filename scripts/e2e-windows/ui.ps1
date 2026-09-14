# Minimal UI automation for e2e.ps1: what xdotool does for e2e.sh, on Windows. Dot-source this file.
# Coordinates passed to Click-Window are relative to the window's client area, like `xdotool --window`.

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class Win32 {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    public const uint LEFTDOWN = 0x02, LEFTUP = 0x04, WHEEL = 0x0800;
    public static List<KeyValuePair<IntPtr, string>> Windows() {
        var list = new List<KeyValuePair<IntPtr, string>>();
        EnumWindows((h, l) => {
            if (!IsWindowVisible(h)) return true;
            var sb = new StringBuilder(512); GetWindowText(h, sb, sb.Capacity);
            if (sb.Length > 0) list.Add(new KeyValuePair<IntPtr, string>(h, sb.ToString()));
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
"@
[void][Win32]::SetProcessDPIAware()

$script:Step = 0
function Log([string]$msg) { Write-Host "[e2e] $msg" }

function Find-Windows([string]$TitlePattern) {
    [Win32]::Windows() | Where-Object { $_.Value -match $TitlePattern }
}

function Get-ClientArea([IntPtr]$hWnd) {
    $r = New-Object Win32+RECT; [void][Win32]::GetClientRect($hWnd, [ref]$r)
    $p = New-Object Win32+POINT; [void][Win32]::ClientToScreen($hWnd, [ref]$p)
    [pscustomobject]@{ X = $p.X; Y = $p.Y; Width = $r.Right; Height = $r.Bottom }
}

function Wait-Window([string]$TitlePattern, [int]$TimeoutSec = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $w = @(Find-Windows $TitlePattern); if ($w.Count -gt 0) { return $w[0].Key }
        Start-Sleep -Seconds 1
    }
    Fail "window '$TitlePattern' did not appear"
}

function Wait-WindowGone([string]$TitlePattern, [int]$TimeoutSec = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (@(Find-Windows $TitlePattern).Count -eq 0) { return }
        Start-Sleep -Seconds 1
    }
    Fail "window '$TitlePattern' did not close"
}

function Click-Screen([int]$X, [int]$Y, [double]$SleepSec = 2) {
    [void][Win32]::SetCursorPos($X, $Y); Start-Sleep -Milliseconds 150
    [Win32]::mouse_event([Win32]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::mouse_event([Win32]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Seconds $SleepSec
}

function Click-Window([string]$TitlePattern, [int]$X, [int]$Y, [double]$SleepSec = 2) {
    $w = @(Find-Windows $TitlePattern); if ($w.Count -eq 0) { Fail "no window '$TitlePattern' to click in" }
    [void][Win32]::SetForegroundWindow($w[0].Key); Start-Sleep -Milliseconds 300
    $c = Get-ClientArea $w[0].Key
    Click-Screen ($c.X + $X) ($c.Y + $Y) $SleepSec
}

function Scroll-Wheel([int]$Notches) {   # negative = down; the delta is a signed value in an unsigned parameter
    $delta = [BitConverter]::ToUInt32([BitConverter]::GetBytes([int32]($Notches * 120)), 0)
    [Win32]::mouse_event([Win32]::WHEEL, 0, 0, $delta, [UIntPtr]::Zero)
}

function Send-Keys([string]$Keys) { [System.Windows.Forms.SendKeys]::SendWait($Keys); Start-Sleep -Milliseconds 300 }

function Save-Screenshot([string]$Name) {
    $script:Step++
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $file = Join-Path $OUT ("{0:D2}-{1}.png" -f $script:Step, $Name)
    $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    Log "screenshot $file"
}

# The live "… - OrcaSlicer" window: the modified ("*…") one when a model is loaded, else the largest.
function Get-MainWindow {
    $best = $null; $bestArea = 0
    foreach ($w in @(Find-Windows ' - OrcaSlicer$')) {
        if ($w.Value.StartsWith('*')) { return $w.Key }
        $c = Get-ClientArea $w.Key; $area = $c.Width * $c.Height
        if ($area -gt $bestArea) { $best = $w.Key; $bestArea = $area }
    }
    $best
}

function Set-MainWindowPlacement([int]$X, [int]$Y, [int]$W, [int]$H) {
    $m = Get-MainWindow; if (-not $m) { Fail "no main window" }
    [void][Win32]::ShowWindow($m, 9)   # SW_RESTORE, in case it opened maximised
    [void][Win32]::MoveWindow($m, $X, $Y, $W, $H, $true)
    [void][Win32]::SetForegroundWindow($m)
    Start-Sleep -Seconds 2
}
