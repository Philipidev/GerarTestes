using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;

string controllersPath = "C:\\Users\\Victor\\Desktop\\GestaoEmergenciaWebAPI\\GestaoEmergenciaWebAPI\\Controllers";
string controllersTestPath = "C:\\Users\\Victor\\Desktop\\GestaoEmergenciaWebAPI\\GestaoEmergenciaWebAPI.Testes\\Controllers";
string assemblyWebAPIPath = "C:\\Users\\Victor\\Desktop\\GestaoEmergenciaWebAPI\\GestaoEmergenciaWebAPI\\bin\\Debug\\net6.0\\GestaoEmergenciaWebAPI.dll";

Assembly assembly = Assembly.LoadFrom(assemblyWebAPIPath);

GerarTestesDeAcordoComEndpoints();

string CamelCase(string s)
{
    return Char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
}

void GerarTestesDeAcordoComEndpoints()
{
    var controllerTypes = assembly.GetTypes().Where(t => t.Name.EndsWith("Controller")).ToList();

    foreach (var controller in controllerTypes)
    {
        StringBuilder sbTestes = new StringBuilder();
        StringBuilder sbTestesParameters = new StringBuilder();

        string controllerName = controller.Name.Replace("Controller", "");
        string testName = $"{controllerName}Tests";
        string namespaceForTest = "GestaoEmergenciaWebAPI.Testes.Controllers." + controllerName + "s"; // Pluralizado com "s" no final
        string testParamsClassName = $"{controllerName}TestParameters";


        // Adiciona o cabeçalho do arquivo de teste
        sbTestes.AppendLine("using FluentAssertions;");
        sbTestes.AppendLine("using GestaoEmergenciaWebAPI.Controllers;");
        sbTestes.AppendLine($"using GestaoEmergenciaWebAPI.Models.{controllerName};");
        sbTestes.AppendLine("using GestaoEmergenciaWebAPI.Testes.Base;");
        sbTestes.AppendLine("using System.Net;");
        sbTestes.AppendLine("using System.Threading.Tasks;");
        sbTestes.AppendLine("using Xunit;");
        sbTestes.AppendLine($"\nnamespace {namespaceForTest}");
        sbTestes.AppendLine("{");
        sbTestes.AppendLine($"\tpublic class {testName} : GestaoEmergenciaIntegrationTest");
        sbTestes.AppendLine("\t{");
        sbTestes.AppendLine($"\t\tpublic {testName}(GestaoEmergenciaTestWebAppFactory fixture) : base(fixture) {{ }}");
        sbTestes.AppendLine("");

        // Adiciona o cabeçalho do arquivo de parametros de teste
        sbTestesParameters.AppendLine($"using GestaoEmergenciaWebAPI.Models.{controllerName};");
        sbTestesParameters.AppendLine("using System.Collections.Generic;");
        sbTestesParameters.AppendLine("using System.Net;");
        sbTestesParameters.AppendLine("");
        sbTestesParameters.AppendLine($"namespace {namespaceForTest}");
        sbTestesParameters.AppendLine("{");
        sbTestesParameters.AppendLine($"\tpublic class {testParamsClassName}");
        sbTestesParameters.AppendLine("\t{");

        StringBuilder sbMetodos = new StringBuilder();

        ISet<Type> actionAttributeTypes = new HashSet<Type>()
        {
            typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute),
            typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute),
            typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute),
            typeof(Microsoft.AspNetCore.Mvc.HttpDeleteAttribute),
        };

        //ProducesResponseTypeAttribute

        IEnumerable<Type> controllers = assembly.GetTypes().Where(t => t.Name.StartsWith("GestaoEmergenciaWebAPI.Controllers"));

        // Loop por todos os métodos públicos no controlador que remetem a uma request http
        MethodInfo[] metodos = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => !m.IsSpecialName && m.DeclaringType.Name == controller.Name && m.GetCustomAttributes().Any(a => actionAttributeTypes.Contains(a.GetType())))
            .ToArray();
        
        //Tratar retorno quando o endpoint retornar no content
        foreach (var method in metodos)
        {
            string uri = $"api/{controllerName}/{{nameof({controller.Name}.{method.Name})}}";
            string httpMethod = "";
            bool ehNoContentResult = false;

            if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute)))
                httpMethod = "GET";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute)))
                httpMethod = "POST";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute)))
                httpMethod = "PUT";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(Microsoft.AspNetCore.Mvc.HttpDeleteAttribute)))
                httpMethod = "DELETE";

            List<Attribute> producesResponseTypeAttribute = method.GetCustomAttributes().Where(a => a.GetType() == typeof(Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute)).ToList();

            if(producesResponseTypeAttribute.Any())
            {
                List<ProducesResponseTypeAttribute> attributes = producesResponseTypeAttribute.Select(a => (ProducesResponseTypeAttribute)a).ToList();
                ehNoContentResult = attributes.Any(a => a.StatusCode == 204);
            }

            string methodNameUri = $"{method.Name}Uri";

            sbTestes.AppendLine($"        private const string {methodNameUri} = $\"{uri}\";");

            if (ehNoContentResult)
                sbMetodos.AppendLine($"\n        [Theory(DisplayName = $\"{httpMethod} {{{methodNameUri}}} retorna codigo http correto\", Skip = \"Gerado pelo GeradorDeTestes e não foi modificado ainda\")]");
            else
                sbMetodos.AppendLine($"\n        [Theory(DisplayName = $\"{httpMethod} {{{methodNameUri}}} retorna codigo http correto e resultado nao nulo\", Skip = \"Gerado pelo GeradorDeTestes e não foi modificado ainda\")]");
            sbMetodos.AppendLine($"        [Trait(\"UseCase\", nameof({controller.Name}.{method.Name}))]");
            sbMetodos.AppendLine($"        [MemberData(nameof({controllerName}TestParameters.{method.Name}), MemberType = typeof({controllerName}TestParameters))]");
            sbMetodos.AppendLine($"        public async Task {method.Name}RetornaCodigosHttpCorreto(string jwt, {method.Name}Input input, HttpStatusCode expectedStatusCode)");
            sbMetodos.AppendLine("        {");
            sbMetodos.AppendLine("            // Act");
            if(ehNoContentResult)
                sbMetodos.AppendLine($"            (HttpStatusCode actualStatusCode, string _) = await Do{CamelCase(httpMethod)}Request({method.Name}Uri, bearerToken: jwt, parameters: input);");
            else
                sbMetodos.AppendLine($"            (HttpStatusCode actualStatusCode, {method.Name}Output output) = await Do{CamelCase(httpMethod)}Request<{method.Name}Output>({method.Name}Uri, bearerToken: jwt, parameters: input);");
            sbMetodos.AppendLine("");
            sbMetodos.AppendLine("            // Assert");
            sbMetodos.AppendLine("            actualStatusCode.Should().Be(expectedStatusCode);");
            if(!ehNoContentResult)
                sbMetodos.AppendLine("            output.Should().NotBeNull();");
            sbMetodos.AppendLine("        }");
            //sbMetodos.AppendLine();

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
            if(ehNoContentResult)
                sbTestesParameters.AppendLine("                    HttpStatusCode.NoContent");
            else
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

        string testFolderPath = Path.Combine(controllersTestPath, $"{controllerName}s");
        Directory.CreateDirectory(testFolderPath);  // Criar a pasta para o controlador, se ainda não existir

        string testFilePath = Path.Combine(testFolderPath, testName + ".cs");
        File.WriteAllText(testFilePath, sbTestes.ToString());
        string testParamsFilePath = Path.Combine(testFolderPath, testParamsClassName + ".cs");
        File.WriteAllText(testParamsFilePath, sbTestesParameters.ToString());
    }
}

