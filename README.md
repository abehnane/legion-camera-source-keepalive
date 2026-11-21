# Legion camera source keepalive

A verified **workaround**, not a permanent hardware repair, for an intermittently disappearing integrated USB camera on a Lenovo Legion 5 15ACH6H (type 82JU).

The tested combination kept the physical camera connected and produced a normal live image:

1. Microsoft's inbox USB Video Class driver.
2. A small helper that retains a Media Foundation camera source without requesting video frames.
3. A software preview that converts native YUY2 frames on the CPU.

**Windows Camera's own preview still shows green. Other video-call apps remain unverified.** One reboot passed startup/driver/device-presence checks. Holding a source cannot guarantee that physically absent hardware will reconnect.

**2026-11-21 follow-up:** Windows had reselected the Sonix driver, which again delivered zero-filled frames. Restoring the Microsoft driver restored the independent preview. Camo was then fully uninstalled, and the user confirmed a normal live image with the independent helper alone. See the [green-preview follow-up](docs/green-preview-followup.md) for measurements, restored diagnostic settings and remaining Windows Camera limits.

The [AMD/Windows Camera update check](docs/update-check-2026-11-21.md) found that the installed AMD driver already matches the version currently offered for the 5800H, and the Store offered no Camera update on this device. No matching published green-preview fix was identified.

## What this repository contains

| Component | Purpose |
|---|---|
| `mf_probe` | Independent Windows API probe and non-recording source keeper |
| `camera_preview` | Live 1280×720 YUY2 preview with CPU conversion; no recording |
| PowerShell and batch launchers | Build, start, stop, open the preview, install/remove sign-in startup |
| [Technical report](docs/technical-report.md) | Tested configuration, observations, code-path findings and limits |
| [Reboot validation](docs/reboot-validation.md) | How to test persistence without claiming permanence |

This is original compatibility/debugging code. It does not load Camo assemblies. No proprietary binaries, decompiled vendor source, driver packages, screenshots, raw traces, personal paths, account details, captured per-device instance identifiers or private diagnostic logs are included.

## Requirements

- Windows x64, tested on Windows 10 Pro 22H2, build 19045.
- [.NET 10 SDK for Windows](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) to build; the .NET 10 Desktop Runtime to run the preview.
- Tested camera hardware ID: `VID_13D3&PID_56FF`. This model identifier is not a device serial number. Per-machine instance IDs are resolved at runtime rather than shipped in the code.
- Windows camera permission for desktop applications.

Other devices, operating-system builds and capture modes are untested. The preview currently requests 1280×720 YUY2; it is not a general-purpose webcam application or a virtual camera for meeting apps.

## Build and run

Keep the checkout in a stable folder, then run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Build only compiles the programs; it does not change drivers or install tasks. The execution-policy argument applies to this invocation, not a global policy change.

On the tested machine the selected driver was **Microsoft `usbvideo.inf`, 10.0.19041.6033**. If testing this alternative, use Device Manager > Cameras > Integrated Camera > Update driver > Browse my computer > Let me pick > USB Video Device. Make sure the selection belongs to the intended camera. This repository does not distribute or automatically install drivers. Keep an OEM rollback path available. Microsoft's inbox driver can omit OEM-specific features.

Then double-click:

- `start_camera_helper.bat` — start the keeper.
- `open_software_camera.bat` — close Windows Camera and Camo Studio first, then open the verified preview path. Finish using those apps before launching it.
- `stop_camera_helper.bat` — stop the keeper until a manual restart or next configured sign-in.

The keeper holds camera access and can affect power consumption or Windows' camera-in-use indication. In keeper mode it does not call `ReadSample` and does not save video. The preview displays live images in memory without recording them. Diagnostic `stream` mode discards frame content after calculating aggregate statistics.

## Automatic sign-in startup

After confirming the helper and preview work, run the following in **Administrator PowerShell**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-CameraHelperStartup.ps1
```

It registers `IntegratedCameraSourceKeepAlive` for the current user's sign-in, with a five-second delay and **Limited** privileges. The task runs the helper supervisor without a time limit. A file lock and process-identity checks prevent duplicate supervisors. The task also permits operation on battery.

The task stores the checkout's absolute location locally. Keep the folder in place. If another installation already owns this task name, remove that startup entry before registering a new checkout; the installer will not silently replace an unrelated task.

Double-click `remove_camera_helper_startup.bat` to remove the task and stop the helper. It may show Windows' normal administrator prompt. This does not revert a separately selected camera driver.

## Probe commands

```powershell
.\mf_probe\bin\Release\net10.0\mf_probe.exe enum 20
.\mf_probe\bin\Release\net10.0\mf_probe.exe activate 20
.\mf_probe\bin\Release\net10.0\mf_probe.exe reader 20
.\mf_probe\bin\Release\net10.0\mf_probe.exe stream 20 yuy2
```

`enum` enumerates sources; `activate` retains a control source; `reader` also creates a source reader; `stream` reads/discards frames and logs counts and aggregate byte statistics. For non-enumeration modes the timeout covers discovery and then the hold/capture interval, so execution can take up to approximately twice the specified interval. A synchronous driver call can take longer.

Use one streaming capture client at a time. A source-holding keeper is not a streaming capture client. `0xC00D3704` means the stream failed to enter its run state; it is not proof of one specific cause.

## Reboot and permanence

A reboot is useful to test task startup, driver retention and reacquisition after sign-in. It cannot demonstrate that the device will **never** disappear again. Follow the [reboot checklist](docs/reboot-validation.md). The original investigation verified a normal image and a running sign-in task, but did not reboot the machine during the session.

`Verify-CameraSetup.ps1` performs read-only task, keeper-identity and driver/presence checks. Run it from the checkout used by the installed task. It does not open a capture stream and cannot certify visual correctness or permanence.

## Privacy and redistribution

Generated state and logs stay local and are ignored by Git. They can contain local paths, process metadata or Windows task/account information; do not add them when sharing results. The published report deliberately contains technical results rather than raw evidence files. Builds, downloaded tools, vendor driver backups, screenshots and traces are also ignored.

This repository does not flash firmware, modify Camo binaries, disable Windows security, change USB power policy or block Windows updates. The exact underlying electrical/firmware/driver failure and the remaining Windows Camera green-preview path are not established.

MIT license covers the original code and documentation here, not any third-party product discussed in the report.
