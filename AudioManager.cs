using System.Collections.Generic;
using Raylib_cs;

namespace KileIsland;

// Gerencia música e efeitos sonoros. Registre seus sons com RegistraEfeito
// e toque com TocaEfeito pra eles respeitarem o volume das configurações.
public static class AudioManager
{
    static bool inicializado;
    static Music musicaAtual;
    static bool temMusica;
    static readonly List<Sound> efeitosRegistrados = new();

    public static void Inicia()
    {
        if (inicializado)
            return;

        Raylib.InitAudioDevice();
        inicializado = true;
    }

    // Troca a música atual. Chame de novo com um caminho diferente pra trocar de faixa
    // (ex: uma música no menu, outra durante o jogo).
    public static void TocaMusica(string caminho, bool loop = true)
    {
        if (!inicializado) Inicia();

        if (temMusica)
            Raylib.UnloadMusicStream(musicaAtual);

        musicaAtual = Raylib.LoadMusicStream(caminho);
        musicaAtual.Looping = loop;
        Raylib.PlayMusicStream(musicaAtual);
        Raylib.SetMusicVolume(musicaAtual, ConfiguracoesJogo.VolumeMusica);
        temMusica = true;
    }

    public static void PausaMusica()
    {
        if (temMusica) Raylib.PauseMusicStream(musicaAtual);
    }

    public static void RetomaMusica()
    {
        if (temMusica) Raylib.ResumeMusicStream(musicaAtual);
    }

    // Chame uma vez por frame (o loop do menu e o loop do jogo já fazem isso).
    public static void AtualizaMusica()
    {
        if (temMusica)
            Raylib.UpdateMusicStream(musicaAtual);
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
            Raylib.SetMusicVolume(musicaAtual, volumeMusica);

        foreach (var som in efeitosRegistrados)
            Raylib.SetSoundVolume(som, volumeEfeitos);
    }
}
