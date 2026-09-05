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
# UTFuel HOST
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
# Math library - Linux/macOS only
# =========================================================

if(NOT WIN32)

    target_link_libraries(
        utfuel_host
        PRIVATE
        m
    )

endif()


# =========================================================
# Tests
# =========================================================

enable_testing()


# =========================================================
# TPS TEST
# =========================================================

add_executable(
    test_tps

    tests/unit/test_tps.c

    firmware/engine/sensors/utfuel_tps.c
)

target_include_directories(
    test_tps
    PRIVATE

    firmware/engine/sensors
)

if(NOT WIN32)
    target_link_libraries(test_tps PRIVATE m)
endif()

add_test(
    NAME TPS
    COMMAND test_tps
)


# =========================================================
# MAP TEST
# =========================================================

add_executable(
    test_map

    tests/unit/test_map.c

    firmware/engine/sensors/utfuel_map.c
)

target_include_directories(
    test_map
    PRIVATE

    firmware/engine/sensors
)

if(NOT WIN32)
    target_link_libraries(test_map PRIVATE m)
endif()

add_test(
    NAME MAP
    COMMAND test_map
)


# =========================================================
# GEAR TEST
# =========================================================

add_executable(
    test_gear

    tests/unit/test_gear.c

    firmware/engine/vehicle/utfuel_gear.c

    firmware/config/utfuel_config.c
)

target_include_directories(
    test_gear
    PRIVATE

    firmware/core
    firmware/engine/vehicle
    firmware/config
)

if(NOT WIN32)
    target_link_libraries(test_gear PRIVATE m)
endif()

add_test(
    NAME Gear
    COMMAND test_gear
)


# =========================================================
# SHIFT TEST
# =========================================================

add_executable(
    test_shift

    tests/unit/test_shift.c

    firmware/engine/vehicle/utfuel_shift.c

    firmware/config/utfuel_config.c
)

target_include_directories(
    test_shift
    PRIVATE

    firmware/core
    firmware/engine/vehicle
    firmware/config
)

add_test(
    NAME Shift
    COMMAND test_shift
)


# =========================================================
# PROTOCOL TEST
# =========================================================

add_executable(
    test_protocol

    tests/integration/test_protocol.c

    firmware/communication/utfuel_protocol.c
)

target_include_directories(
    test_protocol
    PRIVATE

    firmware/core
    firmware/communication
)

add_test(
    NAME Protocol
    COMMAND test_protocol
)