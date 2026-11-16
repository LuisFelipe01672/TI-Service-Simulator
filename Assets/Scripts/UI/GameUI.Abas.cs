using UnityEngine;

// Parte da GameUI: as abas do tablet aparecem aos poucos, seguindo a trilha "Primeiros passos" (Objetivos.cs).
// Como os objetivos são conferidos pelo estado do jogo, um save antigo já abre com as abas que usava.
public partial class GameUI
{
    // Aba nunca fica escondida depois de aparecer (nem se o estado do jogo voltar atrás)
    int abasLiberadas = -1;   // bits por número de aba; -1 = ainda não calculado (não avisa ao carregar)

    // Número do objetivo a partir do qual cada aba aparece (0 = primeiro chamado, 1 = ferramenta, 2 = recibo...)
    static int ObjetivoDaAba(int aba)
    {
        switch (aba)
        {
            case 3: return 2;   // Empresa: "Emita um recibo"
            case 6: return 5;   // Negócios: "Feche uma parceria com fornecedor"
            case 4: return 6;   // Contratos: "Feche o primeiro contrato mensal"
            case 5: return 6;   // Central (alertas dos contratos)
            case 1: return 9;   // Equipe: perto de "Contrate o primeiro funcionário"
            default: return 0;  // Chamados e Loja: desde o começo
        }
    }

    static string NomeDaAba(int aba) =>
        aba == 1 ? "Equipe" : aba == 2 ? "Loja" : aba == 3 ? "Empresa" : aba == 4 ? "Contratos" : aba == 5 ? "Central" : aba == 6 ? "Negócios" : "Chamados";

    void AplicarAbasLiberadas()
    {
        if (abaChamados == null) return;
        if (Objetivos.atual < 0) Objetivos.Atualizar();
        int progresso = Objetivos.Terminou ? int.MaxValue : Mathf.Max(0, Objetivos.atual);
        int alertas = ServicosManager.Instance != null ? ServicosManager.Instance.alertas.Count : 0;

        int bits = 0;
        for (int aba = 0; aba <= 6; aba++)
            if (progresso >= ObjetivoDaAba(aba) || (aba == 5 && alertas > 0)) bits |= 1 << aba;

        bool primeiraVez = abasLiberadas < 0;
        int novas = primeiraVez ? 0 : bits & ~abasLiberadas;
        abasLiberadas = primeiraVez ? bits : abasLiberadas | bits;
        for (int aba = 0; aba <= 6; aba++)
            if ((novas & (1 << aba)) != 0)
                Toast($"Nova aba no tablet: <b>{NomeDaAba(aba)}</b>.", UIFactory.Destaque, 4f);

        abaEquipe.gameObject.SetActive((abasLiberadas & (1 << 1)) != 0);
        abaEmpresa.gameObject.SetActive((abasLiberadas & (1 << 3)) != 0);
        abaContratos.gameObject.SetActive((abasLiberadas & (1 << 4)) != 0);
        abaCentral.gameObject.SetActive((abasLiberadas & (1 << 5)) != 0);
        abaNegocios.gameObject.SetActive((abasLiberadas & (1 << 6)) != 0);
        if ((abasLiberadas & (1 << abaAtiva)) == 0) abaAtiva = 0;
    }
}
