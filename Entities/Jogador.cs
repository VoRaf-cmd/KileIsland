using System;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum Direcao { Esquerda, Direita }

public class Jogador
{
    public int Indice = 0;
    public Color Cor = Color.White;

    public int VidaPontos = 12;
    public int VidaMaxPontos = 12;

    public ItemEquipado ItemAtual = ItemEquipado.Picareta;

    public bool EhFantasma = false;

    public void VidaPontosReset()
    {
        TempoInvulneravel = 0f;
        TempoKnockback = 0f;
        TempoTremor = 0f;
        Batendo = false;
        TempoSwing = 0f;
        VelocidadeKnockback = Vector2.Zero;
    }

    public void Reviver()
    {
        EhFantasma = false;
        VidaPontos = VidaMaxPontos;
        TempoInvulneravel = 1.5f;
        TempoTremor = 0f;
        TempoKnockback = 0f;
        VelocidadeKnockback = Vector2.Zero;
        Batendo = false;
        TempoSwing = 0f;
    }

        public void Cura(int quantidade)
    {
        VidaPontos = Math.Min(VidaMaxPontos, VidaPontos + quantidade);
    }

    public void AumentaVidaMaxima(int quantidade)
    {
        VidaMaxPontos += quantidade;
        VidaPontos += quantidade;
    }

    public Vector2 Pos;
    public Direcao DirecaoAtual = Direcao.Direita;
    public bool Movendo;

    public Animacao IdleLado = null!;
    public Animacao WalkLado = null!;
    public float TempoAnimacao = 0f;
    public const float FpsAnimacao = 8f;

    public float TempoTremor = 0f;

    public float TempoSwing = 0f;
    public bool Batendo = false;
    public const float DuracaoSwing = 0.25f;

    public float TempoInvulneravel = 0f;
    public const float DuracaoInvulneravel = 1f;

    public Vector2 VelocidadeKnockback = Vector2.Zero;
    public const float DuracaoKnockback = 0.2f;
    public float TempoKnockback = 0f;

    public float TempoFlutuacao = 0f;

    public const int TamanhoArte = 16;
    public const int Escala = 6;
    public const float Velocidade = 4f;

    public float TamanhoVisual => TamanhoArte * Escala;

    static Texture2D texturaPicareta;
    static bool tentouCarregarPicareta = false;
    const int TamanhoPicaretaVisual = 96;
    static readonly string[] caminhosEspadas =
    {
        "assets/sprites/espadas/espada_madeira.png",
        "assets/sprites/espadas/espada_pedra.png",
        "assets/sprites/espadas/espada_ferro.png",
        "assets/sprites/espadas/espada_ouro.png",
        "assets/sprites/espadas/espada_lucas.png"
    };
    static Texture2D[] texturasEspadas = new Texture2D[caminhosEspadas.Length];
    static bool tentouCarregarEspadas = false;
    const int TamanhoEspadaVisual = 96;

    public Jogador(int indice = 0)
    {
        Indice = indice;
        Cor = indice == 0
            ? Color.White
            : new Color((byte)140, (byte)200, (byte)255, (byte)255);
    }

    public void CarregaSprites()
    {
        IdleLado = Animacao.Carrega("assets/sprites/kile/idle_lado.png", 16, 3);
        WalkLado = Animacao.Carrega("assets/sprites/kile/walk_lado.png", 16, 4);
    }

    public static void CarregaPicareta()
    {
        if (tentouCarregarPicareta) return;
        tentouCarregarPicareta = true;
        if (System.IO.File.Exists("assets/sprites/picareta.png"))
            texturaPicareta = Raylib.LoadTexture("assets/sprites/picareta.png");
        else
            texturaPicareta = new Texture2D();
    }

    public static void CarregaEspadas()
    {
        if (tentouCarregarEspadas) return;
        tentouCarregarEspadas = true;

        for (int i = 0; i < caminhosEspadas.Length; i++)
        {
            if (System.IO.File.Exists(caminhosEspadas[i]))
                texturasEspadas[i] = Raylib.LoadTexture(caminhosEspadas[i]);
        }
    }

    public Vector2 Centro()
    {
        return new Vector2(Pos.X + TamanhoVisual / 2f, Pos.Y + TamanhoVisual / 2f);
    }

    public Rectangle Retangulo()
    {
        return new Rectangle(Pos.X, Pos.Y, TamanhoVisual, TamanhoVisual);
    }

    public Vector2 LeInput(InputState input, bool travado)
    {
        Movendo = false;
        Vector2 delta = Vector2.Zero;

        if (travado) return delta;
        if (TempoKnockback > 0f) return delta;

        float vel = Velocidade * EstadoJogo.MultiplicadorVelocidadeCarta;

        if (MathF.Abs(input.Move.X) > 0.1f)
        {
            delta.X = input.Move.X * vel;
            DirecaoAtual = input.Move.X > 0f ? Direcao.Direita : Direcao.Esquerda;
            Movendo = true;
        }

        if (MathF.Abs(input.Move.Y) > 0.1f)
        {
            delta.Y = input.Move.Y * vel;
            Movendo = true;
        }

        return delta;
    }

    public void Atualiza(float delta)
    {
        TempoAnimacao += delta * FpsAnimacao;
        if (TempoTremor > 0f) TempoTremor -= delta;
        if (TempoInvulneravel > 0f) TempoInvulneravel -= delta;
        if (TempoKnockback > 0f) TempoKnockback -= delta;

        if (EhFantasma)
            TempoFlutuacao += delta;

        if (Batendo)
        {
            TempoSwing += delta;
            if (TempoSwing >= DuracaoSwing)
            {
                Batendo = false;
                TempoSwing = 0f;
            }
        }
    }

    public void IniciaBatida()
    {
        if (EhFantasma) return;
        Batendo = true;
        TempoSwing = 0f;
        TempoTremor = 0.1f;
    }

    public bool TomaDano(Vector2 origemDano, int dano = 1)
    {
        if (EhFantasma) return false;
        if (TempoInvulneravel > 0f) return false;

        VidaPontos = Math.Max(0, VidaPontos - dano);
        TempoInvulneravel = DuracaoInvulneravel;
        TempoTremor = 0.3f;

        // Se a origem do dano estiver exatamente no centro do jogador, Normalize()
        // devolveria NaN e o jogador sumiria do mapa.
        Vector2 diff = Centro() - origemDano;
        Vector2 dir = diff.LengthSquared() > 0.0001f
            ? Vector2.Normalize(diff)
            : new Vector2(0f, 1f);
        VelocidadeKnockback = dir * 25f;
        TempoKnockback = DuracaoKnockback;

        Efeitos.Shake(8f, 0.3f);

        if (VidaPontos <= 0)
        {
            EhFantasma = true;
            TempoFlutuacao = 0f;
            Batendo = false;
            TempoSwing = 0f;
            TempoKnockback = 0f;
            VelocidadeKnockback = Vector2.Zero;
        }

        return true;
    }

    public void AplicaKnockback(float delta)
    {
        if (TempoKnockback > 0f)
        {
            Pos += VelocidadeKnockback * delta;
            VelocidadeKnockback *= 0.9f;
        }
    }

    public void Desenha()
    {
        float tremorX = 0f;
        if (TempoTremor > 0f)
            tremorX = (float)Math.Sin(TempoTremor * 60f) * 3f;

        bool piscando = TempoInvulneravel > 0f && !EhFantasma;
        if (piscando && ((int)(TempoInvulneravel * 15f) % 2 == 0))
            return;

        Efeitos.DesenhaSombra(Pos.X + TamanhoVisual / 2f,
                              Pos.Y + TamanhoVisual,
                              TamanhoVisual * 0.7f,
                              TamanhoVisual * 0.2f);

        Animacao anim = Movendo ? WalkLado : IdleLado;
        bool espelhar = DirecaoAtual == Direcao.Esquerda;

        float flutuacaoY = 0f;
        byte alpha = 255;

        if (EhFantasma)
        {
            flutuacaoY = MathF.Sin(TempoFlutuacao * 2.5f) * 12f;
            alpha = 140;
        }

        Color corFinal = new Color(Cor.R, Cor.G, Cor.B, alpha);

        if (anim.Textura.Id != 0)
        {
            int frameAtual = (int)(TempoAnimacao) % anim.QuantidadeFrames;

            Rectangle src = new Rectangle(
                frameAtual * anim.TamanhoFrame, 0,
                anim.TamanhoFrame, anim.TamanhoFrame
            );
            if (espelhar) { src.Width = -src.Width; }

            Rectangle dest = new Rectangle(Pos.X + tremorX, Pos.Y + flutuacaoY,
                                            TamanhoVisual, TamanhoVisual);
            Raylib.DrawTexturePro(anim.Textura, src, dest, Vector2.Zero, 0f, corFinal);
        }
        else
        {
            Raylib.DrawRectangle((int)(Pos.X + tremorX), (int)(Pos.Y + flutuacaoY),
                (int)TamanhoVisual, (int)TamanhoVisual, corFinal);
        }
    }

    public void DesenhaArma()
    {
        if (EhFantasma) return;

        if (ItemAtual == ItemEquipado.Espada)
            DesenhaEspada();
        else
            DesenhaPicareta();
    }

    void DesenhaPicareta()
    {
        float progresso = Batendo ? (TempoSwing / DuracaoSwing) : 0f;
        float anguloSwing = 0f;
        if (Batendo) anguloSwing = (float)Math.Sin(progresso * Math.PI) * 70f;

        float offsetFrente = DirecaoAtual == Direcao.Direita ? TamanhoVisual * 0.05f
                                                              : -TamanhoVisual * 0.05f;
        Vector2 mao = new Vector2(
            Pos.X + TamanhoVisual / 2f + offsetFrente,
            Pos.Y + TamanhoVisual * 0.55f
        );

        if (texturaPicareta.Id != 0)
        {
            float largura = TamanhoPicaretaVisual;
            float altura  = TamanhoPicaretaVisual;
            Vector2 origem = new Vector2(0, altura / 2f);

            float direcaoSwing = DirecaoAtual == Direcao.Direita ? 1f : -1f;
            float rotacao = direcaoSwing * anguloSwing;

            Rectangle src = new Rectangle(0, 0, texturaPicareta.Width, texturaPicareta.Height);
            if (DirecaoAtual == Direcao.Esquerda)
            {
                src.Width = -src.Width;
                origem = new Vector2(largura, altura / 2f);
            }

            Rectangle dest = new Rectangle(mao.X, mao.Y, largura, altura);
            Raylib.DrawTexturePro(texturaPicareta, src, dest, origem, rotacao, Cor);
        }
        else
        {
            Raylib.DrawRectangle((int)mao.X, (int)mao.Y, 16, 16, new Color(160, 160, 160, 255));
        }
    }

    void DesenhaEspada()
    {
        int indiceEspada = EstadoJogo.EspadaEquipada;
        if (indiceEspada < 0 || indiceEspada >= texturasEspadas.Length) return;

        Texture2D textura = texturasEspadas[indiceEspada];
        if (textura.Id == 0) return;

        float progresso = Batendo ? (TempoSwing / DuracaoSwing) : 0f;
        float anguloSwing = 0f;
        if (Batendo) anguloSwing = (float)Math.Sin(progresso * Math.PI) * 70f;

        float direcao = DirecaoAtual == Direcao.Direita ? 1f : -1f;
        float offsetFrente = direcao * TamanhoVisual * 0.20f;
        Vector2 mao = new Vector2(
            Pos.X + TamanhoVisual / 2f + offsetFrente,
            Pos.Y + TamanhoVisual * 0.70f
        );

        float largura = TamanhoEspadaVisual;
        float altura = TamanhoEspadaVisual;
        Vector2 origem = direcao > 0f
            ? new Vector2(largura * 0.2f, altura * 0.8f)
            : new Vector2(largura * 0.8f, altura * 0.8f);

        Rectangle src = new Rectangle(0, 0, textura.Width, textura.Height);
        if (direcao < 0f) src.Width = -src.Width;

        Rectangle dest = new Rectangle(mao.X, mao.Y, largura, altura);
        Raylib.DrawTexturePro(textura, src, dest, origem,
                              direcao * anguloSwing, Cor);
    }
}

public class Animacao
{
    public Texture2D Textura;
    public int TamanhoFrame;
    public int QuantidadeFrames;

    public static Animacao Carrega(string caminho, int tamanhoFrame, int quantidade)
    {
        Animacao a = new Animacao();
        a.TamanhoFrame = tamanhoFrame;
        a.QuantidadeFrames = quantidade;
        a.Textura = System.IO.File.Exists(caminho)
            ? Raylib.LoadTexture(caminho)
            : new Texture2D();
        return a;
    }
}