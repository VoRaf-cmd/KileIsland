using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class InventarioUI
{
    static int botaoSelecionado = 0;

    public static void ResetarSelecao() { botaoSelecionado = 0; }

    public static void Desenha(InputState[] inputs, bool podeInteragir)
    {
        int painelLargura = 480;
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

        int totalBotoes = EstadoJogo.Espadas.Count + 1;

        // Combina input dos 2 jogadores
        bool menuDown = inputs[0].MenuDown || inputs[1].MenuDown;
        bool menuUp = inputs[0].MenuUp || inputs[1].MenuUp;
        bool menuCancel = inputs[0].MenuCancel || inputs[1].MenuCancel;
        bool menuConfirm = inputs[0].MenuConfirm || inputs[1].MenuConfirm;

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
            if (menuCancel)
            {
                AudioManager.TocaCliqueMenu();
                Program.InventarioAberto = false;
                return;
            }
        }

        bool confirmar = podeInteragir && menuConfirm;

        int y = painelY + 60;
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
}