using TMPro;
using UnityEngine;

// Parte da GameUI: painel de objetivos (primeiros passos) no canto superior direito.
public partial class GameUI
{
    RectTransform painelObjetivo;
    TextMeshProUGUI txtObjTitulo, txtObjDica, txtObjProgresso;
    float proximaChecagemObj;

    void ConstruirObjetivos(RectTransform raiz)
    {
        painelObjetivo = UIFactory.Painel(raiz, "Objetivo", UIFactory.Fundo);
        UIFactory.Ancorar(painelObjetivo, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(380, 0));
        UIFactory.Vertical(painelObjetivo, 4, 16);
        UIFactory.AjustarAoConteudo(painelObjetivo);
        txtObjProgresso = UIFactory.Texto(painelObjetivo, "", 15, UIFactory.Destaque, FontStyles.Bold);
        txtObjTitulo = UIFactory.Texto(painelObjetivo, "", 20, UIFactory.CorTexto, FontStyles.Bold);
        txtObjDica = UIFactory.Texto(painelObjetivo, "", 16, UIFactory.CorSuave);
    }

    void AtualizarObjetivos()
    {
        if (painelObjetivo == null || Time.unscaledTime < proximaChecagemObj) return;
        proximaChecagemObj = Time.unscaledTime + 1f;

        string concluido = Objetivos.Atualizar();
        if (concluido != null) Toast($"<b>Objetivo concluído:</b> {concluido}", UIFactory.Sucesso, 4f);

        if (Objetivos.Terminou) { painelObjetivo.gameObject.SetActive(false); return; }
        var o = Objetivos.Atual;
        painelObjetivo.gameObject.SetActive(modal == Modal.Nenhum && o != null);
        if (o == null) return;
        txtObjProgresso.text = $"PRIMEIROS PASSOS  {Objetivos.atual + 1}/{Objetivos.Lista.Length}";
        txtObjTitulo.text = o.titulo;
        txtObjDica.text = o.dica;
    }
}
