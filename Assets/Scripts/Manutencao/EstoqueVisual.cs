using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Prateleira de estoque do escritório (Fase 2): mostra de verdade o que o técnico tem — caixas de toner,
// cilindros, saquinho de conectores RJ45, cabos prontos enrolados (com o tamanho na etiqueta) e os toners
// usados esperando descarte. As "vagas" (filhos chamados Vaga_N) são medidas pelo MontarCenario.
public class EstoqueVisual : MonoBehaviour
{
    readonly List<Transform> vagas = new List<Transform>();
    string assinatura = "";
    static Material mSaco, mCabo, mEscuro;

    void Start()
    {
        foreach (Transform t in transform) if (t.name.StartsWith("Vaga_")) vagas.Add(t);
        vagas.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (GetComponent<PontoEstoque>() == null) gameObject.AddComponent<PontoEstoque>();   // E na estante abre o inventário
    }

    void Update()
    {
        var m = Mochila.Instance;
        if (m == null || vagas.Count == 0) return;
        string cabos = "";
        foreach (var c in m.cabosEstoque) cabos += c.metros.ToString("0.#") + ";";
        string nova = $"{m.NoEstoque(Item.Toner)}|{m.NoEstoque(Item.Cilindro)}|{m.NoEstoque(Item.ConectorRJ45)}|{m.NoEstoque(Item.TonerUsado)}|{cabos}";
        if (nova == assinatura) return;
        assinatura = nova;
        Remontar(m);
    }

    void Remontar(Mochila m)
    {
        foreach (var v in vagas) UIFactory.Limpar(v);
        int i = 0;
        Transform Proxima() => i < vagas.Count ? vagas[i++] : null;

        int toner = m.NoEstoque(Item.Toner);
        if (toner > 0) Pilha(Proxima(), Mathf.Min(toner, 4), new Vector3(0.36f, 0.11f, 0.15f), $"TONER\nx{toner}");
        int cil = m.NoEstoque(Item.Cilindro);
        if (cil > 0) Pilha(Proxima(), Mathf.Min(cil, 3), new Vector3(0.38f, 0.1f, 0.12f), $"CILINDRO\nx{cil}");
        int rj = m.NoEstoque(Item.ConectorRJ45);
        if (rj > 0) Saco(Proxima(), new Color(0.9f, 0.92f, 0.95f), $"RJ45\nx{rj}");
        int usados = m.NoEstoque(Item.TonerUsado);
        if (usados > 0) Saco(Proxima(), new Color(0.16f, 0.16f, 0.18f), $"TONER USADO\nx{usados}", true);
        // cabos prontos: até 3 rolos por vaga, cada um com o tamanho na etiqueta
        Transform vagaCabos = null;
        for (int k = 0; k < m.cabosEstoque.Count && k < 6; k++)
        {
            if (k % 3 == 0) vagaCabos = Proxima();
            if (vagaCabos == null) break;
            Rolo(vagaCabos, (k % 3 - 1) * 0.17f, m.cabosEstoque[k]);
        }
    }

    // Caixas de papelão empilhadas com etiqueta na de cima
    static void Pilha(Transform vaga, int n, Vector3 tam, string texto)
    {
        if (vaga == null) return;
        for (int k = 0; k < n; k++)
        {
            var go = new GameObject("Caixa");
            go.transform.SetParent(vaga, false);
            go.transform.localPosition = new Vector3(0f, k * tam.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0, (k % 2 == 0 ? 3f : -4f), 0);
            EncomendaFisica.CriarCaixa(go.transform, tam);
            Destroy(go.GetComponent<BoxCollider>());
            if (k == n - 1) EncomendaFisica.Etiqueta(go.transform, tam, texto, true);
        }
    }

    static void Saco(Transform vaga, Color cor, string texto, bool grande = false)
    {
        if (vaga == null) return;
        mSaco ??= new Material(Shader.Find("Standard"));
        var tam = grande ? new Vector3(0.3f, 0.16f, 0.16f) : new Vector3(0.14f, 0.05f, 0.1f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(vaga, false);
        go.transform.localPosition = new Vector3(0f, tam.y / 2f, 0f);
        go.transform.localScale = tam;
        var mat = new Material(mSaco) { color = cor };
        mat.SetFloat("_Glossiness", 0.75f);   // plástico
        go.GetComponent<Renderer>().material = mat;
        var et = new GameObject("Rotulo").transform;
        et.SetParent(vaga, false);
        EncomendaFisica.Etiqueta(et, tam, texto, false);
    }

    // Cabo de rede enrolado (rolo azul deitado) com uma etiqueta de tamanho; ponta ruim fica laranja
    static void Rolo(Transform vaga, float x, CaboPronto cabo)
    {
        mCabo ??= new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.42f, 0.85f) };
        mEscuro ??= new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.05f, 0.06f) };
        float raio = Mathf.Lerp(0.11f, 0.16f, Mathf.InverseLerp(1f, 20f, cabo.metros));
        var rolo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(rolo.GetComponent<Collider>());
        rolo.transform.SetParent(vaga, false);
        rolo.transform.localPosition = new Vector3(x, 0.014f, 0f);
        rolo.transform.localScale = new Vector3(raio, 0.014f, raio);
        rolo.GetComponent<Renderer>().sharedMaterial = mCabo;
        var miolo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);   // o "furo" do rolo
        Destroy(miolo.GetComponent<Collider>());
        miolo.transform.SetParent(vaga, false);
        miolo.transform.localPosition = new Vector3(x, 0.0145f, 0f);
        miolo.transform.localScale = new Vector3(raio * 0.45f, 0.0146f, raio * 0.45f);
        miolo.GetComponent<Renderer>().sharedMaterial = mEscuro;
        var et = new GameObject("Tamanho").transform;
        et.SetParent(vaga, false);
        et.localPosition = new Vector3(x, 0.03f, 0.05f);
        var t = EncomendaFisica.Etiqueta(et, new Vector3(0.1f, 0.06f, 0.0f), $"{cabo.metros:0.#} m", false);
        if (!cabo.Bom) t.color = new Color(0.85f, 0.35f, 0.05f);
    }
}
