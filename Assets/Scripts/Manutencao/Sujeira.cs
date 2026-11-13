using System.Collections.Generic;
using UnityEngine;

// Pedacinhos de fio e capa que caem no chão durante a manutenção.
// Se o técnico for embora sem recolher, o cliente reclama.
public class Sujeira : Interactable
{
    public static readonly List<Sujeira> Todas = new List<Sujeira>();
    static Material matCapa, matCobre, matConector;

    public override string TextoPrompt => "[E] Recolher a sujeira do chão";

    protected override void OnEnable() { base.OnEnable(); Todas.Add(this); }
    protected override void OnDisable() { base.OnDisable(); Todas.Remove(this); }

    public override void Interagir()
    {
        Destroy(gameObject);
        GameUI.Instance?.Toast(Todas.Count <= 1 ? "Chão limpinho!" : $"Recolhido. Ainda tem {Todas.Count - 1} pedaço(s) no chão.", UIFactory.Sucesso, 1.5f);
    }

    // Espalha alguns pedacinhos no chão perto de 'origem'
    public static void Espalhar(Vector3 origem, int quantidade, string tipo)
    {
        if (matCapa == null)
        {
            matCapa = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.45f, 0.85f) };
            matCobre = new Material(Shader.Find("Standard")) { color = new Color(0.85f, 0.5f, 0.25f) };
            matConector = new Material(Shader.Find("Standard")) { color = new Color(0.9f, 0.92f, 0.95f, 1f) };
        }

        var grupo = new GameObject("Sujeira_" + tipo);
        Vector3 centro = origem + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
        if (Physics.Raycast(centro + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore))
            centro.y = hit.point.y;
        grupo.transform.position = centro;

        for (int i = 0; i < quantidade; i++)
        {
            var p = GameObject.CreatePrimitive(tipo == "conector" && i == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(grupo.transform, false);
            p.transform.localPosition = new Vector3(Random.Range(-0.25f, 0.25f), 0.006f, Random.Range(-0.25f, 0.25f));
            p.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            bool cobre = tipo == "fios" && i % 2 == 0;
            p.transform.localScale = tipo == "conector" && i == 0 ? new Vector3(0.015f, 0.012f, 0.022f)
                                   : cobre ? new Vector3(0.004f, 0.008f, 0.004f) : new Vector3(0.007f, 0.02f + Random.value * 0.03f, 0.007f);
            p.GetComponent<Renderer>().sharedMaterial = tipo == "conector" && i == 0 ? matConector : cobre ? matCobre : matCapa;
        }

        var s = grupo.AddComponent<Sujeira>();
        s.distanciaInteracao = 1.6f;
    }

    public static void LimparTudo()
    {
        foreach (var s in new List<Sujeira>(Todas)) if (s != null) Destroy(s.gameObject);
    }
}
