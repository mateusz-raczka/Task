using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace KeepIt_task
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var inputXMLFilePath = args[0];
            var outputJsonFilePath = "D:\\output_file.json";

            XmlNode? employeesNode = LoadXML(inputXMLFilePath);

            var employeesFlatList = GetEmployeesFlatList(employeesNode);

            var employeeTree = GenerateEmployeesHierarchy(employeesFlatList);

            var employeeTreeJson = JsonSerializer.Serialize(employeeTree, new JsonSerializerOptions { WriteIndented = true });

            GenerateJsonFile(outputJsonFilePath, employeeTreeJson);
        }

        static XmlNode? LoadXML(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(filePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to load XML file. Error: {ex.Message}");
            }
            

            return xmlDoc.DocumentElement;
        }

        static void GenerateJsonFile(string filePath, string json)
        {
            try
            {
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate json file with path: {filePath}. Please try to change path. Error: {ex.Message}");
            }
        }

        static EmployeeTree GenerateEmployeesHierarchy(List<EmployeeTree> employeesFlatList)
        {
            var ceo = employeesFlatList.FirstOrDefault(e => string.IsNullOrEmpty(e.Manager));

            try
            {
                if (ceo == null)
                {
                    throw new ArgumentNullException("Failed to build employee hierarchy - cannot find tree's root (CEO).");
                }

                AssignEmployeesToManager(ceo, employeesFlatList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return ceo;
        }

        static void AssignEmployeesToManager(EmployeeTree manager, List<EmployeeTree> employeesFlatList)
        {
            try
            {
                var directReports = employeesFlatList.Where(e => e.Manager == manager.EmployeeDetails.Email).ToList();
                manager.EmployeeDetails.DirectReports = directReports;

                foreach (var report in directReports)
                {
                    AssignEmployeesToManager(report, employeesFlatList);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to assign employees to manager: {manager.EmployeeDetails.Email}. Error: {ex.Message}");
            }
            
        }

        static List<EmployeeTree> GetEmployeesFlatList(XmlNode? employeesNode)
        {
            var employeesFlatList = new List<EmployeeTree>();

            try
            {
                foreach (XmlNode employeeNode in employeesNode.SelectNodes("employee"))
                {
                    var employeeEmailNode = employeeNode.SelectSingleNode("field[@id='email']");
                    var employeeManagerNode = employeeNode.SelectSingleNode("field[@id='manager']");

                    var email = employeeEmailNode?.InnerText;

                    if(email == null)
                    {
                        throw new Exception("Failed to create employee object - missing mandatory field (email).");
                    }

                    var manager = employeeManagerNode?.InnerText;

                    employeesFlatList.Add(new EmployeeTree(email, manager));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return employeesFlatList;
        }
    }

    class EmployeeTree
    {
        [JsonPropertyName("employee")]
        public EmployeeDetails EmployeeDetails { get; set; }
        [JsonIgnore]
        public string? Manager { get; set; }

        public EmployeeTree(string email, string? manager)
        {
            EmployeeDetails = new EmployeeDetails
            {
                Email = email,
                DirectReports = new List<EmployeeTree>()
            };
            Manager = manager;
        }
    }

    class EmployeeDetails
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("direct_reports")]
        public List<EmployeeTree> DirectReports { get; set; } = new List<EmployeeTree>();
    }
}
