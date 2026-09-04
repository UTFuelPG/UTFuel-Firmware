#include <stdio.h>
#include <inttypes.h>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

#include "esp_log.h"
#include "esp_timer.h"

#include "utfuel_types.h"
#include "utfuel_version.h"


static const char *TAG = "UTFUEL";

static utfuel_system_state_t system_state = UTFUEL_STATE_BOOT;


/*
 * Converte o estado interno para texto.
 */
static const char *state_to_string(utfuel_system_state_t state)
{
    switch (state)
    {
        case UTFUEL_STATE_BOOT:
            return "BOOT";

        case UTFUEL_STATE_READY:
            return "READY";

        case UTFUEL_STATE_RUNNING:
            return "RUNNING";

        case UTFUEL_STATE_WARNING:
            return "WARNING";

        case UTFUEL_STATE_FAULT:
            return "FAULT";

        default:
            return "UNKNOWN";
    }
}


/*
 * Dados simulados.
 *
 * Futuramente esta função será substituída
 * pela leitura dos sensores reais.
 */
static utfuel_engine_data_t get_simulated_engine_data(void)
{
    utfuel_engine_data_t data;

    data.rpm = 3500;
    data.tps_percent = 25.0f;
    data.map_kpa = 60.0f;
    data.coolant_temp_c = 85.0f;
    data.battery_voltage = 13.8f;

    return data;
}


/*
 * Tarefa responsável por transmitir telemetria.
 */
static void telemetry_task(void *parameter)
{
    while (1)
    {
        utfuel_engine_data_t data =
            get_simulated_engine_data();

        uint64_t uptime_ms =
            (uint64_t)(esp_timer_get_time() / 1000);

        printf(
            "@TEL {"
            "\"uptime_ms\":%" PRIu64 ","
            "\"state\":\"%s\","
            "\"rpm\":%" PRIu32 ","
            "\"tps\":%.1f,"
            "\"map\":%.1f,"
            "\"coolant\":%.1f,"
            "\"battery\":%.1f"
            "}\n",

            uptime_ms,
            state_to_string(system_state),
            data.rpm,
            data.tps_percent,
            data.map_kpa,
            data.coolant_temp_c,
            data.battery_voltage
        );

        vTaskDelay(pdMS_TO_TICKS(1000));
    }
}


/*
 * Entrada principal do firmware.
 */
void app_main(void)
{
    printf("\n");
    printf("====================================\n");
    printf("             UTFuel ECU             \n");
    printf("====================================\n");

    printf("Firmware : %s\n", UTFUEL_FW_VERSION);
    printf("Hardware : %s\n", UTFUEL_HW_VERSION);

    printf("====================================\n\n");

    ESP_LOGI(TAG, "System initialization started");

    system_state = UTFUEL_STATE_READY;

    ESP_LOGI(
        TAG,
        "System state: %s",
        state_to_string(system_state)
    );

    xTaskCreate(
        telemetry_task,
        "telemetry_task",
        4096,
        NULL,
        5,
        NULL
    );

    ESP_LOGI(TAG, "UTFuel READY");
}