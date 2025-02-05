using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;

string controllersPath = "C:\\Users\\Victor\\Desktop\\MonitoramentoWebAPI\\MonitoramentoWebAPI\\Controllers";
string controllersTestPath = "C:\\Users\\Victor\\Desktop\\MonitoramentoWebAPI\\MonitoramentoWebAPI.Testes\\Controllers";
string assemblyWebAPIPath = "C:\\Users\\Victor\\Desktop\\MonitoramentoWebAPI\\MonitoramentoWebAPI\\bin\\Debug\\net8.0\\MonitoramentoWebAPI.dll";

Assembly assembly = Assembly.LoadFrom(assemblyWebAPIPath);

GerarTestesDeAcordoComEndpoints();

string CamelCase(string s)
{
    return Char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
}

void GerarTestesDeAcordoComEndpoints()
{
    List<Type> controllerTypes = GetLoadableTypes(assembly)
        .Where(t => t.FullName.StartsWith("MonitoramentoWebAPI") && t.FullName.EndsWith("Controller") && !t.FullName.Contains("UtilController") && !t.FullName.Contains("AuthWebAPIController") && !t.FullName.Contains("ApiController") && !t.FullName.Contains("AnalisesSysgeoController"))
        .ToList();

    foreach (Type controller in controllerTypes)
    {
        StringBuilder sbTestes = new StringBuilder();
        StringBuilder sbTestesParameters = new StringBuilder();

        string controllerName = controller.Name.Replace("Controller", "");
        string testName = $"{controllerName}Tests";
        string namespaceForTest = "MonitoramentoWebAPI.Testes.Controllers." + controllerName + "s";
        string testParamsClassName = $"{controllerName}TestParameters";

        sbTestes.AppendLine("using FluentAssertions;");
        sbTestes.AppendLine("using MonitoramentoWebAPI.Controllers;");
        sbTestes.AppendLine($"using MonitoramentoWebAPI.Models.{controllerName}s;");
        sbTestes.AppendLine("using MonitoramentoWebAPI.Testes.Base;");
        sbTestes.AppendLine("using System.Net;");
        sbTestes.AppendLine("using System.Threading.Tasks;");
        sbTestes.AppendLine("using Xunit;");
        sbTestes.AppendLine("using Xunit.Abstractions;");
        sbTestes.AppendLine($"\nnamespace {namespaceForTest}");
        sbTestes.AppendLine("{");
        sbTestes.AppendLine($"\tpublic class {testName} : IntegrationTestBase");
        sbTestes.AppendLine("\t{");
        sbTestes.AppendLine("\t\t");
        sbTestes.AppendLine($"\t\tpublic {testName}(MonitoramentoTestApplicationFactory fixture, ITestOutputHelper output) : base(fixture, output)");
        sbTestes.AppendLine($"\t\t{{");
        sbTestes.AppendLine($"\t\t\t");
        sbTestes.AppendLine($"\t\t}}");
        sbTestes.AppendLine("");


        sbTestesParameters.AppendLine($"using MonitoramentoWebAPI.Models.{controllerName}s;");
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
            typeof(HttpGetAttribute),
            typeof(HttpPostAttribute),
            typeof(HttpPutAttribute),
            typeof(HttpDeleteAttribute),
        };

        IEnumerable<Type> controllers = GetLoadableTypes(assembly)
            .Where(t => t.FullName.StartsWith("MonitoramentoWebAPI.Controllers") && t.Name.EndsWith("Controller") && !t.FullName.Contains("UtilController") && !t.FullName.Contains("AuthWebAPIController") && !t.FullName.Contains("ApiController") && !t.FullName.Contains("AnalisesSysgeoController"));

        // Loop por todos os métodos públicos no controlador que remetem a uma request http
        MethodInfo[] metodos = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => !m.IsSpecialName && m.DeclaringType.Name == controller.Name && m.GetCustomAttributes().Any(a => actionAttributeTypes.Contains(a.GetType())))
            .ToArray();
        
        foreach (MethodInfo method in metodos)
        {
            string uri = $"api/{controllerName}/{{nameof({controller.Name}.{method.Name})}}";
            string httpMethod = "";
            bool ehNoContentResult = false;
            bool ehMultipartFormData = false;

            if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(HttpGetAttribute)))
                httpMethod = "GET";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(HttpPostAttribute)))
                httpMethod = "POST";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(HttpPutAttribute)))
                httpMethod = "PUT";
            else if (method.GetCustomAttributes().Any(a => a.GetType() == typeof(HttpDeleteAttribute)))
                httpMethod = "DELETE";

            List<Attribute> producesResponseTypeAttribute = method.GetCustomAttributes()
                .Where(a => a.GetType() == typeof(ProducesResponseTypeAttribute))
                .ToList();

            List<Attribute> consumesAttribute = method.GetCustomAttributes()
                .Where(a => a.GetType() == typeof(ConsumesAttribute))
                .ToList();

            if (producesResponseTypeAttribute.Any())
            {
                List<ProducesResponseTypeAttribute> attributes = producesResponseTypeAttribute
                    .Select(a => (ProducesResponseTypeAttribute)a)
                    .ToList();

                ehNoContentResult = attributes.Any(a => a.StatusCode == 204);
            }

            if (consumesAttribute.Any())
            {
                List<ConsumesAttribute> attributes = consumesAttribute
                    .Select(a => (ConsumesAttribute)a)
                    .ToList();

                ehMultipartFormData = attributes.Any(a => a.ContentTypes.Contains("multipart/form-data"));
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
                sbMetodos.AppendLine($"            (HttpStatusCode actualStatusCode, string _) = await Do{CamelCase(httpMethod)}Request<string>({method.Name}Uri, bearerToken: jwt, parameters: input{VerificarAdicaoDeOutrosParametros(ehMultipartFormData)});");
            else
                sbMetodos.AppendLine($"            (HttpStatusCode actualStatusCode, {method.Name}Output output) = await Do{CamelCase(httpMethod)}Request<{method.Name}Output>({method.Name}Uri, bearerToken: jwt, parameters: input{VerificarAdicaoDeOutrosParametros(ehMultipartFormData)});");
            sbMetodos.AppendLine("");
            sbMetodos.AppendLine("            // Assert");
            sbMetodos.AppendLine("            actualStatusCode.Should().Be(expectedStatusCode);");
            if(!ehNoContentResult)
                sbMetodos.AppendLine("            output.Should().NotBeNull();");
            sbMetodos.AppendLine("        }");

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

object VerificarAdicaoDeOutrosParametros(bool ehMultipartFormData)
{
    if(!ehMultipartFormData)
        return "";
    else
        return ", requestContentMediaType: \"multipart/form-data\"";
}

static Type[] GetLoadableTypes(Assembly assembly)
{
    if (assembly == null) 
        throw new ArgumentNullException(nameof(assembly));

    try
    {
        return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
        return ex.Types.Where(t => t != null).ToArray();
    }
}