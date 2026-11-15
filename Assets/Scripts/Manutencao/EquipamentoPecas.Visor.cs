using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Visor LCD da impressora realista: mensagem de verdade conforme o estado ("Pronta", "Tampa aberta",
// "Toner baixo"...). Fica sobre o visor do modelo, inclinado para quem olha de frente, e levanta com o scanner.
public partial class EquipamentoPecas
{
    TextMeshProUGUI txtLcd;
    Image fundoLcd;
    const float PixelsPorMetroLcd = 4000f;

    void CriarLcd(Renderer visorModelo)
    {
        var b = visorModelo.bounds;
        // vetor "para quem olha": para cima, inclinado 35° para a frente da impressora (-Z do Visual)
        Vector3 cima = visual.up, frente = -visual.forward;
        Vector3 paraQuemOlha = (cima * Mathf.Cos(35f * Mathf.Deg2Rad) + frente * Mathf.Sin(35f * Mathf.Deg2Rad)).normalized;

        var go = new GameObject("LCD", typeof(RectTransform));
        go.transform.SetParent(visorModelo.transform.parent, true);   // vai junto com a parte de cima
        go.transform.position = b.center + cima * 0.006f;
        go.transform.rotation = Quaternion.LookRotation(-paraQuemOlha, visual.forward);
        go.transform.localScale = Vector3.one / PixelsPorMetroLcd / go.transform.parent.lossyScale.x;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0.085f, 0.03f) * PixelsPorMetroLcd;   // LCD de 8,5 x 3 cm

        fundoLcd = new GameObject("Fundo", typeof(RectTransform)).AddComponent<Image>();
        fundoLcd.transform.SetParent(rt, false);
        UIFactory.Esticar(fundoLcd.rectTransform);
        fundoLcd.color = new Color(0.55f, 0.75f, 0.95f);   // retroiluminação azulada de LCD monocromático

        txtLcd = UIFactory.Texto(rt, "Pronta", 60, new Color(0.04f, 0.08f, 0.16f), FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(txtLcd.rectTransform, 6);
        txtLcd.enableAutoSizing = true;
        txtLcd.fontSizeMin = 14;
        txtLcd.fontSizeMax = 60;
    }

    void AtualizarLcd(EstadoEquipamentos e, bool ligada)
    {
        if (txtLcd == null) return;
        fundoLcd.color = ligada ? new Color(0.55f, 0.75f, 0.95f) : new Color(0.08f, 0.1f, 0.12f);
        string msg;
        if (!ligada) msg = "";
        else if (e == null) msg = "Pronta";
        else if (e.impTampaAberta) msg = "Tampa aberta";
        else if (!e.impCartuchoPresente) msg = "Instale o toner";
        else if (e.impToner < 0.1f) msg = "Toner esgotado";
        else if (e.impToner < 0.25f) msg = "Toner baixo";
        else if (!e.ImpressoraOnline) msg = "Sem rede";
        else msg = "Pronta";
        if (txtLcd.text != msg) txtLcd.text = msg;
    }
}
