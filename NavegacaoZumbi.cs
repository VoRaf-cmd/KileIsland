using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace KileIsland;

public sealed class NavegacaoZumbi
{
    const float TamanhoCelula = 48f;
    const float CustoDiagonal = 1.41421356f;

    static readonly (int X, int Y)[] Direcoes =
    {
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1),
        (1, 1),
        (1, -1),
        (-1, 1),
        (-1, -1)
    };

    readonly List<ObjetoMapa> objetos;
    readonly float tamanhoAtor;
    readonly float minX;
    readonly float minY;
    readonly int colunas;
    readonly int linhas;
    readonly bool[] celulasCaminhaveis;

    public NavegacaoZumbi(
        List<ObjetoMapa> objetos,
        float tamanhoAtor)
    {
        this.objetos = objetos;
        this.tamanhoAtor = tamanhoAtor;

        minX = CenaMundo.PosIlha.X;
        minY = CenaMundo.PosIlha.Y;

        float maxX =
            CenaMundo.PosIlha.X +
            CenaMundo.GramaLargura -
            tamanhoAtor;

        float maxY =
            CenaMundo.PosIlha.Y +
            CenaMundo.GramaAltura -
            tamanhoAtor;

        colunas =
            (int)MathF.Floor(
                (maxX - minX) / TamanhoCelula
            ) + 1;

        linhas =
            (int)MathF.Floor(
                (maxY - minY) / TamanhoCelula
            ) + 1;

        celulasCaminhaveis =
            new bool[colunas * linhas];

        for (int linha = 0; linha < linhas; linha++)
        {
            for (int coluna = 0; coluna < colunas; coluna++)
            {
                celulasCaminhaveis[
                    Indice(coluna, linha)
                ] = PodeOcupar(
                    Posicao(coluna, linha)
                );
            }
        }
    }

    public bool PodeOcupar(Vector2 pos)
    {
        if (
            pos.X < minX ||
            pos.Y < minY ||
            pos.X + tamanhoAtor >
                CenaMundo.PosIlha.X +
                CenaMundo.GramaLargura ||
            pos.Y + tamanhoAtor >
                CenaMundo.PosIlha.Y +
                CenaMundo.GramaAltura
        )
        {
            return false;
        }

        Rectangle retanguloAtor =
            new Rectangle(
                pos.X,
                pos.Y,
                tamanhoAtor,
                tamanhoAtor
            );

        foreach (var objeto in objetos)
        {
            if (
                objeto.Colide &&
                Raylib.CheckCollisionRecs(
                    retanguloAtor,
                    objeto.RetanguloColisao()
                )
            )
            {
                return false;
            }
        }

        return true;
    }

    public List<Vector2> AchaCaminho(
        Vector2 origem,
        Vector2 alvoCentro)
    {
        Vector2 alvoPos =
            alvoCentro -
            new Vector2(tamanhoAtor / 2f);

        int inicio = CelulaMaisProxima(origem);
        int objetivo = CelulaMaisProxima(alvoPos);

        inicio =
            CelulaCaminhavelMaisProxima(
                inicio,
                3,
                origem
            );

        objetivo =
            CelulaCaminhavelMaisProxima(
                objetivo,
                5,
                alvoPos
            );

        if (
            inicio < 0 ||
            objetivo < 0 ||
            inicio == objetivo
        )
        {
            return new List<Vector2>();
        }

        int total = celulasCaminhaveis.Length;

        float[] custo = new float[total];
        int[] anterior = new int[total];
        bool[] visitadas = new bool[total];

        Array.Fill(
            custo,
            float.PositiveInfinity
        );

        Array.Fill(anterior, -1);

        PriorityQueue<int, float> fila =
            new PriorityQueue<int, float>();

        custo[inicio] = 0f;

        fila.Enqueue(
            inicio,
            Heuristica(inicio, objetivo)
        );

        while (fila.TryDequeue(
            out int atual,
            out _))
        {
            if (visitadas[atual])
                continue;

            if (atual == objetivo)
                break;

            visitadas[atual] = true;

            int colunaAtual =
                atual % colunas;

            int linhaAtual =
                atual / colunas;

            foreach (var direcao in Direcoes)
            {
                int proximoX =
                    colunaAtual + direcao.X;

                int proximoY =
                    linhaAtual + direcao.Y;

                if (
                    proximoX < 0 ||
                    proximoX >= colunas ||
                    proximoY < 0 ||
                    proximoY >= linhas
                )
                {
                    continue;
                }

                int proximo =
                    Indice(proximoX, proximoY);

                if (
                    !celulasCaminhaveis[proximo] ||
                    visitadas[proximo]
                )
                {
                    continue;
                }

                if (
                    direcao.X != 0 &&
                    direcao.Y != 0 &&
                    (
                        !celulasCaminhaveis[
                            Indice(
                                colunaAtual + direcao.X,
                                linhaAtual
                            )
                        ] ||
                        !celulasCaminhaveis[
                            Indice(
                                colunaAtual,
                                linhaAtual + direcao.Y
                            )
                        ]
                    )
                )
                {
                    continue;
                }

                float novoCusto =
                    custo[atual] +
                    (
                        direcao.X != 0 &&
                        direcao.Y != 0
                            ? CustoDiagonal
                            : 1f
                    );

                if (novoCusto >= custo[proximo])
                    continue;

                custo[proximo] = novoCusto;
                anterior[proximo] = atual;

                fila.Enqueue(
                    proximo,
                    novoCusto +
                    Heuristica(proximo, objetivo)
                );
            }
        }

        if (anterior[objetivo] < 0)
            return new List<Vector2>();

        List<int> indices = new();

        int cursor = objetivo;

        while (cursor != inicio)
        {
            indices.Add(cursor);
            cursor = anterior[cursor];

            if (cursor < 0)
                return new List<Vector2>();
        }

        indices.Reverse();

        List<Vector2> caminho =
            new(indices.Count);

        foreach (int indice in indices)
        {
            caminho.Add(
                Posicao(
                    indice % colunas,
                    indice / colunas
                )
            );
        }

        return caminho;
    }

    int CelulaMaisProxima(Vector2 pos)
    {
        int x = Math.Clamp(
            (int)MathF.Round(
                (pos.X - minX) / TamanhoCelula
            ),
            0,
            colunas - 1
        );

        int y = Math.Clamp(
            (int)MathF.Round(
                (pos.Y - minY) / TamanhoCelula
            ),
            0,
            linhas - 1
        );

        return Indice(x, y);
    }

    int CelulaCaminhavelMaisProxima(
        int origem,
        int raioMaximo,
        Vector2 posReferencia)
    {
        int origemX = origem % colunas;
        int origemY = origem / colunas;

        for (int raio = 0;
             raio <= raioMaximo;
             raio++)
        {
            int melhor = -1;
            int menorDistancia = int.MaxValue;

            for (
                int linha =
                    Math.Max(0, origemY - raio);
                linha <=
                    Math.Min(linhas - 1, origemY + raio);
                linha++)
            {
                for (
                    int coluna =
                        Math.Max(0, origemX - raio);
                    coluna <=
                        Math.Min(colunas - 1, origemX + raio);
                    coluna++)
                {
                    int indice =
                        Indice(coluna, linha);

                    Vector2 posicao =
                        Posicao(coluna, linha);

                    if (
                        !celulasCaminhaveis[indice] ||
                        !SegmentoCaminhavel(
                            posReferencia,
                            posicao
                        )
                    )
                    {
                        continue;
                    }

                    int distancia =
                        (coluna - origemX) *
                        (coluna - origemX) +
                        (linha - origemY) *
                        (linha - origemY);

                    if (distancia < menorDistancia)
                    {
                        menorDistancia = distancia;
                        melhor = indice;
                    }
                }
            }

            if (melhor >= 0)
                return melhor;
        }

        return -1;
    }

    bool SegmentoCaminhavel(
        Vector2 inicio,
        Vector2 fim)
    {
        float distancia =
            Vector2.Distance(inicio, fim);

        int passos = Math.Max(
            1,
            (int)MathF.Ceiling(
                distancia /
                (TamanhoCelula / 4f)
            )
        );

        for (int passo = 1;
             passo <= passos;
             passo++)
        {
            Vector2 pos =
                Vector2.Lerp(
                    inicio,
                    fim,
                    (float)passo / passos
                );

            if (!PodeOcupar(pos))
                return false;
        }

        return true;
    }

    float Heuristica(
        int primeiro,
        int segundo)
    {
        int diferencaX =
            Math.Abs(
                primeiro % colunas -
                segundo % colunas
            );

        int diferencaY =
            Math.Abs(
                primeiro / colunas -
                segundo / colunas
            );

        int diagonal =
            Math.Min(
                diferencaX,
                diferencaY
            );

        return Math.Max(
            diferencaX,
            diferencaY
        ) +
        (CustoDiagonal - 1f) *
        diagonal;
    }

    int Indice(int x, int y)
    {
        return y * colunas + x;
    }

    Vector2 Posicao(int x, int y)
    {
        return new Vector2(
            minX + x * TamanhoCelula,
            minY + y * TamanhoCelula
        );
    }
}
