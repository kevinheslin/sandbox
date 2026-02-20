# System Summary Script

function Format-Bytes {
    param([long]$Bytes)
    if ($Bytes -ge 1TB) { return "{0:N2} TB" -f ($Bytes / 1TB) }
    if ($Bytes -ge 1GB) { return "{0:N2} GB" -f ($Bytes / 1GB) }
    if ($Bytes -ge 1MB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    return "{0:N2} KB" -f ($Bytes / 1KB)
}

# --- CPU ---
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$cpuLoad = (Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average

# --- RAM ---
$os = Get-CimInstance Win32_OperatingSystem
$ramTotal = $os.TotalVisibleMemorySize * 1KB
$ramFree  = $os.FreePhysicalMemory * 1KB
$ramUsed  = $ramTotal - $ramFree
$ramPct   = [math]::Round(($ramUsed / $ramTotal) * 100, 1)

# --- Disks ---
$disks = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3"

# --- Network ---
$netAdapters = Get-CimInstance Win32_NetworkAdapterConfiguration -Filter "IPEnabled=True"

# --- Uptime ---
$uptime   = (Get-Date) - $os.LastBootUpTime
$uptimeStr = "{0}d {1}h {2}m" -f [int]$uptime.TotalDays, $uptime.Hours, $uptime.Minutes

# --- Output ---
$separator = "=" * 50

Write-Host ""
Write-Host $separator
Write-Host "  SYSTEM SUMMARY  --  $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
Write-Host $separator

Write-Host ""
Write-Host "  CPU"
Write-Host "    Model  : $($cpu.Name.Trim())"
Write-Host "    Cores  : $($cpu.NumberOfCores) physical / $($cpu.NumberOfLogicalProcessors) logical"
Write-Host "    Load   : $cpuLoad%"

Write-Host ""
Write-Host "  MEMORY"
Write-Host "    Total  : $(Format-Bytes $ramTotal)"
Write-Host "    Used   : $(Format-Bytes $ramUsed)  ($ramPct%)"
Write-Host "    Free   : $(Format-Bytes $ramFree)"

Write-Host ""
Write-Host "  DISK"
foreach ($disk in $disks) {
    $total = $disk.Size
    $free  = $disk.FreeSpace
    $used  = $total - $free
    $pct   = if ($total -gt 0) { [math]::Round(($used / $total) * 100, 1) } else { 0 }
    Write-Host "    $($disk.DeviceID)  Total: $(Format-Bytes $total)  Used: $(Format-Bytes $used) ($pct%)  Free: $(Format-Bytes $free)"
}

Write-Host ""
Write-Host "  NETWORK"
foreach ($adapter in $netAdapters) {
    $ips  = ($adapter.IPAddress | Where-Object { $_ -match '^\d+\.\d+\.\d+\.\d+$' }) -join ", "
    $mac  = $adapter.MACAddress
    $name = $adapter.Description
    Write-Host "    $name"
    Write-Host "      IP  : $ips"
    Write-Host "      MAC : $mac"
}

Write-Host ""
Write-Host "  SYSTEM"
Write-Host "    Host   : $($env:COMPUTERNAME)"
Write-Host "    OS     : $($os.Caption) (Build $($os.BuildNumber))"
Write-Host "    Uptime : $uptimeStr"

Write-Host ""
Write-Host $separator
Write-Host ""
