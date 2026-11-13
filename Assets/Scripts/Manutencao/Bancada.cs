using UnityEngine;

// Bancada da sede: onde se faz cabo do tamanho certo (puxando da caixa de 305 m e crimpando as pontas).
public class Bancada : Interactable
{
    public override string TextoPrompt => "[E] Usar a bancada (fazer cabos)";

    public override void Interagir()
    {
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
        var papelao = new Material(Shader.Find("Standard")) { color = new Color(0.7f, 0.55f, 0.35f) };
        var azul = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.45f, 0.85f) };

        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0.86f, 0f), new Vector3(1.3f, 0.05f, 0.6f), madeira);
        foreach (var x in new[] { -0.6f, 0.6f })
            foreach (var z in new[] { -0.25f, 0.25f })
                Peca(raiz.transform, PrimitiveType.Cube, new Vector3(x, 0.42f, z), new Vector3(0.05f, 0.84f, 0.05f), metal);
        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(-0.4f, 1.06f, 0f), new Vector3(0.35f, 0.35f, 0.35f), papelao);       // caixa de cabo
        Peca(raiz.transform, PrimitiveType.Cylinder, new Vector3(0.25f, 0.9f, 0.05f), new Vector3(0.22f, 0.02f, 0.22f), azul);   // rolo de cabo
        Peca(raiz.transform, PrimitiveType.Cube, new Vector3(0.45f, 0.9f, -0.15f), new Vector3(0.18f, 0.02f, 0.05f), metal);     // alicate

        var col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.6f, 0f);
        col.size = new Vector3(1.3f, 1.2f, 0.6f);

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
