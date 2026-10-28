# Temporary: reports what the Arm64 runner can tell us about WPF rendering. Removed before merge.
param([string]$Mode = 'Default', [string]$Out = $env:RUNNER_TEMP)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing
$ri = [System.Runtime.InteropServices.RuntimeInformation]
$tag = "$Mode-$($ri::FrameworkDescription -replace '[^A-Za-z0-9]', '')"
"== $Mode on $($ri::FrameworkDescription), process $($ri::ProcessArchitecture), OS $($ri::OSArchitecture), PROCESSOR_ARCHITECTURE=$env:PROCESSOR_ARCHITECTURE"
"render tier: $([System.Windows.Media.RenderCapability]::Tier -shr 16)"
if ($Mode -eq 'SoftwareOnly') { [System.Windows.Media.RenderOptions]::ProcessRenderMode = 'SoftwareOnly' }

$panel = New-Object System.Windows.Controls.StackPanel -Property @{ Orientation = 'Horizontal' }
foreach ($c in 'Red', 'Lime', 'Blue') {
  $panel.Children.Add((New-Object System.Windows.Shapes.Rectangle -Property @{ Width = 100; Height = 100; Fill = [System.Windows.Media.Brushes]::$c })) | Out-Null
}
$results = foreach ($transparent in $true, $false) {
  $w = New-Object System.Windows.Window -Property @{
    WindowStyle = 'None'; AllowsTransparency = $transparent; Background = [System.Windows.Media.Brushes]::Black
    SizeToContent = 'WidthAndHeight'; Left = 0; Top = 0; Topmost = $true; ShowActivated = $false; Content = $panel
  }
  if ($transparent) { $w.Background = [System.Windows.Media.Brushes]::Transparent }
  $w.Show()
  $frame = New-Object System.Windows.Threading.DispatcherFrame
  $timer = New-Object System.Windows.Threading.DispatcherTimer -Property @{ Interval = [TimeSpan]::FromSeconds(3) }
  $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
  $timer.Start()
  [System.Windows.Threading.Dispatcher]::PushFrame($frame)

  $src = [System.Windows.PresentationSource]::FromVisual($w)
  $s = $src.CompositionTarget.TransformToDevice.M11
  "transparent=$transparent window render mode: $($src.CompositionTarget.RenderMode); scale $s"
  $bmp = New-Object System.Drawing.Bitmap ([int](300 * $s)), ([int](100 * $s))
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
  $g.Dispose()
  foreach ($i in 0..2) {
    $p = $bmp.GetPixel([int]((50 + 100 * $i) * $s), [int](50 * $s))
    "  expected $(@('Red', 'Lime', 'Blue')[$i]): R=$($p.R) G=$($p.G) B=$($p.B)"
  }
  $bmp.Save((Join-Path $Out "probe-$tag-transparent$transparent.png"))
  $bmp.Dispose()
  $w.Content = $null
  $w.Close()
}
$results
