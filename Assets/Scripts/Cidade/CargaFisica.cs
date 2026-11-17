using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Etapa 9C: volumes que o técnico carrega nas mãos e leva no porta-malas do carro.
// - Encomenda: a caixa que o entregador deixa na porta de casa (EncomendaFisica): [G] pega, E no chão larga.
// - Impressora de locação: no chamado de instalação, a caixa sai do escritório; largada dentro da sala do cliente,
//   fica no lugar da impressora (e aí abre-se com o estilete, como antes).
// - Lote de usados: a caixa do lote (UsadoFisico, LocalUsado.Carro) vai e volta do porta-malas.
// - Escada (dobrada no porta-malas, aberta no chão), maleta grande de ferramentas e galão de gasolina do posto.
// Uma coisa por vez nas mãos (as duas mãos). E sem nada mirado larga no chão, na frente do técnico.
public enum TipoCarga { Encomenda, LoteUsado, ImpressoraLocacao, Escada, Maleta, Galao }

[System.Serializable]
public class ItemCarga
{
    public TipoCarga tipo;
    public int id;               // encomenda: posição na lista de entregas; lote: id do usado; locação: id do contrato
    public string rotulo;
}

public class CargaFisica : Interactable
{
    public static CargaFisica NaMao { get; private set; }
    public static bool MaosOcupadas => NaMao != null || UsadosFisicos.NaMao != null;

    public ItemCarga item = new ItemCarga();
    [System.NonSerialized] public Entrega entrega;      // só para encomendas
    public Vector3 tamanho = new Vector3(0.4f, 0.3f, 0.4f);
    public bool noPortaMalas;
    public Transform modelo;                            // escada: o modelo (dobra e abre mudando a escala)
    public Vector3 escalaAberta = Vector3.one, escalaDobrada = Vector3.one;

    public string Nome => string.IsNullOrEmpty(item.rotulo) ? item.tipo.ToString() : item.rotulo;

    public override string TextoPrompt
    {
        get
        {
            if (MaosOcupadas) return Nome + " (mãos ocupadas)";
            if (noPortaMalas) return $"[E] Tirar do porta-malas: {Nome}";
            return $"[E] Pegar: {Nome}";
        }
    }

    public override void Interagir()
    {
        if (MaosOcupadas) { GameUI.Instance?.Toast("Suas mãos estão ocupadas: largue ou guarde o que está segurando.", UIFactory.Alerta, 3f); return; }
        if (noPortaMalas) PortaMalas.Instance?.Tirar(this);
        Pegar(this);
    }

    // ---------- Mãos ----------

    static int quadroMudou;
    public static bool MudouNesteQuadro => quadroMudou == Time.frameCount;

    public static void Pegar(CargaFisica c)
    {
        var cam = Camera.main;
        if (c == null || cam == null) return;
        NaMao = c;
        quadroMudou = Time.frameCount;
        c.noPortaMalas = false;
        c.enabled = false;                                  // nas mãos não é "interagível"
        foreach (var col in c.GetComponentsInChildren<Collider>()) col.enabled = false;
        if (c.modelo != null && c.modelo.localScale != c.escalaDobrada) Tween.Scale(c.modelo, c.escalaDobrada, 0.35f, Ease.OutCubic);   // a escada fecha
        var t = c.transform;
        Tween.StopAll(t);
        t.SetParent(cam.transform, true);
        Vector3 alvo = new Vector3(0.05f, -0.42f - c.tamanho.y * 0.3f, 0.55f + c.tamanho.z * 0.5f);
        Quaternion giro = c.item.tipo == TipoCarga.Escada ? Quaternion.Euler(0f, 90f, 75f) : Quaternion.Euler(6f, 0f, 0f);
        Tween.LocalPosition(t, alvo, 0.4f, Ease.OutCubic);
        Tween.LocalRotation(t, giro, 0.4f, Ease.OutCubic);
        SomManager.TocarEm("mexer", t.position, 0.7f, 0.8f);
        GameUI.Instance?.Toast($"Nas mãos: <b>{c.Nome}</b>. E no porta-malas aberto guarda; E sem mirar nada larga no chão.", UIFactory.CorSuave, 3f);
    }

    // Sai das mãos sem ir para lugar nenhum (o porta-malas ou a sala do cliente cuidam do resto)
    public static CargaFisica Soltar()
    {
        var c = NaMao;
        NaMao = null;
        quadroMudou = Time.frameCount;
        if (c != null) { c.transform.SetParent(null, true); Tween.StopAll(c.transform); }
        return c;
    }

    // E sem nada mirado: larga no chão na frente do técnico (no chão de verdade, por raio)
    public static void Largar()
    {
        var c = NaMao;
        var cam = Camera.main;
        if (c == null || cam == null) return;
        Soltar();

        // Impressora de locação largada dentro da sala do cliente: entregue no lugar da impressora
        if (c.item.tipo == TipoCarga.ImpressoraLocacao && EntregarImpressora(c)) return;
        // Encomenda: volta a ser a caixa que se abre com E (e se pega de novo com G)
        if (c.item.tipo == TipoCarga.Encomenda && c.entrega != null)
        {
            Vector3 chao = PontoNoChao(cam.transform, 0.9f);
            EncomendaFisica.CriarNoChao(c.entrega, chao, Quaternion.Euler(0f, cam.transform.eulerAngles.y + 180f, 0f));
            Destroy(c.gameObject);
            return;
        }
        Pousar(c, PontoNoChao(cam.transform, c.item.tipo == TipoCarga.Escada ? 1.3f : 0.9f), Quaternion.Euler(0f, cam.transform.eulerAngles.y + (c.item.tipo == TipoCarga.Escada ? 90f : 180f), 0f));
    }

    public static void Pousar(CargaFisica c, Vector3 pos, Quaternion rot)
    {
        var t = c.transform;
        t.SetParent(null, true);
        Tween.Position(t, pos, 0.35f, Ease.OutCubic);
        Tween.Rotation(t, rot, 0.35f, Ease.OutCubic);
        if (c.modelo != null && c.modelo.localScale != c.escalaAberta) Tween.Scale(c.modelo, c.escalaAberta, 0.45f, Ease.OutBack);   // a escada abre no chão
        foreach (var col in c.GetComponentsInChildren<Collider>()) col.enabled = true;
        c.noPortaMalas = false;
        c.enabled = true;
        SomManager.TocarEm("mexer", pos, 0.7f, 0.7f);
    }

    static Vector3 PontoNoChao(Transform cam, float distancia)
    {
        Vector3 frente = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 de = cam.position + frente * distancia;
        if (Physics.Raycast(de, Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
        return new Vector3(de.x, cam.position.y - 1.6f, de.z);
    }

    // ---------- Impressora de locação (chamado de instalação) ----------

    static CargaFisica caixaImpressora;
    static Chamado chamadoDaCaixa;
    public static Vector3 PontoCaixaEscritorio => Bairro.CentroEscritorio + new Vector3(-2.1f, 0f, -2.1f);

    // Aceitou a instalação andando (sem atalho de teste): a caixa da impressora está no escritório e o técnico leva
    public static void PrepararInstalacao(Chamado ch)
    {
        if (ch == null || ch.estado == null || ch.servicoLocacao != ServicoLocacao.Instalacao) return;
        if (MissionManager.TeleporteDeTeste || CarroTecnico.Instance == null) return;
        ch.estado.impNoEscritorio = true;
        chamadoDaCaixa = ch;
        if (caixaImpressora != null) Destroy(caixaImpressora.gameObject);
        caixaImpressora = CriarCaixa(TipoCarga.ImpressoraLocacao, "Impressora de locação (na caixa)", new Vector3(0.52f, 0.46f, 0.5f),
            "LOCAÇÃO\nImpressora laser\nPatrimônio da empresa\n" + ch.nomeCliente);
        caixaImpressora.item.id = ch.contratoId;
        caixaImpressora.transform.SetPositionAndRotation(PontoCaixaEscritorio, Quaternion.Euler(0f, 15f, 0f));
        Debug.Log($"[Carga] Instalação aceita: a caixa da impressora está no escritório ({PontoCaixaEscritorio}). Leve até {ch.nomeCliente}.");
    }

    static bool EntregarImpressora(CargaFisica c)
    {
        var mm = MissionManager.Instance;
        var ch = mm != null ? mm.chamadoAtivo : null;
        if (ch == null || ch != chamadoDaCaixa || ch.estado == null) return false;
        var sala = SalaCliente.Para(ch.nomeCliente);
        if (sala == null) return false;
        var cam = Camera.main.transform.position;
        if (Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(sala.Centro.x, sala.Centro.z)) > 6f) return false;
        ch.estado.impNoEscritorio = false;
        var ancora = sala.Ancora(TipoEquipamento.Impressora);
        Vector3 destino = ancora != null ? ancora.position : sala.Centro;
        Tween.Position(c.transform, destino, 0.4f, Ease.InOutQuad);
        Tween.Scale(c.transform, Vector3.zero, 0.25f, Ease.InBack, startDelay: 0.35f).OnComplete(c, x => Destroy(x.gameObject));
        SomManager.TocarEm("mexer", destino, 0.8f, 0.7f);
        GameUI.Instance?.Toast("Caixa da impressora entregue no lugar dela. Agora é abrir com o estilete e instalar.", UIFactory.Sucesso, 4f);
        Debug.Log($"[Carga] Impressora de locação entregue na sala de {ch.nomeCliente}.");
        caixaImpressora = null;
        return true;
    }

    // Abandonou ou concluiu: a caixa (se ainda não foi entregue) some (a impressora volta para o estoque)
    public static void LimparInstalacao()
    {
        if (caixaImpressora != null)
        {
            if (NaMao == caixaImpressora) Soltar();
            PortaMalas.Instance?.Remover(caixaImpressora);
            Destroy(caixaImpressora.gameObject);
        }
        caixaImpressora = null;
        chamadoDaCaixa = null;
    }

    // ---------- Fábrica ----------

    // Caixa de papelão (o mesmo prefab das encomendas) com etiqueta
    public static CargaFisica CriarCaixa(TipoCarga tipo, string rotulo, Vector3 tam, string etiqueta)
    {
        var go = new GameObject("Carga_" + tipo);
        EncomendaFisica.CriarCaixa(go.transform, tam);
        if (!string.IsNullOrEmpty(etiqueta)) EncomendaFisica.Etiqueta(go.transform, tam, etiqueta, false);
        var c = go.AddComponent<CargaFisica>();
        c.item = new ItemCarga { tipo = tipo, rotulo = rotulo };
        c.tamanho = tam;
        c.distanciaInteracao = 1.8f;
        return c;
    }

    // Galão de gasolina de 5 L (vermelho, com alça e bico)
    public static CargaFisica CriarGalao()
    {
        var go = new GameObject("Carga_Galao");
        var vermelho = new Material(Shader.Find("Standard")) { color = new Color(0.72f, 0.08f, 0.06f) };
        vermelho.SetFloat("_Glossiness", 0.55f);
        var preto = new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.05f, 0.05f) };
        void Peca(PrimitiveType p, Vector3 pos, Vector3 esc, Material m, Vector3 giro = default)
        {
            var g = GameObject.CreatePrimitive(p);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(go.transform, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            g.transform.localRotation = Quaternion.Euler(giro);
            g.GetComponent<Renderer>().sharedMaterial = m;
        }
        Peca(PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.26f, 0.3f, 0.14f), vermelho);
        Peca(PrimitiveType.Cube, new Vector3(-0.03f, 0.33f, 0f), new Vector3(0.14f, 0.03f, 0.04f), vermelho);   // alça
        Peca(PrimitiveType.Cylinder, new Vector3(0.1f, 0.33f, 0f), new Vector3(0.05f, 0.04f, 0.05f), preto);      // tampa
        Peca(PrimitiveType.Cylinder, new Vector3(0.15f, 0.36f, 0f), new Vector3(0.025f, 0.06f, 0.025f), preto, new Vector3(0f, 0f, -50f));   // bico
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.18f, 0f);
        bc.size = new Vector3(0.28f, 0.36f, 0.16f);
        var c = go.AddComponent<CargaFisica>();
        c.item = new ItemCarga { tipo = TipoCarga.Galao, rotulo = "Galão com 5 L de gasolina" };
        c.tamanho = new Vector3(0.28f, 0.36f, 0.16f);
        c.distanciaInteracao = 1.6f;
        return c;
    }

    // ---------- Eventos ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        NaMao = null;
        caixaImpressora = null;
        chamadoDaCaixa = null;
        if (GameObject.Find("Cenario/Rua") == null) return;   // cena sem o bairro
        new GameObject("MaosDoTecnico").AddComponent<MaosDoTecnico>();
    }
}

// Cuida do que está nas mãos: E sem nada mirado larga; G pega a encomenda da porta; chamado aceito prepara a caixa
// da impressora de locação; chamado encerrado limpa.
public class MaosDoTecnico : MonoBehaviour
{
    EventBinding<ChamadoAceitoEvent> bAceito;
    Chamado ultimo;

    void OnEnable()
    {
        bAceito = new EventBinding<ChamadoAceitoEvent>(e => { ultimo = e.chamado; CargaFisica.PrepararInstalacao(e.chamado); });
        EventBus<ChamadoAceitoEvent>.Register(bAceito);
    }

    void OnDisable() => EventBus<ChamadoAceitoEvent>.Deregister(bAceito);

    void Update()
    {
        // O chamado da instalação acabou (concluído ou abandonado): a caixa que sobrou volta para o estoque
        var mm = MissionManager.Instance;
        if (ultimo != null && (mm == null || mm.chamadoAtivo != ultimo)) { ultimo = null; CargaFisica.LimparInstalacao(); }

        if (CarroTecnico.NoCarro) return;
        if (GameUI.Instance != null && GameUI.Instance.PainelAberto) return;
        if (CargaFisica.MudouNesteQuadro) return;

        // G: pegar a caixa da encomenda (E abre)
        if (Input.GetKeyDown(KeyCode.G) && !CargaFisica.MaosOcupadas)
        {
            var alvo = Interactable.MaisProximoEmAlcance();
            if (alvo is EncomendaFisica enc) enc.PegarCaixa();
            else if (alvo is UsadoFisico u && u.e != null && u.e.local == LocalUsado.Caixa) UsadosFisicos.Instance?.PegarCaixa(u.e);
        }

        if (CargaFisica.NaMao != null && Input.GetKeyDown(KeyCode.E)
            && Interactable.MaisProximoEmAlcance() == null && PecaEquipamento.Mirada() == null)
            CargaFisica.Largar();
    }
}
