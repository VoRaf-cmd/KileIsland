using System;
using System.Collections.Generic;
using Raylib_cs;

namespace KileIsland;

enum TelaMenu { Principal, Slots, Configuracoes }
enum AbaConfig { Audio, Teclado1, Teclado2, Controle1, Controle2 }

public static class MenuUI
{
    static TelaMenu tela = TelaMenu.Principal;
    static AbaConfig aba = AbaConfig.Audio;
    static int selecionado;
    static bool modoContinuar = true;

    static AcaoJogo? capturandoTecla;
    static AcaoJogo? capturandoBotao;
    static int jogadorCapturando = 0;

    static float bloqueioInput;

    // Mensagem mostrada na tela de configuracoes (conflito de teclas, controle ausente...)
    static string avisoConfig = "";

    static DadosSave[] slots = new DadosSave[SaveSystem.MaxSlots];
    static bool slotsCarregados;

    public static bool JogoDeveIniciar { get; private set; }
    public static int SlotAtual { get; private set; } = -1;
    public static bool NovoJogo { get; private set; }
    public static bool CoopAtivo { get; private set; } = false;

    static readonly string[] OpcoesPrincipal = { "Continuar", "Novo Jogo", "Coop Local", "Configurações", "Sair" };

    static readonly GamepadButton[] BotoesCapturaveis =
    {
        GamepadButton.RightFaceDown, GamepadButton.RightFaceRight,
        GamepadButton.RightFaceLeft, GamepadButton.RightFaceUp,
        GamepadButton.LeftTrigger1, GamepadButton.LeftTrigger2,
        GamepadButton.RightTrigger1, GamepadButton.RightTrigger2,
        GamepadButton.MiddleLeft, GamepadButton.MiddleRight,
        GamepadButton.LeftThumb, GamepadButton.RightThumb,
    };

    public static void ReiniciaEstado()
    {
        tela = TelaMenu.Principal;
        selecionado = 0;
        JogoDeveIniciar = false;
        SlotAtual = -1;
        NovoJogo = false;
        capturandoTecla = null;
        capturandoBotao = null;
        slotsCarregados = false;
        CoopAtivo = false;
    }

    public static void Atualiza(InputState input)
    {
        if (!slotsCarregados)
        {
            slots = SaveSystem.CarregaTodos();
            slotsCarregados = true;
        }

        if (bloqueioInput > 0f)
        {
            bloqueioInput -= Raylib.GetFrameTime();
            return;
        }

        if ((input.MenuConfirm || input.MenuCancel) &&
            !capturandoTecla.HasValue && !capturandoBotao.HasValue)
        {
            AudioManager.TocaCliqueMenu();
        }

        switch (tela)
        {
            case TelaMenu.Principal: AtualizaPrincipal(input); break;
            case TelaMenu.Slots: AtualizaSlots(input); break;
            case TelaMenu.Configuracoes: AtualizaConfiguracoes(input); break;
        }
    }

    static void MudaTela(TelaMenu novaTela)
    {
        tela = novaTela;
        selecionado = 0;
        bloqueioInput = 0.15f;
        avisoConfig = "";
    }

    static void AtualizaPrincipal(InputState input)
    {
        if (input.MenuDown) DefineSelecao((selecionado + 1) % OpcoesPrincipal.Length);
        if (input.MenuUp) DefineSelecao((selecionado - 1 + OpcoesPrincipal.Length) % OpcoesPrincipal.Length);

        if (!input.MenuConfirm)
            return;

switch (selecionado)
{
    case 0: modoContinuar = true;  CoopAtivo = false; MudaTela(TelaMenu.Slots); break;
    case 1: modoContinuar = false; CoopAtivo = false; MudaTela(TelaMenu.Slots); break;
    case 2: modoContinuar = false; CoopAtivo = true;  MudaTela(TelaMenu.Slots); break;
    case 3: aba = AbaConfig.Audio; MudaTela(TelaMenu.Configuracoes); break;
    case 4: Environment.Exit(0); break;
}
    }

    static void AtualizaSlots(InputState input)
    {
        if (input.MenuDown) DefineSelecao((selecionado + 1) % SaveSystem.MaxSlots);
        if (input.MenuUp) DefineSelecao((selecionado - 1 + SaveSystem.MaxSlots) % SaveSystem.MaxSlots);

        if (input.MenuCancel)
        {
            MudaTela(TelaMenu.Principal);
            return;
        }

        if (!input.MenuConfirm)
            return;

        bool temSave = slots[selecionado] != null;

        SlotAtual = selecionado;
        NovoJogo = !modoContinuar || !temSave;

        // Continuar um save feito em Coop Local volta como Coop Local.
        if (modoContinuar && temSave)
            CoopAtivo = slots[selecionado].Coop;

        if (NovoJogo)
            EstadoJogo.Resetar();

        JogoDeveIniciar = true;
    }

    static void AtualizaConfiguracoes(InputState input)
    {
        if (capturandoTecla.HasValue) { CapturaTecla(); return; }
        if (capturandoBotao.HasValue) { CapturaBotao(); return; }

        List<string> linhas = MontaLinhasConfig();

        if (input.MenuDown) DefineSelecao((selecionado + 1) % linhas.Count);
        if (input.MenuUp) DefineSelecao((selecionado - 1 + linhas.Count) % linhas.Count);

        if (input.MenuCancel)
        {
            ConfiguracoesJogo.Salva();
            MudaTela(TelaMenu.Principal);
            return;
        }

        if (selecionado == 0)
        {
            if (input.MenuEsquerda) TrocaAba(-1);
            if (input.MenuDireita) TrocaAba(1);
            return;
        }

        if (aba == AbaConfig.Audio)
            AtualizaAbaAudio(input);
        else if (aba == AbaConfig.Teclado1)
            AtualizaAbaBindings(input, AcoesTeclado(), teclado: true, jogador: 0);
        else if (aba == AbaConfig.Teclado2)
            AtualizaAbaBindings(input, AcoesTeclado(), teclado: true, jogador: 1);
        else if (aba == AbaConfig.Controle1)
            AtualizaAbaBindings(input, AcoesControle(), teclado: false, jogador: 0);
        else
            AtualizaAbaBindings(input, AcoesControle(), teclado: false, jogador: 1);
    }

    static void AtualizaAbaAudio(InputState input)
    {
        if (selecionado == 1)
        {
            if (input.MenuEsquerda) AjustaVolumeMusica(-0.05f);
            if (input.MenuDireita) AjustaVolumeMusica(0.05f);
        }
        else if (selecionado == 2)
        {
            if (input.MenuEsquerda) AjustaVolumeEfeitos(-0.05f);
            if (input.MenuDireita) AjustaVolumeEfeitos(0.05f);
        }
        else if (selecionado == 3 && input.MenuConfirm)
        {
            ConfiguracoesJogo.Salva();
            MudaTela(TelaMenu.Principal);
        }
    }

    static void AtualizaAbaBindings(InputState input, AcaoJogo[] acoes, bool teclado, int jogador)
    {
        int indiceAcao = selecionado - 1;

        if (indiceAcao < acoes.Length)
        {
            if (input.MenuConfirm)
            {
                jogadorCapturando = jogador;
                avisoConfig = "";
                if (teclado) capturandoTecla = acoes[indiceAcao];
                else capturandoBotao = acoes[indiceAcao];
            }
            return;
        }

        if (indiceAcao == acoes.Length)
        {
            if (input.MenuConfirm)
            {
                ConfiguracoesJogo.RestauraPadrao();
                ConfiguracoesJogo.Salva();
            }
            return;
        }

        if (input.MenuConfirm)
        {
            ConfiguracoesJogo.Salva();
            MudaTela(TelaMenu.Principal);
        }
    }

    static void TrocaAba(int direcao)
    {
        int total = Enum.GetValues(typeof(AbaConfig)).Length;
        aba = (AbaConfig)(((int)aba + direcao + total) % total);
        selecionado = 0;
        avisoConfig = "";
        AudioManager.TocaSelecaoMenu();
    }

    static void DefineSelecao(int novoSelecionado)
    {
        if (selecionado == novoSelecionado) return;

        selecionado = novoSelecionado;
        AudioManager.TocaSelecaoMenu();
    }

    static void AjustaVolumeMusica(float delta)
    {
        float volumeAnterior = ConfiguracoesJogo.VolumeMusica;
        ConfiguracoesJogo.VolumeMusica = Math.Clamp(volumeAnterior + delta, 0f, 1f);
        if (ConfiguracoesJogo.VolumeMusica == volumeAnterior) return;
        ConfiguracoesJogo.AplicaVolumes();
        AudioManager.TocaSelecaoMenu();
    }

    static void AjustaVolumeEfeitos(float delta)
    {
        float volumeAnterior = ConfiguracoesJogo.VolumeEfeitos;
        ConfiguracoesJogo.VolumeEfeitos = Math.Clamp(volumeAnterior + delta, 0f, 1f);
        if (ConfiguracoesJogo.VolumeEfeitos == volumeAnterior) return;
        ConfiguracoesJogo.AplicaVolumes();
        AudioManager.TocaSelecaoMenu();
    }

static AcaoJogo[] AcoesTeclado()
{
    return new[] { AcaoJogo.Interagir, AcaoJogo.Inventario, AcaoJogo.TrocarItem, AcaoJogo.Atacar, AcaoJogo.Fechar };
}

    static AcaoJogo[] AcoesControle()
    {
        return new[] { AcaoJogo.Interagir, AcaoJogo.Inventario, AcaoJogo.TrocarItem, AcaoJogo.Atacar, AcaoJogo.Fechar };
    }

    static void CapturaTecla()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            capturandoTecla = null;
            avisoConfig = "";
            return;
        }

        int tecla = Raylib.GetKeyPressed();

        if (tecla != 0)
        {
            // Evita tecla repetida entre acoes / jogadores e teclas de movimento.
            if (ConfiguracoesJogo.TentaDefinirTecla(
                    jogadorCapturando, capturandoTecla!.Value, (KeyboardKey)tecla, out string aviso))
            {
                ConfiguracoesJogo.Salva();
                capturandoTecla = null;
            }

            avisoConfig = aviso;
        }
    }

    static void CapturaBotao()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            capturandoBotao = null;
            avisoConfig = "";
            return;
        }

        if (!Raylib.IsGamepadAvailable(jogadorCapturando))
        {
            avisoConfig = $"Controle {jogadorCapturando + 1} nao esta conectado.";
            return;
        }

        foreach (var botao in BotoesCapturaveis)
        {
            if (Raylib.IsGamepadButtonPressed(jogadorCapturando, botao))
            {
                ConfiguracoesJogo.TentaDefinirBotao(
                    jogadorCapturando, capturandoBotao!.Value, botao, out string aviso);
                ConfiguracoesJogo.Salva();
                capturandoBotao = null;
                avisoConfig = aviso;
                return;
            }
        }
    }

    static List<string> MontaLinhasConfig()
    {
        List<string> linhas = new() { $"< Aba: {NomeAba(aba)} >" };

        if (aba == AbaConfig.Audio)
        {
            linhas.Add($"Volume Música: {(int)(ConfiguracoesJogo.VolumeMusica * 100)}%");
            linhas.Add($"Volume Efeitos: {(int)(ConfiguracoesJogo.VolumeEfeitos * 100)}%");
            linhas.Add("Voltar");
        }
        else if (aba == AbaConfig.Teclado1)
        {
            foreach (var acao in AcoesTeclado())
                linhas.Add($"{NomeAcao(acao)}: [{ConfiguracoesJogo.Teclado1[acao]}]");

            linhas.Add("Restaurar padrão");
            linhas.Add("Voltar");
        }
        else if (aba == AbaConfig.Teclado2)
        {
            foreach (var acao in AcoesTeclado())
                linhas.Add($"{NomeAcao(acao)}: [{ConfiguracoesJogo.Teclado2[acao]}]");

            linhas.Add("Restaurar padrão");
            linhas.Add("Voltar");
        }
        else if (aba == AbaConfig.Controle1)
        {
            foreach (var acao in AcoesControle())
                linhas.Add($"{NomeAcao(acao)}: [{ConfiguracoesJogo.Controle1[acao]}]");

            linhas.Add("Restaurar padrão");
            linhas.Add("Voltar");
        }
        else
        {
            foreach (var acao in AcoesControle())
                linhas.Add($"{NomeAcao(acao)}: [{ConfiguracoesJogo.Controle2[acao]}]");

            linhas.Add("Restaurar padrão");
            linhas.Add("Voltar");
        }

        return linhas;
    }

    static string NomeAba(AbaConfig a) => a switch
    {
        AbaConfig.Audio => "Áudio",
        AbaConfig.Teclado1 => "Teclado 1",
        AbaConfig.Teclado2 => "Teclado 2",
        AbaConfig.Controle1 => "Controle 1",
        AbaConfig.Controle2 => "Controle 2",
        _ => a.ToString()
    };

    static string NomeAcao(AcaoJogo a) => a switch
    {
        AcaoJogo.Interagir => "Interagir",
        AcaoJogo.Inventario => "Inventário",
        AcaoJogo.TrocarItem => "Trocar Item",
        AcaoJogo.Atacar => "Atacar",
        AcaoJogo.Fechar => "Fechar/Voltar",
        _ => a.ToString()
    };

    public static void Desenha()
    {
        switch (tela)
        {
            case TelaMenu.Principal: DesenhaPrincipal(); break;
            case TelaMenu.Slots: DesenhaSlots(); break;
            case TelaMenu.Configuracoes: DesenhaConfiguracoes(); break;
        }
    }

    static void DesenhaPrincipal()
    {
        DesenhaTitulo("KileIsland");

        int y = 260;
        for (int i = 0; i < OpcoesPrincipal.Length; i++)
        {
            bool ativo = i == selecionado;
            Color cor = ativo ? Color.Yellow : Color.White;
            string texto = ativo ? $"> {OpcoesPrincipal[i]} <" : OpcoesPrincipal[i];
            DesenhaLinhaCentralizada(texto, y, 28, cor);
            y += 46;
        }
    }

    static void DesenhaSlots()
    {
        DesenhaTitulo(modoContinuar ? "Continuar" : (CoopAtivo ? "Novo Jogo (Coop)" : "Novo Jogo"));

        int y = 200;
        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            bool ativo = i == selecionado;
            Color cor = ativo ? Color.Yellow : Color.White;

            string texto = slots[i] != null
                ? $"Slot {i + 1} - Nível {slots[i].Nivel}, Dia {slots[i].Dia}{(slots[i].Coop ? " (Coop)" : "")}"
                : $"Slot {i + 1} - Vazio";

            DesenhaLinhaCentralizada(texto, y, 26, cor);
            y += 44;
        }

        DesenhaRodape("Enter: selecionar   Esc: voltar");
    }

    static void DesenhaConfiguracoes()
    {
        DesenhaTitulo("Configurações");

        List<string> linhas = MontaLinhasConfig();

        int y = 180;
        for (int i = 0; i < linhas.Count; i++)
        {
            bool ativo = i == selecionado;
            Color cor = ativo ? Color.Yellow : Color.White;
            DesenhaLinhaCentralizada(linhas[i], y, 24, cor);
            y += 40;
        }

        if (avisoConfig.Length > 0)
        {
            int tamAviso = 20;
            int larguraAviso = Raylib.MeasureText(avisoConfig, tamAviso);
            Raylib.DrawText(avisoConfig, (Program.LarguraTela - larguraAviso) / 2,
                            Program.AlturaTela - 72, tamAviso, Color.Orange);
        }

        if (capturandoTecla.HasValue)
            DesenhaRodape($"Pressione uma tecla para \"{NomeAcao(capturandoTecla.Value)}\" (Esc cancela)");
        else if (capturandoBotao.HasValue)
            DesenhaRodape($"Pressione um botão do controle para \"{NomeAcao(capturandoBotao.Value)}\" (Esc cancela)");
        else
            DesenhaRodape("Setas: navegar   Esq/Dir: trocar aba   Enter: selecionar   Esc: voltar");
    }

    static void DesenhaTitulo(string texto)
    {
        int tam = 48;
        int largura = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto, (Program.LarguraTela - largura) / 2, 90, tam, Color.White);
    }

    static void DesenhaLinhaCentralizada(string texto, int y, int tamanho, Color cor)
    {
        int largura = Raylib.MeasureText(texto, tamanho);
        Raylib.DrawText(texto, (Program.LarguraTela - largura) / 2, y, tamanho, cor);
    }

    static void DesenhaRodape(string texto)
    {
        int tam = 18;
        int largura = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto, (Program.LarguraTela - largura) / 2, Program.AlturaTela - 40, tam, Color.Gray);
    }
}