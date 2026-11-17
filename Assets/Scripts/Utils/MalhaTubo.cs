using System.Collections.Generic;
using UnityEngine;

// Malha de tubo que passa por uma lista de pontos (curva Catmull-Rom): cordões de rede, cabos pendurados.
// Feita em código para não precisar de modelo (um cordão é só um tubinho de 6 mm de diâmetro).
public static class MalhaTubo
{
    // Pontos da curva suave que passa por todos os pontos de controle
    public static List<Vector3> Curva(IList<Vector3> controle, int passosPorTrecho = 6)
    {
        var r = new List<Vector3>();
        if (controle == null || controle.Count == 0) return r;
        if (controle.Count == 1) { r.Add(controle[0]); return r; }
        for (int i = 0; i < controle.Count - 1; i++)
        {
            Vector3 p0 = controle[Mathf.Max(0, i - 1)], p1 = controle[i], p2 = controle[i + 1], p3 = controle[Mathf.Min(controle.Count - 1, i + 2)];
            for (int s = 0; s < passosPorTrecho; s++)
            {
                float t = s / (float)passosPorTrecho, t2 = t * t, t3 = t2 * t;
                r.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        r.Add(controle[controle.Count - 1]);
        return r;
    }

    // Malha de tubo ao longo do caminho (já na curva final), com tampas nas pontas
    public static Mesh Criar(IList<Vector3> caminho, float raio, int lados = 6, Mesh reaproveitar = null)
    {
        var m = reaproveitar != null ? reaproveitar : new Mesh { name = "Tubo" };
        m.Clear();
        int n = caminho.Count;
        if (n < 2) return m;
        var v = new List<Vector3>(n * lados + 2);
        var nor = new List<Vector3>(n * lados + 2);
        var tri = new List<int>((n - 1) * lados * 6 + lados * 6);

        Vector3 anterior = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            Vector3 dir = (i < n - 1 ? caminho[i + 1] - caminho[i] : caminho[i] - caminho[i - 1]);
            if (i > 0 && i < n - 1) dir = caminho[i + 1] - caminho[i - 1];
            if (dir.sqrMagnitude < 1e-10f) dir = Vector3.forward;
            dir.Normalize();
            // Eixo lateral que gira pouco de um anel para o outro (sem torcer o tubo)
            Vector3 lado = i == 0 ? Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized
                                  : Vector3.Cross(Vector3.Cross(dir, anterior), dir).normalized;
            if (lado.sqrMagnitude < 0.5f) lado = Vector3.Cross(dir, Vector3.right).normalized;
            anterior = lado;
            Vector3 cima = Vector3.Cross(dir, lado);
            for (int k = 0; k < lados; k++)
            {
                float a = k * Mathf.PI * 2f / lados;
                Vector3 nn = lado * Mathf.Cos(a) + cima * Mathf.Sin(a);
                v.Add(caminho[i] + nn * raio);
                nor.Add(nn);
            }
        }
        for (int i = 0; i < n - 1; i++)
            for (int k = 0; k < lados; k++)
            {
                int a = i * lados + k, b = i * lados + (k + 1) % lados, c = a + lados, d = b + lados;
                tri.Add(a); tri.Add(c); tri.Add(b);
                tri.Add(b); tri.Add(c); tri.Add(d);
            }
        // Tampas
        int iniC = v.Count; v.Add(caminho[0]); nor.Add((caminho[0] - caminho[1]).normalized);
        int fimC = v.Count; v.Add(caminho[n - 1]); nor.Add((caminho[n - 1] - caminho[n - 2]).normalized);
        for (int k = 0; k < lados; k++)
        {
            tri.Add(iniC); tri.Add((k + 1) % lados); tri.Add(k);
            int baseF = (n - 1) * lados;
            tri.Add(fimC); tri.Add(baseF + k); tri.Add(baseF + (k + 1) % lados);
        }
        m.SetVertices(v);
        m.SetNormals(nor);
        m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        return m;
    }
}
