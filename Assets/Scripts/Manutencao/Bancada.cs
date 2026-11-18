using UnityEngine;

// Bancadas da sede (Etapa 10B: uma para cada coisa, pedido do colega testador — a caixa de cabo atrapalhava o
// computador usado em cima da mesma bancada):
// - de cabos: caixa de 305 m, rolo e alicate; faz cabo do tamanho certo (puxa da caixa e crimpa as pontas);
// - de equipamentos: tampo livre, onde os usados do lote são testados e recuperados (UsadosFisicos / AcoesBancada).
public class Bancada : Interactable
{
    public bool deCabos;

    // A bancada onde vão os equipamentos usados (a outra é só de cabos)
    public static Bancada DeEquipamentos()
    {
        foreach (var b in FindObjectsByType<Bancada>()) if (!b.deCabos) return b;
        return null;
    }

    public static Bancada DeCabos()
    {
        foreach (var b in FindObjectsByType<Bancada>()) if (b.deCabos) return b;
        return null;
    }

    public override string TextoPrompt
    {
        get
        {
            var mao = UsadosFisicos.NaMao;
            if (deCabos)
                return mao == null ? "[E] Usar a bancada de cabos (fazer cabos)" : "Bancada de cabos: os equipamentos vão na outra bancada";
            if (mao == null) return "Bancada de equipamentos (traga um usado do lote)";
            var u = UsadosFisicos.Instance;
            return u != null && !u.BancadaLivre ? "Bancada ocupada: tire o equipamento que está em cima" : $"[E] Colocar na bancada: {mao.nome}";
        }
    }

    public override void Interagir()
    {
        if (deCabos)
        {
            if (UsadosFisicos.NaMao != null) { GameUI.Instance?.Toast("Esta é a bancada de cabos. Leve o equipamento para a bancada de equipamentos.", UIFactory.Alerta, 3f); return; }
            GameUI.Instance?.AbrirBancada(transform.position + transform.forward * 0.5f);
            return;
        }
        if (UsadosFisicos.NaMao == null)
        {
            GameUI.Instance?.Toast("Bancada de equipamentos: pegue um usado do lote (G) e aperte E aqui para colocar. Cabos são na bancada de cabos.", UIFactory.CorSuave, 3f);
            return;
        }
        if (UsadosFisicos.Instance != null)
        {
            string r = UsadosFisicos.Instance.ColocarNaBancada();
            if (r != null) GameUI.Instance?.Toast(r, UIFactory.Destaque, 3f);
            return;
        }
    }

    // Monta uma bancada simples dentro de 'sala': a de cabos tem a caixa de 305 m, o rolo e o alicate; a de
    // equipamentos fica com o tampo livre
    public static Bancada Criar(Transform sala, Vector3 posLocal, float rotY, bool deCabos)
    {
        float largura = deCabos ? 1.0f : 1.3f;
        var raiz = new GameObject(deCabos ? "Bancada_Cabos" : "Bancada");
        raiz.transform.SetParent(sala, false);
        raiz.transform.localPosition = posLocal;
        raiz.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        var madeira = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.38f, 0.22f) };
        var metal = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.26f, 0.28f) };
        var azul = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.45f, 0.85f) };

        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0.86f, 0f), new Vector3(largura, 0.05f, 0.6f), madeira);
        foreach (var x in new[] { -largura / 2f + 0.05f, largura / 2f - 0.05f })
            foreach (var z in new[] { -0.25f, 0.25f })
                Peca(raiz.transform, PrimitiveType.Cube, new Vector3(x, 0.42f, z), new Vector3(0.05f, 0.84f, 0.05f), metal);
        if (deCabos)
        {
            // caixa de cabo de 305 m realista (aparece quando há cabo; mostra quanto resta)
            var caixa = new GameObject("CaixaCabo");
            caixa.transform.SetParent(raiz.transform, false);
            caixa.transform.localPosition = new Vector3(-0.22f, 0.885f, 0f);
            caixa.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // etiqueta virada para quem usa a bancada
            caixa.AddComponent<CaixaCaboBancada>();
            // rolo de cabo e alicate do lado direito
            Peca(raiz.transform, PrimitiveType.Cylinder, new Vector3(0.3f, 0.895f, 0.12f), new Vector3(0.16f, 0.01f, 0.16f), azul);   // rolo de cabo
            Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0.28f, 0.893f, -0.1f), new Vector3(0.16f, 0.015f, 0.045f), metal);   // alicate
        }

        var col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.445f, 0f);   // só até o tampo: o que está em cima pode ser mirado
        col.size = new Vector3(largura, 0.89f, 0.6f);

        var b = raiz.AddComponent<Bancada>();
        b.deCabos = deCabos;
        b.distanciaInteracao = 1.8f;
        return b;
    }

    static void Peca(Transform pai, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
    {
        var p = GameObject.CreatePrimitive(tipo);
        Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(pai, false);
        p.transform.localPosition = pos;
        p.transform.localScale = escala;
        p.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
