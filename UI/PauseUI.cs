using System;
using Raylib_cs;

namespace KileIsland;

public enum AcaoPausa { Nada, Continuar, Salvar, Sair }

public static class PauseUI
{
    static int selecionado;
    static string mensagem = "";
    static float tempoMensagem;

    // 0 Continuar | 1 Salvar | 2 Volume musica | 3 Volume efeitos | 4 Sair
    static readonly string[] Opcoes = { "Continuar", "Salvar jogo", "", "", "Sair (sem salvar)" };

    public static void Abre()
    {
        selecionado = 0;
        mensagem = "";
        tempoMensagem = 0f;
    }

    public static void MostraMensagem(string texto)
    {
        mensagem = texto;
        tempoMensagem = 2f;
    }

    // Recebe o input de menu combinado dos dois jogadores.
    public static AcaoPausa Atualiza(InputState input)
    {
        if (tempoMensagem > 0f) tempoMensagem -= Raylib.GetFrameTime();

        if (input.MenuDown)
        {
            selecionado = (selecionado + 1) % Opcoes.Length;
            AudioManager.TocaSelecaoMenu();
        }
        if (input.MenuUp)
        {
            selecionado = (selecionado - 1 + Opcoes.Length) % Opcoes.Length;
            AudioManager.TocaSelecaoMenu();
        }

        // Esc / botao B: volta ao jogo
        if (input.MenuCancel)
        {
            AudioManager.TocaCliqueMenu();
            ConfiguracoesJogo.Salva();
            return AcaoPausa.Continuar;
        }

        if (selecionado == 2)
        {
            if (input.MenuEsquerda) AjustaVolume(musica: true, -0.05f);
            if (input.MenuDireita) AjustaVolume(musica: true, 0.05f);
        }
        else if (selecionado == 3)
        {
            if (input.MenuEsquerda) AjustaVolume(musica: false, -0.05f);
            if (input.MenuDireita) AjustaVolume(musica: false, 0.05f);
        }

        if (!input.MenuConfirm) return AcaoPausa.Nada;

        switch (selecionado)
        {
            case 0:
                AudioManager.TocaCliqueMenu();
                ConfiguracoesJogo.Salva();
                return AcaoPausa.Continuar;
            case 1:
                AudioManager.TocaCliqueMenu();
                return AcaoPausa.Salvar;
            case 4:
                AudioManager.TocaCliqueMenu();
                ConfiguracoesJogo.Salva();
                return AcaoPausa.Sair;
        }

        return AcaoPausa.Nada;
    }

    static void AjustaVolume(bool musica, float delta)
    {
        float atual = musica ? ConfiguracoesJogo.VolumeMusica : ConfiguracoesJogo.VolumeEfeitos;
        float novo = Math.Clamp(atual + delta, 0f, 1f);
        if (novo == atual) return;

        if (musica) ConfiguracoesJogo.VolumeMusica = novo;
        else ConfiguracoesJogo.VolumeEfeitos = novo;

        ConfiguracoesJogo.AplicaVolumes();
        AudioManager.TocaSelecaoMenu();
    }

    public static void Desenha()
    {
        Raylib.DrawRectangle(0, 0, Program.LarguraTela, Program.AlturaTela,
                             new Color(0, 0, 0, 170));

        Centraliza("PAUSADO", 100, 56, Color.White);

        int y = 210;
        for (int i = 0; i < Opcoes.Length; i++)
        {
            string texto = i switch
            {
                2 => $"Volume Música: {(int)(ConfiguracoesJogo.VolumeMusica * 100)}%",
                3 => $"Volume Efeitos: {(int)(ConfiguracoesJogo.VolumeEfeitos * 100)}%",
                _ => Opcoes[i],
            };

            bool ativo = i == selecionado;
            if (ativo) texto = $"> {texto} <";

            Centraliza(texto, y, 28, ativo ? Color.Yellow : Color.White);
            y += 46;
        }

        if (tempoMensagem > 0f)
            Centraliza(mensagem, Program.AlturaTela - 76, 22, Color.Green);

        Centraliza("Setas: navegar   Esq/Dir: volume   Enter: selecionar   Esc: continuar",
                   Program.AlturaTela - 40, 18, Color.Gray);
    }

    static void Centraliza(string texto, int y, int tam, Color cor)
    {
        int largura = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto, (Program.LarguraTela - largura) / 2, y, tam, cor);
    }
}