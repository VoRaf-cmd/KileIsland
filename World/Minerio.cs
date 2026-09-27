using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public class Minerio
{
    public TipoMinerio Tipo;
    public Vector2 Pos;
    public int PicaretadasRestantes;
    public float TempoTremor;

    public float TempoAnimacao = 0f;
    public const float DuracaoAnimacao = 0.25f;

    static Dictionary<TipoMinerio, Texture2D> cache = new Dictionary<TipoMinerio, Texture2D>();
    static bool cacheIniciado = false;

    public const int Tamanho = 96;

    public Vector2 Centro()
    {
        return new Vector2(Pos.X + Tamanho / 2f, Pos.Y + Tamanho / 2f);
    }

    public void IniciaAnimacao()
    {
        TempoAnimacao = DuracaoAnimacao;
    }

    public void Atualiza(float delta)
    {
        if (TempoTremor > 0f) TempoTremor -= delta;
        if (TempoAnimacao > 0f) TempoAnimacao -= delta;
    }

    public void Desenha()
    {
        float tremorX = 0f;
        if (TempoTremor > 0f)
            tremorX = (float)Math.Sin(TempoTremor * 80f) * 3f;

        float progresso = 0f;
        if (TempoAnimacao > 0f)
            progresso = TempoAnimacao / DuracaoAnimacao;

        float squash = progresso * 0.2f;
        float rotacao = progresso * 15f;

        float larguraAnim = Tamanho * (1f - squash);
        float alturaAnim  = Tamanho * (1f + squash);

        Vector2 centro = new Vector2(Pos.X + tremorX + Tamanho / 2f,
                                      Pos.Y + Tamanho / 2f);

        Efeitos.DesenhaSombra(centro.X, Pos.Y + Tamanho, Tamanho * 0.7f, Tamanho * 0.2f);

        Texture2D tex = PegaTextura(Tipo);

        if (tex.Id != 0)
        {
            Rectangle src = new Rectangle(0, 0, tex.Width, tex.Height);
            Rectangle dest = new Rectangle(centro.X, centro.Y, larguraAnim, alturaAnim);
            Vector2 origem = new Vector2(larguraAnim / 2f, alturaAnim / 2f);

            Raylib.DrawTexturePro(tex, src, dest, origem, rotacao, Color.White);
        }
        else
        {
            Color corFallback;
            if (Tipo == TipoMinerio.Cobre) corFallback = new Color(180, 90, 50, 255);
            else if (Tipo == TipoMinerio.Ferro) corFallback = new Color(120, 70, 40, 255);
            else corFallback = new Color(240, 200, 50, 255);

            Raylib.DrawRectangle((int)Pos.X, (int)Pos.Y, Tamanho, Tamanho, corFallback);
        }
    }

    public static Texture2D PegaTextura(TipoMinerio tipo)
    {
        if (!cacheIniciado)
        {
            cache[TipoMinerio.Cobre] = Carrega("Assets/sprites/minerios/cobre.png");
            cache[TipoMinerio.Ferro] = Carrega("Assets/sprites/minerios/ferro.png");
            cache[TipoMinerio.Ouro]  = Carrega("Assets/sprites/minerios/ouro.png");
            cacheIniciado = true;
        }
        return cache[tipo];
    }

    static Texture2D Carrega(string caminho)
    {
        if (System.IO.File.Exists(caminho)) return Raylib.LoadTexture(caminho);
        return new Texture2D();
    }
}