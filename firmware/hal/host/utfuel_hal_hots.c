#include <stdio.h>
#include <string.h>

#include "../utfuel_hal.h"


bool utfuel_hal_init(void)
{
    printf("UTFuel HAL initialized in HOST mode.\n");

    return true;
}


bool utfuel_hal_read_inputs(
    utfuel_raw_input_t *input
)
{
    if (input == NULL)
    {
        return false;
    }

    printf("\n");

    printf("TPS voltage (V): ");
    scanf("%f", &input->tps_voltage);

    printf("MAP voltage (V): ");
    scanf("%f", &input->map_voltage);

    printf("Coolant resistance (ohm): ");
    scanf("%f", &input->coolant_resistance);

    printf("Intake resistance (ohm): ");
    scanf("%f", &input->intake_resistance);

    printf("Battery voltage (V): ");
    scanf("%f", &input->battery_voltage);

    printf("Engine RPM: ");
    scanf("%u", &input->engine_rpm);

    printf("Vehicle speed (km/h): ");
    scanf("%f", &input->vehicle_speed_kmh);

    return true;
}


void utfuel_hal_send_vehicle_data(
    const utfuel_vehicle_data_t *data
)
{
    if (data == NULL)
    {
        return;
    }

    printf("\n");
    printf("=======================================\n");
    printf("           UTFuel ECU Output           \n");
    printf("=======================================\n");

    printf(
        "RPM:            %u\n",
        data->engine_rpm
    );

    printf(
        "TPS:            %.2f %%\n",
        data->tps_percent
    );

    printf(
        "MAP:            %.2f kPa\n",
        data->map_kpa
    );

    printf(
        "Battery:        %.2f V\n",
        data->battery_voltage
    );

    printf(
        "Speed:          %.2f km/h\n",
        data->vehicle_speed_kmh
    );

    printf(
        "Gear:           %d\n",
        data->gear
    );

    printf(
        "Shift warning:  %s\n",
        data->shift_warning ? "YES" : "NO"
    );

    printf("=======================================\n");
}