#ifndef UTFUEL_TYPES_H
#define UTFUEL_TYPES_H

#include <stdint.h>
#include <stdbool.h>


typedef enum
{
    UTFUEL_STATE_BOOT = 0,
    UTFUEL_STATE_READY,
    UTFUEL_STATE_RUNNING,
    UTFUEL_STATE_WARNING,
    UTFUEL_STATE_FAULT

} utfuel_state_t;


typedef enum
{
    UTFUEL_GEAR_UNKNOWN = -1,

    UTFUEL_GEAR_NEUTRAL = 0,

    UTFUEL_GEAR_1 = 1,
    UTFUEL_GEAR_2,
    UTFUEL_GEAR_3,
    UTFUEL_GEAR_4,
    UTFUEL_GEAR_5,
    UTFUEL_GEAR_6

} utfuel_gear_t;


typedef struct
{
    uint32_t sequence_id;

    float tps_voltage;
    float map_voltage;

    float coolant_resistance;
    float intake_resistance;

    float battery_voltage;

    uint32_t engine_rpm;

    float vehicle_speed_kmh;

} utfuel_raw_input_t;


typedef struct
{
    uint32_t sequence_id;

    float tps_percent;
    float map_kpa;

    float coolant_temp_c;
    float intake_temp_c;

    float battery_voltage;

    uint32_t engine_rpm;

    float vehicle_speed_kmh;

    utfuel_gear_t gear;

    bool shift_warning;

} utfuel_vehicle_data_t;


#endif