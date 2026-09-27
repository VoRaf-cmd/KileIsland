using System;
using Raylib_cs;

namespace KileIsland;

public static class HUD
{
    static readonly Color Outline      = new((byte)15, (byte)12, (byte)22, (byte)255);
    static readonly Color HeartFull    = new((byte)230, (byte)60, (byte)90, (byte)255);
    static readonly Color HeartEmpty   = new((byte)70, (byte)55, (byte)75, (byte)255);

    static readonly string[] HeartMask =
    {
        "01101",
        "11111",
        "11111",
        "01110",
        "00100",
    };

    const int CoracaoSize = 14;

    public static void Carrega() { }

    public static void Desenha()
    {
        int painelX = 10;
        int painelY = 10;
        int painelLargura = 180;
        int painelAltura = 118;

        Raylib.DrawRectangle(painelX, painelY, painelLargura, painelAltura,
                             new Color(0, 0, 0, 150));
        Raylib.DrawRectangleLines(painelX, painelY, painelLargura, painelAltura,
                                  new Color(255, 255, 255, 80));

        int x = painelX + 10;
        int y = painelY + 8;

        DesenhaCoracoes(x, y, EstadoJogo.VidaPontos, EstadoJogo.VidaMaxPontos);
        y += CoracaoSize + 6;

        Raylib.DrawText($"Nv {EstadoJogo.Nivel}", x, y, 18, Color.White);
        y += 22;

        int barraLargura = 160;
        int barraAltura = 10;
        float porcentagem = (float)EstadoJogo.Xp / EstadoJogo.XpProximoNivel;
        if (porcentagem > 1f) porcentagem = 1f;

        Raylib.DrawRectangle(x, y, barraLargura, barraAltura, new Color(40, 40, 40, 255));
        Raylib.DrawRectangle(x, y, (int)(barraLargura * porcentagem), barraAltura,
                             new Color(80, 180, 255, 255));
        Raylib.DrawRectangleLines(x, y, barraLargura, barraAltura, Color.White);
        y += barraAltura + 6;

        string fase = EstadoJogo.EhDia ? "Dia" : "Noite";
        float restante = CicloDiaNoite.TempoRestante;
        int min = (int)(restante / 60f);
        int seg = (int)(restante % 60f);
        Raylib.DrawText($"{fase} {EstadoJogo.Dia}  {min}:{seg:00}", x, y, 14,
                        new Color(200, 200, 200, 255));
        y += 20;

        Raylib.DrawText($"$ {EstadoJogo.Moedas}", x, y, 16, new Color(255, 220, 90, 255));

        // Aviso de level up no centro
        if (LevelUpAviso.Tempo > 0f)
        {
            int tam = 44;
            int largura = Raylib.MeasureText(LevelUpAviso.Texto, tam);
            float alpha = LevelUpAviso.Tempo / LevelUpAviso.Duracao;
            if (alpha > 1f) alpha = 1f;

            Raylib.DrawText(LevelUpAviso.Texto,
                (Program.LarguraTela - largura) / 2,
                Program.AlturaTela / 2 - 120,
                tam, new Color((byte)255, (byte)220, (byte)90, (byte)(alpha * 255)));
        }
    }

    static void DesenhaCoracoes(int x, int y, int pontos, int pontosMax)
    {
        int totalCoracoes = pontosMax / 4;

        for (int i = 0; i < totalCoracoes; i++)
        {
            int pontosNoCoracao = pontos - (i * 4);
            if (pontosNoCoracao < 0) pontosNoCoracao = 0;
            if (pontosNoCoracao > 4) pontosNoCoracao = 4;

            int cx = x + i * (CoracaoSize + 4);
            DesenhaCoracao(cx, y, CoracaoSize, pontosNoCoracao);
        }
    }

    static void DesenhaCoracao(int x, int y, int size, int pontosNoCoracao)
    {
        Raylib.DrawRectangle(x - 1, y - 1, size + 2, size + 2, Outline);
        DesenhaMascara(x, y, size, HeartEmpty, 5);

        if (pontosNoCoracao > 0)
        {
            int linhasPreencher = pontosNoCoracao switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 5,
                _ => 5,
            };
            DesenhaMascaraParcial(x, y, size, HeartFull, linhasPreencher);
        }
    }

    static void DesenhaMascara(int x, int y, int size, Color cor, int linhas)
    {
        float px = size / 5f;
        for (int row = 0; row < linhas && row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (HeartMask[row][col] == '1')
                    Raylib.DrawRectangle((int)(x + col * px), (int)(y + row * px),
                        (int)Math.Ceiling(px), (int)Math.Ceiling(px), cor);
    }

    static void DesenhaMascaraParcial(int x, int y, int size, Color cor, int linhas)
    {
        float px = size / 5f;
        for (int row = 0; row < 5; row++)
        {
            int rowFromBottom = 4 - row;
            if (rowFromBottom >= linhas) continue;
            for (int col = 0; col < 5; col++)
                if (HeartMask[row][col] == '1')
                    Raylib.DrawRectangle((int)(x + col * px), (int)(y + row * px),
                        (int)Math.Ceiling(px), (int)Math.Ceiling(px), cor);
        }
    }
}