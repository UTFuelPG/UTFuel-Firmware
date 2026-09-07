# Protocolo do Simulador UTFuel v0.1

**Status:** Em desenvolvimento  
**Versão do protocolo:** 0.1  
**Transporte:** USB Serial  
**Codificação:** ASCII  
**Final de linha:** LF (`\n`)

---

# 1. Objetivo

O Protocolo do Simulador UTFuel define a comunicação entre o aplicativo
UTFuel TestBench e o simulador de motor/sensores baseado em ESP32-S3.

O simulador é responsável por receber do TestBench os estados automotivos
solicitados e traduzi-los em sinais elétricos físicos que poderão ser
medidos pela ECU UTFuel.

Este protocolo não representa a comunicação entre o simulador e a ECU.

A ECU deve receber os sinais simulados através de suas entradas físicas
normais, da mesma maneira que receberia sinais provenientes de sensores
reais instalados em um veículo.

---

# 2. Arquitetura

Fluxo do comando de simulação:

```text
UTFuel TestBench
       |
       | USB Serial
       |
       v
ESP32-S3 Simulator
       |
       | Sinais elétricos físicos
       |
       v
UTFuel ECU
```

O retorno da ECU para o computador ocorre por uma conexão independente:

```text
UTFuel ECU
       |
       | USB Serial
       |
       v
UTFuel TestBench
```

A arquitetura completa é:

```text
                     USB #1
UTFuel TestBench -----------------> ESP32-S3
                                      |
                                      |
                               sinais físicos
                                      |
                                      v
                                  UTFuel ECU
                                      |
                                      |
                     USB #2           |
UTFuel TestBench <--------------------+
```

Essa arquitetura permite comparar:

```text
Estado solicitado
       vs
Estado realmente medido pela ECU
```

sem inserir informações artificiais do TestBench no caminho físico
dos sensores.

---

# 3. Regras gerais

Todos os valores transmitidos pelo protocolo utilizam números inteiros.

Valores de ponto flutuante não são transmitidos diretamente.

Essa decisão evita:

- problemas com separadores decimais `,` e `.`;
- dependência da configuração regional do computador;
- diferenças de formatação entre plataformas;
- processamento desnecessário de ponto flutuante no sistema embarcado;
- maior complexidade no parser do ESP32.

Exemplos:

```text
3,420 V      -> 3420 mV

13,800 V     -> 13800 mV

73,12 %      -> 7312

108,10 km/h  -> 10810
```

Todas as mensagens são finalizadas por:

```text
\n
```

---

# 4. Comando PING

O comando `PING` verifica se o simulador está conectado e respondendo.

Formato enviado pelo TestBench:

```text
PING,<sequence>
```

Exemplo:

```text
PING,100
```

Resposta esperada:

```text
PONG,<sequence>
```

Exemplo:

```text
PONG,100
```

O valor de `sequence` deve ser retornado sem alterações.

---

# 5. Comando SIM_SET

O comando `SIM_SET` solicita ao simulador um novo estado de motor e
sensores.

Formato:

```text
SIM_SET,<sequence>,<tps_mv>,<map_mv>,<clt_ohm>,<iat_ohm>,<battery_mv>,<rpm>,<speed_centi_kmh>
```

Exemplo:

```text
SIM_SET,50231,3420,2170,1200,2500,13800,7200,10800
```

Interpretação:

```text
sequence       = 50231

TPS            = 3420 mV
               = 3,420 V

MAP            = 2170 mV
               = 2,170 V

CLT            = 1200 ohm

IAT            = 2500 ohm

Bateria        = 13800 mV
               = 13,800 V

RPM            = 7200 RPM

Velocidade     = 10800 / 100
               = 108,00 km/h
```

---

# 6. Resposta SIM_ACK

A resposta `SIM_ACK` indica que o simulador aceitou um comando
`SIM_SET`.

Formato básico:

```text
SIM_ACK,<sequence>
```

Formato recomendado:

```text
SIM_ACK,<sequence>,<applied_us>
```

Exemplo:

```text
SIM_ACK,50231,82451230
```

Onde:

```text
50231
=
sequência originalmente enviada pelo TestBench

82451230
=
timestamp interno do ESP32 em microssegundos
```

O campo `applied_us` indica aproximadamente o instante em que o
simulador considerou o novo estado aplicado às suas saídas.

Esse timestamp é utilizado apenas para diagnóstico.

Não é assumido que o relógio do ESP32 esteja sincronizado com o
computador ou com a ECU.

---

# 7. Resposta SIM_ERR

A resposta `SIM_ERR` indica que o simulador não conseguiu aceitar ou
processar um comando.

Formato:

```text
SIM_ERR,<sequence>,<error_code>
```

Exemplo:

```text
SIM_ERR,50231,BAD_RANGE
```

Códigos iniciais sugeridos:

```text
BAD_FORMAT
BAD_COMMAND
BAD_RANGE
NOT_READY
OUTPUT_ERROR
INTERNAL_ERROR
```

Significados:

```text
BAD_FORMAT
Formato da mensagem inválido.

BAD_COMMAND
Comando não reconhecido.

BAD_RANGE
Valor fora da faixa permitida.

NOT_READY
Simulador ainda não está pronto.

OUTPUT_ERROR
Falha ao aplicar algum sinal de saída.

INTERNAL_ERROR
Erro interno do firmware.
```

---

# 8. Definição dos campos

| Campo | Unidade | Representação |
|---|---:|---:|
| `sequence` | - | uint32 |
| `tps_mv` | mV | inteiro |
| `map_mv` | mV | inteiro |
| `clt_ohm` | ohm | inteiro |
| `iat_ohm` | ohm | inteiro |
| `battery_mv` | mV | inteiro |
| `rpm` | RPM | inteiro |
| `speed_centi_kmh` | 0,01 km/h | inteiro |

---

# 9. Sequência de comunicação

O número de sequência identifica exclusivamente uma solicitação entre:

```text
TestBench <-> ESP32 Simulator
```

Exemplo:

```text
TestBench

SIM_SET,100,...
       |
       v
ESP32

SIM_ACK,100,...
```

O número de sequência não é transmitido fisicamente para a ECU.

A ECU não deve conhecer o número de sequência utilizado entre
TestBench e ESP32.

Exemplo:

```text
SIM_SET,100,...
       |
       v
ESP32
       |
       v
sinal físico
       |
       v
ECU

ECU_DATA,4812,...
```

Não existe relação obrigatória entre:

```text
SIM_SET sequence
```

e:

```text
ECU_DATA sample_id
```

Portanto:

```text
SIM_SET sequence == ECU_DATA sample_id
```

não deve ser utilizado como mecanismo de correlação.

---

# 10. Medição de latência

O TestBench registra o instante de envio de um comando utilizando o
relógio do próprio computador.

Exemplo:

```text
Tsend
=
instante em que SIM_SET é transmitido
```

Depois, o TestBench observa a telemetria retornada pela ECU.

```text
Tdetect
=
instante em que a mudança correspondente aparece na ECU
```

A latência física ponta a ponta poderá ser calculada por:

```text
Latência = Tdetect - Tsend
```

Esse método mede aproximadamente o caminho:

```text
TestBench
   |
   v
USB
   |
   v
ESP32
   |
   v
geração do sinal
   |
   v
hardware
   |
   v
entrada da ECU
   |
   v
ADC / trigger
   |
   v
firmware da ECU
   |
   v
USB
   |
   v
TestBench
```

Como os dois timestamps utilizados no cálculo são registrados no
computador, não é necessária sincronização de relógios entre PC,
ESP32 e STM32 para esse tipo de medição.

---

# 11. Escopo da versão 0.1

A versão 0.1 prevê o transporte dos seguintes valores:

```text
TPS
MAP
CLT
IAT
tensão de bateria
RPM
velocidade do veículo
```

Representações atuais:

```text
TPS
-> milivolts

MAP
-> milivolts

CLT
-> ohms

IAT
-> ohms

Bateria
-> milivolts

RPM
-> rotações por minuto

Velocidade
-> centésimos de km/h
```

O protocolo define somente a comunicação entre TestBench e simulador.

A maneira como esses valores são convertidos em sinais elétricos é
documentada separadamente em:

```text
docs/hardware-interface/signal-translation.md
```

---

# 12. Compatibilidade

Qualquer alteração que modifique de forma incompatível a estrutura de:

```text
SIM_SET
SIM_ACK
SIM_ERR
PING
PONG
```

deverá resultar em uma nova versão do protocolo.