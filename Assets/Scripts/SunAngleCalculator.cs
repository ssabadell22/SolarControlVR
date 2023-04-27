using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public enum MajorCities { NewYork, SanFrancisco }
public enum Timezones { Eastern, Central, Mountain, Pacific }
public enum Months { January, February, March, April, May, June, July, 
                    August, September, October, November, December }


public class SunAngleCalculator : MonoBehaviour
{
    [SerializeField] private Light _sunLight;

    // Using an enum gives me a selectable list that the user cannot add to; we don't
    // have a mechanism for adding both name and lat/long.
    [Header("Project Location")]
    public MajorCities nearestMajorCity;
    [Tooltip("Only US timezones, for now")] public Timezones timeZone;
    
    [Header("Date")]
    // MISSING: YEAR (would only be used for determining leap years, given the formulae I have)
    public Months month = Months.June;
    [Range(1, 31)] [Tooltip("No validation on month-days yet")] 
        public int dayOfMonth = 21;

    [Header("Time")]
    [Range(0, 23)] [Tooltip("Range is 0:00 to 13:59")] 
        public int hour = 12;
    [Range(0, 59)] public int minute = 0;

    [Button(ButtonSizes.Medium)]
        private void DoTheMath() { DoCalculationAndReport(); }
    [Button(ButtonSizes.Medium)]
        private void ApplyToSunLight() { ApplySolarValuesToSunLight(); }

    // Latitudes to the north are positive
    [Range(-90f, 90f)]
    private float _latitudeDecimalDegrees = 32f;
    // Longitude values are positive to the east of the Prime Meridian
    [Range(-180f, 180f)]
    private float _longitudeDecimalDegrees = 120f;

    private float _sunAngleLerpTime = 3f;
    private float _fullDayLerpTime = 15f;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(_sunLight.type == LightType.Directional, 
            "Lights controlled by the Sun Angle Calculator must be type Directional.");
    }

    public void ApplySolarValuesToSunLight(bool lerp = true)
    {
        var currentCity = CityLatitudeAndLongitude();
        _latitudeDecimalDegrees = currentCity.lat;
        _longitudeDecimalDegrees = currentCity.lng;
        var sunAngles = CalculateSolarValues();
        
        // Until more control is added, North will be assumed to be +Z on the global axis.
        // I am not sure in what order rotations are applied to objects. In our case,
        // we very much need to calculate the Quaternion with two distinct rotations:
        // the azimuth rotation around the Y, then, the elevation rotation around the
        // now-rotated (local) X.
        // Looking at the UI:
        //   At rotational value of (0,0,0), the sun points due-north.
        //   At (90,0,0), the sun is directly overhead.
        //   At (0,45,0), the sun will be on the horizon, in the SW.
        // According to the help docs: rotations are performed around the Z axis, then X axis,
        // and then Y axis, in that order. I feel like that needs to be reversed.  
        
        // Found one example of how to apply the angles to a quaternion. It seems to be working,
        // though it is difficult to know for sure without some real world validation.
        Vector3 angles = new Vector3();
        angles.x = (float)sunAngles.elevation;
        angles.y = (float)sunAngles.azimuth - 180f;
        Quaternion newLocalRotation = Quaternion.Euler(angles);
        Debug.Log($"Setting azimuth to {angles.y} and elevation to {angles.x}");

        if (lerp)
            StartCoroutine(LerpSunAngleChange(newLocalRotation));
        else
            _sunLight.transform.localRotation = newLocalRotation;
    }

    private IEnumerator LerpSunAngleChange(Quaternion newLocalRotation)
    {
        Quaternion oldLocalRotation = _sunLight.transform.localRotation;
        for (float f = 0f; f < _sunAngleLerpTime; f += Time.deltaTime)
        {
            _sunLight.transform.localRotation =
                Quaternion.Lerp(oldLocalRotation, newLocalRotation, f / _sunAngleLerpTime);
            yield return null;
        }
        _sunLight.transform.localRotation = newLocalRotation;
    }

    public void AnimateTheDay()
    {
        StartCoroutine(LerpADayOfSunshine());
    }

    private IEnumerator LerpADayOfSunshine()
    {
        int assumedStartHour = 6;
        int assumedEndHour = 18;
        // Hang on to our exposed properties, restore them at the end
        int userHours = hour;
        int userMinutes = minute;
        
        // Here is where we start
        hour = assumedStartHour;
        minute = 0;
        float startingMinutesIntoDay = (float)(hour * 60);
        float endingMinutesIntoDay = (float)(assumedEndHour * 60);

        for (float f = 0f; f < _fullDayLerpTime; f += Time.deltaTime)
        {
            // lerp based on minutes, then convert back to hours and minutes
            float minutesIntoDay = 
                Mathf.Lerp(startingMinutesIntoDay, endingMinutesIntoDay, f / _fullDayLerpTime);
            hour = (int)Mathf.Floor(minutesIntoDay / 60f);
            minute = (int)(minutesIntoDay - (float)(hour * 60));
            ApplySolarValuesToSunLight(false);
            yield return null;
        }
        
        // restore hour and minute values, but don't reset the sun
        hour = userHours;
        minute = userMinutes;
    }

    // For very quick testing purposes. Source: https://gml.noaa.gov/grad/solcalc/azel.html
    //   New York City, 09:00 Dec 1:
    //      equation of time 11.04
    //      declination -21.81
    //      azimuth 140.29 (2a)
    //      elevation 16.71 (1)
    //   New York City, 17:00 Jun 1: 
    //      equation of time 2.12
    //      declination 22.11
    //      azimuth 279.11 (2b)
    //      elevation 24.19 (1)
    //   New York City, 12:00 Mar 1: 
    //      equation of time -12.33
    //      declination -7.51
    //      azimuth 177.21
    //      elevation 41.76
    //   San Francisco, 09:00 Mar 1: 
    //      equation of time -12.33
    //      declination -7.5
    //      azimuth 122.61 (2a)
    //      elevation 24.78 (1)
    //   San Francisco, 17:00 Sep 1: 
    //      equation of time 0.07
    //      declination 8.07
    //      azimuth 265.74 (2b)
    //      elevation 18.71 (1)
    //   San Francisco, 12:00 Jun 1: 
    //      equation of time 2.12
    //      declination 22.1
    //      azimuth 173.55
    //      elevation 74.25
    // I do see slightly different numbers from this math, but the numbers at the end of
    // the calculations - azimuth and elevation - we are within a degree, and I suspect this
    // is due to the math being an approximation. The NOAA spreadsheet math seems to be
    // more accurate (for one thing it takes the year into account).
    private void DoCalculationAndReport()
    {
        var currentCity = CityLatitudeAndLongitude();
        _latitudeDecimalDegrees = currentCity.lat;
        _longitudeDecimalDegrees = currentCity.lng;
        CalculateSolarValues();
    }

    private int DayOfYear()
    {
        // Order here must match sequence in the enum
        (Months month, int dayIndex) [] monthsAndStartingIndex = 
            {
                (Months.January,   0),
                (Months.February,  31),
                (Months.March,     31+28), // no accounting for leap year here
                (Months.April,     31+28+31),
                (Months.May,       31+28+31+30),
                (Months.June,      31+28+31+30+31),
                (Months.July,      31+28+31+30+31+30),
                (Months.August,    31+28+31+30+31+30+31),
                (Months.September, 31+28+31+30+31+30+31+31),
                (Months.October,   31+28+31+30+31+30+31+31+30),
                (Months.November,  31+28+31+30+31+30+31+31+30+31),
                (Months.December,  31+28+31+30+31+30+31+31+30+31+30),
            };

        int dayOfYearIndex = monthsAndStartingIndex[(int)month].dayIndex;
        return (dayOfYearIndex + dayOfMonth);
    }

    // Returns lat and long for the selected major city, in degrees.
    private (float lat, float lng) CityLatitudeAndLongitude()
    {
        // Order here must match sequence in the enum.
        // Source: https://www.latlong.net/
        (MajorCities city, float lat, float lng) [] cities = 
        {
            (MajorCities.NewYork, 40.712776f, -74.005974f),
            (MajorCities.SanFrancisco, 37.774929f, -122.419418f)
        };
        var currentCity = cities[(int)nearestMajorCity];
        return (currentCity.lat, currentCity.lng);
    }

    // Returns azimuth and elevation in degrees. Note that when applying with Euler
    // rotation, the azimuth rotation must be applied first, as the elevation angle
    // is measured up from the horizon at the azimuth location.
    private (double azimuth, double elevation) CalculateSolarValues()
    {
        double fractionalYear = FractionalYear();
        double equationOfTime = EquationOfTime(fractionalYear);
        double solarDeclinationAngle = SolarDeclinationAngle(fractionalYear);
        double solarDeclinationAngleDeg = Rad2Deg(solarDeclinationAngle);
        double timeOffset = TimeOffset(equationOfTime);
        double trueSolarTime = TrueSolarTime(timeOffset);
        double solarHourAngle = SolarHourAngle(trueSolarTime);
        (double, double) zenithAngles = SolarZenithAngle(solarHourAngle, solarDeclinationAngle);
        double solarElevation1 = SolarElevation(zenithAngles.Item1);
        //double solarElevation2 = SolarElevation(zenithAngles.Item2);
        double solarElevation1Deg = Rad2Deg(solarElevation1);
        //double solarElevation2Deg = Rad2Deg(solarElevation2);
        //(double, double) solarAzimuths1 = SolarAzimuth(zenithAngles.Item1, solarDeclinationAngle);
        //double solarAzimuthDeg1_a = Rad2Deg(solarAzimuths1.Item1);
        //double solarAzimuthDeg1_b = Rad2Deg(solarAzimuths1.Item2);
        (double, double) solarAzimuths2 = SolarAzimuth(zenithAngles.Item2, solarDeclinationAngle);
        double solarAzimuthDeg = 
            (solarHourAngle < 0.0) ? Rad2Deg(solarAzimuths2.Item1) : Rad2Deg(solarAzimuths2.Item2);
        //Debug.Log(
                //$"Fractional year {fractionalYear:0.0#####} radians"
                //+ $"\nTime offset {timeOffset:0.0#####} minutes"
                //+ $"\nTrue solar time {trueSolarTime:0.0#####} minutes"
                //+ $"\nSolar hour angle {solarHourAngle:0.0#####} radians"
                // $"\nEquation of time {equationOfTime:0.0#####} minutes"
                //+ $"\nSolar declination angle {solarDeclinationAngleDeg:0.0#####} degrees"
                // $"\nSolar elevation {solarElevation1Deg:0.0#####} degrees"
                //+ $"\nSolar elevation 2 {solarElevation2Deg:0.0#####} degrees"
                //+ $"\nSolar azimuth 1_a {solarAzimuthDeg1_a:0.0#####} degrees"
                //+ $"\nSolar azimuth 1_b {solarAzimuthDeg1_b:0.0#####} degrees"
                //+ $"\nSolar azimuth 2_a {solarAzimuthDeg2_a:0.0#####} degrees"
                //+ $"\nSolar azimuth 2_b {solarAzimuthDeg2_b:0.0#####} degrees"
                //+ $"\nSolar azimuth {solarAzimuthDeg:0.0#####} degrees"
        //   );
        Debug.Log($"Equation of time {equationOfTime:0.0#####} minutes"
                  + $"\nSolar declination angle {solarDeclinationAngleDeg:0.0#####} degrees");
        Debug.Log($"Solar azimuth {solarAzimuthDeg:0.0##} deg, elevation {solarElevation1Deg:0.0##} deg");

        //(double sunriseHA, double sunsetHA) daysEndHourAngles = SolarHourAngleSunriseSunset(solarDeclinationAngle);
        //(double sunriseMin, double sunsetMin) minutesRiseSet =
        //    TimeOfSunriseSunset(daysEndHourAngles.sunriseHA, daysEndHourAngles.sunsetHA, equationOfTime);
        //Debug.Log($"Sunrise in minutes {minutesRiseSet.sunriseMin} Sunset in minutes {minutesRiseSet.sunsetMin}");
        
        //double hoursBeforeLocalNoon = HoursBeforeLocalNoon();
        //Debug.Log($"Hours before local noon {hoursBeforeLocalNoon}");
        
        return (solarAzimuthDeg, solarElevation1Deg);
    }

    private static double Deg2Rad(double deg) { return (deg * (Math.PI / 180.0)); }
    private static double Rad2Deg(double rad) { return (rad * (180.0 / Math.PI)); }
    
    #region Solar Angle Mathematics
    // Source: https://gml.noaa.gov/grad/solcalc/solareqns.PDF
    
    // Calculate the fractional year, in radians
    private double FractionalYear()
    {
        // For leap year accommodation, use 366 instead of 365
        double baseRadians = (2.0 * Math.PI) / 365.0;
        double baseHours = (double)(hour - 12) / 24.0;
        int dayOfYear = DayOfYear() + dayOfMonth;
        double fracYear = (baseRadians * ((double)(dayOfYear - 1) + baseHours));
        return fracYear;
    }

    // Calculate equation of time (in minutes). fractionalYear expected in radians.
    private double EquationOfTime(double fractionalYear)
    {
        double y = fractionalYear;
        double eqOfTime = 229.18 *
                          (0.000075 
                           + (0.001868 * Math.Cos(y))
                           - (0.032077 * Math.Sin(y))
                           - (0.014615 * Math.Cos(2.0 * y))
                           - (0.040849 * Math.Sin(2.0 * y))
                          );
        return eqOfTime;
    }

    // Calculate the solar declination angle, in radians. fractionalYear expected in radians.
    private double SolarDeclinationAngle(double fractionalYear)
    {
        double y = fractionalYear;
        double declination = 0.006918
                             - (0.399912 * Math.Cos(y))
                             + (0.070257 * Math.Sin(y))
                             - (0.006758 * Math.Cos(2.0 * y))
                             + (0.000907 * Math.Sin(2.0 * y))
                             - (0.002697 * Math.Cos(3.0 * y))
                             + (0.00148 * Math.Sin(3.0 * y));
        return declination;
    }

    // Calculate time offset, in minutes
    private double TimeOffset(double equationOfTime)
    {
        // Order here must match sequence in the enum
        (Timezones tz, double hoursFromUTC) [] timeZoneOffsets = 
        {
            (Timezones.Eastern, -5.0),
            (Timezones.Central, -6.0),
            (Timezones.Mountain, -7.0),
            (Timezones.Pacific, -8.0)
        };
        double tzHoursFromUTC = timeZoneOffsets[(int)timeZone].hoursFromUTC;
        // The formula calls for longitude in degrees
        double timeOffset = equationOfTime 
                            + (4.0 * _longitudeDecimalDegrees) 
                            - (60.0 * tzHoursFromUTC);
        return timeOffset;
    }

    // Calculate true solar time, in minutes
    private double TrueSolarTime(double timeOffset)
    {
        // Assumes hours are in the range 0-23, minutes 0-59, and (when used) seconds 0-59
        double trueSolarTime = ((double)hour * 60.0)
                               + (double)minute
                               // + (seconds / 60.0)
                               + timeOffset;
        return trueSolarTime;
    }

    // Calculate the solar hour angle, in radians
    private double SolarHourAngle(double trueSolarTime)
    {
        double hourAngle = (trueSolarTime / 4.0) - 180.0;
        return Deg2Rad(hourAngle);
    }

    // These two not giving me the right answer, and it might be because declination
    // calc includes fractionalYear, which includes the hours setting; seems circular.
    /*
    private (double, double) SolarHourAngleSunriseSunset(double declination)
    {
        double latitudeInRadians = Deg2Rad(_latitudeDecimalDegrees);
        double specialZenithValue = Deg2Rad(90.833);
        double partA = Math.Cos(specialZenithValue) / (Math.Cos(latitudeInRadians) * Math.Cos(declination));
        double partB = Math.Tan(latitudeInRadians) * Math.Tan(declination);
        double partC = partA - partB;

        Debug.Assert(partC >= -1.0 && partC <= 1.0, $"partC value out of range in SolarHourAngleSunriseSunset {partC}");
        double hourAngleRiseSet = Math.Acos(partC);
        //Debug.Log($"Sunrise Sunset hour angle {hourAngleRiseSet}");
        if (hourAngleRiseSet > 0.0)
            return (hourAngleRiseSet, -hourAngleRiseSet);
        else
            return (-hourAngleRiseSet, hourAngleRiseSet);
    }

    private (double, double) TimeOfSunriseSunset(double hourAngleSunrise, double hourAngleSunset, double equationOfTime)
    {
        double sunrise = 720.0 - (4.0 * (_longitudeDecimalDegrees + hourAngleSunrise)) - equationOfTime;
        double sunset = 720.0 - (4.0 * (_longitudeDecimalDegrees + hourAngleSunset)) - equationOfTime;
        return (sunrise, sunset);
    }
    */
    
    // Can't get this to work, out of time
    /*
    // Alternate math, from https://www.had2know.org/society/sunrise-sunset-time-calculator-formula.html
    private double HoursBeforeLocalNoon()
    {
        int dayOfYear = DayOfYear();
        double tiltOfEarthRadians = Deg2Rad(23.44);
        double latitudeInRadians = Deg2Rad(_latitudeDecimalDegrees);
        //double partA = Math.Sin(Math.PI * 2.0 * (dayOfYear + 284) / 365.0);
        double partA = Math.Sin(360.0 * (dayOfYear + 284) / 365.0);
        double partB = Math.Tan(latitudeInRadians) * Math.Tan(tiltOfEarthRadians * partA);
        double hoursBeforeLocalNoon = Math.Abs((1.0 / 15.0) * Math.Acos(-partB));
        return hoursBeforeLocalNoon;
    }
    */

    // Calculate the solar zenith angle, in radians. hourAngle and declination expected in radians.
    // I am not yet sure what range of values to expect, and since the arc-cos can be + or -,
    // it needs to be validated.
    // Note regarding sunrise and sunset with the zenith angle: per NOAA, sunrise and sunset's
    // zenith is 90.833 deg, which allows for an approximate correction for atmospheric
    // refraction, and the size of the solar disk.
    private (double, double) SolarZenithAngle(double hourAngle, double declination)
    {
        double latitudeInRadians = Deg2Rad(_latitudeDecimalDegrees);
        double phi = (Math.Sin(latitudeInRadians) * Math.Sin(declination))
                     + (Math.Cos(latitudeInRadians) * Math.Cos(declination) * Math.Cos(hourAngle));
        // Math.Acos returns a value in the range 0 <= val <= PI when the asserted condition
        // is met, otherwise it returns NaN.
        Debug.Assert(phi >= -1.0 && phi <= 1.0, $"phi value out of range in SolarZenithAngle {phi}");
        
        // Mathematically, there are 2 solutions, because we need to take the arccos of phi. Essentially:
        // x = +-arccos(y)
        // The NOAA solar angle math doc does not talk about this at all, but from an NOAA spreadsheet,
        // and some empirical testing, we make the determination based on the hour angle.
        double zenithAngleAM = Math.Acos(phi);
        double zenithAnglePM = -Math.Acos(phi);
        return (zenithAngleAM, zenithAnglePM);
    }

    // Convert zenith angle to elevation angle, in radians. zenithAngle expected in radians.
    private double SolarElevation(double zenithAngle)
    {
        return (Math.PI / 2.0) - zenithAngle;
    }

    // Calculate the solar azimuth, in radians. I am not yet sure what range of values to expect,
    // and since the arc-cos can be + or -, it needs to be validated.
    private (double, double) SolarAzimuth(double zenithAngle, double declination)
    {
        double latitudeInRadians = Deg2Rad(_latitudeDecimalDegrees);
        double theta = -(
                            ((Math.Sin(latitudeInRadians) * Math.Cos(zenithAngle)) - Math.Sin(declination))
                          / (Math.Cos(latitudeInRadians) * Math.Sin(zenithAngle))
                        );
        // Math.Acos returns a value in the range 0 <= val <= PI when the asserted condition
        // is met, otherwise it returns NaN.
        Debug.Assert(theta >= -1.0 && theta <= 1.0, $"theta value out of range in SolarAzimuth {theta}");
        
        // I don't know yet if this is the range I need. Mathematically, there are 2 solutions, in
        // the simplest case, when you have cos(x) = y and want to solve for x. They are
        // x = 2*pi*n - arccos(y)
        // x = 2*pi*n + arccos(y)
        // If we keep it simple and assume n=0, then it is just
        // x = +-arccos(y)
        // The NOAA solar angle math doc does not talk about this at all, so, we will need to run
        // some test data through this math and check for expected values.
        double azimuth1 = Math.PI - Math.Acos(theta);
        double azimuth2 = Math.PI + Math.Acos(theta);
        return (azimuth1, azimuth2);
    }
    
    #endregion
    
    #region Sun Calculations Version 2, from an NOAA spreadsheet
    
    // B3 is latitude (+ to N)
    // B4 is longitude (+ to E)
    // B5 is timezone (+ to E)
    // B7 is date
    
    /* Formulas from the Excel spreadsheet; see NOAA_Solar_Calculation_Day.xls
       I think ultimately these would result in more accurate calculations, but I need to move on 
       and start implementing interactivity in the prototype.
     
     Julian Day (F91) = D91 + 2415018.5 + E91 - $B$5 / 24
     Julian century (G91) = (F91 - 2451545) / 36525
     Geom Mean Long Sun (deg) (I91) = MOD(280.46646 + G91 * (36000.76983 + G91 * 0.0003032), 360)
     Geom Mean Anom Sun (deg) (J91) = 357.52911 + G91 * (35999.05029 - 0.0001537 * G91)
     Eccent Earth Orbit (K91) = 0.016708634 - G91 * (0.000042037 + 0.0000001267 * G91)
       - always seems to be 0.016699
     Sun Eq of Ctr (L91) =SIN(RADIANS(J91))*(1.914602-G91*(0.004817+0.000014*G91))+SIN(RADIANS(2*J91))*(0.019993-0.000101*G91)+SIN(RADIANS(3*J91))*0.000289
     
     ... I stopped this effort for now ...
     
     
     */
    
    #endregion
}
