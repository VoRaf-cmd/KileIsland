using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum EstadoZumbi
{
    Andando,
    Parado,
    Queimando,   // morte por sol (só ativa de dia)
    Morrendo,    // morte genérica (ex: espada) - mesma queda/fade, sem chamas
    Morto
}

public class Zumbi
{
    public Vector2 Pos;
    public int Vida;
    public int DanoContato = 1;
    public Direcao DirecaoAtual = Direcao.Direita;

    // Escala a dificuldade com as noites sobrevividas (definido pelo Program ao spawnar)
    public float MultiplicadorVelocidade = 1f;

    public float TempoAnimacao;
    public float TempoTremor;

    public EstadoZumbi Estado = EstadoZumbi.Andando;
    public float TempoEstado;
    public float TempoQueimando;
    public float TempoMorrendo;

    public const float DuracaoQueimando = 2f;
    public const float DuracaoMorrendo = 0.6f;

    public static Animacao WalkLado = null!;
    public static Animacao IdleLado = null!;

    static bool tentouCarregar;

    // Movimento / pathing
    List<Vector2> caminho = new();
    int indiceCaminho;
    float tempoRecalculo;
    Vector2 alvoCaminho;
    bool caminhoCalculado;

    // Patrulha (sai da âncora, para, volta pra âncora, para, sai de novo)
    Vector2 alvoPatrulhaAtual;
    bool temAlvoPatrulha;
    float tempoEsperaPatrulha;
    Vector2 ancoraPatrulha;
    bool ancoraDefinida;
    bool indoParaFora;

    bool perseguindo;

    // Animação
    bool paradoAnimacaoAnterior;

    public const int TamanhoArte = 16;
    public const int Escala = 6;
    public const float FpsWalk = 8f;
    public const float FpsIdle = 6f;

    public float TamanhoVisual => TamanhoArte * Escala;

    public static void CarregaSprites()
    {
        if (tentouCarregar)
            return;

        tentouCarregar = true;

        WalkLado = Animacao.Carrega("assets/sprites/zumbi/walk_lado.png", 16, 4);
        IdleLado = Animacao.Carrega("assets/sprites/zumbi/idle_lado.png", 16, 4);
    }

    public Vector2 Centro()
    {
        return new Vector2(Pos.X + TamanhoVisual / 2f, Pos.Y + TamanhoVisual / 2f);
    }

    public Rectangle Retangulo()
    {
        return new Rectangle(Pos.X, Pos.Y, TamanhoVisual, TamanhoVisual);
    }

    public void Atualiza(
        float delta,
        Vector2 alvo,
        float velocidade,
        NavegacaoZumbi navegacao,
        bool ehDia)
    {
        if (Estado == EstadoZumbi.Morto)
            return;

        TempoTremor = Math.Max(0f, TempoTremor - delta);

        if (Estado == EstadoZumbi.Queimando)
        {
            TempoQueimando += delta;

            if (TempoQueimando >= DuracaoQueimando)
                Estado = EstadoZumbi.Morto;

            return;
        }

        if (Estado == EstadoZumbi.Morrendo)
        {
            TempoMorrendo += delta;

            if (TempoMorrendo >= DuracaoMorrendo)
                Estado = EstadoZumbi.Morto;

            return;
        }

        // Pega fogo assim que amanhece - só de dia, nunca de noite.
        if (ehDia)
        {
            Estado = EstadoZumbi.Queimando;
            TempoQueimando = 0f;
            caminho.Clear();
            temAlvoPatrulha = false;
            caminhoCalculado = false;
            return;
        }

        float distanciaJogador = Vector2.Distance(Centro(), alvo);

        bool novoPerseguindo = perseguindo
            ? distanciaJogador <= 460f
            : distanciaJogador <= 400f;

        if (novoPerseguindo != perseguindo)
        {
            perseguindo = novoPerseguindo;
            temAlvoPatrulha = false;
            caminhoCalculado = false;
            ancoraDefinida = false; // ao voltar a patrulhar, ancora no ponto onde parou de perseguir
        }

        Vector2 destino;

        if (perseguindo)
        {
            destino = alvo;
        }
        else
        {
            if (!ancoraDefinida)
            {
                ancoraPatrulha = Pos;
                ancoraDefinida = true;
                indoParaFora = true;
            }

            if (temAlvoPatrulha &&
                Vector2.DistanceSquared(Pos, alvoPatrulhaAtual) <= 24f * 24f)
            {
                temAlvoPatrulha = false;
                caminhoCalculado = false;
                indoParaFora = !indoParaFora;
                tempoEsperaPatrulha = 0.4f + Random.Shared.NextSingle() * 0.6f;
            }

            if (!temAlvoPatrulha)
            {
                tempoEsperaPatrulha -= delta;

                if (tempoEsperaPatrulha > 0f)
                {
                    Estado = EstadoZumbi.Parado;
                    TempoEstado += delta;
                    AtualizaAnimacao(delta);
                    return;
                }

                Vector2? destinoDesejado = indoParaFora
                    ? EscolhePontoAleatorio(navegacao)
                    : ancoraPatrulha;

                bool conseguiu = destinoDesejado.HasValue &&
                    TentaCaminhoPara(destinoDesejado.Value, navegacao);

                if (!conseguiu)
                {
                    // Não trava esperando: tenta de novo rapidinho em vez de ficar parado por 1s+ sem fazer nada.
                    tempoEsperaPatrulha = 0.3f;
                    Estado = EstadoZumbi.Parado;
                    TempoEstado += delta;
                    AtualizaAnimacao(delta);
                    return;
                }
            }

            destino = alvoPatrulhaAtual;
        }

        tempoRecalculo -= delta;

        bool alvoMudou = Vector2.DistanceSquared(destino, alvoCaminho) > 48f * 48f;
        bool caminhoTerminado = indiceCaminho >= caminho.Count;

        if (!caminhoCalculado || tempoRecalculo <= 0f || alvoMudou || caminhoTerminado)
        {
            caminho = navegacao.AchaCaminho(Pos, destino);
            indiceCaminho = 0;
            alvoCaminho = destino;
            tempoRecalculo = perseguindo ? 0.5f : 1f;
            caminhoCalculado = true;

            if (caminho.Count == 0)
            {
                // Alvo inalcançável: não fica preso, libera a patrulha pra escolher outro destino logo.
                temAlvoPatrulha = false;
                tempoEsperaPatrulha = 0.3f;
                Estado = EstadoZumbi.Parado;
                TempoEstado += delta;
                AtualizaAnimacao(delta);
                return;
            }
        }

        while (indiceCaminho < caminho.Count &&
               Vector2.DistanceSquared(Pos, caminho[indiceCaminho]) <= 4f)
        {
            indiceCaminho++;
        }

        if (indiceCaminho >= caminho.Count)
        {
            Estado = EstadoZumbi.Parado;
            TempoEstado += delta;
            AtualizaAnimacao(delta);
            return;
        }

        Vector2 distanciaPonto = caminho[indiceCaminho] - Pos;
        float distancia = distanciaPonto.Length();

        if (distancia <= 0.001f)
        {
            Estado = EstadoZumbi.Parado;
            TempoEstado += delta;
            AtualizaAnimacao(delta);
            return;
        }

        Estado = EstadoZumbi.Andando;
        TempoEstado = 0f;

        Vector2 movimento = distanciaPonto / distancia *
            Math.Min(distancia, velocidade * MultiplicadorVelocidade * delta * 60f);

        Vector2 tentativaX = new Vector2(Pos.X + movimento.X, Pos.Y);
        if (navegacao.PodeOcupar(tentativaX))
            Pos.X = tentativaX.X;

        Vector2 tentativaY = new Vector2(Pos.X, Pos.Y + movimento.Y);
        if (navegacao.PodeOcupar(tentativaY))
            Pos.Y = tentativaY.Y;

        if (movimento.X > 0.01f)
            DirecaoAtual = Direcao.Direita;
        else if (movimento.X < -0.01f)
            DirecaoAtual = Direcao.Esquerda;

        AtualizaAnimacao(delta);
    }

    void AtualizaAnimacao(float delta)
    {
        bool parado = Estado == EstadoZumbi.Parado;
        float fps = parado ? FpsIdle : FpsWalk;

        // Reseta o quadro ao trocar de andar <-> parado pra não "pular" no meio do ciclo.
        if (parado != paradoAnimacaoAnterior)
        {
            TempoAnimacao = 0f;
            paradoAnimacaoAnterior = parado;
        }

        TempoAnimacao += delta * fps;

        int quadros = (parado ? IdleLado : WalkLado)?.QuantidadeFrames ?? 0;
        if (quadros > 0)
            TempoAnimacao %= quadros;
    }

    bool TentaCaminhoPara(Vector2 alvoDesejado, NavegacaoZumbi navegacao)
    {
        List<Vector2> novoCaminho = navegacao.AchaCaminho(Pos, alvoDesejado);

        if (novoCaminho.Count == 0)
            return false;

        caminho = novoCaminho;
        indiceCaminho = 0;
        alvoCaminho = alvoDesejado;
        tempoRecalculo = 1f;
        caminhoCalculado = true;
        alvoPatrulhaAtual = alvoDesejado;
        temAlvoPatrulha = true;

        return true;
    }

    Vector2? EscolhePontoAleatorio(NavegacaoZumbi navegacao)
    {
        for (int tentativa = 0; tentativa < 12; tentativa++)
        {
            float angulo = Random.Shared.NextSingle() * MathF.Tau;
            float distancia = 96f + Random.Shared.NextSingle() * 144f;

            Vector2 candidato = ancoraPatrulha + new Vector2(
                MathF.Cos(angulo) * distancia,
                MathF.Sin(angulo) * distancia
            );

            if (navegacao.PodeOcupar(candidato))
                return candidato;
        }

        return null;
    }

    public bool TomaDano(int dano)
    {
        if (Estado == EstadoZumbi.Morto ||
            Estado == EstadoZumbi.Queimando ||
            Estado == EstadoZumbi.Morrendo)
            return false;

        Vida -= dano;
        TempoTremor = 0.15f;

        if (Vida <= 0)
        {
            Morrer();
            return true;
        }

        return false;
    }

    void Morrer()
    {
        // Morre exatamente onde tomou o último hit - Pos não é alterado aqui.
        Estado = EstadoZumbi.Morrendo;
        TempoMorrendo = 0f;
        caminho.Clear();
        temAlvoPatrulha = false;
        caminhoCalculado = false;
    }

    public bool EstaMorto()
    {
        return Estado == EstadoZumbi.Morto;
    }

    public void Desenha()
    {
        if (Estado == EstadoZumbi.Morto)
            return;

        float tremorX = TempoTremor > 0f
            ? MathF.Sin(TempoTremor * 60f) * 3f
            : 0f;

        Efeitos.DesenhaSombra(
            Pos.X + TamanhoVisual / 2f,
            Pos.Y + TamanhoVisual,
            TamanhoVisual * 0.7f,
            TamanhoVisual * 0.2f
        );

        if (Estado == EstadoZumbi.Queimando)
        {
            DesenhaMorte(TempoQueimando / DuracaoQueimando, comFogo: true);
            return;
        }

        if (Estado == EstadoZumbi.Morrendo)
        {
            DesenhaMorte(TempoMorrendo / DuracaoMorrendo, comFogo: false);
            return;
        }

        DesenhaCorpo(tremorX, Color.White, 0f, false);
    }

    void DesenhaMorte(float progresso, bool comFogo)
    {
        progresso = Math.Clamp(progresso, 0f, 1f);
        float alpha = 255f * (1f - progresso);

        float rotacao = (DirecaoAtual == Direcao.Direita ? 1f : -1f) * 90f * progresso;

        // Pivô no pé do zumbi (base central) - destino também ancorado ali,
        // assim ele gira/desaparece no mesmo lugar em vez de "pular".
        Vector2 origem = new Vector2(TamanhoVisual / 2f, TamanhoVisual);
        Vector2 pivotMundo = new Vector2(Pos.X + TamanhoVisual / 2f, Pos.Y + TamanhoVisual);

        Rectangle destino = new Rectangle(
            pivotMundo.X,
            pivotMundo.Y,
            TamanhoVisual,
            TamanhoVisual
        );

        if (WalkLado.Textura.Id != 0)
        {
            Rectangle origemSprite = new Rectangle(0, 0, WalkLado.TamanhoFrame, WalkLado.TamanhoFrame);

            if (DirecaoAtual == Direcao.Esquerda)
                origemSprite.Width = -origemSprite.Width;

            Raylib.DrawTexturePro(
                WalkLado.Textura,
                origemSprite,
                destino,
                origem,
                rotacao,
                new Color((byte)255, (byte)255, (byte)255, (byte)alpha)
            );
        }
        else
        {
            Raylib.DrawRectanglePro(
                destino,
                origem,
                rotacao,
                new Color((byte)60, (byte)140, (byte)60, (byte)alpha)
            );
        }

        if (comFogo)
            DesenhaChamas(alpha, progresso);
    }

    void DesenhaChamas(float alphaCorpo, float progresso)
    {
        float intensidade = Math.Clamp(1f - progresso * 1.2f, 0f, 1f);
        byte alpha = (byte)(alphaCorpo * intensidade);

        if (alpha == 0)
            return;

        float oscilacao = MathF.Sin(TempoQueimando * 17f) * 5f;

        Color laranja = new Color((byte)255, (byte)105, (byte)20, alpha);
        Color amarelo = new Color((byte)255, (byte)220, (byte)70, alpha);

        DesenhaChama(Pos.X + 13f, Pos.Y + 70f, 30f + oscilacao, laranja);
        DesenhaChama(Pos.X + 40f, Pos.Y + 76f, 38f - oscilacao, laranja);
        DesenhaChama(Pos.X + 66f, Pos.Y + 70f, 28f + oscilacao, laranja);
        DesenhaChama(Pos.X + 39f, Pos.Y + 75f, 22f - oscilacao * 0.4f, amarelo);
    }

    static void DesenhaChama(float x, float baseY, float altura, Color cor)
    {
        Raylib.DrawTriangle(
            new Vector2(x, baseY),
            new Vector2(x + 15f, baseY),
            new Vector2(x + 8f, baseY - altura),
            cor
        );
    }

    void DesenhaCorpo(float tremorX, Color cor, float rotacao, bool deitado)
    {
        bool espelhar = DirecaoAtual == Direcao.Esquerda;

        Animacao animacao = Estado == EstadoZumbi.Parado ? IdleLado : WalkLado;

        if (animacao.Textura.Id != 0)
        {
            int frameAtual = (int)TempoAnimacao % animacao.QuantidadeFrames;

            Rectangle src = new Rectangle(
                frameAtual * animacao.TamanhoFrame,
                0,
                animacao.TamanhoFrame,
                animacao.TamanhoFrame
            );

            if (espelhar)
                src.Width = -src.Width;

            Rectangle dest = new Rectangle(Pos.X + tremorX, Pos.Y, TamanhoVisual, TamanhoVisual);

            Vector2 origem = deitado
                ? new Vector2(TamanhoVisual / 2f, TamanhoVisual)
                : Vector2.Zero;

            Raylib.DrawTexturePro(animacao.Textura, src, dest, origem, rotacao, cor);
        }
        else
        {
            Rectangle dest = new Rectangle(Pos.X + tremorX, Pos.Y, TamanhoVisual, TamanhoVisual);

            Raylib.DrawRectanglePro(
                dest,
                Vector2.Zero,
                rotacao,
                new Color((byte)60, (byte)140, (byte)60, (byte)255)
            );
        }
    }
}