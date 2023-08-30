using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

string controllersPath = "C:\\Users\\phili\\source\\repos\\InspecaoWebAPI\\InspecaoWebAPI\\Controllers";
string controllersTestPath = "C:\\Users\\phili\\source\\repos\\InspecaoWebAPI\\InspecaoWebAPI.Tests\\Controllers";
string assemblyWebAPIPath = "C:\\Users\\phili\\source\\repos\\InspecaoWebAPI\\InspecaoWebAPI\\bin\\Debug\\net6.0\\InspecaoWebAPI.dll";

Assembly assembly = Assembly.LoadFrom(assemblyWebAPIPath);

////Tratar retorno quando o endpoint retornar no content
GerarTestesDeAcordoComEndpoints();

void GerarTestesDeAcordoComEndpoints()
{
    // Obtém todas as classes de controladores
    var controllerTypes = assembly.GetTypes().Where(t => t.Name.EndsWith("Controller")).ToList();

    foreach (var controller in controllerTypes)
    {
        StringBuilder sbTestes = new StringBuilder();
        StringBuilder sbTestesParameters = new StringBuilder();

        string controllerName = controller.Name.Replace("Controller", "");
        string testName = $"{controllerName}Tests";
        string namespaceForTest = "InspecaoWebAPI.Testes.Controllers." + controllerName + "s"; // Pluralizado com "s" no final
        string testParamsClassName = $"{controllerName}TestParameters";


        // Adiciona o cabeçalho
        sbTestes.AppendLine("using FluentAssertions;");
        sbTestes.AppendLine("using InspecaoWebAPI.Controllers;");
        sbTestes.AppendLine($"using InspecaoWebAPI.Models.{controllerName};");
        //AcaoRecomendadaController
        //

        sbTestes.AppendLine("using InspecaoWebAPI.Testes.Base;");
        sbTestes.AppendLine("using System.Net;");
        sbTestes.AppendLine("using System.Threading.Tasks;");
        sbTestes.AppendLine("using Xunit;");

        sbTestes.AppendLine($"\nnamespace {namespaceForTest}");
        sbTestes.AppendLine("{");
        sbTestes.AppendLine($"    public class {testName} : InspecaoIntegrationTest");
        sbTestes.AppendLine("    {");
        sbTestes.AppendLine($"        public {testName}(InspecaoTestWebAppFactory fixture) : base(fixture) {{ }}");

        sbTestesParameters.AppendLine($"using InspecaoWebAPI.Models.{controllerName};");
        sbTestesParameters.AppendLine("using System.Collections.Generic;");
        sbTestesParameters.AppendLine("using System.Net;");
        sbTestesParameters.AppendLine("");
        sbTestesParameters.AppendLine($"namespace {namespaceForTest}");
        sbTestesParameters.AppendLine("{");
        sbTestesParameters.AppendLine($"    public static class {testParamsClassName}");
        sbTestesParameters.AppendLine("    {");

        StringBuilder sbMetodos = new StringBuilder();

        // Loop por todos os métodos públicos no controlador
        var metodos = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => !m.IsSpecialName && m.DeclaringType.Name == controller.Name).ToArray();
        
        
        //Tratar retorno quando o endpoint retornar no content
        foreach (var method in metodos)
        {
            string uri = $"api/{controllerName}/{method.Name}";

            sbTestes.AppendLine($"        private const string {method.Name}Uri = \"{uri}\";");


            sbMetodos.AppendLine($"\n        [Theory(DisplayName = \"GET {uri} retorna codigos http correto e resultados esperados\", Skip = \"Gerado pelo GeradorDeTestes e não foi modificado ainda\")]");
            sbMetodos.AppendLine($"        [Trait(\"UseCase\", nameof({controller.Name}.{method.Name}))]");
            sbMetodos.AppendLine($"        [MemberData(nameof({controllerName}TestParameters.{method.Name}), MemberType = typeof({controllerName}TestParameters))]");
            sbMetodos.AppendLine($"        public async Task {method.Name}RetornaCodigosHttpCorreto(string jwt, {method.Name}Input input, HttpStatusCode expectedStatusCode)");
            sbMetodos.AppendLine("        {");
            sbMetodos.AppendLine("            // Act");
            sbMetodos.AppendLine($"            (HttpStatusCode actualStatusCode, {method.Name}Output output) = await DoGetRequest<{method.Name}Output>({method.Name}Uri, bearerToken: jwt, parameters: input);");
            sbMetodos.AppendLine("");
            sbMetodos.AppendLine("            // Assert");
            sbMetodos.AppendLine("            actualStatusCode.Should().Be(expectedStatusCode);");
            sbMetodos.AppendLine("        }");
            sbMetodos.AppendLine();

            sbTestesParameters.AppendLine($"        public static List<object[]> {method.Name}()");
            sbTestesParameters.AppendLine("        {");
            sbTestesParameters.AppendLine("            return new List<object[]>");
            sbTestesParameters.AppendLine("            {");
            sbTestesParameters.AppendLine("                new object [] {");
            sbTestesParameters.AppendLine("                    \"<JWT TOKEN HERE>\",");
            sbTestesParameters.AppendLine($"                    new {method.Name}Input()");
            sbTestesParameters.AppendLine("                    {");
            sbTestesParameters.AppendLine("                        // Popule os campos necessários aqui");
            sbTestesParameters.AppendLine("                    },");
            sbTestesParameters.AppendLine("                    HttpStatusCode.OK");
            sbTestesParameters.AppendLine("                },");
            sbTestesParameters.AppendLine("            };");
            sbTestesParameters.AppendLine("        }");
            sbTestesParameters.AppendLine();
        }

        sbTestes.AppendLine(sbMetodos.ToString());
        sbTestes.AppendLine("    }");
        sbTestes.AppendLine("}");

        sbTestesParameters.AppendLine("    }");
        sbTestesParameters.AppendLine("}");

        string testFolderPath = Path.Combine(controllersTestPath, controllerName);
        Directory.CreateDirectory(testFolderPath);  // Criar a pasta para o controlador, se ainda não existir

        string testFilePath = Path.Combine(testFolderPath, testName + ".cs");
        File.WriteAllText(testFilePath, sbTestes.ToString());
        string testParamsFilePath = Path.Combine(testFolderPath, testParamsClassName + ".cs");
        File.WriteAllText(testParamsFilePath, sbTestesParameters.ToString());
    }
}

