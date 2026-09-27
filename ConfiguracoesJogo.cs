using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Raylib_cs;

namespace KileIsland;

public enum AcaoJogo
{
    Interagir,
    Inventario,
    TrocarItem,
    Atacar,     // só faz sentido pro controle - no teclado o ataque é no mouse
    Fechar
}

class ConfiguracoesSalvas
{
    public float VolumeMusica { get; set; } = 0.1f;
    public float VolumeEfeitos { get; set; } = 0.6f;
    public Dictionary<string, int> TeclasTeclado { get; set; } = new();
    public Dictionary<string, int> BotoesControle { get; set; } = new();
}

public static class ConfiguracoesJogo
{
    const string CaminhoArquivo = "configuracoes.json";

    public static float VolumeMusica = 0.1f;
    public static float VolumeEfeitos = 0.6f;

    public static readonly Dictionary<AcaoJogo, KeyboardKey> Teclado = new()
    {
        { AcaoJogo.Interagir,  KeyboardKey.E },
        { AcaoJogo.Inventario, KeyboardKey.I },
        { AcaoJogo.TrocarItem, KeyboardKey.Q },
        { AcaoJogo.Fechar,     KeyboardKey.Escape },
    };

    public static readonly Dictionary<AcaoJogo, GamepadButton> Controle = new()
    {
        { AcaoJogo.Interagir,  GamepadButton.RightFaceDown },
        { AcaoJogo.Inventario, GamepadButton.MiddleLeft },
        { AcaoJogo.TrocarItem, GamepadButton.RightFaceUp },
        { AcaoJogo.Atacar,     GamepadButton.RightFaceLeft },
        { AcaoJogo.Fechar,     GamepadButton.RightFaceRight },
    };

    public static void Carrega()
    {
        try
        {
            if (File.Exists(CaminhoArquivo))
            {
                string json = File.ReadAllText(CaminhoArquivo);
                ConfiguracoesSalvas dados = JsonSerializer.Deserialize<ConfiguracoesSalvas>(json);

                if (dados != null)
                {
                    VolumeMusica = dados.VolumeMusica;
                    VolumeEfeitos = dados.VolumeEfeitos;

                    foreach (AcaoJogo acao in Enum.GetValues(typeof(AcaoJogo)))
                    {
                        if (dados.TeclasTeclado.TryGetValue(acao.ToString(), out int tecla))
                            Teclado[acao] = (KeyboardKey)tecla;

                        if (dados.BotoesControle.TryGetValue(acao.ToString(), out int botao))
                            Controle[acao] = (GamepadButton)botao;
                    }
                }
            }
        }
        catch
        {
            // Arquivo corrompido ou ilegível: mantém os padrões.
        }

        AplicaVolumes();
    }

    public static void Salva()
    {
        var dados = new ConfiguracoesSalvas
        {
            VolumeMusica = VolumeMusica,
            VolumeEfeitos = VolumeEfeitos
        };

        foreach (var par in Teclado)
            dados.TeclasTeclado[par.Key.ToString()] = (int)par.Value;

        foreach (var par in Controle)
            dados.BotoesControle[par.Key.ToString()] = (int)par.Value;

        try
        {
            string json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CaminhoArquivo, json);
        }
        catch
        {
            // Sem permissão de escrita, por exemplo: o jogo continua normalmente, só não persiste.
        }
    }

    public static void AplicaVolumes()
    {
        AudioManager.AplicaVolumes(VolumeMusica, VolumeEfeitos);
    }

    public static void RestauraPadrao()
    {
        Teclado[AcaoJogo.Interagir] = KeyboardKey.E;
        Teclado[AcaoJogo.Inventario] = KeyboardKey.I;
        Teclado[AcaoJogo.TrocarItem] = KeyboardKey.Q;
        Teclado[AcaoJogo.Fechar] = KeyboardKey.Escape;

        Controle[AcaoJogo.Interagir] = GamepadButton.RightFaceDown;
        Controle[AcaoJogo.Inventario] = GamepadButton.MiddleLeft;
        Controle[AcaoJogo.TrocarItem] = GamepadButton.RightFaceUp;
        Controle[AcaoJogo.Atacar] = GamepadButton.RightFaceLeft;
        Controle[AcaoJogo.Fechar] = GamepadButton.RightFaceRight;
    }
}
