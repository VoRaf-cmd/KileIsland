using System;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum FaseEventoTelefone
{
    Inativo,
    FocoCamera,
    ApareceTelefone,
    RetornaCamera,
    Tocando,
    Dialogo,
    FadeArena,
    RevelaArena,
    Batalha
}

public sealed class EventoTelefone
{
    public static int DiaDoEvento = 5;

    public const float AlcanceAtender = 150f;

    // Escala visual do sprite 16x32.
    private const float EscalaSprite = 6f;

    private const float DuracaoFocoCamera = 2f;
    private const float DuracaoAparicao = 1.2f;
    private const float DuracaoRetornoCamera = 1.8f;
    private const float DuracaoFadeArena = 1.1f;
    private const float IntervaloToqueTelefone = 2.4f;

    private const string CaminhoSprite =
        "assets/sprites/telefone.png";

    private const string CaminhoSomTelefone =
        "assets/audio/telefone/trim.wav";

    private static readonly string[] CaminhosSomAparecer =
    {
        "assets/audio/telefone/aparecer.wav",
        "assets/audio/telefone/aparecer.mp3"
    };

    private static Sound somAparecer;
    private static Sound somTelefone;

    private static bool temSomAparecer;
    private static bool temSomTelefone;
    private static bool audioCarregado;

    private static Texture2D spriteTelefone;
    private static bool spriteCarregado;

    private readonly string[] falas =
    {
        "[Escreva aqui o dialogo do telefonema]"
    };

    private FaseEventoTelefone fase =
        FaseEventoTelefone.Inativo;

    private float tempoFase;
    private float tempoToque;
    private int indiceFala;

    private Vector2 cameraInicio;
    private float zoomInicial;

    private bool pedidoInicioBatalha;

    public FaseEventoTelefone Fase => fase;

    public bool Ativo =>
        fase != FaseEventoTelefone.Inativo;

    public bool BatalhaAtiva =>
        fase == FaseEventoTelefone.Batalha;

    public bool ArenaAtiva =>
        fase is
            FaseEventoTelefone.RevelaArena or
            FaseEventoTelefone.Batalha;

    public bool ControlaCamera =>
        fase is
            FaseEventoTelefone.FocoCamera or
            FaseEventoTelefone.ApareceTelefone or
            FaseEventoTelefone.RetornaCamera;

    public bool BloqueiaJogador =>
        fase is
            FaseEventoTelefone.FocoCamera or
            FaseEventoTelefone.ApareceTelefone or
            FaseEventoTelefone.RetornaCamera or
            FaseEventoTelefone.Dialogo or
            FaseEventoTelefone.FadeArena or
            FaseEventoTelefone.RevelaArena;

    public bool TelefoneVisivel =>
        fase is
            FaseEventoTelefone.ApareceTelefone or
            FaseEventoTelefone.RetornaCamera or
            FaseEventoTelefone.Tocando or
            FaseEventoTelefone.Dialogo or
            FaseEventoTelefone.FadeArena;

    // Posição do canto superior esquerdo do sprite.
    public Vector2 PosicaoTelefone =>
        CenaMundo.CentroIlha + new Vector2(-52f, 50f);

    // ============================================================
    // CARREGAMENTO
    // ============================================================

    public static void CarregaRecursos()
    {
        CarregaAudio();
        CarregaSprite();
    }

    public static void CarregaAudio()
    {
        if (audioCarregado)
            return;

        audioCarregado = true;

        if (File.Exists(CaminhoSomTelefone))
        {
            somTelefone =
                AudioManager.RegistraEfeito(
                    CaminhoSomTelefone);

            temSomTelefone = true;
        }

        foreach (string caminho in CaminhosSomAparecer)
        {
            if (!File.Exists(caminho))
                continue;

            somAparecer =
                AudioManager.RegistraEfeito(caminho);

            temSomAparecer = true;
            break;
        }
    }

    public static void CarregaSprite()
    {
        if (spriteCarregado)
            return;

        if (!File.Exists(CaminhoSprite))
        {
            Console.WriteLine(
                $"[EventoTelefone] Sprite não encontrado: {CaminhoSprite}");

            return;
        }

        spriteTelefone =
            Raylib.LoadTexture(CaminhoSprite);

        spriteCarregado =
            spriteTelefone.Id != 0;

        if (!spriteCarregado)
        {
            Console.WriteLine(
                "[EventoTelefone] Não foi possível carregar o sprite.");
        }
    }

    // ============================================================
    // EVENTO
    // ============================================================

    public void Inicia(
        Vector2 alvoCameraAtual,
        float zoomAtual)
    {
        if (fase != FaseEventoTelefone.Inativo)
            return;

        cameraInicio = alvoCameraAtual;
        zoomInicial = zoomAtual;

        tempoFase = 0f;
        tempoToque = 0f;
        indiceFala = 0;
        pedidoInicioBatalha = false;

        fase = FaseEventoTelefone.FocoCamera;
    }

    public void Atualiza(
        float delta,
        InputState input,
        Vector2 jogadorCentro,
        ref Camera2D camera,
        Vector2? centroCamera = null)
    {
        if (
            fase == FaseEventoTelefone.Inativo ||
            fase == FaseEventoTelefone.Batalha)
        {
            return;
        }

        tempoFase += delta;

        switch (fase)
        {
            case FaseEventoTelefone.FocoCamera:
                AtualizaFocoCamera(ref camera);
                break;

            case FaseEventoTelefone.ApareceTelefone:
                AtualizaAparecimento();
                break;

            case FaseEventoTelefone.RetornaCamera:
                // No coop a camera volta para o meio dos jogadores, nao para um so.
                AtualizaRetornoCamera(
                    centroCamera ?? jogadorCentro,
                    ref camera);
                break;

            case FaseEventoTelefone.Tocando:
                AtualizaTelefoneTocando(
                    delta,
                    input,
                    jogadorCentro);
                break;

            case FaseEventoTelefone.Dialogo:
                AtualizaDialogo(input);
                break;

            case FaseEventoTelefone.FadeArena:
                AtualizaFadeArena();
                break;

            case FaseEventoTelefone.RevelaArena:
                AtualizaRevelaArena();
                break;
        }
    }

    private void AtualizaFocoCamera(
        ref Camera2D camera)
    {
        float progresso =
            Suaviza(
                tempoFase / DuracaoFocoCamera);

        camera.Target =
            Vector2.Lerp(
                cameraInicio,
                CenaMundo.CentroIlha,
                progresso);

        camera.Zoom =
            Lerp(
                zoomInicial,
                zoomInicial * 1.2f,
                progresso);

        if (tempoFase < DuracaoFocoCamera)
            return;

        camera.Target =
            CenaMundo.CentroIlha;

        camera.Zoom =
            zoomInicial * 1.2f;

        MudaFase(
            FaseEventoTelefone.ApareceTelefone);

        if (temSomAparecer)
            AudioManager.TocaEfeito(somAparecer);
    }

    private void AtualizaAparecimento()
    {
        if (tempoFase >= DuracaoAparicao)
            MudaFase(
                FaseEventoTelefone.RetornaCamera);
    }

    private void AtualizaRetornoCamera(
        Vector2 jogadorCentro,
        ref Camera2D camera)
    {
        float progresso =
            Suaviza(
                tempoFase / DuracaoRetornoCamera);

        camera.Target =
            Vector2.Lerp(
                CenaMundo.CentroIlha,
                jogadorCentro,
                progresso);

        camera.Zoom =
            Lerp(
                zoomInicial * 1.2f,
                zoomInicial,
                progresso);

        if (tempoFase < DuracaoRetornoCamera)
            return;

        camera.Target = jogadorCentro;
        camera.Zoom = zoomInicial;

        MudaFase(
            FaseEventoTelefone.Tocando);

        tempoToque =
            IntervaloToqueTelefone;

        TocaTelefone();
    }

    private void AtualizaTelefoneTocando(
        float delta,
        InputState input,
        Vector2 jogadorCentro)
    {
        tempoToque -= delta;

        if (tempoToque <= 0f)
        {
            TocaTelefone();
            tempoToque =
                IntervaloToqueTelefone;
        }

        if (
            JogadorPertoDoTelefone(jogadorCentro) &&
            input.InteractPressed)
        {
            indiceFala = 0;

            MudaFase(
                FaseEventoTelefone.Dialogo);
        }
    }

    private void AtualizaDialogo(
        InputState input)
    {
        if (!input.InteractPressed)
            return;

        indiceFala++;

        if (indiceFala >= falas.Length)
        {
            MudaFase(
                FaseEventoTelefone.FadeArena);
        }
    }

    private void AtualizaFadeArena()
    {
        if (tempoFase < DuracaoFadeArena)
            return;

        MudaFase(
            FaseEventoTelefone.RevelaArena);

        pedidoInicioBatalha = true;
    }

    private void AtualizaRevelaArena()
    {
        if (tempoFase >= DuracaoFadeArena)
        {
            MudaFase(
                FaseEventoTelefone.Batalha);
        }
    }

    // ============================================================
    // BATALHA
    // ============================================================

    public bool ConsomePedidoInicioBatalha()
    {
        if (!pedidoInicioBatalha)
            return false;

        pedidoInicioBatalha = false;

        return true;
    }

    // ============================================================
    // TELEFONE
    // ============================================================

    public bool JogadorPertoDoTelefone(
        Vector2 jogadorCentro)
    {
        // Sprite original: 16x32.
        // Centro lógico do sprite.
        Vector2 centroTelefone =
            PosicaoTelefone +
            new Vector2(8f, 16f);

        return Vector2.Distance(
            jogadorCentro,
            centroTelefone) <= AlcanceAtender;
    }

    public float OpacidadeTelefone
    {
        get
        {
            if (
                fase ==
                FaseEventoTelefone.ApareceTelefone)
            {
                return Math.Clamp(
                    tempoFase / DuracaoAparicao,
                    0f,
                    1f);
            }

            return TelefoneVisivel
                ? 1f
                : 0f;
        }
    }

    public void DesenhaTelefone()
    {
        if (!TelefoneVisivel)
            return;

        if (!spriteCarregado)
            return;

        byte alpha =
            (byte)(
                OpacidadeTelefone *
                255f);

        Color cor =
            new Color(
                (byte)255,
                (byte)255,
                (byte)255,
                alpha);

        // Tamanho original do sprite:
        // 16 x 32
        //
        // Escala:
        // 6x
        //
        // Tamanho final:
        // 96 x 192

        float largura =
            spriteTelefone.Width *
            EscalaSprite;

        float altura =
            spriteTelefone.Height *
            EscalaSprite;

        Rectangle origem =
            new Rectangle(
                0f,
                0f,
                spriteTelefone.Width,
                spriteTelefone.Height);

        Rectangle destino =
            new Rectangle(
                PosicaoTelefone.X,
                PosicaoTelefone.Y,
                largura,
                altura);

        Raylib.DrawTexturePro(
            spriteTelefone,
            origem,
            destino,
            Vector2.Zero,
            0f,
            cor);
    }

    // ============================================================
    // INTERFACE
    // ============================================================

    public float OpacidadeTelaPreta
    {
        get
        {
            if (
                fase ==
                FaseEventoTelefone.FadeArena)
            {
                return Math.Clamp(
                    tempoFase / DuracaoFadeArena,
                    0f,
                    1f);
            }

            if (
                fase ==
                FaseEventoTelefone.RevelaArena)
            {
                return 1f -
                    Math.Clamp(
                        tempoFase / DuracaoFadeArena,
                        0f,
                        1f);
            }

            return 0f;
        }
    }

    public void DesenhaInterface(
        Vector2 jogadorCentro,
        string teclaInteragir = "E")
    {
        if (
            fase ==
            FaseEventoTelefone.Tocando &&
            JogadorPertoDoTelefone(jogadorCentro))
        {
            DesenhaTextoCentral(
                $"Pressione {teclaInteragir} para atender",
                Program.AlturaTela - 54,
                22,
                Color.White);
        }
        else if (
            fase ==
            FaseEventoTelefone.Dialogo)
        {
            Raylib.DrawRectangle(
                32,
                Program.AlturaTela - 142,
                Program.LarguraTela - 64,
                118,
                new Color(
                    0,
                    0,
                    0,
                    220));

            Raylib.DrawRectangleLines(
                32,
                Program.AlturaTela - 142,
                Program.LarguraTela - 64,
                118,
                new Color(
                    220,
                    220,
                    220,
                    255));

            Raylib.DrawText(
                falas[indiceFala],
                52,
                Program.AlturaTela - 116,
                20,
                Color.White);

            Raylib.DrawText(
                $"{teclaInteragir}: continuar",
                Program.LarguraTela - 170,
                Program.AlturaTela - 54,
                16,
                new Color(
                    200,
                    200,
                    200,
                    255));
        }

        float opacidade =
            OpacidadeTelaPreta;

        if (opacidade <= 0f)
            return;

        byte alpha =
            (byte)(
                opacidade *
                255f);

        Raylib.DrawRectangle(
            0,
            0,
            Program.LarguraTela,
            Program.AlturaTela,
            new Color(
                (byte)0,
                (byte)0,
                (byte)0,
                alpha));
    }

    // ============================================================
    // UTILITÁRIOS
    // ============================================================

    private void MudaFase(
        FaseEventoTelefone novaFase)
    {
        fase = novaFase;
        tempoFase = 0f;
    }

    private static void TocaTelefone()
    {
        if (temSomTelefone)
            AudioManager.TocaEfeito(
                somTelefone);
    }

    private static float Suaviza(
        float valor)
    {
        float t =
            Math.Clamp(
                valor,
                0f,
                1f);

        return t * t * (3f - 2f * t);
    }

    private static float Lerp(
        float inicio,
        float fim,
        float progresso)
    {
        return inicio +
            (fim - inicio) *
            progresso;
    }

    private static void DesenhaTextoCentral(
        string texto,
        int y,
        int tamanho,
        Color cor)
    {
        int largura =
            Raylib.MeasureText(
                texto,
                tamanho);

        Raylib.DrawText(
            texto,
            (Program.LarguraTela - largura) / 2,
            y,
            tamanho,
            cor);
    }
}