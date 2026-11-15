using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Interruptor de parede: E liga/desliga as luzes ligadas a ele (Light e a emissão das lâmpadas).
// A tecla gira um pouco (animação) e faz o clique.
public class Interruptor : Interactable
{
    public List<Light> luzes = new List<Light>();
    public List<Renderer> lampadas = new List<Renderer>();   // objetos que brilham quando acesos
    public Transform tecla;
    public bool ligado = true;
    public string comodo = "do quarto";   // com o artigo: "do quarto", "da sala"

    readonly Dictionary<Renderer, Material[]> materiaisAcesos = new Dictionary<Renderer, Material[]>();
    static Material matApagada;

    public override string TextoPrompt => ligado ? $"[E] Apagar a luz {comodo}" : $"[E] Acender a luz {comodo}";

    void Awake()
    {
        distanciaInteracao = 1.6f;
        foreach (var r in lampadas) if (r != null) materiaisAcesos[r] = r.sharedMaterials;
    }

    public override void Interagir()
    {
        ligado = !ligado;
        Aplicar();
        SomManager.TocarEm("clique", transform.position, 0.7f, 1.4f);
        if (tecla != null)
        {
            Tween.StopAll(tecla);
            Tween.LocalRotation(tecla, Quaternion.Euler(ligado ? -12f : 12f, 0f, 0f), 0.08f, Ease.OutQuad);
        }
    }

    void Aplicar()
    {
        foreach (var l in luzes) if (l != null) l.enabled = ligado;
        if (matApagada == null)
        {
            matApagada = new Material(Shader.Find("Standard")) { name = "Lampada_Apagada_Runtime" };
            matApagada.color = new Color(0.85f, 0.85f, 0.82f);
        }
        foreach (var kv in materiaisAcesos)
        {
            if (kv.Key == null) continue;
            if (ligado) kv.Key.sharedMaterials = kv.Value;
            else
            {
                var m = new Material[kv.Value.Length];
                for (int i = 0; i < m.Length; i++) m[i] = TemEmissao(kv.Value[i]) ? matApagada : kv.Value[i];
                kv.Key.sharedMaterials = m;
            }
        }
    }

    static bool TemEmissao(Material m) => m != null && m.IsKeywordEnabled("_EMISSION");
}
