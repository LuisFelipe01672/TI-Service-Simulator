using PrimeTween;
using UnityEngine;

// Cafeteira da cozinha: um café recupera energia. Intervalo mínimo entre cafés e limite por dia
// (café demais não resolve cansaço — dormir resolve).
public class Cafeteira : Interactable
{
    public float energiaPorCafe = 20f;
    public float intervaloMinutos = 45f;
    public int maximoPorDia = 3;
    public Transform xicara;   // aparece ao lado da cafeteira quando o café fica pronto
    public float bonusCafeDaManha = 10f;   // o primeiro café do dia, logo ao acordar, rende mais (rotina da manhã)
    public static event System.Action CafeTomado;

    int cafesHoje, diaContado = -1;
    float ultimoCafe = -999f;   // minuto do jogo

    public override string TextoPrompt
    {
        get
        {
            var d = DiaManager.Instance;
            AtualizarDia();
            if (cafesHoje >= maximoPorDia) return "Já tomou café demais hoje";
            if (d != null && d.Hora * 60f - ultimoCafe < intervaloMinutos) return "Acabou de tomar um café";
            return $"[E] Passar um café (+{energiaPorCafe:0} de energia)";
        }
    }

    void Awake() => distanciaInteracao = 1.6f;

    void AtualizarDia()
    {
        int dia = DiaManager.Instance != null ? DiaManager.Instance.dia : 0;
        if (dia != diaContado) { diaContado = dia; cafesHoje = 0; ultimoCafe = -999f; }
    }

    public override void Interagir()
    {
        var d = DiaManager.Instance;
        AtualizarDia();
        if (cafesHoje >= maximoPorDia || (d != null && d.Hora * 60f - ultimoCafe < intervaloMinutos)) return;
        bool cafeDaManha = cafesHoje == 0 && d != null && d.Hora < d.horaInicio + 1.5f;   // logo depois de acordar
        float ganho = energiaPorCafe + (cafeDaManha ? bonusCafeDaManha : 0f);
        cafesHoje++;
        ultimoCafe = d != null ? d.Hora * 60f : 0f;
        EnergiaTecnico.Instance?.Recuperar(ganho);
        SomManager.TocarEm("mexer", transform.position, 0.6f, 1.2f);
        GameUI.Instance?.Toast(cafeDaManha ? $"Café da manhã! +{ganho:0} de energia." : $"Café passado! +{ganho:0} de energia.", UIFactory.Sucesso, 2.5f);
        CafeTomado?.Invoke();
        if (xicara != null)
        {
            xicara.gameObject.SetActive(true);
            xicara.localScale = Vector3.zero;
            Tween.Scale(xicara, Vector3.one, 0.35f, Ease.OutBack);
        }
    }
}
