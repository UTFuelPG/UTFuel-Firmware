#ifndef UTFUEL_SHIFT_H
#define UTFUEL_SHIFT_H

#include <stdint.h>
#include <stdbool.h>

#include "../../core/utfuel_types.h"


bool utfuel_should_shift(
    uint32_t rpm,
    utfuel_gear_t gear
);

#endif