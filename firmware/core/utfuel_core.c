#include "utfuel_core.h"

#include "../hal/utfuel_hal.h"

#include "../engine/sensors/utfuel_tps.h"

#include "../engine/vehicle/utfuel_gear.h"
#include "../engine/vehicle/utfuel_shift.h"


static utfuel_state_t system_state =
    UTFUEL_STATE_BOOT;


bool utfuel_core_init(void)
{
    if (!utfuel_hal_init())
    {
        system_state =
            UTFUEL_STATE_FAULT;

        return false;
    }

    system_state =
        UTFUEL_STATE_READY;

    return true;
}


void utfuel_core_update(void)
{
    utfuel_raw_input_t raw_input;

    utfuel_vehicle_data_t vehicle_data;


    if (
        !utfuel_hal_read_inputs(
            &raw_input
        )
    )
    {
        return;
    }


    vehicle_data.engine_rpm =
        raw_input.engine_rpm;


    vehicle_data.vehicle_speed_kmh =
        raw_input.vehicle_speed_kmh;


    vehicle_data.battery_voltage =
        raw_input.battery_voltage;


    vehicle_data.tps_percent =
        utfuel_tps_from_voltage(
            raw_input.tps_voltage
        );


    /*
     * MAP, temperaturas etc.
     * serão implementados posteriormente.
     */

    vehicle_data.map_kpa = 0.0f;

    vehicle_data.coolant_temp_c = 0.0f;

    vehicle_data.intake_temp_c = 0.0f;


    vehicle_data.gear =
        utfuel_detect_gear(
            vehicle_data.engine_rpm,
            vehicle_data.vehicle_speed_kmh
        );


    vehicle_data.shift_warning =
        utfuel_should_shift(
            vehicle_data.engine_rpm,
            vehicle_data.gear
        );


    utfuel_hal_send_vehicle_data(
        &vehicle_data
    );
}