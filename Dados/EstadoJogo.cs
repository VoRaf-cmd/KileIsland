using System.Collections.Generic;

namespace KileIsland;

public enum ItemEquipado { Espada, Picareta }
public enum TipoMinerio { Cobre, Ferro, Ouro }

public class Espada
{
    public string Nome = "";
    public int Nivel;
    public int Preco;
    public int XP;
    public int Dano;
}

public static class EstadoJogo
{
    public static int Nivel = 1;
    public static int Xp = 0;
    public static int XpProximoNivel = 100;
    public static int Dia = 1;
    public static int Moedas = 0;
    public static bool EhDia = true;

    // Quantas noites já passaram (incrementa ao amanhecer)
    public static int NoitesSobrevividas = 0;

    // Vida por pontos (4 pontos = 1 coração)
    public static int VidaMaxPontos = 12;
    public static int VidaPontos = 12;

    // Minérios
    public static int Cobre = 0;
    public static int Ferro = 0;
    public static int Ouro  = 0;

    public const int PrecoCobre = 10;
    public const int PrecoFerro = 25;
    public const int PrecoOuro  = 60;

    public static ItemEquipado ItemAtual = ItemEquipado.Picareta;

    public static bool[] EspadasCompradas = new bool[4];
    public static int EspadaEquipada = -1;

    public static List<Espada> Espadas = new List<Espada>()
    {
        new Espada { Nome = "Madeira", Nivel = 1, Preco = 50,   XP = 0,   Dano = 5  },
        new Espada { Nome = "Pedra",   Nivel = 3, Preco = 150,  XP = 50,  Dano = 10 },
        new Espada { Nome = "Ferro",   Nivel = 5, Preco = 400,  XP = 150, Dano = 18 },
        new Espada { Nome = "Ouro",    Nivel = 8, Preco = 1000, XP = 400, Dano = 30 },
    };

    public static void Resetar()
    {
        Nivel = 1;
        Xp = 0;
        XpProximoNivel = 100;
        Dia = 1;
        Moedas = 0;
        EhDia = true;
        NoitesSobrevividas = 0;
        VidaPontos = VidaMaxPontos;
        Cobre = 0;
        Ferro = 0;
        Ouro = 0;
        ItemAtual = ItemEquipado.Picareta;
        EspadasCompradas = new bool[4];
        EspadaEquipada = -1;
    }

    // Ganha XP e sobe de nível se necessário
    public static void GanhaXp(int quantidade)
    {
        Xp += quantidade;
        while (Xp >= XpProximoNivel)
        {
            Xp -= XpProximoNivel;
            Nivel++;
            XpProximoNivel += 100;
            LevelUpAviso.Avisa($"Nível {Nivel}!");
        }
    }
}

// Aviso global de level up (usado pelo HUD)
public static class LevelUpAviso
{
    public static string Texto = "";
    public static float Tempo = 0f;
    public const float Duracao = 2f;

    public static void Avisa(string texto)
    {
        Texto = texto;
        Tempo = Duracao;
    }

    public static void Atualiza(float delta)
    {
        if (Tempo > 0f) Tempo -= delta;
    }
}