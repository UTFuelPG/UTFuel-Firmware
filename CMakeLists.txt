cmake_minimum_required(VERSION 3.20)

project(
    UTFuelFirmware
    VERSION 0.0.1
    LANGUAGES C
)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)


# =========================================================
# UTFuel HOST executable
# =========================================================

add_executable(
    utfuel_host

    firmware/main/main_host.c

    firmware/core/utfuel_core.c

    firmware/hal/host/utfuel_hal_host.c

    firmware/communication/utfuel_protocol.c

    firmware/engine/sensors/utfuel_tps.c
    firmware/engine/sensors/utfuel_map.c

    firmware/engine/vehicle/utfuel_gear.c
    firmware/engine/vehicle/utfuel_shift.c

    firmware/config/utfuel_config.c
)


# =========================================================
# Include directories
# =========================================================

target_include_directories(
    utfuel_host
    PRIVATE

    firmware/core

    firmware/hal
    firmware/hal/host

    firmware/communication

    firmware/engine/sensors
    firmware/engine/vehicle

    firmware/config
)


# =========================================================
# Compiler warnings
# =========================================================

if(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang")

    target_compile_options(
        utfuel_host
        PRIVATE

        -Wall
        -Wextra
        -Wpedantic
    )

endif()


# =========================================================
# Math library
# =========================================================

if(NOT WIN32)

    target_link_libraries(
        utfuel_host
        PRIVATE
        m
    )

endif()