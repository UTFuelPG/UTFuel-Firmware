#include <stdio.h>
#include <string.h>

#include "../utfuel_hal.h"

#include "../../communication/utfuel_protocol.h"


#define UTFUEL_RX_BUFFER_SIZE 256
#define UTFUEL_TX_BUFFER_SIZE 256


bool utfuel_hal_init(void)
{
    return true;
}


bool utfuel_hal_read_inputs(
    utfuel_raw_input_t *input
)
{
    char buffer[UTFUEL_RX_BUFFER_SIZE];

    while (fgets(
        buffer,
        sizeof(buffer),
        stdin
    ) != NULL)
    {
        uint32_t ping_sequence;

        /*
         * PING command
         */
        if (
            utfuel_protocol_is_ping(
                buffer,
                &ping_sequence
            )
        )
        {
            char response[64];

            utfuel_protocol_format_pong(
                response,
                sizeof(response),
                ping_sequence
            );

            printf("%s", response);

            fflush(stdout);

            continue;
        }


        /*
         * Sensor input packet
         */
        if (
            utfuel_protocol_parse_input(
                buffer,
                input
            )
        )
        {
            return true;
        }


        /*
         * Invalid packet
         */
        printf(
            "ERR,0,INVALID_PACKET\n"
        );

        fflush(stdout);
    }

    return false;
}


void utfuel_hal_send_vehicle_data(
    const utfuel_vehicle_data_t *data
)
{
    char buffer[UTFUEL_TX_BUFFER_SIZE];

    int length =
        utfuel_protocol_format_output(
            buffer,
            sizeof(buffer),
            data
        );

    if (length > 0)
    {
        printf("%s", buffer);

        /*
         * Muito importante quando o programa
         * estiver conectado ao TestBench.
         */
        fflush(stdout);
    }
}