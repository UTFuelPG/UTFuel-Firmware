#include <stdio.h>

#include "../core/utfuel_core.h"
#include "../core/utfuel_version.h"


int main(void)
{
    fprintf(
        stderr,
        "[UTFuel] Firmware %s\n",
        UTFUEL_VERSION
    );

    fprintf(
        stderr,
        "[UTFuel] Platform %s\n",
        UTFUEL_PLATFORM
    );


    if (!utfuel_core_init())
    {
        fprintf(
            stderr,
            "[UTFuel] Initialization failed\n"
        );

        return 1;
    }


    fprintf(
        stderr,
        "[UTFuel] READY\n"
    );


    while (1)
    {
        utfuel_core_update();
    }


    return 0;
}