using System.Collections.Generic;
using UnityEngine;

// Uma parte do equipamento que o jogador pode mirar e usar com E (tampa, cartucho, visor, cabo atrás...).
// O colisor é um gatilho (não atrapalha o andar); as ações vêm de AcoesFisicas, filtradas pelo 'id'.
public class PecaEquipamento : MonoBehaviour
{
    public Equipamento equipamento;
    public string id;
    public string nome;

    public const float Alcance = 2.2f;

    static Transform camJogador;
    static readonly RaycastHit[] hits = new RaycastHit[16];

    // A peça no centro da tela (ou null). Paredes e móveis na frente bloqueiam; o colisor
    // "inteiro" do equipamento não bloqueia as peças dele.
    public static PecaEquipamento Mirada()
    {
        if (camJogador == null)
        {
            var jogador = GameObject.FindWithTag("Player");
            var cam = jogador != null ? jogador.GetComponentInChildren<Camera>() : Camera.main;
            if (cam == null) return null;
            camJogador = cam.transform;
        }

        var raio = new Ray(camJogador.position, camJogador.forward);
        int n = Physics.RaycastNonAlloc(raio, hits, Alcance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, 0, n, Comparador);

        for (int i = 0; i < n; i++)
        {
            var col = hits[i].collider;
            if (col == null || col.CompareTag("Player") || col is CharacterController) continue;
            var peca = col.GetComponent<PecaEquipamento>();
            if (peca != null) return peca.isActiveAndEnabled ? peca : null;
            if (col.isTrigger) continue;
            if (col.GetComponentInParent<Equipamento>() != null) continue;
            return null;   // parede, móvel etc. na frente
        }
        return null;
    }

    static readonly IComparer<RaycastHit> Comparador =
        Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    public static PecaEquipamento Criar(Transform pai, Equipamento eq, string id, string nome, Vector3 centro, Vector3 tamanho)
    {
        var go = new GameObject("Peca_" + id);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = centro;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = tamanho;
        var p = go.AddComponent<PecaEquipamento>();
        p.equipamento = eq;
        p.id = id;
        p.nome = nome;
        return p;
    }
}
