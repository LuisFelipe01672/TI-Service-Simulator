using UnityEngine;

// Funcionário do cliente fazendo o seu trabalho (digitando na mesa, conversando sentado, arrumando a prateleira).
// O MontarCenario deixa um ponto com este script no lugar da pessoa (pés no chão, de frente para o trabalho);
// o personagem do Mixamo é criado ao carregar a cena. Sem os personagens no projeto (repositório público), o ponto fica vazio.
public class PessoaTrabalhando : MonoBehaviour
{
    public enum Atividade { Digitando, ConversandoSentado, EmPe }
    public Atividade atividade = Atividade.Digitando;
    public int personagem;   // índice no BancoPersonagens (varia a roupa)

    void Start()
    {
        var anim = BancoPersonagens.Instanciar(transform, personagem);
        if (anim == null) return;
        string estado = atividade == Atividade.Digitando ? BancoPersonagens.Digitando :
                        atividade == Atividade.ConversandoSentado ? BancoPersonagens.ConversandoSentado : BancoPersonagens.Parado;
        anim.Play(estado, 0, Random.value);   // cada um num ponto da animação, para não mexerem juntos
        anim.speed = Random.Range(0.9f, 1.1f);
    }
}
