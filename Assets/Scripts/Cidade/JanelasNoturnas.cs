using UnityEngine;

// Etapa 9D: janelas das casas e prédios do bairro acendendo à noite. Cada janela tem um "horário" sorteado: no fim da
// tarde vão acendendo aos poucos, perto das 22h começam a apagar (o pessoal vai dormir) e de madrugada sobram poucas.
public class JanelasNoturnas : MonoBehaviour
{
    public Renderer[] janelas;
    float[] sorteio;
    float proxima;

    // Fração das janelas acesas na hora 'h' (0 de dia)
    public static float Acesas(float h)
    {
        if (h >= 6.5f && h < 17.5f) return 0f;
        if (h >= 17.5f && h < 18.75f) return Mathf.Lerp(0.05f, 0.65f, (h - 17.5f) / 1.25f);
        if (h >= 18.75f && h < 22f) return 0.65f;
        if (h >= 22f && h < 23.5f) return Mathf.Lerp(0.65f, 0.25f, (h - 22f) / 1.5f);
        if (h >= 23.5f || h < 5f) return 0.15f;
        return 0.3f;   // 5h–6h30: quem acorda cedo
    }

    void Start()
    {
        if (janelas == null) return;
        var rnd = new System.Random(4242);
        sorteio = new float[janelas.Length];
        for (int i = 0; i < sorteio.Length; i++) sorteio[i] = (float)rnd.NextDouble();
    }

    void Update()
    {
        if (sorteio == null || Time.time < proxima) return;
        proxima = Time.time + 1.5f;
        var d = DiaManager.Instance;
        float f = d == null ? 0f : Acesas(d.Hora);
        for (int i = 0; i < janelas.Length; i++)
        {
            bool acesa = sorteio[i] < f;
            if (janelas[i] != null && janelas[i].enabled != acesa) janelas[i].enabled = acesa;
        }
    }
}
