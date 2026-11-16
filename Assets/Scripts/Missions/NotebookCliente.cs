using UnityEngine;

// Etapa 7: o notebook do cliente é criado em código (não está na cena montada) e fica na mesa do computador,
// ao lado do monitor, do lado oposto ao gabinete, com a tela virada para o mesmo lado da tela do monitor.
// Modelo: primitivas (EquipamentoPecas.Notebook). Candidato CC-BY para trocar: ver ROADMAP (precisa da aprovação do dono).
public static class NotebookCliente
{
    static Equipamento atual;

    public static Equipamento Garantir()
    {
        if (atual != null) return atual;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == TipoEquipamento.Notebook) return atual = e;
        var go = new GameObject("Notebook_Cliente");
        var eq = go.AddComponent<Equipamento>();
        eq.tipoEquipamento = TipoEquipamento.Notebook;
        eq.distanciaInteracao = 2f;
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.012f, 0f);
        bc.size = new Vector3(0.34f, 0.024f, 0.235f);
        go.AddComponent<EquipamentoPecas>();
        return atual = eq;
    }

    // Põe o notebook na mesa do computador desta sala
    public static void Posicionar(SalaCliente sala)
    {
        var nb = Garantir();
        if (sala == null || sala.ancoraComputador == null) return;
        Equipamento pc = null;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == TipoEquipamento.Computador) { pc = e; break; }

        // Para onde a tela do monitor olha (o lado de quem senta)
        Vector3 frente = -sala.ancoraComputador.forward;
        var tela = pc != null ? pc.GetComponentInChildren<TelaComputador>(true) : null;
        if (tela != null) frente = tela.transform.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
        frente.Normalize();
        Vector3 direita = Vector3.Cross(Vector3.up, frente);   // à direita de quem olha a tela de frente

        Vector3 centro = tela != null ? tela.transform.position : sala.ancoraComputador.position;
        // Lado oposto ao gabinete (se a torre estiver em cima da mesa)
        float lado = -1f;
        var gab = pc != null ? FindDeep(pc.transform, "Ancora_Gabinete") : null;
        if (gab != null && Vector3.Dot(gab.position - centro, direita) < 0f) lado = 1f;

        // Procura um lugar livre no tampo: dos lados do monitor, mais perto ou mais longe, sem nada em cima
        // (telefone, porta-lápis, teclado...). Sem lugar livre, fica no primeiro candidato.
        float yMesa = sala.ancoraComputador.position.y;
        var renderers = Object.FindObjectsByType<Renderer>();
        Vector3 alvo = Vector3.zero;
        bool achou = false;
        foreach (float d in new[] { 0.55f, 0.7f, 0.85f, 0.45f })
        {
            foreach (float s in new[] { lado, -lado })
                foreach (float f in new[] { 0.12f, 0.22f, 0.02f })
                {
                    Vector3 c = centro + direita * (s * d) + frente * f;
                    if (!Tampo(c, nb.transform, yMesa, out float y)) continue;
                    c.y = y;
                    if (alvo == Vector3.zero) alvo = c;
                    if (Ocupado(c, renderers, nb.transform)) continue;
                    alvo = c; achou = true; break;
                }
            if (achou) break;
        }
        if (alvo == Vector3.zero) alvo = new Vector3(centro.x, yMesa, centro.z) + direita * (lado * 0.55f) + frente * 0.12f;
        if (!achou) Debug.LogWarning("[Salas] Notebook: nenhum lugar livre na mesa; ficou ao lado do monitor.");
        // O Visual do notebook tem a frente (lado do teclado) em -z: -z do notebook = para onde a tela olha
        nb.transform.SetPositionAndRotation(alvo, Quaternion.LookRotation(-frente, Vector3.up));
        Physics.SyncTransforms();
    }

    // Tem tampo de mesa embaixo deste ponto, na altura da mesa do computador?
    static bool Tampo(Vector3 p, Transform ignorar, float yMesa, out float y)
    {
        y = yMesa;
        float melhor = float.MaxValue;
        bool ok = false;
        foreach (var h in Physics.RaycastAll(p + Vector3.up * 0.6f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(ignorar) || h.collider.CompareTag("Player")) continue;
            if (h.distance < melhor) { melhor = h.distance; y = h.point.y; ok = true; }
        }
        return ok && Mathf.Abs(y - yMesa) < 0.06f;
    }

    // Algo em cima do tampo dentro da área do notebook (com o carregador ao lado)?
    static bool Ocupado(Vector3 p, Renderer[] renderers, Transform ignorar)
    {
        var area = new Bounds(p + Vector3.up * 0.1f, new Vector3(0.44f, 0.18f, 0.44f));
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled || r.transform.IsChildOf(ignorar)) continue;
            var b = r.bounds;
            if (b.max.y < p.y + 0.012f) continue;                              // o próprio tampo, papéis
            if (b.size.x > 1.4f || b.size.z > 1.4f) continue;                  // mesa, parede, chão
            if (b.Intersects(area)) return true;
        }
        return false;
    }

    static Transform FindDeep(Transform t, string nome)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == nome) return c;
        return null;
    }
}
