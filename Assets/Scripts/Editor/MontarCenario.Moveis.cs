using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Móveis realistas no lugar do kit simples (Etapa 2.3). Os modelos vêm prontos (Poly Haven / Sketchfab);
// só a lixeira de escritório é feita aqui (um cesto de plástico é um tronco de cone aberto — o único
// modelo pronto CC0 era uma lata de lixo de rua enferrujada).
public static partial class MontarCenario
{
    // Mesa de metal do Poly Haven: 2,0 x 0,79 m no arquivo. 1,9 m de comprimento = 75 cm de altura (mesa real)
    const float LadoMesaMetal = 1.9f;

    // Cesto de lixo de plástico (28 cm de altura), com saco plástico preto dobrado na borda
    static GameObject Lixeira(Transform pai, Vector3 pos)
    {
        const float altura = 0.30f, rBaixo = 0.105f, rCima = 0.135f, parede = 0.006f;
        var go = new GameObject("Lixeira");
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;

        var mPlastico = Mat("Lixeira_Plastico", new Color(0.22f, 0.24f, 0.26f), 0.35f);
        var mSaco = Mat("Saco_Lixo", new Color(0.03f, 0.03f, 0.035f), 0.55f);

        var corpo = new GameObject("Corpo");
        corpo.transform.SetParent(go.transform, false);
        corpo.AddComponent<MeshFilter>().sharedMesh = MalhaCesto(altura, rBaixo, rCima, parede);
        corpo.AddComponent<MeshRenderer>().sharedMaterial = mPlastico;

        // dobra do saco por cima da borda
        var saco = new GameObject("Saco");
        saco.transform.SetParent(go.transform, false);
        saco.transform.localPosition = new Vector3(0, altura - 0.025f, 0);
        saco.AddComponent<MeshFilter>().sharedMesh = MalhaCesto(0.032f, rCima + 0.001f, rCima + 0.0035f, 0.002f, fundo: false);
        saco.AddComponent<MeshRenderer>().sharedMaterial = mSaco;

        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, altura / 2f, 0);
        bc.size = new Vector3(rCima * 2f, altura, rCima * 2f);
        return go;
    }

    // Tronco de cone aberto em cima, com parede (lado de fora e de dentro), borda e fundo
    static Mesh MalhaCesto(float altura, float rBaixo, float rCima, float parede, bool fundo = true)
    {
        const int n = 32;
        var v = new List<Vector3>(); var nor = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        void Anel(float r0, float y0, float r1, float y1, bool dentro)
        {
            int i0 = v.Count;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var normal = (d * (y1 - y0) + Vector3.up * (r0 - r1)).normalized * (dentro ? -1f : 1f);
                v.Add(d * r0 + Vector3.up * y0); nor.Add(normal); uv.Add(new Vector2(i / (float)n, 0));
                v.Add(d * r1 + Vector3.up * y1); nor.Add(normal); uv.Add(new Vector2(i / (float)n, 1));
            }
            for (int i = 0; i < n; i++)
            {
                int a = i0 + i * 2, b = a + 1, c = a + 2, e = a + 3;
                if (dentro) t.AddRange(new[] { a, c, b, b, c, e });
                else t.AddRange(new[] { a, b, c, b, e, c });
            }
        }
        Anel(rBaixo, 0f, rCima, altura, false);                                       // fora
        Anel(rBaixo - parede, parede, rCima - parede, altura, true);                  // dentro
        // borda (aro de cima)
        int b0 = v.Count;
        for (int i = 0; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            v.Add(d * rCima + Vector3.up * altura); nor.Add(Vector3.up); uv.Add(Vector2.zero);
            v.Add(d * (rCima - parede) + Vector3.up * altura); nor.Add(Vector3.up); uv.Add(Vector2.one);
        }
        for (int i = 0; i < n; i++) { int a = b0 + i * 2; t.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
        if (fundo)
        {
            foreach (var (y, up) in new[] { (0f, false), (parede, true) })
            {
                int c0 = v.Count;
                float r = up ? rBaixo - parede : rBaixo;
                v.Add(Vector3.up * y); nor.Add(up ? Vector3.up : Vector3.down); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); nor.Add(up ? Vector3.up : Vector3.down); uv.Add(Vector2.zero);
                }
                for (int i = 0; i < n; i++)
                    if (up) t.AddRange(new[] { c0, c0 + i + 2, c0 + i + 1 }); else t.AddRange(new[] { c0, c0 + i + 1, c0 + i + 2 });
            }
        }
        var m = new Mesh { name = "Cesto" };
        m.SetVertices(v); m.SetNormals(nor); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}
