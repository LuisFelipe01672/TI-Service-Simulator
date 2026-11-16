using System;
using System.Collections.Generic;

// Uma definição por equipamento do cliente: nome, peças, ações com as mãos, "está funcionando?" e o checklist
// do fim do atendimento. O estado fica em EstadoEquipamentos.<Equipamento>.cs; os defeitos de cada problema ficam
// no próprio Problema do catálogo (CatalogoProblemas).
// Equipamento novo: um valor no enum TipoEquipamento + uma classe aqui + RegistroEquipamentos.Todas.
public abstract class DefinicaoEquipamento
{
    public abstract TipoEquipamento Tipo { get; }
    public abstract string Nome { get; }                 // "Impressora"
    public virtual string Artigo => "o";                  // "a impressora", "o roteador"
    public abstract TipoChamado TipoChamado { get; }
    public abstract string[] Pecas { get; }               // ids das peças 3D (mirar + E); a primeira é a principal

    // Ações possíveis agora, de acordo com o estado (a peça de cada ação vem de l.peca)
    public abstract void Acoes(EstadoEquipamentos e, ListaAcoes l);

    // O equipamento está funcionando como o cliente espera? (base do "resolvido" de todo problema dele)
    public abstract bool Funcionando(EstadoEquipamentos e);

    // Checklist do fim do atendimento: nada aberto/desmontado e tudo ligado como estava
    public abstract bool Fechado(EstadoEquipamentos e);
    public abstract bool Ligado(EstadoEquipamentos e);
}

// Lista de ações que vai sendo montada por peça: muda "peca" e as próximas ações ficam nela.
public class ListaAcoes : List<AcaoFisica>
{
    public string peca;
    public ListaAcoes(string peca = null) { this.peca = peca; }

    public new void Add(AcaoFisica a)
    {
        if (a.peca == null) a.peca = peca;
        base.Add(a);
    }
}

public static class RegistroEquipamentos
{
    // Todas as definições. Falta uma? O construtor estático acusa na primeira consulta (e o teste também).
    public static readonly DefinicaoEquipamento[] Todas =
    {
        new DefinicaoImpressora(),
        new DefinicaoComputador(),
        new DefinicaoRoteador(),
        new DefinicaoNotebook(),
    };

    static readonly Dictionary<TipoEquipamento, DefinicaoEquipamento> porTipo = Montar();

    static Dictionary<TipoEquipamento, DefinicaoEquipamento> Montar()
    {
        var d = new Dictionary<TipoEquipamento, DefinicaoEquipamento>();
        foreach (var def in Todas)
        {
            if (d.ContainsKey(def.Tipo)) throw new InvalidOperationException($"Equipamento {def.Tipo} definido duas vezes em RegistroEquipamentos.");
            d[def.Tipo] = def;
        }
        foreach (TipoEquipamento t in Enum.GetValues(typeof(TipoEquipamento)))
            if (!d.ContainsKey(t)) throw new InvalidOperationException($"Equipamento {t} sem definição: crie a classe e ponha em RegistroEquipamentos.Todas.");
        return d;
    }

    public static DefinicaoEquipamento De(TipoEquipamento t)
    {
        if (porTipo.TryGetValue(t, out var def)) return def;
        throw new InvalidOperationException($"Equipamento {t} sem definição em RegistroEquipamentos.");
    }

    public static string Nome(TipoEquipamento t) => De(t).Nome;
}
