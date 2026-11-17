using UnityEngine;

// Sons gerados por código (ondas e ruído com envelope). Servem enquanto não há arquivos de som:
// qualquer arquivo em Resources/Sons/<nome> substitui o som gerado com o mesmo nome.
public static class SonsGerados
{
    const int Taxa = 44100;
    static System.Random rnd = new System.Random(7);

    public static AudioClip Gerar(string nome)
    {
        switch (nome)
        {
            case "clique": return Criar(nome, 0.045f, (t, i) => Ruido() * Dec(t, 140f) * 0.5f + Seno(1900f, t) * Dec(t, 90f) * 0.35f);
            case "aviso": return Criar(nome, 0.28f, (t, i) => Seno(t < 0.1f ? 880f : 1320f, t) * Ataque(t, 0.005f) * Dec(t < 0.1f ? t : t - 0.1f, 14f) * 0.4f);
            case "dinheiro":
                return Criar(nome, 0.35f, (t, i) =>
                    (Seno(1568f, t) + 0.4f * Seno(3136f, t)) * Ataque(t, 0.003f) * Dec(t, 30f) * (t < 0.07f ? 1f : 0f) * 0.4f +
                    (t >= 0.07f ? (Seno(2093f, t) + 0.4f * Seno(4186f, t)) * Dec(t - 0.07f, 9f) * 0.4f : 0f));
            case "sucesso": return Arpejo(nome, new[] { 523f, 659f, 784f, 1047f }, 0.08f, 0.35f);
            case "conquista": return Arpejo(nome, new[] { 784f, 1047f, 1319f, 1568f, 2093f }, 0.07f, 0.5f);
            case "erro":
                return Criar(nome, 0.38f, (t, i) => (Quadrada(140f, t) + Quadrada(147f, t)) * 0.18f * Ataque(t, 0.01f) * Dec(t, 6f));
            case "porta":
            {
                float lp = 0f;
                return Criar(nome, 0.45f, (t, i) =>
                {
                    lp += (Ruido() - lp) * 0.08f;   // ruído abafado (madeira)
                    float rangido = Seno(210f + 40f * Mathf.Sin(t * 30f), t) * 0.15f * Mathf.Clamp01(t * 6f) * Dec(t, 5f);
                    float batida = (lp * 2.5f + Seno(85f, t) * 0.6f) * Dec(t, 18f);
                    return batida * 0.6f + rangido;
                });
            }
            case "passo":
            {
                float lp = 0f;
                return Criar(nome, 0.14f, (t, i) => { lp += (Ruido() - lp) * 0.12f; return (lp * 2.2f + Seno(70f, t) * 0.4f) * Ataque(t, 0.004f) * Dec(t, 38f) * 0.7f; });
            }
            case "mexer":
            {
                float lp = 0f;
                return Criar(nome, 0.12f, (t, i) => { lp += (Ruido() - lp) * 0.35f; return (lp * 1.4f + Seno(1200f, t) * 0.15f) * Dec(t, 40f) * 0.6f; });
            }
            case "impressora":
            {
                float lp = 0f;
                return Criar(nome, 1.8f, (t, i) =>
                {
                    lp += (Ruido() - lp) * 0.2f;
                    float motor = Serra(110f, t) * 0.18f + lp * 0.5f * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 11f));
                    float env = Mathf.Clamp01(t * 8f) * Mathf.Clamp01((1.8f - t) * 6f);
                    return motor * env * 0.7f;
                });
            }
            case "caneta": return Criar(nome, 1.4f, (t, i) => Ruido() * 0.16f * (0.55f + 0.45f * Mathf.Sin(t * 42f)) * Mathf.Clamp01(t * 20f) * Mathf.Clamp01((1.4f - t) * 10f));   // assinatura no papel
            case "bip": return Criar(nome, 0.13f, (t, i) => Seno(1250f, t) * Ataque(t, 0.004f) * Mathf.Clamp01((0.13f - t) * 40f) * 0.35f);
            // Etapa 8: furadeira de impacto (motor zunindo + batidas) para fixar caixa e canaleta
            case "furadeira": return Criar(nome, 1.3f, (t, i) => (Seno(180f + 40f * Mathf.Min(t * 6f, 1f), t) * 0.25f + Ruido() * 0.18f * (0.6f + 0.4f * Mathf.Sin(t * 260f)))
                                                              * Mathf.Clamp01(t * 25f) * Mathf.Clamp01((1.3f - t) * 8f));
            case "dormir": return Arpejo(nome, new[] { 784f, 659f, 523f, 392f }, 0.14f, 0.6f);
            // Etapa 9C: carro
            case "porta_carro":
            {
                // batida seca de lata + trinco
                float lp = 0f;
                return Criar(nome, 0.5f, (t, i) =>
                {
                    lp += (Ruido() - lp) * 0.15f;
                    float lata = (Seno(95f, t) * 0.7f + Seno(240f, t) * 0.25f + lp * 1.6f) * Dec(t, 14f);
                    float trinco = t > 0.03f ? Ruido() * 0.35f * Dec(t - 0.03f, 90f) : 0f;
                    return lata * 0.7f + trinco;
                });
            }
            case "porta_malas":
            {
                float lp = 0f;
                return Criar(nome, 0.6f, (t, i) => { lp += (Ruido() - lp) * 0.1f; return (Seno(75f, t) * 0.6f + lp * 1.8f) * Dec(t, 10f) * 0.7f; });
            }
            case "partida":
            {
                // motor de arranque girando e o motor pegando
                return Criar(nome, 1.1f, (t, i) =>
                {
                    float arranque = t < 0.6f ? Serra(18f + t * 10f, t) * 0.3f * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 9f)) : 0f;
                    float pega = t >= 0.55f ? (Serra(32f, t) * 0.35f + Ruido() * 0.08f) * Mathf.Clamp01((t - 0.55f) * 10f) * Mathf.Clamp01((1.1f - t) * 6f) : 0f;
                    return arranque + pega;
                });
            }
            case "motor":
            {
                // marcha lenta de um 1.0 de 4 cilindros (o pitch sobe com a rotação); loop
                float lp = 0f;
                return Criar(nome, 2f, (t, i) =>
                {
                    lp += (Ruido() - lp) * 0.05f;
                    float giro = 25f;   // explosões por segundo na lenta (~750 rpm)
                    return (Serra(giro, t) * 0.32f + Seno(giro * 2f, t) * 0.12f + lp * 0.9f) * 0.8f;
                }, true);
            }
            case "buzina": return Criar(nome, 0.55f, (t, i) => (Quadrada(420f, t) * 0.5f + Quadrada(500f, t) * 0.5f) * 0.22f * Mathf.Clamp01(t * 60f) * Mathf.Clamp01((0.55f - t) * 30f));
            case "bomba":
            {
                // bomba de combustível: motor elétrico zunindo + líquido correndo
                float lp = 0f;
                return Criar(nome, 2f, (t, i) => { lp += (Ruido() - lp) * 0.3f; return (Seno(120f, t) * 0.12f + Seno(240f, t) * 0.05f + lp * 0.35f) * 0.8f; }, true);
            }
            case "freio_mao": return Criar(nome, 0.35f, (t, i) => Ruido() * 0.3f * (0.5f + 0.5f * Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 38f))) * Mathf.Clamp01((0.35f - t) * 10f));
            case "ambiente_dentro":
            {
                // Zumbido de ar-condicionado/lâmpadas + ar parado
                float lp = 0f;
                return Criar(nome, 6f, (t, i) => { lp += (Ruido() - lp) * 0.01f; return lp * 1.5f + Seno(60f, t) * 0.03f + Seno(120f, t) * 0.015f; }, true);
            }
            case "ambiente_rua":
            {
                // Vento e trânsito distante: ruído abafado que sobe e desce devagar
                float lp = 0f, lp2 = 0f;
                return Criar(nome, 8f, (t, i) =>
                {
                    lp += (Ruido() - lp) * 0.02f;
                    lp2 += (Ruido() - lp2) * 0.004f;
                    float onda = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI / 8f);   // um "carro" passando a cada volta
                    return (lp * 1.2f * onda + lp2 * 3f) * 0.8f;
                }, true);
            }
            default: return null;
        }
    }

    // ---------- Montagem ----------

    delegate float Amostra(float t, int i);

    static AudioClip Criar(string nome, float duracao, Amostra f, bool loop = false)
    {
        int n = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[n];
        for (int i = 0; i < n; i++) dados[i] = Mathf.Clamp(f(i / (float)Taxa, i), -1f, 1f);

        // Loop sem estalo: mistura o fim com o começo
        if (loop)
        {
            int cruz = Mathf.Min(n / 4, Taxa / 2);
            for (int i = 0; i < cruz; i++)
            {
                float k = i / (float)cruz;
                dados[i] = dados[i] * k + dados[n - cruz + i] * (1f - k);
            }
            System.Array.Resize(ref dados, n - cruz);
            n -= cruz;
        }

        var clip = AudioClip.Create(nome, n, 1, Taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    static AudioClip Arpejo(string nome, float[] notas, float passo, float cauda)
    {
        float dur = notas.Length * passo + cauda;
        return Criar(nome, dur, (t, i) =>
        {
            float s = 0f;
            for (int k = 0; k < notas.Length; k++)
            {
                float tk = t - k * passo;
                if (tk < 0f) continue;
                s += (Seno(notas[k], tk) + 0.3f * Seno(notas[k] * 2f, tk)) * Ataque(tk, 0.004f) * Dec(tk, k == notas.Length - 1 ? 5f : 12f);
            }
            return s * 0.22f;
        });
    }

    static float Seno(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float Quadrada(float f, float t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
    static float Serra(float f, float t) => 2f * (t * f - Mathf.Floor(t * f + 0.5f));
    static float Ruido() => (float)(rnd.NextDouble() * 2.0 - 1.0);
    static float Dec(float t, float k) => Mathf.Exp(-t * k);
    static float Ataque(float t, float a) => Mathf.Clamp01(t / a);
}
