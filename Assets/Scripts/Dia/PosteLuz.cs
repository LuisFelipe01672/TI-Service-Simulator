using UnityEngine;

// Luz de poste da rua: acende no fim da tarde e apaga de manhã, pelo relógio do DiaManager.
[RequireComponent(typeof(Light))]
public class PosteLuz : MonoBehaviour
{
    public Renderer lampada;
    public Material acesa, apagada;

    Light luz;
    float proximaChecagem;

    void Awake() => luz = GetComponent<Light>();

    void Update()
    {
        if (Time.time < proximaChecagem) return;
        proximaChecagem = Time.time + 0.5f;

        var d = DiaManager.Instance;
        bool ligar = d == null || d.Hora >= 17.5f || d.Hora < 6.5f;
        if (luz.enabled == ligar) return;
        luz.enabled = ligar;
        if (lampada != null && acesa != null && apagada != null) lampada.sharedMaterial = ligar ? acesa : apagada;
    }
}
