using System.Collections.Generic;
using UnityEngine;

// Estado da impressora do cliente (campos imp*, cartucho, fila, papel) e o que ela mostra/faz.
public partial class EstadoEquipamentos
{
    // ---------- Impressora ----------
    public bool impLigada = true;
    public CaboRede impCabo = new CaboRede();
    public bool impTampaAberta;
    public bool impCartuchoPresente = true;
    public bool impCartuchoComLacre;          // cartucho novo colocado sem puxar o lacre: imprime em branco
    // Cartucho novo na mão do técnico (tirado da maleta)
    public bool cartuchoNaMao;
    public bool cartuchoNaMaoComLacre;
    public bool cartuchoNaMaoChacoalhado;
    public float cartuchoNaMaoToner = 1f;     // quanto toner tem o cartucho que está na mão
    public bool cartuchoNaMaoUsado;           // é o que estava na impressora (não um novo da mochila)
    // Papel e cilindro (Fase 3)
    public bool impPapelAtolado;
    public bool impSemPapel;
    public bool impCilindroRiscado;           // listra vertical sempre no mesmo lugar (não é o toner)
    public bool impGavetaAberta;
    public bool impTampaMalFechada;           // a tampa não travou: visor "Feche a tampa"
    public bool impIpMudou;                   // pegou outro IP por DHCP: o computador ainda imprime no IP antigo
    public bool impRelatorioVisto;            // o técnico imprimiu o relatório de configuração de rede
    public bool impDriverErrado;              // driver errado no computador: saem folhas com caracteres estranhos
    // Causas raiz: o sintoma some, mas o problema volta em 1–3 dias se ficarem (retrabalho)
    public bool impPapelUmido;                // resma úmida na gaveta: volta a atolar
    public bool impCaboSolto;                 // cabo solto no chão, no caminho do armário que a limpeza arrasta
    public bool tonerDoCliente;               // chamado guiado: o cliente já comprou um toner novo (fica no armário)
    public const string IpNovoImpressora = "192.168.0.87";
    [System.NonSerialized] public string visorTemporario;     // ex.: contador de páginas (menu Informações)
    [System.NonSerialized] public float visorTemporarioAte;
    public int testesDeCabo;                  // o visual mostra o testador a cada teste
    public DefeitoCabo ultimoTesteCabo;
    public ResultadoPagina ultimaPagina = ResultadoPagina.Nenhuma;
    public int paginasImpressas;              // conta as folhas que saíram (o visual cria uma folha a cada uma)
    public float impToner = 0.8f;             // 0 a 1
    public bool spoolerRodando = true;
    public List<TrabalhoImpressao> fila = new List<TrabalhoImpressao>();
    public string ipImpressora = "192.168.0.50";

    // ---------- Consultas ----------

    public bool ImpressoraOnline => impLigada && impCabo.Funciona && RoteadorFuncionando;
    public bool FilaTravada => fila.Exists(t => t.travado);

    // Mensagem no visor da impressora
    public string VisorImpressora()
    {
        if (impNaCaixa) return "(na caixa)";
        if (!impLigada) return "(desligada)";
        if (impTampaAberta) return "Tampa aberta";
        if (impTampaMalFechada) return "Feche a tampa";
        if (!impCartuchoPresente) return "Instale o cartucho de toner";
        if (impPapelAtolado) return "Atolamento de papel";
        if (impGavetaAberta) return "Feche a gaveta";
        if (impSemPapel) return "Sem papel - bandeja 1";
        if (!impCabo.Funciona) return "Rede desconectada";
        if (impToner < 0.1f) return $"Toner baixo - {Mathf.RoundToInt(impToner * 100)}%";
        if (FilaTravada) return "Erro - imprimindo";
        return "Pronta";
    }

    public string LedRedeImpressora => !impLigada ? "apagado" : impCabo.Funciona ? "verde, piscando" : "apagado";

    // ---------- Ações ----------

    public string Imprimir(bool pelaRede, out bool saiuBoa)
    {
        saiuBoa = false;
        if (!impLigada) return "A impressora está desligada.";
        if (impTampaAberta) return "Feche a tampa da impressora primeiro.";
        if (impTampaMalFechada) return "Nada sai. Visor: \"Feche a tampa\" — mas a tampa parece fechada...";
        if (!impCartuchoPresente) return "Sem cartucho de toner.";
        if (pelaRede)
        {
            if (pcSemImpressora) return "Nenhuma impressora instalada neste computador. Adicione a impressora pelo IP primeiro.";
            if (swPadraoErrado) return "Abriu a janela \"Salvar saída de impressão como...\" pedindo um nome de arquivo: a impressora padrão é \"Imprimir em arquivo PDF\". Nada saiu na impressora.";
            if (!spoolerRodando) return "Erro: o serviço Spooler de Impressão não está em execução.";
            if (!PcNaRede) return "Erro: este computador está sem rede.";
            if (!ImpressoraOnline) return "Impressora offline. O documento ficou na fila.";
            if (impIpMudou) return $"Erro: a impressora não responde em {ipImpressora}. O documento ficou na fila.";
            if (FilaTravada) return "O documento entrou na fila atrás de um trabalho travado e não sai.";
        }
        if (impGavetaAberta) return "A gaveta de papel está aberta. Visor: \"Feche a gaveta\".";
        if (impSemPapel) return "A impressora puxou e não achou folha: a bandeja está vazia.";
        if (impPapelAtolado) return "A folha entrou e travou lá dentro! Visor: \"Atolamento de papel\".";
        // A folha sai: o visual mostra o resultado na bandeja (é a pista)
        paginasImpressas++;
        if (pelaRede && impDriverErrado) { ultimaPagina = ResultadoPagina.Lixo; return "Saíram folhas e mais folhas cheias de símbolos estranhos (driver errado no computador)."; }
        if (impCartuchoComLacre) { ultimaPagina = ResultadoPagina.Branca; return "A folha saiu em branco! (o lacre do toner ainda está no cartucho)"; }
        if (impToner < 0.1f) { ultimaPagina = ResultadoPagina.Clara; return "A folha saiu clarinha, cheia de faixas brancas."; }
        if (impCilindroRiscado) { ultimaPagina = ResultadoPagina.Listra; return "Saiu com uma listra preta vertical, sempre no mesmo lugar da folha."; }
        impToner = Mathf.Max(0f, impToner - 0.002f);
        ultimaPagina = ResultadoPagina.Perfeita;
        saiuBoa = true;
        if (pelaRede) impTestePeloPc = true;
        return impPapelUmido ? "A página saiu legível, mas ondulada e meio mole (o papel parece úmido)." : "A página saiu perfeita!";
    }
}
