using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public class ObjetoMapa
{
    public Texture2D Textura;
    public bool TemSprite;
    public Vector2 Pos;
    public int Largura;
    public int Altura;

    // Altura da colisão. Se 0, usa Altura inteira.
    public int AlturaColisao = 0;

    public bool Colide;
    public Color CorFallback;
    public string Nome = "";

    public Rectangle RetanguloColisao()
    {
        int alturaCol = AlturaColisao > 0 ? AlturaColisao : Altura;
        // Colisão começa no "pé" do objeto (base)
        float yCol = Pos.Y + Altura - alturaCol;
        return new Rectangle(Pos.X, yCol, Largura, alturaCol);
    }

    public Vector2 Centro()
    {
        return new Vector2(Pos.X + Largura / 2f, Pos.Y + Altura / 2f);
    }

    public static ObjetoMapa Cria(
        string caminho, Vector2 pos, int largura, int altura,
        bool colide, Color corFallback, string nome,
        int alturaColisao = 0)
    {
        ObjetoMapa o = new ObjetoMapa();
        o.Pos = pos;
        o.Largura = largura;
        o.Altura = altura;
        o.Colide = colide;
        o.CorFallback = corFallback;
        o.Nome = nome;
        o.AlturaColisao = alturaColisao;

        if (System.IO.File.Exists(caminho))
        {
            o.Textura = Raylib.LoadTexture(caminho);
            o.TemSprite = true;
        }
        else
        {
            o.Textura = new Texture2D();
            o.TemSprite = false;
        }

        return o;
    }

    public void Desenha()
    {
        if (TemSprite)
        {
            Rectangle src = new Rectangle(0, 0, Textura.Width, Textura.Height);
            Rectangle dest = new Rectangle(Pos.X, Pos.Y, Largura, Altura);
            Raylib.DrawTexturePro(Textura, src, dest, Vector2.Zero, 0f, Color.White);
        }
        else
        {
            Raylib.DrawRectangle((int)Pos.X, (int)Pos.Y, Largura, Altura, CorFallback);
            Raylib.DrawText(Nome, (int)Pos.X, (int)Pos.Y, 20, Color.White);
        }
    }
}