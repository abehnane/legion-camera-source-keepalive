# Green-preview follow-up — 2026-11-21

## Confirmed outcome

The independent preview again produces a normal live image using Microsoft `usbvideo.inf` 10.0.19041.6033. Camo Studio, its compatibility add-on, its service, its virtual camera/audio devices and its Reincubate driver-store packages were removed at the user's request. No matching Camo kernel driver remained in the system-driver inventory. The physical integrated camera and the independent keeper remained available.

With Camo removed, a ten-second YUY2 capture delivered 97 buffers. Whole-frame inspections at frames 1, 30, 60 and 90 were nonzero and changed over time. The user then confirmed a normal live picture in the preview. This establishes a working Camo-free capture/preview path in this session; it does not prove recovery from every future cold boot or disconnect.

Windows Camera still displayed green after Camo removal. That application remains unresolved.

## Why the independent preview had become green again

The active driver had changed back to Sonix 10.13.22000.20 (`oem6.inf`). Windows' SetupAPI installation log records Device Setup Manager reinstalling the already-present Sonix package on 2026-11-19 at approximately 23:06. The log identifies a driver-selection/install event, but does not establish which policy, update workflow or user action prompted it, or demonstrate a new package download.

Before changing the driver, native 1280 x 720 YUY2 capture was tested. All eight inspected whole frames contained only zero bytes: 1,843,200 bytes per frame, including chroma. Zero YUY2 chroma is not neutral black and converts to green. The bad data was observed before the preview's CPU conversion; adjusting the display's green channel would not repair it.

Restoring Microsoft's compatible inbox driver succeeded without requesting a reboot. The same native YUY2 path delivered 92 frames in a ten-second comparison, with changing, nonzero whole-frame samples. The user confirmed the normal live preview. An isolated native NV12 test also delivered 228 buffers in eight seconds with changing, nonzero inspected samples.

These results support the driver/capture-path change as the operational fix for the independent preview. They do not locate the exact component that generated the zero-filled Sonix-path samples.

## Windows Camera investigation and limits

- The failure persisted with the Microsoft driver and after complete Camo removal.
- Before a pipeline restart, Frame Server's loaded-module inventory contained SonixDeviceMFT and AMD's MJPEG decoder. Later inventories still showed the AMD graphics/decoder path; loaded modules alone do not prove which component corrupted a displayed frame.
- Temporarily disabling this camera interface's optional Platform DMFT did not restore the picture.
- Frame Server mode 0 produced a black/unusable result rather than the desired normal picture; the original mode 1 was restored in both registry views.
- A temporary Windows Camera GPU preference did not establish a switch away from AMD and was restored.
- Decoder-category, blocked-list and hardware-decoder experiments did not establish a verified Microsoft-only decoding path. The API enumeration still included AMD. Those trials cannot establish AMD's decoder as the cause of green.
- All trial changes were restored: hardware decoders enabled, AMD MJPEG category registration present, original GPU preferences, original Platform DMFT setting and original Frame Server settings. Camo removal and the intended Microsoft camera-driver selection remain.

No graphics-driver replacement, BIOS flash, restart or physical repair was performed in this follow-up.

## Code changes

The preview labels an entirely zero-filled frame explicitly instead of presenting it without explanation. The preview launcher first verifies and reuses the running scheduled keeper, including its executable path and saved process start time, when the installed task belongs to another checkout. This avoids the duplicate keepers observed when launching the published checkout alongside the original installation. It does not terminate unrelated processes or change drivers automatically.

Both preview builds completed successfully with no warnings/errors, the launcher passed PowerShell parsing, and the updated public-checkout launcher was exercised with exactly one keeper and a user-confirmed live image.

## If green returns

Check the actual selected camera driver, not only whether the physical device is present. Windows can reselect an OEM package later. `Verify-CameraSetup.ps1` reports the provider, INF and version. The current workaround does not pin drivers, block Windows Update or repair Windows Camera's processing path.

Microsoft references: [UVC camera processing and DMFT configuration](https://learn.microsoft.com/en-us/windows-hardware/drivers/stream/uvc-camera-implementation-guide), [Media Foundation transform enumeration](https://learn.microsoft.com/en-us/windows/win32/api/mfapi/nf-mfapi-mftenumex), [process-wide plug-in control](https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nn-mfobjects-imfplugincontrol).
