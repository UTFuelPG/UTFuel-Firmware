#include <stdio.h>

#include "utfuel_shift.h"


static int assert_bool(
    const char *name,
    int expected,
    int actual
)
{
    if (expected != actual)
    {
        printf(
            "[FAIL] %s | expected %d | actual %d\n",
            name,
            expected,
            actual
        );

        return 1;
    }

    printf(
        "[PASS] %s\n",
        name
    );

    return 0;
}


int main(void)
{
    int failures = 0;


    failures += assert_bool(
        "Gear 1 below shift RPM",
        0,
        utfuel_should_shift(
            8500,
            UTFUEL_GEAR_1
        )
    );


    failures += assert_bool(
        "Gear 1 at shift RPM",
        1,
        utfuel_should_shift(
            9000,
            UTFUEL_GEAR_1
        )
    );


    failures += assert_bool(
        "Gear 3 above shift RPM",
        1,
        utfuel_should_shift(
            9200,
            UTFUEL_GEAR_3
        )
    );


    failures += assert_bool(
        "Unknown gear",
        0,
        utfuel_should_shift(
            9500,
            UTFUEL_GEAR_UNKNOWN
        )
    );


    failures += assert_bool(
        "Neutral",
        0,
        utfuel_should_shift(
            9500,
            UTFUEL_GEAR_NEUTRAL
        )
    );


    if (failures > 0)
    {
        printf(
            "\nShift tests failed: %d\n",
            failures
        );

        return 1;
    }


    printf(
        "\nAll shift tests passed.\n"
    );

    return 0;
}