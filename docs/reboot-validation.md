# Reboot validation

## What a restart establishes

A restart can check that the selected driver persists, the sign-in task starts the keeper and the camera can be used again. One successful restart cannot establish that the device will never disappear. This workaround avoids an observed source-lifetime transition; it is not a confirmed repair of the underlying fault.

## Before restarting

1. Save work and finish any camera sessions.
2. Keep the checkout in its installed location.
3. Confirm that `IntegratedCameraSourceKeepAlive` is present and Running in Task Scheduler and that the intended Microsoft UVC driver is selected.
4. Confirm a normal live image using `open_software_camera.bat`.

## After restarting

1. Sign into the same Windows account and wait approximately ten seconds.
2. Check the task:

   ```powershell
   Get-ScheduledTask -TaskName IntegratedCameraSourceKeepAlive |
       Select-Object TaskName, State
   ```

3. Open `open_software_camera.bat`. Confirm a normal live image; close/reopen it three times with other capture apps closed.
4. Confirm that the physical integrated camera remains present and the driver is still Microsoft in Device Manager.
5. Check for exactly one keeper/supervisor using its local state file and Task Manager. Do not publish that machine-local state file.

Test sleep/wake and a later cold boot separately. Keep failures and successful cases distinct rather than repeatedly changing unrelated settings.

## If it fails

- **Task not running:** verify the saved folder still exists, the build outputs and .NET runtime are available, and the task targets the correct account/path. Try `start_camera_helper.bat` once.
- **Device absent:** the holder cannot activate hardware that Windows does not currently enumerate. A waiting helper does not prove the physical fault is fixed.
- **Device present, wrong driver:** an update may have selected an OEM package again. Check before changing anything.
- **Windows Camera green, software preview normal:** this matches the known unresolved Windows Camera path; do not equate it with a failed USB connection.
- **Stream-start error:** close other camera capture applications and retry the software preview alone. The error does not uniquely identify a competing app.

Use `stop_camera_helper.bat` to stop the keeper, or `remove_camera_helper_startup.bat` to remove automatic startup. Neither action restores a separately changed camera driver.

## Status at publication

| Check | Result |
|---|---|
| Normal live software preview | Confirmed |
| Repeated non-zero raw frame samples | Confirmed |
| Holder survives tested capture-client exits | Confirmed in bounded tests |
| Sign-in task installed and started | Confirmed on the investigated machine |
| Single supervised keeper | Confirmed |
| Actual reboot/sign-out recovery | Not yet tested |
| Sleep/wake / long-term permanence | Not established |
| Windows Camera / meeting-app compatibility | Windows Camera still green; meeting apps untested |
