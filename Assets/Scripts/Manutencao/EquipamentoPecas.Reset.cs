using PrimeTween;
using UnityEngine;

// Etapa 8: reset do roteador com o clipe. Enquanto o técnico segura E na ação do reset, o clipe desdobrado entra
// no furo de trás e fica apertando o botão; perto dos 10 s todas as luzes piscam juntas (o sinal de que vai voltar
// ao padrão de fábrica). Soltou antes: o clipe sai. (O roteador não sai do lugar: o modelo da cena tem geometria
// duplicada que aparece se o Visual girar.)
public partial class EquipamentoPecas
{
    Vector3 furoReset;
    Transform clipe;
    bool clipeNoFuro;
    float clipeProgresso;
    static Material mClipe;
    public static float testeClipeAte;   // atalho de teste: simula segurar E no reset até este Time.time

    void AtualizarClipe()
    {
        var a = GameUI.AcaoSegurada;
        bool segurando = a != null && a.animacao == "clipe" && GameUI.EquipamentoSegurado == eq;
        clipeProgresso = segurando ? Mathf.Clamp01(GameUI.ProgressoSegurado) : 0f;
        if (!segurando && Time.time < testeClipeAte) { segurando = true; clipeProgresso = 1f - (testeClipeAte - Time.time) / 10f; }
        if (segurando == clipeNoFuro || visual == null) return;
        clipeNoFuro = segurando;

        if (segurando)
        {
            if (clipe == null) clipe = MontarClipe(visual);
            clipe.gameObject.SetActive(true);
            Vector3 fora = furoReset + new Vector3(0f, 0f, 0.07f), dentro = furoReset + new Vector3(0f, 0f, 0.012f);
            clipe.localPosition = fora;
            Sequence.Create()
                .ChainDelay(0.3f)
                .Chain(Tween.LocalPosition(clipe, dentro, 0.35f, Ease.OutCubic))
                .ChainCallback(clipe, c => SomManager.TocarEm("clique", c.position, 0.6f, 2.2f));   // o botãozinho lá dentro
        }
        else
        {
            if (clipe != null)
            {
                var c = clipe;
                Tween.LocalPosition(c, furoReset + new Vector3(0f, 0f, 0.09f), 0.25f, Ease.InCubic)
                    .OnComplete(c, x => x.gameObject.SetActive(false));
            }
        }
    }

    // Clipe de papel desdobrado: a perna reta entra no furo, a volta fica para fora (na mão do técnico)
    static Transform MontarClipe(Transform pai)
    {
        if (mClipe == null)
        {
            mClipe = Mat(new Color(0.78f, 0.8f, 0.83f));
            mClipe.SetFloat("_Metallic", 0.9f);
            mClipe.SetFloat("_Glossiness", 0.75f);
        }
        var t = new GameObject("Clipe").transform;
        t.SetParent(pai, false);
        void Arame(Vector3 a, Vector3 b)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(t, false);
            c.transform.localPosition = (a + b) / 2f;
            c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            c.transform.localScale = new Vector3(0.0016f, (b - a).magnitude / 2f, 0.0016f);
            c.GetComponent<Renderer>().sharedMaterial = mClipe;
        }
        // perna reta (z+ = para fora do furo) e a volta do clipe
        Arame(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.045f));
        Arame(new Vector3(0f, 0f, 0.045f), new Vector3(0f, 0.006f, 0.05f));
        Arame(new Vector3(0f, 0.006f, 0.05f), new Vector3(0f, 0.012f, 0.045f));
        Arame(new Vector3(0f, 0.012f, 0.045f), new Vector3(0f, 0.012f, 0.012f));
        Arame(new Vector3(0f, 0.012f, 0.012f), new Vector3(0f, 0.016f, 0.008f));
        Arame(new Vector3(0f, 0.016f, 0.008f), new Vector3(0f, 0.02f, 0.012f));
        Arame(new Vector3(0f, 0.02f, 0.012f), new Vector3(0f, 0.02f, 0.035f));
        return t;
    }
}
