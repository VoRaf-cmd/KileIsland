using Raylib_cs;

namespace KileIsland;

public enum EstadoSono { Acordado, FadeOut, Escuro, FadeIn }

public static class OverlaySono
{
    public static EstadoSono Estado = EstadoSono.Acordado;
    public static float Tempo = 0f;

    public const float DuracaoFadeOut = 1.5f;
    public const float DuracaoEscuro = 2.0f;
    public const float DuracaoFadeIn = 1.5f;

    public static bool Dormindo => Estado != EstadoSono.Acordado;

    public static void Inicia()
    {
        Estado = EstadoSono.FadeOut;
        Tempo = 0f;
    }

    public static void Atualiza(float delta)
    {
        if (!Dormindo) return;

        Tempo += delta;

        switch (Estado)
        {
            case EstadoSono.FadeOut:
                if (Tempo >= DuracaoFadeOut) { Estado = EstadoSono.Escuro; Tempo = 0f; }
                break;
            case EstadoSono.Escuro:
                if (Tempo >= DuracaoEscuro)
                {
                    EstadoJogo.Dia++;
                    EstadoJogo.EhDia = true;   // acorda de dia
                    CicloDiaNoite.ForcaDia();  // reseta o relógio do ciclo
                    Estado = EstadoSono.FadeIn;
                    Tempo = 0f;
                }
                break;
            case EstadoSono.FadeIn:
                if (Tempo >= DuracaoFadeIn) { Estado = EstadoSono.Acordado; Tempo = 0f; }
                break;
        }
    }

    public static void Desenha()
    {
        if (!Dormindo) return;

        float alpha = 0f;
        switch (Estado)
        {
            case EstadoSono.FadeOut: alpha = Tempo / DuracaoFadeOut; break;
            case EstadoSono.Escuro:  alpha = 1f; break;
            case EstadoSono.FadeIn:  alpha = 1f - (Tempo / DuracaoFadeIn); break;
        }

        if (alpha < 0f) alpha = 0f;
        if (alpha > 1f) alpha = 1f;

        byte a = (byte)(alpha * 255);
        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color((byte)0, (byte)0, (byte)0, a));

        if (Estado == EstadoSono.Escuro)
        {
            string msg = $"Dia {EstadoJogo.Dia + 1}";
            int tam = 48;
            int larguraTexto = Raylib.MeasureText(msg, tam);
            Raylib.DrawText(msg, (Program.LarguraTela - larguraTexto) / 2,
                            Program.AlturaTela / 2 - tam / 2, tam, Color.White);
        }
        else if (Estado == EstadoSono.FadeIn)
        {
            string msg = $"Dia {EstadoJogo.Dia}";
            int tam = 48;
            int larguraTexto = Raylib.MeasureText(msg, tam);
            Raylib.DrawText(msg, (Program.LarguraTela - larguraTexto) / 2,
                            Program.AlturaTela / 2 - tam / 2, tam,
                            new Color((byte)255, (byte)255, (byte)255, (byte)(alpha * 255)));
        }
    }
}