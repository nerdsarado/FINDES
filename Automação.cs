using Microsoft.Playwright;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FINDES
{
    public class Automação
    {
        public const bool Headless = false;
        private readonly string cnpj = "20.837.281/0001-49";
        private readonly string razãoSocial = "UNIAO COMERCIO DE INFORMATICA EIRELI";
        private readonly string email = "vendas@venturainformatica.com.br";
        private readonly string telefone = "(27)3299-1664";
        private readonly string contato = "ELIZIANE";
        private readonly string endereço = "RUA SETE";
        private readonly string numero = "560";
        private readonly string complemento = "ANDAR 1 E 2";
        private readonly string bairro = "COCAL";
        private readonly string cep = "29.105-770";
        public static async Task Navegar(string numeroLicitacao)
        {
            IPlaywright playwright = null;
            IBrowser browser = null;
            IPage paginaPrincipal;
            string diretorioDownloads = @"\\SERVIDOR2\Publico\ALLAN\FINDES";

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Headless = Headless
            });
            var context = await browser.NewContextAsync();
            paginaPrincipal = await context.NewPageAsync();

            Console.WriteLine("Navegando para o portal de compras da FINDES...");
            await paginaPrincipal.GotoAsync("https://portaldecompras.findes.org.br/Portal/Mural.aspx");

            try
            {
                bool cdeEncontrada = false;
                Console.WriteLine("Aguardando o carregamento da página...");
                Task.Delay(3000).Wait();

                var cdes = await paginaPrincipal.QuerySelectorAllAsync($".areaClique");
                foreach (var cde in cdes) 
                {
                    var texto = await cde.InnerTextAsync();
                    if (texto != null && texto.Contains($"{numeroLicitacao}"))
                    {
                        Console.WriteLine($"Número da CDE encontrada: {texto}");
                        await  cde.ClickAsync();
                        cdeEncontrada = true;
                        continue;
                    }
                }
                if( cdeEncontrada )
                {
                    Console.WriteLine("Extraindo informações da página...");
                    var objeto = await paginaPrincipal.QuerySelectorAsync("#lblObjeto");
                    var material = await objeto.InnerTextAsync();
                    if (objeto != null)
                    {
                        Console.WriteLine($"Objeto da licitação: {material}");

                        var termino = await paginaPrincipal.QuerySelectorAsync("#lblDataTerminoProposta");
                        var propostas = await termino.InnerTextAsync();
                        if (termino != null)
                        {
                            Console.WriteLine($"Data de término: {propostas}");


                            var anexo = paginaPrincipal.Locator("div[onclick*='btBaixaAnexo']").First;
                            if (anexo != null)
                            {
                                Console.WriteLine("Baixando os anexos...");
                                while (!await anexo.IsVisibleAsync())
                                {
                                    await paginaPrincipal.Mouse.WheelAsync(0, 300);
                                    await Task.Delay(200);
                                }
                                await anexo.ClickAsync();

                                var interesse = paginaPrincipal.Locator("#rbParticiparCertameCadastroVisitanteAnexo").First;
                                if (interesse != null)
                                {
                                    Console.WriteLine("Clicando para manifestar interesse...");
                                    await interesse.ClickAsync();

                                    var cnpjSelecao = paginaPrincipal.Locator("#rbCNPJCadastroVisitanteAnexo").First;
                                    if(cnpjSelecao != null)
                                    {
                                        Console.WriteLine("Clicando para informar o CNPJ...");
                                        await cnpjSelecao.ClickAsync();

                                        var cadastro = new Automação();
                                        bool cadastroCompleto = await cadastro.EfetuarCadastro(paginaPrincipal);
                                        if (cadastroCompleto)
                                        {
                                            var baixarArquivo = paginaPrincipal.Locator("#btCadastroVisitanteConfirmarCadastroVisitanteAnexo").First;
                                            if (baixarArquivo != null)
                                            {
                                                Console.WriteLine("Anexo encontrado. Baixando...");
                                                Task.Delay(2000).Wait();
                                                await baixarArquivo.ClickAsync();

                                                // Aguardar o download completar
                                                var download = await paginaPrincipal.WaitForDownloadAsync();

                                                // Salvar no diretório específico com nome original
                                                string caminhoArquivo = Path.Combine(diretorioDownloads, download.SuggestedFilename);
                                                await download.SaveAsAsync(caminhoArquivo);

                                                Console.WriteLine($"Download concluído: {caminhoArquivo}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Não foi possível encontrar o botão para informar o CNPJ.");
                                    }
                                    if (!string.IsNullOrEmpty(numeroLicitacao) &&
                                       !string.IsNullOrEmpty(material) &&
                                       !string.IsNullOrEmpty(propostas))
                                    {
                                        Console.WriteLine($"Bom dia!\r\n\r\nCotação do FINDES retirada.\r\nCDE: {numeroLicitacao}\r\nMATERIAL: {material}\r\nTÉRMINO DAS PROPOSTAS: {propostas}\r\n");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Não foi possível encontrar o botão para manifestar interesse.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Não foi possível encontrar o botão de anexos.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Não foi possível extrair a data de término da licitação.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Não foi possível extrair o objeto da licitação.");
                    }
                }
                else
                {
                    Console.WriteLine($"CDE com número {numeroLicitacao} não encontrada.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (browser != null)
                {
                    Console.WriteLine("Fechando o navegador...");
                    await browser.CloseAsync();
                }

            }

        }
        private async Task<bool> EfetuarCadastro(IPage paginaPrincipal)
        {
            try
            {
                Console.WriteLine("Efetuando cadastro...");
                var cnpjCampo = await paginaPrincipal.QuerySelectorAsync("#tbDocumentoCadastroVisitanteAnexo");
                if (cnpjCampo != null)
                {
                    await cnpjCampo.FillAsync("");
                    Task.Delay(500).Wait();
                    await cnpjCampo.FillAsync(cnpj);
                    var razaoSocialCampo = await paginaPrincipal.QuerySelectorAsync("#tbNomeCadastroVisitanteAnexo");
                    if (razaoSocialCampo != null)
                    {
                        await razaoSocialCampo.FillAsync(razãoSocial);
                        var emailCampo = await paginaPrincipal.QuerySelectorAsync("#tbEmailCadastroVisitanteAnexo");
                        if (emailCampo != null)
                        {
                            await emailCampo.FillAsync(email);
                            var telefoneCampo = await paginaPrincipal.QuerySelectorAsync("#tbTelefoneCadastroVisitanteAnexo");
                            if (telefoneCampo != null)
                            {
                                await telefoneCampo.FillAsync(telefone);
                                var contatoCampo = await paginaPrincipal.QuerySelectorAsync("#tbContatoCadastroVisitanteAnexo");
                                if (contatoCampo != null)
                                {
                                    await contatoCampo.FillAsync(contato);
                                    var endereçoCampo = await paginaPrincipal.QuerySelectorAsync("#tbEnderecoCadastroVisitanteAnexo");
                                    if (endereçoCampo != null)
                                    {
                                        await endereçoCampo.FillAsync(endereço);
                                        var numeroCampo = await paginaPrincipal.QuerySelectorAsync("#tbNumeroCadastroVisitanteAnexo");
                                        if (numeroCampo != null)
                                        {
                                            await numeroCampo.FillAsync(numero);
                                            var complementoCampo = await paginaPrincipal.QuerySelectorAsync("#tbComplementoCadastroVisitanteAnexo");
                                            if (complementoCampo != null)
                                            {
                                                await complementoCampo.FillAsync(complemento);
                                                var bairroCampo = await paginaPrincipal.QuerySelectorAsync("#tbBairroCadastroVisitanteAnexo");
                                                if (bairroCampo != null)
                                                {
                                                    await bairroCampo.FillAsync(bairro);
                                                    var cepCampo = await paginaPrincipal.QuerySelectorAsync("#tbCEPCadastroVisitanteAnexo");
                                                    if (cepCampo != null)
                                                    {
                                                        await cepCampo.FillAsync("");
                                                        Task.Delay(500).Wait();
                                                        await cepCampo.FillAsync(cep);
                                                        bool camposPreenchidos = true; // Variável para verificar se todos os campos foram preenchidos
                                                        if (camposPreenchidos)
                                                        {
                                                            var paisCampo = await paginaPrincipal.QuerySelectorAsync("#ddlPaisCadastroVisitanteAnexo");
                                                            if (paisCampo != null)
                                                            {
                                                                Console.WriteLine("Selecionando o país no formulário de cadastro...");
                                                                await paginaPrincipal.SelectOptionAsync("#ddlPaisCadastroVisitanteAnexo", "BR");

                                                                var estadoCampo = await paginaPrincipal.QuerySelectorAsync("#ddlEstadoCadastroVisitanteAnexo");
                                                                if (estadoCampo != null)
                                                                {
                                                                    Console.WriteLine("Selecionando o estado no formulário de cadastro...");
                                                                    await paginaPrincipal.SelectOptionAsync("#ddlEstadoCadastroVisitanteAnexo", "ES");

                                                                    var cidadeCampo = await paginaPrincipal.QuerySelectorAsync("#ddlCidadeCadastroVisitanteAnexo");
                                                                    if (cidadeCampo != null)
                                                                    {
                                                                        Console.WriteLine("Selecionando a cidade no formulário de cadastro...");
                                                                        await paginaPrincipal.SelectOptionAsync("#ddlCidadeCadastroVisitanteAnexo", "1531");

                                                                        return true; // Cadastro preenchido com sucesso
                                                                    }
                                                                    else
                                                                    {
                                                                        Console.WriteLine("Não foi possível encontrar o campo de cidade.");
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("Não foi possível encontrar o campo de estado.");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Não foi possível encontrar o campo de país.");
                                                            }
                                                            Console.WriteLine("Formulário de cadastro preenchido com sucesso!");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Não foi possível encontrar o campo de CEP.");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Não foi possível encontrar o campo de bairro.");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Não foi possível encontrar o campo de complemento.");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Não foi possível encontrar o campo de número.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Não foi possível encontrar o campo de endereço.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Não foi possível encontrar o campo de contato.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Não foi possível encontrar o campo de telefone.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Não foi possível encontrar o campo de email.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Não foi possível encontrar o campo de razão social.");
                    }
                }
                else
                {
                    Console.WriteLine("Não foi possível encontrar o campo de CNPJ.");
                }
                return false; // Retorna false se algum campo não foi encontrado ou preenchido corretamente
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false; // Retorna false em caso de exceção
            }
        }

    }
    public class Licitacao
    {
        List<string> cde { get; set; }
    }
}
