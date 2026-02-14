using CoreLocation;
using CommunityToolkit.Mvvm.Messaging;

namespace AviationApp.Services;

public class LocationService : NSObject, ICLocationManagerDelegate
{
    private CLLocationManager? _locationManager;
    private const float DMMS_THRESHOLD = 258.0f; // Knots
    private const float METERS_PER_SECOND_TO_KNOTS = 1.94384f;
    private DateTime _lastAlertTime = DateTime.MinValue;
    private readonly TimeSpan _alertThrottleInterval = TimeSpan.FromSeconds(5);
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning)
        {
            System.Diagnostics.Debug.WriteLine("LocationService iOS: Already running");
            return;
        }

        try
        {
            _locationManager = new CLLocationManager
            {
                DesiredAccuracy = CLLocationAccuracy.Best,
                DistanceFilter = 5,
                Delegate = this
            };

            _locationManager.StartUpdatingLocation();
            _isRunning = true;
            System.Diagnostics.Debug.WriteLine("LocationService iOS: Started location updates");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocationService iOS: Start error: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _locationManager?.StopUpdatingLocation();
            _isRunning = false;
            System.Diagnostics.Debug.WriteLine("LocationService iOS: Stopped location updates");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocationService iOS: Stop error: {ex.Message}");
        }
    }

    [Export("locationManager:didUpdateLocations:")]
    public void DidUpdateLocations(CLLocationManager manager, CLLocation[] locations)
    {
        if (locations == null || locations.Length == 0)
            return;

        var location = locations[locations.Length - 1];
        
        System.Diagnostics.Debug.WriteLine($"LocationService iOS: Location updated - Lat={location.Coordinate.Latitude}, Lon={location.Coordinate.Longitude}, Alt={location.Altitude}, Speed={location.Speed * METERS_PER_SECOND_TO_KNOTS:F1} knots");

        try
        {
            var locationMessage = new LocationMessage(location, DateTime.Now);
            WeakReferenceMessenger.Default.Send(locationMessage);

            // Check for DMMS alert
            if (location.Speed >= 0)
            {
                float speedKnots = (float)(location.Speed * METERS_PER_SECOND_TO_KNOTS);
                if (speedKnots < DMMS_THRESHOLD)
                {
                    string alertMessage = $"Acceleration plateau detected at {speedKnots:F1} knots, below DMMS {DMMS_THRESHOLD:F1}";
                    System.Diagnostics.Debug.WriteLine($"LocationService iOS: Triggering alert: {alertMessage}");
                    TriggerAlert(alertMessage);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocationService iOS: Error processing location: {ex.Message}");
        }
    }

    [Export("locationManager:didFailWithError:")]
    public void DidFailWithError(CLLocationManager manager, NSError error)
    {
        System.Diagnostics.Debug.WriteLine($"LocationService iOS: Location error: {error.LocalizedDescription}");
    }

    [Export("locationManagerDidChangeAuthorization:")]
    public void DidChangeAuthorization(CLLocationManager manager)
    {
        var status = manager.Status;
        System.Diagnostics.Debug.WriteLine($"LocationService iOS: Authorization changed: {status}");

        if (status == CLAuthorizationStatus.AuthorizedAlways || 
            status == CLAuthorizationStatus.AuthorizedWhenInUse)
        {
            if (!_isRunning)
            {
                Start();
            }
        }
    }

    private void TriggerAlert(string alertMessage)
    {
        try
        {
            if (DateTime.Now - _lastAlertTime < _alertThrottleInterval)
            {
                System.Diagnostics.Debug.WriteLine($"LocationService iOS: Alert throttled: {alertMessage}");
                return;
            }
            _lastAlertTime = DateTime.Now;

            // Send alert message through messenger
            WeakReferenceMessenger.Default.Send(new AlertMessage(alertMessage));
            System.Diagnostics.Debug.WriteLine($"LocationService iOS: Alert sent: {alertMessage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocationService iOS: TriggerAlert error: {ex.Message}");
        }
    }
}

public class LocationMessage
{
    public CLLocation Location { get; }
    public DateTime UpdateTime { get; }

    public LocationMessage(CLLocation location, DateTime updateTime)
    {
        Location = location;
        UpdateTime = updateTime;
    }
}

public class AlertMessage
{
    public string Message { get; }

    public AlertMessage(string message)
    {
        Message = message;
    }
}
