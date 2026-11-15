using UnityEngine;

// Gira os ponteiros de um relógio de verdade (modelos com ponteiros separados) conforme a hora do jogo.
// Os ponteiros são achados pelo nome ("hour"/"houd", "minute", "second"). A posição inicial de cada ponteiro é
// medida pela própria malha (do pivô até o centro do ponteiro), então funciona com qualquer modelo.
public class RelogioAnalogico : MonoBehaviour
{
    [Tooltip("Direção (no mundo) para onde o mostrador está virado — para dentro do cômodo")]
    public Vector3 frente = Vector3.back;

    class Ponteiro { public Transform t; public Quaternion rotInicial; public float anguloInicial; public float voltasPorHora; }
    Ponteiro hora, minuto, segundo;
    Vector3 eixo;

    void Start()
    {
        eixo = frente.normalized;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (t.GetComponent<Renderer>() == null && t.childCount > 0) continue;
            if (n.Contains("second")) segundo = Criar(t, 60f);
            else if (n.Contains("minute")) minuto = Criar(t, 1f);
            else if (n.Contains("hour") || n.Contains("houd")) hora = Criar(t, 1f / 12f);
        }
    }

    Ponteiro Criar(Transform t, float voltasPorHora)
    {
        var r = t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>();
        if (r == null) return null;
        // direção para onde o ponteiro aponta: do pivô até o centro da malha, no plano do mostrador
        Vector3 dir = Vector3.ProjectOnPlane(r.bounds.center - t.position, eixo);
        Vector3 cima = Vector3.ProjectOnPlane(Vector3.up, eixo);
        return new Ponteiro
        {
            t = t, rotInicial = t.rotation, voltasPorHora = voltasPorHora,
            anguloInicial = dir.sqrMagnitude > 1e-8f ? Vector3.SignedAngle(cima, dir, eixo) : 0f,
        };
    }

    void LateUpdate()
    {
        var d = DiaManager.Instance;
        if (d == null) return;
        float horas = d.Hora;   // 0–24, com fração
        Aplicar(hora, horas);
        Aplicar(minuto, horas);
        // segundos em tempo real, aos saltos (o tempo do jogo é acelerado e giraria rápido demais)
        if (segundo != null && segundo.t != null)
        {
            float alvo = Mathf.Floor(Time.time % 60f) * 6f;
            segundo.t.rotation = Quaternion.AngleAxis(alvo - segundo.anguloInicial, eixo) * segundo.rotInicial;
        }
    }

    // Ângulo no sentido horário visto de frente (o eixo aponta para quem olha)
    void Aplicar(Ponteiro p, float horas)
    {
        if (p == null || p.t == null) return;
        float alvo = Mathf.Repeat(horas * p.voltasPorHora, 1f) * 360f;
        p.t.rotation = Quaternion.AngleAxis(alvo - p.anguloInicial, eixo) * p.rotInicial;
    }
}
