using System.Collections.Generic;
using UnityEngine;

// Etapa 8: um cordão (patch cord) entre uma porta do patch panel e uma porta do switch, dentro do rack
[System.Serializable]
public class CordaoRack
{
    public int portaPatch, portaSwitch;   // 1 a 24
    public float metros;                  // 0,3 m = do tamanho certo; 2,5 m = sobra embolada
    public int cor;                       // índice em EquipamentoPecas.CoresCordao
    public bool Curto => metros <= 0.5f;
}

// Etapa 8: um ponto de rede novo do projeto de cabeamento (tomada na parede » canaleta » patch panel do rack)
[System.Serializable]
public class PontoRede
{
    public int numero;          // número no projeto (P-07)
    public int portaPatch;      // porta do patch panel onde ele chega
    public bool canaleta;       // caixa de sobrepor e canaleta fixadas até o rack
    public bool caboPassado;    // cabo Cat6 passado pela canaleta
    public bool crimpado;       // ponta crimpada na tomada
    public bool pontaBoa;       // a crimpagem ficou certa (só o testador mostra)
    public bool noPatch;        // cabo inserido no patch panel com o punch-down
    public bool testado;        // passou pelo testador depois da última mexida
    public bool etiquetado;     // etiqueta na tomada e na porta do patch panel
    public bool Ok => testado && pontaBoa && noPatch && crimpado;
    public bool Pronto => Ok && etiquetado;
    public string Nome => $"P-{numero:00}";
}

// Rack de parede do cliente (organizar o rack bagunçado e o projeto de cabeamento).
// Fora dos chamados de rede (rackServico = false) o rack fica arrumado e o visual mostra o padrão.
public partial class EstadoEquipamentos
{
    public const int PortasRack = 8;           // portas em uso no patch panel/switch do cliente

    public bool rackServico;                   // chamado de organizar o rack
    public bool rackPortaAberta;               // a porta de vidro do rack
    public bool rackFotoAntes, rackFotoDepois;
    public bool rackMexeu;                     // já mexeu nos cordões (a foto do antes não dá mais)
    public bool rackMapaAnotado;               // anotou qual porta do patch vai em qual porta do switch
    public List<CordaoRack> rackCordoes = new List<CordaoRack>();
    public bool rackNoOrganizador, rackVelcro, rackLinksConferidos;
    public List<int> rackEtiquetas = new List<int>();   // portas do patch panel já etiquetadas
    public List<PontoRede> pontos = new List<PontoRede>();   // projeto de cabeamento: pontos desta visita
    public int rackVersao;                     // muda a cada mexida (o visual refaz os cordões)
    [System.NonSerialized] public Texture2D fotoAntes, fotoDepois;

    static readonly int[] mapaBaguncado = { 7, 2, 11, 4, 1, 9, 3, 6 };

    // Rack bagunçado: 8 cordões de 2,5 m cruzados (patch 1 » switch 7...), sem etiqueta, porta que nem fecha
    public void MontarRackBaguncado()
    {
        rackServico = true;
        rackPortaAberta = true;
        rackFotoAntes = rackFotoDepois = rackMexeu = rackMapaAnotado = false;
        rackNoOrganizador = rackVelcro = rackLinksConferidos = false;
        rackEtiquetas.Clear();
        rackCordoes.Clear();
        for (int i = 0; i < PortasRack; i++)
            rackCordoes.Add(new CordaoRack { portaPatch = i + 1, portaSwitch = mapaBaguncado[i], metros = 2.5f, cor = (i * 3) % 4 });
        rackVersao++;
    }

    // Projeto de cabeamento: rack já arrumado; os pontos novos vêm do projeto (PrepararPontos)
    public void PrepararPontos(int primeiroNumero, int quantidade)
    {
        pontos.Clear();
        for (int i = 0; i < quantidade; i++)
            pontos.Add(new PontoRede { numero = primeiroNumero + i, portaPatch = PortasRack + 1 + i });
        rackVersao++;
    }

    // ---------- Organizar o rack ----------

    public bool RackTodosConectados => rackCordoes.Count == PortasRack;
    public bool RackCordoesCurtos => RackTodosConectados && rackCordoes.TrueForAll(c => c.Curto);
    public bool RackEtiquetado => rackEtiquetas.Count >= PortasRack;
    public int ProximaPortaSemCordao() { for (int p = 1; p <= PortasRack; p++) if (!rackCordoes.Exists(c => c.portaPatch == p)) return p; return 0; }
    public int ProximaPortaSemEtiqueta() { for (int p = 1; p <= PortasRack; p++) if (!rackEtiquetas.Contains(p)) return p; return 0; }

    public bool RackOrganizado => RackCordoesCurtos && rackNoOrganizador && rackVelcro && RackEtiquetado && rackLinksConferidos && rackFotoDepois;

    // A rede do cliente funciona (todo cordão no lugar)?
    public bool RackFuncionando => !rackServico || RackTodosConectados;

    public string OlharRack()
    {
        if (!rackServico) return "Rack de parede 12U: patch panel, organizador e switch, cordões curtos e etiquetados. Tudo em ordem.";
        if (RackTodosConectados && !RackCordoesCurtos)
            return $"{rackCordoes.FindAll(c => !c.Curto).Count} cordão(ões) de 2,5 m embolados, pendurados para fora do rack, " +
                   $"{(RackEtiquetado ? "" : "nenhum com etiqueta, ")}cruzando de um lado para o outro. A porta de vidro nem fecha.";
        if (rackCordoes.Count == 0) return "Patch panel e switch sem nenhum cordão: a rede do escritório está fora.";
        return $"{rackCordoes.Count} de {PortasRack} portas com cordão." + (rackNoOrganizador ? " Passando pelo organizador." : "") +
               (rackVelcro ? " Presos com velcro." : "") + $" Etiquetas: {rackEtiquetas.Count}/{PortasRack}.";
    }

    public string MapaPortas()
    {
        var sb = new System.Text.StringBuilder("Mapa anotado: ");
        foreach (var c in rackCordoes) sb.Append($"P{c.portaPatch:00} » SW{c.portaSwitch:00}  ");
        return sb.ToString().TrimEnd();
    }

    // Desconecta todos os cordões velhos. Sem o mapa anotado: ninguém sabe mais qual ramal era de quem.
    public string DesconectarCordoes(out bool semMapa)
    {
        semMapa = !rackMapaAnotado;
        int n = rackCordoes.Count;
        rackCordoes.Clear();
        rackNoOrganizador = rackVelcro = rackLinksConferidos = false;
        rackMexeu = true;
        rackVersao++;
        return $"Você soltou os {n} cordões velhos (as travas dos conectores fazem clique). A rede do escritório caiu enquanto isso.";
    }

    // Liga o próximo cordão: curto (0,3 m, do tamanho certo) ou o velho de 2,5 m reaproveitado
    public string ConectarCordao(bool curto)
    {
        int p = ProximaPortaSemCordao();
        if (p == 0) return "Todas as portas já têm cordão.";
        rackCordoes.Add(new CordaoRack { portaPatch = p, portaSwitch = p, metros = curto ? 0.3f : 2.5f, cor = curto ? 0 : (p * 3) % 4 });
        rackMexeu = true;
        rackLinksConferidos = false;
        rackVersao++;
        return curto ? $"Cordão de 0,3 m: P{p:00} » SW{p:00}. Clique!"
                     : $"Cordão velho de 2,5 m: P{p:00} » SW{p:00}. Sobrou um bolo de cabo pendurado.";
    }

    public string PassarNoOrganizador()
    {
        if (!RackTodosConectados) return "Ligue todos os cordões antes de passar pelo organizador.";
        if (!RackCordoesCurtos) return "Cordões de 2,5 m não cabem no organizador: sobra cabo para todo lado. Use os de 0,3 m.";
        rackNoOrganizador = true;
        rackVersao++;
        return "Cordões passados pelos anéis do organizador, um ao lado do outro.";
    }

    public string PrenderVelcro()
    {
        if (!rackNoOrganizador) return "Passe os cordões pelo organizador primeiro.";
        rackVelcro = true;
        rackVersao++;
        return "Feixe preso com abraçadeira de velcro (abraçadeira de nylon apertada estraga o cabo).";
    }

    public string Etiquetar()
    {
        int p = ProximaPortaSemEtiqueta();
        if (p == 0) return "Todas as portas já estão etiquetadas.";
        rackEtiquetas.Add(p);
        rackVersao++;
        return $"Etiqueta P{p:00} colada na porta do patch panel e na ponta do cordão.";
    }

    public string ConferirLinks()
    {
        int ok = rackCordoes.Count;
        rackLinksConferidos = RackTodosConectados;
        if (ok == 0) return "Nenhuma luz de link acesa no switch.";
        return RackTodosConectados ? $"As {ok} luzes de link estão verdes e piscando: todo mundo tem rede de novo."
                                   : $"Só {ok} de {PortasRack} luzes de link acesas: falta cordão.";
    }

    // O que ainda falta para entregar o rack (janela de manutenção e atalhos de teste)
    public List<string> PendenciasRack()
    {
        var l = new List<string>();
        if (!rackServico) return l;
        if (!rackFotoAntes && !rackMexeu) l.Add("foto do antes");
        if (!rackMapaAnotado && !rackMexeu) l.Add("anotar o mapa das portas");
        if (!RackCordoesCurtos) l.Add("cordões curtos em todas as portas");
        if (!rackNoOrganizador) l.Add("passar pelo organizador");
        if (!rackVelcro) l.Add("velcro");
        if (!RackEtiquetado) l.Add($"etiquetas ({rackEtiquetas.Count}/{PortasRack})");
        if (!rackLinksConferidos) l.Add("conferir as luzes do switch");
        if (rackPortaAberta) l.Add("fechar a porta do rack");
        if (!rackFotoDepois) l.Add("foto do depois");
        return l;
    }

    // ---------- Pontos de rede (projeto de cabeamento) ----------

    public bool PontosConcluidos => pontos.Count > 0 && pontos.TrueForAll(p => p.Pronto);

    public List<string> PendenciasPontos()
    {
        var l = new List<string>();
        foreach (var p in pontos)
        {
            if (p.Pronto) continue;
            string f = !p.canaleta ? "canaleta" : !p.caboPassado ? "passar o cabo" : !p.crimpado ? "crimpar" :
                       !p.noPatch ? "punch-down no patch" : !p.Ok ? (p.testado ? "refazer a ponta" : "testar") : "etiquetar";
            l.Add($"{p.Nome}: {f}");
        }
        return l;
    }

    public PontoRede Ponto(int indice) => indice >= 0 && indice < pontos.Count ? pontos[indice] : null;
}
