using System;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum EstadoBoss
{
    CaindoSpawn,
    VooNormal,
    SubindoCeu,
    MarcandoX,
    TravaX,
    CaindoStomp,
    InvocandoZumbis,
    Vulneravel,
    Morto
}

public sealed class Jerisvaldo
{
    public const int TamanhoArte = 48;
    public const int Escala = 6;
    public const int QuantidadeQuadros = 9;
    public const float Velocidade = 145f;
    public const int HitboxXArte = 10;
    public const int HitboxYArte = 8;
    public const int HitboxLarguraArte = 28;
    public const int HitboxAlturaArte = 34;

    private static Texture2D texturaMarcaX;
    private static bool tentouCarregarMarcaX;
    private static Texture2D texturaBoss;
    private static bool tentouCarregarBoss;

    public Vector2 Pos { get; private set; }
    public float TamanhoVisual => TamanhoArte * Escala;

    // Propriedades para cálculo de distância da espada (usadas pelo jogador)
    public Vector2 Centro => Pos + new Vector2(TamanhoVisual / 2f);
    public Rectangle Hitbox => new Rectangle(
        Pos.X + HitboxXArte * Escala,
        Pos.Y + HitboxYArte * Escala,
        HitboxLarguraArte * Escala,
        HitboxAlturaArte * Escala);

    // Vida e Status
    public int VidaMaxima { get; private set; } = 500;
    public int VidaAtual { get; private set; }
    public bool EstaVivo => VidaAtual > 0;
    public bool EstaVulneravel => estadoAtual == EstadoBoss.Vulneravel;

    // Máquina de Estados e Tempos
    private EstadoBoss estadoAtual = EstadoBoss.CaindoSpawn;
    private float temporizadorEstado;
    private float tempoPiscaDano;

    // Mecânica do X e Impacto
    private Vector2 posicaoX;
    private Vector2 posicaoPouso;
    private bool danoImpactoPendente;
    public float RaioDanoArea => 240f;
    public int DanoImpacto => 3;
    public Vector2 PosicaoImpacto => posicaoX;

    // Animação e Movimento
    private Vector2 alvoVoo;
    private float tempoNovoAlvo;
    private float tempoVoo;
    private int quadroAnimacao = 0;
    private float tempoQuadroAnim = 0f;

    // Efeitos de Tela (screen shake e flash)
    public float IntensidadeShake { get; private set; }
    public float OpacidadeFlash { get; private set; }

    // Invocação de Zumbis
    public bool DeveSpawnarZumbis { get; private set; }
    public float AlfaInvocacaoZumbis { get; private set; }
    public Vector2[] PosicoesZumbis { get; private set; } = Array.Empty<Vector2>();
    private int ondasInvocacao;

    public Jerisvaldo(Vector2 centroInicial)
    {
        VidaAtual = VidaMaxima;
        posicaoPouso = centroInicial - new Vector2(TamanhoVisual / 2f);
        Pos = new Vector2(posicaoPouso.X, posicaoPouso.Y - 800f);
        alvoVoo = posicaoPouso;
    }

    public static void CarregaSprite()
    {
        if (tentouCarregarBoss) return;
        tentouCarregarBoss = true;

        const string caminho = "Assets/sprites/boss/jerisvaldo.png";
        if (System.IO.File.Exists(caminho))
            texturaBoss = Raylib.LoadTexture(caminho);
    }

    public static void CarregaSpriteMarcaX()
    {
        if (tentouCarregarMarcaX) return;
        tentouCarregarMarcaX = true;

        const string caminho = "Assets/sprites/boss/x.png";
        if (System.IO.File.Exists(caminho))
            texturaMarcaX = Raylib.LoadTexture(caminho);
    }

    // Chamada idêntica ao Zumbi.cs quando a espada acerta o boss
    public bool TomaDano(int dano)
    {
        if (!EstaVivo || !EstaVulneravel) return false;

        VidaAtual = Math.Max(0, VidaAtual - dano);
        tempoPiscaDano = 0.15f;
        AtivarImpacto(shake: 6f, flash: 0.12f);

        if (VidaAtual <= 0)
        {
            estadoAtual = EstadoBoss.Morto;
            AtivarImpacto(shake: 20f, flash: 1.0f);
        }

        return true;
    }

    public bool EstaNoAlcance(Vector2 ponto, float alcance)
    {
        Rectangle hitbox = Hitbox;
        float x = Math.Clamp(ponto.X, hitbox.X, hitbox.X + hitbox.Width);
        float y = Math.Clamp(ponto.Y, hitbox.Y, hitbox.Y + hitbox.Height);
        return Vector2.Distance(ponto, new Vector2(x, y)) <= alcance;
    }

    public bool ConsomeDanoImpacto()
    {
        if (!danoImpactoPendente) return false;
        danoImpactoPendente = false;
        return true;
    }

    public void Atualiza(float delta, Vector2 jogadorCentro, Rectangle limites)
    {
        // Decaimento suave de Shake, Flash e do pisca de Dano
        if (IntensidadeShake > 0) IntensidadeShake = MathF.Max(0, IntensidadeShake - delta * 35f);
        if (OpacidadeFlash > 0) OpacidadeFlash = MathF.Max(0, OpacidadeFlash - delta * 2.5f);
        if (tempoPiscaDano > 0) tempoPiscaDano -= delta;

        if (!EstaVivo) return;

        tempoVoo += delta;

        // Animação de quadros
        AtualizaAnimacao(delta);

        switch (estadoAtual)
        {
            case EstadoBoss.CaindoSpawn:
                Pos = new Vector2(Pos.X, Pos.Y + 1200f * delta);
                if (Pos.Y >= posicaoPouso.Y)
                {
                    Pos = posicaoPouso;
                    AtivarImpacto(shake: 18f, flash: 0.5f);
                    MudarEstado(EstadoBoss.VooNormal, 4.5f);
                }
                break;

            case EstadoBoss.VooNormal:
                AtualizaVoo(delta, jogadorCentro, limites);
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    MudarEstado(EstadoBoss.SubindoCeu, 1.0f);
                }
                break;

            case EstadoBoss.SubindoCeu:
                Pos = new Vector2(Pos.X, Pos.Y - 1100f * delta);
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    MudarEstado(EstadoBoss.MarcandoX, 2.5f);
                }
                break;

            case EstadoBoss.MarcandoX:
                posicaoX = jogadorCentro;
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    MudarEstado(EstadoBoss.TravaX, 3.0f); // Trava o X no chão por 3 segundos
                }
                break;

            case EstadoBoss.TravaX:
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    Pos = posicaoX - new Vector2(TamanhoVisual / 2f, 950f);
                    MudarEstado(EstadoBoss.CaindoStomp, 0.4f);
                }
                break;

            case EstadoBoss.CaindoStomp:
                Pos = new Vector2(Pos.X, Pos.Y + 2400f * delta);
                if (Pos.Y >= posicaoX.Y - TamanhoVisual / 2f)
                {
                    Pos = posicaoX - new Vector2(TamanhoVisual / 2f);
                    AtivarImpacto(shake: 28f, flash: 0.85f);

                    ondasInvocacao++;
                    int quantidadeZumbis = ondasInvocacao == 1
                        ? 1
                        : Math.Min(8, (ondasInvocacao - 1) * 2);
                    PosicoesZumbis = new Vector2[quantidadeZumbis];

                    const float distanciaInvocacao = 420f;
                    float margemZumbi = Zumbi.TamanhoArte * Zumbi.Escala / 2f;
                    for (int i = 0; i < quantidadeZumbis; i++)
                    {
                        float angulo = MathF.Tau * i / quantidadeZumbis;
                        Vector2 centroInvocacao = posicaoX + new Vector2(
                            MathF.Cos(angulo) * distanciaInvocacao,
                            MathF.Sin(angulo) * distanciaInvocacao
                        );

                        PosicoesZumbis[i] = new Vector2(
                            Math.Clamp(centroInvocacao.X,
                                limites.X + margemZumbi,
                                limites.X + limites.Width - margemZumbi),
                            Math.Clamp(centroInvocacao.Y,
                                limites.Y + margemZumbi,
                                limites.Y + limites.Height - margemZumbi)
                        );
                    }

                    DeveSpawnarZumbis = true;
                    AlfaInvocacaoZumbis = 0f;
                    danoImpactoPendente = true;

                    MudarEstado(EstadoBoss.InvocandoZumbis, 2.0f);
                }
                break;

            case EstadoBoss.InvocandoZumbis:
                AlfaInvocacaoZumbis = Math.Clamp(AlfaInvocacaoZumbis + delta * 0.75f, 0f, 1f);
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    DeveSpawnarZumbis = false;
                    MudarEstado(EstadoBoss.Vulneravel, 5.0f); // 5s PARADÃO para o player desferir golpes!
                }
                break;

            case EstadoBoss.Vulneravel:
                // Fica imóvel tomando dano da espada
                temporizadorEstado -= delta;
                if (temporizadorEstado <= 0)
                {
                    MudarEstado(EstadoBoss.VooNormal, 4.0f);
                }
                break;
        }
    }

    private void MudarEstado(EstadoBoss novoEstado, float tempo)
    {
        estadoAtual = novoEstado;
        temporizadorEstado = tempo;
    }

    private void AtivarImpacto(float shake, float flash)
    {
        IntensidadeShake = shake;
        OpacidadeFlash = flash;
    }

    private void AtualizaVoo(float delta, Vector2 jogadorCentro, Rectangle limites)
    {
        tempoNovoAlvo -= delta;

        if (tempoNovoAlvo <= 0f || Vector2.DistanceSquared(Centro, alvoVoo) < 90f * 90f)
            EscolheAlvo(jogadorCentro, limites);

        Vector2 direcaoMovimento = alvoVoo - Centro;
        float distancia = direcaoMovimento.Length();
        if (distancia > 0.01f)
        {
            Vector2 movimento = direcaoMovimento / distancia * Math.Min(distancia, Velocidade * delta);
            Pos += movimento;
        }
    }

    private void EscolheAlvo(Vector2 jogadorCentro, Rectangle limites)
    {
        float angulo = Random.Shared.NextSingle() * MathF.Tau;
        float raioX = 330f + Random.Shared.NextSingle() * 190f;
        float raioY = 180f + Random.Shared.NextSingle() * 100f;
        alvoVoo = jogadorCentro + new Vector2(MathF.Cos(angulo) * raioX, MathF.Sin(angulo) * raioY);

        float margem = TamanhoVisual / 2f;
        alvoVoo.X = Math.Clamp(alvoVoo.X, limites.X + margem, limites.X + limites.Width - margem);
        alvoVoo.Y = Math.Clamp(alvoVoo.Y, limites.Y + margem, limites.Y + limites.Height - margem);
        tempoNovoAlvo = 2.5f + Random.Shared.NextSingle() * 1.5f;
    }

    private void AtualizaAnimacao(float delta)
    {
        tempoQuadroAnim += delta;

        switch (estadoAtual)
        {
            case EstadoBoss.VooNormal:
                if (quadroAnimacao < 0 || quadroAnimacao >= 4)
                    quadroAnimacao = 0;
                if (tempoQuadroAnim >= 0.15f)
                {
                    quadroAnimacao = (quadroAnimacao + 1) % 4;
                    tempoQuadroAnim = 0f;
                }
                break;

            case EstadoBoss.SubindoCeu:
                quadroAnimacao = 4;
                tempoQuadroAnim = 0f;
                break;

            case EstadoBoss.CaindoSpawn:
            case EstadoBoss.CaindoStomp:
                quadroAnimacao = 5;
                tempoQuadroAnim = 0f;
                break;

            case EstadoBoss.InvocandoZumbis:
                quadroAnimacao = 6;
                tempoQuadroAnim = 0f;
                break;

            case EstadoBoss.Vulneravel:
                if (quadroAnimacao < 7 || quadroAnimacao > 8)
                    quadroAnimacao = 7;
                if (tempoQuadroAnim >= 0.3f)
                {
                    quadroAnimacao = quadroAnimacao == 7 ? 8 : 7;
                    tempoQuadroAnim = 0f;
                }
                break;

            default:
                quadroAnimacao = 0;
                tempoQuadroAnim = 0f;
                break;
        }
    }

    public void Desenha()
    {
        if (!EstaVivo) return;

        // Desenha a marcação em X no chão
        if (estadoAtual == EstadoBoss.MarcandoX || estadoAtual == EstadoBoss.TravaX)
        {
            DesenharMarcaX();
        }

        // Não desenha o Boss enquanto ele estiver no alto do céu escondido
        if (estadoAtual == EstadoBoss.MarcandoX || estadoAtual == EstadoBoss.TravaX) return;

        // Flutuação sutil se estiver voando ou vulnerável
        float bob = (estadoAtual == EstadoBoss.VooNormal || estadoAtual == EstadoBoss.Vulneravel)
            ? MathF.Sin(tempoVoo * 3.5f) * 8f
            : 0f;

        Rectangle destino = new Rectangle(Pos.X, Pos.Y + bob, TamanhoVisual, TamanhoVisual);

        if (texturaBoss.Id != 0)
        {
            Rectangle origem = new Rectangle(
                quadroAnimacao * TamanhoArte,
                0,
                TamanhoArte,
                TamanhoArte);
            Color cor = tempoPiscaDano > 0f
                ? new Color((byte)255, (byte)255, (byte)255, (byte)235)
                : Color.White;

            Raylib.DrawTexturePro(
                texturaBoss,
                origem,
                destino,
                Vector2.Zero,
                0f,
                cor);

            if (tempoPiscaDano > 0f)
                Raylib.DrawTexturePro(
                    texturaBoss,
                    origem,
                    destino,
                    Vector2.Zero,
                    0f,
                    new Color((byte)255, (byte)255, (byte)255, (byte)180));
        }
        else
        {
            DesenhaPlaceholder(destino);
        }
    }

    private void DesenharMarcaX()
    {
        if (texturaMarcaX.Id != 0)
        {
            float largura = texturaMarcaX.Width * Escala;
            float altura = texturaMarcaX.Height * Escala;
            Rectangle origem = new Rectangle(
                0f, 0f, texturaMarcaX.Width, texturaMarcaX.Height);
            Rectangle destino = new Rectangle(
                posicaoX.X - largura / 2f,
                posicaoX.Y - altura / 2f,
                largura,
                altura);
            Color cor = estadoAtual == EstadoBoss.TravaX
                ? new Color((byte)255, (byte)190, (byte)190, (byte)255)
                : Color.White;

            Raylib.DrawTexturePro(
                texturaMarcaX,
                origem,
                destino,
                Vector2.Zero,
                0f,
                cor);
            return;
        }

        Color corX = estadoAtual == EstadoBoss.TravaX
            ? new Color(120, 32, 32, 230)
            : new Color(150, 82, 30, 190);
        const float tamanhoPlaceholder = 32f * Escala;
        Rectangle placeholder = new Rectangle(
            posicaoX.X - tamanhoPlaceholder / 2f,
            posicaoX.Y - tamanhoPlaceholder / 2f,
            tamanhoPlaceholder,
            tamanhoPlaceholder);

        Raylib.DrawRectangleRec(placeholder, corX);
        Raylib.DrawRectangleLinesEx(placeholder, 4f, Color.White);
        const int tamanhoFonte = 72;
        int larguraTexto = Raylib.MeasureText("X", tamanhoFonte);
        Raylib.DrawText(
            "X",
            (int)(placeholder.X + (placeholder.Width - larguraTexto) / 2f),
            (int)(placeholder.Y + (placeholder.Height - tamanhoFonte) / 2f),
            tamanhoFonte,
            Color.White);
    }

    public void DesenhaInterface()
    {
        if (!EstaVivo && OpacidadeFlash <= 0f) return;

        // Flash na Tela inteira no impacto/morte
        if (OpacidadeFlash > 0f)
        {
            Color corFlash = new Color((byte)255, (byte)255, (byte)255, (byte)(OpacidadeFlash * 255));
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), corFlash);
        }

        // Boss Bar na UI
        int larguraBarra = 360;
        int alturaBarra = 18;
        int posX = (Raylib.GetScreenWidth() - larguraBarra) / 2;
        int posY = 24;

        float percentualVida = (float)VidaAtual / VidaMaxima;

        // Borda e Fundo
        Raylib.DrawRectangle(posX - 4, posY - 4, larguraBarra + 8, alturaBarra + 8, Color.Black);
        Raylib.DrawRectangle(posX, posY, larguraBarra, alturaBarra, new Color(40, 40, 40, 255));

        // Preenchimento (Fica dourada na vulnerabilidade)
        Color corBarra = estadoAtual == EstadoBoss.Vulneravel ? Color.Gold : Color.Maroon;
        Raylib.DrawRectangle(posX, posY, (int)(larguraBarra * percentualVida), alturaBarra, corBarra);

        // Texto do Boss
        string texto = estadoAtual == EstadoBoss.Vulneravel ? "JERISVALDO (VULNERÁVEL!)" : "JERISVALDO";
        int tamanhoFonte = 16;
        int larguraTexto = Raylib.MeasureText(texto, tamanhoFonte);
        Raylib.DrawText(texto, (Raylib.GetScreenWidth() - larguraTexto) / 2, posY - 28, tamanhoFonte, Color.White);
    }

    private void DesenhaPlaceholder(Rectangle destino)
    {
        Color fundo = tempoPiscaDano > 0f
            ? Color.White
            : estadoAtual == EstadoBoss.Vulneravel
                ? new Color(126, 112, 70, 255)
                : new Color(65, 58, 68, 255);

        Raylib.DrawRectangleRec(destino, fundo);
        Raylib.DrawRectangleLinesEx(
            destino,
            6f,
            estadoAtual == EstadoBoss.Vulneravel ? Color.Gold : Color.White);

        string rotulo = estadoAtual switch
        {
            EstadoBoss.CaindoSpawn or EstadoBoss.CaindoStomp => "QUEDA",
            EstadoBoss.VooNormal => "JERISVALDO - VOO",
            EstadoBoss.SubindoCeu => "SUBINDO",
            EstadoBoss.InvocandoZumbis => "INVOCANDO",
            EstadoBoss.Vulneravel => "VULNERAVEL",
            _ => "JERISVALDO"
        };

        DesenhaTextoPlaceholder(rotulo, destino, Color.White);
    }

    private static void DesenhaTextoPlaceholder(string texto, Rectangle destino, Color cor)
    {
        int tamanhoFonte = 24;
        int larguraTexto = Raylib.MeasureText(texto, tamanhoFonte);
        Raylib.DrawText(
            texto,
            (int)(destino.X + (destino.Width - larguraTexto) / 2f),
            (int)(destino.Y + destino.Height / 2f - tamanhoFonte / 2f),
            tamanhoFonte,
            cor);
    }

}