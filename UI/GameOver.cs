using Raylib_cs;

namespace KileIsland;

public static class GameOverUI
{
    public static bool Ativo = false;

    public static void Inicia()
    {
        Ativo = true;
    }

    public static void Desenha()
    {
        if (!Ativo) return;

        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(0, 0, 0, 220));

        string titulo = "VOCE MORREU";
        int tamTitulo = 64;
        int larguraTitulo = Raylib.MeasureText(titulo, tamTitulo);
        Raylib.DrawText(titulo,
            (Program.LarguraTela - larguraTitulo) / 2,
            Program.AlturaTela / 2 - 100,
            tamTitulo, new Color(220, 60, 60, 255));

        string sub = $"Dia {EstadoJogo.Dia}   Nivel {EstadoJogo.Nivel}";
        int tamSub = 28;
        int larguraSub = Raylib.MeasureText(sub, tamSub);
        Raylib.DrawText(sub,
            (Program.LarguraTela - larguraSub) / 2,
            Program.AlturaTela / 2 - 10,
            tamSub, Color.White);

        string dica = "Pressione R para recomecar";
        int tamDica = 24;
        int larguraDica = Raylib.MeasureText(dica, tamDica);
        Raylib.DrawText(dica,
            (Program.LarguraTela - larguraDica) / 2,
            Program.AlturaTela / 2 + 60,
            tamDica, new Color(200, 200, 200, 255));
    }
}