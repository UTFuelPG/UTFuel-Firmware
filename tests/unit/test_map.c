#include <stdio.h>
#include <math.h>

#include "utfuel_map.h"


#define TEST_TOLERANCE 0.05f


static int assert_float(
    const char *name,
    float expected,
    float actual
)
{
    float error =
        fabsf(expected - actual);

    if (error > TEST_TOLERANCE)
    {
        printf(
            "[FAIL] %s | expected %.2f | actual %.2f\n",
            name,
            expected,
            actual
        );

        return 1;
    }

    printf(
        "[PASS] %s | %.2f\n",
        name,
        actual
    );

    return 0;
}


int main(void)
{
    int failures = 0;

    failures += assert_float(
        "MAP minimum",
        20.0f,
        utfuel_map_from_voltage(0.50f)
    );

    failures += assert_float(
        "MAP maximum",
        250.0f,
        utfuel_map_from_voltage(4.50f)
    );

    failures += assert_float(
        "MAP middle",
        135.0f,
        utfuel_map_from_voltage(2.50f)
    );

    failures += assert_float(
        "MAP below range clamp",
        20.0f,
        utfuel_map_from_voltage(0.10f)
    );

    failures += assert_float(
        "MAP above range clamp",
        250.0f,
        utfuel_map_from_voltage(5.00f)
    );


    if (failures > 0)
    {
        printf(
            "\nMAP tests failed: %d\n",
            failures
        );

        return 1;
    }


    printf(
        "\nAll MAP tests passed.\n"
    );

    return 0;
}