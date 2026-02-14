using AviationApp.Services;
using CoreLocation;
using UIKit;

namespace AviationApp.Platforms.iOS.Services;

public class PlatformService : IPlatformService
{
    private CLLocationManager? _locationManager;
    private TaskCompletionSource<bool>? _permissionTcs;

    public async Task ShowPermissionPopupAndRequestAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                var alert = UIAlertController.Create(
                    "Location Permission Required",
                    "Set Location Settings to Always Allow - it may save your life. Your device location data is used to calculate speed in knots. These data never leave the app and are not collected.",
                    UIAlertControllerStyle.Alert);

                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, async (action) =>
                {
                    await RequestLocationPermissionsAsync();
                }));

                if (UIApplication.SharedApplication.KeyWindow?.RootViewController != null)
                {
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlatformService iOS: Error showing popup: {ex}");
            }
        });
    }

    public Task<bool> ArePermissionsGrantedAsync()
    {
        var status = CLLocationManager.Status;
        bool granted = status == CLAuthorizationStatus.AuthorizedAlways || 
                      status == CLAuthorizationStatus.AuthorizedWhenInUse;
        
        System.Diagnostics.Debug.WriteLine($"PlatformService iOS: ArePermissionsGrantedAsync - {status}, granted: {granted}");
        return Task.FromResult(granted);
    }

    private Task RequestLocationPermissionsAsync()
    {
        _permissionTcs = new TaskCompletionSource<bool>();
        
        _locationManager = new CLLocationManager();
        _locationManager.AuthorizationChanged += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"PlatformService iOS: Authorization changed to {args.Status}");
            
            if (args.Status == CLAuthorizationStatus.AuthorizedAlways || 
                args.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                _permissionTcs?.TrySetResult(true);
            }
            else if (args.Status == CLAuthorizationStatus.Denied || 
                     args.Status == CLAuthorizationStatus.Restricted)
            {
                _permissionTcs?.TrySetResult(false);
            }
        };

        _locationManager.RequestAlwaysAuthorization();
        
        return _permissionTcs.Task;
    }
}
