namespace UTFuel.TestBench.Core;


public interface ITestBenchSession :
    IAsyncDisposable
{
    /*
     * =========================================
     * MODE
     * =========================================
     */

    ConnectionMode Mode
    {
        get;
    }



    /*
     * =========================================
     * START
     * =========================================
     */

    Task StartAsync();



    /*
     * =========================================
     * COMMUNICATION TEST
     * =========================================
     */

    Task<double> PingAsync(
        uint sequence,
        TimeSpan timeout
    );



    /*
     * =========================================
     * TEST FRAME
     * =========================================
     *
     * Local:
     *
     * InputPacket
     *      ↓
     * HOST
     *      ↓
     * OutputPacket
     *
     *
     * Hardware:
     *
     * InputPacket
     *      ↓
     * ESP32
     *      ↓
     * physical signals
     *      ↓
     * STM32
     *      ↓
     * ECU telemetry
     *      ↓
     * OutputPacket
     */

    Task<(OutputPacket Packet, double RttMs)>
        SendInputAsync(
            InputPacket input,
            TimeSpan timeout
        );
}