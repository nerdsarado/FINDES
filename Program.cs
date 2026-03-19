using FINDES;

class Program
{
    static async Task Main(string[] args)
    {
        List<string> cde = new List<string>();
        bool continuar = true;
        Console.Clear();
        Console.WriteLine("Iniciando automação...");

        try
        {
            while (continuar)
            {
                Console.WriteLine("Qual licitação você quer retirar?");
                string licitacao = Console.ReadLine();
                if(!string.IsNullOrEmpty(licitacao) && licitacao.ToLower() != "sair")
                {
                    cde.Add(licitacao);
                }
                else if (licitacao.ToLower() == "sair")
                {
                    continuar = false;
                    break;
                }
                else
                {
                    Console.WriteLine("Entrada inválida. Por favor, tente novamente.");
                }
            }
            await Parallel.ForEachAsync(cde, async (licitacao, cancellationToken) =>
            {
                {
                    Console.WriteLine($"Processando licitação: {licitacao}");
                    await Automação.Navegar(licitacao);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro: {ex.Message}");
        }
    }
}
