using System.Collections.Generic;
using UnityEngine;

// Etapa 4: ferramentas de diagnóstico no computador do cliente. Cada medição aparece na ferramenta, na mão
// do técnico (EquipamentoPecas.Ferramentas.cs), ou na tela do monitor (pendrive), e vira uma linha do laudo.
public static partial class AcoesFisicas
{
    static string V(float v) => v.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    // Check-up: cada medição pode completar o laudo (o cliente confere na hora, como nos outros serviços)
    static void ConferirLaudo(EstadoEquipamentos e)
    {
        if (e.checkup) MM?.Verificar();
    }

    // Com o gabinete fechado: pendrive (no monitor) e multímetro na tomada (atrás)
    static void FerramentasPcFechado(EstadoEquipamentos e, ListaAcoes l)
    {
        l.peca = "traseira";
        l.Add(new AcaoFisica("Medir a tomada com o multímetro (750 V AC)", () =>
        {
            float v = 124f + Random.Range(0f, 4.5f);
            e.MostrarFerramenta(FerramentaDiag.Multimetro, "", v, "V~");
            e.Anotar(ItemLaudo.Nenhum, "Tomada", $"{v:0} V AC (rede de 127 V, normal).");
            return $"As pontas na tomada do estabilizador: o visor firma em <b>{v:0} V</b>. A energia da tomada está normal.";
        }, 1.5f, Item.Multimetro) { observacao = true, close = false });

        if (e.pcGabineteAberto) return;
        l.peca = "tela";
        if (e.pcLigado)
        {
            l.Add(new AcaoFisica("Dar boot pelo pendrive: teste de memória", () =>
            {
                if (!e.PcDaVideo) return "O computador nem chega a ler o pendrive (não dá vídeo).";
                e.clienteSalvouTrabalho = true;
                e.MostrarFerramenta(FerramentaDiag.Pendrive, "");
                MM?.ConsumirTempo(20f);
                int erros = e.ErrosMemoria;
                e.MostrarTelaPendrive("TESTE DE MEMÓRIA  ·  8 GB",
                    "Teste 3/9 (padrões móveis)  ·  passagem 1",
                    erros > 0 ? $"<color=#FF5A5A>ERROS: {erros}</color>  ·  endereços 0x1F3A2000 a 0x1F3A2FFF\nPente do slot 2 com falha"
                              : "<color=#5AFF8C>ERROS: 0</color>  ·  passagem completa sem falhas");
                e.Anotar(ItemLaudo.Memoria, "Memória", erros > 0
                    ? $"teste de memória com {erros} erros (pente do slot 2) — trocar."
                    : $"teste de memória completo, 0 erros — saúde {e.SaudeMemoriaPct}% ({EstadoEquipamentos.Situacao(e.SaudeMemoriaPct)}).");
                ConferirLaudo(e);
                return erros > 0
                    ? $"Pediu para salvar tudo, reiniciou pelo pendrive e rodou o teste por 20 minutos: <color=#F2545B>{erros} erros</color>, sempre no mesmo pente (slot 2)."
                    : "Pediu para salvar tudo, reiniciou pelo pendrive e rodou o teste por 20 minutos: nenhum erro. Voltou para o TI OS.";
            }, 3f, Item.PendriveDiagnostico) { observacao = true, close = false });

            l.Add(new AcaoFisica("Dar boot pelo pendrive: disco e sensores (S.M.A.R.T.)", () =>
            {
                if (!e.PcDaVideo) return "O computador nem chega a ler o pendrive (não dá vídeo).";
                e.clienteSalvouTrabalho = true;
                e.MostrarFerramenta(FerramentaDiag.Pendrive, "");
                MM?.ConsumirTempo(5f);
                int pct = e.SaudeDiscoPct, setores = e.SetoresRealocados, temp = e.TemperaturaCpu, rpm = e.RpmCooler;
                string cor = pct >= 70 ? "#5AFF8C" : pct >= 40 ? "#FFD54A" : "#FF5A5A";
                e.MostrarTelaPendrive("DIAGNÓSTICO DE DISCO E SENSORES",
                    "Lendo S.M.A.R.T. do disco 1 (500 GB)...",
                    $"Disco 1: saúde <color={cor}>{pct}%</color>  ·  setores realocados: {setores}\n" +
                    $"CPU: {temp} °C  ·  ventoinha do cooler: {rpm} RPM  ·  idade estimada: {e.idadePcAnos} anos");
                e.Anotar(ItemLaudo.Disco, "Disco", $"S.M.A.R.T. {pct}% ({EstadoEquipamentos.Situacao(pct)}), {setores} setores realocados.");
                int pctCooler = e.SaudeCoolerPct;
                e.Anotar(ItemLaudo.Temperatura, "Temperatura", $"CPU a {temp} °C, cooler a {rpm} RPM — ventoinha {pctCooler}% ({EstadoEquipamentos.Situacao(pctCooler)}).");
                ConferirLaudo(e);
                return (e.pcHdRuim ? $"<color=#F2545B>Disco em estado CRÍTICO:</color> saúde {pct}%, {setores} setores realocados e subindo. "
                                   : $"Disco com saúde <b>{pct}%</b> ({setores} setores realocados). ") +
                       $"Processador a {temp} °C, ventoinha a {rpm} RPM.";
            }, 2f, Item.PendriveDiagnostico) { observacao = true, close = false });
        }
    }

    // Com o gabinete aberto: placa POST, testador de fonte e multímetro nas peças
    static void FerramentasGabineteAberto(EstadoEquipamentos e, ListaAcoes l)
    {
        l.peca = "gabinete";
        l.Add(new AcaoFisica("Espetar a placa POST num slot livre e ligar (bancada)", () =>
        {
            if (e.pcCoolerFora) return "Não ligue sem o cooler: o processador queima em segundos.";
            string codigo = e.CodigoPost();
            e.MostrarFerramenta(FerramentaDiag.PlacaPOST, codigo);
            string sig = EstadoEquipamentos.SignificadoPost(codigo);
            e.Anotar(ItemLaudo.Nenhum, "Placa POST", codigo == "" ? $"visor apagado ({sig})." : $"código {codigo} ({sig}).");
            return codigo == ""
                ? $"Você espetou a placa POST e apertou o botão: o visor da placa nem acende. Tabela da placa: {sig}."
                : $"Você espetou a placa POST e ligou: os códigos correram e o visor parou em <b>{codigo}</b>. Tabela da placa: {sig}.";
        }, 2f, Item.PlacaPOST) { observacao = true, close = false });

        l.Add(new AcaoFisica("Ligar o testador de fonte no conector de 24 pinos", () =>
        {
            if (e.pcLigado) return "Desligue o computador antes de soltar o conector de 24 pinos.";
            float v12 = e.Tensao12V, v5 = e.Tensao5V, v33 = e.Tensao33V;
            if (e.pcFonteQueimada) e.ledsFonte = "------";
            else e.ledsFonte = (v12 < 11.4f ? "r" : "v") + (v5 < 4.75f ? "r" : "v") + "vvv" + (e.FonteNaNorma ? "v" : "r");
            e.MostrarFerramenta(FerramentaDiag.TestadorFonte, e.pcFonteQueimada ? "" :
                $"+12V {V(v12)}\n+5V  {V(v5)}\n3,3V {V(v33)}\nPG   {(e.FonteNaNorma ? "320ms" : "FAIL")}");
            int pct = e.SaudeFontePct;
            e.Anotar(ItemLaudo.Fonte, "Fonte", e.pcFonteQueimada ? "não liga no testador (queimada) — trocar."
                : $"12 V = {V(v12)} · 5 V = {V(v5)} · 3,3 V = {V(v33)} · Power Good {(e.FonteNaNorma ? "ok" : "falhou")} — saúde {pct}% ({EstadoEquipamentos.Situacao(pct)}).");
            ConferirLaudo(e);
            if (e.pcFonteQueimada) return "Você soltou o conector de 24 pinos e ligou no testador: nenhum LED acende, o visor fica apagado. A fonte não entrega nada.";
            return e.FonteNaNorma
                ? $"Todos os LEDs verdes: 12 V em {V(v12)}, 5 V em {V(v5)}, 3,3 V em {V(v33)} e o Power Good em 320 ms. Fonte dentro da norma."
                : $"<color=#F2545B>LED do +12 V vermelho:</color> só {V(v12)} V (o mínimo é 11,40), e o Power Good falhou. A fonte não segura a carga.";
        }, 2f, Item.TestadorFonte) { observacao = true, close = false });

        l.Add(new AcaoFisica("Medir os 12 V da fonte com o multímetro (20 V DC)", () =>
        {
            float v = e.Tensao12V;
            e.MostrarFerramenta(FerramentaDiag.Multimetro, "", v, "V");
            int pct = e.SaudeFontePct;
            if (!e.pcFonteQueimada)
                e.Anotar(ItemLaudo.Fonte, "Fonte", $"12 V medidos no multímetro: {V(v)} V — saúde {pct}% ({EstadoEquipamentos.Situacao(pct)}).");
            ConferirLaudo(e);
            return e.pcFonteQueimada ? "Ponta preta no fio preto, vermelha no amarelo, fonte ligada pelo PS_ON: o visor fica em 0,00 V."
                 : v < 11.4f ? $"Fio amarelo (12 V): o visor oscila e firma em <color=#F2545B>{V(v)} V</color>. Abaixo dos 11,40 V da norma."
                 : $"Fio amarelo (12 V): o visor firma em <b>{V(v)} V</b>. Dentro da norma (11,40 a 12,60).";
        }, 2f, Item.Multimetro) { observacao = true, close = false });

        l.Add(new AcaoFisica("Medir a bateria da placa-mãe com o multímetro", () =>
        {
            float v = e.TensaoBateria;
            e.MostrarFerramenta(FerramentaDiag.Multimetro, "", v, "V");
            int pct = e.SaudeBateriaPct;
            e.Anotar(ItemLaudo.Bateria, "Bateria CR2032", $"{V(v)} V — saúde {pct}% ({EstadoEquipamentos.Situacao(pct)}).");
            ConferirLaudo(e);
            return v < 2.5f ? $"Pontas na bateria redonda da placa: <color=#F2545B>{V(v)} V</color>. Uma CR2032 boa tem perto de 3 V: esta não segura mais a data."
                            : $"Pontas na bateria redonda da placa: <b>{V(v)} V</b> (nova tem perto de 3 V).";
        }, 1.5f, Item.Multimetro) { observacao = true, close = false });

        l.Add(new AcaoFisica("Testar a continuidade do fusível da fonte (multímetro)", () =>
        {
            if (e.pcLigado) return "Desligue o computador antes.";
            bool aberto = e.pcFonteQueimada;
            e.MostrarFerramenta(FerramentaDiag.Multimetro, aberto ? "OL" : "", aberto ? float.NaN : 0.2f, "Ω", !aberto);
            e.Anotar(ItemLaudo.Nenhum, "Fusível da fonte", aberto ? "aberto (queimado)." : "com continuidade.");
            return aberto ? "Escala de continuidade, pontas nas duas pernas do fusível: o visor mostra <color=#F2545B>OL</color> e não bipa. Fusível queimado."
                          : "Escala de continuidade: o multímetro bipa e mostra 0,2 Ω. Fusível inteiro.";
        }, 1.5f, Item.Multimetro) { observacao = true, close = false });

        if (e.pcBateriaFraca || e.SaudeBateriaPct < 40)
            l.Add(new AcaoFisica("Trocar a bateria da placa-mãe (CR2032)", () =>
            {
                if (!Mochila.Instance.Usar(Item.BateriaCMOS)) return "Você não tem bateria CR2032.";
                e.pcBateriaFraca = false;
                e.saudeBateria = 1f;
                e.pcTrocasBateria++;
                MM?.CobrarPeca(Mochila.Info(Item.BateriaCMOS).preco / Mochila.Info(Item.BateriaCMOS).quantidadePorCompra, "Bateria CR2032");
                return "Você empurrou a trava com a chave de fenda pequena, a bateria velha pulou e a nova encaixou (lado + para cima). " +
                       "Ao ligar, é só acertar a data (o TI OS pega a hora da internet)." + PecaVelhaNaMochila();
            }, 2f, Item.BateriaCMOS));
    }
}
