// Etapa 5 — Locação física: a impressora alugada chega na caixa e é instalada no cliente; todo mês o técnico
// visita, lê o contador no visor, troca o toner se estiver baixo, deixa reserva e faz a troca preventiva.
public partial class EstadoEquipamentos
{
    // ---------- Instalação ----------
    public bool impNaCaixa;                   // a impressora ainda está lacrada na caixa de papelão
    public bool impNoEscritorio;              // Etapa 9C: a caixa ainda está no escritório (o técnico leva de carro)
    public bool impComFitas;                  // fitas laranja e calços de transporte ainda presos
    public bool tonerDaCaixa;                 // o toner que veio na caixa ainda não foi instalado
    public bool impIpPendente;                // instalação: a impressora pegou um IP por DHCP; precisa fixar no painel
    public bool pcSemImpressora;              // o computador do cliente ainda não tem a impressora adicionada
    public bool impTestePeloPc;               // a página de teste saiu boa mandando pelo computador (rede)
    public int caixasAbertas;                 // o visual anima a caixa abrindo a cada vez
    public const string IpDhcpNovaImpressora = "192.168.0.112";

    // ---------- Visita mensal ----------
    public bool visita;                       // este atendimento é a visita mensal do contrato
    public bool contadorLido;
    public bool visitaTonerBaixo;             // chegou com o toner baixo: precisa trocar
    public bool preventivaPendente;           // unidade de imagem perto do fim: troca preventiva
    public int reservasDeixadas;              // toners reserva deixados nesta visita (o visual põe a caixa ao lado)
    public int trocasCilindro;                // o visual anima a troca da unidade de imagem
    public const float TonerBaixoVisita = 0.3f;

    public bool instalacaoEmAndamento;        // o chamado é uma instalação (fica true até o fim)

    // A impressora foi tirada da caixa, montada, configurada e testada pelo computador
    public bool InstalacaoCompleta =>
        !impNaCaixa && !impComFitas && !tonerDaCaixa && !impIpPendente && !pcSemImpressora && impTestePeloPc;

    // Contador anotado, toner bom (trocado se estava baixo) e unidade de imagem trocada se estava no fim
    public bool VisitaCompleta =>
        contadorLido && (!visitaTonerBaixo || impToner >= TonerBaixoVisita) && !preventivaPendente;

    // A visita começa com a impressora como o contrato está: toner do mês e uso da unidade de imagem
    public void PrepararVisita(float suprimento, float usoPeca)
    {
        visita = true;
        contadorLido = false;
        impToner = UnityEngine.Mathf.Clamp01(suprimento);
        visitaTonerBaixo = impToner < TonerBaixoVisita;
        preventivaPendente = usoPeca >= 0.7f;
    }

    // TI OS: Impressoras > Adicionar > por endereço TCP/IP
    public string AdicionarImpressoraPeloIp(out bool ok)
    {
        ok = false;
        if (!pcSemImpressora) return "A impressora já está instalada neste computador.";
        if (!PcNaRede) return "O computador está sem rede: resolva isso antes.";
        if (impNaCaixa || !impLigada || !impCabo.Funciona)
            return $"Procurando {ipImpressora}... nada responde. A impressora está ligada e no cabo de rede?";
        if (impIpPendente)
            return $"Nada responde em {ipImpressora}: a impressora ainda está com o IP que pegou por DHCP " +
                   $"({IpDhcpNovaImpressora}). Fixe o IP no painel dela primeiro.";
        pcSemImpressora = false;
        ok = true;
        return $"Achou a impressora em {ipImpressora}. O TI OS baixou o driver do modelo certo e ela virou a impressora padrão. " +
               "Agora mande uma página de teste pelo computador.";
    }

    // O que falta na instalação/visita (a janela de manutenção mostra)
    public System.Collections.Generic.List<string> PendenciasLocacao()
    {
        var l = new System.Collections.Generic.List<string>();
        if (instalacaoEmAndamento)
        {
            if (impNoEscritorio) l.Add("Trazer a caixa da impressora do escritório (no porta-malas) e largar aqui na sala");
            if (impNaCaixa) l.Add("Abrir a caixa e tirar a impressora");
            if (impComFitas) l.Add("Tirar as fitas e calços de transporte");
            if (tonerDaCaixa || !impCartuchoPresente) l.Add("Instalar o toner que veio na caixa (sem esquecer o lacre)");
            if (impSemPapel) l.Add("Colocar papel na gaveta");
            if (!impCabo.conectado) l.Add("Ligar o cabo de rede");
            if (!impLigada) l.Add("Ligar a impressora");
            if (impIpPendente) l.Add("Fixar o IP no painel da impressora");
            if (pcSemImpressora) l.Add("Adicionar a impressora pelo IP no computador");
            if (!impTestePeloPc) l.Add("Imprimir a página de teste pelo computador");
        }
        if (visita)
        {
            if (!contadorLido) l.Add("Ler o contador no visor");
            if (visitaTonerBaixo && impToner < TonerBaixoVisita) l.Add("Trocar o toner (está baixo)");
            if (preventivaPendente) l.Add("Troca preventiva da unidade de imagem");
        }
        return l;
    }
}
