#ifndef UTFUEL_HAL_H
#define UTFUEL_HAL_H

#include <stdbool.h>

#include "../core/utfuel_types.h"

/*
 * Inicializa a plataforma.
 */
bool utfuel_hal_init(void);


/*
 * Obtém as entradas externas disponíveis.
 *
 * Retorna true caso existam novos dados.
 */
bool utfuel_hal_read_inputs(
    utfuel_raw_input_t *input
);


/*
 * Envia dados processados para fora da ECU.
 */
void utfuel_hal_send_vehicle_data(
    const utfuel_vehicle_data_t *data
);

#endif