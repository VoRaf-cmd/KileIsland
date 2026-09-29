using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class Program
{
    public const int LarguraTela = 1024;
    public const int AlturaTela = 576;

    public const float DistanciaInteracao = 400f;
    public const float DistanciaMinerar = 150f;
    public const float DistanciaAtaque = 150f;
    public const float EspacamentoMinerio = 400f;
    public const float DistanciaDeEstruturas = 400f;
    public const int MaxMineriosNoMapa = 12;
    public const float TempoEntreSpawns = 12f;
    public const int PicaretadasParaQuebrar = 3;

    public const int XpPorZumbi = 10;
    const string MusicaDoMenu = "assets/audio/musicas/menu.wav";
    const string MusicaDoDia = "assets/audio/musicas/musica_dia.mp3";
    const string MusicaDaNoite = "assets/audio/musicas/musica_noite.mp3";

    public static bool ForjaAberta = false;
    public static bool InventarioAberto = false;
    public static float TempoDesdeAbrirMenu = 0f;

    public static Jogador[]? jogadoresGlobais = null;

    struct CoisaDesenhavel
    {
        public float BaseY;
        public Action Desenha;
    }

    [STAThread]
    public static void Main()
    {
        Raylib.InitWindow(LarguraTela, AlturaTela, "KileIsland");
        Raylib.SetTargetFPS(60);

        // Esc e a tecla "Fechar/Voltar" dos menus (P1 e P2). Por padrao o Raylib fecha a
        // janela ao apertar Esc, o que encerrava o jogo ao tentar fechar a forja/inventario.
        // O jogo continua fechando pelo X da janela e pela opcao "Sair" do menu.
        Raylib.SetExitKey(KeyboardKey.Null);

        ConfiguracoesJogo.Carrega();
        AudioManager.Inicia();
        EventoTelefone.CarregaSprite();
        AudioManager.CarregaSonsMenu();
        EventoTelefone.CarregaAudio();
        List<Sound> sonsSwooshEspada = CarregaEfeitos(
            "assets/audio/espada/swoosh_1.wav",
            "assets/audio/espada/swoosh_2.wav",
            "assets/audio/espada/swoosh_3.mp3");
        List<Sound> sonsAcertoEspada = CarregaEfeitos(
            "assets/audio/espada/hit_1.wav",
            "assets/audio/espada/hit_2.mp3",
            "assets/audio/espada/hit_3.wav");
        List<Sound> sonsPicareta = CarregaEfeitos(
            "assets/audio/picareta/hit.wav");
        int ultimaVariacaoSwoosh = -1;
        int ultimaVariacaoAcerto = -1;
        int ultimaVariacaoPicareta = -1;
        AudioManager.TocaMusica(MusicaDoMenu);

        // ================= MENU INICIAL =================
        MenuUI.ReiniciaEstado();

        while (!Raylib.WindowShouldClose() && !MenuUI.JogoDeveIniciar)
        {
            InputState menuInput = Input.Read(0);
            AudioManager.AtualizaMusica();
            MenuUI.Atualiza(menuInput);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            MenuUI.Desenha();
            Raylib.EndDrawing();
        }

        if (Raylib.WindowShouldClose())
        {
            Raylib.CloseWindow();
            return;
        }

        // ================= SETUP DO JOGO =================
        int qtdJogadores = MenuUI.CoopAtivo ? 2 : 1;
        Console.WriteLine($"[COOP] CoopAtivo={MenuUI.CoopAtivo} | qtdJogadores={qtdJogadores}");
        Jogador[] jogadores = new Jogador[qtdJogadores];
        for (int i = 0; i < qtdJogadores; i++)
            jogadores[i] = new Jogador(i);

        jogadoresGlobais = jogadores;

        foreach (var j in jogadores)
            j.CarregaSprites();

        Jogador.CarregaPicareta();
        Jogador.CarregaEspadas();
        HUD.Carrega();
        CenaMundo.Carrega();
        Zumbi.CarregaSprites();
        Jerisvaldo.CarregaSprite();
        Jerisvaldo.CarregaSpriteMarcaX();

        Vector2 posInicial = new Vector2(
            CenaMundo.LarguraMapa / 2f - jogadores[0].TamanhoVisual / 2f,
            CenaMundo.AlturaMapa / 2f - jogadores[0].TamanhoVisual / 2f
        );

        jogadores[0].Pos = posInicial;
        if (jogadores.Length > 1)
            jogadores[1].Pos = posInicial + new Vector2(120f, 0f);

        List<ObjetoMapa> objetos = new List<ObjetoMapa>();

        ObjetoMapa casa = ObjetoMapa.Cria("assets/sprites/casa.png",
            new Vector2(180, 180), 384, 384, true,
            new Color(140, 90, 50, 255), "CASA",
            alturaColisao: 96);

        objetos.Add(casa);

        ObjetoMapa juju = ObjetoMapa.Cria("assets/sprites/juju.png",
            new Vector2(1700, 200), 256, 256, true,
            new Color(120, 60, 160, 255), "JUJU",
            alturaColisao: 64);

        objetos.Add(juju);

        ObjetoMapa forja = ObjetoMapa.Cria("assets/sprites/forja.png",
            new Vector2(1700, 1000), 256, 256, true,
            Color.Gray, "FORJA",
            alturaColisao: 64);

        objetos.Add(forja);

        NavegacaoZumbi navegacaoZumbi = new NavegacaoZumbi(
            objetos, Zumbi.TamanhoArte * Zumbi.Escala);

        List<Minerio> minerios = new List<Minerio>();
        List<Drop> drops = new List<Drop>();
        List<Zumbi> zumbis = new List<Zumbi>();
        Random rng = new Random();
        float tempoSpawn = 0f;
        float tempoSpawnZumbi = 0f;
        float[] tempoCooldownPicareta = new float[2] { 0f, 0f };
        bool zumbisInvocadosNesteCiclo = false;
        bool pausado = false;
        bool sairDoJogo = false;

        for (int i = 0; i < 10; i++)
            TentaSpawnarMinerio(minerios, rng, objetos);

        if (MenuUI.SlotAtual >= 0 && !MenuUI.NovoJogo)
        {
            DadosSave dados = SaveSystem.Carrega(MenuUI.SlotAtual);
            if (dados != null)
            {
                SaveSystem.Aplica(dados, jogadores[0]);

                if (jogadores.Length > 1)
                    jogadores[1].Pos = new Vector2(dados.PosX + 120f, dados.PosY);

                if (EstadoJogo.EhDia)
                    CicloDiaNoite.ForcaDia();
            }
        }

        AudioManager.TocaMusica(MusicaDoHorario());

        Camera2D camera = new Camera2D();
        camera.Offset = new Vector2(LarguraTela / 2f, AlturaTela / 2f);
        camera.Zoom = 0.75f;
        camera.Rotation = 0f;
        camera.Target = CentroDosJogadores(jogadores);
        EventoTelefone eventoTelefone = new EventoTelefone();
        Jerisvaldo jerisvaldo = null;

        List<CoisaDesenhavel> coisas = new List<CoisaDesenhavel>();

        // ================= LOOP DO JOGO =================
        while (!Raylib.WindowShouldClose())
        {
            float delta = Raylib.GetFrameTime();
            for (int ci = 0; ci < 2; ci++)
                tempoCooldownPicareta[ci] = Math.Max(0f, tempoCooldownPicareta[ci] - delta);

            InputState[] inputs = new InputState[2];
            inputs[0] = Input.Read(0);
            inputs[1] = jogadores.Length > 1 ? Input.Read(1) : inputs[0];

            // ===== Pausa =====
            // Enquanto pausado, delta = 0 e os inputs de jogo sao zerados: nada anda, nenhum
            // timer avanca e a cena continua sendo desenhada (fica como fundo do menu).
            bool startPressionado = false;
            for (int gi = 0; gi < 2; gi++)
                if (Raylib.IsGamepadAvailable(gi) &&
                    Raylib.IsGamepadButtonPressed(gi, GamepadButton.MiddleRight))
                    startPressionado = true;

            bool estavaPausado = pausado;
            if (pausado)
            {
                InputState menuPausa = CombinaMenu(inputs[0], inputs[1]);
                AcaoPausa acaoPausa = PauseUI.Atualiza(menuPausa);
                if (acaoPausa == AcaoPausa.Nada && startPressionado)
                    acaoPausa = AcaoPausa.Continuar;

                if (acaoPausa == AcaoPausa.Continuar) pausado = false;
                else if (acaoPausa == AcaoPausa.Sair) sairDoJogo = true;
                else if (acaoPausa == AcaoPausa.Salvar)
                {
                    if (MenuUI.SlotAtual >= 0)
                    {
                        SaveSystem.Salva(MenuUI.SlotAtual, jogadores[0], jogadores.Length > 1);
                        PauseUI.MostraMensagem("Jogo salvo!");
                    }
                    else PauseUI.MostraMensagem("Nenhum slot de save selecionado.");
                }

                delta = 0f;
                inputs = new InputState[2];
            }

            if (sairDoJogo) break;

            InputState input = inputs[0];

            AudioManager.AtualizaMusica();

            int diaAntesDeDormir = EstadoJogo.Dia;
            bool eraDiaAntesDeDormir = EstadoJogo.EhDia;
            OverlaySono.Atualiza(delta);
            if (eraDiaAntesDeDormir != EstadoJogo.EhDia)
                AudioManager.TocaMusica(MusicaDoHorario());

            if (diaAntesDeDormir < EventoTelefone.DiaDoEvento &&
                EstadoJogo.Dia >= EventoTelefone.DiaDoEvento)
            {
                eventoTelefone.Inicia(camera.Target, camera.Zoom);
            }

            // Telefone: quem atende e o jogador vivo que apertou "Interagir" perto dele
            // (ou o mais proximo). Nos dialogos qualquer um avanca a fala. A camera
            // volta para o meio dos dois jogadores.
            int idEvento = JogadorDoEvento(jogadores, inputs, eventoTelefone);
            InputState inputEvento = inputs[idEvento];
            if (jogadores.Length > 1 && eventoTelefone.Fase == FaseEventoTelefone.Dialogo)
                inputEvento.InteractPressed = inputs[0].InteractPressed || inputs[1].InteractPressed;

            eventoTelefone.Atualiza(delta, inputEvento, jogadores[idEvento].Centro(),
                                    ref camera, CentroDosJogadores(jogadores));
            if (eventoTelefone.ConsomePedidoInicioBatalha())
            {
                CenaMundo.AmpliaIlha(1.25f);
                objetos.Clear();
                minerios.Clear();
                drops.Clear();
                zumbis.Clear();
                tempoSpawn = 0f;
                tempoSpawnZumbi = 0f;
                navegacaoZumbi = new NavegacaoZumbi(
                    objetos, Zumbi.TamanhoArte * Zumbi.Escala);
                jerisvaldo = new Jerisvaldo(
                    CenaMundo.CentroIlha + new Vector2(0f, -360f));
            }

            bool dormindo = OverlaySono.Dormindo;
            bool menuAberto = ForjaAberta || InventarioAberto;
            bool travado = dormindo || menuAberto || GameOverUI.Ativo ||
                           eventoTelefone.BloqueiaJogador;
            LevelUpAviso.Atualiza(delta);

            // Esc ou Start abre a pausa (Esc fecha forja/inventario quando eles estao abertos,
            // entao nesse caso nao pausa).
            if (!estavaPausado && !pausado && !menuAberto && !dormindo && !GameOverUI.Ativo &&
                (Raylib.IsKeyPressed(KeyboardKey.Escape) || startPressionado))
            {
                pausado = true;
                PauseUI.Abre();
            }

            // Debug P2 (F2)
            if (Raylib.IsKeyPressed(KeyboardKey.F2) && jogadores.Length > 1)
            {
                Vector2 c2 = jogadores[1].Centro();
                Console.WriteLine($"[DEBUG P2] Interact={inputs[1].InteractPressed} Inv={inputs[1].InventoryPressed} Swap={inputs[1].SwapItemPressed}");
                Console.WriteLine($"[DEBUG P2] Centro={c2} | Casa={casa.Centro()} Dist={Vector2.Distance(c2, casa.Centro()):F0}");
                Console.WriteLine($"[DEBUG P2] Forja={forja.Centro()} Dist={Vector2.Distance(c2, forja.Centro()):F0}");
                Console.WriteLine($"[DEBUG P2] ForjaAberta={ForjaAberta} InvAberto={InventarioAberto} Travado={travado}");
            }

            if (GameOverUI.Ativo)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.R) || AlgumControleConfirmou())
                {
                    AudioManager.TocaCliqueMenu();
                    CenaMundo.RestauraDimensoes();
                    EstadoJogo.Resetar();
                    jogadores[0].Pos = posInicial;
                    if (jogadores.Length > 1)
                        jogadores[1].Pos = posInicial + new Vector2(120f, 0f);

                    foreach (var j in jogadores)
                    {
                        j.VidaPontos = j.VidaMaxPontos;
                        j.EhFantasma = false;
                        j.ItemAtual = ItemEquipado.Picareta;
                        j.VidaPontosReset();
                    }

                    ForjaAberta = false;
                    InventarioAberto = false;
                    OverlaySono.Estado = EstadoSono.Acordado;
                    OverlaySono.Tempo = 0f;
                    LevelUpAviso.Tempo = 0f;

                    objetos.Clear();
                    objetos.Add(casa);
                    objetos.Add(juju);
                    objetos.Add(forja);
                    navegacaoZumbi = new NavegacaoZumbi(
                        objetos, Zumbi.TamanhoArte * Zumbi.Escala);

                    zumbis.Clear();
                    zumbisInvocadosNesteCiclo = false;
                    drops.Clear();
                    minerios.Clear();
                    for (int i = 0; i < 10; i++)
                        TentaSpawnarMinerio(minerios, rng, objetos);

                    CicloDiaNoite.ForcaDia();
                    eventoTelefone = new EventoTelefone();
                    jerisvaldo = null;
                    camera.Target = CentroDosJogadores(jogadores);
                    camera.Zoom = 0.75f;
                    AudioManager.TocaMusica(MusicaDoDia);
                    GameOverUI.Ativo = false;
                    tempoSpawnZumbi = 0f;
                    tempoSpawn = 0f;
                    tempoCooldownPicareta[0] = 0f;
                    tempoCooldownPicareta[1] = 0f;
                }
            }

            if (menuAberto) TempoDesdeAbrirMenu += delta;
            else TempoDesdeAbrirMenu = 0f;

            // ===== Movimento dos jogadores =====
            for (int ji = 0; ji < jogadores.Length; ji++)
            {
                var j = jogadores[ji];
                Vector2 inputDelta = j.LeInput(inputs[ji], travado);
                float tv = j.TamanhoVisual;

                Vector2 tentativaX = new Vector2(j.Pos.X + inputDelta.X, j.Pos.Y);
                if (!Colide(tentativaX, tv, objetos))
                    j.Pos = new Vector2(tentativaX.X, j.Pos.Y);

                Vector2 tentativaY = new Vector2(j.Pos.X, j.Pos.Y + inputDelta.Y);
                if (!Colide(tentativaY, tv, objetos))
                    j.Pos = new Vector2(j.Pos.X, tentativaY.Y);

                j.AplicaKnockback(delta);

                j.Pos = new Vector2(
                    Math.Clamp(j.Pos.X, CenaMundo.PosIlha.X,
                        CenaMundo.PosIlha.X + CenaMundo.GramaLargura - tv),
                    Math.Clamp(j.Pos.Y, CenaMundo.PosIlha.Y,
                        CenaMundo.PosIlha.Y + CenaMundo.GramaAltura - tv)
                );
            }

            // ===== Interacao (qualquer um dos dois pode abrir) =====
            for (int ji = 0; ji < jogadores.Length; ji++)
            {
                if (jogadores[ji].EhFantasma) continue;

                Vector2 centroJ = jogadores[ji].Centro();
                bool pertoCasa  = Vector2.Distance(centroJ, casa.Centro())  < DistanciaInteracao;
                bool pertoForja = Vector2.Distance(centroJ, forja.Centro()) < DistanciaInteracao;

                if (!travado && !eventoTelefone.Ativo && inputs[ji].InteractPressed)
                {
                    if (pertoForja)
                    {
                        ForjaAberta = true;
                        ForjaUI.ResetarSelecao();
                        TempoDesdeAbrirMenu = 0f;
                        break;
                    }
                    else if (pertoCasa)
                    {
                        OverlaySono.Inicia();
                        Efeitos.Shake(2f, 0.3f);
                        break;
                    }
                }
            }

            // Inventario: qualquer um abre
            if (!dormindo && !ForjaAberta &&
                !GameOverUI.Ativo && !eventoTelefone.BloqueiaJogador)
            {
                bool algumAbriuInv = inputs[0].InventoryPressed ||
                                     (jogadores.Length > 1 && inputs[1].InventoryPressed);

                if (algumAbriuInv)
                {
                    if (InventarioAberto) InventarioAberto = false;
                    else
                    {
                        InventarioAberto = true;
                        InventarioUI.ResetarSelecao();
                        TempoDesdeAbrirMenu = 0f;
                    }
                }
            }

            // Trocar item: individual por jogador
            for (int ji = 0; ji < jogadores.Length; ji++)
            {
                if (travado || jogadores[ji].EhFantasma) continue;

                if (inputs[ji].SwapItemPressed)
                {
                    jogadores[ji].ItemAtual = jogadores[ji].ItemAtual == ItemEquipado.Espada
                        ? ItemEquipado.Picareta
                        : ItemEquipado.Espada;
                }
            }

            bool podeInteragirMenu = TempoDesdeAbrirMenu > 0.2f;

            // Fechar menu: qualquer um fecha
            if (podeInteragirMenu && menuAberto)
            {
                bool algumFechou = inputs[0].ClosePressed ||
                                   (jogadores.Length > 1 && inputs[1].ClosePressed);
                if (algumFechou)
                {
                    ForjaAberta = false;
                    InventarioAberto = false;
                }
            }

            // ===== Ataque (os 2 podem atacar) =====
            for (int ji = 0; ji < jogadores.Length; ji++)
            {
                var j = jogadores[ji];
                if (j.EhFantasma) continue;

                InputState inputJ = inputs[ji];

                bool querAtacar = inputJ.MouseClickPressed || inputJ.AttackPressed;
                if (travado || j.Batendo || !querAtacar) continue;

                bool ehPicareta = j.ItemAtual == ItemEquipado.Picareta;
                if (ehPicareta && tempoCooldownPicareta[ji] > 0f) continue;

                Vector2 centroJ = j.Centro();

                if (ehPicareta)
                {
                    Minerio alvo = MinerioMaisProximo(minerios, centroJ, DistanciaMinerar);
                    if (alvo != null)
                    {
                        TocaVariacao(sonsPicareta, ref ultimaVariacaoPicareta);
                        tempoCooldownPicareta[ji] = 0.6f;
                        alvo.PicaretadasRestantes--;
                        alvo.TempoTremor = 0.15f;
                        alvo.IniciaAnimacao();
                        j.IniciaBatida();

                        if (alvo.PicaretadasRestantes <= 0)
                        {
                            Drop novoDrop = new Drop();
                            novoDrop.Tipo = alvo.Tipo;
                            novoDrop.Pos = new Vector2(
                                alvo.Pos.X + (Minerio.Tamanho - Drop.Tamanho) / 2f,
                                alvo.Pos.Y + (Minerio.Tamanho - Drop.Tamanho) / 2f
                            );
                            novoDrop.TempoAnimacao = Drop.DuracaoPulo;
                            drops.Add(novoDrop);
                            minerios.Remove(alvo);
                            Efeitos.Shake(5f, 0.3f);
                        }
                        else Efeitos.Shake(2f, 0.15f);
                    }
                }
                else
                {
                    j.IniciaBatida();
                    TocaVariacao(sonsSwooshEspada, ref ultimaVariacaoSwoosh);

                    // Dano base 2 (sem espada) ou dano da espada equipada
                    int dano = 2;
                    int indiceEspada = EstadoJogo.EspadaEquipada;
                    if (indiceEspada >= 0
                        && indiceEspada < EstadoJogo.Espadas.Count
                        && indiceEspada < EstadoJogo.EspadasCompradas.Length
                        && EstadoJogo.EspadasCompradas[indiceEspada])
                    {
                        dano = EstadoJogo.Espadas[indiceEspada].Dano;
                    }

                    bool bossNoAlcance = eventoTelefone.BatalhaAtiva &&
                        jerisvaldo != null && jerisvaldo.EstaVivo &&
                        jerisvaldo.EstaNoAlcance(centroJ, DistanciaAtaque);

                    if (bossNoAlcance && jerisvaldo!.EstaVulneravel)
                    {
                        if (jerisvaldo.TomaDano(dano))
                        {
                            TocaVariacao(sonsAcertoEspada, ref ultimaVariacaoAcerto);
                            Efeitos.Shake(6f, 0.18f);
                        }
                    }
                    else
                    {
                        Zumbi alvoZumbi = ZumbiMaisProximo(zumbis, centroJ, DistanciaAtaque);
                        if (alvoZumbi != null)
                        {
                            TocaVariacao(sonsAcertoEspada, ref ultimaVariacaoAcerto);
                            bool morreu = alvoZumbi.TomaDano(dano);

                            if (morreu)
                            {
                                EstadoJogo.GanhaXp(XpPorZumbi);
                                Efeitos.Shake(8f, 0.4f);
                            }
                            else Efeitos.Shake(4f, 0.2f);
                        }
                    }
                }
            }

            // ===== Coleta de drops =====
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                bool coletado = false;
                foreach (var j in jogadores)
                {
                    if (j.EhFantasma) continue;
                    Rectangle rectJ = new Rectangle(j.Pos.X, j.Pos.Y,
                        j.TamanhoVisual, j.TamanhoVisual);
                    if (Raylib.CheckCollisionRecs(rectJ, drops[i].Retangulo()))
                    {
                        coletado = true;
                        break;
                    }
                }

                if (coletado)
                {
                    switch (drops[i].Tipo)
                    {
                        case TipoMinerio.Cobre: EstadoJogo.Cobre++; break;
                        case TipoMinerio.Ferro: EstadoJogo.Ferro++; break;
                        case TipoMinerio.Ouro:  EstadoJogo.Ouro++;  break;
                    }
                    drops.RemoveAt(i);
                }
            }

            bool eraDia = EstadoJogo.EhDia;
            if (!eventoTelefone.BatalhaAtiva)
                CicloDiaNoite.Atualiza(delta);
            if (eraDia != EstadoJogo.EhDia)
                AudioManager.TocaMusica(MusicaDoHorario());

            if (!eraDia && EstadoJogo.EhDia)
            {
                EstadoJogo.NoitesSobrevividas++;

                if (MenuUI.SlotAtual >= 0)
                    SaveSystem.Salva(MenuUI.SlotAtual, jogadores[0], jogadores.Length > 1);
            }

            if (!EstadoJogo.EhDia && !eventoTelefone.ArenaAtiva)
            {
                tempoSpawnZumbi += delta;
                float intervalo = 3f - EstadoJogo.NoitesSobrevividas * 0.3f;
                if (intervalo < 1f) intervalo = 1f;

                if (tempoSpawnZumbi >= intervalo)
                {
                    tempoSpawnZumbi = 0f;
                    Vector2 pos = PosAleatoriaZumbi(rng, jogadores, navegacaoZumbi);

                    int forcaNoite = EstadoJogo.NoitesSobrevividas;
                    float fatorNoite = 1f + forcaNoite * 0.75f;

                    Zumbi z = new Zumbi();
                    z.Pos = pos;
                    z.Vida = (int)MathF.Round(5f * fatorNoite);
                    z.DanoContato = (int)MathF.Round(1f * fatorNoite);
                    z.MultiplicadorVelocidade = fatorNoite;
                    zumbis.Add(z);

                    Console.WriteLine($"[ZUMBI] Noite {forcaNoite} | Vida {z.Vida} | Dano {z.DanoContato} | Vel {z.MultiplicadorVelocidade:F2}");
                }
            }
            else tempoSpawnZumbi = 0f;

            foreach (var j in jogadores) j.Atualiza(delta);
            foreach (var m in minerios) m.Atualiza(delta);
            foreach (var d in drops) d.Atualiza(delta);
            Efeitos.Atualiza(delta);

            float velZumbiBase = Jogador.Velocidade * 0.72f;

            for (int i = zumbis.Count - 1; i >= 0; i--)
            {
                var z = zumbis[i];
                Vector2 alvoZ = JogadorVivoMaisProximoDe(jogadores, z.Centro());

                bool queimarAoAmanhecer = EstadoJogo.EhDia && !eventoTelefone.ArenaAtiva;
                z.Atualiza(delta, alvoZ, velZumbiBase, navegacaoZumbi, queimarAoAmanhecer);

                if (z.EstaMorto())
                {
                    zumbis.RemoveAt(i);
                    continue;
                }

                bool podeMachucar = z.Estado == EstadoZumbi.Andando || z.Estado == EstadoZumbi.Parado;

                if (podeMachucar)
                {
                    foreach (var j in jogadores)
                    {
                        if (j.EhFantasma) continue;
                        if (Raylib.CheckCollisionRecs(j.Retangulo(), z.Retangulo()))
                        {
                            j.TomaDano(z.Centro(), z.DanoContato);
                            break;
                        }
                    }
                }
            }

            if (jerisvaldo != null && eventoTelefone.ArenaAtiva)
            {
                Rectangle limitesIlha = new Rectangle(
                    CenaMundo.PosIlha.X,
                    CenaMundo.PosIlha.Y,
                    CenaMundo.GramaLargura,
                    CenaMundo.GramaAltura
                );
                Vector2 alvoBoss = JogadorVivoMaisProximoDe(jogadores, jerisvaldo.Centro);
                float shakeAntes = jerisvaldo.IntensidadeShake;
                jerisvaldo.Atualiza(delta, alvoBoss, limitesIlha);

                if (jerisvaldo.IntensidadeShake > shakeAntes)
                    Efeitos.Shake(jerisvaldo.IntensidadeShake, 0.45f);

                if (jerisvaldo.ConsomeDanoImpacto())
                {
                    foreach (var j in jogadores)
                    {
                        if (j.EhFantasma) continue;
                        if (Vector2.Distance(j.Centro(), jerisvaldo.PosicaoImpacto) <= jerisvaldo.RaioDanoArea)
                            j.TomaDano(jerisvaldo.PosicaoImpacto, jerisvaldo.DanoImpacto);
                    }
                }

                if (jerisvaldo.DeveSpawnarZumbis && !zumbisInvocadosNesteCiclo)
                {
                    foreach (Vector2 centroInvocacao in jerisvaldo.PosicoesZumbis)
                    {
                        Zumbi invocado = new Zumbi
                        {
                            Pos = centroInvocacao - new Vector2(Zumbi.TamanhoArte * Zumbi.Escala / 2f),
                            Vida = Math.Max(2, EstadoJogo.NoitesSobrevividas + 2),
                            DanoContato = 1
                        };
                        invocado.IniciaInvocacao();
                        zumbis.Add(invocado);
                    }

                    zumbisInvocadosNesteCiclo = true;
                }

                if (!jerisvaldo.DeveSpawnarZumbis)
                {
                    zumbisInvocadosNesteCiclo = false;
                }
            }

            bool todosFantasmas = true;
            foreach (var j in jogadores)
            {
                if (!j.EhFantasma) { todosFantasmas = false; break; }
            }

            if (todosFantasmas && !GameOverUI.Ativo)
            {
                GameOverUI.Inicia();
                Efeitos.Shake(15f, 0.6f);
            }

            if (EstadoJogo.EhDia && !eventoTelefone.ArenaAtiva)
            {
                tempoSpawn += delta;
                if (tempoSpawn >= TempoEntreSpawns && minerios.Count < MaxMineriosNoMapa)
                {
                    tempoSpawn = 0f;
                    TentaSpawnarMinerio(minerios, rng, objetos);
                }
            }

            if (!eventoTelefone.ControlaCamera)
            {
                Vector2 alvoCamera = CentroDosJogadores(jogadores) + (pausado ? Vector2.Zero : Efeitos.OffsetShake());
                camera.Target = Vector2.Lerp(camera.Target, alvoCamera, 0.05f);

                float zoomAlvo = CalculaZoomDinamico(jogadores);
                camera.Zoom += (zoomAlvo - camera.Zoom) * 0.05f;
            }

            Raylib.BeginDrawing();
            Raylib.BeginMode2D(camera);
            CenaMundo.DesenhaChao();

            coisas.Clear();

            foreach (var obj in objetos)
            {
                var objLocal = obj;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = objLocal.Pos.Y + objLocal.Altura,
                    Desenha = () =>
                    {
                        Efeitos.DesenhaSombra(objLocal.Pos.X + objLocal.Largura / 2f,
                                              objLocal.Pos.Y + objLocal.Altura,
                                              objLocal.Largura * 0.7f,
                                              objLocal.Altura * 0.15f);
                        objLocal.Desenha();
                    }
                });
            }

            foreach (var m in minerios)
            {
                var mLocal = m;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = mLocal.Pos.Y + Minerio.Tamanho,
                    Desenha = () => mLocal.Desenha()
                });
            }

            foreach (var d in drops)
            {
                var dLocal = d;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = dLocal.Pos.Y + Drop.Tamanho,
                    Desenha = () => dLocal.Desenha()
                });
            }

            if (eventoTelefone.TelefoneVisivel)
            {
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = eventoTelefone.PosicaoTelefone.Y + 80f,
                    Desenha = () => eventoTelefone.DesenhaTelefone()
                });
            }

            foreach (var z in zumbis)
            {
                var zLocal = z;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = zLocal.Pos.Y + zLocal.TamanhoVisual,
                    Desenha = () =>
                    {
                        if (ZumbiAtrasDeEstrutura(zLocal, objetos))
                            DesenhaOutlineZumbi(zLocal);
                        zLocal.Desenha();
                    }
                });
            }

            if (jerisvaldo != null && eventoTelefone.ArenaAtiva)
            {
                var bossLocal = jerisvaldo;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = bossLocal.Pos.Y + bossLocal.TamanhoVisual,
                    Desenha = () => bossLocal.Desenha()
                });
            }

            foreach (var j in jogadores)
            {
                var jLocal = j;
                coisas.Add(new CoisaDesenhavel
                {
                    BaseY = jLocal.Pos.Y + jLocal.TamanhoVisual,
                    Desenha = () =>
                    {
                        jLocal.Desenha();
                        jLocal.DesenhaArma();
                    }
                });
            }

            coisas.Sort((a, b) => a.BaseY.CompareTo(b.BaseY));

            foreach (var c in coisas) c.Desenha();

            Raylib.EndMode2D();

            CicloDiaNoite.Desenha();
            HUD.Desenha(jogadores);
            if (eventoTelefone.BatalhaAtiva && jerisvaldo != null)
                jerisvaldo.DesenhaInterface();

            if (!eventoTelefone.Ativo && !travado && !dormindo)
            {
                int linhaAviso = 0;
                for (int ji = 0; ji < jogadores.Length; ji++)
                {
                    if (jogadores[ji].EhFantasma) continue;

                    Vector2 centroJ = jogadores[ji].Centro();
                    bool pertoCasa  = Vector2.Distance(centroJ, casa.Centro())  < DistanciaInteracao;
                    bool pertoForja = Vector2.Distance(centroJ, forja.Centro()) < DistanciaInteracao;

                    string? acaoAviso = pertoForja ? "abrir a forja" : (pertoCasa ? "dormir" : null);
                    if (acaoAviso == null) continue;

                    // Mostra a tecla/botao REAL de cada jogador (P2 usa Enter, nao E).
                    DesenhaAviso($"P{ji + 1}: Pressione {Input.NomeInteragir(ji)} para {acaoAviso}", linhaAviso);
                    linhaAviso++;
                }
            }

            if (ForjaAberta) ForjaUI.Desenha(inputs, podeInteragirMenu);
            if (InventarioAberto) InventarioUI.Desenha(inputs, podeInteragirMenu);

            OverlaySono.Desenha(jogadores);
            GameOverUI.Desenha();
            eventoTelefone.DesenhaInterface(jogadores[idEvento].Centro(), Input.NomeInteragir(idEvento));

            if (pausado) PauseUI.Desenha();

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    // ================= HELPERS (fora do Main) =================

    public static void ReviverTodosAoDormir()
    {
        if (jogadoresGlobais == null) return;

        foreach (var j in jogadoresGlobais)
        {
            j.VidaPontos = j.VidaMaxPontos;
            j.EhFantasma = false;
            j.VidaPontosReset();
            j.TempoKnockback = 0f;
            j.VelocidadeKnockback = Vector2.Zero;
        }
    }

    static Vector2 CentroDosJogadores(Jogador[] jogadores)
    {
        Vector2 soma = Vector2.Zero;
        foreach (var j in jogadores) soma += j.Centro();
        return soma / jogadores.Length;
    }

    // Zoom dinamico: quanto mais longe os jogadores, mais a camera afasta
    static float CalculaZoomDinamico(Jogador[] jogadores)
    {
        if (jogadores.Length < 2)
            return 0.75f;

        float distancia = Vector2.Distance(jogadores[0].Centro(), jogadores[1].Centro());

        const float distMin = 400f;
        const float distMax = 1400f;
        const float zoomMax = 0.75f;
        const float zoomMin = 0.45f;

        if (distancia <= distMin) return zoomMax;
        if (distancia >= distMax) return zoomMin;

        float t = (distancia - distMin) / (distMax - distMin);
        t = t * t * (3f - 2f * t);

        return zoomMax + (zoomMin - zoomMax) * t;
    }

    static Vector2 JogadorVivoMaisProximoDe(Jogador[] jogadores, Vector2 ponto)
    {
        Vector2 melhor = jogadores[0].Centro();
        float melhorDist = float.MaxValue;

        foreach (var j in jogadores)
        {
            if (j.EhFantasma) continue;
            float d = Vector2.Distance(ponto, j.Centro());
            if (d < melhorDist) { melhorDist = d; melhor = j.Centro(); }
        }

        return melhor;
    }

    static bool ZumbiAtrasDeEstrutura(Zumbi z, List<ObjetoMapa> objetos)
    {
        Rectangle rectZumbi = z.Retangulo();
        float baseYzumbi = z.Pos.Y + z.TamanhoVisual;

        foreach (var obj in objetos)
        {
            float baseYobj = obj.Pos.Y + obj.Altura;
            if (baseYzumbi >= baseYobj) continue;

            Rectangle rectObj = obj.RetanguloColisao();
            if (Raylib.CheckCollisionRecs(rectZumbi, rectObj))
                return true;
        }
        return false;
    }

    static void DesenhaOutlineZumbi(Zumbi z)
    {
        float x = z.Pos.X;
        float y = z.Pos.Y;
        float tam = z.TamanhoVisual;
        int esp = 3;

        Color cor = Color.White;

        Raylib.DrawRectangle((int)(x - esp), (int)(y - esp), (int)(tam + esp * 2), esp, cor);
        Raylib.DrawRectangle((int)(x - esp), (int)(y + tam), (int)(tam + esp * 2), esp, cor);
        Raylib.DrawRectangle((int)(x - esp), (int)(y - esp), esp, (int)(tam + esp * 2), cor);
        Raylib.DrawRectangle((int)(x + tam), (int)(y - esp), esp, (int)(tam + esp * 2), cor);
    }

    static Vector2 PosAleatoriaZumbi(
        Random rng, Jogador[] jogadores, NavegacaoZumbi navegacao)
    {
        float minX = CenaMundo.PosIlha.X + 150;
        float maxX = CenaMundo.PosIlha.X + CenaMundo.GramaLargura - 150;
        float minY = CenaMundo.PosIlha.Y + 150;
        float maxY = CenaMundo.PosIlha.Y + CenaMundo.GramaAltura - 150;
        Vector2? alternativa = null;

        for (int i = 0; i < 60; i++)
        {
            Vector2 pos = new Vector2(
                minX + (float)rng.NextDouble() * (maxX - minX),
                minY + (float)rng.NextDouble() * (maxY - minY)
            );

            if (!navegacao.PodeOcupar(pos)) continue;
            if (DistanciaAoJogadorMaisProximo(jogadores, pos) > 600f) return pos;
            alternativa ??= pos;
        }

        if (alternativa.HasValue) return alternativa.Value;

        for (float y = minY; y <= maxY; y += 48f)
        {
            for (float x = minX; x <= maxX; x += 48f)
            {
                Vector2 pos = new Vector2(x, y);
                if (navegacao.PodeOcupar(pos)) return pos;
            }
        }

        return new Vector2(minX, minY);
    }

    static Zumbi ZumbiMaisProximo(List<Zumbi> zumbis, Vector2 de, float raioMax)
    {
        Zumbi melhor = null;
        float melhorDist = raioMax;
        foreach (var z in zumbis)
        {
            if (z.Estado == EstadoZumbi.Queimando ||
                z.Estado == EstadoZumbi.Morrendo ||
                z.EstaMorto())
                continue;

            float d = Vector2.Distance(de, z.Centro());
            if (d < melhorDist) { melhorDist = d; melhor = z; }
        }
        return melhor;
    }

    static void TentaSpawnarMinerio(List<Minerio> minerios, Random rng, List<ObjetoMapa> objetos)
    {
        float minX = CenaMundo.PosIlha.X + 150;
        float maxX = CenaMundo.PosIlha.X + CenaMundo.GramaLargura - 150;
        float minY = CenaMundo.PosIlha.Y + 150;
        float maxY = CenaMundo.PosIlha.Y + CenaMundo.GramaAltura - 150;

        for (int tentativa = 0; tentativa < 60; tentativa++)
        {
            Vector2 pos = new Vector2(
                minX + (float)rng.NextDouble() * (maxX - minX),
                minY + (float)rng.NextDouble() * (maxY - minY)
            );

            bool ok = true;
            foreach (var m in minerios)
                if (Vector2.Distance(m.Pos, pos) < EspacamentoMinerio) { ok = false; break; }
            if (!ok) continue;

            foreach (var obj in objetos)
                if (Vector2.Distance(obj.Centro(), pos) < DistanciaDeEstruturas) { ok = false; break; }
            if (!ok) continue;

            double r = rng.NextDouble();
            TipoMinerio tipo = r < 0.6 ? TipoMinerio.Cobre
                              : (r < 0.9 ? TipoMinerio.Ferro : TipoMinerio.Ouro);

            Minerio novo = new Minerio();
            novo.Tipo = tipo;
            novo.Pos = pos;
            novo.PicaretadasRestantes = PicaretadasParaQuebrar;
            novo.TempoTremor = 0f;
            minerios.Add(novo);
            return;
        }
    }

    static Minerio MinerioMaisProximo(List<Minerio> minerios, Vector2 de, float raioMax)
    {
        Minerio melhor = null;
        float melhorDist = raioMax;
        foreach (var m in minerios)
        {
            float d = Vector2.Distance(de, m.Centro());
            if (d < melhorDist) { melhorDist = d; melhor = m; }
        }
        return melhor;
    }

    static bool Colide(Vector2 posKile, float tamanho, List<ObjetoMapa> objetos)
    {
        Rectangle rectKile = new Rectangle(posKile.X, posKile.Y, tamanho, tamanho);
        foreach (var obj in objetos)
        {
            if (!obj.Colide) continue;
            if (Raylib.CheckCollisionRecs(rectKile, obj.RetanguloColisao()))
                return true;
        }
        return false;
    }

    static List<Sound> CarregaEfeitos(params string[] caminhos)
    {
        List<Sound> efeitos = new();
        foreach (string caminho in caminhos)
        {
            if (File.Exists(caminho))
                efeitos.Add(AudioManager.RegistraEfeito(caminho));
        }

        return efeitos;
    }

    static void TocaVariacao(List<Sound> efeitos, ref int ultimaVariacao)
    {
        if (efeitos.Count == 0) return;

        int indice = Random.Shared.Next(efeitos.Count);
        if (efeitos.Count > 1 && indice == ultimaVariacao)
            indice = (indice + 1 + Random.Shared.Next(efeitos.Count - 1)) % efeitos.Count;

        ultimaVariacao = indice;
        AudioManager.TocaEfeito(efeitos[indice]);
    }

    static string MusicaDoHorario()
    {
        return EstadoJogo.EhDia ? MusicaDoDia : MusicaDaNoite;
    }

    // Junta os inputs de menu dos dois jogadores (qualquer um controla a pausa).
    static InputState CombinaMenu(InputState a, InputState b)
    {
        InputState r = new InputState();
        r.MenuUp       = a.MenuUp       || b.MenuUp;
        r.MenuDown     = a.MenuDown     || b.MenuDown;
        r.MenuEsquerda = a.MenuEsquerda || b.MenuEsquerda;
        r.MenuDireita  = a.MenuDireita  || b.MenuDireita;
        r.MenuConfirm  = a.MenuConfirm  || b.MenuConfirm;
        r.MenuCancel   = a.MenuCancel   || b.MenuCancel;
        return r;
    }

    static void DesenhaAviso(string texto, int linha = 0)
    {
        int tam = 24;
        int larguraTexto = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto,
            (LarguraTela - larguraTexto) / 2,
            AlturaTela - 60 - linha * 30,
            tam, Color.White);
    }

    // Qualquer controle conectado (P1 ou P2) confirmando com o botao de baixo.
    static bool AlgumControleConfirmou()
    {
        for (int i = 0; i < 2; i++)
        {
            if (Raylib.IsGamepadAvailable(i) &&
                Raylib.IsGamepadButtonPressed(i, GamepadButton.RightFaceDown))
                return true;
        }
        return false;
    }

    // Menor distancia entre um ponto e os jogadores vivos (se todos estiverem
    // fantasmas, considera todos).
    static float DistanciaAoJogadorMaisProximo(Jogador[] jogadores, Vector2 ponto)
    {
        float melhor = float.MaxValue;
        bool algumVivo = false;
        foreach (var j in jogadores) if (!j.EhFantasma) { algumVivo = true; break; }

        foreach (var j in jogadores)
        {
            if (algumVivo && j.EhFantasma) continue;
            float d = Vector2.Distance(ponto, j.Centro());
            if (d < melhor) melhor = d;
        }
        return melhor;
    }

    // Escolhe qual jogador "representa" o evento do telefone neste frame.
    static int JogadorDoEvento(Jogador[] jogadores, InputState[] inputs, EventoTelefone evento)
    {
        Vector2 centroTelefone = evento.PosicaoTelefone + new Vector2(8f, 16f);
        int maisProximo = -1;
        float melhorDist = float.MaxValue;

        for (int ji = 0; ji < jogadores.Length; ji++)
        {
            if (jogadores[ji].EhFantasma) continue;

            Vector2 c = jogadores[ji].Centro();
            if (inputs[ji].InteractPressed && evento.JogadorPertoDoTelefone(c))
                return ji;

            float d = Vector2.Distance(c, centroTelefone);
            if (d < melhorDist) { melhorDist = d; maisProximo = ji; }
        }

        return maisProximo >= 0 ? maisProximo : 0;
    }
}