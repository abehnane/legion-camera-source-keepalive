# Technical report: intermittent Legion integrated camera

## Scope and confirmed outcome

Investigation date: 2026-11-19. A normal live image was confirmed in the independent CPU-converted preview, without replacing the module again or opening the laptop during this investigation. A non-recording source holder kept the physical camera available during the validated client-lifetime tests. The user still saw green in Windows Camera itself, including an isolated test.

This is a working software path and a connection-lifetime workaround. It is not a conclusive diagnosis or permanent repair of an electrical, firmware or driver fault.

## Tested configuration

| Component | Observed configuration |
|---|---|
| Laptop | Lenovo Legion 5 15ACH6H, type 82JU |
| OS | Windows 10 Pro 22H2, build 19045 |
| Firmware | BIOS GKCN65WW; no firmware flash performed |
| Integrated camera | AzureWave/Sonix USB, `VID_13D3&PID_56FF` |
| OEM camera package | Sonix 10.13.22000.20, driver date 2023-01-06 |
| Working alternative | Microsoft inbox `usbvideo.inf`, 10.0.19041.6033 |
| GPUs | AMD Radeon integrated graphics and NVIDIA RTX 3070 Laptop GPU |
| Relevant application | Camo Studio 2.9.5.26360 |
| Working preview | Native 1280×720 YUY2, CPU conversion to RGB |

Per-device instance strings, hostnames, user names, account identifiers, process IDs, screenshots and local filesystem paths have been omitted. The hardware vendor/product ID and software versions above describe the technical configuration, not a uniquely identified device.

## Initial symptoms

The camera intermittently disappeared at the USB/PnP level. Earlier Windows and Linux checks had sometimes found no attached camera. During the new investigation it returned and vanished again. Windows Camera displayed a green preview; a similar green result had been reported when an external camera was connected, but that external camera was not stream-tested in this session.

Camo's recent installation was a useful lead. Its virtual camera must be distinguished from the actual physical USB camera: a present virtual device is not evidence that the integrated module is connected.

## Inspected Camo behavior

The installed .NET assemblies were inspected locally with ILSpy, alongside Windows USB/media traces and existing Camo logs. Only a behavioral summary is published here; no decompiled vendor source or binary is redistributed.

The inspected capture path:

1. Starts Media Foundation and polls video capture sources every 1,000 ms.
2. Uses `MFCreateAttributes` and `MFEnumDeviceSources`.
3. Activates the chosen `IMFMediaSource` using `IMFActivate::ActivateObject`, then detaches the activation object.
4. Retains a source for camera controls, queries supported formats, creates a source reader and requests samples.
5. Shuts down/releases its source and reader objects during disposal.

The service can relaunch Studio, and Studio remembers/reactivates its selected integrated camera. No Lenovo-specific EC camera-enable command was identified in this capture path. This was not an exhaustive audit of every application function.

## Controlled results

| Experiment | Observation | Interpretation / limit |
|---|---|---|
| Camo Studio termination | Repeatedly followed by physical camera disappearance around two seconds later | Reproducible association with client lifetime; source of the underlying fault still unproven |
| Actual USB selective suspend disabled temporarily | Disconnects continued | The power-plan change alone was insufficient; original settings restored |
| Microsoft UVC substituted without a holder | Dropouts still occurred | Driver substitution alone did not fix disappearance |
| Independent enumeration, Camo verified stopped | Device remained intermittent | Enumeration is not a reliable power-on or recovery guarantee |
| Activated source retained, no frame reads | Camera remained started throughout a 20-second check; absent after cleanup | Holding source lifetime reproduced the useful connection effect |
| Reader retained without frame reads | Camera stayed started through another 20-second check | An active video stream was not necessary for that observed effect |
| Holder acquired before Camo exit | An independent reader received 601 buffers in 20 seconds while the device stayed present | Demonstrated a Camo-independent holder/reader path |
| Sonix path, entire YUY2 samples inspected | All 1,843,200 bytes were zero at frames 1, 150, 300, 450 and 600 | Timed buffer delivery was not proof of valid image data |
| Microsoft native YUY2 plus holder | 190 buffers in a 20-second run; non-zero whole-frame values | Improved raw data on the tested path; negotiated frame rates/formats differed |
| Longer Microsoft validation | 300 buffers in 30 seconds; repeated whole-frame samples were non-zero and changed over time | Supported live image content rather than a single startup buffer |
| Independent CPU preview, other streaming apps closed | User confirmed a normal live picture at 1280×720 | A usable integrated-camera image was verified |
| Windows Camera tested alone, Camo virtual device temporarily disabled | User still saw green | Remaining app/format/processing issue is not solely another active Camo client |
| FrameServer clean restart | New service process; Windows Camera issue not resolved by this test sequence | Did not establish stale Sonix state as the cause |

There was also a stream-start failure (`0xC00D3704`) when the software preview was attempted with other camera applications open. An isolated retry worked. That HRESULT means the device failed to enter its run state; it does not uniquely diagnose an exclusive-client conflict.

Zero-filled YUY2 is not neutral black: zero chroma values can produce green after conversion. The provider/path comparison is evidence about these experiments, not proof that the Sonix package alone is the universal cause. CPU conversion, negotiated native format and frame rate differ from other paths. The exact bad transform, decoder or graphics operation has not been identified.

## USB trace findings

USBHUB3 event 123 reported a port-change failure for this camera, with status changing from `0x507` to `0x100` and the connection-change bit set. A client recovery request was observed before one disconnect. This corroborates a low-level USB port-state change, not just a disappearing app menu entry.

The broad trace decoded 75,122 events and reported 23,439 lost events; the narrower trace decoded 8,376 events and reported 1,031 lost events. These captures are incomplete causal records. Successful isochronous transfers with non-zero lengths were present, but transfer success alone does not establish correct pixel data. Raw USB video payload was not saved.

## Working setup and persistence

The retained setup selects Microsoft's UVC driver, keeps one non-recording source holder running and uses the CPU YUY2 preview. A sign-in task with Limited privileges, five-second delay, no execution-time limit and duplicate prevention was registered and started. One supervised keeper was verified active.

Actual sign-out, reboot, sleep/wake and long-term stability remain unverified. The keeper can retry source acquisition when the camera is enumerated; it cannot guarantee recovery of an electrically absent module. It holds camera access and may increase power use or affect camera-use indicators. It does not read frames in keeper mode.

No BIOS flash, physical work, permanent USB-power change, blanket update block or reboot was performed. No specific mainboard component was established as faulty. Obtaining the working picture contradicts a blanket claim that this module cannot produce an image, but does not rule out an intermittent hardware or power-transition issue.

## Reproduction code and references

The original independent `mf_probe` code in this repository performs the Windows API operations described above. Its public version resolves per-machine instance identifiers in memory and omits their values from normal discovery logging. The preview displays frames without recording them.

Primary references: [Media Foundation capture attributes](https://learn.microsoft.com/en-us/windows/win32/medfound/capture-device-attributes), [MFEnumDeviceSources](https://learn.microsoft.com/en-us/windows/win32/api/mfidl/nf-mfidl-mfenumdevicesources), [USB event capture](https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/how-to-capture-a-usb-event-trace), [USB device power states](https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/comparing-usb-device-states-to-wdm-device-states), [stream-start failure documentation](https://learn.microsoft.com/en-us/windows-hardware/test/hlk/testref/2132f6b2-8351-43f1-866b-3107de2b55c4).
