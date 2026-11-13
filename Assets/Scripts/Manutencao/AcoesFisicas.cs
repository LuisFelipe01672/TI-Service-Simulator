using System;
using System.Collections.Generic;

// Uma ação que o técnico faz com as mãos num equipamento
public class AcaoFisica
{
    public string rotulo;
    public float segurar;              // segundos segurando (0 = instantâneo)
    public Item? requer;               // ferramenta/peça necessária
    public Func<string> executar;      // devolve o que aconteceu
    public bool abreCrimpagem;         // abre o minigame de crimpar
    public bool abreComputador;        // abre o "Windows" do cliente
    public bool observacao;            // só olhar (não conta como mexer)

    public AcaoFisica(string rotulo, Func<string> executar, float segurar = 0f, Item? requer = null)
    {
        this.rotulo = rotulo; this.executar = executar; this.segurar = segurar; this.requer = requer;
    }
}

// Lista das ações possíveis em cada equipamento, de acordo com o estado atual dele.
// A janela de manutenção (e, depois, as peças 3D) usa isso.
public static class AcoesFisicas
{
    public static List<AcaoFisica> Para(TipoEquipamento tipo, EstadoEquipamentos e)
    {
        var lista = new List<AcaoFisica>();
        switch (tipo)
        {
            case TipoEquipamento.Impressora: Impressora(e, lista); break;
            case TipoEquipamento.Computador: Computador(e, lista); break;
            default: Roteador(e, lista); break;
        }
        return lista;
    }

    static MissionManager MM => MissionManager.Instance;

    // ---------- Impressora ----------

    static void Impressora(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        l.Add(new AcaoFisica("Olhar o visor", () => $"Visor: \"{e.VisorImpressora()}\"") { observacao = true });
        l.Add(new AcaoFisica("Olhar atrás da impressora", () => DescreverCabo(e.impCabo, e.impLigada, "impressora")) { observacao = true });
        AcoesCabo(e.impCabo, "da impressora", l);

        if (!e.impTampaAberta)
            l.Add(new AcaoFisica("Abrir a tampa frontal", () => { e.impTampaAberta = true; return "Tampa aberta. Dá para ver o cartucho de toner."; }));
        else
        {
            if (e.impCartuchoPresente)
                l.Add(new AcaoFisica("Retirar o cartucho", () =>
                {
                    e.impCartuchoPresente = false;
                    return e.impToner < 0.1f ? "Você tirou o cartucho. Está levinho: o toner acabou." : "Você tirou o cartucho. Ainda tem toner.";
                }, 1f));
            else if (!e.cartuchoNaMao)
                l.Add(new AcaoFisica("Pegar um toner novo da mochila", () =>
                {
                    if (!Mochila.Instance.Usar(Item.Toner)) return "Você não tem toner.";
                    e.cartuchoNaMao = true;
                    e.cartuchoNaMaoComLacre = true;
                    e.cartuchoNaMaoChacoalhado = false;
                    MM?.CobrarPeca(Mochila.Info(Item.Toner).preco);
                    return "Você tirou o cartucho novo da embalagem.";
                }, 0f, Item.Toner));

            if (e.cartuchoNaMao)
            {
                if (!e.cartuchoNaMaoChacoalhado)
                    l.Add(new AcaoFisica("Chacoalhar o cartucho", () => { e.cartuchoNaMaoChacoalhado = true; return "Você chacoalhou para espalhar o toner por igual."; }, 1.5f));
                if (e.cartuchoNaMaoComLacre)
                    l.Add(new AcaoFisica("Puxar o lacre (fita de ~70 cm)", () => { e.cartuchoNaMaoComLacre = false; return "Você puxou o lacre inteiro, em linha reta."; }, 2f));
                l.Add(new AcaoFisica("Encaixar o cartucho", () =>
                {
                    e.cartuchoNaMao = false;
                    e.impCartuchoPresente = true;
                    e.impCartuchoComLacre = e.cartuchoNaMaoComLacre;
                    e.impToner = e.cartuchoNaMaoChacoalhado ? 1f : 0.85f;
                    return "Cartucho encaixado até o fim.";
                }, 1f));
            }
            l.Add(new AcaoFisica("Fechar a tampa", () => { e.impTampaAberta = false; return "Tampa fechada."; }));
        }

        l.Add(new AcaoFisica("Imprimir página de teste (botão da impressora)", () =>
        {
            string r = e.Imprimir(false, out bool boa);
            if (boa) MM?.Verificar();
            return r;
        }, 2f));
        l.Add(new AcaoFisica(e.impLigada ? "Desligar a impressora" : "Ligar a impressora", () =>
        {
            e.impLigada = !e.impLigada;
            return e.impLigada ? "A impressora ligou e fez o barulhinho de aquecimento." : "Impressora desligada.";
        }));
    }

    // ---------- Computador ----------

    static void Computador(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (e.pcLigado) l.Add(new AcaoFisica("Sentar e usar o computador", () => "") { abreComputador = true, observacao = true });
        l.Add(new AcaoFisica("Olhar atrás do gabinete", () => DescreverCabo(e.pcCabo, e.pcLigado, "computador")) { observacao = true });
        AcoesCabo(e.pcCabo, "do computador", l);
        l.Add(new AcaoFisica(e.pcLigado ? "Desligar no botão (forçado)" : "Ligar o computador", () =>
        {
            if (e.pcLigado)
            {
                e.pcLigado = false;
                if (!e.clienteSalvouTrabalho) MM?.RegistrarErro("Desligou o PC no botão sem o cliente salvar: ele perdeu o trabalho!", 3f);
                return "O computador apagou na hora.";
            }
            e.pcLigado = true;
            e.ReiniciarPc();
            return "O computador ligou e o Windows iniciou.";
        }, e.pcLigado ? 3f : 0f));
    }

    // ---------- Roteador ----------

    static void Roteador(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        l.Add(new AcaoFisica("Olhar as luzes", () => e.LuzesRoteador()) { observacao = true });
        l.Add(new AcaoFisica("Ler a etiqueta embaixo do roteador", () =>
            $"Painel: http://192.168.0.1 · Usuário: admin · Senha: {EstadoEquipamentos.SenhaPadrao} · Wi-Fi de fábrica: ROTEADOR_5G_A1B2") { observacao = true });
        l.Add(new AcaoFisica(e.rotNaTomada ? "Tirar da tomada" : "Colocar de volta na tomada", () => e.TomadaRoteador()));
        if (e.rotNaTomada)
            l.Add(new AcaoFisica("Segurar o botão de reset com o clipe (10 s)", () =>
            {
                bool precisava = MM != null && MM.chamadoAtivo != null && MM.chamadoAtivo.problema != null &&
                                 MM.chamadoAtivo.problema.solucao == "Resetar para padrão";
                e.ResetFabrica();
                if (!precisava) MM?.RegistrarErro("Você resetou o roteador sem precisar: a internet e o Wi-Fi de todo mundo caíram!", 3f);
                return "As luzes piscaram todas juntas: o roteador voltou ao padrão de fábrica. Configure de novo pelo painel (192.168.0.1).";
            }, 10f, Item.Clipe));
    }

    // ---------- Cabo de rede (impressora e computador) ----------

    static string DescreverCabo(CaboRede c, bool ligado, string onde)
    {
        string led = !ligado ? "apagado (equipamento desligado)" : c.Funciona ? "verde, piscando" : "apagado";
        string cabo = !c.conectado ? "O cabo de rede está FORA da porta!" :
                      c.defeito == DefeitoCabo.Conector ? "O cabo está na porta, mas o conector parece meio torto e a trava está quebrada." :
                      "O cabo está encaixado na porta.";
        return $"{cabo} LED da porta de rede do(a) {onde}: {led}.";
    }

    static void AcoesCabo(CaboRede c, string de, List<AcaoFisica> l)
    {
        l.Add(new AcaoFisica(c.conectado ? $"Desconectar o cabo {de}" : $"Conectar o cabo {de} (até dar o clique)", () =>
        {
            c.conectado = !c.conectado;
            return c.conectado ? (c.defeito == DefeitoCabo.Conector ? "Encaixou, mas sem o clique: o conector está ruim." : "Clique! Cabo encaixado.")
                               : "Cabo desconectado.";
        }));
        l.Add(new AcaoFisica($"Testar o cabo {de} com o testador", () =>
        {
            if (c.conectado) return "Desconecte o cabo primeiro para ligar no testador.";
            return c.Testar();
        }, 2f, Item.TestadorCabo));
        if (!c.conectado)
        {
            l.Add(new AcaoFisica($"Refazer a ponta do cabo {de} (crimpar)", () => "") { abreCrimpagem = true, requer = Item.AlicateCrimpagem });
            l.Add(new AcaoFisica($"Medir a distância até a tomada de rede", () =>
                $"Da porta {de} até a tomada da parede dá uns {c.metrosNecessarios:0.#} m.") { observacao = true });
            l.Add(new AcaoFisica($"Passar um cabo novo da mochila", () =>
            {
                var m = Mochila.Instance;
                if (m == null || m.cabos.Count == 0) return "Você não tem nenhum cabo pronto na mochila.";
                var novo = m.PegarCabo(c.metrosNecessarios);
                if (novo == null)
                    return $"<color=#F2545B>Nenhum cabo seu alcança!</color> Precisa de {c.metrosNecessarios:0.#} m e você tem: {m.ListaCabos()}.";
                MM?.CobrarPeca(novo.metros * 3f + 4f);   // cabo + 2 conectores
                Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 1, "cabo velho");
                c.defeito = novo.Bom ? DefeitoCabo.Nenhum : DefeitoCabo.Conector;
                c.conectado = true;
                return $"Você passou um cabo de {novo.metros:0.#} m e conectou. Clique!" +
                       (novo.metros > c.metrosNecessarios * 2.5f ? " (sobrou bastante cabo enrolado)" : "");
            }, 4f));
        }
    }
}
