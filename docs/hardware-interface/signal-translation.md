# Tradução de Sinais do Simulador UTFuel

**Status:** Em desenvolvimento  
**Projeto:** UTFuel  
**Subsistema:** Simulador de Motor e Sensores ESP32-S3

---

# 1. Objetivo

Este documento descreve como os valores definidos no UTFuel TestBench
serão convertidos em sinais elétricos capazes de estimular fisicamente
as entradas da ECU UTFuel.

O sistema é dividido em três domínios:

```text
DOMÍNIO DE ENGENHARIA
        |
        v
DOMÍNIO DE TRANSPORTE
        |
        v
DOMÍNIO ELÉTRICO
```

Exemplo:

```text
TPS = 73 %
        |
        v
3,420 V
        |
        v
3420 mV no protocolo
        |
        v
ESP32-S3
        |
        v
circuito de saída
        |
        v
3,420 V físico
        |
        v
entrada ADC da ECU
```

Essa separação permite identificar em qual etapa um eventual erro foi
introduzido.

---

# 2. Cadeia completa de validação

O fluxo completo planejado para a V1 é:

```text
UTFuel TestBench
        |
        | valor solicitado
        v
protocolo do simulador
        |
        v
ESP32-S3 Simulator
        |
        | conversão
        v
circuito de saída
        |
        | sinal físico
        v
entrada da ECU
        |
        v
aquisição
        |
        v
processamento
        |
        v
telemetria da ECU
        |
        v
UTFuel TestBench
```

O TestBench poderá então comparar:

```text
Valor solicitado
       vs
Valor recebido da ECU
```

---

# 3. TPS — Throttle Position Sensor

A calibração temporária atualmente adotada é:

```text
0,50 V = 0 %

4,50 V = 100 %
```

A relação é linear.

Fórmula:

```text
V_TPS =
0,5 + (TPS_percent / 100) * 4,0
```

Exemplo:

```text
TPS = 73 %
```

Então:

```text
V =
0,5 + (73 / 100) * 4
```

Resultado:

```text
V =
3,42 V
```

---

# 4. TPS no protocolo

O protocolo utiliza milivolts.

Portanto:

```text
3,420 V
```

é convertido para:

```text
3420 mV
```

O comando poderá conter:

```text
SIM_SET,...,3420,...
```

O objetivo final do simulador será produzir aproximadamente:

```text
3,420 V
```

no pino TPS da ECU.

---

# 5. Saída física de TPS

A arquitetura prevista é:

```text
ESP32-S3
   |
   | comunicação digital
   v
DAC externo
   |
   v
condicionamento analógico
   |
   v
TPS ECU
```

O DAC externo ainda não foi definido.

O componente deverá ser escolhido considerando:

```text
resolução

precisão

linearidade

ruído

taxa de atualização

faixa de tensão

impedância de saída
```

O circuito de condicionamento deverá permitir uma saída compatível com
a faixa utilizada pelos sensores automotivos.

---

# 6. MAP — Manifold Absolute Pressure

A calibração temporária atualmente utilizada é:

```text
0,50 V = 20 kPa

4,50 V = 250 kPa
```

O valor normalizado pode ser calculado por:

```text
normalized =
(MAP_kPa - 20) / 230
```

Depois:

```text
V_MAP =
0,5 + normalized * 4
```

---

# 7. MAP no protocolo

A tensão calculada é convertida para milivolts.

Exemplo:

```text
2,170 V
```

será transmitido como:

```text
2170
```

Exemplo:

```text
SIM_SET,...,2170,...
```

O circuito do simulador deverá tentar produzir fisicamente:

```text
2,170 V
```

na entrada MAP da ECU.

---

# 8. CLT — Coolant Temperature

Sensores de temperatura automotivos normalmente utilizam termistores
do tipo NTC.

Por esse motivo, o TestBench representa CLT inicialmente através da
resistência equivalente do sensor.

Exemplo:

```text
CLT resistance =
1200 ohm
```

O protocolo transporta:

```text
1200
```

Fluxo:

```text
TestBench
        |
        | 1200 ohm
        v
ESP32-S3
        |
        v
circuito de resistência programável
        |
        v
entrada CLT da ECU
```

---

# 9. Circuito de simulação CLT

A implementação física ainda não foi definida.

Algumas possibilidades:

```text
potenciômetro digital

rede de resistores de precisão chaveados

emulador eletrônico de resistência
```

A escolha final deverá considerar:

```text
faixa de resistência

precisão

corrente no circuito

tensão utilizada pela ECU

resolução necessária

velocidade de mudança
```

---

# 10. IAT — Intake Air Temperature

O IAT seguirá o mesmo conceito utilizado para CLT.

O protocolo transportará resistência equivalente em ohms.

Exemplo:

```text
2500
```

representa:

```text
2500 ohm
```

Fluxo:

```text
TestBench
        |
        v
ESP32-S3
        |
        v
emulador de resistência
        |
        v
entrada IAT da ECU
```

O circuito físico permanece TBD.

---

# 11. Tensão de bateria

A tensão da bateria é transportada em milivolts.

Exemplo:

```text
13,800 V
```

será transmitido como:

```text
13800
```

O ESP32 não poderá produzir diretamente uma tensão automotiva desse
nível.

Será necessário um estágio elétrico intermediário.

Fluxo conceitual:

```text
ESP32-S3
    |
    v
circuito controlado
    |
    v
sinal equivalente de bateria
    |
    v
entrada da ECU
```

---

# 12. Estratégia A — Simulação completa da bateria

Uma possibilidade é gerar uma tensão próxima à tensão real do veículo.

Exemplo:

```text
13,8 V
   |
   v
entrada de bateria da ECU
   |
   v
divisor resistivo interno
   |
   v
ADC
```

Vantagem:

```text
valida também o circuito de entrada da ECU
```

Essa alternativa é mais interessante para validação completa.

---

# 13. Estratégia B — Simulação diretamente no ADC

Durante prototipagem inicial também poderá ser utilizado:

```text
tensão já reduzida
     |
     v
entrada ADC
```

Essa abordagem é mais simples, porém não valida o circuito completo de
entrada de bateria.

A estratégia utilizada deverá ser documentada para cada versão de
hardware.

---

# 14. RPM do motor

RPM não é representado por uma tensão analógica contínua.

O protocolo transporta diretamente:

```text
RPM
```

Exemplo:

```text
7200
```

significa:

```text
7200 RPM
```

O simulador deverá converter esse valor em pulsos temporizados.

Fluxo:

```text
7200 RPM
   |
   v
cálculo do período
   |
   v
timer do ESP32
   |
   v
GPIO
   |
   v
condicionamento elétrico
   |
   v
entrada de trigger da ECU
```

---

# 15. Roda fônica

O formato dos pulsos depende da roda fônica configurada.

Exemplos de padrões possíveis:

```text
60-2

36-1

24-1
```

O padrão definitivo ainda não foi escolhido.

O simulador deverá futuramente permitir configurar esse parâmetro.

Exemplo:

```text
Trigger Pattern:
60-2

RPM:
6000
```

O ESP32 deverá gerar:

```text
58 pulsos presentes

2 posições correspondentes aos dentes ausentes
```

em cada revolução da roda.

---

# 16. Frequência básica do trigger

Para uma roda de 60 posições:

```text
RPM = 6000
```

temos:

```text
6000 / 60
=
100 rotações por segundo
```

Com 60 posições:

```text
100 * 60
=
6000 posições por segundo
```

O firmware do simulador deverá gerar os eventos com temporização
adequada e representar corretamente os dentes ausentes.

Esse cálculo será refinado quando o formato de trigger definitivo for
especificado.

---

# 17. Velocidade do veículo

A velocidade no protocolo é representada utilizando:

```text
velocidade * 100
```

Exemplo:

```text
108,10 km/h
```

é transmitido como:

```text
10810
```

---

# 18. Geração física da velocidade

Caso a ECU utilize entrada pulsada de velocidade:

```text
km/h
 |
 v
velocidade da roda
 |
 v
rotações da roda
 |
 v
pulsos por revolução
 |
 v
frequência
 |
 v
timer do ESP32
 |
 v
GPIO
 |
 v
entrada da ECU
```

O cálculo dependerá de:

```text
circunferência do pneu

quantidade de pulsos por volta

tipo de sensor
```

Configuração temporária atual do TestBench:

```text
Circunferência do pneu =
1,60 m
```

O número de pulsos por revolução ainda permanece TBD.

---

# 19. Três representações de cada sinal

Todo sinal deve possuir três representações claramente documentadas.

Exemplo TPS:

```text
Valor de engenharia
=
73,00 %

Valor de transporte
=
3420 mV

Valor elétrico
=
aproximadamente 3,420 V
```

Exemplo MAP:

```text
Valor de engenharia
=
88,4 kPa

Valor de transporte
=
tensão equivalente em mV

Valor elétrico
=
tensão correspondente no pino MAP
```

Exemplo CLT:

```text
Valor de engenharia
=
temperatura / resistência equivalente

Valor de transporte
=
1200 ohm

Valor elétrico
=
comportamento equivalente a 1200 ohm
```

---

# 20. Fontes de erro

A cadeia completa poderá introduzir diferentes erros.

Exemplos:

```text
erro na conversão matemática

erro de quantização

erro do protocolo

erro do DAC

erro do circuito analógico

erro de resistência simulada

ruído elétrico

erro do ADC da ECU

erro da calibração

erro de processamento

erro de comunicação
```

Essas fontes deverão ser analisadas separadamente quando possível.

---

# 21. O simulador também possui erro

O ESP32 Simulator não deve ser tratado como referência perfeita.

Exemplo:

```text
TestBench solicita:
3,420 V
```

O simulador poderá gerar:

```text
3,416 V
```

A ECU poderá medir:

```text
3,412 V
```

Portanto existem pelo menos duas diferenças:

```text
Erro do simulador:

3,416 - 3,420


Erro da ECU:

3,412 - 3,416
```

Sem medir a saída real do simulador, seria possível atribuir à ECU um
erro que na verdade veio do sistema de geração de sinal.

---

# 22. Instrumentação externa

Durante a validação do simulador poderão ser utilizados instrumentos
externos.

Exemplos:

```text
multímetro de precisão

osciloscópio

analisador lógico

DAQ

ADC externo de referência
```

Esses instrumentos poderão medir diretamente as saídas físicas antes
de elas entrarem na ECU.

---

# 23. Exemplo completo de TPS

Estado solicitado:

```text
TPS =
75 %
```

Conversão:

```text
0,5 + (75 / 100) * 4
=
3,5 V
```

Protocolo:

```text
3500 mV
```

Comando:

```text
SIM_SET,100,3500,...
```

ESP32:

```text
recebe 3500
```

Sistema analógico:

```text
gera aproximadamente 3,500 V
```

ECU:

```text
ADC mede o sinal
```

Firmware da ECU:

```text
converte para aproximadamente 75 %
```

Telemetria:

```text
ECU_DATA,...,7500,...
```

TestBench:

```text
Target TPS =
75,00 %

ECU TPS =
75,00 %

Erro =
0,00 %
```

---

# 24. Exemplo completo de MAP

Estado solicitado:

```text
MAP =
valor definido pelo TestBench
```

Conversão:

```text
MAP kPa
    |
    v
calibração 20-250 kPa
    |
    v
0,5-4,5 V
```

Depois:

```text
volts * 1000
```

e o valor é enviado pelo protocolo.

ESP32:

```text
recebe tensão em mV
```

Circuito:

```text
gera tensão física
```

ECU:

```text
ADC
    |
    v
conversão
    |
    v
MAP em kPa
```

TestBench:

```text
Target MAP
      vs
ECU MAP
```

---

# 25. Estado atual do hardware

Atualmente já estão definidos no domínio de software:

```text
TPS

MAP

CLT

IAT

Bateria

RPM

Velocidade
```

Já existe uma representação definida para transporte serial.

Ainda permanecem TBD:

```text
DAC externo

circuito analógico 0-5 V

circuito de simulação de bateria

emulador de resistência CLT

emulador de resistência IAT

interface elétrica de crank

interface elétrica de velocidade

padrão definitivo da roda fônica

pulsos por revolução da velocidade
```

Esses pontos não devem ser considerados fechados até a criação e
validação do hardware correspondente.

---

# 26. Objetivo final do simulador

O objetivo do simulador não é apenas enviar números para a ECU.

O objetivo é reproduzir progressivamente o caminho físico existente em
um veículo:

```text
estado do motor
      |
      v
sensor
      |
      v
sinal elétrico
      |
      v
chicote
      |
      v
entrada da ECU
      |
      v
condicionamento
      |
      v
ADC / timer
      |
      v
firmware
```

No UTFuel, o ESP32-S3 Simulator substitui o motor e os sensores durante
o desenvolvimento em bancada.

Isso permitirá testar a ECU de maneira controlada, repetível e
mensurável.