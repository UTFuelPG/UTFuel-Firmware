#include "utfuel_protocol.h"

#include <stdio.h>
#include <string.h>


bool utfuel_protocol_parse_input(
    const char *line,
    utfuel_raw_input_t *input
)
{
    if (line == NULL || input == NULL)
    {
        return false;
    }

    unsigned long sequence;
    unsigned long rpm;

    int fields = sscanf(
        line,
        "IN,%lu,%f,%f,%f,%f,%f,%lu,%f",
        &sequence,
        &input->tps_voltage,
        &input->map_voltage,
        &input->coolant_resistance,
        &input->intake_resistance,
        &input->battery_voltage,
        &rpm,
        &input->vehicle_speed_kmh
    );

    if (fields != 8)
    {
        return false;
    }

    input->sequence_id = (uint32_t)sequence;
    input->engine_rpm = (uint32_t)rpm;

    return true;
}


bool utfuel_protocol_is_ping(
    const char *line,
    uint32_t *sequence_id
)
{
    if (line == NULL || sequence_id == NULL)
    {
        return false;
    }

    unsigned long sequence;

    int fields = sscanf(
        line,
        "PING,%lu",
        &sequence
    );

    if (fields != 1)
    {
        return false;
    }

    *sequence_id = (uint32_t)sequence;

    return true;
}


int utfuel_protocol_format_output(
    char *buffer,
    size_t buffer_size,
    const utfuel_vehicle_data_t *data
)
{
    if (
        buffer == NULL ||
        data == NULL ||
        buffer_size == 0
    )
    {
        return -1;
    }

    return snprintf(
        buffer,
        buffer_size,

        "OUT,%lu,%lu,%.2f,%.2f,%.2f,%.2f,%d,%d\n",

        (unsigned long)data->sequence_id,
        (unsigned long)data->engine_rpm,

        data->tps_percent,
        data->map_kpa,
        data->battery_voltage,
        data->vehicle_speed_kmh,

        (int)data->gear,

        data->shift_warning ? 1 : 0
    );
}


int utfuel_protocol_format_pong(
    char *buffer,
    size_t buffer_size,
    uint32_t sequence_id
)
{
    return snprintf(
        buffer,
        buffer_size,
        "PONG,%lu\n",
        (unsigned long)sequence_id
    );
}