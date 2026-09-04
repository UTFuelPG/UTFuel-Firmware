#include "utfuel_tps.h"

#define TPS_MIN_VOLTAGE 0.50f
#define TPS_MAX_VOLTAGE 4.50f


float utfuel_tps_from_voltage(
    float voltage
)
{
    float tps;

    tps =
        (voltage - TPS_MIN_VOLTAGE) /
        (TPS_MAX_VOLTAGE - TPS_MIN_VOLTAGE);

    tps *= 100.0f;

    if (tps < 0.0f)
    {
        tps = 0.0f;
    }

    if (tps > 100.0f)
    {
        tps = 100.0f;
    }

    return tps;
}