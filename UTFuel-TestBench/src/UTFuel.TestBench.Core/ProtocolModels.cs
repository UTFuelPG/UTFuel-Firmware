using System.Globalization;

namespace UTFuel.TestBench.Core;

public sealed record InputPacket(
    uint SequenceId,
    double TpsVoltage,
    double MapVoltage,
    double CoolantResistance,
    double IntakeResistance,
    double BatteryVoltage,
    uint Rpm,
    double SpeedKmh
)
{
    public string ToProtocolLine()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "IN,{0},{1:F2},{2:F2},{3:F0},{4:F0},{5:F2},{6},{7:F2}",
            SequenceId,
            TpsVoltage,
            MapVoltage,
            CoolantResistance,
            IntakeResistance,
            BatteryVoltage,
            Rpm,
            SpeedKmh
        );
    }
}


public sealed record OutputPacket(
    uint SequenceId,
    uint Rpm,
    double TpsPercent,
    double MapKpa,
    double BatteryVoltage,
    double SpeedKmh,
    int Gear,
    bool ShiftWarning
)
{
    public static bool TryParse(
        string line,
        out OutputPacket? packet
    )
    {
        packet = null;

        string[] parts = line.Split(',');

        if (parts.Length != 9)
            return false;

        if (parts[0] != "OUT")
            return false;

        if (!uint.TryParse(parts[1], out uint sequence))
            return false;

        if (!uint.TryParse(parts[2], out uint rpm))
            return false;

        if (!double.TryParse(
                parts[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double tps))
            return false;

        if (!double.TryParse(
                parts[4],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double map))
            return false;

        if (!double.TryParse(
                parts[5],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double battery))
            return false;

        if (!double.TryParse(
                parts[6],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double speed))
            return false;

        if (!int.TryParse(parts[7], out int gear))
            return false;

        if (!int.TryParse(parts[8], out int shift))
            return false;

        packet = new OutputPacket(
            sequence,
            rpm,
            tps,
            map,
            battery,
            speed,
            gear,
            shift == 1
        );

        return true;
    }
}


public sealed record FirmwareResponse(
    string Line,
    double RttMilliseconds
);