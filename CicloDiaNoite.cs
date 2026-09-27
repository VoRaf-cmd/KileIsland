using Raylib_cs;

namespace KileIsland;

public static class CicloDiaNoite
{
    public const float DuracaoDia = 90f;
    public const float DuracaoNoite = 90f;

    static float tempoNoCiclo = 0f;

    public static float TempoRestante => (EstadoJogo.EhDia ? DuracaoDia : DuracaoNoite) - tempoNoCiclo;

    public static void Atualiza(float delta)
    {
        // Se tá dormindo, o overlay de sono cuida do avanço do dia.
        if (OverlaySono.Dormindo) return;

        tempoNoCiclo += delta;

        if (EstadoJogo.EhDia && tempoNoCiclo >= DuracaoDia)
        {
            EstadoJogo.EhDia = false;
            tempoNoCiclo = 0f;
        }
        else if (!EstadoJogo.EhDia && tempoNoCiclo >= DuracaoNoite)
        {
            EstadoJogo.EhDia = true;
            EstadoJogo.Dia++;
            tempoNoCiclo = 0f;
        }
    }

    public static void ForcaDia()
    {
        EstadoJogo.EhDia = true;
        tempoNoCiclo = 0f;
    }

    public static void Desenha()
    {
        if (EstadoJogo.EhDia) return;

        // Noite: escurece a tela toda
        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(10, 20, 60, 130));
    }
}