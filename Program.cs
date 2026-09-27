using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class Program
{
    public const int LarguraTela = 1024;
    public const int AlturaTela = 576;

    public const float DistanciaInteracao = 300f;
    public const float DistanciaMinerar = 150f;
    public const float DistanciaAtaque = 150f;
    public const float EspacamentoMinerio = 400f;
    public const float DistanciaDeEstruturas = 400f;
    public const int MaxMineriosNoMapa = 12;
    public const float TempoEntreSpawns = 12f;
    public const int PicaretadasParaQuebrar = 3;

    public const int XpPorZumbi = 10;

    public static bool ForjaAberta = false;
    public static bool InventarioAberto = false;
    public static float TempoDesdeAbrirMenu = 0f;

    // Pra ordenação por Y: cada "coisa desenhável" tem base Y (o pé) e uma Action pra se desenhar.
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

        ConfiguracoesJogo.Carrega();
        AudioManager.Inicia();
        // Se quiser uma música de menu, chame aqui, por ex:
        // AudioManager.TocaMusica("assets/audio/menu.ogg");

        // ================= MENU INICIAL =================
        MenuUI.ReiniciaEstado();

        while (!Raylib.WindowShouldClose() && !MenuUI.JogoDeveIniciar)
        {
            InputState menuInput = Input.Read();
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

        // Se quiser trocar pra música de gameplay ao começar a jogar:
        // AudioManager.TocaMusica("assets/audio/gameplay.ogg");

        // ================= SETUP DO JOGO =================
        Jogador jogador = new Jogador();
        jogador.CarregaSprites();
        Jogador.CarregaPicareta();
        Jogador.CarregaEspadas();
        HUD.Carrega();
        CenaMundo.Carrega();
        Zumbi.CarregaSprites();

        Vector2 posInicial = new Vector2(
            CenaMundo.LarguraMapa / 2f - jogador.TamanhoVisual / 2f,
            CenaMundo.AlturaMapa / 2f - jogador.TamanhoVisual / 2f
        );
        jogador.Pos = posInicial;

        List<ObjetoMapa> objetos = new List<ObjetoMapa>();

        // A colisão ocupa a base para permitir que o Kile passe por trás.
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

        for (int i = 0; i < 10; i++)
            TentaSpawnarMinerio(minerios, rng, objetos);

        // Se veio de "Continuar" com um save existente, aplica o progresso salvo por cima do estado inicial.
        if (MenuUI.SlotAtual >= 0 && !MenuUI.NovoJogo)
        {
            DadosSave dados = SaveSystem.Carrega(MenuUI.SlotAtual);
            if (dados != null)
            {
                SaveSystem.Aplica(dados, jogador);

                if (EstadoJogo.EhDia)
                    CicloDiaNoite.ForcaDia();
            }
        }

        Camera2D camera = new Camera2D();
        camera.Offset = new Vector2(LarguraTela / 2f, AlturaTela / 2f);
        camera.Zoom = 0.75f;
        camera.Rotation = 0f;
        camera.Target = jogador.Centro();

        List<CoisaDesenhavel> coisas = new List<CoisaDesenhavel>();

        // ================= LOOP DO JOGO =================
        while (!Raylib.WindowShouldClose())
        {
            float delta = Raylib.GetFrameTime();
            bool dormindo = OverlaySono.Dormindo;
            bool menuAberto = ForjaAberta || InventarioAberto;
            bool travado = dormindo || menuAberto || GameOverUI.Ativo;

            InputState input = Input.Read();
            AudioManager.AtualizaMusica();

            OverlaySono.Atualiza(delta);
            LevelUpAviso.Atualiza(delta);

            if (GameOverUI.Ativo)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.R) ||
                    (Raylib.IsGamepadAvailable(0) &&
                     Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown)))
                {
                    EstadoJogo.Resetar();
                    jogador.Pos = posInicial;
                    jogador.VidaPontosReset();
                    zumbis.Clear();
                    drops.Clear();
                    minerios.Clear();
                    for (int i = 0; i < 10; i++)
                        TentaSpawnarMinerio(minerios, rng, objetos);

                    CicloDiaNoite.ForcaDia();
                    GameOverUI.Ativo = false;
                    tempoSpawnZumbi = 0f;
                    tempoSpawn = 0f;
                }
            }

            if (menuAberto) TempoDesdeAbrirMenu += delta;
            else TempoDesdeAbrirMenu = 0f;

            Vector2 inputDelta = jogador.LeInput(input, travado);
            float tv = jogador.TamanhoVisual;

            Vector2 tentativaX = new Vector2(jogador.Pos.X + inputDelta.X, jogador.Pos.Y);
            if (!Colide(tentativaX, tv, objetos))
                jogador.Pos = new Vector2(tentativaX.X, jogador.Pos.Y);

            Vector2 tentativaY = new Vector2(jogador.Pos.X, jogador.Pos.Y + inputDelta.Y);
            if (!Colide(tentativaY, tv, objetos))
                jogador.Pos = new Vector2(jogador.Pos.X, tentativaY.Y);

            jogador.AplicaKnockback(delta);

            jogador.Pos = new Vector2(
                Math.Clamp(jogador.Pos.X, CenaMundo.PosIlha.X,
                    CenaMundo.PosIlha.X + CenaMundo.GramaLargura - tv),
                Math.Clamp(jogador.Pos.Y, CenaMundo.PosIlha.Y,
                    CenaMundo.PosIlha.Y + CenaMundo.GramaAltura - tv)
            );

            Vector2 centroKile = jogador.Centro();
            bool pertoDaCasa  = Vector2.Distance(centroKile, casa.Centro())  < DistanciaInteracao;
            bool pertoDaForja = Vector2.Distance(centroKile, forja.Centro()) < DistanciaInteracao;

            if (!travado && input.InteractPressed)
            {
                if (pertoDaForja)
                {
                    ForjaAberta = true;
                    ForjaUI.ResetarSelecao();
                    TempoDesdeAbrirMenu = 0f;
                }
                else if (pertoDaCasa)
                {
                    OverlaySono.Inicia();
                    Efeitos.Shake(2f, 0.3f);
                }
            }

            if (input.InventoryPressed && !dormindo && !ForjaAberta && !GameOverUI.Ativo)
            {
                if (InventarioAberto) InventarioAberto = false;
                else
                {
                    InventarioAberto = true;
                    InventarioUI.ResetarSelecao();
                    TempoDesdeAbrirMenu = 0f;
                }
            }

            if (input.SwapItemPressed && !travado)
            {
                EstadoJogo.ItemAtual = EstadoJogo.ItemAtual == ItemEquipado.Espada
                    ? ItemEquipado.Picareta
                    : ItemEquipado.Espada;
            }

            bool podeInteragirMenu = TempoDesdeAbrirMenu > 0.2f;

            if (podeInteragirMenu && input.ClosePressed && menuAberto)
            {
                ForjaAberta = false;
                InventarioAberto = false;
            }

            if (!travado && !jogador.Batendo
                && (input.MouseClickPressed || input.AttackPressed))
            {
                if (EstadoJogo.ItemAtual == ItemEquipado.Picareta)
                {
                    Minerio alvo = MinerioMaisProximo(minerios, centroKile, DistanciaMinerar);
                    if (alvo != null)
                    {
                        alvo.PicaretadasRestantes--;
                        alvo.TempoTremor = 0.15f;
                        alvo.IniciaAnimacao();
                        jogador.IniciaBatida();

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
                    jogador.IniciaBatida();
                    Zumbi alvo = ZumbiMaisProximo(zumbis, centroKile, DistanciaAtaque);
                    if (alvo != null)
                    {
                        int indiceEspada = EstadoJogo.EspadaEquipada;
                        int dano = indiceEspada >= 0
                                   && indiceEspada < EstadoJogo.Espadas.Count
                                   && indiceEspada < EstadoJogo.EspadasCompradas.Length
                                   && EstadoJogo.EspadasCompradas[indiceEspada]
                            ? EstadoJogo.Espadas[indiceEspada].Dano
                            : 1;
                        bool morreu = alvo.TomaDano(dano);

                        if (morreu)
                        {
                            EstadoJogo.GanhaXp(XpPorZumbi);
                            Efeitos.Shake(8f, 0.4f);
                        }
                        else Efeitos.Shake(4f, 0.2f);
                    }
                }
            }

            Rectangle rectKile = new Rectangle(jogador.Pos.X, jogador.Pos.Y, tv, tv);
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                if (Raylib.CheckCollisionRecs(rectKile, drops[i].Retangulo()))
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
            CicloDiaNoite.Atualiza(delta);

            if (!eraDia && EstadoJogo.EhDia)
            {
                EstadoJogo.NoitesSobrevividas++;

                // Auto-save ao sobreviver uma noite (se veio de um slot de save).
                if (MenuUI.SlotAtual >= 0)
                    SaveSystem.Salva(MenuUI.SlotAtual, jogador);
            }

            if (!EstadoJogo.EhDia)
            {
                tempoSpawnZumbi += delta;
                float intervalo = 3f - EstadoJogo.NoitesSobrevividas * 0.3f;
                if (intervalo < 1f) intervalo = 1f;

                if (tempoSpawnZumbi >= intervalo)
                {
                    tempoSpawnZumbi = 0f;
                    Vector2 pos = PosAleatoriaZumbi(rng, jogador.Centro(), navegacaoZumbi);

                    int forcaNoite = EstadoJogo.NoitesSobrevividas;

                    Zumbi z = new Zumbi();
                    z.Pos = pos;
                    z.Vida = EstadoJogo.Espadas[0].Dano * (forcaNoite + 1);
                    z.DanoContato = 1 + forcaNoite / 3;
                    z.MultiplicadorVelocidade = 1f + Math.Min(forcaNoite * 0.05f, 0.6f);
                    zumbis.Add(z);
                }
            }
            else tempoSpawnZumbi = 0f;

            jogador.Atualiza(delta);
            foreach (var m in minerios) m.Atualiza(delta);
            foreach (var d in drops) d.Atualiza(delta);
            Efeitos.Atualiza(delta);

            float velZumbiBase = Jogador.Velocidade * 0.72f;

            for (int i = zumbis.Count - 1; i >= 0; i--)
            {
                var z = zumbis[i];
                z.Atualiza(delta, jogador.Centro(), velZumbiBase, navegacaoZumbi, EstadoJogo.EhDia);

                if (z.EstaMorto())
                {
                    zumbis.RemoveAt(i);
                    continue;
                }

                bool podeMachucar = z.Estado == EstadoZumbi.Andando || z.Estado == EstadoZumbi.Parado;

                if (podeMachucar && Raylib.CheckCollisionRecs(jogador.Retangulo(), z.Retangulo()))
                    jogador.TomaDano(z.Centro(), z.DanoContato);
            }

            if (EstadoJogo.VidaPontos <= 0 && !GameOverUI.Ativo)
            {
                GameOverUI.Inicia();
                Efeitos.Shake(15f, 0.6f);
            }

            if (EstadoJogo.EhDia)
            {
                tempoSpawn += delta;
                if (tempoSpawn >= TempoEntreSpawns && minerios.Count < MaxMineriosNoMapa)
                {
                    tempoSpawn = 0f;
                    TentaSpawnarMinerio(minerios, rng, objetos);
                }
            }

            Vector2 alvoCamera = jogador.Centro() + Efeitos.OffsetShake();
            camera.Target = Vector2.Lerp(camera.Target, alvoCamera, 0.05f);

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

            coisas.Add(new CoisaDesenhavel
            {
                BaseY = jogador.Pos.Y + jogador.TamanhoVisual,
                Desenha = () =>
                {
                    jogador.Desenha();
                    jogador.DesenhaArma();
                }
            });

            coisas.Sort((a, b) => a.BaseY.CompareTo(b.BaseY));

            foreach (var c in coisas) c.Desenha();

            Raylib.EndMode2D();

            CicloDiaNoite.Desenha();
            HUD.Desenha();

            if (!travado && !dormindo)
            {
                if (pertoDaForja) DesenhaAviso("Pressione E para abrir a forja");
                else if (pertoDaCasa) DesenhaAviso("Pressione E para dormir");
            }

            if (ForjaAberta) ForjaUI.Desenha(input, podeInteragirMenu);
            if (InventarioAberto) InventarioUI.Desenha(input, podeInteragirMenu);

            OverlaySono.Desenha();
            GameOverUI.Desenha();

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
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
        Random rng, Vector2 pertoDe, NavegacaoZumbi navegacao)
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
            if (Vector2.Distance(pos, pertoDe) > 600f) return pos;
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

    static void DesenhaAviso(string texto)
    {
        int tam = 24;
        int larguraTexto = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto,
            (LarguraTela - larguraTexto) / 2,
            AlturaTela - 60,
            tam, Color.White);
    }
}