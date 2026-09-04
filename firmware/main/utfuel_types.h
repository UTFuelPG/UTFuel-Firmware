#pragma once

#include <stdint.h>

typedef enum
{
    UTFUEL_STATE_BOOT = 0,
    UTFUEL_STATE_READY,
    UTFUEL_STATE_RUNNING,
    UTFUEL_STATE_WARNING,
    UTFUEL_STATE_FAULT

} utfuel_system_state_t;


typedef struct
{
    uint32_t rpm;

    float tps_percent;
    float map_kpa;
    float coolant_temp_c;
    float battery_voltage;

} utfuel_engine_data_t;