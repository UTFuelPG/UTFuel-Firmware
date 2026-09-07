#include <math.h>

#include "utfuel_gear.h"

#include "../../config/utfuel_config.h"


#define GEAR_RATIO_TOLERANCE 0.15f


utfuel_gear_t utfuel_detect_gear(
    uint32_t engine_rpm,
    float vehicle_speed_kmh
)
{
    if (engine_rpm < 500)
    {
        return UTFUEL_GEAR_UNKNOWN;
    }

    if (vehicle_speed_kmh < 2.0f)
    {
        return UTFUEL_GEAR_NEUTRAL;
    }

    const utfuel_vehicle_config_t *config =
        utfuel_get_vehicle_config();


    float vehicle_speed_ms =
        vehicle_speed_kmh / 3.6f;


    float wheel_rps =
        vehicle_speed_ms /
        config->tire_circumference_m;


    float wheel_rpm =
        wheel_rps * 60.0f;


    if (wheel_rpm <= 0.0f)
    {
        return UTFUEL_GEAR_UNKNOWN;
    }


    float measured_ratio =
        (float)engine_rpm /
        (
            wheel_rpm *
            config->final_drive_ratio
        );


    float smallest_error = 9999.0f;

    int best_gear = -1;


    for (
        int i = 0;
        i < UTFUEL_GEAR_COUNT;
        i++
    )
    {
        float error =
            fabsf(
                measured_ratio -
                config->gear_ratios[i]
            );


        if (error < smallest_error)
        {
            smallest_error = error;
            best_gear = i;
        }
    }


    if (
        best_gear >= 0 &&
        smallest_error <= GEAR_RATIO_TOLERANCE
    )
    {
        return
            (utfuel_gear_t)(best_gear + 1);
    }


    return UTFUEL_GEAR_UNKNOWN;
}