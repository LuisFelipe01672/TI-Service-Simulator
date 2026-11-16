using System.Collections.Generic;
using UnityEngine;

// Ordem de serviço (OS): aberta quando o técnico aceita o chamado e preenchida sozinha pelo que ele faz
// (relato do cliente, perguntas, ações, peças, tempo). No fim, o cliente confere o checklist, assina e avalia.
[System.Serializable]
public class OrdemServico
{
    public int numero;
    public string cliente;
    public string contato;
    public string equipamento;
    public string relato;                 // como o cliente descreveu
    public string abertura;               // "Seg 02/03 08:00"
    public string prioridade;
    public string diagnostico;            // o que o técnico concluiu (preenchido ao resolver)
    public List<string> acoes = new List<string>();
    public List<string> pecas = new List<string>();
    public float valorPecas;
    public float valorServico;
    public float minutosTrabalho;         // do aceite até a assinatura (horas do jogo)
    public bool noPrazo = true;
    public bool assinada;
    public int estrelas;
    public string comentario = "";
    public bool retrabalho;               // atendimento de retorno (garantia)
    public string causaPendente = "";     // o que ficou sem resolver (o técnico só descobre quando o cliente volta)

    // Ação repetida seguida (ex.: imprimir 3 páginas de teste) vira uma linha só com "(x3)"
    public void RegistrarAcao(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (acoes.Count > 0)
        {
            string ultima = acoes[acoes.Count - 1];
            if (ultima == texto) { acoes[acoes.Count - 1] = texto + " (x2)"; return; }
            int i = ultima.LastIndexOf(" (x");
            if (i > 0 && ultima.Substring(0, i) == texto && int.TryParse(ultima.Substring(i + 3).TrimEnd(')'), out int n))
            { acoes[acoes.Count - 1] = $"{texto} (x{n + 1})"; return; }
        }
        if (acoes.Count < 40) acoes.Add(texto);
    }

    public void RegistrarPeca(string nome, float valor)
    {
        valorPecas += valor;
        pecas.Add($"{(string.IsNullOrEmpty(nome) ? "Material" : nome)} — {UIFactory.Dinheiro(valor)}");
    }

    public static string HorasTexto(float minutos)
    {
        int m = Mathf.Max(0, Mathf.RoundToInt(minutos));
        return m < 60 ? $"{m} min" : $"{m / 60}h{m % 60:00}";
    }
}

// Prioridade do chamado: define o prazo de atendimento (SLA) em horas do jogo, contado desde a abertura.
public enum PrioridadeSLA { Baixa, Normal, Alta, Urgente }

public static class SLA
{
    // Horas do jogo para resolver, contadas desde que o cliente abriu o chamado
    public static float Horas(PrioridadeSLA p) =>
        p == PrioridadeSLA.Urgente ? 2f : p == PrioridadeSLA.Alta ? 4f : p == PrioridadeSLA.Normal ? 6f : 9f;

    public static PrioridadeSLA Definir(DificuldadeChamado dif, Personalidade jeito, bool contrato, bool retrabalho)
    {
        if (retrabalho) return PrioridadeSLA.Urgente;
        if (contrato || jeito == Personalidade.VIP || dif == DificuldadeChamado.Dificil) return PrioridadeSLA.Alta;
        if (jeito == Personalidade.Apressado || dif == DificuldadeChamado.Medio) return PrioridadeSLA.Normal;
        return PrioridadeSLA.Baixa;
    }

    public static string Texto(PrioridadeSLA p) =>
        p == PrioridadeSLA.Urgente ? "Urgente" : p == PrioridadeSLA.Alta ? "Alta" : p == PrioridadeSLA.Normal ? "Normal" : "Baixa";

    public static string Cor(PrioridadeSLA p) =>
        p == PrioridadeSLA.Urgente ? "#F2545B" : p == PrioridadeSLA.Alta ? "#F5B83F" : p == PrioridadeSLA.Normal ? "#3FA9F5" : "#9AA6B8";

    // Minuto absoluto do jogo (dia 1 às 00:00 = 0). Sem relógio (testes, cena sem DiaManager): segundos desde o início.
    public static float MinutoAbsoluto(int dia, float minutoDoDia) => (Mathf.Max(1, dia) - 1) * 1440f + minutoDoDia;

    public static float Agora()
    {
        var d = DiaManager.Instance;
        return d != null ? MinutoAbsoluto(d.dia, d.minutoAtual) : 480f + Time.time;
    }

    public static float Prazo(Chamado c) => c.abertoEm + c.tempoLimite;
    public static float Restante(Chamado c, float agora) => Prazo(c) - agora;

    // "11:30" (ou "amanhã 08:30" quando passa do dia)
    public static string HoraDoPrazo(Chamado c, float agora)
    {
        float prazo = Prazo(c);
        int diaPrazo = Mathf.FloorToInt(prazo / 1440f), diaAgora = Mathf.FloorToInt(agora / 1440f);
        int m = Mathf.FloorToInt(prazo - diaPrazo * 1440f);
        string hora = $"{m / 60:00}:{m % 60:00}";
        return diaPrazo == diaAgora ? hora : diaPrazo == diaAgora + 1 ? "amanhã " + hora : hora;
    }
}

// Um item do checklist que o cliente confere antes de assinar
public struct ItemChecklist
{
    public string texto;
    public bool ok;
    public string falha;   // como o cliente reclama se faltou
    public ItemChecklist(string texto, bool ok, string falha) { this.texto = texto; this.ok = ok; this.falha = falha; }
}

// Checklist do fim do atendimento, estrelas e comentário do cliente.
// Funções puras (sem cena), testadas em Editor/Testes/TestesOSeSLA.cs.
public static class AvaliacaoCliente
{
    public static List<ItemChecklist> Checklist(Problema p, EstadoEquipamentos e, int sujeirasNoChao)
    {
        var l = new List<ItemChecklist>();
        bool resolvido = p == null || e == null || CatalogoProblemas.Resolvido(p, e);
        l.Add(new ItemChecklist("Defeito testado na frente do cliente", resolvido, "o problema não foi resolvido"));
        bool fechado = e == null || (!e.pcGabineteAberto && !e.pcCoolerFora && !e.impTampaAberta && !e.impGavetaAberta && !e.cartuchoNaMao);
        l.Add(new ItemChecklist("Equipamento fechado e montado", fechado, "deixou o equipamento aberto"));
        bool ligado = e == null || (e.impLigada && e.pcLigado && e.rotNaTomada);
        l.Add(new ItemChecklist("Tudo ligado como estava", ligado, "deixou equipamento desligado"));
        l.Add(new ItemChecklist("Local limpo (nada no chão)", sujeirasNoChao == 0, "deixou sujeira no chão"));
        return l;
    }

    public static int Falhas(List<ItemChecklist> l)
    {
        int n = 0;
        foreach (var i in l) if (!i.ok) n++;
        return n;
    }

    // 5 estrelas = tudo certo. Atraso, checklist incompleto, estragos e retorno tiram estrelas. VIP é mais exigente.
    public static int Estrelas(float minutosAtraso, int itensFalhos, int erros, Personalidade jeito, bool retrabalho)
    {
        int e = 5;
        if (minutosAtraso > 0f) e -= minutosAtraso > 120f ? 2 : 1;
        e -= itensFalhos;
        if (erros > 0) e -= erros >= 3 ? 2 : 1;
        if (retrabalho) e -= 1;
        if (jeito == Personalidade.VIP && e < 5) e -= 1;
        return Mathf.Clamp(e, 1, 5);
    }

    // Reputação ganha (ou perdida) por estrela, antes da escala do ReputationManager
    public static float ReputacaoPorEstrelas(int estrelas) =>
        estrelas >= 5 ? 6f : estrelas == 4 ? 4f : estrelas == 3 ? 1f : estrelas == 2 ? -6f : -12f;

    // O que o cliente escreve: o motivo principal aparece quando faltou algo
    public static string Comentario(int estrelas, string motivo, int variacao = 0)
    {
        string[] bons = { "Rápido, educado e deixou tudo limpo. Recomendo!", "Resolveu de primeira e ainda explicou o que era. Nota 10.",
                          "Atendimento excelente, já salvei o contato." };
        string[] medios = { "Resolveu, mas", "Ficou funcionando, só que", "Bom serviço, porém" };
        string[] ruins = { "Não gostei:", "Péssimo:", "Decepcionado:" };
        int v = Mathf.Abs(variacao);
        if (estrelas >= 5 || string.IsNullOrEmpty(motivo)) return bons[v % bons.Length];
        if (estrelas >= 3) return $"{medios[v % medios.Length]} {motivo}.";
        return $"{ruins[v % ruins.Length]} {motivo}. Vou pensar duas vezes antes de chamar de novo.";
    }
}

// Cliente que vai voltar a chamar porque a causa do problema ficou (retrabalho em garantia)
[System.Serializable]
public class RetornoPendente
{
    public int id;
    public string cliente;
    public string contato;
    public TipoEquipamento equipamento;
    public string solucao;
    public DificuldadeChamado dificuldade;
    public Personalidade personalidade;
    public bool exigeNota;
    public int diaRetorno;
    public string motivo;
    public bool avisado;     // o cliente já ligou (a penalidade já foi aplicada)
    public int numeroOS;     // OS original
}
