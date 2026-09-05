#include "utfuel_config.h"


static const utfuel_vehicle_config_t vehicle_config =
{
    /*
     * Valores TEMPORÁRIOS.
     *
     * Devem ser substituídos pelos dados reais
     * do veículo utilizado.
     */

    .tire_circumference_m = 1.60f,

    .final_drive_ratio = 4.0f,

    .gear_ratios =
    {
        3.00f,
        2.10f,
        1.60f,
        1.30f,
        1.05f,
        0.85f
    },

    .shift_rpm =
    {
        9000,
        9000,
        9000,
        9000,
        9000,
        9000
    }
};


const utfuel_vehicle_config_t *
utfuel_get_vehicle_config(void)
{
    return &vehicle_config;
}