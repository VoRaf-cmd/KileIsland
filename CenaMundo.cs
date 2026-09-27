using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class CenaMundo
{
    public const int LarguraMapa = 2600;
    public const int AlturaMapa = 1500;

    public const int GramaLargura = 2400;
    public const int GramaAltura = 1300;

    public static Vector2 PosIlha = new Vector2(
        (LarguraMapa - GramaLargura) / 2f,
        (AlturaMapa - GramaAltura) / 2f
    );

    static Texture2D texturaIlha;
    static bool tentouCarregar = false;

    public static void Carrega()
    {
        if (tentouCarregar) return;
        tentouCarregar = true;

        if (System.IO.File.Exists("assets/sprites/ilha.png"))
            texturaIlha = Raylib.LoadTexture("assets/sprites/ilha.png");
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