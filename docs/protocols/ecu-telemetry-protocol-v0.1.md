# Protocolo de Telemetria da ECU UTFuel v0.1

**Status:** Em desenvolvimento  
**Versão do protocolo:** 0.1  
**Transporte:** USB Serial  
**Codificação:** ASCII  
**Final de linha:** LF (`\n`)

---

# 1. Objetivo

O Protocolo de Telemetria da ECU UTFuel define a comunicação da ECU
com o aplicativo UTFuel TestBench.

A telemetria informa ao computador os valores realmente adquiridos e
processados pela ECU.

Essa comunicação é independente do protocolo utilizado entre o
TestBench e o simulador ESP32-S3.

Arquitetura:

```text
TestBench
   |
   | SIM_SET
   v
ESP32 Simulator
   |
   | sinais físicos
   v
UTFuel ECU
   |
   | ECU_DATA
   v
TestBench
```

Essa separação permite validar a ECU utilizando suas entradas físicas.

---

# 2. Pacote ECU_DATA

Formato:

```text
ECU_DATA,<sample_id>,<ecu_uptime_us>,<rpm>,<tps_centi_pct>,<map_deci_kpa>,<battery_mv>,<speed_centi_kmh>,<gear>,<shift>
```

Exemplo:

```text
ECU_DATA,18291,82451230,7201,7312,884,13780,10810,4,0
```

Interpretação:

```text
sample_id
=
18291

ecu_uptime_us
=
82451230 us

RPM
=
7201 RPM

TPS
=
7312 / 100
=
73,12 %

MAP
=
884 / 10
=
88,4 kPa

Bateria
=
13780 / 1000
=
13,780 V

Velocidade
=
10810 / 100
=
108,10 km/h

Marcha
=
4

Shift Warning
=
false
```

---

# 3. Definição dos campos

| Campo | Representação | Significado |
|---|---:|---|
| `sample_id` | uint32 | Contador de amostras da ECU |
| `ecu_uptime_us` | uint64 | Tempo interno da ECU em microssegundos |
| `rpm` | uint32 | Rotação do motor |
| `tps_centi_pct` | uint32 | TPS × 100 |
| `map_deci_kpa` | uint32 | MAP × 10 |
| `battery_mv` | uint32 | Tensão de bateria × 1000 |
| `speed_centi_kmh` | uint32 | Velocidade × 100 |
| `gear` | int32 | Marcha detectada |
| `shift` | 0/1 | Alerta de troca de marcha |

---

# 4. Sample ID

O campo:

```text
sample_id
```

é um contador pertencente exclusivamente à ECU.

Ele deve ser incrementado para cada nova amostra de telemetria.

Exemplo:

```text
ECU_DATA,500,...
ECU_DATA,501,...
ECU_DATA,502,...
```

Caso o TestBench receba:

```text
ECU_DATA,500,...
ECU_DATA,503,...
```

as amostras:

```text
501
502
```

não foram observadas.

Isso permite detectar perda de pacotes na comunicação:

```text
ECU -> TestBench
```

---

# 5. Rollover

O contador poderá utilizar um inteiro de 32 bits sem sinal.

Quando atingir o valor máximo:

```text
4294967295
```

o próximo valor poderá ser:

```text
0
```

O TestBench deverá considerar esse comportamento normal.

---

# 6. Timestamp da ECU

O campo:

```text
ecu_uptime_us
```

representa o tempo monotônico da ECU em microssegundos.

Esse valor pode ser utilizado para analisar:

```text
frequência de atualização

jitter da ECU

tempo entre amostras

regularidade do loop

comportamento do scheduler
```

Exemplo:

```text
ECU_DATA,100,1000000,...
ECU_DATA,101,1010000,...
ECU_DATA,102,1020000,...
```

Nesse exemplo, existe aproximadamente:

```text
10000 us
```

entre amostras.

Portanto:

```text
10 ms
```

ou aproximadamente:

```text
100 Hz
```

---

# 7. Relógios independentes

Os relógios de:

```text
computador
ESP32
STM32
```

são independentes.

O campo:

```text
ecu_uptime_us
```

não deve ser comparado diretamente com:

```text
ESP32 applied_us
```

ou com timestamps absolutos do computador, a menos que futuramente
seja criado um sistema de sincronização de relógios.

---

# 8. Independência entre sequence e sample_id

O simulador utiliza:

```text
SIM_SET sequence
```

A ECU utiliza:

```text
ECU_DATA sample_id
```

São identificadores diferentes.

Exemplo:

```text
SIM_SET,1500,...
```

pode resultar fisicamente em uma alteração observada durante:

```text
ECU_DATA,8901,...
```

O TestBench deverá correlacionar os dados através de:

```text
tempo

estado solicitado

mudanças detectadas

comportamento do sinal
```

e não através da igualdade entre os identificadores.

---

# 9. Frequência de telemetria

A versão 0.1 não define uma frequência obrigatória de telemetria.

O TestBench deverá medir a frequência observada.

Exemplo:

```text
100 amostras recebidas
durante aproximadamente 1 segundo
```

Resultado:

```text
aproximadamente 100 Hz
```

No futuro, a frequência poderá ser configurável.

---

# 10. Uso da telemetria

Os dados de `ECU_DATA` poderão ser utilizados para:

```text
comparação Target vs ECU

medição de erro de TPS

medição de erro de MAP

medição de RPM

validação de velocidade

detecção de marcha

validação de Shift Warning

Live Telemetry

Showcase

medição de perda de pacotes

medição de update rate

medição de jitter

medição de latência física
```

---

# 11. Independência da ECU

A ECU UTFuel não deve depender do TestBench para realizar suas funções
principais.

O TestBench é uma ferramenta externa de:

```text
teste

diagnóstico

desenvolvimento

validação

visualização
```

A lógica principal da ECU deverá continuar funcionando mesmo quando o
computador não estiver conectado.

---

# 12. Expansões futuras

O pacote poderá futuramente transportar também:

```text
CLT

IAT

pressão de óleo

temperatura de óleo

lambda

sincronismo de crank

sincronismo de cam

estado do trigger

flags de diagnóstico

códigos de falha

estado CAN

carga de CPU

tempo do loop principal

tempo de execução de tarefas

estado de entradas e saídas
```

Qualquer alteração incompatível com o formato atual deverá resultar em
uma nova versão do protocolo.