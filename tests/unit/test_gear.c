#include <stdio.h>

#include "utfuel_gear.h"
#include "utfuel_config.h"


static int assert_gear(
    const char *name,
    utfuel_gear_t expected,
    utfuel_gear_t actual
)
{
    if (expected != actual)
    {
        printf(
            "[FAIL] %s | expected gear %d | actual %d\n",
            name,
            expected,
            actual
        );

        return 1;
    }

    printf(
        "[PASS] %s | gear %d\n",
        name,
        actual
    );

    return 0;
}


static float calculate_speed_for_gear(
    unsigned int rpm,
    float gear_ratio
)
{
    const utfuel_vehicle_config_t *config =
        utfuel_get_vehicle_config();

    float wheel_rpm =
        (float)rpm /
        (
            gear_ratio *
            config->final_drive_ratio
        );

    float wheel_rps =
        wheel_rpm / 60.0f;

    float speed_ms =
        wheel_rps *
        config->tire_circumference_m;

    return speed_ms * 3.6f;
}


int main(void)
{
    int failures = 0;

    const utfuel_vehicle_config_t *config =
        utfuel_get_vehicle_config();


    const unsigned int rpm = 6000;


    for (
        int i = 0;
        i < UTFUEL_GEAR_COUNT;
        i++
    )
    {
        float speed =
            calculate_speed_for_gear(
                rpm,
                config->gear_ratios[i]
            );

        utfuel_gear_t result =
            utfuel_detect_gear(
                rpm,
                speed
            );

        failures += assert_gear(
            "Gear detection",
            (utfuel_gear_t)(i + 1),
            result
        );
    }


    failures += assert_gear(
        "Vehicle stopped",
        UTFUEL_GEAR_NEUTRAL,
        utfuel_detect_gear(
            1000,
            0.0f
        )
    );


    failures += assert_gear(
        "Engine stopped",
        UTFUEL_GEAR_UNKNOWN,
        utfuel_detect_gear(
            0,
            50.0f
        )
    );


    if (failures > 0)
    {
        printf(
            "\nGear tests failed: %d\n",
            failures
        );

        return 1;
    }


    printf(
        "\nAll gear tests passed.\n"
    );

    return 0;
}