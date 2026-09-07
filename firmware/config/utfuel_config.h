#ifndef UTFUEL_CONFIG_H
#define UTFUEL_CONFIG_H

#define UTFUEL_GEAR_COUNT 6

typedef struct
{
    float tire_circumference_m;

    float final_drive_ratio;

    float gear_ratios[UTFUEL_GEAR_COUNT];

    unsigned int shift_rpm[UTFUEL_GEAR_COUNT];

} utfuel_vehicle_config_t;


const utfuel_vehicle_config_t *
utfuel_get_vehicle_config(void);

#endif