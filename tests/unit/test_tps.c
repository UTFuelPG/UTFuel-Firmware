#include <stdio.h>
#include <math.h>

#include "utfuel_tps.h"


#define TEST_TOLERANCE 0.01f


static int assert_float(
    const char *name,
    float expected,
    float actual
)
{
    float error = fabsf(expected - actual);

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
        "TPS closed",
        0.0f,
        utfuel_tps_from_voltage(0.50f)
    );

    failures += assert_float(
        "TPS 25%",
        25.0f,
        utfuel_tps_from_voltage(1.50f)
    );

    failures += assert_float(
        "TPS 50%",
        50.0f,
        utfuel_tps_from_voltage(2.50f)
    );

    failures += assert_float(
        "TPS 75%",
        75.0f,
        utfuel_tps_from_voltage(3.50f)
    );

    failures += assert_float(
        "TPS fully open",
        100.0f,
        utfuel_tps_from_voltage(4.50f)
    );

    failures += assert_float(
        "TPS below range clamp",
        0.0f,
        utfuel_tps_from_voltage(0.10f)
    );

    failures += assert_float(
        "TPS above range clamp",
        100.0f,
        utfuel_tps_from_voltage(5.00f)
    );


    if (failures > 0)
    {
        printf(
            "\nTPS tests failed: %d\n",
            failures
        );

        return 1;
    }


    printf(
        "\nAll TPS tests passed.\n"
    );

    return 0;
}