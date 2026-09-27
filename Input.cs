using System;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public enum InputDevice { Keyboard, Gamepad }
public enum GamepadBrand { Unknown, Xbox, PlayStation }

public struct InputState
{
    public Vector2 Move;
    public bool InteractPressed;    // configurável (padrão E / A)
    public bool InventoryPressed;   // configurável (padrão I / Select)
    public bool SwapItemPressed;    // configurável (padrão Q / Y)
    public bool AttackPressed;      // Clique esq / configurável no controle
    public bool AttackHeld;
    public bool ClosePressed;       // configurável (padrão ESC / B)
    public bool MouseClickPressed;  // Clique esq (separado pra minerar)

    // ----- Navegação de menu -----
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

    public static InputState Read()
    {
        InputState s = new InputState();
        bool pad = Raylib.IsGamepadAvailable(0);

        if (pad && Brand == GamepadBrand.Unknown)
            Brand = DetectBrand();

        // ----- Movimento -----
        float mx = 0f, my = 0f;
        bool keyboardUsed = false, gamepadUsed = false;

        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))  { mx -= 1f; keyboardUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) { mx += 1f; keyboardUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up))    { my -= 1f; keyboardUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down))  { my += 1f; keyboardUsed = true; }

        if (pad)
        {
            float ax = Raylib.GetGamepadAxisMovement(0, AX_LX);
            float ay = Raylib.GetGamepadAxisMovement(0, AX_LY);

            if (MathF.Abs(ax) > Deadzone) { mx = ax; gamepadUsed = true; }
            if (MathF.Abs(ay) > Deadzone) { my = ay; gamepadUsed = true; }

            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  { mx = -1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) { mx =  1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp))    { my = -1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown))  { my =  1f; gamepadUsed = true; }
        }

        s.Move = new Vector2(mx, my);

        // ----- Ações (lidas das configurações, remapeáveis) -----
        KeyboardKey teclaInteragir  = ConfiguracoesJogo.Teclado[AcaoJogo.Interagir];
        KeyboardKey teclaInventario = ConfiguracoesJogo.Teclado[AcaoJogo.Inventario];
        KeyboardKey teclaTrocar     = ConfiguracoesJogo.Teclado[AcaoJogo.TrocarItem];
        KeyboardKey teclaFechar     = ConfiguracoesJogo.Teclado[AcaoJogo.Fechar];

        GamepadButton botaoInteragir  = ConfiguracoesJogo.Controle[AcaoJogo.Interagir];
        GamepadButton botaoInventario = ConfiguracoesJogo.Controle[AcaoJogo.Inventario];
        GamepadButton botaoTrocar     = ConfiguracoesJogo.Controle[AcaoJogo.TrocarItem];
        GamepadButton botaoAtacar     = ConfiguracoesJogo.Controle[AcaoJogo.Atacar];
        GamepadButton botaoFechar     = ConfiguracoesJogo.Controle[AcaoJogo.Fechar];

        s.InteractPressed  = Raylib.IsKeyPressed(teclaInteragir)
                          || (pad && Raylib.IsGamepadButtonPressed(0, botaoInteragir));

        s.InventoryPressed = Raylib.IsKeyPressed(teclaInventario)
                          || (pad && Raylib.IsGamepadButtonPressed(0, botaoInventario));

        s.SwapItemPressed  = Raylib.IsKeyPressed(teclaTrocar)
                          || (pad && Raylib.IsGamepadButtonPressed(0, botaoTrocar));

        s.AttackPressed    = Raylib.IsMouseButtonPressed(MouseButton.Left)
                          || (pad && Raylib.IsGamepadButtonPressed(0, botaoAtacar));

        s.AttackHeld       = Raylib.IsMouseButtonDown(MouseButton.Left)
                          || (pad && Raylib.IsGamepadButtonDown(0, botaoAtacar));

        s.ClosePressed     = Raylib.IsKeyPressed(teclaFechar)
                          || (pad && Raylib.IsGamepadButtonPressed(0, botaoFechar));

        s.MouseClickPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);

        // ----- Último dispositivo -----
        if (gamepadUsed) LastDevice = InputDevice.Gamepad;
        else if (keyboardUsed) LastDevice = InputDevice.Keyboard;

        // ----- Navegação de menu (fixa, não remapeável) -----
        s.MenuUp     = Raylib.IsKeyPressed(KeyboardKey.Up)
                    || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceUp));

        s.MenuDown   = Raylib.IsKeyPressed(KeyboardKey.Down)
                    || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown));

        s.MenuEsquerda = Raylib.IsKeyPressed(KeyboardKey.Left)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftTrigger1));

        s.MenuDireita  = Raylib.IsKeyPressed(KeyboardKey.Right)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightTrigger1));

        s.MenuConfirm = Raylib.IsKeyPressed(KeyboardKey.Enter)
                     || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown));

        s.MenuCancel  = Raylib.IsKeyPressed(KeyboardKey.Escape)
                     || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceRight));

        // Se mexeu o mouse, "deseleciona" o controle
        if (Raylib.GetMouseDelta().X != 0 || Raylib.GetMouseDelta().Y != 0)
            LastDevice = InputDevice.Keyboard;

        return s;
    }

    static GamepadBrand DetectBrand()
    {
        try
        {
            unsafe
            {
                sbyte* raw = Raylib.GetGamepadName(0);
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

    // Direção pro swing da picareta: devolve -1 (esquerda) ou +1 (direita)
    public static Direcao DirecaoDoMovimento(InputState s, Direcao atual)
    {
        if (MathF.Abs(s.Move.X) > 0.1f)
            return s.Move.X > 0f ? Direcao.Direita : Direcao.Esquerda;
        return atual;
    }
}