# KileIsland

KileIsland é um jogo 2D de sobrevivência feito em C# com Raylib-cs. Explore a ilha, colete minérios, equipe uma espada e sobreviva às noites.

## Requisitos

- .NET SDK 10
- Windows, Linux ou macOS com suporte a janela grafica

Para iniciar, rode este comando na pasta do projeto:

    dotnet run

O jogo procura sprites e arquivos locais a partir da pasta em que é iniciado.

## Controles

| Ação | Padrão (P1) | Padrão (P2) |
| --- | --- | --- |
| Mover | WASD | Setas |
| Interagir com a casa, a forja ou a Juju | E | Enter |
| Abrir inventário | I | T |
| Alternar entre picareta e espada | Q | RightShift |
| Minerar ou atacar | Clique esquerdo / Espaço | RightControl |
| Usar carta da fila | H | H |
| Navegar nos menus | Setas; Enter confirma; Esc volta | Igual |

Os botões do controle e as teclas de interação podem ser remapeados em **Configurações**. O movimento usa WASD, as setas, o direcional analógico ou o D-pad.

## Como jogar

Durante o dia, procure veios de cobre, ferro e ouro. Aproxime-se e use a picareta; cada veio precisa de três golpes. Os drops são coletados ao encostar neles. Há um limite de minérios no mapa, e novos veios aparecem ao longo do dia.

Na forja, venda minérios por moedas e compre espadas usando nível, moedas e XP. Depois, equipe a espada pelo inventário. Cada zumbi derrotado concede 10 XP. Os valores atuais das espadas são:

| Espada | Nivel | Moedas | XP | Dano |
| --- | ---: | ---: | ---: | ---: |
| Madeira | 1 | 50 | 0 | 5 |
| Pedra | 3 | 150 | 50 | 10 |
| Ferro | 5 | 400 | 150 | 18 |
| Ouro | 8 | 1000 | 400 | 30 |

O ciclo tem 90 segundos de dia e 90 segundos de noite. Também é possível dormir na casa para avançar para o próximo dia (dormir também aumenta a dificuldade). Os zumbis patrulham a ilha, perseguem o jogador quando ele se aproxima e procuram contornar as construções. Ao amanhecer, os que ainda estão vivos pegam fogo. A dificuldade aumenta conforme as noites passam.

A Juju vende cartas consumíveis por moedas. As cartas ficam numa fila e podem ser usadas com o botão **H** (ou **B** no controle do Xbox), respeitando um cooldown de 8 segundos. As cartas iniciais são:

| Carta | Efeito | Preço | Única? |
| --- | --- | ---: | :---: |
| Empadinha | Cura 4 de vida | 30 moedas | Não |
| Coração Extra | +1 coração permanente | 100 moedas | Sim |
| Velocidade | +30% por 3 dias | 60 moedas | Não |

### Evento do telefone

Durante o jogo, um telefone toca e inicia um evento especial. Atenda para avançar na história e desbloquear a batalha contra o boss.

### Batalha de boss

Ao final do evento do telefone, o boss **Jerisvaldo** aparece. Derrote-o para progredir. Durante a luta, ele invoca zumbis para atrapalhar o jogador.

## Menu, configurações e saves

O menu inicial oferece **Continuar**, **Novo Jogo**, **Coop Local**, **Configurações** e **Sair**. Há três slots de progresso. Ao sobreviver uma noite, o jogo salva automaticamente no slot selecionado.

O volume da música e dos efeitos, além das teclas e dos botões remapeados, fica em `Dados/configuracoes.json`. O progresso fica em `Dados/save1.json`, `Dados/save2.json` e `Dados/save3.json`. Esses arquivos são locais e não precisam ser compartilhados (estão no `.gitignore`).

## Código e recursos

- `Program.cs`: inicialização e loop principal.
- `Core/`: `AudioManager.cs` (áudio) e `Input.cs` (entrada do jogador).
- `Dados/`: `EstadoJogo.cs` (estado da partida) e os arquivos `configuracoes.json` e `saveN.json`.
- `Entities/`: `Jogador.cs`, `Zumbi.cs`, `Drop.cs`, `Carta.cs` e `Jerisvaldo.cs` (boss) — movimento, combate e entidades.
- `Eventos/`: `CicloDiaNoite.cs` (ciclo dia/noite) e `EventoTelefone.cs` (evento especial).
- `Systems/`: `ConfiguracoesJogo.cs` (configurações) e `SaveSystem.cs` (saves).
- `World/`: `CenaMundo.cs`, `Minerio.cs`, `NavegacaoZumbi.cs` e `ObjetoMapa.cs` — mundo, minérios e navegação.
- `UI/`: `MenuUI.cs`, `HUD.cs`, `Inventario.cs`, `ForjaUI.cs`, `JujuUI.cs`, `GameOver.cs`, `Efeitos.cs` e `OverlaySono.cs` — menus e HUD.
- `assets/sprites/` e `assets/audio/`: sprites e sons do jogador, zumbis, ilha, minérios, estruturas, espadas, cartas e boss.

A versão atual do projeto é **0.4.1**. O Raylib-cs é restaurado pelo .NET a partir do `KileIsland.csproj`.