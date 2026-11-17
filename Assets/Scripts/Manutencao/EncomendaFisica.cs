using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Compras físicas (Fase 2): a encomenda chega como caixa de papelão na porta de casa (ponto "PontoEntrega").
// E abre a caixa e guarda os itens na mochila; a caixa de cabo de 305 m vai para a bancada do escritório.
public class EncomendaFisica : Interactable
{
    public Entrega entrega;
    static readonly Dictionary<Entrega, EncomendaFisica> naCena = new Dictionary<Entrega, EncomendaFisica>();

    bool CaixaDeCabo => entrega != null && entrega.item == Item.CaixaCabo;

    public override string TextoPrompt => entrega == null ? "" :
        (CaixaDeCabo ? "[E] Levar a caixa de cabo (305 m) para a bancada do escritório"
                     : $"[E] Abrir a encomenda: {Mochila.Info(entrega.item).nome}") +
        (CarroTecnico.Instance != null ? "   [G] Pegar a caixa" : "");

    // Garante que cada entrega que já chegou tem a sua caixa na porta (também depois de carregar o jogo)
    public static void Sincronizar(List<Entrega> entregas)
    {
        var ponto = GameObject.Find("PontoEntrega");
        if (ponto == null) return;
        int i = 0;
        foreach (var e in entregas)
        {
            if (!e.naPorta || e.local != 0) continue;   // no porta-malas, nas mãos ou largada em outro lugar
            if (!naCena.TryGetValue(e, out var f) || f == null) naCena[e] = Criar(e, ponto.transform, i);
            i++;
        }
    }

    public static bool TemPontoDeEntrega => GameObject.Find("PontoEntrega") != null;

    static EncomendaFisica Criar(Entrega e, Transform ponto, int indice)
    {
        bool cabo = e.item == Item.CaixaCabo;
        // caixas lado a lado em frente à porta; tamanho conforme o que tem dentro
        Vector3 tam = cabo ? new Vector3(0.36f, 0.42f, 0.36f) : new Vector3(0.42f, 0.24f, 0.32f);
        var go = new GameObject("Encomenda_" + e.item);
        go.transform.SetPositionAndRotation(ponto.position + ponto.right * (indice * 0.5f - 0.25f), ponto.rotation * Quaternion.Euler(0, Random.Range(-12f, 12f), 0));
        CriarCaixa(go.transform, tam);
        Etiqueta(go.transform, tam, cabo ? "CABO DE REDE U/UTP Cat6\n305 m · 23 AWG · azul" : "ENTREGA\n" + Mochila.Info(e.item).nome, cabo);
        if (cabo) CaboSaindo(go.transform, tam);

        var f = go.AddComponent<EncomendaFisica>();
        f.entrega = e;
        f.distanciaInteracao = 1.6f;
        go.transform.localScale = Vector3.zero;
        Tween.Scale(go.transform, Vector3.one, 0.3f, Ease.OutBack);
        return f;
    }

    // Etapa 9C: caixa largada no chão (fora da porta de casa) — abre com E, pega de novo com G
    public static EncomendaFisica CriarNoChao(Entrega e, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject("Encomenda_" + e.item);
        go.transform.SetPositionAndRotation(pos, rot);
        bool cabo = e.item == Item.CaixaCabo;
        Vector3 tam = Tamanho(e);
        CriarCaixa(go.transform, tam);
        Etiqueta(go.transform, tam, cabo ? "CABO DE REDE U/UTP Cat6\n305 m · 23 AWG · azul" : "ENTREGA\n" + Mochila.Info(e.item).nome, cabo);
        if (cabo) CaboSaindo(go.transform, tam);
        var f = go.AddComponent<EncomendaFisica>();
        f.entrega = e;
        f.distanciaInteracao = 1.6f;
        e.local = 3;
        naCena[e] = f;
        return f;
    }

    public static Vector3 Tamanho(Entrega e) => e.item == Item.CaixaCabo ? new Vector3(0.36f, 0.42f, 0.36f) : new Vector3(0.42f, 0.24f, 0.32f);

    // [G]: a caixa vai para as mãos (para levar no porta-malas)
    public void PegarCaixa()
    {
        if (entrega == null) return;
        var e = entrega;
        bool cabo = CaixaDeCabo;
        var c = CargaFisica.CriarCaixa(TipoCarga.Encomenda, "Encomenda: " + Mochila.Info(e.item).nome, Tamanho(e),
            cabo ? "CABO DE REDE U/UTP Cat6\n305 m · 23 AWG · azul" : "ENTREGA\n" + Mochila.Info(e.item).nome);
        if (cabo) CaboSaindo(c.transform, Tamanho(e));
        c.entrega = e;
        c.transform.SetPositionAndRotation(transform.position, transform.rotation);
        e.local = 2;
        naCena.Remove(e);
        entrega = null;
        Destroy(gameObject);
        CargaFisica.Pegar(c);
    }

    // A caixa da porta sumiu porque foi para o porta-malas (ao carregar o jogo)
    public static void Esconder(Entrega e)
    {
        if (e != null && naCena.TryGetValue(e, out var f) && f != null) Destroy(f.gameObject);
        if (e != null) naCena.Remove(e);
    }

    // Caixa de papelão realista (prefab gerado pelo MontarCenario em Resources/Encomendas/Caixa, 1 m de lado)
    public static Transform CriarCaixa(Transform pai, Vector3 tam)
    {
        var prefab = Resources.Load<GameObject>("Encomendas/Caixa");
        GameObject caixa;
        if (prefab != null) caixa = Instantiate(prefab);
        else
        {
            caixa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caixa.transform.localPosition = Vector3.up * 0.5f;
            caixa.GetComponent<Renderer>().material.color = new Color(0.7f, 0.55f, 0.35f);
        }
        caixa.name = "Caixa";
        caixa.transform.SetParent(pai, false);
        caixa.transform.localScale = tam;   // o prefab tem 1 m e base em y = 0
        if (prefab == null) caixa.transform.localPosition = Vector3.up * tam.y / 2f;
        foreach (var c in caixa.GetComponentsInChildren<Collider>()) Destroy(c);
        var bc = pai.gameObject.AddComponent<BoxCollider>();
        bc.center = Vector3.up * tam.y / 2f;
        bc.size = tam;
        return caixa.transform;
    }

    // Etiqueta impressa colada na frente da caixa (texto de verdade, legível de perto)
    public static TextMeshProUGUI Etiqueta(Transform pai, Vector3 tam, string texto, bool grande)
    {
        const float ppm = 3000f;
        var go = new GameObject("Etiqueta", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.transform.localPosition = new Vector3(0f, tam.y * 0.55f, -tam.z / 2f - 0.002f);
        go.transform.localRotation = Quaternion.identity;   // canvas olha para -Z (frente da caixa)
        go.transform.localScale = Vector3.one / ppm;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(tam.x * (grande ? 0.9f : 0.7f), tam.y * (grande ? 0.45f : 0.5f)) * ppm;
        var fundo = new GameObject("Fundo", typeof(RectTransform)).AddComponent<Image>();
        fundo.transform.SetParent(rt, false);
        UIFactory.Esticar(fundo.rectTransform);
        fundo.color = grande ? new Color(0.93f, 0.93f, 0.9f) : Color.white;
        var t = UIFactory.Texto(rt, texto, 60, new Color(0.08f, 0.1f, 0.14f), FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(t.rectTransform, 10);
        t.enableAutoSizing = true;
        t.fontSizeMin = 12;
        t.fontSizeMax = 70;
        return t;
    }

    // Ponta do cabo azul saindo pelo furo do topo da caixa (é assim que se puxa o cabo da caixa)
    public static void CaboSaindo(Transform pai, Vector3 tam)
    {
        var azul = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.42f, 0.85f) };
        azul.SetFloat("_Glossiness", 0.55f);
        var furo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(furo.GetComponent<Collider>());
        furo.transform.SetParent(pai, false);
        furo.transform.localPosition = new Vector3(0.05f, tam.y + 0.001f, -0.05f);
        furo.transform.localScale = new Vector3(0.05f, 0.001f, 0.05f);
        furo.GetComponent<Renderer>().material.color = new Color(0.12f, 0.09f, 0.06f);
        // arco de cabo: segmentos curtos saindo do furo e caindo para o lado
        Vector3 p = furo.transform.localPosition;
        for (int i = 0; i < 9; i++)
        {
            float a = i / 8f;
            Vector3 prox = new Vector3(0.05f + a * 0.16f, tam.y + Mathf.Sin(a * Mathf.PI) * 0.12f - a * a * 0.1f, -0.05f - a * 0.04f);
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(seg.GetComponent<Collider>());
            seg.transform.SetParent(pai, false);
            seg.transform.localPosition = (p + prox) / 2f;
            seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, prox - p);
            seg.transform.localScale = new Vector3(0.0065f, (prox - p).magnitude / 2f + 0.002f, 0.0065f);
            seg.GetComponent<Renderer>().sharedMaterial = azul;
            p = prox;
        }
    }

    public override void Interagir()
    {
        var m = Mochila.Instance;
        if (m == null || entrega == null) return;
        var info = Mochila.Info(entrega.item);
        // Limite da mochila: se não couber, a caixa fica na porta até você liberar espaço
        if (!CaixaDeCabo && !m.Cabe(entrega.item, entrega.quantidade))
        {
            GameUI.Instance?.Toast($"Não cabe na mochila: <b>{info.nome}</b> pesa {Mochila.Peso(entrega.item) * entrega.quantidade:0.0} kg e você só tem {m.KgLivres:0.0} kg livres. " +
                "Guarde algo na estante do escritório e volte.", UIFactory.Alerta, 5f);
            return;
        }
        m.entregas.Remove(entrega);
        naCena.Remove(entrega);
        SomManager.TocarEm("mexer", transform.position, 0.8f, 0.9f);
        // Pedido errado ou peça com defeito: não entra na mochila; a garantia manda outra (RMA)
        string problema = m.ProblemaNaEntrega(entrega);
        if (problema != null)
        {
            SomManager.Tocar("erro", 0.5f);
            GameUI.Instance?.Toast(problema, UIFactory.Perigo, 5f);
        }
        else
        {
            m.Adicionar(entrega.item, entrega.quantidade);
            GameUI.Instance?.Toast(CaixaDeCabo
                ? "Caixa de cabo (305 m) levada para a bancada do escritório."
                : $"Encomenda aberta: <b>{info.nome}</b> guardado na mochila.", UIFactory.Sucesso, 3f);
        }
        entrega = null;
        enabled = false;
        Tween.Scale(transform, Vector3.zero, 0.25f, Ease.InBack).OnComplete(this, f => Destroy(f.gameObject));
    }
}
