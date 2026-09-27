using Raylib_cs;

namespace KileIsland;

public static class CicloDiaNoite
{
    public const float DuracaoDia = 90f;
    public const float DuracaoNoite = 90f;
    const float DuracaoTransicao = 2.5f;

    static float tempoNoCiclo = 0f;
    static float tempoTransicao = 0f;
    static float intensidadeEscurecimento = 0f;
    static bool transicaoAtiva = false;
    static bool escurecendo = false;

    public static float TempoRestante => (EstadoJogo.EhDia ? DuracaoDia : DuracaoNoite) - tempoNoCiclo;

    public static void Atualiza(float delta)
    {
        // Se tá dormindo, o overlay de sono cuida do avanço do dia.
        if (OverlaySono.Dormindo) return;

        if (transicaoAtiva)
        {
            tempoTransicao += delta;
            float progresso = Math.Clamp(tempoTransicao / DuracaoTransicao, 0f, 1f);
            intensidadeEscurecimento = escurecendo ? progresso : 1f - progresso;

            if (progresso >= 1f)
            {
                intensidadeEscurecimento = escurecendo ? 1f : 0f;
                transicaoAtiva = false;
            }
        }

        tempoNoCiclo += delta;

        if (EstadoJogo.EhDia && tempoNoCiclo >= DuracaoDia)
        {
            EstadoJogo.EhDia = false;
            tempoNoCiclo = 0f;
            IniciaTransicao(escurecer: true);
        }
        else if (!EstadoJogo.EhDia && tempoNoCiclo >= DuracaoNoite)
        {
            if (EstadoJogo.Dia == EventoTelefone.DiaDoEvento - 1)
            {
                tempoNoCiclo = DuracaoNoite;
                return;
            }

            EstadoJogo.EhDia = true;
            EstadoJogo.Dia++;
            tempoNoCiclo = 0f;
            IniciaTransicao(escurecer: false);
        }
    }

    static void IniciaTransicao(bool escurecer)
    {
        escurecendo = escurecer;
        tempoTransicao = 0f;
        transicaoAtiva = true;
    }

    public static void ForcaDia()
    {
        EstadoJogo.EhDia = true;
        tempoNoCiclo = 0f;
        tempoTransicao = 0f;
        intensidadeEscurecimento = 0f;
        transicaoAtiva = false;
    }

    public static void Desenha()
    {
        if (intensidadeEscurecimento <= 0f) return;

        byte alpha = (byte)(130f * intensidadeEscurecimento);
        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color((byte)10, (byte)20, (byte)60, alpha));
    }
}