using System.IO;
using System.Collections.Generic;
using Raylib_cs;

namespace KileIsland;

// Gerencia música e efeitos sonoros. Registre seus sons com RegistraEfeito
// e toque com TocaEfeito pra eles respeitarem o volume das configurações.
public static class AudioManager
{
    static bool inicializado;
    static Music musicaAtual;
    static Music proximaMusica;
    static bool temMusica;
    static bool temProximaMusica;
    static string caminhoMusicaAtual = "";
    static string caminhoProximaMusica = "";
    static float tempoFade;
    static float duracaoFadeTotal;
    static float progressoFade;
    static readonly List<Sound> efeitosRegistrados = new();
    static Sound somMenuClicar;
    static Sound somMenuSelecionar;
    static bool temSomMenuClicar;
    static bool temSomMenuSelecionar;

    public static void Inicia()
    {
        if (inicializado)
            return;

        Raylib.InitAudioDevice();
        inicializado = true;
    }

    public static void CarregaSonsMenu()
    {
        if (File.Exists("Assets/audio/menus/clicar.mp3"))
        {
            somMenuClicar = RegistraEfeito("Assets/audio/menus/clicar.mp3");
            temSomMenuClicar = true;
        }

        if (File.Exists("Assets/audio/menus/selecionar.mp3"))
        {
            somMenuSelecionar = RegistraEfeito("Assets/audio/menus/selecionar.mp3");
            temSomMenuSelecionar = true;
        }
    }

    public static void TocaCliqueMenu()
    {
        if (temSomMenuClicar)
            TocaEfeito(somMenuClicar);
    }

    public static void TocaSelecaoMenu()
    {
        if (temSomMenuSelecionar)
            TocaEfeito(somMenuSelecionar);
    }

    // Troca a música atual. Chame de novo com um caminho diferente pra trocar de faixa
    // (ex: uma música no menu, outra durante o jogo).
    public static void TocaMusica(string caminho, bool loop = true, float duracaoFade = 3f)
    {
        if (!File.Exists(caminho)) return;
        if (!inicializado) Inicia();

        if (temMusica && caminhoMusicaAtual == caminho && !temProximaMusica)
            return;
        if (temProximaMusica && caminhoProximaMusica == caminho)
            return;

        if (temProximaMusica)
            Raylib.UnloadMusicStream(proximaMusica);

        proximaMusica = Raylib.LoadMusicStream(caminho);
        proximaMusica.Looping = loop;
        Raylib.PlayMusicStream(proximaMusica);
        Raylib.SetMusicVolume(proximaMusica, 0f);

        caminhoProximaMusica = caminho;
        temProximaMusica = true;
        tempoFade = 0f;
        duracaoFadeTotal = Math.Max(0f, duracaoFade);
        progressoFade = 0f;

        if (duracaoFadeTotal == 0f)
            FinalizaFadeMusica();
    }

    public static void PausaMusica()
    {
        if (temMusica) Raylib.PauseMusicStream(musicaAtual);
        if (temProximaMusica) Raylib.PauseMusicStream(proximaMusica);
    }

    public static void RetomaMusica()
    {
        if (temMusica) Raylib.ResumeMusicStream(musicaAtual);
        if (temProximaMusica) Raylib.ResumeMusicStream(proximaMusica);
    }

    // Chame uma vez por frame (o loop do menu e o loop do jogo já fazem isso).
    public static void AtualizaMusica()
    {
        float delta = Raylib.GetFrameTime();
        if (temMusica)
            Raylib.UpdateMusicStream(musicaAtual);

        if (!temProximaMusica)
            return;

        Raylib.UpdateMusicStream(proximaMusica);
        tempoFade += delta;
        progressoFade = duracaoFadeTotal <= 0f
            ? 1f
            : Math.Clamp(tempoFade / duracaoFadeTotal, 0f, 1f);

        if (temMusica)
            Raylib.SetMusicVolume(musicaAtual,
                ConfiguracoesJogo.VolumeMusica * (1f - progressoFade));
        Raylib.SetMusicVolume(proximaMusica,
            ConfiguracoesJogo.VolumeMusica * progressoFade);

        if (progressoFade >= 1f)
            FinalizaFadeMusica();
    }

    static void FinalizaFadeMusica()
    {
        if (temMusica)
            Raylib.UnloadMusicStream(musicaAtual);

        musicaAtual = proximaMusica;
        caminhoMusicaAtual = caminhoProximaMusica;
        temMusica = true;
        temProximaMusica = false;
        caminhoProximaMusica = "";
        tempoFade = 0f;
        duracaoFadeTotal = 0f;
        progressoFade = 0f;
        Raylib.SetMusicVolume(musicaAtual, ConfiguracoesJogo.VolumeMusica);
    }

    public static Sound RegistraEfeito(string caminho)
    {
        if (!inicializado) Inicia();

        Sound som = Raylib.LoadSound(caminho);
        Raylib.SetSoundVolume(som, ConfiguracoesJogo.VolumeEfeitos);
        efeitosRegistrados.Add(som);
        return som;
    }

    public static void TocaEfeito(Sound som)
    {
        Raylib.PlaySound(som);
    }

    public static void AplicaVolumes(float volumeMusica, float volumeEfeitos)
    {
        if (temMusica)
            Raylib.SetMusicVolume(musicaAtual,
                volumeMusica * (temProximaMusica ? 1f - progressoFade : 1f));
        if (temProximaMusica)
            Raylib.SetMusicVolume(proximaMusica, volumeMusica * progressoFade);

        foreach (var som in efeitosRegistrados)
            Raylib.SetSoundVolume(som, volumeEfeitos);
    }
}
