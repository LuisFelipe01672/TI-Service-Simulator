using System.Collections.Generic;

// Etapa 5 — Locação física: ações da instalação da impressora alugada (caixa, fitas, IP fixo no painel,
// impressora adicionada no computador) e da visita mensal (contador, toner reserva, troca preventiva).
public static partial class AcoesFisicas
{
    static ContratoLocacao ContratoDoServico() =>
        ContratosManager.Instance != null ? ContratosManager.Instance.DoServico(MM != null ? MM.chamadoAtivo : null) : null;

    // Serviço da locação: o toner e o cilindro são da empresa (incluso no contrato), não entram na conta do cliente
    static bool ServicoLocacaoAtivo => MM != null && MM.chamadoAtivo != null && MM.chamadoAtivo.servicoLocacao != ServicoLocacao.Nenhum;

    static void Espalhar(int qtd, string tipo)
    {
        if (!UnityEngine.Application.isPlaying) return;
        var pos = MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero;
        // a embalagem vai para o chão da sala, na frente do móvel (não em cima dele)
        if (SalaCliente.Atual != null)
        {
            var paraSala = SalaCliente.Atual.Centro - pos;
            paraSala.y = 0f;
            pos += paraSala.normalized * UnityEngine.Mathf.Min(0.9f, paraSala.magnitude);
        }
        Sujeira.Espalhar(pos, qtd, tipo);
    }

    // Impressora ainda na caixa: só dá para mexer na caixa. Devolve true se a lista termina aqui.
    static bool ImpressoraNaCaixa(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (!e.impNaCaixa) return false;
        PecaAtual = "caixa";
        l.Add(new AcaoFisica("Olhar a caixa", () =>
            "Caixa de papelão lacrada com fita, setas \"ESTE LADO PARA CIMA\" e a etiqueta de patrimônio da sua empresa. " +
            "Dentro: a impressora presa com calços de isopor, o toner inicial, o cabo de força e o manual.") { observacao = true });
        l.Add(new AcaoFisica("Abrir a caixa com o estilete e tirar a impressora (com cuidado, pelas alças)", () =>
        {
            e.impNaCaixa = false;
            e.caixasAbertas++;
            Espalhar(1, "caixa");
            return "Você cortou a fita do lacre, abriu as abas, tirou os calços de isopor de cima e levantou a impressora " +
                   "pelas alças até o móvel. A caixa vazia e o isopor ficaram no chão (recolha antes de ir embora).";
        }, 3f));
        return true;
    }

    // Fitas e calços de transporte presos na tampa e na gaveta
    static void FitasDeTransporte(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (!e.impComFitas) return;
        PecaAtual = "tampa";
        l.Add(new AcaoFisica("Tirar as fitas laranja e os calços de transporte", () =>
        {
            e.impComFitas = false;
            Espalhar(1, "fitas");
            return "Você puxou as fitas laranja da tampa, das laterais e da gaveta, e tirou o calço de papelão de dentro da saída de papel.";
        }, 2f));
    }

    // Painel da impressora: IP fixo na instalação
    static void IpFixoNaInstalacao(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (!e.impLigada || !e.impIpPendente) return;
        PecaAtual = "visor";
        l.Add(new AcaoFisica($"Configurar IP fixo {e.ipImpressora} no painel (Rede > TCP/IP > Manual)", () =>
        {
            if (!e.impCabo.conectado) return "Ligue o cabo de rede antes: sem rede o painel não deixa salvar o endereço.";
            e.impIpPendente = false;
            e.visorTemporario = $"IP {e.ipImpressora}";
            e.visorTemporarioAte = UnityEngine.Time.time + 6f;
            return $"No painel: Rede > TCP/IP > Manual. IP {e.ipImpressora}, máscara 255.255.255.0, gateway 192.168.0.1. " +
                   "Salvo: a impressora reiniciou a rede e o visor mostra o endereço novo. Assim ela não muda de IP quando faltar luz.";
        }, 3f));
    }

    // Visita: leitura do contador vale mesmo sem o contrato carregado (testes)
    static void ContadorNaVisita(EstadoEquipamentos e, List<AcaoFisica> l, ContratoLocacao locacao)
    {
        if (!e.visita || !e.impLigada || locacao != null) return;
        PecaAtual = "visor";
        l.Add(new AcaoFisica("Ler o contador de páginas (menu Informações)", () =>
        {
            e.contadorLido = true;
            MM?.Verificar();
            return "Contador anotado na OS.";
        }, 1.5f) { observacao = true });
    }

    // Visita: troca preventiva da unidade de imagem (com o cartucho fora) e toner reserva ao lado da impressora
    static void ServicosDaVisita(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (!e.visita) return;
        if (e.impTampaAberta && e.preventivaPendente && !e.impCartuchoPresente)
        {
            PecaAtual = "cartucho";
            l.Add(new AcaoFisica("Troca preventiva da unidade de imagem (cilindro da mochila)", () =>
            {
                var m = Mochila.Instance;
                if (m != null && !m.Usar(Item.Cilindro)) return "Você não tem cilindro (unidade de imagem) com você.";
                e.preventivaPendente = false;
                e.impCilindroRiscado = false;
                e.trocasCilindro++;
                ContratosManager.Instance?.PecaTrocada(ContratoDoServico());
                MM?.chamadoAtivo?.os?.RegistrarPeca("Unidade de imagem (incluso no contrato)", 0f);
                return "Você soltou as duas travas verdes, puxou a unidade de imagem velha pela alça e encaixou a nova, " +
                       "sem tocar no cilindro. A velha vai na caixa da nova, para a reciclagem.";
            }, 3f, Item.Cilindro));
        }
        var c = ContratoDoServico();
        if (c == null || c.reserva < ContratosManager.MaxReserva)
        {
            PecaAtual = "gaveta";
            l.Add(new AcaoFisica("Deixar um toner reserva ao lado da impressora (e mostrar como trocar)", () =>
            {
                var m = Mochila.Instance;
                if (m != null && !m.Usar(Item.Toner)) return "Você não tem toner com você para deixar de reserva.";
                e.reservasDeixadas++;
                if (c != null) c.reserva++;
                MM?.chamadoAtivo?.os?.RegistrarPeca("Toner reserva deixado no cliente (incluso no contrato)", 0f);
                MM?.Verificar();
                return "Você deixou a caixa do toner ao lado da impressora e mostrou ao cliente como trocar: abrir a tampa, " +
                       "puxar o velho, chacoalhar o novo, tirar o lacre e encaixar. Quando acabar, ele mesmo troca." +
                       (c != null ? $" Reserva lá: {c.reserva}." : "");
            }, 2f, Item.Toner));
        }
    }

    // Computador do cliente: adicionar a impressora pelo IP
    static void AdicionarImpressoraNoPc(EstadoEquipamentos e, List<AcaoFisica> l)
    {
        if (!e.pcLigado || !e.pcSemImpressora) return;
        PecaAtual = "tela";
        l.Add(new AcaoFisica($"Adicionar a impressora pelo IP {e.ipImpressora} (Impressoras > Adicionar > TCP/IP)", () =>
        {
            string r = e.AdicionarImpressoraPeloIp(out bool ok);
            if (ok) MM?.ConsumirTempo(10f);
            return r;
        }, 3f));
    }
}
