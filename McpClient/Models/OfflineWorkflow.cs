using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class OfflineWorkflow
{
    public int Id { get; set; }
    public int User_id { get; set; }
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public string Payload { get; set; }

    public string TryGetPrompt()
    {
        if (!string.IsNullOrWhiteSpace(Payload))
        {
            try
            {
                JsonNode jsonNode = JsonNode.Parse(Payload);
                if (jsonNode != null)
                {
                    // Parse user message, let user to modify
                    if (jsonNode["message"] != null)
                    {
                        return jsonNode["message"].ToString().Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse workflow prompt: " + ex.Message);
            }
        }

        return null;
    }

    public List<string> TryGetTools()
    {
        if (!string.IsNullOrWhiteSpace(Payload))
        {
            try
            {
                JsonNode jsonNode = JsonNode.Parse(Payload);
                if (jsonNode != null)
                {
                    // Parse used MCP tool list
                    if (jsonNode["tools"] != null)
                    {
                        List<string> usedTools = jsonNode["tools"].Deserialize<List<string>>();
                        return usedTools;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse workflow prompt: " + ex.Message);
            }
        }

        return null;
    }
}
