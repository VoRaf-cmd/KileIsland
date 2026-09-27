using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class CenaMundo
{
    const int LarguraMapaInicial = 2600;
    const int AlturaMapaInicial = 1500;
    const int GramaLarguraInicial = 2400;
    const int GramaAlturaInicial = 1300;

    public static int LarguraMapa { get; private set; } = LarguraMapaInicial;
    public static int AlturaMapa { get; private set; } = AlturaMapaInicial;

    public static int GramaLargura { get; private set; } = GramaLarguraInicial;
    public static int GramaAltura { get; private set; } = GramaAlturaInicial;

    public static Vector2 PosIlha { get; private set; } = new Vector2(
        (LarguraMapa - GramaLargura) / 2f,
        (AlturaMapa - GramaAltura) / 2f
    );

    public static Vector2 CentroIlha => new Vector2(
        PosIlha.X + GramaLargura / 2f,
        PosIlha.Y + GramaAltura / 2f
    );

    public static void AmpliaIlha(float multiplicador)
    {
        if (multiplicador <= 1f) return;

        Vector2 centro = CentroIlha;
        GramaLargura = (int)MathF.Round(GramaLargura * multiplicador);
        GramaAltura = (int)MathF.Round(GramaAltura * multiplicador);
        LarguraMapa = Math.Max(LarguraMapa, GramaLargura + 200);
        AlturaMapa = Math.Max(AlturaMapa, GramaAltura + 200);
        PosIlha = new Vector2(
            centro.X - GramaLargura / 2f,
            centro.Y - GramaAltura / 2f
        );
    }

    public static void RestauraDimensoes()
    {
        LarguraMapa = LarguraMapaInicial;
        AlturaMapa = AlturaMapaInicial;
        GramaLargura = GramaLarguraInicial;
        GramaAltura = GramaAlturaInicial;
        PosIlha = new Vector2(
            (LarguraMapa - GramaLargura) / 2f,
            (AlturaMapa - GramaAltura) / 2f
        );
    }

    static Texture2D texturaIlha;
    static bool tentouCarregar = false;

    public static void Carrega()
    {
        if (tentouCarregar) return;
        tentouCarregar = true;

        if (System.IO.File.Exists("Assets/sprites/ilha.png"))
            texturaIlha = Raylib.LoadTexture("Assets/sprites/ilha.png");
        else
            texturaIlha = new Texture2D();
    }

    public static void DesenhaChao()
    {
        Raylib.ClearBackground(new Color(30, 80, 160, 255));

        if (texturaIlha.Id != 0)
        {
            Rectangle src = new Rectangle(0, 0, texturaIlha.Width, texturaIlha.Height);
            Rectangle dest = new Rectangle(PosIlha.X, PosIlha.Y, GramaLargura, GramaAltura);
            Raylib.DrawTexturePro(texturaIlha, src, dest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Raylib.DrawRectangle((int)PosIlha.X, (int)PosIlha.Y,
                                 GramaLargura, GramaAltura,
                                 new Color(80, 160, 80, 255));
        }
    }
}