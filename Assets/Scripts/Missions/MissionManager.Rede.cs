using UnityEngine;

// Etapa 8: visitas de execução dos projetos de rede viram chamados no cliente (rack bagunçado, pontos de
// cabeamento). O tablet não faz o trabalho: só mostra o andamento e manda o técnico para lá.
public partial class MissionManager
{
    public Chamado ChamadoDoProjeto(int projetoId)
    {
        if (chamadoAtivo != null && chamadoAtivo.projetoId == projetoId) return chamadoAtivo;
        return chamadosDisponiveis.Find(c => c.projetoId == projetoId);
    }

    public Chamado AdicionarChamadoProjeto(Projeto p)
    {
        if (p == null) return null;
        var existente = ChamadoDoProjeto(p.id);
        if (existente != null) return existente;
        var prob = p.tipo == TipoProjeto.Rack ? CatalogoProblemas.OrganizacaoRack : CatalogoProblemas.CabeamentoPontos;
        var prioridade = PrioridadeSLA.Baixa;   // serviço agendado
        var ch = new Chamado(p.empresa, prob.descricao, TipoChamado.Rede, DificuldadeChamado.Medio, 0f, SLA.Horas(prioridade) * 60f)
        {
            equipamento = prob.equipamento, solucao = prob.solucao, problema = prob, projetoId = p.id,
            personalidade = Personalidade.Tranquilo, contato = CatalogoProblemas.SortearContato(),
            prioridade = prioridade, abertoEm = SLA.Agora(), exigeNota = false
        };
        ch.anotacoes.Add(p.tipo == TipoProjeto.Rack
            ? "Organizar o rack: foto do antes, mapa das portas, cordões curtos, organizador, velcro, etiquetas, links e foto do depois."
            : $"Cabeamento: {ProjetosManager.PontosNaVisita(p)} ponto(s) nesta visita. Canaleta, cabo, crimpar, punch-down, testar e etiquetar.");
        chamadosDisponiveis.Insert(0, ch);
        Debug.Log($"[MissionManager] Visita do projeto {p.id} ({p.Info.nome}) na {p.empresa}.");
        return ch;
    }

    static string DiagnosticoProjeto(Chamado ch)
    {
        var e = ch.estado;
        if (ch.problema != null && ch.problema.solucao == CatalogoProblemas.OrganizarRack)
            return $"Rack organizado: {EstadoEquipamentos.PortasRack} cordões de 2,5 m trocados por cordões de 0,3 m, passados pelo organizador, " +
                   $"presos com velcro e etiquetados porta a porta (P01 a P{EstadoEquipamentos.PortasRack:00}). Links conferidos no switch." +
                   (e != null && e.rackMapaAnotado ? " Mapa das portas anotado antes de desconectar." : "");
        int ok = 0;
        var nomes = new System.Collections.Generic.List<string>();
        if (e != null) foreach (var p in e.pontos) if (p.Pronto) { ok++; nomes.Add(p.Nome); }
        return $"Cabeamento estruturado: {ok} ponto(s) Cat6 ({string.Join(", ", nomes)}) por canaleta até o rack, crimpados, inseridos no patch panel, " +
               "testados ponto a ponto (8 fios) e etiquetados nas duas pontas.";
    }
}
