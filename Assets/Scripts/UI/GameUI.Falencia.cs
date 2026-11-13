using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Parte da GameUI: tela de falência (game over).
public partial class GameUI
{
    RectTransform falencia;
    TextMeshProUGUI txtFalencia;
    EventBinding<FalenciaEvent> bFalencia;

    void ConstruirFalencia(RectTransform pai)
    {
        falencia = JanelaModal(pai, "Falencia", new Vector2(680, 0));
        UIFactory.Vertical(falencia, 14, 36, TextAnchor.UpperCenter);
        UIFactory.AjustarAoConteudo(falencia);
        UIFactory.Texto(falencia, "Sua empresa faliu", 42, UIFactory.Perigo, FontStyles.Bold, TextAlignmentOptions.Center);
        txtFalencia = UIFactory.Texto(falencia, "", 20, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
        UIFactory.Botao(falencia, "Recomeçar do zero", Recomecar, UIFactory.Destaque, 600, 58);
        UIFactory.Botao(falencia, "Voltar ao menu", () => { Time.timeScale = 1f; SceneManager.LoadScene(0); }, UIFactory.CartaoClaro, 600, 54);
    }

    void MostrarFalencia(FalenciaEvent e)
    {
        SaveManager.ApagarSave();
        float faturado = 0f;
        if (EmpresaManager.Instance != null) foreach (var f in EmpresaManager.Instance.faturamentoMes) faturado += f;
        int dias = e.dia;
        txtFalencia.text =
            $"{e.motivo}\nSaldo final: <color=#F2545B>{UIFactory.Dinheiro(e.saldo)}</color>\n\n" +
            $"Você durou <b>{dias} dias</b> (até {DiaManager.DataDoDia(dias):dd/MM/yyyy}).\n" +
            $"Chamados resolvidos: <b>{Conquistas.chamados}</b>   ·   Faturamento total: <b>{UIFactory.Dinheiro(faturado)}</b>\n" +
            $"Conquistas: <b>{Conquistas.desbloqueadas.Count}/{Conquistas.Lista.Length}</b>\n\n" +
            "<size=18>Dica: guarde reserva para os custos fixos, cobre quem deve e use o empréstimo antes de o buraco ficar fundo.</size>";
        UIFactory.Limpar(areaToasts);   // as notificações ficam por cima das janelas: aqui elas tapariam o título
        AbrirModal(Modal.Falencia, falencia);
    }

    void Recomecar()
    {
        SaveManager.ApagarSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
