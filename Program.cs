using FINDES;
using System.Text;

namespace Findes
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Log();
        }
        static async Task Iniciar()
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
                    if (!string.IsNullOrEmpty(licitacao) && licitacao.ToLower() != "sair")
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
        static async Task Log()
        {

            using var logger = new ConsoleFileLogger(@"\\SERVIDOR2\Publico\ALLAN\Logs");

            Console.WriteLine("=== INICIANDO APLICAÇÃO ===");
            Console.WriteLine($"Data: {DateTime.Now:F}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Chamando Iniciar...");
                await Iniciar();

                Console.WriteLine("Processamento concluído com sucesso!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"!!! ERRO CAPTURADO !!!");
                Console.Error.WriteLine($"Mensagem: {ex.Message}");
                Console.Error.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("=== APLICAÇÃO FINALIZADA ===");
        }
    }
    public class ConsoleFileLogger : IDisposable
    {
        private readonly string _logDirectory;
        private readonly StreamWriter _fileWriter;
        private readonly TextWriter _originalOutput;
        private readonly TextWriter _originalError;
        private readonly MultiTextWriter _multiOutput;
        private readonly MultiTextWriter _multiError;

        public ConsoleFileLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(logDirectory);

            // Salva os escritores originais
            _originalOutput = Console.Out;
            _originalError = Console.Error;

            // Cria o arquivo de log com data no nome
            var logFile = Path.Combine(logDirectory, $"mercadoeletronico.txt");

            // StreamWriter com AutoFlush = true para escrever IMEDIATAMENTE
            _fileWriter = new StreamWriter(logFile, append: true)
            {
                AutoFlush = true  // <--- ESSENCIAL para escrever continuamente
            };

            // Escreve cabeçalho no início do log
            _fileWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] === SESSÃO INICIADA ===");

            // Cria escritores que escrevem tanto no console quanto no arquivo
            _multiOutput = new MultiTextWriter(_originalOutput, _fileWriter);
            _multiError = new MultiTextWriter(_originalError, _fileWriter);

            // Redireciona o console
            Console.SetOut(_multiOutput);
            Console.SetError(_multiError);
        }

        public void Dispose()
        {
            // Escreve rodapé no final do log
            _fileWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] === SESSÃO FINALIZADA ===");
            _fileWriter.WriteLine();

            // Restaura o console original
            Console.SetOut(_originalOutput);
            Console.SetError(_originalError);
            _fileWriter?.Dispose();
        }
    }

    public class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] _writers;

        public MultiTextWriter(params TextWriter[] writers)
        {
            _writers = writers;
        }

        public override void Write(char value)
        {
            foreach (var writer in _writers)
            {
                writer.Write(value);
            }
        }

        public override void Write(string value)
        {
            foreach (var writer in _writers)
            {
                writer.Write(value);
            }
        }

        public override void WriteLine(string value)
        {
            foreach (var writer in _writers)
            {
                writer.WriteLine(value);
            }
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}

