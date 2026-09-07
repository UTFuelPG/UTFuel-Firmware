# Arquitetura do UTFuel TestBench

**Status:** Em desenvolvimento  
**Projeto:** UTFuel  
**Componente:** UTFuel TestBench  
**Versão inicial da arquitetura:** 0.2

---

# 1. Objetivo

O UTFuel TestBench é a aplicação utilizada para desenvolvimento,
simulação, diagnóstico, validação e visualização da plataforma UTFuel.

O TestBench deve ser capaz de operar em dois modos principais:

```text
LOCAL SIMULATION

e

HARDWARE BENCH
```

O objetivo dessa arquitetura é permitir que as mesmas ferramentas de:

```text
controle manual

benchmark dinâmico

telemetria

validação automática

análise de erro

Showcase
```

possam ser utilizadas tanto com uma ECU simulada no computador quanto
com hardware físico.

---

# 2. Modos de operação

O TestBench possui dois modos principais.

```text
Local Simulation
Hardware Bench
```

A seleção do modo determina como os dados enviados pelo TestBench
chegam à ECU e como as respostas são obtidas.

---

# 3. Local Simulation

O modo `Local Simulation` é utilizado para desenvolvimento de software
sem necessidade de hardware físico.

Arquitetura:

```text
UTFuel TestBench
       |
       | stdin / stdout
       v
utfuel_host.exe
       |
       v
UTFuel Core
```

Nesse modo:

```text
não existe ESP32 físico

não existe STM32 físico

não existem sinais elétricos reais
```

O executável:

```text
utfuel_host.exe
```

executa a lógica da ECU diretamente no computador.

---

# 4. Objetivos do Local Simulation

O modo local é utilizado principalmente para:

```text
desenvolvimento rápido

teste da lógica da ECU

teste de protocolos

teste da interface gráfica

teste do algoritmo de marcha

teste de Shift Warning

teste de calibração matemática

regression tests

desenvolvimento sem hardware
```

Esse modo não deve ser utilizado para validar:

```text
ADC físico

ruído

circuito analógico

latência elétrica

trigger físico

CAN físico

qualidade do sinal

precisão do simulador
```

---

# 5. Hardware Bench

O modo `Hardware Bench` representa a bancada física de validação da
UTFuel.

Arquitetura planejada:

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

Nesse modo existem dois dispositivos independentes conectados ao
computador.

```text
Simulator
=
ESP32-S3

ECU
=
STM32
```

---

# 6. Função do ESP32-S3

O ESP32-S3 atua como simulador de motor e sensores.

Sua função é receber valores solicitados pelo TestBench e convertê-los
em sinais físicos.

Exemplos:

```text
TPS
-> tensão analógica

MAP
-> tensão analógica

CLT
-> resistência equivalente

IAT
-> resistência equivalente

RPM
-> sinal pulsado / trigger

Velocidade
-> sinal pulsado

Bateria
-> sinal elétrico equivalente
```

O ESP32-S3 não executa a lógica da ECU.

Ele representa:

```text
motor
+
sensores
+
fontes de sinais
```

durante testes em bancada.

---

# 7. Função da ECU

A ECU UTFuel recebe os sinais físicos gerados pelo simulador.

Arquitetura:

```text
ESP32-S3
    |
    v
sinal físico
    |
    v
entrada elétrica da ECU
    |
    v
condicionamento
    |
    v
ADC / timer / trigger
    |
    v
STM32
    |
    v
UTFuel Core
```

A ECU deve interpretar os sinais como faria em um veículo real.

---

# 8. Caminho de retorno da ECU

A telemetria da ECU retorna diretamente para o computador.

```text
UTFuel ECU
     |
     | USB Serial
     v
UTFuel TestBench
```

O ESP32 não participa do caminho de retorno da telemetria.

Isso é proposital.

A arquitetura evita:

```text
TestBench
   |
   v
ESP32
   |
   v
ECU
   |
   v
ESP32
   |
   v
TestBench
```

O caminho preferido é:

```text
TestBench
   |
   v
ESP32
   |
   v
ECU
   |
   v
TestBench
```

---

# 9. Motivo para duas conexões USB

O Hardware Bench utiliza duas conexões independentes.

Exemplo:

```text
COM5
=
ESP32-S3 Simulator

COM7
=
UTFuel ECU
```

A primeira conexão é utilizada para controlar o simulador.

```text
TestBench -> ESP32
```

A segunda conexão é utilizada para receber a telemetria da ECU.

```text
ECU -> TestBench
```

Isso permite analisar separadamente:

```text
comunicação com simulador

comunicação com ECU

perda de pacotes do simulador

perda de telemetria da ECU

latência do sistema físico
```

---

# 10. Abstração de sessão

O restante do TestBench não deve depender diretamente da maneira como a
ECU está sendo executada.

Para isso é utilizada uma abstração de sessão.

Conceito:

```text
                 ITestBenchSession
                        |
             +----------+----------+
             |                     |
             v                     v
     Local Simulation        Hardware Bench
```

A aplicação poderá então utilizar operações comuns independentemente
do modo selecionado.

---

# 11. ConnectionMode

Os modos principais são representados por:

```text
ConnectionMode.LocalSimulation

ConnectionMode.HardwareBench
```

A interface gráfica utilizará essa informação para selecionar a
arquitetura ativa.

---

# 12. Arquitetura de software planejada

Estrutura conceitual:

```text
MainWindowViewModel
        |
        v
ITestBenchSession
        |
        +-----------------------------+
        |                             |
        v                             v
Local Simulation              Hardware Bench
        |                             |
        v                    +--------+--------+
HostFirmwareConnection       |                 |
        |                    v                 v
        v             SimulatorSerial    EcuSerial
utfuel_host.exe         Connection        Connection
                              |                 |
                              v                 v
                           ESP32              STM32
```

---

# 13. Local Simulation Session

O modo Local Simulation utiliza:

```text
HostFirmwareConnection
```

para iniciar e controlar:

```text
utfuel_host.exe
```

O protocolo atual utiliza comandos como:

```text
PING
IN
OUT
ERR
```

Exemplo:

```text
IN,<sequence>,...
```

Resposta:

```text
OUT,<sequence>,...
```

Nesse caso é possível correlacionar diretamente entrada e saída através
do mesmo número de sequência.

---

# 14. Hardware Bench Session

O Hardware Bench é diferente.

Ele utiliza simultaneamente:

```text
SimulatorSerialConnection

e

EcuSerialConnection
```

Conceito:

```text
HardwareBenchSession
        |
        +---- SimulatorSerialConnection
        |
        +---- EcuSerialConnection
```

A primeira conexão envia estados para o ESP32.

A segunda recebe amostras produzidas pela ECU.

---

# 15. Diferença fundamental entre os modos

No Local Simulation:

```text
IN sequence 100
       |
       v
ECU simulada
       |
       v
OUT sequence 100
```

Existe relação direta entre entrada e resposta.

No Hardware Bench:

```text
SIM_SET sequence 100
       |
       v
ESP32
       |
       v
sinal físico
       |
       v
ECU
       |
       v
ECU_DATA sample_id 4512
```

Não existe relação direta entre:

```text
sequence
```

e:

```text
sample_id
```

Essa diferença é fundamental para a arquitetura do TestBench.

---

# 16. Correlação física

No Hardware Bench, o TestBench deve correlacionar os dados utilizando
tempo e comportamento dos sinais.

Exemplo:

```text
T0

Target TPS:
10 %
```

Depois:

```text
T1

TestBench solicita:
80 %
```

O ESP32 altera o sinal físico.

A ECU pode produzir:

```text
ECU_DATA 1001 -> 10,1 %
ECU_DATA 1002 -> 10,2 %
ECU_DATA 1003 -> 79,6 %
ECU_DATA 1004 -> 80,0 %
```

O TestBench detecta a mudança durante:

```text
sample_id 1003
```

e pode calcular uma latência aproximada.

---

# 17. Medição de latência física

O TestBench registra:

```text
Tsend
```

no momento em que o comando é enviado ao simulador.

Depois registra:

```text
Tdetect
```

quando a alteração correspondente é observada na telemetria da ECU.

Então:

```text
Latência física =
Tdetect - Tsend
```

Essa latência inclui aproximadamente:

```text
PC
|
USB
|
ESP32
|
geração do sinal
|
hardware de saída
|
entrada elétrica da ECU
|
ADC / timer
|
processamento STM32
|
telemetria
|
USB
|
PC
```

---

# 18. Métricas do Local Simulation

No modo Local Simulation podem ser medidas:

```text
RTT do processo local

perda de mensagens

taxa de atualização

jitter de comunicação

erros matemáticos

resultado esperado vs resultado processado
```

Esses valores são úteis para desenvolvimento de software.

Porém não representam latência real da ECU física.

---

# 19. Métricas do Hardware Bench

No Hardware Bench poderão ser medidas separadamente:

```text
PC -> ESP32 ACK RTT

perda de comandos do simulador

ECU -> PC packet loss

ECU update rate

ECU telemetry jitter

erro Target vs ECU

tempo de resposta física

estabilidade do sinal

comportamento durante transientes
```

---

# 20. Comunicação com o simulador

A comunicação:

```text
TestBench -> ESP32
```

é definida em:

```text
docs/protocols/simulator-protocol-v0.1.md
```

Principais mensagens:

```text
PING
PONG
SIM_SET
SIM_ACK
SIM_ERR
```

---

# 21. Comunicação com a ECU

A comunicação:

```text
ECU -> TestBench
```

é definida em:

```text
docs/protocols/ecu-telemetry-protocol-v0.1.md
```

Principal pacote:

```text
ECU_DATA
```

---

# 22. Tradução dos sinais físicos

A conversão entre:

```text
valor de engenharia

valor transportado

sinal elétrico
```

é documentada em:

```text
docs/hardware-interface/signal-translation.md
```

Exemplo:

```text
TPS = 75 %
       |
       v
3,500 V
       |
       v
3500 mV no protocolo
       |
       v
ESP32 + DAC
       |
       v
aproximadamente 3,500 V físico
```

---

# 23. Interface gráfica

A interface do TestBench deverá permitir selecionar o modo de conexão.

Exemplo:

```text
CONNECTION MODE

[ LOCAL SIMULATION ] [ HARDWARE BENCH ]
```

A seleção só poderá ser alterada quando o TestBench estiver
desconectado.

---

# 24. GUI — Local Simulation

Quando `Local Simulation` estiver selecionado, a interface deverá
apresentar informações relacionadas ao executável local.

Exemplo:

```text
LOCAL SIMULATION

Firmware Target

C:\...\utfuel_host.exe


Status

DISCONNECTED


[ CONNECT ]
```

---

# 25. GUI — Hardware Bench

Quando `Hardware Bench` estiver selecionado, a interface deverá
apresentar duas conexões.

Exemplo:

```text
HARDWARE BENCH


SIMULATOR

Port
[ COM5 ]

Baud
[ 115200 ]


UTFUEL ECU

Port
[ COM7 ]

Baud
[ 115200 ]


[ REFRESH PORTS ]

[ CONNECT HARDWARE ]
```

Estados individuais poderão ser exibidos:

```text
Simulator
CONNECTED

ECU
CONNECTED
```

---

# 26. Bloqueio da seleção de modo

A troca de modo deverá ser bloqueada quando existir uma sessão ativa.

Exemplo:

```text
CONNECTED
```

então:

```text
Local Simulation / Hardware Bench
```

não poderá ser alterado.

Isso evita mudar a arquitetura durante:

```text
Manual Live

Dynamic Benchmark

Validation

telemetria ativa
```

---

# 27. Hardware Bench sem hardware

Durante o desenvolvimento, poderá existir um modo de simulação interna
da infraestrutura de Hardware Bench.

Exemplo:

```text
Hardware Bench
+
Development Mock
```

Arquitetura:

```text
TestBench
    |
    v
Simulator Mock
    |
    v
ECU Mock
    |
    v
TestBench
```

Esse modo possui finalidade exclusiva de desenvolvimento de software.

---

# 28. Identificação obrigatória do Mock

Quando mocks forem utilizados, a interface deverá indicar claramente
que não existe hardware físico.

Exemplo:

```text
HARDWARE BENCH — MOCK
```

Resultados obtidos nesse modo não devem ser apresentados como medições
de hardware real.

Especialmente valores como:

```text
latência física

precisão ADC

qualidade de sinal

ruído

resposta elétrica
```

não possuem validade nesse modo.

---

# 29. Estado da implementação

## Implementado

```text
UTFuel Core

Host Firmware

Local Simulation

HostFirmwareConnection

TestBench GUI

Manual Control

Manual Live

Dynamic Benchmark

Live Telemetry

Validation

Showcase

ITestBenchSession

ConnectionMode
```

## Em desenvolvimento

```text
SimulatorSerialConnection

EcuSerialConnection

HardwareBenchSession

seleção de modo na GUI
```

## Planejado

```text
ESP32-S3 Simulator físico

STM32 ECU V1

sinais físicos

CAN

HMI ESP32-S3

validação elétrica
```

---

# 30. Princípio arquitetural

O TestBench deve conhecer:

```text
o que solicitar

o que foi recebido

como medir

como comparar
```

mas deve minimizar dependências sobre:

```text
onde a lógica da ECU está sendo executada
```

Isso permite utilizar as mesmas ferramentas de validação durante várias
fases do projeto:

```text
software puro
       |
       v
firmware em desenvolvimento
       |
       v
bancada
       |
       v
ECU física
       |
       v
veículo
```

---

# 31. Evolução planejada

A evolução esperada é:

```text
FASE 1

TestBench
   |
   v
utfuel_host.exe
```

Depois:

```text
FASE 2

TestBench
   |
   +---- ESP32 Simulator
   |
   +---- STM32 ECU
```

Depois:

```text
FASE 3

ESP32 Simulator
      |
      v
STM32 ECU
      |
      +---- USB -> TestBench
      |
      +---- CAN -> HMI
```

E posteriormente:

```text
FASE 4

Veículo
  |
  v
UTFuel ECU
  |
  +---- CAN -> HMI
  |
  +---- diagnóstico / TestBench
```

A arquitetura do software deve permitir essa evolução sem exigir uma
reescrita completa da aplicação.

---

# 32. Objetivo final

O objetivo do UTFuel TestBench é se tornar uma ferramenta única para:

```text
desenvolvimento

simulação

diagnóstico

validação

calibração

benchmark

visualização
```

acompanhando o desenvolvimento da UTFuel desde a execução puramente
simulada até a utilização com hardware e veículo reais.