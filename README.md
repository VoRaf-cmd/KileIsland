# KileIsland

KileIsland é um jogo 2D de sobrevivência feito em C# com Raylib-cs. Explore a ilha, colete minérios, equipe uma espada e sobreviva às noites.

## Requisitos

- .NET SDK 10
- Windows, Linux ou macOS com suporte a janela grafica

Para iniciar, rode este comando na pasta do projeto:

```powershell
dotnet run
```

O jogo procura sprites e arquivos locais a partir da pasta em que é iniciado.

## Controles

| Ação | Padrão |
| --- | --- |
| Mover | WASD ou setas |
| Interagir com a casa ou a forja | E |
| Abrir inventário | I |
| Alternar entre picareta e espada | Q |
| Minerar ou atacar | Clique esquerdo |
| Navegar nos menus | Setas; Enter confirma; Esc volta |

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

O ciclo tem 90 segundos de dia e 90 segundos de noite. Também é possível dormir na casa para avançar para o próximo dia. Os zumbis patrulham a ilha, perseguem o jogador quando ele se aproxima e procuram contornar as construções. Ao amanhecer, os que ainda estão vivos pegam fogo. A dificuldade aumenta conforme as noites passam.

A Juju aparece no cenário, mas ainda não tem interação própria.

## Menu, configuracoes e saves

O menu inicial oferece **Continuar**, **Novo Jogo**, **Configurações** e **Sair**. Há três slots de progresso. Ao sobreviver uma noite, o jogo salva automaticamente no slot selecionado.

O volume da música e dos efeitos, além das teclas e dos botões remapeados, fica em `configuracoes.json`. O progresso fica em `save1.json`, `save2.json` e `save3.json`. Esses arquivos são locais e não precisam ser compartilhados.

## Codigo e recursos

- `Program.cs`: inicialização, loop principal, combate, coleta e spawn.
- `Jogador.cs`, `Zumbi.cs` e `NavegacaoZumbi.cs`: movimento, combate, IA e navegação.
- `Dados/EstadoJogo.cs` e `SaveSystem.cs`: estado da partida e saves.
- `MenuUI.cs`, `UI/` e `ConfiguracoesJogo.cs`: menus, HUD, forja, inventário e configurações.
- `assets/sprites/`: sprites do jogador, dos zumbis, da ilha, dos minérios, das estruturas e das espadas.

A versão atual do projeto é **0.2.0**. O Raylib-cs é restaurado pelo .NET a partir do `KileIsland.csproj`.
