using UnityEngine;

// Etapa 9D: carro dos vizinhos circulando pelo bairro. Sem física: anda pela rota (RotasTransito.Caminho) controlando
// só a velocidade — acelera, freia antes de quem está na frente (outro carro, pedestre, o técnico a pé ou o carro
// dele), para na faixa de pedestre ocupada, no PARE e no sinal vermelho, e espera a vez no cruzamento.
// Carro do técnico parado junto ao meio-fio: depois de um tempo, desvia por fora (se a contramão estiver livre).
// O modelo (mesmo hatch do técnico, outras cores) é um molde desligado na cena, montado pelo MontarCenario.
public class CarroNPC : MonoBehaviour
{
    // Montados no editor (molde)
    public Transform[] rodas;
    public float raioRoda = 0.3f;
    public Renderer carroceria;
    public int indicePintura = -1, indiceFarol = -1, indiceFreio = -1;
    public Material[] pinturas;
    public Material farolAceso, farolApagado, freioAceso, freioApagado, lanterna;
    public Light farol;
    public TMPro.TMP_Text[] placas;

    // Estado
    public RotasTransito.Caminho caminho;
    public float s, v;
    public float lateral;          // desvio da faixa (negativo = para o meio da rua)
    public int id;
    public bool chegouNoFim;
    public float tempoNoFim, tempoBloqueado;
    public int cruzamentoReservado = -1;
    public float sLiberaCruzamento;
    public bool parouNoPare;
    public string motivoParada = "";
    public float distLivre;        // até o próximo obstáculo/parada (debug e testes)
    public CarroNPC bloqueador;    // carro da frente que me fez parar (para achar travamento mútuo)
    bool farolLigado, freioLigado;
    float desvioRestante, ignorarCarrosAte, proximaBuzina;
    Material[] mats;

    // Desvio do carro parado no meio-fio: o meio do carro fica 30 cm além do eixo da rua (passa rente aos dois)
    public const float DesvioLateral = RotasTransito.DeslocFaixa - 0.3f;

    public Vector3 Frente => transform.forward;
    public bool Parado => v < 0.3f;

    public void Iniciar(RotasTransito.Caminho c, float s0, int novoId)
    {
        caminho = c; s = s0; id = novoId;
        v = RotasTransito.VelCidade * 0.6f;
        lateral = 0f; desvioRestante = 0f; chegouNoFim = false; tempoNoFim = 0f; tempoBloqueado = 0f;
        cruzamentoReservado = -1; parouNoPare = false;
        mats = carroceria != null ? carroceria.sharedMaterials : null;
        if (mats != null && indicePintura >= 0 && pinturas != null && pinturas.Length > 0)
            mats[indicePintura] = pinturas[Random.Range(0, pinturas.Length)];
        farolLigado = true; freioLigado = true;   // força aplicar os materiais (carro reaproveitado)
        AplicarLuzes(false, false);
        if (placas != null)
        {
            string placa = PlacaAleatoria();
            foreach (var t in placas) if (t != null) t.text = placa;
        }
        Posicionar();
    }

    // Placa Mercosul fictícia: 3 letras, 1 número, 1 letra, 2 números
    public static string PlacaAleatoria()
    {
        const string L = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char Le() => L[Random.Range(0, L.Length)];
        char Nu() => (char)('0' + Random.Range(0, 10));
        return $"{Le()}{Le()}{Le()}{Nu()}{Le()}{Nu()}{Nu()}";
    }

    public void Posicionar()
    {
        Vector3 d = caminho.Direcao(s);
        transform.SetPositionAndRotation(caminho.Posicao(s) + RotasTransito.Direita(d) * lateral, Quaternion.LookRotation(d));
    }

    // Um passo da direção (chamado pelo VidaNaRua, que conhece os outros carros, os pedestres e o jogador)
    public void Passo(float dt, VidaNaRua rua)
    {
        if (caminho == null) return;
        float alvo = RotasTransito.VelCidade;
        motivoParada = "";

        // 1) Curvas à frente (velocidade máxima de cada trecho, com distância para frear)
        const float desacel = 3.5f;
        for (float k = 0f; k <= 24f; k += 2f)
        {
            float lim = caminho.Limite(s + k);
            alvo = Mathf.Min(alvo, Mathf.Sqrt(lim * lim + 2f * desacel * k));
        }

        // 2) Paradas: faixa ocupada, cruzamento (semáforo, PARE e a vez), fim da rota
        float dParada = float.MaxValue;
        foreach (var p in caminho.paradas)
        {
            float d = p.s - s;
            if (d < -0.5f || d > 35f) continue;
            if (p.tipo == RotasTransito.TipoParada.Faixa)
            {
                if (rua.FaixaOcupada(p.indice) && d > v * v / (2f * 7f) - 0.3f) { dParada = Mathf.Min(dParada, d); motivoParada = "faixa"; }
                continue;
            }
            if (cruzamentoReservado == p.indice) continue;
            if (cruzamentoReservado >= 0) break;   // ainda dentro do anterior
            if (!PodeEntrar(p, d, rua)) { dParada = Mathf.Min(dParada, Mathf.Max(0f, d)); if (motivoParada == "") motivoParada = "cruzamento"; break; }
            if (d < 6f || d < v * v / (2f * desacel) + 2f)
            {
                rua.Reservar(this, p);
                cruzamentoReservado = p.indice;
                sLiberaCruzamento = p.sSaida;
            }
            break;   // só o próximo cruzamento interessa
        }
        if (cruzamentoReservado >= 0 && s > sLiberaCruzamento) { rua.Liberar(this); cruzamentoReservado = -1; parouNoPare = false; }
        float dFim = caminho.Comprimento - s;
        if (dFim < dParada) dParada = dFim;

        // 3) Obstáculos no caminho (amostras a cada metro até 18 m)
        bool ignorarCarros = Time.time < ignorarCarrosAte;
        float dObst = rua.LivreAte(this, 18f, ignorarCarros, out var quem);
        float folga = quem == VidaNaRua.TipoObstaculo.Carro || quem == VidaNaRua.TipoObstaculo.CarroJogador ? 1.6f : quem == VidaNaRua.TipoObstaculo.Jogador ? 3f : 2.5f;
        float dO = dObst - folga;
        if (dO < dParada && quem != VidaNaRua.TipoObstaculo.Nenhum) motivoParada = quem.ToString();
        float d0 = Mathf.Min(dParada, dO);
        distLivre = d0;
        alvo = Mathf.Min(alvo, Mathf.Sqrt(2f * desacel * Mathf.Max(0f, d0)));
        if (d0 < 0.05f) alvo = 0f;

        // 4) Desvio do carro do técnico parado no meio-fio (passa por fora quando a contramão está livre)
        if (quem == VidaNaRua.TipoObstaculo.CarroJogador && v < 0.5f && dObst < 8f) tempoBloqueado += dt;
        else if (quem == VidaNaRua.TipoObstaculo.Jogador && v < 0.5f && dObst < 8f) { tempoBloqueado += dt; if (tempoBloqueado > 4f && Time.time > proximaBuzina) { proximaBuzina = Time.time + 9f; SomManager.TocarEm("buzina", transform.position, 0.7f); } }
        else if (quem == VidaNaRua.TipoObstaculo.Carro && v < 0.3f) { tempoBloqueado += dt; if (tempoBloqueado > 10f && rua.Travados(this)) { ignorarCarrosAte = Time.time + 2.5f; tempoBloqueado = 0f; } }
        else tempoBloqueado = 0f;
        if (quem == VidaNaRua.TipoObstaculo.CarroJogador && tempoBloqueado > 1.5f && desvioRestante <= 0f && rua.PodeDesviar(this))
        {
            desvioRestante = 16f;
            tempoBloqueado = 0f;
        }
        float lateralAlvo = desvioRestante > 0f ? -DesvioLateral : 0f;
        if (desvioRestante > 0f) alvo = Mathf.Min(alvo, 3.5f);

        // 5) Integra
        float acel = alvo > v ? 2f : -7f;
        v = acel > 0 ? Mathf.Min(alvo, v + acel * dt) : Mathf.Max(alvo, v + acel * dt);
        if (v < 0f) v = 0f;
        float ds = v * dt;
        s += ds;
        if (desvioRestante > 0f) desvioRestante -= ds;
        lateral = Mathf.MoveTowards(lateral, lateralAlvo, (0.6f + 0.25f * v) * dt);
        Posicionar();
        if (rodas != null)
        {
            float giro = ds / Mathf.Max(0.1f, raioRoda) * Mathf.Rad2Deg;
            foreach (var r in rodas) if (r != null) r.Rotate(giro, 0f, 0f, Space.Self);
        }
        chegouNoFim = s >= caminho.Comprimento - 0.3f;
        tempoNoFim = chegouNoFim ? tempoNoFim + dt : 0f;

        AplicarLuzes(rua.Noite, alvo < v - 0.2f || (v < 0.2f && d0 < 3f));
    }

    // Pode passar da linha de parada deste cruzamento agora?
    bool PodeEntrar(RotasTransito.Parada p, float d, VidaNaRua rua)
    {
        if (rua.Malha.cruzamentos[p.indice].controle == ControleCruzamento.Semaforo)
        {
            var cor = RotasTransito.CorSemaforo(rua.Malha, p.indice, p.preferencial, VidaNaRua.TempoSemaforo);
            if (cor == RotasTransito.Cor.Vermelho) { if (d > 0.2f || v < 2f) return false; }
            else if (cor == RotasTransito.Cor.Amarelo && d > v * v / (2f * 3f) + 0.5f) return false;
        }
        else if (!p.preferencial)
        {
            // PARE: para de vez na linha, depois espera brecha na rua principal
            if (!parouNoPare)
            {
                if (d < 0.6f && v < 0.3f) parouNoPare = true;
                else return false;
            }
            if (rua.PreferencialChegando(p.indice, 18f)) return false;
        }
        return rua.CruzamentoLivrePara(this, p);
    }

    void AplicarLuzes(bool noite, bool freio)
    {
        if (mats == null || carroceria == null) return;
        bool mudou = false;
        if (noite != farolLigado)
        {
            farolLigado = noite;
            if (indiceFarol >= 0) mats[indiceFarol] = noite ? farolAceso : farolApagado;
            mudou = true;
        }
        if (freio != freioLigado || mudou)
        {
            freioLigado = freio;
            if (indiceFreio >= 0) mats[indiceFreio] = freio ? freioAceso : noite && lanterna != null ? lanterna : freioApagado;
            mudou = true;
        }
        if (mudou) carroceria.sharedMaterials = mats;
    }

    // Luz de verdade dos faróis: só os carros mais perto da câmera (luz por pixel custa caro)
    public void FarolDeVerdade(bool ligar)
    {
        if (farol != null && farol.enabled != ligar) farol.enabled = ligar;
    }

    public bool FarolLigado => farolLigado;

    // Longe da câmera o carro some (não vale o custo de desenhar um carro de 10 px)
    Renderer[] desenhos;
    bool mostrando = true;
    public void Mostrar(bool sim)
    {
        if (sim == mostrando) return;
        mostrando = sim;
        if (desenhos == null) desenhos = GetComponentsInChildren<Renderer>(true);
        foreach (var r in desenhos) if (r != null) r.enabled = sim;
    }
}
