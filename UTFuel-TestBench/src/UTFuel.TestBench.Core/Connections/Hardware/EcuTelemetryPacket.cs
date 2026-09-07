using System;
using System.Diagnostics;
using System.Globalization;

namespace UTFuel.TestBench.Core;


/*
 * =========================================
 * ECU TELEMETRY PACKET
 * =========================================
 *
 * Serial format:
 *
 * ECU_DATA,
 * <sample_id>,
 * <ecu_uptime_us>,
 * <rpm>,
 * <tps_centi_pct>,
 * <map_deci_kpa>,
 * <battery_mV>,
 * <speed_centi_kmh>,
 * <gear>,
 * <shift>
 *
 * Example:
 *
 * ECU_DATA,18291,82451230,7201,7312,884,13780,10810,4,0
 *
 * Meaning:
 *
 * Sample ID:       18291
 * ECU uptime:      82.451230 s
 * RPM:             7201
 * TPS:             73.12 %
 * MAP:             88.4 kPa
 * Battery:         13.780 V
 * Speed:           108.10 km/h
 * Gear:            4
 * Shift warning:   false
 *
 * IMPORTANT:
 *
 * sample_id belongs exclusively to the ECU
 * telemetry stream.
 *
 * It is NOT the same sequence used by SIM_SET.
 */


/*
 * =========================================
 * PACKET
 * =========================================
 */

public sealed record EcuTelemetryPacket(
    uint SampleId,
    ulong EcuUptimeUs,
    uint Rpm,
    double TpsPercent,
    double MapKpa,
    double BatteryVoltage,
    double SpeedKmh,
    int Gear,
    bool ShiftWarning,
    DateTime HostReceivedUtc,
    long HostReceivedTimestamp
)
{
    /*
     * =====================================
     * PROTOCOL CONSTANT
     * =====================================
     */

    private const string PacketPrefix =
        "ECU_DATA";


    /*
     * =====================================
     * PARSE USING CURRENT HOST TIME
     * =====================================
     */

    public static bool TryParse(
        string? line,
        out EcuTelemetryPacket packet
    )
    {
        return TryParse(
            line,
            DateTime.UtcNow,
            Stopwatch.GetTimestamp(),
            out packet
        );
    }


    /*
     * =====================================
     * PARSE WITH EXPLICIT UTC TIME
     * =====================================
     */

    public static bool TryParse(
        string? line,
        DateTime hostReceivedUtc,
        out EcuTelemetryPacket packet
    )
    {
        return TryParse(
            line,
            hostReceivedUtc,
            Stopwatch.GetTimestamp(),
            out packet
        );
    }


    /*
     * =====================================
     * PARSE WITH EXPLICIT HOST TIMESTAMPS
     * =====================================
     */

    public static bool TryParse(
        string? line,
        DateTime hostReceivedUtc,
        long hostReceivedTimestamp,
        out EcuTelemetryPacket packet
    )
    {
        packet =
            null!;


        if (
            string.IsNullOrWhiteSpace(
                line
            )
        )
        {
            return false;
        }


        string[] fields =
            line
                .Trim()
                .Split(
                    ',',
                    StringSplitOptions.TrimEntries
                );


        /*
         * Expected fields:
         *
         * 0 ECU_DATA
         * 1 sample_id
         * 2 ecu_uptime_us
         * 3 rpm
         * 4 tps_centi_pct
         * 5 map_deci_kpa
         * 6 battery_mV
         * 7 speed_centi_kmh
         * 8 gear
         * 9 shift
         */

        if (
            fields.Length !=
            10
        )
        {
            return false;
        }


        if (
            !string.Equals(
                fields[0],
                PacketPrefix,
                StringComparison.Ordinal
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * SAMPLE ID
         * =================================
         */

        if (
            !uint.TryParse(
                fields[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint sampleId
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * ECU UPTIME
         * =================================
         */

        if (
            !ulong.TryParse(
                fields[2],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ulong ecuUptimeUs
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * RPM
         * =================================
         */

        if (
            !uint.TryParse(
                fields[3],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint rpm
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * TPS
         * =================================
         */

        if (
            !uint.TryParse(
                fields[4],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint tpsCentiPercent
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * MAP
         * =================================
         */

        if (
            !uint.TryParse(
                fields[5],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint mapDeciKpa
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * BATTERY
         * =================================
         */

        if (
            !uint.TryParse(
                fields[6],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint batteryMillivolts
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * SPEED
         * =================================
         */

        if (
            !uint.TryParse(
                fields[7],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint speedCentiKmh
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * GEAR
         * =================================
         */

        if (
            !int.TryParse(
                fields[8],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int gear
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * SHIFT WARNING
         * =================================
         */

        if (
            !TryParseBooleanFlag(
                fields[9],
                out bool shiftWarning
            )
        )
        {
            return false;
        }


        /*
         * =================================
         * TRANSPORT -> ENGINEERING UNITS
         * =================================
         */

        double tpsPercent =
            tpsCentiPercent /
            100.0;


        double mapKpa =
            mapDeciKpa /
            10.0;


        double batteryVoltage =
            batteryMillivolts /
            1000.0;


        double speedKmh =
            speedCentiKmh /
            100.0;


        /*
         * =================================
         * CREATE PACKET
         * =================================
         */

        packet =
            new EcuTelemetryPacket(
                SampleId:
                    sampleId,

                EcuUptimeUs:
                    ecuUptimeUs,

                Rpm:
                    rpm,

                TpsPercent:
                    tpsPercent,

                MapKpa:
                    mapKpa,

                BatteryVoltage:
                    batteryVoltage,

                SpeedKmh:
                    speedKmh,

                Gear:
                    gear,

                ShiftWarning:
                    shiftWarning,

                HostReceivedUtc:
                    NormalizeUtc(
                        hostReceivedUtc
                    ),

                HostReceivedTimestamp:
                    hostReceivedTimestamp
            );


        return true;
    }


    /*
     * =====================================
     * BOOLEAN FLAG
     * =====================================
     */

    private static bool TryParseBooleanFlag(
        string value,
        out bool result
    )
    {
        switch (
            value
        )
        {
            case "0":

                result =
                    false;

                return true;


            case "1":

                result =
                    true;

                return true;


            default:

                result =
                    false;

                return false;
        }
    }


    /*
     * =====================================
     * NORMALIZE UTC TIMESTAMP
     * =====================================
     */

    private static DateTime NormalizeUtc(
        DateTime value
    )
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc
                )
        };
    }
}