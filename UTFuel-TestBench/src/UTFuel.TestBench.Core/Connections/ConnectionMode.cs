namespace UTFuel.TestBench.Core;


public enum ConnectionMode
{
    /*
     * =========================================
     * LOCAL SIMULATION
     * =========================================
     *
     * TestBench
     *      ↓
     * utfuel_host.exe
     */

    LocalSimulation,


    /*
     * =========================================
     * HARDWARE BENCH
     * =========================================
     *
     * TestBench
     *      ↓ USB
     * ESP32-S3 Simulator
     *      ↓ physical signals
     * STM32 UTFuel ECU
     *      ↓ USB
     * TestBench
     */

    HardwareBench
}