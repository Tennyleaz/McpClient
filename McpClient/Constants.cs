using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient;

internal static class Constants
{
    public static readonly List<string> LOCAL_MODELS = new List<string>() { "Qwen2.5-72B-Instruct-AWQ", "Qwen/Qwen3-14B-AWQ", "qwen3-8b", "qwen3-4b-instruct-2507",  "llama-3.2-3b-instruct",  "gemma-3-4b-it" };

    
    public static readonly List<StoreCategoryItem> STORE_CATEGORIES = new List<StoreCategoryItem>
    {
        new StoreCategoryItem("Offical Servers", "official-servers"),
        new StoreCategoryItem("Research and Data", "research-and-data"),
        new StoreCategoryItem("Cloud Platform", "cloud-platforms"),
        new StoreCategoryItem("Browser Automation", "browser-automation"),
        new StoreCategoryItem("Databases", "databases"),
        new StoreCategoryItem("AI Chatbot", "ai-chatbot"),
        new StoreCategoryItem("File System", "file-systems"),
        new StoreCategoryItem("OS Automation", "os-automation"),
        new StoreCategoryItem("Finance", "finance"),
        new StoreCategoryItem("Communication", "communication"),
        new StoreCategoryItem("Developer Tool", "developer-tools"),
        new StoreCategoryItem("Knowledge and Memory", "knowledge-and-memory"),
        new StoreCategoryItem("Entertainment and Media", "entertainment-and-media"),
        new StoreCategoryItem("Calendat Management", "calendar-management"),
        new StoreCategoryItem("Database", "database"),
        new StoreCategoryItem("Location Service", "location-services"),
        new StoreCategoryItem("Customer Data Platform", "'customer-data-platforms"),
        new StoreCategoryItem("Security", "security"),
        new StoreCategoryItem("Monitoring", "monitoring"),
        new StoreCategoryItem("Virtualization", "virtualization"),
        new StoreCategoryItem("Cloud Storage", "cloud-storage")
    };
}