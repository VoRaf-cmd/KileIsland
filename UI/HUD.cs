using System;
using Raylib_cs;

namespace KileIsland;

public static class HUD
{
    static readonly Color Outline      = new((byte)15, (byte)12, (byte)22, (byte)255);
    static readonly Color HeartFull    = new((byte)230, (byte)60, (byte)90, (byte)255);
    static readonly Color HeartFullP2  = new((byte)100, (byte)180, (byte)255, (byte)255);
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

    // Agora recebe os 2 jogadores
public static void Desenha(Jogador[] jogadores)
{
    DesenhaPainel(jogadores[0], 10, 10);

    if (jogadores.Length > 1)
        DesenhaPainel(jogadores[1], Program.LarguraTela - 190, 10);

    DesenhaDiaTimer();
    DesenhaLevelUp();
}

static void DesenhaDiaTimer()
{
    int largura = 220;
    int altura = 56;
    int x = (Program.LarguraTela - largura) / 2;
    int y = 10;

    Raylib.DrawRectangle(x, y, largura, altura, new Color(0, 0, 0, 170));
    Raylib.DrawRectangleLines(x, y, largura, altura, new Color(255, 255, 255, 100));

    string texto = $"Dia {EstadoJogo.Dia}";
    int tam = 22;
    int larguraTexto = Raylib.MeasureText(texto, tam);
    Raylib.DrawText(texto, x + (largura - larguraTexto) / 2, y + 6, tam, Color.White);

    float restante = CicloDiaNoite.TempoRestante;
    int min = (int)(restante / 60f);
    int seg = (int)(restante % 60f);

    string fase = EstadoJogo.EhDia ? "Dia" : "Noite";
    string textoTempo = $"{fase}: {min}:{seg:00}";
    int tamTempo = 18;
    int larguraTempo = Raylib.MeasureText(textoTempo, tamTempo);

    Color corTempo = EstadoJogo.EhDia
        ? new Color((byte)255, (byte)230, (byte)140, (byte)255)
        : new Color((byte)180, (byte)200, (byte)255, (byte)255);

    Raylib.DrawText(textoTempo, x + (largura - larguraTempo) / 2, y + 32, tamTempo, corTempo);
}

    static void DesenhaPainel(Jogador j, int painelX, int painelY)
    {
        int painelLargura = 180;
        int painelAltura = 118;

        Raylib.DrawRectangle(painelX, painelY, painelLargura, painelAltura,
                             new Color(0, 0, 0, 150));
        Raylib.DrawRectangleLines(painelX, painelY, painelLargura, painelAltura,
                                  new Color(255, 255, 255, 80));

        int x = painelX + 10;
        int y = painelY + 8;

        // Nome do jogador
        string nome = j.Indice == 0 ? "P1" : "P2";
        Raylib.DrawText(nome, x, y, 16, j.Cor);
        y += 20;

        // Coracoes
        DesenhaCoracoes(x, y, j.VidaPontos, j.VidaMaxPontos, j.Indice);
        y += CoracaoSize + 6;

        // Level (compartilhado)
        Raylib.DrawText($"Nv {EstadoJogo.Nivel}", x, y, 18, Color.White);
        y += 22;

        // Barra XP (compartilhada)
        int barraLargura = 160;
        int barraAltura = 10;
        float porcentagem = (float)EstadoJogo.Xp / EstadoJogo.XpProximoNivel;
        if (porcentagem > 1f) porcentagem = 1f;

        Raylib.DrawRectangle(x, y, barraLargura, barraAltura, new Color(40, 40, 40, 255));
        Raylib.DrawRectangle(x, y, (int)(barraLargura * porcentagem), barraAltura,
                             new Color(80, 180, 255, 255));
        Raylib.DrawRectangleLines(x, y, barraLargura, barraAltura, Color.White);
        y += barraAltura + 6;

        // Item equipado (individual)
        string item = j.ItemAtual == ItemEquipado.Espada ? "Espada" : "Picareta";
        Raylib.DrawText($"[{item}]", x, y, 14, new Color(220, 220, 220, 255));
    }

    static void DesenhaLevelUp()
    {
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

    static void DesenhaCoracoes(int x, int y, int pontos, int pontosMax, int indice)
    {
        int totalCoracoes = pontosMax / 4;

        for (int i = 0; i < totalCoracoes; i++)
        {
            int pontosNoCoracao = pontos - (i * 4);
            if (pontosNoCoracao < 0) pontosNoCoracao = 0;
            if (pontosNoCoracao > 4) pontosNoCoracao = 4;

            int cx = x + i * (CoracaoSize + 4);
            DesenhaCoracao(cx, y, CoracaoSize, pontosNoCoracao, indice);
        }
    }

    static void DesenhaCoracao(int x, int y, int size, int pontosNoCoracao, int indice)
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
            Color cor = indice == 0 ? HeartFull : HeartFullP2;
            DesenhaMascaraParcial(x, y, size, cor, linhasPreencher);
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