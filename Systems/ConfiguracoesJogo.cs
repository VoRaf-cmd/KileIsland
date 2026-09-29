using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Raylib_cs;

namespace KileIsland;

public enum AcaoJogo
{
    Interagir,
    Inventario,
    TrocarItem,
    Atacar,
    Fechar
}

class ConfiguracoesSalvas
{
    public float VolumeMusica { get; set; } = 0.1f;
    public float VolumeEfeitos { get; set; } = 0.6f;

    // Novos formatos (2 jogadores)
    public Dictionary<string, int> TeclasTeclado1 { get; set; } = new();
    public Dictionary<string, int> TeclasTeclado2 { get; set; } = new();
    public Dictionary<string, int> BotoesControle1 { get; set; } = new();
    public Dictionary<string, int> BotoesControle2 { get; set; } = new();

    // Compatibilidade com saves antigos (1 jogador)
    public Dictionary<string, int>? TeclasTeclado { get; set; }
    public Dictionary<string, int>? BotoesControle { get; set; }
}

public static class ConfiguracoesJogo
{
    public static readonly string CaminhoArquivo =
        Path.Combine("Dados", "configuracoes.json");

    public static float VolumeMusica = 0.1f;
    public static float VolumeEfeitos = 0.6f;

    // Teclas de movimento fixas (P1 = WASD, P2 = setas). Nao podem ser usadas em acoes,
    // senao o jogador andaria ao mesmo tempo em que executa a acao.
    static readonly KeyboardKey[] TeclasDeMovimento =
    {
        KeyboardKey.W, KeyboardKey.A, KeyboardKey.S, KeyboardKey.D,
        KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Left, KeyboardKey.Right
    };

    // Bindings do jogador 1 (teclado)
    public static readonly Dictionary<AcaoJogo, KeyboardKey> Teclado1 = new()
    {
        { AcaoJogo.Interagir,  KeyboardKey.E },
        { AcaoJogo.Inventario, KeyboardKey.I },
        { AcaoJogo.TrocarItem, KeyboardKey.Q },
        { AcaoJogo.Atacar,     KeyboardKey.Space },       // P1 tambem pode atacar com espaco
        { AcaoJogo.Fechar,     KeyboardKey.Escape },
    };

    // Bindings do jogador 2 (teclado) - padrao: setas + teclas da direita
    public static readonly Dictionary<AcaoJogo, KeyboardKey> Teclado2 = new()
    {
        { AcaoJogo.Interagir,  KeyboardKey.Enter },
        { AcaoJogo.Inventario, KeyboardKey.T },
        { AcaoJogo.TrocarItem, KeyboardKey.RightShift },
        { AcaoJogo.Atacar,     KeyboardKey.RightControl }, // P2 ataca com Ctrl direito
        { AcaoJogo.Fechar,     KeyboardKey.Escape },
    };

    // Bindings do jogador 1 (controle)
    public static readonly Dictionary<AcaoJogo, GamepadButton> Controle1 = new()
    {
        { AcaoJogo.Interagir,  GamepadButton.RightFaceDown },
        { AcaoJogo.Inventario, GamepadButton.MiddleLeft },
        { AcaoJogo.TrocarItem, GamepadButton.RightFaceUp },
        { AcaoJogo.Atacar,     GamepadButton.RightFaceLeft },
        { AcaoJogo.Fechar,     GamepadButton.RightFaceRight },
    };

    // Bindings do jogador 2 (controle)
    public static readonly Dictionary<AcaoJogo, GamepadButton> Controle2 = new()
    {
        { AcaoJogo.Interagir,  GamepadButton.RightFaceDown },
        { AcaoJogo.Inventario, GamepadButton.MiddleLeft },
        { AcaoJogo.TrocarItem, GamepadButton.RightFaceUp },
        { AcaoJogo.Atacar,     GamepadButton.RightFaceLeft },
        { AcaoJogo.Fechar,     GamepadButton.RightFaceRight },
    };

    // Atalhos para compatibilidade com codigo existente que usa "Teclado" / "Controle"
    public static Dictionary<AcaoJogo, KeyboardKey> Teclado => Teclado1;
    public static Dictionary<AcaoJogo, GamepadButton> Controle => Controle1;

    // ------------------------------------------------------------------
    // Padroes
    // ------------------------------------------------------------------

    static KeyboardKey PadraoTeclado(int jogador, AcaoJogo acao)
    {
        if (jogador == 0)
        {
            return acao switch
            {
                AcaoJogo.Interagir  => KeyboardKey.E,
                AcaoJogo.Inventario => KeyboardKey.I,
                AcaoJogo.TrocarItem => KeyboardKey.Q,
                AcaoJogo.Atacar     => KeyboardKey.Space,
                _                   => KeyboardKey.Escape,
            };
        }

        return acao switch
        {
            AcaoJogo.Interagir  => KeyboardKey.Enter,
            AcaoJogo.Inventario => KeyboardKey.T,
            AcaoJogo.TrocarItem => KeyboardKey.RightShift,
            AcaoJogo.Atacar     => KeyboardKey.RightControl,
            _                   => KeyboardKey.Escape,
        };
    }

    static GamepadButton PadraoControle(AcaoJogo acao)
    {
        return acao switch
        {
            AcaoJogo.Interagir  => GamepadButton.RightFaceDown,
            AcaoJogo.Inventario => GamepadButton.MiddleLeft,
            AcaoJogo.TrocarItem => GamepadButton.RightFaceUp,
            AcaoJogo.Atacar     => GamepadButton.RightFaceLeft,
            _                   => GamepadButton.RightFaceRight,
        };
    }

    static Dictionary<AcaoJogo, KeyboardKey> TecladoDe(int jogador) =>
        jogador == 0 ? Teclado1 : Teclado2;

    static Dictionary<AcaoJogo, GamepadButton> ControleDe(int jogador) =>
        jogador == 0 ? Controle1 : Controle2;

    static AcaoJogo[] TodasAcoes() =>
        (AcaoJogo[])Enum.GetValues(typeof(AcaoJogo));

    public static bool EhTeclaDeMovimento(KeyboardKey tecla) =>
        Array.IndexOf(TeclasDeMovimento, tecla) >= 0;

    // ------------------------------------------------------------------
    // Carregar / salvar
    // ------------------------------------------------------------------

    public static void Carrega()
    {
        try
        {
            if (File.Exists(CaminhoArquivo))
            {
                string json = File.ReadAllText(CaminhoArquivo);
                ConfiguracoesSalvas? dados =
                    JsonSerializer.Deserialize<ConfiguracoesSalvas>(json);

                if (dados != null)
                {
                    VolumeMusica = dados.VolumeMusica;
                    VolumeEfeitos = dados.VolumeEfeitos;

                    // Formato antigo (1 jogador): vale so para o P1.
                    // (Antes era aplicado nos DOIS jogadores, o que deixava P1 e P2
                    // com as mesmas teclas e um atrapalhava o outro.)
                    if (dados.TeclasTeclado != null && dados.TeclasTeclado.Count > 0)
                    {
                        AplicaTeclado(dados.TeclasTeclado, Teclado1);
                    }
                    else
                    {
                        AplicaTeclado(dados.TeclasTeclado1, Teclado1);
                        AplicaTeclado(dados.TeclasTeclado2, Teclado2);
                    }

                    if (dados.BotoesControle != null && dados.BotoesControle.Count > 0)
                    {
                        AplicaControle(dados.BotoesControle, Controle1);
                    }
                    else
                    {
                        AplicaControle(dados.BotoesControle1, Controle1);
                        AplicaControle(dados.BotoesControle2, Controle2);
                    }
                }
            }
        }
        catch
        {
            // Arquivo corrompido: mantem padroes.
        }

        // Garante que um arquivo antigo/editado a mao nao deixe as teclas em conflito.
        ResolveConflitos();

        AplicaVolumes();
    }

    static void AplicaTeclado(Dictionary<string, int> origem, Dictionary<AcaoJogo, KeyboardKey> destino)
    {
        if (origem == null) return;

        foreach (AcaoJogo acao in TodasAcoes())
        {
            if (origem.TryGetValue(acao.ToString(), out int tecla))
                destino[acao] = (KeyboardKey)tecla;
        }
    }

    static void AplicaControle(Dictionary<string, int> origem, Dictionary<AcaoJogo, GamepadButton> destino)
    {
        if (origem == null) return;

        foreach (AcaoJogo acao in TodasAcoes())
        {
            if (origem.TryGetValue(acao.ToString(), out int botao))
                destino[acao] = (GamepadButton)botao;
        }
    }

    public static void Salva()
    {
        var dados = new ConfiguracoesSalvas
        {
            VolumeMusica = VolumeMusica,
            VolumeEfeitos = VolumeEfeitos
        };

        foreach (var par in Teclado1)
            dados.TeclasTeclado1[par.Key.ToString()] = (int)par.Value;
        foreach (var par in Teclado2)
            dados.TeclasTeclado2[par.Key.ToString()] = (int)par.Value;
        foreach (var par in Controle1)
            dados.BotoesControle1[par.Key.ToString()] = (int)par.Value;
        foreach (var par in Controle2)
            dados.BotoesControle2[par.Key.ToString()] = (int)par.Value;

        try
        {
            string? pasta = Path.GetDirectoryName(CaminhoArquivo);
            if (!string.IsNullOrEmpty(pasta))
                Directory.CreateDirectory(pasta);

            string json = JsonSerializer.Serialize(
                dados,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(CaminhoArquivo, json);
        }
        catch
        {
            // Sem permissao de escrita.
        }
    }

    public static void AplicaVolumes()
    {
        AudioManager.AplicaVolumes(VolumeMusica, VolumeEfeitos);
    }

    public static void RestauraPadrao()
    {
        foreach (AcaoJogo acao in TodasAcoes())
        {
            Teclado1[acao]  = PadraoTeclado(0, acao);
            Teclado2[acao]  = PadraoTeclado(1, acao);
            Controle1[acao] = PadraoControle(acao);
            Controle2[acao] = PadraoControle(acao);
        }
    }

    // ------------------------------------------------------------------
    // Conflitos de teclas
    // ------------------------------------------------------------------

    // Define a tecla de uma acao. Retorna false (e nao altera nada) se a tecla for
    // de movimento. Se a tecla ja estiver em uso por outra acao (do mesmo jogador
    // ou do outro), as duas acoes TROCAM de tecla, entao nunca fica duplicada.
    // Unica excecao: "Fechar" pode ser a mesma tecla nos dois jogadores (menus
    // compartilhados), como no padrao (Esc).
    public static bool TentaDefinirTecla(int jogador, AcaoJogo acao, KeyboardKey nova, out string aviso)
    {
        aviso = "";

        if (EhTeclaDeMovimento(nova))
        {
            aviso = $"{nova} e tecla de movimento. Escolha outra.";
            return false;
        }

        var meu = TecladoDe(jogador);
        KeyboardKey antiga = meu[acao];
        if (antiga == nova) return true;

        for (int p = 0; p < 2; p++)
        {
            var dict = TecladoDe(p);
            foreach (AcaoJogo outra in TodasAcoes())
            {
                if (p == jogador && outra == acao) continue;
                if (dict[outra] != nova) continue;
                if (outra == AcaoJogo.Fechar && acao == AcaoJogo.Fechar) continue;

                dict[outra] = antiga;
                aviso = $"{nova} ja era de P{p + 1} ({outra}); as duas teclas foram trocadas.";
            }
        }

        meu[acao] = nova;
        ResolveConflitos();
        return true;
    }

    // Define o botao do controle. Se ja estiver em uso por outra acao do MESMO
    // jogador, as duas trocam de botao. (Controles de jogadores diferentes sao
    // fisicamente separados, entao repetir botao entre P1 e P2 nao e conflito.)
    public static bool TentaDefinirBotao(int jogador, AcaoJogo acao, GamepadButton novo, out string aviso)
    {
        aviso = "";

        var meu = ControleDe(jogador);
        GamepadButton antigo = meu[acao];
        if (antigo == novo) return true;

        foreach (AcaoJogo outra in TodasAcoes())
        {
            if (outra == acao) continue;
            if (meu[outra] != novo) continue;

            meu[outra] = antigo;
            aviso = $"Botao ja era usado em {outra}; os dois foram trocados.";
        }

        meu[acao] = novo;
        return true;
    }

    // Corrige qualquer conflito restante (arquivo antigo, editado a mao, etc.):
    // acoes em conflito voltam para o padrao.
    static void ResolveConflitos()
    {
        // 1) Nenhuma acao pode usar tecla de movimento.
        for (int p = 0; p < 2; p++)
        {
            var dict = TecladoDe(p);
            foreach (AcaoJogo acao in TodasAcoes())
            {
                if (EhTeclaDeMovimento(dict[acao]))
                    dict[acao] = PadraoTeclado(p, acao);
            }
        }

        // 2) Sem teclas duplicadas (mesmo jogador ou jogadores diferentes),
        //    exceto Fechar x Fechar.
        for (int rodada = 0; rodada < 10; rodada++)
        {
            var entradas = new List<(int p, AcaoJogo a, KeyboardKey k)>();
            for (int p = 0; p < 2; p++)
                foreach (AcaoJogo a in TodasAcoes())
                    entradas.Add((p, a, TecladoDe(p)[a]));

            bool mudou = false;
            for (int i = 0; i < entradas.Count && !mudou; i++)
            {
                for (int j = i + 1; j < entradas.Count && !mudou; j++)
                {
                    if (entradas[i].k != entradas[j].k) continue;
                    if (entradas[i].a == AcaoJogo.Fechar && entradas[j].a == AcaoJogo.Fechar) continue;

                    TecladoDe(entradas[i].p)[entradas[i].a] = PadraoTeclado(entradas[i].p, entradas[i].a);
                    TecladoDe(entradas[j].p)[entradas[j].a] = PadraoTeclado(entradas[j].p, entradas[j].a);
                    mudou = true;
                }
            }

            if (!mudou) break;
        }

        // 3) Controles: sem botao duplicado dentro do mesmo jogador.
        for (int p = 0; p < 2; p++)
        {
            var dict = ControleDe(p);
            var acoes = TodasAcoes();
            for (int i = 0; i < acoes.Length; i++)
            {
                for (int j = i + 1; j < acoes.Length; j++)
                {
                    if (dict[acoes[i]] != dict[acoes[j]]) continue;
                    dict[acoes[i]] = PadraoControle(acoes[i]);
                    dict[acoes[j]] = PadraoControle(acoes[j]);
                }
            }
        }
    }
}