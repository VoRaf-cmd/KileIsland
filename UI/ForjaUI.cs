using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public static class ForjaUI
{
    static int botaoSelecionado = 0;
    static List<int> IdsBotoes = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };

    public static void ResetarSelecao() { botaoSelecionado = 0; }

    public static void Desenha(InputState[] inputs, bool podeInteragir)
    {
        int painelLargura = 520;
        int painelAltura = 460;
        int painelX = (Program.LarguraTela - painelLargura) / 2;
        int painelY = (Program.AlturaTela - painelAltura) / 2;

        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(0, 0, 0, 120));
        Raylib.DrawRectangle(painelX, painelY, painelLargura, painelAltura,
                             new Color(45, 35, 25, 245));
        Raylib.DrawRectangleLinesEx(new Rectangle(painelX, painelY, painelLargura, painelAltura),
                                    3, new Color(200, 160, 90, 255));

        Vector2 mouse = Raylib.GetMousePosition();

        Raylib.DrawText("FORJA", painelX + 20, painelY + 15, 32, new Color(255, 220, 90, 255));
        Raylib.DrawText($"Nivel {EstadoJogo.Nivel}   Moedas {EstadoJogo.Moedas}   XP {EstadoJogo.Xp}",
                        painelX + 20, painelY + 55, 18, Color.White);

        // Combina input dos 2 jogadores
        bool menuDown = inputs[0].MenuDown || inputs[1].MenuDown;
        bool menuUp = inputs[0].MenuUp || inputs[1].MenuUp;
        bool menuCancel = inputs[0].MenuCancel || inputs[1].MenuCancel;
        bool menuConfirm = inputs[0].MenuConfirm || inputs[1].MenuConfirm;

        if (podeInteragir)
        {
            if (menuDown)
            {
                botaoSelecionado = (botaoSelecionado + 1) % IdsBotoes.Count;
                AudioManager.TocaSelecaoMenu();
            }
            if (menuUp)
            {
                botaoSelecionado--;
                if (botaoSelecionado < 0) botaoSelecionado = IdsBotoes.Count - 1;
                AudioManager.TocaSelecaoMenu();
            }
            if (menuCancel)
            {
                AudioManager.TocaCliqueMenu();
                Program.ForjaAberta = false;
                return;
            }
        }

        bool confirmar = podeInteragir && menuConfirm;

        int idx = 0;
        int y = painelY + 90;

        Raylib.DrawText("Vender:", painelX + 20, y, 22, new Color(200, 200, 200, 255));
        y += 30;

        if (Botao($"Cobre  x{EstadoJogo.Cobre}   ({EstadoJogo.PrecoCobre}$ cada)",
                  painelX + 20, y, 480, 32, mouse, EstadoJogo.Cobre > 0,
                  idx == botaoSelecionado, confirmar))
        {
            if (EstadoJogo.Cobre > 0) { EstadoJogo.Cobre--; EstadoJogo.Moedas += EstadoJogo.PrecoCobre; }
        }
        y += 38; idx++;

        if (Botao($"Ferro  x{EstadoJogo.Ferro}   ({EstadoJogo.PrecoFerro}$ cada)",
                  painelX + 20, y, 480, 32, mouse, EstadoJogo.Ferro > 0,
                  idx == botaoSelecionado, confirmar))
        {
            if (EstadoJogo.Ferro > 0) { EstadoJogo.Ferro--; EstadoJogo.Moedas += EstadoJogo.PrecoFerro; }
        }
        y += 38; idx++;

        if (Botao($"Ouro   x{EstadoJogo.Ouro}   ({EstadoJogo.PrecoOuro}$ cada)",
                  painelX + 20, y, 480, 32, mouse, EstadoJogo.Ouro > 0,
                  idx == botaoSelecionado, confirmar))
        {
            if (EstadoJogo.Ouro > 0) { EstadoJogo.Ouro--; EstadoJogo.Moedas += EstadoJogo.PrecoOuro; }
        }
        y += 50; idx++;

        Raylib.DrawText("Comprar espada:", painelX + 20, y, 22, new Color(200, 200, 200, 255));
        y += 30;

        for (int i = 0; i < EstadoJogo.Espadas.Count; i++)
        {
            var e = EstadoJogo.Espadas[i];
            bool desbloqueada = EstadoJogo.Nivel >= e.Nivel;
            bool jaTem = EstadoJogo.EspadasCompradas[i];
            bool podeComprar = desbloqueada && !jaTem
                               && EstadoJogo.Moedas >= e.Preco
                               && EstadoJogo.Xp >= e.XP;

            string texto;
            if (jaTem) texto = $"{e.Nome}  -  COMPRADA";
            else if (!desbloqueada) texto = $"{e.Nome}  (precisa Nv {e.Nivel})";
            else texto = $"{e.Nome}  -  {e.Preco}$ + {e.XP} XP  (dano {e.Dano})";

            if (Botao(texto, painelX + 20, y, 480, 30, mouse, podeComprar,
                      idx == botaoSelecionado, confirmar))
            {
                if (podeComprar)
                {
                    EstadoJogo.Moedas -= e.Preco;
                    EstadoJogo.Xp -= e.XP;
                    EstadoJogo.EspadasCompradas[i] = true;
                    Efeitos.Shake(6f, 0.2f);
                }
            }
            y += 36; idx++;
        }

        if (Botao("Fechar", painelX + painelLargura - 120, painelY + painelAltura - 50,
                  100, 32, mouse, true,
                  idx == botaoSelecionado, confirmar))
        {
            Program.ForjaAberta = false;
        }
    }

    public static bool Botao(string texto, int x, int y, int largura, int altura,
                             Vector2 mouse, bool ativo,
                             bool selecionadoControle, bool confirmaControle)
    {
        Rectangle rect = new Rectangle(x, y, largura, altura);
        bool hoverMouse = Raylib.CheckCollisionPointRec(mouse, rect) && ativo;

        bool clicadoMouse = hoverMouse && Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool confirmadoControle = selecionadoControle && confirmaControle && ativo;

        bool destacado = selecionadoControle || hoverMouse;

        Color corFundo;
        if (!ativo) corFundo = new Color(40, 35, 30, 255);
        else if (destacado) corFundo = new Color(110, 85, 50, 255);
        else corFundo = new Color(60, 45, 30, 255);

        Color corBorda = ativo
            ? (destacado ? new Color(255, 220, 120, 255) : new Color(220, 180, 100, 255))
            : new Color(90, 80, 70, 255);

        Color corTexto = ativo ? Color.White : new Color(140, 130, 120, 255);

        Raylib.DrawRectangleRec(rect, corFundo);
        Raylib.DrawRectangleLinesEx(rect, destacado ? 3 : 2, corBorda);

        int tamTexto = 18;
        int larguraTexto = Raylib.MeasureText(texto, tamTexto);
        Raylib.DrawText(texto,
            x + (largura - larguraTexto) / 2,
            y + (altura - tamTexto) / 2,
            tamTexto, corTexto);

        bool ativado = clicadoMouse || confirmadoControle;
        if (ativado)
            AudioManager.TocaCliqueMenu();

        return ativado;
    }
}