using System;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum InputDevice { Keyboard, Gamepad }
public enum GamepadBrand { Unknown, Xbox, PlayStation }

public struct InputState
{
    public Vector2 Move;
    public bool InteractPressed;
    public bool InventoryPressed;
    public bool SwapItemPressed;
    public bool AttackPressed;
    public bool AttackHeld;
    public bool ClosePressed;
    public bool MouseClickPressed;

    public bool MenuUp;
    public bool MenuDown;
    public bool MenuEsquerda;
    public bool MenuDireita;
    public bool MenuConfirm;
    public bool MenuCancel;
}

public static class Input
{
    const GamepadAxis AX_LX = GamepadAxis.LeftX;
    const GamepadAxis AX_LY = GamepadAxis.LeftY;

    const float Deadzone = 0.25f;

    public static InputDevice LastDevice { get; private set; } = InputDevice.Keyboard;
    public static GamepadBrand Brand { get; private set; } = GamepadBrand.Unknown;

    // Le input do jogador indicado (0 = P1, 1 = P2)
    public static InputState Read(int jogador = 0)
    {
        InputState s = new InputState();
        int gamepadIndex = jogador;

        bool pad = Raylib.IsGamepadAvailable(gamepadIndex);

        // LastDevice / Brand pertencem ao P1 (dica de botoes na tela).
        // O P2 nao pode sobrescrever isso a cada frame.
        bool principal = jogador == 0;

        if (principal && pad && Brand == GamepadBrand.Unknown)
            Brand = DetectBrand(gamepadIndex);

        // ----- Movimento -----
        float mx = 0f, my = 0f;
        bool keyboardUsed = false, gamepadUsed = false;

        if (jogador == 0)
        {
            // P1 usa WASD
            if (Raylib.IsKeyDown(KeyboardKey.A)) { mx -= 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.D)) { mx += 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.W)) { my -= 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.S)) { my += 1f; keyboardUsed = true; }
        }
        else
        {
            // P2 usa setas
            if (Raylib.IsKeyDown(KeyboardKey.Left))  { mx -= 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.Right)) { mx += 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.Up))    { my -= 1f; keyboardUsed = true; }
            if (Raylib.IsKeyDown(KeyboardKey.Down))  { my += 1f; keyboardUsed = true; }
        }

        if (pad)
        {
            float ax = Raylib.GetGamepadAxisMovement(gamepadIndex, AX_LX);
            float ay = Raylib.GetGamepadAxisMovement(gamepadIndex, AX_LY);

            if (MathF.Abs(ax) > Deadzone) { mx = ax; gamepadUsed = true; }
            if (MathF.Abs(ay) > Deadzone) { my = ay; gamepadUsed = true; }

            if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceLeft))  { mx = -1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceRight)) { mx =  1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceUp))    { my = -1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceDown))  { my =  1f; gamepadUsed = true; }
        }

        s.Move = new Vector2(mx, my);

        // ----- Acoes -----
        var teclas = jogador == 0 ? ConfiguracoesJogo.Teclado1 : ConfiguracoesJogo.Teclado2;
        var botoes = jogador == 0 ? ConfiguracoesJogo.Controle1 : ConfiguracoesJogo.Controle2;

        KeyboardKey teclaInteragir  = teclas[AcaoJogo.Interagir];
        KeyboardKey teclaInventario = teclas[AcaoJogo.Inventario];
        KeyboardKey teclaTrocar     = teclas[AcaoJogo.TrocarItem];
        KeyboardKey teclaFechar     = teclas[AcaoJogo.Fechar];

        KeyboardKey teclaAtacar     = teclas[AcaoJogo.Atacar];

        GamepadButton botaoInteragir  = botoes[AcaoJogo.Interagir];
        GamepadButton botaoInventario = botoes[AcaoJogo.Inventario];
        GamepadButton botaoTrocar     = botoes[AcaoJogo.TrocarItem];
        GamepadButton botaoAtacar     = botoes[AcaoJogo.Atacar];
        GamepadButton botaoFechar     = botoes[AcaoJogo.Fechar];

        s.InteractPressed  = Raylib.IsKeyPressed(teclaInteragir)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, botaoInteragir));

        s.InventoryPressed = Raylib.IsKeyPressed(teclaInventario)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, botaoInventario));

        s.SwapItemPressed  = Raylib.IsKeyPressed(teclaTrocar)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, botaoTrocar));

        // Ataque: P1 = mouse (+ tecla configurada), P2 = tecla configurada (sem mouse)
        bool ataqueMouse = principal && Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool ataqueMouseHeld = principal && Raylib.IsMouseButtonDown(MouseButton.Left);

        s.AttackPressed    = ataqueMouse
                          || Raylib.IsKeyPressed(teclaAtacar)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, botaoAtacar));

        s.AttackHeld       = ataqueMouseHeld
                          || Raylib.IsKeyDown(teclaAtacar)
                          || (pad && Raylib.IsGamepadButtonDown(gamepadIndex, botaoAtacar));

        s.ClosePressed     = Raylib.IsKeyPressed(teclaFechar)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, botaoFechar));

        s.MouseClickPressed = principal && Raylib.IsMouseButtonPressed(MouseButton.Left);

        if (principal)
        {
            if (gamepadUsed) LastDevice = InputDevice.Gamepad;
            else if (keyboardUsed) LastDevice = InputDevice.Keyboard;

            Vector2 mouseDelta = Raylib.GetMouseDelta();
            if (mouseDelta.X != 0 || mouseDelta.Y != 0)
                LastDevice = InputDevice.Keyboard;
        }

        // ----- Navegacao de menu -----
        // P1: teclado (setas/WASD/Enter/Esc) + controle 0.
        // P2: apenas o proprio controle (o teclado ja e coberto pelo P1, que usa as
        // mesmas setas/Enter). Os menus in-game combinam os inputs dos dois jogadores.
        if (principal)
        {
            s.MenuUp     = Raylib.IsKeyPressed(KeyboardKey.Up)
                        || Raylib.IsKeyPressed(KeyboardKey.W)
                        || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftFaceUp));

            s.MenuDown   = Raylib.IsKeyPressed(KeyboardKey.Down)
                        || Raylib.IsKeyPressed(KeyboardKey.S)
                        || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftFaceDown));

            s.MenuEsquerda = Raylib.IsKeyPressed(KeyboardKey.Left)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftTrigger1));

            s.MenuDireita  = Raylib.IsKeyPressed(KeyboardKey.Right)
                          || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightTrigger1));

            s.MenuConfirm = Raylib.IsKeyPressed(KeyboardKey.Enter)
                         || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightFaceDown));

            s.MenuCancel  = Raylib.IsKeyPressed(KeyboardKey.Escape)
                         || (pad && Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightFaceRight));
        }
        else if (pad)
        {
            s.MenuUp       = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftFaceUp);
            s.MenuDown     = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftFaceDown);
            s.MenuEsquerda = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.LeftTrigger1);
            s.MenuDireita  = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightTrigger1);
            s.MenuConfirm  = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightFaceDown);
            s.MenuCancel   = Raylib.IsGamepadButtonPressed(gamepadIndex, GamepadButton.RightFaceRight);
        }

        return s;
    }

    static GamepadBrand DetectBrand(int index)
    {
        try
        {
            unsafe
            {
                sbyte* raw = Raylib.GetGamepadName(index);
                if (raw == null) return GamepadBrand.Unknown;

                string name = System.Runtime.InteropServices.Marshal
                    .PtrToStringAnsi((IntPtr)raw) ?? "";
                name = name.ToLowerInvariant();

                if (name.Contains("sony") || name.Contains("dualshock") || name.Contains("dualsense")
                    || name.Contains("playstation") || name.Contains("ps3")
                    || name.Contains("ps4") || name.Contains("ps5")
                    || name.Contains("wireless controller"))
                    return GamepadBrand.PlayStation;

                if (name.Contains("xbox") || name.Contains("xinput") || name.Contains("microsoft"))
                    return GamepadBrand.Xbox;

                return GamepadBrand.Unknown;
            }
        }
        catch
        {
            return GamepadBrand.Unknown;
        }
    }

    // Texto do botao de "Interagir" do jogador (para os avisos na tela).
    // P1 segue o ultimo dispositivo usado; P2 usa o controle 2 se ele estiver conectado.
    public static string NomeInteragir(int jogador)
    {
        bool usaControle = jogador == 0
            ? LastDevice == InputDevice.Gamepad && Raylib.IsGamepadAvailable(0)
            : Raylib.IsGamepadAvailable(1);

        if (usaControle)
        {
            var botoes = jogador == 0 ? ConfiguracoesJogo.Controle1 : ConfiguracoesJogo.Controle2;
            return NomeBotao(botoes[AcaoJogo.Interagir]);
        }

        var teclas = jogador == 0 ? ConfiguracoesJogo.Teclado1 : ConfiguracoesJogo.Teclado2;
        return teclas[AcaoJogo.Interagir].ToString();
    }

    static string NomeBotao(GamepadButton b)
    {
        bool ps = Brand == GamepadBrand.PlayStation;
        return b switch
        {
            GamepadButton.RightFaceDown  => ps ? "X" : "A",
            GamepadButton.RightFaceRight => ps ? "O" : "B",
            GamepadButton.RightFaceLeft  => ps ? "Quadrado" : "X",
            GamepadButton.RightFaceUp    => ps ? "Triangulo" : "Y",
            _ => b.ToString()
        };
    }

    public static Direcao DirecaoDoMovimento(InputState s, Direcao atual)
    {
        if (MathF.Abs(s.Move.X) > 0.1f)
            return s.Move.X > 0f ? Direcao.Direita : Direcao.Esquerda;
        return atual;
    }
}