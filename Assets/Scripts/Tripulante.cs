using UnityEngine;

public class Tripulante : MonoBehaviour
{
    [Header("Identidade do Tripulante")]
    public string nomeDoPersonagem;
    public string profissao; // Ex: Engenheiro, Cozinheiro, Capitão

    [Header("Status de Relacionamento")]
    public int proximidadeComOGrupo = 5; // Escala de 0 a 10 (começa neutro)
    
    [Header("Estados Mentais (Surtos)")]
    public bool estaEmPanico = false;
    public bool estaCatatonico = false;
    public bool estaIrracional = false;

    // Função para alterar a proximidade nas escolhas do roteiro
    public void AlterarProximidade(int valor)
    {
        proximidadeComOGrupo += valor;
        proximidadeComOGrupo = Mathf.Clamp(proximidadeComOGrupo, 0, 10); // Trava entre 0 e 10
    }
}