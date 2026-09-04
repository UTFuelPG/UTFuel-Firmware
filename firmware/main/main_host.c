#include <stdio.h>

#include "../core/utfuel_core.h"
#include "../core/utfuel_version.h"


int main(void)
{
    printf("\n");

    printf(
        "====================================\n"
    );

    printf(
        "            %s\n",
        UTFUEL_NAME
    );

    printf(
        "Firmware: %s\n",
        UTFUEL_VERSION
    );

    printf(
        "Platform: %s\n",
        UTFUEL_PLATFORM
    );

    printf(
        "====================================\n"
    );


    if (!utfuel_core_init())
    {
        printf(
            "ERROR: UTFuel initialization failed.\n"
        );

        return 1;
    }


    printf(
        "UTFuel READY\n"
    );


    while (1)
    {
        utfuel_core_update();
    }


    return 0;
}