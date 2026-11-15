using TMPro;
using UnityEngine;

// Caixa de cabo de rede (305 m) em cima da bancada do escritório: aparece quando há cabo na caixa, com a
// etiqueta do produto, o cabo azul saindo pelo furo e quantos metros restam (anotado na lateral).
public class CaixaCaboBancada : MonoBehaviour
{
    static readonly Vector3 Tam = new Vector3(0.36f, 0.42f, 0.36f);
    GameObject visual;
    TextMeshProUGUI restante;
    int ultimoMetros = -1;

    void Start()
    {
        visual = new GameObject("CaixaCabo305");
        visual.transform.SetParent(transform, false);
        EncomendaFisica.CriarCaixa(visual.transform, Tam);
        Destroy(visual.GetComponent<BoxCollider>());   // a bancada já tem colisor
        EncomendaFisica.Etiqueta(visual.transform, Tam, "CABO DE REDE U/UTP Cat6\n305 m · 23 AWG · azul", true);
        EncomendaFisica.CaboSaindo(visual.transform, Tam);

        // "Restam X m" escrito na lateral
        var lado = EncomendaFisica.Etiqueta(visual.transform, Tam, "", false);
        lado.transform.parent.localPosition = new Vector3(Tam.x / 2f + 0.002f, Tam.y * 0.3f, 0f);
        lado.transform.parent.localRotation = Quaternion.Euler(0, -90f, 0);
        restante = lado;
        restante.color = new Color(0.1f, 0.15f, 0.5f);
    }

    void Update()
    {
        var m = Mochila.Instance;
        int metros = m != null ? m.MetrosNaCaixa : 0;
        if (metros == ultimoMetros) return;
        ultimoMetros = metros;
        if (visual != null) visual.SetActive(metros > 0);
        if (restante != null) restante.text = $"Restam\n{metros} m";
    }
}
