#include "utfuel_shift.h"

#include "../../config/utfuel_config.h"


bool utfuel_should_shift(
    uint32_t rpm,
    utfuel_gear_t gear
)
{
    if (
        gear < UTFUEL_GEAR_1 ||
        gear > UTFUEL_GEAR_6
    )
    {
        return false;
    }


    const utfuel_vehicle_config_t *config =
        utfuel_get_vehicle_config();


    unsigned int index =
        (unsigned int)gear - 1;


    return rpm >= config->shift_rpm[index];
}