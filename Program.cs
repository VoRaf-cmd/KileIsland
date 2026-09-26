using Raylib_cs;

namespace KileIsland;

public static class Program
{
    // Resolução da janela (16:9)
    const int LarguraTela = 1024;
    const int AlturaTela = 576;

    // Tamanho do retângulo em pixels de arte (16x16)
    const int TamanhoQuadrado = 16;

    // Escala visual (16x16 desenhado vira 64x64 na tela)
    const int Escala = 4;

    // Velocidade (pixels por frame)
    const float Velocidade = 4f;

    [STAThread]
    public static void Main()
    {
        Raylib.InitWindow(LarguraTela, AlturaTela, "KileIsland");
        Raylib.SetTargetFPS(60);

        // Posição inicial (centro da tela)
        float x = LarguraTela / 2f - (TamanhoQuadrado * Escala) / 2f;
        float y = AlturaTela / 2f - (TamanhoQuadrado * Escala) / 2f;

        while (!Raylib.WindowShouldClose())
        {
            // ----- MOVIMENTO -----
            if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up))
                y -= Velocidade;

            if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down))
                y += Velocidade;

            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))
                x -= Velocidade;

            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))
                x += Velocidade;

            // ----- DESENHO -----
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.SkyBlue);

            Raylib.DrawRectangle(
                (int)x,
                (int)y,
                TamanhoQuadrado * Escala,
                TamanhoQuadrado * Escala,
                Color.Red
            );

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}