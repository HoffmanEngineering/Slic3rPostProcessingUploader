# VM Testing Setup Guide

Step-by-step guide for spinning up a Hyper-V Windows 11 VM with OrcaSlicer-family slicers installed, for testing the profile installer wizard.

---

## What Claude Can Help With

| Step | Claude can do it? |
|------|-------------------|
| Enable Hyper-V (Step 1) | ✅ Run PowerShell directly |
| Create VM (Steps 3–4) | ✅ Run PowerShell directly |
| Download ISO (Step 2) | ❌ Needs your browser |
| Install Windows (Steps 5–6) | ❌ GUI clicking required |
| Configure drive redirection (Steps 7–8) | ❌ GUI clicking (instructions below) |
| Install slicers (Step 9) | ❌ GUI installers |
| Take/restore snapshots (Steps 10–11) | ✅ Run PowerShell directly |

---

## Phase 1: Enable Hyper-V

### Step 1 — Enable the Windows feature

Open PowerShell as Administrator and run:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

Restart when prompted.

---

## Phase 2: Get a Windows 11 ISO

### Step 2 — Download the ISO *(manual)*

1. Go to [microsoft.com/en-us/software-download/windows11](https://www.microsoft.com/en-us/software-download/windows11)
2. Scroll to **"Download Windows 11 Disk Image (ISO)"**
3. Select Windows 11, click Download
4. Save the ISO (~6 GB) somewhere like `D:\ISOs\Win11.iso`

---

## Phase 3: Create the VM

### Step 3 — Create the VM via PowerShell

Adjust the paths to suit your machine, then run:

```powershell
$VMName    = "SlicerTestVM"
$VMPath    = "D:\VMs"
$ISOPath   = "D:\ISOs\Win11_25H2_English_x64.iso"
$VHDSizeGB = 80

New-VM -Name $VMName -Path $VMPath -Generation 2 -MemoryStartupBytes 16GB -SwitchName "Default Switch"
Set-VM -Name $VMName -ProcessorCount 8 -AutomaticCheckpointsEnabled $false
New-VHD -Path "$VMPath\$VMName\$VMName.vhdx" -SizeBytes ($VHDSizeGB * 1GB) -Dynamic
Add-VMHardDiskDrive -VMName $VMName -Path "$VMPath\$VMName\$VMName.vhdx"
Add-VMDvdDrive -VMName $VMName -Path $ISOPath
Set-VMFirmware -VMName $VMName -FirstBootDevice (Get-VMDvdDrive -VMName $VMName)
Set-VMFirmware -VMName $VMName -EnableSecureBoot On -SecureBootTemplate MicrosoftWindows
```

### Step 4 — Enable Enhanced Session Mode and TPM

```powershell
Set-VM -Name "SlicerTestVM" -EnhancedSessionTransportType HvSocket
Set-VMHost -EnableEnhancedSessionMode $true

# Required for Windows 11 TPM check — must set key protector before enabling vTPM
Set-VMKeyProtector -VMName "SlicerTestVM" -NewLocalKeyProtector
Enable-VMTPM -VMName "SlicerTestVM"
```

> **Note:** If you run `Enable-VMTPM` without `Set-VMKeyProtector` first, you'll get a "valid key protector" error. Always run both commands in order.

---

## Phase 4: Install Windows 11

### Step 5 — Boot and install *(manual)*

1. Open **Hyper-V Manager**
2. Double-click **SlicerTestVM** → click **Start**
3. Walk through the Windows 11 installer
4. When asked for a product key, click **"I don't have a product key"** (90-day evaluation)
5. Choose **Windows 11 Pro**

### Step 6 — Accept Enhanced Session *(manual)*

Once at the Windows desktop, Hyper-V will prompt you to switch to Enhanced Session. Click **Yes**. This enables RDP-based connection which powers drive sharing.

> **Note:** If Enhanced Session is greyed out, run these from an elevated PowerShell on the host, then reconnect:
> ```powershell
> Enable-VMIntegrationService -VMName "SlicerTestVM" -Name "Guest Service Interface"
> Invoke-Command -VMName "SlicerTestVM" -ScriptBlock {
>     Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections" -Value 0
>     Enable-NetFirewallRule -DisplayGroup "Remote Desktop"
>     Start-Service TermService
> } -Credential (Get-Credential)
> ```
> Use a local admin account (not a Microsoft account) for the credentials prompt.

---

## Phase 5: Set Up the Shared Folder

Hyper-V's Enhanced Session uses RDP under the hood. **RDP drive redirection** is the ideal approach, but if Enhanced Session doesn't work, use an **SMB network share** instead (more reliable in practice).

### Step 7 — Option A: RDP drive redirection *(if Enhanced Session works)*

When the VM connection window opens, **before clicking Connect**:

1. Click **"Show Options"** (bottom-left of the connection window)
2. Go to the **Local Resources** tab
3. Click **"More…"** under Local devices and resources
4. Expand **Drives** and check the drive where your repo lives (e.g. `D:`)
5. Click OK → Connect

Your `D:\` drive will appear inside the VM as `\\tsclient\D`.

### Step 7 — Option B: SMB share *(fallback, more reliable)*

Run in **elevated PowerShell** on the host:

```powershell
$buildPath = "D:\Development\3d-print-log\Slic3rPostProcessingUploader\Slic3rPostProcessingUploader\bin\Release\net10.0\win-x64\publish"
New-SmbShare -Name "SlicerUploader" -Path $buildPath -FullAccess "Everyone"

# Create a local user for VM authentication (needed since host may have no password)
$password = ConvertTo-SecureString "SlicerVM123!" -AsPlainText -Force
New-LocalUser -Name "vmshare" -Password $password -Description "VM Share Access"
Grant-SmbShareAccess -Name "SlicerUploader" -AccountName "vmshare" -AccessRight Full -Force

# Find the host IP on the Default Switch
Get-NetIPAddress -InterfaceAlias "vEthernet (Default Switch)" -AddressFamily IPv4 | Select-Object IPAddress
```

Inside the VM, open **regular (non-admin) Command Prompt**:

```cmd
net use Z: \\<host-ip>\SlicerUploader /user:HOFFMAN-DESKTOP\vmshare SlicerVM123!
```

> **Note:** If you need to access the share from an **elevated** Command Prompt, you must remap it there too — elevated sessions don't inherit network drives from the regular user session.

### Step 8 — Important: use a local path for the exe

**Do not run the wizard from the Z: drive.** The Z: path gets stored in the profile JSON and OrcaSlicer cannot access network drives when running post-process scripts.

Copy the exe to a local path first:

```cmd
mkdir C:\Tools
copy Z:\Slic3rPostProcessingUploader.exe C:\Tools\
```

Always run the wizard from `C:\Tools\Slic3rPostProcessingUploader.exe`.

---

## Phase 5b: GPU Partitioning (Required for OrcaSlicer)

OrcaSlicer requires OpenGL 2.0+. The default Hyper-V video adapter does not support this. GPU partitioning passes the host GPU through to the VM.

> **Note:** GPU partitioning blocks standard Hyper-V checkpoints. Use `Set-VM -CheckpointType Standard` before checkpointing, or accept that checkpoints are unavailable while GPU-P is enabled.

Run in **elevated PowerShell** (VM must be off):

```powershell
Stop-VM -Name "SlicerTestVM" -Force
Add-VMGpuPartitionAdapter -VMName "SlicerTestVM"
Set-VM -VMName "SlicerTestVM" -GuestControlledCacheTypes $true -LowMemoryMappedIoSpace 1GB -HighMemoryMappedIoSpace 32GB
```

Copy NVIDIA display drivers to a staging folder:

```powershell
New-Item -Path "D:\VM-GPU-Drivers" -ItemType Directory -Force
Copy-Item "C:\Windows\System32\DriverStore\FileRepository\nv_dispi.inf_amd64_*" "D:\VM-GPU-Drivers\" -Recurse
Copy-Item "C:\Windows\System32\nv*.dll" "D:\VM-GPU-Drivers\" -ErrorAction SilentlyContinue
Copy-Item "C:\Windows\System32\nv*.exe" "D:\VM-GPU-Drivers\" -ErrorAction SilentlyContinue
```

Share the staging folder, start the VM, then inside an **elevated Command Prompt** (remapping the share first — see Step 7 note):

```cmd
net use Z: \\HOFFMAN-DESKTOP\SlicerUploader /user:HOFFMAN-DESKTOP\vmshare SlicerVM123!
mkdir "C:\Windows\System32\HostDriverStore\FileRepository"
xcopy "Z:\nv_dispi.inf_amd64_*" "C:\Windows\System32\HostDriverStore\FileRepository\" /E /I /Y
xcopy "Z:\nv*.dll" C:\Windows\System32\ /Y
xcopy "Z:\nv*.exe" C:\Windows\System32\ /Y
```

Restart the VM. OrcaSlicer should launch without the OpenGL warning.

---

## Phase 6: Install Slicers

### Step 9 — Download and install slicers *(manual, inside VM)*

Open the browser inside the VM and download + install each slicer:

| Slicer | Where to download |
|--------|-------------------|
| OrcaSlicer | github.com/SoftFever/OrcaSlicer/releases |
| Snapmaker Orca | Snapmaker's GitHub releases page |
| AnycubicSlicer Next | Anycubic's website |

**Important:** After installing each slicer:
1. **Launch it once and close it** — this generates the `user/default/process/` directory our installer scans. Without this, the wizard will find 0 profiles.
2. **Add your printers** through the slicer's setup wizard — vendor-specific profiles (e.g. TwoTrees, Bambu) are only downloaded when you add a printer. The uploader wizard must be run after adding printers to pick up those profiles.

If you add a new printer later, just re-run the wizard — it detects and updates only the new profiles.

---

## Phase 7: Snapshots

### Step 10 — Take your baseline snapshot

Once all slicers are installed and launched once, take a snapshot:

```powershell
Checkpoint-VM -Name "SlicerTestVM" -SnapshotName "Slicers Installed - Clean"
```

This is your reset point. Revert to it between test runs to get a clean slate without reinstalling anything.

### Step 11 — Revert to snapshot between test runs

```powershell
Restore-VMCheckpoint -Name "Slicers Installed - Clean" -VMName "SlicerTestVM"
Start-VM -Name "SlicerTestVM"
```

---

## Typical Test Run

Once set up, a full install/uninstall test cycle looks like this:

1. Revert to snapshot (Step 11)
2. Run dry-run to preview: `Slic3rPostProcessingUploader.exe install --dry-run`
3. Run install wizard: `Slic3rPostProcessingUploader.exe install`
4. Open a slicer → check that a process profile now has the `post_process` entry
5. Run uninstall wizard: `Slic3rPostProcessingUploader.exe uninstall`
6. Verify profiles are cleaned up
7. Revert to snapshot, repeat for next scenario

---

## Lessons Learned from Initial VM Testing

These issues were discovered during the first real-world test run and are documented here to save time on future testing sessions.

### OrcaSlicer silently skips same-name user profiles

**Symptom:** Installed profiles don't appear in OrcaSlicer's dropdown.
**Cause:** OrcaSlicer loads system profiles first. Any user profile with the same name as a system profile is skipped with "Preset already present, not loading" in the debug log.
**Fix:** User override profiles must have a unique name (e.g. `0.20mm Standard @SK1 - 3DPrintLog`) with `"inherits"` pointing to the system profile.
**How to diagnose:** Check `%APPDATA%\OrcaSlicer\log\debug_*.log` for "Preset already present" warnings.

### OrcaSlicer silently skips profiles with empty version field

**Symptom:** Profiles don't appear in dropdown despite correct file name and structure.
**Cause:** OrcaSlicer parses the `version` field with `Semver::parse()`. If the field is empty or absent, the profile is silently skipped at load time (no log message).
**Fix:** Always write a valid semver version (e.g. `"1.0.0.0"`) in user profiles.

### Post-process script path must be local

**Symptom:** Wizard installs successfully but OrcaSlicer doesn't call the script after slicing.
**Cause:** The wizard stores `Environment.ProcessPath` in the profile JSON. If run from a network share (Z:), OrcaSlicer can't access that path when running post-process scripts.
**Fix:** Always copy the exe to a local path (e.g. `C:\Tools\`) and run the wizard from there.

### Checkpoints blocked by GPU partitioning

**Symptom:** `Checkpoint-VM` fails with "cannot be performed on virtual machine because it is assigned one or more GPUP partitions".
**Cause:** Production checkpoints are incompatible with GPU-P.
**Workaround:** None found for this Hyper-V version. Take checkpoints before enabling GPU-P, or accept that checkpoints are unavailable during GPU testing sessions.

---

## Optional: Additional Snapshots for Version Testing

To test against different slicer versions, take separate snapshots after installing each version combo:

```
[Slicers Installed - Clean]          ← OrcaSlicer 2.2, Snapmaker 1.x, Anycubic 1.x
[Slicers Installed - OrcaSlicer 2.1] ← downgraded OrcaSlicer
```

Restore whichever snapshot matches the version you want to test.
