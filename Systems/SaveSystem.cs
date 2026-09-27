using System;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace KileIsland;

public class DadosSave
{
    public DateTime DataSalvo { get; set; } = DateTime.Now;

    public int Nivel { get; set; }
    public int Xp { get; set; }
    public int XpProximoNivel { get; set; }
    public int Dia { get; set; }
    public int Moedas { get; set; }
    public bool EhDia { get; set; }
    public int NoitesSobrevividas { get; set; }
    public int VidaMaxPontos { get; set; }
    public int VidaPontos { get; set; }
    public int Cobre { get; set; }
    public int Ferro { get; set; }
    public int Ouro { get; set; }
    public ItemEquipado ItemAtual { get; set; }
    public bool[] EspadasCompradas { get; set; } = new bool[4];
    public int EspadaEquipada { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
}

public static class SaveSystem
{
    public const int MaxSlots = 3;

    static string Caminho(int slot) => Path.Combine("Dados", $"save{slot + 1}.json");

    public static DadosSave[] CarregaTodos()
    {
        DadosSave[] slots = new DadosSave[MaxSlots];

        for (int i = 0; i < MaxSlots; i++)
            slots[i] = Carrega(i);

        return slots;
    }

    public static DadosSave Carrega(int slot)
    {
        try
        {
            string caminho = Caminho(slot);
            if (!File.Exists(caminho))
                return null;

            string json = File.ReadAllText(caminho);
            return JsonSerializer.Deserialize<DadosSave>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Salva(int slot, Jogador jogador)
    {
        var dados = new DadosSave
        {
            DataSalvo = DateTime.Now,
            Nivel = EstadoJogo.Nivel,
            Xp = EstadoJogo.Xp,
            XpProximoNivel = EstadoJogo.XpProximoNivel,
            Dia = EstadoJogo.Dia,
            Moedas = EstadoJogo.Moedas,
            EhDia = EstadoJogo.EhDia,
            NoitesSobrevividas = EstadoJogo.NoitesSobrevividas,
            VidaMaxPontos = EstadoJogo.VidaMaxPontos,
            VidaPontos = EstadoJogo.VidaPontos,
            Cobre = EstadoJogo.Cobre,
            Ferro = EstadoJogo.Ferro,
            Ouro = EstadoJogo.Ouro,
            ItemAtual = EstadoJogo.ItemAtual,
            EspadasCompradas = (bool[])EstadoJogo.EspadasCompradas.Clone(),
            EspadaEquipada = EstadoJogo.EspadaEquipada,
            PosX = jogador.Pos.X,
            PosY = jogador.Pos.Y,
        };

        try
        {
            string json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Caminho(slot), json);
        }
        catch
        {
            // Falha ao salvar em disco: o jogo continua rodando normalmente mesmo assim.
        }
    }

    public static void Apaga(int slot)
    {
        try
        {
            string caminho = Caminho(slot);
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
        catch
        {
        }
    }

    public static void Aplica(DadosSave dados, Jogador jogador)
    {
        EstadoJogo.Nivel = dados.Nivel;
        EstadoJogo.Xp = dados.Xp;
        EstadoJogo.XpProximoNivel = dados.XpProximoNivel;
        EstadoJogo.Dia = dados.Dia;
        EstadoJogo.Moedas = dados.Moedas;
        EstadoJogo.EhDia = dados.EhDia;
        EstadoJogo.NoitesSobrevividas = dados.NoitesSobrevividas;
        EstadoJogo.VidaMaxPontos = dados.VidaMaxPontos;
        EstadoJogo.VidaPontos = dados.VidaPontos;
        EstadoJogo.Cobre = dados.Cobre;
        EstadoJogo.Ferro = dados.Ferro;
        EstadoJogo.Ouro = dados.Ouro;
        EstadoJogo.ItemAtual = dados.ItemAtual;
        EstadoJogo.EspadasCompradas = (bool[])dados.EspadasCompradas.Clone();
        EstadoJogo.EspadaEquipada = dados.EspadaEquipada;

        jogador.Pos = new Vector2(dados.PosX, dados.PosY);
    }
}
