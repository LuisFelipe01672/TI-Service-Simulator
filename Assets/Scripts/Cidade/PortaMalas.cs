using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Porta-malas do carro do técnico (Etapa 9C): mire a traseira e E abre a tampa (ela sobe nos amortecedores).
// Aberto: com um volume nas mãos, E guarda; sem nada nas mãos, cada volume lá dentro se tira com E nele, e E na
// tampa fecha. Cabem 4 caixas no assoalho, além da escada dobrada (no fundo, encostada no banco) e da maleta grande.
// Não se sai dirigindo com a tampa aberta: o carro avisa para fechar antes.
public class PortaMalas : Interactable
{
    public static PortaMalas Instance { get; private set; }

    public CarroTecnico carro;
    public Transform[] vagas = new Transform[4];   // assoalho (montado pelo editor, no espaço do carro)
    public Transform vagaEscada, vagaMaleta;
    public CargaFisica escada, maleta;             // vêm com o carro

    readonly CargaFisica[] ocupacao = new CargaFisica[4];
    readonly List<EquipUsado> usadosDentro = new List<EquipUsado>();
    public int Livres
    {
        get
        {
            usadosDentro.RemoveAll(x => x.local != LocalUsado.Carro || !EquipamentosUsados.Estoque.Contains(x));   // vendido ou tirado
            int n = 0;
            foreach (var o in ocupacao) if (o == null) n++;
            return n - usadosDentro.Count;
        }
    }

    // Tampa fechada: o que está lá dentro não se alcança (nem aparece o aviso do E)
    public void AtualizarConteudo()
    {
        bool aberta = Aberta;
        foreach (var o in ocupacao) if (o != null) o.enabled = aberta;
        if (escada != null && escada.noPortaMalas) escada.enabled = aberta;
        if (maleta != null && maleta.noPortaMalas) maleta.enabled = aberta;
        foreach (var e in usadosDentro) UsadosFisicos.Instance?.Objeto(e)?.AoMudarLocal();
    }

    protected override bool PrecisaMirar => true;

    void Awake()
    {
        Instance = this;
        if (escada != null) Encaixar(escada, vagaEscada);
        if (maleta != null) Encaixar(maleta, vagaMaleta);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    bool Aberta => carro != null && carro.TampaAberta;

    public override string TextoPrompt
    {
        get
        {
            if (CarroTecnico.NoCarro) return "";
            if (!Aberta) return "[E] Abrir o porta-malas";
            if (CargaFisica.NaMao != null) return Cabe(CargaFisica.NaMao) ? $"[E] Guardar no porta-malas: {CargaFisica.NaMao.Nome}" : "Porta-malas cheio";
            if (UsadosFisicos.NaMao != null) return Livres > 0 ? $"[E] Guardar no porta-malas: {UsadosFisicos.NaMao.nome}" : "Porta-malas cheio";
            return "[E] Fechar o porta-malas";
        }
    }

    public override void Interagir()
    {
        if (CarroTecnico.NoCarro || carro == null) return;
        if (!Aberta) { carro.AbrirTampa(true); Debug.Log("[Carga] Porta-malas aberto."); return; }
        if (CargaFisica.NaMao != null) { Guardar(CargaFisica.NaMao); return; }
        if (UsadosFisicos.NaMao != null) { GuardarUsado(UsadosFisicos.NaMao); return; }
        carro.AbrirTampa(false);
        Debug.Log("[Carga] Porta-malas fechado.");
    }

    // ---------- Guardar e tirar ----------

    bool Cabe(CargaFisica c) => c.item.tipo == TipoCarga.Escada ? escada == c : c.item.tipo == TipoCarga.Maleta ? maleta == c : Livres > 0;

    public bool Guardar(CargaFisica c)
    {
        if (c == null) return false;
        if (!Cabe(c)) { GameUI.Instance?.Toast("Não cabe mais nada no porta-malas.", UIFactory.Alerta, 3f); return false; }
        if (CargaFisica.NaMao == c) CargaFisica.Soltar();
        Transform vaga = c.item.tipo == TipoCarga.Escada ? vagaEscada : c.item.tipo == TipoCarga.Maleta ? vagaMaleta : null;
        if (vaga == null)
            for (int i = 0; i < ocupacao.Length; i++)
                if (ocupacao[i] == null && VagaSemUsado(i)) { ocupacao[i] = c; vaga = vagas[i]; break; }
        if (vaga == null) return false;
        if (c.item.tipo == TipoCarga.Encomenda && c.entrega != null) c.entrega.local = 1;
        Encaixar(c, vaga, true);
        SomManager.TocarEm("mexer", vaga.position, 0.8f, 0.7f);
        Debug.Log($"[Carga] Guardou no porta-malas: {c.Nome} ({Livres} vaga(s) livre(s)).");
        return true;
    }

    void Encaixar(CargaFisica c, Transform vaga, bool animar = false)
    {
        var t = c.transform;
        t.SetParent(vaga, true);
        if (c.modelo != null) c.modelo.localScale = c.escalaDobrada;
        Quaternion giro = c.item.tipo == TipoCarga.Escada ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        if (animar)
        {
            Tween.LocalPosition(t, Vector3.zero, 0.4f, Ease.OutCubic);
            Tween.LocalRotation(t, giro, 0.4f, Ease.OutCubic);
        }
        else { t.localPosition = Vector3.zero; t.localRotation = giro; }
        foreach (var col in c.GetComponentsInChildren<Collider>()) { col.enabled = true; col.isTrigger = true; }   // não empurra o carro
        c.noPortaMalas = true;
        c.enabled = Aberta;
    }

    // Saiu do porta-malas (vai para as mãos)
    public void Tirar(CargaFisica c)
    {
        for (int i = 0; i < ocupacao.Length; i++) if (ocupacao[i] == c) ocupacao[i] = null;
        foreach (var col in c.GetComponentsInChildren<Collider>()) col.isTrigger = false;
        if (c.item.tipo == TipoCarga.Encomenda && c.entrega != null) c.entrega.local = 2;
        c.noPortaMalas = false;
        Debug.Log($"[Carga] Tirou do porta-malas: {c.Nome}.");
    }

    public void Remover(CargaFisica c)
    {
        for (int i = 0; i < ocupacao.Length; i++) if (ocupacao[i] == c) ocupacao[i] = null;
    }

    // Lote de usados: a caixa do lote ocupa uma vaga (o UsadoFisico se posiciona pela vaga, LocalUsado.Carro)
    void GuardarUsado(EquipUsado e)
    {
        if (Livres <= 0) { GameUI.Instance?.Toast("Não cabe mais nada no porta-malas.", UIFactory.Alerta, 3f); return; }
        int vaga = VagaLivreParaUsado();
        if (vaga < 0) return;
        usadosDentro.Add(e);
        e.vaga = 100 + vaga;            // 100+ = vaga do porta-malas
        e.local = LocalUsado.Carro;
        UsadosFisicos.Instance?.SoltarDaMao(e);
        UsadosFisicos.Instance?.Posicionar(e, true);
        Debug.Log($"[Carga] Guardou no porta-malas o usado: {e.nome}.");
    }

    int VagaLivreParaUsado()
    {
        for (int i = ocupacao.Length - 1; i >= 0; i--) if (ocupacao[i] == null && VagaSemUsado(i)) return i;
        return -1;
    }

    bool VagaSemUsado(int i)
    {
        foreach (var e in usadosDentro) if (e.vaga == 100 + i) return false;
        return true;
    }

    public Transform VagaDoUsado(EquipUsado e) => e.vaga >= 100 && e.vaga - 100 < vagas.Length ? vagas[e.vaga - 100] : vagas[0];

    public void UsadoSaiu(EquipUsado e) => usadosDentro.Remove(e);

    public IEnumerable<CargaFisica> Volumes()
    {
        foreach (var o in ocupacao) if (o != null) yield return o;
    }

    // ---------- Save ----------

    // Salva as encomendas (pela posição na lista de entregas) e os lotes de usados; escada e maleta voltam sempre
    public List<ItemCarga> Exportar()
    {
        var l = new List<ItemCarga>();
        var m = Mochila.Instance;
        foreach (var o in ocupacao)
            if (o != null && o.item.tipo == TipoCarga.Encomenda && o.entrega != null && m != null && m.entregas.Contains(o.entrega))
                l.Add(new ItemCarga { tipo = TipoCarga.Encomenda, id = m.entregas.IndexOf(o.entrega), rotulo = o.Nome });
        foreach (var e in usadosDentro) l.Add(new ItemCarga { tipo = TipoCarga.LoteUsado, id = e.id, rotulo = e.nome });
        return l;
    }

    public void Carregar(List<ItemCarga> itens)
    {
        for (int i = 0; i < ocupacao.Length; i++)
            if (ocupacao[i] != null) { Destroy(ocupacao[i].gameObject); ocupacao[i] = null; }
        usadosDentro.Clear();
        if (escada != null && CargaFisica.NaMao != escada) Encaixar(escada, vagaEscada);
        if (maleta != null && CargaFisica.NaMao != maleta) Encaixar(maleta, vagaMaleta);
        if (itens == null) return;
        var m = Mochila.Instance;
        foreach (var it in itens)
        {
            if (it.tipo == TipoCarga.Encomenda && m != null && it.id >= 0 && it.id < m.entregas.Count)
            {
                var e = m.entregas[it.id];
                EncomendaFisica.Esconder(e);
                var c = CargaFisica.CriarCaixa(TipoCarga.Encomenda, "Encomenda: " + Mochila.Info(e.item).nome, EncomendaFisica.Tamanho(e),
                    "ENTREGA\n" + Mochila.Info(e.item).nome);
                c.entrega = e;
                Guardar(c);
            }
            else if (it.tipo == TipoCarga.LoteUsado)
            {
                var e = EquipamentosUsados.Estoque.Find(x => x.id == it.id);
                if (e != null && e.local == LocalUsado.Carro) usadosDentro.Add(e);
            }
        }
        foreach (var e in EquipamentosUsados.Estoque)
            if (e.local == LocalUsado.Carro && !usadosDentro.Contains(e)) usadosDentro.Add(e);
        UsadosFisicos.Instance?.Sincronizar();
    }
}
