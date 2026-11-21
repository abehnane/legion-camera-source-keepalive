# AMD and Windows Camera update check — 2026-11-21

Research used OpenCLI to read AMD's model-specific download page and official release notes. Installed versions and Store availability were checked locally. No graphics driver or Camera app was installed, reinstalled or downgraded during this check.

## AMD graphics

The investigated system has a Ryzen 7 5800H with integrated Radeon graphics and a separate NVIDIA RTX 3070 Laptop GPU. The installed AMD Windows driver version is **31.0.21925.1001**.

AMD's [5800H download page](https://www.amd.com/en/support/downloads/drivers.html/processors/ryzen/ryzen-5000-series/amd-ryzen-7-5800h.html) currently offers Adrenalin **26.5.2 for Polaris/Vega**, dated May 21, 2026. Its [official release notes](https://www.amd.com/en/resources/support-articles/release-notes/RN-RAD-WIN-26-5-2-POLARIS-VEGA.html) map that release to Windows driver-store version **31.0.21925.1001**, matching the installed driver. They list an Apex Legends crash fix, not a Windows Camera/MJPEG green-preview fix. The notes support Windows 10 and recommend laptop OEM drivers for system-specific compatibility.

Consequently, this check did not identify a newer AMD reference graphics driver offered for this exact processor. A newer release number for a different Radeon product family is not evidence of a compatible upgrade for the 5800H.

AMD has acknowledged other accelerated-video faults historically. For example, [22.10.2 release notes](https://www.amd.com/en/resources/support-articles/release-notes/RN-RAD-WIN-22-10-2.html) describe fixes for Chromium hardware-accelerated playback and other video issues on RX 6000 products. Those are different GPU/application cases and do not establish a fix or diagnosis for this Vega/Windows Camera symptom.

If AMD is involved here, the observed lead is its graphics/media-decoding path: the processor includes an integrated GPU, and AMD graphics/decoder modules were seen in the capture pipeline. This is not evidence that the CPU hardware is defective. The independent CPU-converted preview produces a normal live image.

## Windows Camera

Installed package: **2026.2608.1.0**, Microsoft Store product **9WZDNCRFJBBG**. The locally opened [official Store product page](https://apps.microsoft.com/detail/9wzdncrfjbbg) offered **Open**, with no Update button. WinGet identified the installed product and an exact-name upgrade query against `msstore` reported no newer available package.

These are availability observations for this device/account/configured sources, not proof that every Store rollout channel has the same version. No specific Camera update fixing this machine's green preview was identified.

## Conclusion and remaining work

An unspecified "latest AMD driver" or Camera update is not currently an identified fix: the relevant AMD reference driver already matches the installed version, and no Camera update was offered. Windows Camera remains unresolved, while the Microsoft UVC driver plus independent holder/CPU preview works without Camo.

A controlled OEM-driver comparison or a direct decoder-output comparison could provide new evidence. Neither was performed in this update check. Keep that distinction separate from the confirmed Sonix zero-filled-frame problem documented in the [Camo-free follow-up](green-preview-followup.md).
