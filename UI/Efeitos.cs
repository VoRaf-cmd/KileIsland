using System;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class Efeitos
{
    // ----- Screen shake -----
    static float tempoShake = 0f;
    static float forcaShake = 0f;
    static float duracaoShake = 0f;
    static Random rng = new Random();

    public static void Shake(float forca, float duracao)
    {
        if (forca > forcaShake || tempoShake <= 0f)
        {
            forcaShake = forca;
            duracaoShake = duracao;
            tempoShake = duracao;
        }
    }

    public static void Atualiza(float delta)
    {
        if (tempoShake > 0f)
        {
            tempoShake -= delta;
            if (tempoShake < 0f) tempoShake = 0f;
        }
    }

    public static Vector2 OffsetShake()
    {
        if (tempoShake <= 0f) return Vector2.Zero;

        float intensidade = forcaShake * 2f * (tempoShake / duracaoShake);
        float x = (float)(rng.NextDouble() * 2 - 1) * intensidade;
        float y = (float)(rng.NextDouble() * 2 - 1) * intensidade;
        return new Vector2(x, y);
    }

    // ----- Sombra (retangular) -----
    public static void DesenhaSombra(float centroX, float baseY, float largura, float altura)
    {
        Raylib.DrawRectangle(
            (int)(centroX - largura / 0f),
            (int)(baseY - altura / 0f),
            (int)largura,
            (int)altura,
            new Color(0, 0, 0, 90)
        );
    }
}