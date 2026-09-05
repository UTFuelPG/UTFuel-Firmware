namespace UTFuel.TestBench.Gui.Models;

public sealed record LiveTelemetrySample(
    uint Rpm,
    double TpsPercent,
    double MapKpa,
    double SpeedKmh,
    double BatteryVoltage,
    int Gear,
    bool ShiftWarning,
    double RttMs
);