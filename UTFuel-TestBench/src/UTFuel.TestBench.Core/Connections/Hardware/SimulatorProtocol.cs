using System;
using System.Globalization;

namespace UTFuel.TestBench.Core;


/*
 * =========================================
 * SIMULATOR RESPONSE TYPE
 * =========================================
 */

public enum SimulatorResponseType
{
    Pong,
    Ack,
    Error
}


/*
 * =========================================
 * SIMULATOR RESPONSE
 * =========================================
 */

public sealed record SimulatorResponse(
    SimulatorResponseType Type,
    uint Sequence,
    ulong? AppliedAtUs = null,
    string? ErrorCode = null
);


/*
 * =========================================
 * SIMULATOR APPLY RESULT
 * =========================================
 */

public sealed record SimulatorApplyResult(
    uint Sequence,
    double AckRttMs,
    ulong? AppliedAtUs
);


/*
 * =========================================
 * SIMULATOR PROTOCOL
 * =========================================
 */

public static class SimulatorProtocol
{
    /*
     * =====================================
     * COMMAND NAMES
     * =====================================
     */

    private const string PingCommand =
        "PING";

    private const string SetCommand =
        "SIM_SET";

    private const string PongResponse =
        "PONG";

    private const string AckResponse =
        "SIM_ACK";

    private const string ErrorResponse =
        "SIM_ERR";


    /*
     * =====================================
     * BUILD PING
     * =====================================
     */

    public static string BuildPing(
        uint sequence
    )
    {
        return
            $"{PingCommand},{sequence}";
    }


    /*
     * =====================================
     * BUILD SIM_SET
     * =====================================
     */

    public static string BuildSet(
        InputPacket input
    )
    {
        ArgumentNullException.ThrowIfNull(
            input
        );


        int tpsMillivolts =
            VoltageToMillivolts(
                input.TpsVoltage
            );


        int mapMillivolts =
            VoltageToMillivolts(
                input.MapVoltage
            );


        uint coolantResistanceOhms =
            ResistanceToOhms(
                input.CoolantResistance
            );


        uint intakeResistanceOhms =
            ResistanceToOhms(
                input.IntakeResistance
            );


        int batteryMillivolts =
            VoltageToMillivolts(
                input.BatteryVoltage
            );


        uint speedCentiKmh =
            SpeedToCentiKmh(
                input.SpeedKmh
            );


        string[] fields =
        {
            SetCommand,

            input.SequenceId.ToString(
                CultureInfo.InvariantCulture
            ),

            tpsMillivolts.ToString(
                CultureInfo.InvariantCulture
            ),

            mapMillivolts.ToString(
                CultureInfo.InvariantCulture
            ),

            coolantResistanceOhms.ToString(
                CultureInfo.InvariantCulture
            ),

            intakeResistanceOhms.ToString(
                CultureInfo.InvariantCulture
            ),

            batteryMillivolts.ToString(
                CultureInfo.InvariantCulture
            ),

            input.Rpm.ToString(
                CultureInfo.InvariantCulture
            ),

            speedCentiKmh.ToString(
                CultureInfo.InvariantCulture
            )
        };


        return string.Join(
            ",",
            fields
        );
    }


    /*
     * =====================================
     * PARSE RESPONSE
     * =====================================
     */

    public static bool TryParseResponse(
        string? line,
        out SimulatorResponse response
    )
    {
        response =
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


        if (
            fields.Length <
            2
        )
        {
            return false;
        }


        if (
            !uint.TryParse(
                fields[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint sequence
            )
        )
        {
            return false;
        }


        return fields[0] switch
        {
            PongResponse =>
                TryParsePong(
                    fields,
                    sequence,
                    out response
                ),

            AckResponse =>
                TryParseAcknowledgement(
                    fields,
                    sequence,
                    out response
                ),

            ErrorResponse =>
                TryParseError(
                    fields,
                    sequence,
                    out response
                ),

            _ =>
                false
        };
    }


    /*
     * =====================================
     * PARSE PONG
     * =====================================
     */

    private static bool TryParsePong(
        string[] fields,
        uint sequence,
        out SimulatorResponse response
    )
    {
        response =
            null!;


        /*
         * Expected:
         *
         * PONG,<seq>
         */

        if (
            fields.Length !=
            2
        )
        {
            return false;
        }


        response =
            new SimulatorResponse(
                Type:
                    SimulatorResponseType.Pong,

                Sequence:
                    sequence
            );


        return true;
    }


    /*
     * =====================================
     * PARSE ACK
     * =====================================
     */

    private static bool TryParseAcknowledgement(
        string[] fields,
        uint sequence,
        out SimulatorResponse response
    )
    {
        response =
            null!;


        /*
         * Valid formats:
         *
         * SIM_ACK,<seq>
         *
         * SIM_ACK,<seq>,<esp_applied_us>
         */

        if (
            fields.Length !=
            2 &&
            fields.Length !=
            3
        )
        {
            return false;
        }


        ulong? appliedAtUs =
            null;


        if (
            fields.Length ==
            3
        )
        {
            if (
                !ulong.TryParse(
                    fields[2],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out ulong parsedTimestamp
                )
            )
            {
                return false;
            }


            appliedAtUs =
                parsedTimestamp;
        }


        response =
            new SimulatorResponse(
                Type:
                    SimulatorResponseType.Ack,

                Sequence:
                    sequence,

                AppliedAtUs:
                    appliedAtUs
            );


        return true;
    }


    /*
     * =====================================
     * PARSE ERROR
     * =====================================
     */

    private static bool TryParseError(
        string[] fields,
        uint sequence,
        out SimulatorResponse response
    )
    {
        response =
            null!;


        /*
         * Expected:
         *
         * SIM_ERR,<seq>,<code>
         */

        if (
            fields.Length !=
            3
        )
        {
            return false;
        }


        string errorCode =
            fields[2];


        if (
            string.IsNullOrWhiteSpace(
                errorCode
            )
        )
        {
            return false;
        }


        response =
            new SimulatorResponse(
                Type:
                    SimulatorResponseType.Error,

                Sequence:
                    sequence,

                ErrorCode:
                    errorCode
            );


        return true;
    }


    /*
     * =====================================
     * VOLTAGE -> MILLIVOLTS
     * =====================================
     */

    private static int VoltageToMillivolts(
        double voltage
    )
    {
        if (
            double.IsNaN(
                voltage
            ) ||
            double.IsInfinity(
                voltage
            )
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(voltage),
                "Voltage must be a finite number."
            );
        }


        return checked(
            (int)Math.Round(
                voltage *
                1000.0,

                MidpointRounding.AwayFromZero
            )
        );
    }


    /*
     * =====================================
     * RESISTANCE -> OHMS
     * =====================================
     */

    private static uint ResistanceToOhms(
        double resistance
    )
    {
        if (
            double.IsNaN(
                resistance
            ) ||
            double.IsInfinity(
                resistance
            ) ||
            resistance <
            0.0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(resistance),
                "Resistance must be a finite non-negative value."
            );
        }


        return checked(
            (uint)Math.Round(
                resistance,
                MidpointRounding.AwayFromZero
            )
        );
    }


    /*
     * =====================================
     * SPEED -> 0.01 km/h
     * =====================================
     */

    private static uint SpeedToCentiKmh(
        double speedKmh
    )
    {
        if (
            double.IsNaN(
                speedKmh
            ) ||
            double.IsInfinity(
                speedKmh
            ) ||
            speedKmh <
            0.0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedKmh),
                "Speed must be a finite non-negative value."
            );
        }


        return checked(
            (uint)Math.Round(
                speedKmh *
                100.0,

                MidpointRounding.AwayFromZero
            )
        );
    }
}