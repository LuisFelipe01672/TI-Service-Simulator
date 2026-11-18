using System.Collections.Generic;
using UnityEngine;

// Luz de poste da rua: acende no fim da tarde e apaga de manhã, pelo relógio do DiaManager.
// A lâmpada (material que brilha) acende em todos; a luz de verdade (que ilumina o chão) só nos postes mais perto da
// câmera — com 34 postes acesos o "Ultra" desenhava cada objeto várias vezes e a noite caía para ~70 FPS (Etapa 9D).
[RequireComponent(typeof(Light))]
public class PosteLuz : MonoBehaviour
{
    public Renderer lampada;
    public Material acesa, apagada;

    public const int LuzesDeVerdade = 8;
    public const float AlcanceLuzes = 55f;

    static readonly List<PosteLuz> todos = new List<PosteLuz>();
    static int quadroEscolha = -1;
    static float proximaEscolha;

    Light luz;
    bool noite;
    float proximaChecagem;

    void Awake() => luz = GetComponent<Light>();
    void OnEnable() => todos.Add(this);
    void OnDisable() => todos.Remove(this);

    public static bool HoraDeAcender(float hora) => hora >= 17.5f || hora < 6.5f;

    void Update()
    {
        if (Time.time >= proximaChecagem)
        {
            proximaChecagem = Time.time + 0.5f;
            var d = DiaManager.Instance;
            bool ligar = d == null || HoraDeAcender(d.Hora);
            if (ligar != noite)
            {
                noite = ligar;
                if (lampada != null && acesa != null && apagada != null) lampada.sharedMaterial = ligar ? acesa : apagada;
                if (!ligar) luz.enabled = false;
            }
        }
        // Um poste por quadro escolhe quais ficam com a luz de verdade (os mais perto da câmera)
        if (quadroEscolha != Time.frameCount && Time.time >= proximaEscolha)
        {
            quadroEscolha = Time.frameCount;
            proximaEscolha = Time.time + 0.25f;
            Escolher();
        }
    }

    static void Escolher()
    {
        var cam = Camera.main;
        Vector3 o = cam != null ? cam.transform.position : Vector3.zero;
        todos.Sort((a, b) => (a.transform.position - o).sqrMagnitude.CompareTo((b.transform.position - o).sqrMagnitude));
        int n = 0;
        foreach (var p in todos)
        {
            bool ligar = p.noite && n < LuzesDeVerdade && (p.transform.position - o).sqrMagnitude < AlcanceLuzes * AlcanceLuzes;
            if (ligar) n++;
            if (p.luz != null && p.luz.enabled != ligar) p.luz.enabled = ligar;
        }
    }
}
