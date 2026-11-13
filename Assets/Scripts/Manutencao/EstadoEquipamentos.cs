using System.Collections.Generic;
using UnityEngine;

// Um documento na fila de impressão
[System.Serializable]
public class TrabalhoImpressao
{
    public string nome;
    public int paginas;
    public bool travado;
    public TrabalhoImpressao(string nome, int paginas, bool travado = false) { this.nome = nome; this.paginas = paginas; this.travado = travado; }
}

public enum DefeitoCabo { Nenhum, Conector, Rompido }

// Um cabo de rede ligando um equipamento à tomada de rede da parede
[System.Serializable]
public class CaboRede
{
    public bool conectado = true;
    public DefeitoCabo defeito;
    public float metrosNecessarios = 2f;   // distância do equipamento até a tomada de rede
    public bool Bom => defeito == DefeitoCabo.Nenhum;
    public bool Funciona => conectado && Bom;

    // O que o testador de cabo mostra (luzes 1 a 8)
    public string Testar()
    {
        switch (defeito)
        {
            case DefeitoCabo.Conector: return "Luzes: 1 2 · 4 5 · 7 8  → os fios 3 e 6 não passam sinal: mau contato no conector.";
            case DefeitoCabo.Rompido:  return "Luzes: nenhuma acende → o cabo está rompido no meio.";
            default:                   return "Luzes: 1 2 3 4 5 6 7 8 em sequência → cabo perfeito.";
        }
    }
}

// "Simulação" dos equipamentos do escritório do cliente durante um chamado:
// cada problema começa quebrando alguma coisa aqui, e o jogador conserta mexendo nas peças e no PC.
// Tudo é calculado a partir desse estado (LEDs, mensagens, ping, impressão), como num equipamento de verdade.
[System.Serializable]
public class EstadoEquipamentos
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
    public float impToner = 0.8f;             // 0 a 1
    public bool spoolerRodando = true;
    public List<TrabalhoImpressao> fila = new List<TrabalhoImpressao>();
    public string ipImpressora = "192.168.0.50";

    // ---------- Computador do cliente ----------
    public bool pcLigado = true;
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

    // ---------- Roteador ----------
    public bool rotNaTomada = true;
    public float rotDesligadoDesde = -1f;      // Time.time em que foi tirado da tomada
    public float rotLigaEm = -1f;              // Time.time em que termina de iniciar (luzes piscando antes)
    public bool rotTravado;                    // memória cheia: a internet cai sozinha
    public bool rotConfigOk = true;            // configuração da internet/Wi-Fi certa
    public bool rotSenhaDesconhecida;          // alguém trocou a senha do painel
    public bool wifiAtivo = true;
    public string wifiNome = "Escritorio";
    public bool dhcpDuplo;                     // o modem da operadora também está entregando IP
    public bool modemEmBridge = true;

    public const string SenhaPadrao = "admin";
    public const float SegundosParaReiniciarRoteador = 10f;   // tempo mínimo fora da tomada
    public const float SegundosIniciandoRoteador = 15f;

    // ---------- Consultas (o que o jogador vê) ----------

    public bool RoteadorIniciando => rotNaTomada && Time.time < rotLigaEm;
    public bool RoteadorFuncionando => rotNaTomada && !RoteadorIniciando;
    public bool InternetOk => RoteadorFuncionando && rotConfigOk && !rotTravado && !dhcpDuplo;

    public bool ImpressoraOnline => impLigada && impCabo.Funciona && RoteadorFuncionando;
    public bool FilaTravada => fila.Exists(t => t.travado);

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

    // Mensagem no visor da impressora
    public string VisorImpressora()
    {
        if (!impLigada) return "(desligada)";
        if (impTampaAberta) return "Tampa aberta";
        if (!impCartuchoPresente) return "Instale o cartucho de toner";
        if (!impCabo.Funciona) return "Rede desconectada";
        if (impToner < 0.1f) return $"Toner baixo - {Mathf.RoundToInt(impToner * 100)}%";
        if (FilaTravada) return "Erro - imprimindo";
        return "Pronta";
    }

    public string LedRedeImpressora => !impLigada ? "apagado" : impCabo.Funciona ? "verde, piscando" : "apagado";
    public string LedRedePc => !pcLigado ? "apagado" : pcCabo.Funciona ? "verde, piscando" : "apagado";

    // As 4 luzes do roteador: Power, Internet, Wi-Fi, LAN
    public string LuzesRoteador()
    {
        if (!rotNaTomada) return "Todas apagadas (fora da tomada)";
        if (RoteadorIniciando) return "Power: piscando · Internet: apagada · Wi-Fi: apagada · LAN: piscando (iniciando...)";
        string internet = !rotConfigOk || dhcpDuplo ? "laranja" : rotTravado ? "piscando laranja de vez em quando" : "verde";
        string wifi = wifiAtivo && rotConfigOk ? "verde" : "apagada";
        return $"Power: verde · Internet: {internet} · Wi-Fi: {wifi} · LAN: verde";
    }

    // ---------- Ações (cada uma devolve o que aconteceu) ----------

    public string Imprimir(bool pelaRede, out bool saiuBoa)
    {
        saiuBoa = false;
        if (!impLigada) return "A impressora está desligada.";
        if (impTampaAberta) return "Feche a tampa da impressora primeiro.";
        if (!impCartuchoPresente) return "Sem cartucho de toner.";
        if (pelaRede)
        {
            if (!spoolerRodando) return "Erro: o serviço Spooler de Impressão não está em execução.";
            if (!PcNaRede) return "Erro: este computador está sem rede.";
            if (!ImpressoraOnline) return "Impressora offline. O documento ficou na fila.";
            if (FilaTravada) return "O documento entrou na fila atrás de um trabalho travado e não sai.";
        }
        if (impCartuchoComLacre) return "A folha saiu em branco! (o lacre do toner ainda está no cartucho)";
        if (impToner < 0.1f) return "A folha saiu clarinha, cheia de faixas brancas.";
        impToner = Mathf.Max(0f, impToner - 0.002f);
        saiuBoa = true;
        return "A página saiu perfeita!";
    }

    public string Ping(string destino)
    {
        if (!PcRedeFisica) return "Falha geral. Nenhuma placa de rede conectada.";
        if (pcIpLiberado) return "Falha geral. O adaptador não tem endereço IP (use ipconfig /renew).";
        if (PcConflitoIp) return "Falha geral. Conflito de endereço IP detectado.";
        bool local = destino == "192.168.0.1" || destino == ipImpressora;
        if (!RoteadorFuncionando || pcIpErrado) return $"Esgotado o tempo limite do pedido. ({destino})";
        if (local)
        {
            if (destino == ipImpressora && !ImpressoraOnline) return $"Host de destino inacessível. ({destino})";
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

    // Tirar/colocar o roteador na tomada. Só "destrava" se ficou fora o tempo suficiente.
    public string TomadaRoteador()
    {
        if (rotNaTomada)
        {
            rotNaTomada = false;
            rotDesligadoDesde = Time.time;
            return "Você tirou o roteador da tomada. Todas as luzes apagaram.";
        }
        float fora = Time.time - rotDesligadoDesde;
        rotNaTomada = true;
        rotLigaEm = Time.time + SegundosIniciandoRoteador;
        if (fora >= SegundosParaReiniciarRoteador)
        {
            rotTravado = false;
            return $"Você ligou o roteador de volta depois de {fora:F0}s. Ele está iniciando...";
        }
        return $"Você religou rápido demais ({fora:F0}s). O ideal é esperar uns {SegundosParaReiniciarRoteador:F0}s.";
    }

    // Reset de fábrica (botão com clipe): apaga tudo
    public void ResetFabrica()
    {
        rotConfigOk = false;            // precisa configurar a internet/Wi-Fi de novo
        rotSenhaDesconhecida = false;   // volta para a senha da etiqueta
        rotTravado = false;
        wifiAtivo = true;
        wifiNome = "ROTEADOR_5G_A1B2";
        rotNaTomada = true;
        rotLigaEm = Time.time + SegundosIniciandoRoteador;
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

    public void ConfigurarRoteador(string nomeWifi)
    {
        rotConfigOk = true;
        wifiNome = string.IsNullOrEmpty(nomeWifi) ? "Escritorio" : nomeWifi;
    }
}
