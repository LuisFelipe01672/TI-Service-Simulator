using UnityEngine;

// Bancada da sede: onde se faz cabo do tamanho certo (puxando da caixa de 305 m e crimpando as pontas) e,
// na Etapa 6, onde os equipamentos usados do lote são testados e recuperados (UsadosFisicos / AcoesBancada).
public class Bancada : Interactable
{
    public override string TextoPrompt
    {
        get
        {
            var mao = UsadosFisicos.NaMao;
            if (mao == null) return "[E] Usar a bancada (fazer cabos)";
            var u = UsadosFisicos.Instance;
            return u != null && !u.BancadaLivre ? "Bancada ocupada: tire o equipamento que está em cima" : $"[E] Colocar na bancada: {mao.nome}";
        }
    }

    public override void Interagir()
    {
        if (UsadosFisicos.NaMao != null && UsadosFisicos.Instance != null)
        {
            string r = UsadosFisicos.Instance.ColocarNaBancada();
            if (r != null) GameUI.Instance?.Toast(r, UIFactory.Destaque, 3f);
            return;
        }
        if (GameUI.Instance != null) GameUI.Instance.AbrirBancada(transform.position + transform.forward * 0.5f);
    }

    // Monta uma bancada simples (tampo, pés, caixa de cabo e um rolo) dentro de 'sala'
    public static Bancada Criar(Transform sala, Vector3 posLocal, float rotY)
    {
        var raiz = new GameObject("Bancada");
        raiz.transform.SetParent(sala, false);
        raiz.transform.localPosition = posLocal;
        raiz.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        var madeira = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.38f, 0.22f) };
        var metal = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.26f, 0.28f) };
        var azul = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.45f, 0.85f) };

        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0.86f, 0f), new Vector3(1.3f, 0.05f, 0.6f), madeira);
        foreach (var x in new[] { -0.6f, 0.6f })
            foreach (var z in new[] { -0.25f, 0.25f })
                Peca(raiz.transform, PrimitiveType.Cube, new Vector3(x, 0.42f, z), new Vector3(0.05f, 0.84f, 0.05f), metal);
        // caixa de cabo de 305 m realista (aparece quando há cabo; mostra quanto resta)
        var caixa = new GameObject("CaixaCabo");
        caixa.transform.SetParent(raiz.transform, false);
        caixa.transform.localPosition = new Vector3(-0.4f, 0.885f, 0f);
        caixa.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // etiqueta virada para quem usa a bancada
        caixa.AddComponent<CaixaCaboBancada>();
        // rolo de cabo e alicate no canto da direita (o meio fica livre para os equipamentos usados)
        Peca(raiz.transform, PrimitiveType.Cylinder, new Vector3(0.5f, 0.895f, 0.17f), new Vector3(0.16f, 0.01f, 0.16f), azul);   // rolo de cabo
        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0.36f, 0.893f, 0.2f), new Vector3(0.16f, 0.015f, 0.045f), metal);     // alicate

        var col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.445f, 0f);   // só até o tampo: o que está em cima pode ser mirado
        col.size = new Vector3(1.3f, 0.89f, 0.6f);

        var b = raiz.AddComponent<Bancada>();
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
