#include "utfuel_map.h"


#define MAP_MIN_VOLTAGE 0.50f
#define MAP_MAX_VOLTAGE 4.50f

#define MAP_MIN_KPA 20.0f
#define MAP_MAX_KPA 250.0f


float utfuel_map_from_voltage(
    float voltage
)
{
    if (voltage < MAP_MIN_VOLTAGE)
    {
        voltage = MAP_MIN_VOLTAGE;
    }

    if (voltage > MAP_MAX_VOLTAGE)
    {
        voltage = MAP_MAX_VOLTAGE;
    }


    float normalized =
        (voltage - MAP_MIN_VOLTAGE) /
        (MAP_MAX_VOLTAGE - MAP_MIN_VOLTAGE);


    return MAP_MIN_KPA +
        normalized *
        (MAP_MAX_KPA - MAP_MIN_KPA);
}