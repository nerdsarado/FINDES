using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINDES
{
    public class Locaweb
    {
        private static readonly string email = "vendas2@venturainformatica.com.br";
        private static readonly string senha = "Ven1664*#";
        public const bool Headless = Automação.Headless; // Defina como true para rodar sem interface gráfica, ou false para ver o navegador em ação
        public static async Task EnviarEmail()
        {
            IPlaywright playwright = null;
            IBrowser browser = null;
            IPage paginaPrincipal;
            string diretorioDownloads = @"\\SERVIDOR2\Publico\ALLAN\FINDES";
            

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = Headless
            });
            var context = await browser.NewContextAsync();
            paginaPrincipal = await context.NewPageAsync();

            try
            {
                Console.WriteLine("Navegando para o Locaweb...");
                await paginaPrincipal.GotoAsync("https://webmail-seguro.com.br/?_task=logout&_token=PuO5zPUa8OfJmHPnnbbEUP1o5ncjMCJq");

                var login = paginaPrincipal.Locator("#rcmloginuser").First;
                if (login != null)
                {
                    Console.WriteLine("Preenchendo o campo de login...");
                    await login.FillAsync(email);

                    var senhaInput = paginaPrincipal.Locator("#rcmloginpwd").First;
                    if (senhaInput != null)
                    {
                        {
                            Console.WriteLine("Preenchendo o campo de senha...");
                            await senhaInput.FillAsync("");
                            foreach (char c in senha)
                            {
                                await senhaInput.PressAsync(c.ToString());
                                await Task.Delay(400); // Pequena pausa para simular a digitação humana
                            }

                            Console.WriteLine("Senha preenchida. Verificando se há captcha...");
                            await ResolverCaptchaLocaweb(paginaPrincipal);

                            var submitButton = paginaPrincipal.Locator("#submitloginform");
                                if (submitButton != null)
                                {
                                    Console.WriteLine("Clicando no botão de login...");
                                    await submitButton.ClickAsync();
                                    // Aguarde a navegação para a caixa de entrada
                                    await paginaPrincipal.WaitForURLAsync("https://webmail-seguro.com.br/?_task=mail&_mbox=INBOX");
                                    Console.WriteLine("Login bem-sucedido! Navegando para a caixa de entrada...");
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro: {ex.Message}");
            }
            finally
            {
                if (browser != null)
                {
                    await browser.CloseAsync();
                }
            }
        }
        public static async Task ResolverCaptchaLocaweb(IPage paginaPrincipal)
        {
            try
            {
                Console.WriteLine("Verificando se há captcha no Locaweb...");

                // Aguarda um pouco para a página estabilizar
                await Task.Delay(5000);

                // Obtém todos os frames disponíveis
                var frames = paginaPrincipal.Frames;
                Console.WriteLine($"Total de frames: {frames.Count}");

                // Primeiro, verifica o frame principal
                await VerificarFramePrincipal(paginaPrincipal);

                // Depois verifica os frames filhos
                foreach (var frame in frames.Skip(1)) // Pula o frame principal
                {
                    Console.WriteLine($"Verificando frame filho: {frame.Url}");

                    // Não usa WaitForLoadStateAsync para evitar timeout
                    await Task.Delay(2000); // Aguarda 2 segundos

                    // Procura por qualquer checkbox no frame
                    var checkboxes = await frame.Locator("input[type='checkbox']").AllAsync();

                    if (checkboxes.Any())
                    {
                        Console.WriteLine($"Checkbox encontrado no frame filho!");

                        foreach (var checkbox in checkboxes)
                        {
                            await TentarClicarCheckbox(checkbox, frame, paginaPrincipal);
                        }
                        return;
                    }
                }

                // Se não encontrar, tenta seletores específicos do Locaweb
                await TentarSeletoresEspecificos(paginaPrincipal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar captcha: {ex.Message}");
            }
        }

        private static async Task VerificarFramePrincipal(IPage paginaPrincipal)
        {
            Console.WriteLine("Verificando frame principal...");

            // Procura por checkbox no frame principal
            var checkboxesPrincipal = await paginaPrincipal.Locator("input[type='checkbox']").AllAsync();

            if (checkboxesPrincipal.Any())
            {
                Console.WriteLine($"Checkbox encontrado no frame principal!");

                foreach (var checkbox in checkboxesPrincipal)
                {
                    var visivel = await checkbox.IsVisibleAsync();
                    Console.WriteLine($"Checkbox visível no principal: {visivel}");

                    if (visivel)
                    {
                        await checkbox.ClickAsync();
                        Console.WriteLine("Clique realizado no frame principal!");
                        return;
                    }
                }
            }

            // Procura por elementos que possam conter captcha
            var possiveisCaptchas = await paginaPrincipal.Locator("[class*='captcha'], [id*='captcha'], .g-recaptcha, iframe[src*='recaptcha']").AllAsync();
            Console.WriteLine($"Possíveis elementos de captcha no principal: {possiveisCaptchas.Count}");
        }

        private static async Task TentarClicarCheckbox(ILocator checkbox, IFrame frame, IPage paginaPrincipal)
        {
            try
            {
                // Tenta verificar se é um reCAPTCHA
                var isRecaptcha = await frame.Locator(".recaptcha-checkbox, iframe[title*='recaptcha']").CountAsync() > 0;

                if (isRecaptcha)
                {
                    Console.WriteLine("Detectado reCAPTCHA, tentando método específico...");
                    await ClicarReCaptcha(frame);
                    return;
                }

                // Tenta diferentes métodos de clique
                Console.WriteLine("Tentando clicar no checkbox...");

                // Método 1: Clique direto
                try
                {
                    await checkbox.ClickAsync(new LocatorClickOptions
                    {
                        Timeout = 5000,
                        Force = true
                    });
                    Console.WriteLine("Clique direto realizado!");
                    return;
                }
                catch { }

                // Método 2: JavaScript
                try
                {
                    await checkbox.EvaluateAsync("el => el.click()");
                    Console.WriteLine("Clique via JavaScript realizado!");
                    return;
                }
                catch { }

                // Método 3: Simular clique do mouse
                try
                {
                    var box = await checkbox.BoundingBoxAsync();
                    if (box != null)
                    {
                        await paginaPrincipal.Mouse.ClickAsync(box.X + 5, box.Y + 5);
                        Console.WriteLine("Clique por coordenadas realizado!");
                        return;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao tentar clicar: {ex.Message}");
            }
        }

        private static async Task ClicarReCaptcha(IFrame frame)
        {
            // Seletores comuns do reCAPTCHA
            var seletores = new[]
            {
        ".recaptcha-checkbox-border",
        "#recaptcha-anchor",
        ".rc-anchor-content",
        "iframe[src*='recaptcha']"
    };

            foreach (var seletor in seletores)
            {
                try
                {
                    var elemento = frame.Locator(seletor).First;
                    if (await elemento.CountAsync() > 0)
                    {
                        Console.WriteLine($"Tentando clicar no reCAPTCHA com seletor: {seletor}");
                        await elemento.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                        Console.WriteLine("Clique no reCAPTCHA realizado!");
                        return;
                    }
                }
                catch { }
            }
        }

        private static async Task TentarSeletoresEspecificos(IPage paginaPrincipal)
        {
            Console.WriteLine("Tentando seletores específicos do Locaweb...");

            var seletoresEspecificos = new[]
            {
        "input[name='stayconnected']",
        "input[id='stayconnected']",
        "#stayconnected",
        "label[for='stayconnected']",
        ".checkbox input",
        "div.checkbox label"
    };

            foreach (var seletor in seletoresEspecificos)
            {
                try
                {
                    var elemento = paginaPrincipal.Locator(seletor).First;
                    if (await elemento.CountAsync() > 0)
                    {
                        var visivel = await elemento.IsVisibleAsync();
                        Console.WriteLine($"Elemento encontrado com seletor '{seletor}', visível: {visivel}");

                        if (visivel)
                        {
                            await elemento.ClickAsync();
                            Console.WriteLine("Clique realizado com sucesso!");
                            return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
