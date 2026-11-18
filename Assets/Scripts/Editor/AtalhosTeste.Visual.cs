using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 10D (visual do tablet, da loja com fotos e do TI OS). Menu "TI Service/Teste (Play)/Visual".
public static partial class AtalhosTeste
{
    const string MenuVisual = Menu + "Visual/";

    static void AbaTablet(int aba, float rolagem = 0f)
    {
        CampoGameUI("abasLiberadas", 0xFFFF);
        GameUI.Instance?.AbrirTablet();
        ChamarGameUI("TrocarAba", aba);
        if (rolagem > 0f)
        {
            var f = typeof(GameUI).GetField("tabletConteudo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f?.GetValue(GameUI.Instance) is RectTransform rt) rt.anchoredPosition = new Vector2(0f, rolagem);
        }
    }

    [MenuItem(MenuVisual + "1. Fotos da interface (tablet, loja, TI OS, celular)", false, 1400)]
    static void FotosInterface()
    {
        if (Jogador() == null) return;
        var l = new List<(System.Action, float, string)>
        {
            (() => { TesteDriver(); }, 2.5f, null),
            (() => AbaTablet(0), 1.0f, "ui_1_tablet_chamados"),
            (() => AbaTablet(2), 1.0f, "ui_2_loja_topo"),
            (() => AbaTablet(2, 520f), 1.0f, "ui_3_loja_vitrine"),
            (() => AbaTablet(2, 1300f), 1.0f, "ui_4_loja_vitrine_2"),
            (() => { CampoGameUI("secaoNegocios", null); AbaTablet(6); }, 1.0f, "ui_5_negocios"),
            (() => AbaTablet(3), 1.0f, "ui_6_empresa"),
            (() => { GameUI.Instance?.AbrirComputador(); CampoGameUI("appAtual", "Impressoras"); ChamarGameUI("MontarComputador"); }, 1.2f, "ui_7_tios_impressoras"),
            (() => { GameUI.Instance?.AbrirComputador(); CampoGameUI("appAtual", "Prompt"); ChamarGameUI("MontarComputador"); }, 1.2f, "ui_8_tios_prompt"),
            (() => GameUI.Instance?.AbrirCelular(), 1.0f, "ui_9_celular"),
        };
        SequenciaFotos("interface", l);
    }
}
