using System.IO.Ports;

namespace UTFuel.TestBench.Core;

public static class SerialPortDiscovery
{
    public static IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPort
            .GetPortNames()
            .OrderBy(
                port =>
                    GetPortNumber(
                        port
                    )
            )
            .ThenBy(
                port =>
                    port
            )
            .ToArray();
    }


    private static int GetPortNumber(
        string port
    )
    {
        if (
            port.StartsWith(
                "COM",
                StringComparison.OrdinalIgnoreCase
            ) &&
            int.TryParse(
                port[3..],
                out int number
            )
        )
        {
            return number;
        }


        return int.MaxValue;
    }
}