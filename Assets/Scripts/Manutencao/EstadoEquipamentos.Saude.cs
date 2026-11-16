using System.Collections.Generic;
using UnityEngine;

// Ferramenta de diagnóstico que aparece na mão do técnico (o visual em EquipamentoPecas.Ferramentas.cs)
public enum FerramentaDiag { Nenhuma, PlacaPOST, TestadorFonte, Multimetro, Pendrive }

// Partes do laudo do check-up: cada medição feita marca a sua
[System.Flags]
public enum ItemLaudo { Nenhum = 0, Fonte = 1, Memoria = 2, Disco = 4, Bateria = 8, Temperatura = 16, Interno = 32, Todos = 63 }

// Etapa 4: saúde das peças em % e as leituras das ferramentas de diagnóstico.
// Cada peça tem um desgaste (1 = nova, 0 = morta) sorteado pela idade do computador; os defeitos do catálogo
// derrubam a peça culpada. As ferramentas só mostram números: quem interpreta é o técnico.
public partial class EstadoEquipamentos
{
    // ---------- Saúde (0 a 1) ----------
    public float saudeMemoria = 0.97f, saudeFonte = 0.92f, saudeDisco = 0.93f, saudeCooler = 0.9f, saudeBateria = 0.9f;
    public float saudeCilindro = 0.9f;
    public int idadePcAnos = 2;

    // ---------- Defeitos que só a ferramenta certa mostra bem ----------
    public bool pcFonteFraca;                 // 12 V caindo para ~11,2 V: reinicia sozinho sob carga
    public bool pcMemoriaIntermitente;        // tela azul de vez em quando; o teste de memória acha erros
    public bool pcMemoriaSolta;               // pente meio fora do slot: liga sem vídeo e sem apito (gabinete sem alto-falante)
    public bool pcBateriaFraca;               // bateria da placa-mãe a 2,1 V: perde a data e a configuração
    public int pcTrocasBateria;
    public float pcSegundosFonteFraca, pcSegundosTelaAzul;

    // ---------- Check-up com laudo ----------
    public bool checkup;                      // o serviço é o check-up: só termina com o laudo completo
    public ItemLaudo laudoFeito;
    public List<string> medicoes = new List<string>();   // leituras anotadas (vão para a OS como laudo)

    // ---------- Ferramenta na mão (o visual lê daqui) ----------
    public int usosFerramenta;                // sobe a cada uso: o visual mostra a ferramenta de novo
    public FerramentaDiag ultimaFerramenta;
    public string leituraTexto = "";          // texto final do visor (código POST, "OL"...)
    public float leituraValor = float.NaN;    // multímetro: o número que o visor procura até firmar
    public string leituraUnidade = "";
    public bool leituraBipe;                  // multímetro em continuidade: bipa
    public string ledsFonte = "";             // testador de fonte: 'v' verde, 'r' vermelho, '-' apagado (12V 5V 3,3V -12V 5VSB PG)
    // Tela do pendrive de diagnóstico (no monitor do cliente)
    public int telasPendrive;
    public string telaPendriveTitulo = "", telaPendriveProgresso = "", telaPendriveResultado = "";

    public bool LaudoCompleto => (laudoFeito & ItemLaudo.Todos) == ItemLaudo.Todos;
    // Chega a dar vídeo (e a ler um pendrive)?
    public bool PcDaVideo => !pcFonteQueimada && !pcMemoriaRuim && !pcMemoriaSolta && !pcCoolerFora;

    // ---------- Desgaste ----------

    // Sorteia o desgaste de cada peça pela idade do computador. Nunca vira defeito sozinho (mínimo 30%).
    public void SortearDesgaste(int anos)
    {
        idadePcAnos = Mathf.Max(1, anos);
        float D(float porAno) => Mathf.Clamp(1f - idadePcAnos * porAno * Random.Range(0.6f, 1.4f), 0.3f, 0.99f);
        saudeDisco = D(0.07f);
        saudeFonte = D(0.05f);
        saudeCooler = D(0.06f);
        saudeBateria = D(0.08f);
        saudeMemoria = D(0.015f);
        saudeCilindro = Random.Range(0.3f, 0.98f);
    }

    // ---------- Leituras (o que cada ferramenta mostra) ----------

    public float Tensao12V => pcFonteQueimada ? 0f : pcFonteFraca ? 11.18f : 11.86f + saudeFonte * 0.32f;
    public float Tensao5V => pcFonteQueimada ? 0f : pcFonteFraca ? 4.91f : 4.94f + saudeFonte * 0.09f;
    public float Tensao33V => pcFonteQueimada ? 0f : 3.27f + saudeFonte * 0.05f;
    public float TensaoBateria => pcBateriaFraca ? 2.08f : 2.58f + saudeBateria * 0.47f;
    public int ErrosMemoria => pcMemoriaIntermitente ? 37 : 0;
    public int SaudeDiscoPct => pcHdRuim ? 7 : Mathf.RoundToInt(saudeDisco * 100f);
    public int SetoresRealocados => pcHdRuim ? 1243 : Mathf.RoundToInt((1f - saudeDisco) * (1f - saudeDisco) * 300f);
    public int TemperaturaCpu => PcSuperaquecendo ? 94 : Mathf.RoundToInt(35f + pcPoeira * 24f + (1f - saudeCooler) * 16f);
    public int RpmCooler => pcPoeira > 0.7f || pcPastaSeca ? 3150 : Mathf.RoundToInt(850f + saudeCooler * 750f);

    // Fonte dentro da norma ATX (12 V ± 5%)?
    public bool FonteNaNorma => Tensao12V >= 11.4f && Tensao5V >= 4.75f;
    // Saúde da fonte em % pelo que dá para medir (fraca = perto do fim)
    public int SaudeFontePct => pcFonteQueimada ? 0 : pcFonteFraca ? 12 : Mathf.RoundToInt(saudeFonte * 100f);
    public int SaudeBateriaPct => pcBateriaFraca ? 5 : Mathf.RoundToInt(saudeBateria * 100f);
    public int SaudeCoolerPct => Mathf.RoundToInt(saudeCooler * 100f);
    public int SaudeMemoriaPct => pcMemoriaRuim ? 0 : pcMemoriaIntermitente ? 20 : Mathf.RoundToInt(saudeMemoria * 100f);

    // Código de duas letras que a placa POST mostra quando o computador para (ou "A0": entregou para o sistema)
    public string CodigoPost()
    {
        if (pcFonteQueimada) return "";           // sem energia: visor apagado
        if (pcMemoriaRuim || pcMemoriaSolta) return "55";
        if (pcSemSistema) return "A2";
        return "A0";
    }

    public static string SignificadoPost(string codigo)
    {
        switch (codigo)
        {
            case "": return "visor apagado: a placa não recebe energia (fonte)";
            case "55": return "parou no teste de memória: pente com defeito ou mal encaixado";
            case "A2": return "procurando o disco de inicialização: disco vazio ou sem sistema";
            default: return "passou por todos os testes e entregou para o sistema";
        }
    }

    public static string Situacao(int pct) => pct >= 70 ? "boa" : pct >= 40 ? "atenção" : "trocar";
    public static string CorSituacao(int pct) => pct >= 70 ? "#1E7A4C" : pct >= 40 ? "#B7791F" : "#B3262E";

    // ---------- Anotações (laudo) ----------

    // Anota uma medição; a mesma parte medida de novo substitui a linha anterior
    public void Anotar(ItemLaudo item, string chave, string linha)
    {
        laudoFeito |= item;
        string prefixo = chave + ":";
        medicoes.RemoveAll(m => m.StartsWith(prefixo));
        medicoes.Add($"{prefixo} {linha}");
    }

    public void MostrarFerramenta(FerramentaDiag f, string texto, float valor = float.NaN, string unidade = "", bool bipe = false)
    {
        ultimaFerramenta = f;
        leituraTexto = texto ?? "";
        leituraValor = valor;
        leituraUnidade = unidade ?? "";
        leituraBipe = bipe;
        usosFerramenta++;
    }

    public void MostrarTelaPendrive(string titulo, string progresso, string resultado)
    {
        telaPendriveTitulo = titulo;
        telaPendriveProgresso = progresso;
        telaPendriveResultado = resultado;
        telasPendrive++;
    }

    // O que o check-up ainda não mediu (para o card e o log)
    public string FaltaNoLaudo()
    {
        var falta = new List<string>();
        if ((laudoFeito & ItemLaudo.Fonte) == 0) falta.Add("fonte");
        if ((laudoFeito & ItemLaudo.Memoria) == 0) falta.Add("memória");
        if ((laudoFeito & ItemLaudo.Disco) == 0) falta.Add("disco");
        if ((laudoFeito & ItemLaudo.Bateria) == 0) falta.Add("bateria da placa-mãe");
        if ((laudoFeito & ItemLaudo.Temperatura) == 0) falta.Add("temperatura/cooler");
        if ((laudoFeito & ItemLaudo.Interno) == 0) falta.Add("inspeção interna");
        return falta.Count == 0 ? "" : string.Join(", ", falta);
    }

    // Recomendações do laudo: peças medidas que estão no fim
    public List<string> Recomendacoes()
    {
        var r = new List<string>();
        if ((laudoFeito & ItemLaudo.Disco) != 0 && SaudeDiscoPct < 40) r.Add($"Trocar o disco por um SSD em breve (saúde {SaudeDiscoPct}%), com backup antes.");
        else if ((laudoFeito & ItemLaudo.Disco) != 0 && SaudeDiscoPct < 70) r.Add($"Acompanhar o disco (saúde {SaudeDiscoPct}%) e manter o backup em dia.");
        if ((laudoFeito & ItemLaudo.Fonte) != 0 && SaudeFontePct < 40) r.Add($"Trocar a fonte (tensões no limite, saúde {SaudeFontePct}%).");
        if ((laudoFeito & ItemLaudo.Bateria) != 0 && SaudeBateriaPct < 40) r.Add($"Trocar a bateria CR2032 da placa-mãe ({TensaoBateria:0.00} V).");
        if ((laudoFeito & ItemLaudo.Temperatura) != 0 && SaudeCoolerPct < 40) r.Add($"Trocar a ventoinha do cooler (gira a {RpmCooler} RPM, rolamento gasto).");
        if ((laudoFeito & ItemLaudo.Interno) != 0 && pcPoeira > 0.3f) r.Add("Limpeza interna (poeira acumulada).");
        if ((laudoFeito & ItemLaudo.Memoria) != 0 && ErrosMemoria > 0) r.Add("Trocar o pente de memória com erros.");
        return r;
    }
}
