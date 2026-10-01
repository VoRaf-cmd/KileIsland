using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class JujuUI
{
    static int cartaSelecionada = 0;

    public static void ResetarSelecao() { cartaSelecionada = 0; }

    public static void Desenha(InputState[] inputs, bool podeInteragir)
    {
        int painelLargura = 720;
        int painelAltura = 460;
        int painelX = (Program.LarguraTela - painelLargura) / 2;
        int painelY = (Program.AlturaTela - painelAltura) / 2;

        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(0, 0, 0, 120));
        Raylib.DrawRectangle(painelX, painelY, painelLargura, painelAltura,
                             new Color(35, 25, 45, 245));
        Raylib.DrawRectangleLinesEx(new Rectangle(painelX, painelY, painelLargura, painelAltura),
                                    3, new Color(180, 130, 220, 255));

        Vector2 mouse = Raylib.GetMousePosition();

        Raylib.DrawText("JUJU", painelX + 20, painelY + 15, 32, new Color(220, 180, 255, 255));
        Raylib.DrawText($"Moedas: {EstadoJogo.Moedas}   XP: {EstadoJogo.Xp}",
                        painelX + 20, painelY + 55, 18, Color.White);

        // ----- Input dos 2 jogadores -----
        bool menuDown    = inputs[0].MenuDown    || inputs[1].MenuDown;
        bool menuUp      = inputs[0].MenuUp      || inputs[1].MenuUp;
        bool menuCancel  = inputs[0].MenuCancel  || inputs[1].MenuCancel;
        bool menuConfirm = inputs[0].MenuConfirm || inputs[1].MenuConfirm;

        List<Carta> cartas = Carta.Todas();
        int total = cartas.Count;

        if (podeInteragir)
        {
            if (menuDown)
            {
                cartaSelecionada = (cartaSelecionada + 1) % total;
                AudioManager.TocaSelecaoMenu();
            }
            if (menuUp)
            {
                cartaSelecionada--;
                if (cartaSelecionada < 0) cartaSelecionada = total - 1;
                AudioManager.TocaSelecaoMenu();
            }
            if (menuCancel)
            {
                AudioManager.TocaCliqueMenu();
                Program.JujuAberta = false;
                return;
            }
        }

        bool confirmar = podeInteragir && menuConfirm;

        // ----- Coluna esquerda: lista -----
        int listaX = painelX + 20;
        int listaY = painelY + 100;
        int listaItemAltura = 60;

        for (int i = 0; i < total; i++)
        {
            var c = cartas[i];
            bool selecionada = i == cartaSelecionada;

            Rectangle rect = new Rectangle(listaX, listaY + i * listaItemAltura,
                                           280, listaItemAltura - 6);

            Color fundo = selecionada
                ? new Color(90, 60, 130, 255)
                : new Color(50, 35, 70, 255);
            Color borda = selecionada
                ? new Color(220, 180, 255, 255)
                : new Color(120, 90, 150, 255);

            Raylib.DrawRectangleRec(rect, fundo);
            Raylib.DrawRectangleLinesEx(rect, selecionada ? 3 : 2, borda);

            Raylib.DrawText(c.Nome, (int)rect.X + 10, (int)rect.Y + 8, 20, Color.White);
            Raylib.DrawText($"{c.Preco} moedas", (int)rect.X + 10, (int)rect.Y + 32, 14,
                            new Color(255, 220, 90, 255));

            // Já comprada (única)?
            if (c.Unica && EstadoJogo.CoracaoExtraComprado)
                Raylib.DrawText("COMPRADA", (int)rect.X + 180, (int)rect.Y + 32, 14,
                                new Color(140, 220, 140, 255));
        }

        // ----- Coluna direita: carta grande + descrição -----
        var cartaAtual = cartas[cartaSelecionada];

        int direitaX = painelX + 330;
        int direitaY = painelY + 100;

        // Fundo da "carta grande"
        Rectangle fundoCarta = new Rectangle(direitaX + 40, direitaY, 144, 192);
        Raylib.DrawRectangleRec(fundoCarta, new Color(20, 15, 30, 255));
        Raylib.DrawRectangleLinesEx(fundoCarta, 3, new Color(200, 160, 240, 255));

        // Nome da carta dentro do quadrado
        int larguraNome = Raylib.MeasureText(cartaAtual.Nome, 20);
        Raylib.DrawText(cartaAtual.Nome,
            (int)(fundoCarta.X + (fundoCarta.Width - larguraNome) / 2f),
            (int)(fundoCarta.Y + 80),
            20, new Color(240, 220, 255, 255));

        // Descrição embaixo do quadrado
        int descY = direitaY + 210;
        Raylib.DrawText(cartaAtual.Descricao, direitaX, descY, 18, Color.White);
        descY += 30;
        Raylib.DrawText($"Preco: {cartaAtual.Preco} moedas", direitaX, descY, 16,
                        new Color(255, 220, 90, 255));
        descY += 30;

        // Botão "Comprar"
        bool jaTemUnica = cartaAtual.Unica && EstadoJogo.CoracaoExtraComprado;
        bool podeComprar = !jaTemUnica && EstadoJogo.Moedas >= cartaAtual.Preco;

        if (Botao("Comprar", direitaX, descY + 10, 200, 40, mouse, podeComprar,
                  podeInteragir, confirmar))
        {
            if (podeComprar)
            {
                EstadoJogo.Moedas -= cartaAtual.Preco;

                if (cartaAtual.Tipo == TipoCarta.CoracaoExtra)
                {
                    // Aplica em TODOS os jogadores
                    if (Program.jogadoresGlobais != null)
                    {
                        foreach (var j in Program.jogadoresGlobais)
                            j.AumentaVidaMaxima(4);
                    }

                    // Espelha no EstadoJogo (pro save)
                    EstadoJogo.VidaMaxPontos += 4;
                    EstadoJogo.VidaPontos += 4;
                    EstadoJogo.CoracaoExtraComprado = true;
                }
                else
                {
                    // Vai pra fila
                    EstadoJogo.FilaCartas.Add(cartaAtual.Tipo);
                }

                Efeitos.Shake(4f, 0.2f);
                AudioManager.TocaCliqueMenu();
            }
        }

        // Fechar
        if (Botao("Fechar", painelX + painelLargura - 120, painelY + painelAltura - 50,
                  100, 32, mouse, true, podeInteragir, confirmar))
        {
            Program.JujuAberta = false;
        }
    }

    static bool Botao(string texto, int x, int y, int largura, int altura,
                      Vector2 mouse, bool ativo, bool podeInteragir, bool confirmar)
    {
        Rectangle rect = new Rectangle(x, y, largura, altura);
        bool hoverMouse = Raylib.CheckCollisionPointRec(mouse, rect) && ativo;
        bool clicadoMouse = hoverMouse && Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool confirmadoControle = confirmar && ativo;

        bool destacado = hoverMouse;

        Color fundo = !ativo
            ? new Color(40, 35, 50, 255)
            : (destacado ? new Color(110, 80, 150, 255) : new Color(70, 50, 100, 255));

        Color borda = ativo
            ? (destacado ? new Color(255, 220, 255, 255) : new Color(180, 140, 220, 255))
            : new Color(80, 70, 90, 255);

        Color corTexto = ativo ? Color.White : new Color(140, 130, 150, 255);

        Raylib.DrawRectangleRec(rect, fundo);
        Raylib.DrawRectangleLinesEx(rect, destacado ? 3 : 2, borda);

        int tamTexto = 18;
        int larguraTexto = Raylib.MeasureText(texto, tamTexto);
        Raylib.DrawText(texto,
            x + (largura - larguraTexto) / 2,
            y + (altura - tamTexto) / 2,
            tamTexto, corTexto);

        return clicadoMouse || confirmadoControle;
    }
}