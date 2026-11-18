using System.Collections.Generic;
using UnityEngine;

// Etapa 10: Wi-Fi corporativo. Duas coisas moram aqui:
// 1) A visita do projeto de Wi-Fi (wifiServico): medir a sala (mapa de calor), marcar o lugar do access point no
//    teto, subir na escada e fixar o suporte, passar o cabo pela canaleta até o rack, crimpar e testar, encaixar o
//    AP, ligar na porta PoE do switch, configurar a rede da empresa e a de visitantes (isolada), escolher um canal
//    livre, desligar o Wi-Fi do roteador da operadora, testar como visitante e medir de novo.
// 2) Defeitos de Wi-Fi do roteador (chamados avulsos): canal congestionado pelos vizinhos e visitantes usando a rede
//    da empresa. Os valores iniciais são "sem defeito".
public partial class EstadoEquipamentos
{
    public const float MetaDbm = -67f;              // sinal mínimo em toda a sala (voz e vídeo sem cortar)
    public const float CoberturaSurvey = 0.75f;     // parte da sala que precisa ser medida
    public const float SegundosIniciandoAp = 20f;
    public const int PortaSwitchAp = 24;            // porta PoE do switch do rack

    // ---------- Visita do projeto ----------
    public bool wifiServico;
    public int apNumero = 1;                        // AP-01, AP-02... (número no projeto)
    // Medição (site survey). Fase 0 = antes do AP; fase 1 = depois
    public bool surveyAntes, surveyDepois;
    public float piorAntes, piorDepois;             // pior sinal medido (dBm)
    public int fracasAntes, fracasDepois;           // pontos abaixo da meta
    public int surveysFeitos;
    // Access point
    public bool apMarcado;
    public Vector3 apPos;                           // no mundo, na face de baixo do forro
    public bool apSuporte, apCabo, apCrimpado, apPontaBoa, apTestado, apInstalado, apNoSwitch;
    public int apRemocoes;                          // AP tirado do lugar (furo a mais no forro)
    public float apLigaEm = -1f;                    // Time.time em que termina de iniciar
    // Configuração no painel do AP (navegador do computador do cliente)
    public bool apRedeEmpresa, apRedeVisitantes, apVisitantesIsolada, apVisitanteTestado;
    public int apCanal = 6;                         // padrão de fábrica
    public int canalVizinhos = 6;                   // onde estão as redes dos vizinhos (sorteado)
    [System.NonSerialized] public float[] mapaAntes, mapaDepois;   // dBm por célula (NaN = não medida)

    // ---------- Roteador (chamados avulsos) ----------
    public int rotCanal = 1;
    public bool rotRedeVisitantes = true, rotVisitantesIsolada = true;
    public bool rotSenhaVazada;                     // a senha da empresa está no quadro da recepção

    // ---------- Consultas ----------

    public bool ApLigado => apInstalado && apNoSwitch && apLigaEm >= 0f && Time.time >= apLigaEm;
    public bool ApIniciando => apInstalado && apNoSwitch && (apLigaEm < 0f || Time.time < apLigaEm);
    public bool ApCanalLivre => apCanal != canalVizinhos;
    public bool ApConfigurado => apRedeEmpresa && apRedeVisitantes && apVisitantesIsolada && ApCanalLivre;
    public bool RotCanalLivre => rotCanal != canalVizinhos;
    public bool WiFiNoRoteador => rotNaTomada && wifiAtivo && rotConfigOk;

    public bool WiFiConcluido =>
        wifiServico && apInstalado && ApLigado && ApConfigurado && apVisitanteTestado && !wifiAtivo &&
        surveyDepois && piorDepois >= MetaDbm && !rackPortaAberta;

    // A visita começa: AP na caixa, vizinhos num canal sorteado (quase sempre o 6, o padrão de todo mundo)
    public void PrepararWiFi(int numero)
    {
        wifiServico = true;
        apNumero = Mathf.Max(1, numero);
        surveyAntes = surveyDepois = false;
        apMarcado = apSuporte = apCabo = apCrimpado = apPontaBoa = apTestado = apInstalado = apNoSwitch = false;
        apRedeEmpresa = apRedeVisitantes = apVisitantesIsolada = apVisitanteTestado = false;
        apCanal = 6;
        canalVizinhos = Random.value < 0.7f ? 6 : Random.value < 0.5f ? 1 : 11;
        apLigaEm = -1f;
        mapaAntes = mapaDepois = null;
        rackVersao++;
    }

    public string NomeAp => $"AP-{apNumero:00}";

    // Medição terminada: guarda o pior ponto e o mapa (antes de instalar o AP é o "antes")
    public string RegistrarSurvey(float[] mapa, float pior, int fracos, int medidos)
    {
        surveysFeitos++;
        bool depois = apInstalado;
        if (depois) { surveyDepois = true; piorDepois = pior; fracasDepois = fracos; mapaDepois = mapa; }
        else { surveyAntes = true; piorAntes = pior; fracasAntes = fracos; mapaAntes = mapa; }
        string fase = depois ? "depois" : "antes";
        if (fracos == 0)
            return $"Medição ({fase}) pronta: {medidos} pontos, pior sinal {pior:0} dBm. A sala inteira está acima de {MetaDbm:0} dBm.";
        return $"Medição ({fase}) pronta: {medidos} pontos, pior sinal {pior:0} dBm. <color=#F5B83F>{fracos} ponto(s) abaixo de {MetaDbm:0} dBm</color> (vermelho e laranja no mapa).";
    }

    // O que ainda falta na visita (janela de manutenção e atalhos de teste)
    public List<string> PendenciasWiFi()
    {
        var l = new List<string>();
        if (!wifiServico) return l;
        if (!surveyAntes && !apInstalado) l.Add("medir a sala antes (celular » Wi-Fi)");
        if (!apMarcado) l.Add("marcar o lugar do AP no teto");
        if (!apSuporte) l.Add("subir na escada e fixar o suporte");
        if (!apCabo) l.Add("passar o cabo pela canaleta até o rack");
        if (!apCrimpado || !apPontaBoa || !apTestado) l.Add(!apCrimpado ? "crimpar a ponta" : !apTestado ? "testar o cabo" : "refazer a ponta");
        if (!apInstalado) l.Add("encaixar o AP no suporte");
        if (!apNoSwitch) l.Add($"ligar o cabo na porta {PortaSwitchAp} (PoE) do switch");
        if (!apRedeEmpresa) l.Add("criar a rede da empresa");
        if (!apRedeVisitantes || !apVisitantesIsolada) l.Add("rede de visitantes isolada");
        if (!ApCanalLivre) l.Add("canal livre (fora do canal dos vizinhos)");
        if (wifiAtivo) l.Add("desligar o Wi-Fi do roteador da operadora");
        if (!apVisitanteTestado) l.Add("testar como visitante (celular)");
        if (!surveyDepois || piorDepois < MetaDbm) l.Add(surveyDepois ? "o sinal ainda está fraco: mudar o AP de lugar" : "medir a sala depois");
        if (rackPortaAberta) l.Add("fechar a porta do rack");
        return l;
    }

    // ---------- Ações ----------

    public string LigarApNoSwitch()
    {
        if (!rackPortaAberta) return "Abra a porta do rack primeiro.";
        if (!apCabo) return "O cabo do AP ainda não chegou no rack.";
        apNoSwitch = true;
        if (apInstalado) apLigaEm = Time.time + SegundosIniciandoAp;
        rackVersao++;
        return $"Cabo do {NomeAp} na porta {PortaSwitchAp} do switch (porta com PoE: o próprio cabo leva a energia até o teto)." +
               (apInstalado ? " A luz do AP começou a piscar laranja: iniciando." : "");
    }

    public string EncaixarAp()
    {
        if (!apSuporte) return "Fixe o suporte no teto primeiro.";
        if (!apCrimpado || !apTestado || !apPontaBoa) return "Crimpe e teste a ponta do cabo antes de fechar o AP no suporte.";
        apInstalado = true;
        if (apNoSwitch) apLigaEm = Time.time + SegundosIniciandoAp;
        rackVersao++;
        return $"Cabo no AP, AP encaixado no suporte e girado até travar (clique)." +
               (apNoSwitch ? " A luz começou a piscar laranja: iniciando." : " Ainda sem energia: falta ligar na porta PoE do switch.");
    }

    // Tirar o AP do lugar (sinal fraco depois de instalado): o furo fica no forro e o cabo precisa ser refeito
    public string TirarAp()
    {
        apMarcado = apSuporte = apCabo = apCrimpado = apPontaBoa = apTestado = apInstalado = false;
        apLigaEm = -1f;
        surveyDepois = false;
        apRemocoes++;
        rackVersao++;
        return $"Você desceu o {NomeAp}, soltou o suporte e puxou o cabo de volta. Ficou um furo no forro (massa e tinta por sua conta).";
    }

    public string TestarComoVisitante()
    {
        if (wifiServico)
        {
            if (!ApLigado) return "O AP ainda não está no ar.";
            if (!apRedeVisitantes) return "Ainda não existe rede de visitantes no AP.";
            if (!apVisitantesIsolada)
                return "Conectado na rede \"Visitantes\"... e o celular abriu a página da impressora e viu as pastas do computador da recepção! " +
                       "<color=#F5B83F>A rede de visitantes não está isolada.</color>";
            apVisitanteTestado = true;
            return "Conectado na rede \"Visitantes\": a internet abre, mas a impressora e o computador da recepção não respondem. Isolada, como deve ser.";
        }
        if (!WiFiNoRoteador) return "Nenhuma rede Wi-Fi do cliente no ar.";
        if (!rotRedeVisitantes) return "Não existe rede de visitantes: o único Wi-Fi é o da empresa (e a senha está no quadro da recepção).";
        if (!rotVisitantesIsolada) return "Na rede de visitantes o celular enxerga a impressora e o computador da recepção. <color=#F5B83F>Não está isolada.</color>";
        return "Na rede de visitantes só a internet funciona: nada da rede da empresa aparece. Isolada.";
    }

    // Redes que o celular enxerga (analisador do app Wi-Fi): as do cliente e as dos vizinhos, com o canal
    public List<string> RedesVisiveis(float dbmAqui, string nomeCliente)
    {
        var l = new List<string>();
        string empresa = string.IsNullOrEmpty(nomeCliente) ? wifiNome : nomeCliente.Replace(" ", "_");
        if (WiFiNoRoteador) l.Add($"{wifiNome}  · canal {(wifiServico ? 6 : rotCanal)} · {dbmAqui:0} dBm (roteador da operadora)");
        if (!wifiServico && WiFiNoRoteador && rotRedeVisitantes) l.Add($"{wifiNome}_Visitantes  · canal {rotCanal}");
        if (wifiServico && ApLigado)
        {
            if (apRedeEmpresa) l.Add($"{empresa}  · canal {apCanal} · WPA2/WPA3 (access point)");
            if (apRedeVisitantes) l.Add($"{empresa}_Visitantes  · canal {apCanal}{(apVisitantesIsolada ? " · isolada" : "")}");
            if (!apRedeEmpresa && !apRedeVisitantes) l.Add($"AP_Config_{apNumero:00}  · canal {apCanal} · aberta (padrão de fábrica)");
        }
        string[] vizinhos = { "FIBRA_2G_7731", "Familia Souza", "LOJA_ROUPAS", "Celular de Ana", "CASA-8F21", "Escritorio_2andar", "REDE_CASA", "Clinica Sorriso Wifi" };
        for (int i = 0; i < vizinhos.Length; i++)
        {
            int canal = i < 6 ? canalVizinhos : (canalVizinhos == 11 ? 1 : 11);
            l.Add($"<color=#9AA6B8>{vizinhos[i]}  · canal {canal} · {-72 - (i * 7) % 17} dBm</color>");
        }
        return l;
    }
}
