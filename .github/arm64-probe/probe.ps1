# Temporary: reports what the Arm64 runner can tell us about WPF rendering. Removed before merge.
param([string]$Mode = 'Default', [string]$Out = $env:RUNNER_TEMP)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing
Add-Type -Namespace Probe -Name Native -MemberDefinition '[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);'
$ri = [System.Runtime.InteropServices.RuntimeInformation]
$tag = "$Mode-$($ri::FrameworkDescription -replace '[^A-Za-z0-9]', '')"
"== $Mode on $($ri::FrameworkDescription), process $($ri::ProcessArchitecture)"
"render tier: $([System.Windows.Media.RenderCapability]::Tier -shr 16)"
if ($Mode -eq 'SoftwareOnly') { [System.Windows.Media.RenderOptions]::ProcessRenderMode = 'SoftwareOnly' }

foreach ($transparent in $true, $false) {
  $panel = New-Object System.Windows.Controls.StackPanel -Property @{ Orientation = 'Horizontal' }
  foreach ($c in 'Red', 'Lime', 'Blue') {
    $panel.Children.Add((New-Object System.Windows.Shapes.Rectangle -Property @{ Width = 100; Height = 100; Fill = [System.Windows.Media.Brushes]::$c })) | Out-Null
  }
  $w = New-Object System.Windows.Window -Property @{
    WindowStyle = 'None'; AllowsTransparency = $transparent; Background = [System.Windows.Media.Brushes]::Black
    SizeToContent = 'WidthAndHeight'; Left = 100; Top = 100; ShowActivated = $false; Content = $panel
  }
  $w.Show()
  $frame = New-Object System.Windows.Threading.DispatcherFrame
  $timer = New-Object System.Windows.Threading.DispatcherTimer -Property @{ Interval = [TimeSpan]::FromSeconds(3) }
  $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
  $timer.Start()
  [System.Windows.Threading.Dispatcher]::PushFrame($frame)

  $src = [System.Windows.PresentationSource]::FromVisual($w)
  $hwnd = $src.Handle
  $s = $src.CompositionTarget.TransformToDevice.M11
  "transparent=${transparent}: window render mode $($src.CompositionTarget.RenderMode), scale $s"
  foreach ($flags in 0, 2) {
    $bmp = New-Object System.Drawing.Bitmap ([int](300 * $s)), ([int](100 * $s))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [Probe.Native]::PrintWindow($hwnd, $hdc, $flags)
    $g.ReleaseHdc($hdc)
    $g.Dispose()
    $px = foreach ($i in 0..2) { $p = $bmp.GetPixel([int]((50 + 100 * $i) * $s), [int](50 * $s)); "$(@('Red', 'Lime', 'Blue')[$i])->($($p.R),$($p.G),$($p.B))" }
    "  PrintWindow flags=$flags ok=${ok}: $($px -join ' ')"
    $bmp.Save((Join-Path $Out "probe-$tag-transparent$transparent-flags$flags.png"))
    $bmp.Dispose()
  }
  $w.Close()
}
