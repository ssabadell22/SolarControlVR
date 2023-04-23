using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SunAngleCalculator : MonoBehaviour
{
    [SerializeField] private Light _sunLight;

    private enum MajorCities { NewYork, SanFrancisco }
    private enum Timezones { Eastern, Central, Mountain, Pacific }
    // Using an enum gives me a selectable list that the user cannot add to; we don't
    // have a mechanism for adding both name and lat/long.
    [Header("Project Location")]
    [SerializeField] private MajorCities _nearestMajorCity;
    [SerializeField] [Tooltip("Only US timezones, for now")]
        private Timezones _timeZone;
    
    private enum Months { January, February, March, April, May, June, July, August, 
        September, October, November, December }
    [Header("Date")]
    // MISSING: YEAR (would only be used for determining leap years, given the formulae I have)
    [SerializeField] private Months _month = Months.June;
    [SerializeField] [Range(1, 31)] [Tooltip("No validation on month-days yet")] 
        private int _dayOfMonth = 21;

    [Header("Time")]
    [SerializeField] [Range(0, 23)] [Tooltip("Range is 0:00 to 13:59")] 
        private int _hour = 12;
    [SerializeField] [Range(0, 59)] private int _minute = 0;

    [Button(ButtonSizes.Medium)]
        private void DoTheMath() { DoCalculationAndReport(); }
    
    // Latitudes to the north are positive
    [Range(-90f, 90f)]
    private float _latitudeDecimalDegrees = 32f;
    // Longitude values are positive to the east of the Prime Meridian
    [Range(-180f, 180f)]
    private float _longitudeDecimalDegrees = 120f;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(_sunLight.type == LightType.Directional, 
            "Lights controlled by the Sun Angle Calculator must be type Directional.");
    }

    // Update is called once per frame
    void Update()
    {
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

        int dayOfYearIndex = monthsAndStartingIndex[(int)_month].dayIndex;
        return (dayOfYearIndex + _dayOfMonth);
    }

    // Returns azimuth and elevation in degrees. Note that when applying with Euler
    // rotation, the azimuth rotation must be applied first, as the elevation angle
    // is measured up from the horizon at the azimuth location.
    private (double azimuth, double elevation) CalculateSolarValues()
    {
        double fractionalYear = FractionalYear();
        double equationOfTime = EquationOfTime(fractionalYear);
        double solarDeclinationAngle = SolarDeclinationAngle(fractionalYear);
        //double solarDeclinationAngleDeg = Rad2Deg(solarDeclinationAngle);
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
        Debug.Log(
                //$"Fractional year {fractionalYear:0.0#####} radians"
                //+ $"\nTime offset {timeOffset:0.0#####} minutes"
                //+ $"\nTrue solar time {trueSolarTime:0.0#####} minutes"
                //+ $"\nSolar hour angle {solarHourAngle:0.0#####} radians"
                // $"\nEquation of time {equationOfTime:0.0#####} minutes"
                //+ $"\nSolar declination angle {solarDeclinationAngleDeg:0.0#####} degrees"
                 $"\nSolar elevation {solarElevation1Deg:0.0#####} degrees"
                //+ $"\nSolar elevation 2 {solarElevation2Deg:0.0#####} degrees"
                //+ $"\nSolar azimuth 1_a {solarAzimuthDeg1_a:0.0#####} degrees"
                //+ $"\nSolar azimuth 1_b {solarAzimuthDeg1_b:0.0#####} degrees"
                //+ $"\nSolar azimuth 2_a {solarAzimuthDeg2_a:0.0#####} degrees"
                //+ $"\nSolar azimuth 2_b {solarAzimuthDeg2_b:0.0#####} degrees"
                + $"\nSolar azimuth {solarAzimuthDeg:0.0#####} degrees"
            );
        return (solarAzimuthDeg, solarElevation1Deg);
    }

    // For very quick testing purposes. Source: https://gml.noaa.gov/grad/solcalc/azel.html
    //   New York City, 09:00 Dec 1:
    //      equation of time 11.04
    //      declination -21.81
    //      elevation 16.71 (1)
    //      azimuth 140.29 (2a)
    //   New York City, 17:00 Jun 1: 
    //      equation of time 2.12
    //      declination 22.11
    //      elevation 24.19 (1)
    //      azimuth 279.11 (2b)
    //   San Francisco, 09:00 Mar 1: 
    //      equation of time -12.33
    //      declination -7.5
    //      elevation 24.78 (1)
    //      azimuth 122.61 (2a)
    //   San Francisco, 17:00 Sep 1: 
    //      equation of time 0.07
    //      declination 8.07
    //      elevation 18.71 (1)
    //      azimuth 265.74 (2b)
    // I do see slightly different numbers from this math, but the numbers at the end of
    // the calculations - azimuth and elevation - we are within a degree, and I suspect this
    // is due to the math being an approximation. The NOAA spreadsheet math seems to be
    // more accurate (for one thing it takes the year into account).
    private void DoCalculationAndReport()
    {
        // Order here must match sequence in the enum.
        // Source: https://www.latlong.net/
        (MajorCities city, float lat, float lng) [] cities = 
        {
            (MajorCities.NewYork, 40.712776f, -74.005974f),
            (MajorCities.SanFrancisco, 37.774929f, -122.419418f)
        };
        var currentCity = cities[(int)_nearestMajorCity];
        _latitudeDecimalDegrees = currentCity.lat;
        _longitudeDecimalDegrees = currentCity.lng;
        CalculateSolarValues();
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
        double baseHours = (double)(_hour - 12) / 24.0;
        int dayOfYear = DayOfYear() + _dayOfMonth;
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
        double tzHoursFromUTC = timeZoneOffsets[(int)_timeZone].hoursFromUTC;
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
        double trueSolarTime = ((double)_hour * 60.0)
                               + (double)_minute
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
