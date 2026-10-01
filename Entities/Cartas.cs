namespace KileIsland;

public enum TipoCarta
{
    Empadinha,
    CoracaoExtra,
    Velocidade
}

public class Carta
{
    public TipoCarta Tipo;
    public string Nome = "";
    public string Descricao = "";
    public int Preco;
    public bool Unica;

    public static Carta Cria(TipoCarta tipo)
    {
        Carta c = new Carta();
        c.Tipo = tipo;

        switch (tipo)
        {
            case TipoCarta.Empadinha:
                c.Nome = "Empadinha";
                c.Descricao = "Cura 4 de vida";
                c.Preco = 30;
                c.Unica = false;
                break;

            case TipoCarta.CoracaoExtra:
                c.Nome = "Coracao Extra";
                c.Descricao = "+1 coracao permanente";
                c.Preco = 150;
                c.Unica = true;
                break;

            case TipoCarta.Velocidade:
                c.Nome = "Velocidade";
                c.Descricao = "+30% por 3 dias";
                c.Preco = 60;
                c.Unica = false;
                break;
        }

        return c;
    }

    public static System.Collections.Generic.List<Carta> Todas()
    {
        return new System.Collections.Generic.List<Carta>
        {
            Cria(TipoCarta.Empadinha),
            Cria(TipoCarta.CoracaoExtra),
            Cria(TipoCarta.Velocidade),
        };
    }
}