#include <stdio.h>
#include <string.h>

#include "utfuel_protocol.h"


static int assert_int(
    const char *name,
    long expected,
    long actual
)
{
    if (expected != actual)
    {
        printf(
            "[FAIL] %s | expected %ld | actual %ld\n",
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


    /*
     * INPUT packet test
     */
    utfuel_raw_input_t input = {0};


    const char *input_packet =
        "IN,1001,3.50,2.10,1200,2500,13.80,8500,90.0";


    if (
        !utfuel_protocol_parse_input(
            input_packet,
            &input
        )
    )
    {
        printf(
            "[FAIL] Input packet parsing\n"
        );

        failures++;
    }
    else
    {
        printf(
            "[PASS] Input packet parsing\n"
        );
    }


    failures += assert_int(
        "Sequence ID",
        1001,
        input.sequence_id
    );


    failures += assert_int(
        "Engine RPM",
        8500,
        input.engine_rpm
    );


    /*
     * PING packet test
     */
    uint32_t ping_sequence = 0;


    if (
        !utfuel_protocol_is_ping(
            "PING,3456",
            &ping_sequence
        )
    )
    {
        printf(
            "[FAIL] PING parsing\n"
        );

        failures++;
    }
    else
    {
        printf(
            "[PASS] PING parsing\n"
        );
    }


    failures += assert_int(
        "PING sequence",
        3456,
        ping_sequence
    );


    /*
     * PONG formatting
     */
    char pong[64];


    utfuel_protocol_format_pong(
        pong,
        sizeof(pong),
        3456
    );


    if (
        strcmp(
            pong,
            "PONG,3456\n"
        ) != 0
    )
    {
        printf(
            "[FAIL] PONG formatting | %s",
            pong
        );

        failures++;
    }
    else
    {
        printf(
            "[PASS] PONG formatting\n"
        );
    }


    /*
     * Invalid packet
     */
    utfuel_raw_input_t invalid = {0};


    if (
        utfuel_protocol_parse_input(
            "THIS_IS_NOT_A_PACKET",
            &invalid
        )
    )
    {
        printf(
            "[FAIL] Invalid packet accepted\n"
        );

        failures++;
    }
    else
    {
        printf(
            "[PASS] Invalid packet rejected\n"
        );
    }


    if (failures > 0)
    {
        printf(
            "\nProtocol tests failed: %d\n",
            failures
        );

        return 1;
    }


    printf(
        "\nAll protocol tests passed.\n"
    );

    return 0;
}