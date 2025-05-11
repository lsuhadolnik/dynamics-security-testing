using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LS.SecurityRoleListGenerator
{
    class Program
    {

        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            string connectionString = configuration["AdminConnectionString"];
            string classNamespace = configuration["ClassNamespace"];
            string outputFilePath = configuration["OutputFile"];

            using (ServiceClient serviceClient = new ServiceClient(connectionString))
            {
                if (serviceClient.IsReady)
                {
                    var securityRoles = FetchSecurityRoles(serviceClient);
                    GenerateSecurityRolesClass(outputFilePath, classNamespace, securityRoles);
                    Console.WriteLine($"Security roles successfully exported to {outputFilePath}");
                }
                else
                {
                    Console.WriteLine("Failed to connect to CRM.");
                }
            }
        }

        private static Dictionary<string, string> FetchSecurityRoles(ServiceClient serviceClient)
        {
            var securityRoles = new Dictionary<string, string>();

            QueryExpression query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name")
            };

            EntityCollection roles = serviceClient.RetrieveMultiple(query);

            foreach (Entity role in roles.Entities)
            {
                string roleId = role.Id.ToString();
                string roleName = role.GetAttributeValue<string>("name");
                securityRoles[roleName] = roleId;
            }

            return securityRoles;
        }

        private static void GenerateSecurityRolesClass(string outputFilePath, string classNamespace, Dictionary<string, string> securityRoles)
        {
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("// Auto-generated file containing security roles");
                writer.WriteLine($"namespace {classNamespace} {{");
                writer.WriteLine("    public static class SecurityRole");
                writer.WriteLine("    {");

                foreach (var role in securityRoles)
                {
                    string sanitizedRoleName = SanitizeName(role.Key);
                    writer.WriteLine($"        public const string {sanitizedRoleName} = \"{role.Key}\"; // {role.Value}");
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            string pascal = string.Concat(name.Split(' ').Select(word => char.ToUpper(word[0]) + word.Substring(1)));
            string sanitized = new string(pascal.Where(char.IsLetterOrDigit).ToArray());
            return sanitized;
        }
    }
}
