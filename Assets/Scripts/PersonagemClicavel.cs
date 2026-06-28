using UnityEngine;

public class PersonagemClicavel : MonoBehaviour
{
    [Header("Configurações Narrativas")]
    public string nomeDoPersonagem; // Ex: Augusto, Helena, Maya, Rafael, Lucas
    [TextArea(3, 5)] public System.Collections.Generic.List<string> falasDosDias; 

    [Header("Componentes Visuais")]
    public Sprite arteDoRetrato; // Retrato/Arte de Close-up se necessário
    public GameObject iconeInterrogacao; // O objeto filho "IndicadorExclamacao"

    [HideInInspector] public string falaAtual; // Armazena a fofoca ativa
    private GerenciadorDeJogo gerenciador;

    private void Start()
    {
        // Encontra o GameManager automaticamente na cena
        gerenciador = FindFirstObjectByType<GerenciadorDeJogo>();
        AtualizarIndicador();
    }

    // Método chamado pelo GameManager quando o dia avança ou inicia
    public void AtualizarFalaDoDia(int dia)
    {
        int indiceDoDia = dia - 1; // Dia 1 = Elemento 0

        if (falasDosDias != null && indiceDoDia >= 0 && indiceDoDia < falasDosDias.Count)
        {
            falaAtual = falasDosDias[indiceDoDia];
        }
        else
        {
            falaAtual = ""; // Sem fala configurada para este dia
        }

        AtualizarIndicador();
    }

    // Liga ou desliga a Exclamação amarela acima da cabeça do boneco
    public void AtualizarIndicador()
    {
        if (iconeInterrogacao != null)
        {
            iconeInterrogacao.SetActive(!string.IsNullOrEmpty(falaAtual));
        }
    }

    // 🆕 MÉTODO PÚBLICO: Chamado diretamente pelo componente de Botão (Button) da UI do Canvas
    public void AoClicarNoPersonagemUI()
    {
        if (gerenciador == null || gerenciador.estadoAtual != GerenciadorDeJogo.EstadoDoJogo.Gameplay) return;
        if (string.IsNullOrEmpty(falaAtual)) return;

        Debug.Log("Botão de UI clicado com sucesso para: " + nomeDoPersonagem);
        
        // Abre o diálogo correspondente no Canvas
        gerenciador.AbrirPainelDialogo(nomeDoPersonagem, falaAtual, arteDoRetrato, this);
    }
}