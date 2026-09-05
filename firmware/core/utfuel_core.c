#include "utfuel_core.h"
#include "utfuel_types.h"

#include "../hal/utfuel_hal.h"

#include "../engine/sensors/utfuel_tps.h"
#include "../engine/sensors/utfuel_map.h"

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
    utfuel_raw_input_t raw_input = {0};

    utfuel_vehicle_data_t vehicle_data = {0};


    /*
     * Aguarda dados externos através da HAL.
     */
    if (!utfuel_hal_read_inputs(&raw_input))
    {
        return;
    }


    /*
     * Mantém o ID do pacote recebido.
     */
    vehicle_data.sequence_id =
        raw_input.sequence_id;


    /*
     * Dados que ainda chegam diretamente
     * da camada de entrada.
     */
    vehicle_data.engine_rpm =
        raw_input.engine_rpm;

    vehicle_data.vehicle_speed_kmh =
        raw_input.vehicle_speed_kmh;

    vehicle_data.battery_voltage =
        raw_input.battery_voltage;


    /*
     * Conversão dos sensores.
     */
    vehicle_data.tps_percent =
        utfuel_tps_from_voltage(
            raw_input.tps_voltage
        );

    vehicle_data.map_kpa =
        utfuel_map_from_voltage(
            raw_input.map_voltage
        );


    /*
     * Temperaturas serão implementadas
     * posteriormente.
     */
    vehicle_data.coolant_temp_c = 0.0f;

    vehicle_data.intake_temp_c = 0.0f;


    /*
     * Detecção da marcha.
     */
    vehicle_data.gear =
        utfuel_detect_gear(
            vehicle_data.engine_rpm,
            vehicle_data.vehicle_speed_kmh
        );


    /*
     * Indicador de troca de marcha.
     */
    vehicle_data.shift_warning =
        utfuel_should_shift(
            vehicle_data.engine_rpm,
            vehicle_data.gear
        );


    /*
     * Devolve os dados processados.
     */
    utfuel_hal_send_vehicle_data(
        &vehicle_data
    );
}