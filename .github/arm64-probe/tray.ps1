# Temporary: checks whether the runner's shell accepts notification area icons. Removed before merge.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type -Namespace Probe -Name Tray -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern IntPtr FindWindow(string className, string title);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct NotifyIconData
{
    public int cbSize; public IntPtr hWnd; public int uID; public int uFlags; public int uCallbackMessage; public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
    public int dwState; public int dwStateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
    public int uVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
    public int dwInfoFlags; public Guid guidItem; public IntPtr hBalloonIcon;
}

[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
public static extern bool Shell_NotifyIcon(int message, ref NotifyIconData data);
'@

$form = New-Object System.Windows.Forms.Form
$data = New-Object Probe.Tray+NotifyIconData
$data.cbSize = [System.Runtime.InteropServices.Marshal]::SizeOf($data)
$data.hWnd = $form.Handle
$data.uID = 1
$data.uFlags = 2 -bor 4
$data.hIcon = [System.Drawing.SystemIcons]::Application.Handle
$data.szTip = 'probe'

$start = Get-Date
while ($true) {
  $elapsed = [int]((Get-Date) - $start).TotalSeconds
  $tray = [Probe.Tray]::FindWindow('Shell_TrayWnd', $null)
  $explorer = @(Get-Process explorer -ErrorAction SilentlyContinue).Count
  $ok = [Probe.Tray]::Shell_NotifyIcon(0, [ref]$data)
  $err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
  "t=${elapsed}s Shell_TrayWnd=$tray explorer=$explorer NIM_ADD=$ok lastError=$err"
  if ($ok) { [void][Probe.Tray]::Shell_NotifyIcon(2, [ref]$data); break }
  if ($elapsed -ge 90) { break }
  Start-Sleep 5
}
"Top-level windows: $((Get-Process | Where-Object MainWindowHandle -ne 0 | ForEach-Object { "$($_.ProcessName) '$($_.MainWindowTitle)'" }) -join '; ')"
