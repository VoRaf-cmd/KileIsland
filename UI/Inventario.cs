using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class InventarioUI
{
    enum AbaInv { Equipamento, Deck }

    static AbaInv abaAtual = AbaInv.Equipamento;
    static int botaoSelecionado = 0;
    static int cartaSelecionada = 0;
    static int cartaMarcada = -1;  // -1 = nenhuma

    public static void ResetarSelecao()
    {
        abaAtual = AbaInv.Equipamento;
        botaoSelecionado = 0;
        cartaSelecionada = 0;
        cartaMarcada = -1;
    }

    public static void Desenha(InputState[] inputs, bool podeInteragir)
    {
        int painelLargura = 560;
        int painelAltura = 460;
        int painelX = (Program.LarguraTela - painelLargura) / 2;
        int painelY = (Program.AlturaTela - painelAltura) / 2;

        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(0, 0, 0, 120));
        Raylib.DrawRectangle(painelX, painelY, painelLargura, painelAltura,
                             new Color(35, 30, 45, 245));
        Raylib.DrawRectangleLinesEx(new Rectangle(painelX, painelY, painelLargura, painelAltura),
                                    3, new Color(140, 110, 200, 255));

        Vector2 mouse = Raylib.GetMousePosition();

        Raylib.DrawText("INVENTARIO", painelX + 20, painelY + 15, 32,
                        new Color(200, 180, 255, 255));

        // ----- Abas -----
        bool menuEsq    = inputs[0].MenuEsquerda || inputs[1].MenuEsquerda;
        bool menuDir    = inputs[0].MenuDireita  || inputs[1].MenuDireita;
        bool menuUp     = inputs[0].MenuUp       || inputs[1].MenuUp;
        bool menuDown   = inputs[0].MenuDown     || inputs[1].MenuDown;
        bool menuCancel = inputs[0].MenuCancel   || inputs[1].MenuCancel;
        bool menuConfirm= inputs[0].MenuConfirm  || inputs[1].MenuConfirm;

        if (podeInteragir && (menuEsq || menuDir))
        {
            abaAtual = abaAtual == AbaInv.Equipamento ? AbaInv.Deck : AbaInv.Equipamento;
            AudioManager.TocaSelecaoMenu();
        }

        if (podeInteragir && menuCancel)
        {
            if (cartaMarcada >= 0)
            {
                // Cancelar marcação
                cartaMarcada = -1;
                AudioManager.TocaCliqueMenu();
            }
            else
            {
                AudioManager.TocaCliqueMenu();
                Program.InventarioAberto = false;
                return;
            }
        }

        // Desenha as abas
        int abaY = painelY + 55;
        DesenhaAba("Equipamento", painelX + 20, abaY, 150, 28, abaAtual == AbaInv.Equipamento);
        DesenhaAba("Deck",        painelX + 180, abaY, 100, 28, abaAtual == AbaInv.Deck);

        if (abaAtual == AbaInv.Equipamento)
            DesenhaEquipamento(inputs, mouse, painelX, painelY, painelLargura, painelAltura,
                               podeInteragir, menuUp, menuDown, menuConfirm);
        else
            DesenhaDeck(inputs, mouse, painelX, painelY, painelLargura, painelAltura,
                        podeInteragir, menuUp, menuDown, menuConfirm);
    }

    static void DesenhaAba(string nome, int x, int y, int largura, int altura, bool ativa)
    {
        Rectangle rect = new Rectangle(x, y, largura, altura);
        Color fundo = ativa ? new Color(90, 70, 130, 255) : new Color(45, 38, 60, 255);
        Color borda = ativa ? new Color(220, 180, 255, 255) : new Color(100, 80, 130, 255);

        Raylib.DrawRectangleRec(rect, fundo);
        Raylib.DrawRectangleLinesEx(rect, ativa ? 3 : 2, borda);

        int tam = 18;
        int larg = Raylib.MeasureText(nome, tam);
        Raylib.DrawText(nome, x + (largura - larg) / 2, y + (altura - tam) / 2, tam,
                        ativa ? Color.White : new Color(180, 170, 200, 255));
    }

    // ============================================================
    // ABA EQUIPAMENTO
    // ============================================================
    static void DesenhaEquipamento(InputState[] inputs, Vector2 mouse,
        int painelX, int painelY, int painelLargura, int painelAltura,
        bool podeInteragir, bool menuUp, bool menuDown, bool menuConfirm)
    {
        int totalBotoes = EstadoJogo.Espadas.Count + 1;

        if (podeInteragir)
        {
            if (menuDown)
            {
                botaoSelecionado = (botaoSelecionado + 1) % totalBotoes;
                AudioManager.TocaSelecaoMenu();
            }
            if (menuUp)
            {
                botaoSelecionado--;
                if (botaoSelecionado < 0) botaoSelecionado = totalBotoes - 1;
                AudioManager.TocaSelecaoMenu();
            }
        }

        bool confirmar = podeInteragir && menuConfirm;

        int y = painelY + 100;
        int x = painelX + 20;
        int idx = 0;

        Raylib.DrawText($"Nivel {EstadoJogo.Nivel}   XP {EstadoJogo.Xp}", x, y, 18, Color.White);
        y += 30;

        Raylib.DrawText("Espadas:", x, y, 20, new Color(200, 200, 200, 255));
        y += 28;

        for (int i = 0; i < EstadoJogo.Espadas.Count; i++)
        {
            bool tem = EstadoJogo.EspadasCompradas[i];
            bool equipada = EstadoJogo.EspadaEquipada == i;

            string texto;
            if (!tem) texto = $"{EstadoJogo.Espadas[i].Nome}  (nao comprada)";
            else if (equipada) texto = $"{EstadoJogo.Espadas[i].Nome}  [EQUIPADA]";
            else texto = $"{EstadoJogo.Espadas[i].Nome}  (clique para equipar)";

            if (ForjaUI.Botao(texto, x, y, 420, 28, mouse, tem,
                              idx == botaoSelecionado, confirmar))
            {
                if (tem) EstadoJogo.EspadaEquipada = i;
            }
            y += 32; idx++;
        }

        y += 10;
        Raylib.DrawText("Picareta: (fixa, sempre disponivel)", x, y, 16,
                        new Color(200, 200, 200, 255));
        y += 30;

        Raylib.DrawText("Minerios:", x, y, 20, new Color(200, 200, 200, 255));
        y += 28;

        Raylib.DrawText($"  Cobre: {EstadoJogo.Cobre}", x, y, 18, new Color(220, 160, 120, 255)); y += 24;
        Raylib.DrawText($"  Ferro: {EstadoJogo.Ferro}", x, y, 18, new Color(200, 200, 200, 255)); y += 24;
        Raylib.DrawText($"  Ouro:  {EstadoJogo.Ouro}",  x, y, 18, new Color(255, 230, 100, 255)); y += 30;

        if (ForjaUI.Botao("Fechar", painelX + painelLargura - 120,
                          painelY + painelAltura - 50, 100, 32, mouse, true,
                          idx == botaoSelecionado, confirmar))
        {
            Program.InventarioAberto = false;
        }
    }

    // ============================================================
    // ABA DECK (cartas)
    // ============================================================
    static void DesenhaDeck(InputState[] inputs, Vector2 mouse,
        int painelX, int painelY, int painelLargura, int painelAltura,
        bool podeInteragir, bool menuUp, bool menuDown, bool menuConfirm)
    {
        int y = painelY + 100;
        int x = painelX + 20;

        Raylib.DrawText($"Fila de cartas (usa com H):", x, y, 18,
                        new Color(220, 200, 255, 255));
        y += 30;

        int total = EstadoJogo.FilaCartas.Count;

        if (total == 0)
        {
            Raylib.DrawText("(nenhuma carta - compre na Juju)", x, y, 18,
                            new Color(180, 180, 200, 255));
            y += 40;
        }
        else
        {
            // Navegação
            if (podeInteragir)
            {
                if (menuDown) { cartaSelecionada = (cartaSelecionada + 1) % total; AudioManager.TocaSelecaoMenu(); }
                if (menuUp)
                {
                    cartaSelecionada--;
                    if (cartaSelecionada < 0) cartaSelecionada = total - 1;
                    AudioManager.TocaSelecaoMenu();
                }
            }

            if (cartaSelecionada >= total) cartaSelecionada = total - 1;

            // Enter: marca / troca
            bool confirmar = podeInteragir && menuConfirm;

            for (int i = 0; i < total; i++)
            {
                bool selecionada = i == cartaSelecionada;
                bool marcada = i == cartaMarcada;

                Rectangle rect = new Rectangle(x, y + i * 36, 480, 30);

                Color fundo = marcada
                    ? new Color(150, 100, 60, 255)
                    : (selecionada ? new Color(90, 70, 130, 255) : new Color(45, 38, 60, 255));

                Color borda = marcada
                    ? new Color(255, 200, 100, 255)
                    : (selecionada ? new Color(220, 180, 255, 255) : new Color(100, 80, 130, 255));

                bool hoverMouse = Raylib.CheckCollisionPointRec(mouse, rect);
                bool clicadoMouse = hoverMouse && Raylib.IsMouseButtonPressed(MouseButton.Left);
                bool confirmadoControle = selecionada && confirmar;

                if (clicadoMouse || confirmadoControle)
                {
                    if (cartaMarcada < 0)
                    {
                        // Marca
                        cartaMarcada = i;
                        AudioManager.TocaSelecaoMenu();
                    }
                    else if (cartaMarcada == i)
                    {
                        // Desmarca
                        cartaMarcada = -1;
                        AudioManager.TocaCliqueMenu();
                    }
                    else
                    {
                        // Troca
                        var temp = EstadoJogo.FilaCartas[cartaMarcada];
                        EstadoJogo.FilaCartas[cartaMarcada] = EstadoJogo.FilaCartas[i];
                        EstadoJogo.FilaCartas[i] = temp;
                        cartaMarcada = -1;
                        AudioManager.TocaCliqueMenu();
                    }
                }

                Raylib.DrawRectangleRec(rect, fundo);
                Raylib.DrawRectangleLinesEx(rect, selecionada ? 3 : 2, borda);

                string prefixo = marcada ? ">> " : $"{i + 1}. ";
                string texto = $"{prefixo}{EstadoJogo.FilaCartas[i]}";

                Raylib.DrawText(texto, (int)rect.X + 10, (int)rect.Y + 7, 16,
                                selecionada || marcada ? Color.White : new Color(180, 180, 200, 255));
            }

            y += total * 36 + 20;
        }

        // Instruções
        Raylib.DrawText("Enter: marcar / trocar   (Esc cancela marcacao)", x, y, 14,
                        new Color(160, 160, 180, 255));

        // Fechar
        if (ForjaUI.Botao("Fechar", painelX + painelLargura - 120,
                          painelY + painelAltura - 50, 100, 32, mouse, true,
                          false, false))
        {
            Program.InventarioAberto = false;
        }
    }
}