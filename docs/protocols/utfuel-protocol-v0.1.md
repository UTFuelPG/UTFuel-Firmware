# UTFuel Communication Protocol

Version: 0.1

## Purpose

Initial communication protocol used between the UTFuel TestBench
and the UTFuel firmware during HOST and bench development.

Each message is ASCII encoded and terminated by a newline (`\n`).

---

## Input packet

Format:

IN,<sequence>,<tps_voltage>,<map_voltage>,<clt_resistance>,<iat_resistance>,<battery_voltage>,<rpm>,<speed>

Example:

IN,1001,3.50,2.10,1200,2500,13.80,8500,90.0

Fields:

| Field | Unit |
| --- | --- |
| sequence | integer |
| tps_voltage | V |
| map_voltage | V |
| clt_resistance | ohm |
| iat_resistance | ohm |
| battery_voltage | V |
| rpm | RPM |
| speed | km/h |

---

## Output packet

Format:

OUT,<sequence>,<rpm>,<tps>,<map>,<battery>,<speed>,<gear>,<shift>

Example:

OUT,1001,8500,75.00,90.20,13.80,90.00,3,0

---

## Ping

Request:

PING,<sequence>

Response:

PONG,<sequence>

---

## Error

Format:

ERR,<sequence>,<error_code>

Example:

ERR,1001,INVALID_PACKET