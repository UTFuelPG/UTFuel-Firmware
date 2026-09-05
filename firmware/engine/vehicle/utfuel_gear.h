#ifndef UTFUEL_GEAR_H
#define UTFUEL_GEAR_H

#include <stdint.h>

#include "../../core/utfuel_types.h"


utfuel_gear_t utfuel_detect_gear(
    uint32_t engine_rpm,
    float vehicle_speed_kmh
);

#endif