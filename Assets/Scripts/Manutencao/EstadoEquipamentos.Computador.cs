using System.Collections.Generic;
using UnityEngine;

// Estado do computador do cliente (campos pc*, rede, driver, memória) e o que ele mostra/faz.
public partial class EstadoEquipamentos
{
    // ---------- Computador do cliente ----------
    public bool pcLigado = true;
    // Por dentro do gabinete (Fase 4)
    public bool pcGabineteAberto;
    public float pcPoeira = 0.15f;            // 0 a 1; acima de 0,7 superaquece e desliga sozinho
    public bool pcMemoriaRuim;                // liga, dá 3 bipes e não dá vídeo
    public bool pcFonteQueimada;              // não liga: nem luz, nem ventoinha
    public int pcTrocasMemoria, pcTrocasFonte, pcLimpezas;   // o visual anima a cada troca/limpeza
    public bool pcPastaSeca;                  // pasta térmica velha e esfarelada: esquenta mesmo limpo
    public bool pcCoolerFora;                 // cooler do processador desmontado
    public bool pcPastaLimpa, pcPastaNova;    // etapas da troca da pasta
    public bool pcHdRuim;                     // setores ruins: lento, trava, faz clique
    public bool pcBackupFeito;                // arquivos do cliente copiados antes de trocar o disco
    public bool pcSemSistema;                 // disco novo, ainda sem Windows
    public int pcTrocasHd;
    public float pcSegundosQuente;            // há quanto tempo está ligado superaquecendo
    public const float SegundosAteDesligarPorCalor = 25f;
    public bool PcSuperaquecendo => pcPoeira > 0.7f || pcPastaSeca;
    public bool PcHardwareOk => !pcMemoriaRuim && !pcFonteQueimada && !PcSuperaquecendo && !pcGabineteAberto &&
                                !pcCoolerFora && !pcHdRuim && !pcSemSistema &&
                                !pcFonteFraca && !pcMemoriaIntermitente && !pcMemoriaSolta && !pcBateriaFraca;
    public CaboRede pcCabo = new CaboRede();
    public bool pcIpFixo;
    public string pcIp = "192.168.0.23";
    public bool pcIpLiberado;                 // depois de "ipconfig /release" fica sem IP até o /renew
    public bool pcIpErrado;                   // pegou IP do servidor DHCP errado (modem da operadora)
    public bool driverInstalado = true;
    public bool driverOk = true;
    public float memoria = 0.35f;
    public int diasLigado = 1;
    public bool clienteSalvouTrabalho;

    // ---------- Consultas ----------

    public bool PcRedeFisica => pcLigado && pcCabo.Funciona && driverInstalado && driverOk;
    public bool PcConflitoIp => pcIpFixo && pcIp == ipImpressora;
    public bool PcTemIp => PcRedeFisica && !pcIpLiberado && RoteadorFuncionando;
    public bool PcNaRede => PcTemIp && !PcConflitoIp && !pcIpErrado;
    public bool PcNaInternet => PcNaRede && InternetOk;

    public string PcIpMostrado =>
        !PcRedeFisica ? "(desconectado)" :
        pcIpLiberado ? "0.0.0.0" :
        pcIpErrado ? "10.0.0.37" :
        pcIp;

    public string LedRedePc => !pcLigado ? "apagado" : pcCabo.Funciona ? "verde, piscando" : "apagado";

    // ---------- Ações ----------

    public string Ping(string destino)
    {
        if (!PcRedeFisica) return "Falha geral. Nenhuma placa de rede conectada.";
        if (pcIpLiberado) return "Falha geral. O adaptador não tem endereço IP (use ipconfig /renew).";
        if (PcConflitoIp) return "Falha geral. Conflito de endereço IP detectado.";
        bool local = destino == "192.168.0.1" || destino == ipImpressora;
        if (!RoteadorFuncionando || pcIpErrado) return $"Esgotado o tempo limite do pedido. ({destino})";
        if (local)
        {
            if (destino == ipImpressora && (!ImpressoraOnline || impIpMudou || impIpPendente || impNaCaixa)) return $"Host de destino inacessível. ({destino})";
            return $"Resposta de {destino}: bytes=32 tempo=1ms TTL=64";
        }
        if (!InternetOk) return $"Esgotado o tempo limite do pedido. ({destino})";
        return $"Resposta de {destino}: bytes=32 tempo=15ms TTL=117";
    }

    public string RenovarIp()
    {
        if (!PcRedeFisica) return "Nenhuma operação pode ser executada: a mídia está desconectada.";
        if (pcIpFixo) return "Erro: o adaptador usa IP fixo (manual). Mude para Automático (DHCP) nas configurações de rede.";
        if (!RoteadorFuncionando) return "Erro: não foi possível contatar o servidor DHCP.";
        pcIpLiberado = false;
        pcIp = "192.168.0.23";
        pcIpErrado = dhcpDuplo;
        return dhcpDuplo ? "Endereço IPv4: 10.0.0.37   Gateway: 10.0.0.1  (veio de outro servidor DHCP!)"
                         : $"Endereço IPv4: {pcIp}   Gateway: 192.168.0.1";
    }

    public string LiberarIp()
    {
        if (!PcRedeFisica) return "Nenhuma operação pode ser executada: a mídia está desconectada.";
        if (pcIpFixo) return "Erro: o adaptador usa IP fixo (manual). Não há concessão DHCP para liberar.";
        pcIpLiberado = true;
        return "Endereço IP liberado. Endereço IPv4: 0.0.0.0";
    }

    public void ReiniciarPc()
    {
        memoria = 0.3f;
        diasLigado = 0;
        pcIpLiberado = false;
        if (!pcIpFixo) { pcIp = "192.168.0.23"; pcIpErrado = dhcpDuplo; }
        // O Windows reinstala sozinho o driver que foi desinstalado
        if (!driverInstalado) { driverInstalado = true; driverOk = true; }
    }

    public string ProcurarHardware()
    {
        if (driverInstalado) return "Nenhuma alteração de hardware encontrada.";
        driverInstalado = true;
        driverOk = true;
        return "Novo hardware encontrado: Placa de rede Ethernet. Driver instalado com sucesso.";
    }

    // Mudar o adaptador do PC entre IP fixo e automático
    public string DefinirIpAutomatico(bool automatico)
    {
        if (automatico == !pcIpFixo) return automatico ? "O IP já está em Automático (DHCP)." : "O IP já está manual.";
        pcIpFixo = !automatico;
        if (automatico)
        {
            pcIpLiberado = true;   // fica sem IP até renovar (ipconfig /renew ou reiniciar)
            return "Atribuição de IP: Automático (DHCP). Renove o endereço para pegar um IP novo.";
        }
        pcIp = "192.168.0.50";
        return "Atribuição de IP: Manual (192.168.0.50).";
    }
}
