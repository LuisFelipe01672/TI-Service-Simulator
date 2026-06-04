using UnityEngine;
using System.Collections;

public enum NivelFuncionario { Junior, Pleno, Senior }

[System.Serializable]
public class Funcionario
{
    public string nome;
    public NivelFuncionario nivel;
    public float salarioMensal;
    public bool ocupado;

    public Funcionario(string nome, NivelFuncionario nivel)
    {
        this.nome = nome;
        this.nivel = nivel;
        this.salarioMensal = nivel == NivelFuncionario.Junior ? 1500f :
                             nivel == NivelFuncionario.Pleno  ? 2800f : 4500f;
        this.ocupado = false;
    }

    // Chance de resolver sem erro (0 a 1)
    public float ChanceAcerto()
    {
        return nivel == NivelFuncionario.Junior ? 0.5f :
               nivel == NivelFuncionario.Pleno  ? 0.8f : 0.95f;
    }

    // Tempo em segundos para resolver conforme dificuldade
    public float TempoResolucao(DificuldadeChamado dificuldade)
    {
        float base_ = nivel == NivelFuncionario.Junior ? 120f :
                      nivel == NivelFuncionario.Pleno  ? 70f  : 40f;

        float multiplicador = dificuldade == DificuldadeChamado.Facil  ? 1f :
                              dificuldade == DificuldadeChamado.Medio  ? 1.5f : 2.5f;

        return base_ * multiplicador;
    }

    public string NivelTexto()
    {
        return nivel == NivelFuncionario.Junior ? "Júnior" :
               nivel == NivelFuncionario.Pleno  ? "Pleno"  : "Sênior";
    }
}