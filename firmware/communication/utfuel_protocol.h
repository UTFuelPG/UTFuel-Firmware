#ifndef UTFUEL_PROTOCOL_H
#define UTFUEL_PROTOCOL_H

#include <stdbool.h>
#include <stddef.h>

#include "../core/utfuel_types.h"


bool utfuel_protocol_parse_input(
    const char *line,
    utfuel_raw_input_t *input
);


bool utfuel_protocol_is_ping(
    const char *line,
    uint32_t *sequence_id
);


int utfuel_protocol_format_output(
    char *buffer,
    size_t buffer_size,
    const utfuel_vehicle_data_t *data
);


int utfuel_protocol_format_pong(
    char *buffer,
    size_t buffer_size,
    uint32_t sequence_id
);

#endif