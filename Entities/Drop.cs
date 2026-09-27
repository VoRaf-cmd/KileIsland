using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public class Drop
{
    public TipoMinerio Tipo;
    public Vector2 Pos;
    public float TempoAnimacao = 0f;

    public const float DuracaoPulo = 0.4f;
    public const int Tamanho = 96;   // 16 de arte × 6 de escala

    // Cache de texturas
    static Dictionary<TipoMinerio, Texture2D> cache = new Dictionary<TipoMinerio, Texture2D>();
    static bool cacheIniciado = false;

    public Rectangle Retangulo()
    {
        return new Rectangle(Pos.X, Pos.Y, Tamanho, Tamanho);
    }

    public void Atualiza(float delta)
    {
        if (TempoAnimacao > 0f) TempoAnimacao -= delta;
    }

    public void Desenha()
    {
        // Offset do pulinho (curva: sobe e desce)
        float offsetY = 0f;
        if (TempoAnimacao > 0f)
        {
            float progresso = 1f - (TempoAnimacao / DuracaoPulo); // 0..1
            // sin(pi * p) vai de 0 a 1 a 0
            offsetY = -(float)Math.Sin(progresso * Math.PI) * 30f;
        }

        Vector2 pos = new Vector2(Pos.X, Pos.Y + offsetY);

        // Sombra
        Efeitos.DesenhaSombra(pos.X + Tamanho / 2f,
                              Pos.Y + Tamanho,
                              Tamanho * 0.6f,
                              Tamanho * 0.2f);

        Texture2D tex = PegaTextura(Tipo);
        if (tex.Id != 0)
        {
            Rectangle src = new Rectangle(0, 0, tex.Width, tex.Height);
            Rectangle dest = new Rectangle(pos.X, pos.Y, Tamanho, Tamanho);
            Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Color cor;
            if (Tipo == TipoMinerio.Cobre) cor = new Color(220, 130, 80, 255);
            else if (Tipo == TipoMinerio.Ferro) cor = new Color(160, 160, 170, 255);
            else cor = new Color(255, 220, 60, 255);

            Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Tamanho, Tamanho, cor);
        }
    }

    public static Texture2D PegaTextura(TipoMinerio tipo)
    {
        if (!cacheIniciado)
        {
            cache[TipoMinerio.Cobre] = Carrega("assets/sprites/minerios/drop_cobre.png");
            cache[TipoMinerio.Ferro] = Carrega("assets/sprites/minerios/drop_ferro.png");
            cache[TipoMinerio.Ouro]  = Carrega("assets/sprites/minerios/drop_ouro.png");
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