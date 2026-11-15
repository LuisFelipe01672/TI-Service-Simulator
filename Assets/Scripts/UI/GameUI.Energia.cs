using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD: energia do técnico (barra abaixo da reputação)
public partial class GameUI
{
    TextMeshProUGUI txtEnergia;
    RectTransform barraEnergia;
    Image barraEnergiaImg;

    void ConstruirEnergia(RectTransform hud)
    {
        txtEnergia = UIFactory.Texto(hud, "Energia 100%", 20, UIFactory.CorTexto);
        var fundo = UIFactory.Painel(hud, "EnergiaFundo", UIFactory.CartaoClaro);
        fundo.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
        barraEnergia = UIFactory.Painel(fundo, "Barra", UIFactory.Sucesso);
        barraEnergiaImg = barraEnergia.GetComponent<Image>();
        barraEnergia.anchorMin = Vector2.zero;
        barraEnergia.anchorMax = Vector2.one;
        barraEnergia.offsetMin = barraEnergia.offsetMax = Vector2.zero;
    }

    void AtualizarEnergia()
    {
        var e = EnergiaTecnico.Instance;
        if (txtEnergia == null || e == null) return;
        float v = e.energia;
        string estado = v < EnergiaTecnico.Exausto ? "  <color=#F2545B>exausto</color>"
                      : v < EnergiaTecnico.Cansado ? "  <color=#F5B83F>cansado</color>" : "";
        txtEnergia.text = $"Energia {v:0}%{estado}";
        barraEnergia.anchorMax = new Vector2(Mathf.Clamp01(v / 100f), 1f);
        barraEnergiaImg.color = v < EnergiaTecnico.Exausto ? UIFactory.Perigo : v < EnergiaTecnico.Cansado ? UIFactory.Alerta : UIFactory.Sucesso;
    }
}
