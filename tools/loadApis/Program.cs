// See https://aka.ms/new-console-template for more information
// Load OpenApis.csv from the parent folder

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace OpenApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var baseUrl = "http://localhost:5165";
            var urlTemplate = new UriTemplate("{+baseUrl}/ApiDescriptions?apiDescriptionUrl={+url}");
            var httpClient = new HttpClient(); 
            // Load file from current working directory
            string[] lines = File.ReadAllLines("C:\\Users\\darrmi\\src\\github\\darrelmiller\\tafuta\\tools\\openapi.csv");
            var lineCount = lines.Length;
            var currentLine = 0;
            var startLine = 157;
            foreach (string line in lines)
            {
                if (currentLine < startLine) {
                    currentLine++;
                    continue;
                }
                if (line.Contains(',')) {
                    string[] parts = line.Split(',');
                    var url = parts[1];
                    var uploadUrl = urlTemplate.AddParameters(new {baseUrl=baseUrl, url=url}).Resolve();
                    Console.Write($"{currentLine}/{lineCount} {url} -> ");
                    try {
                    await httpClient.PostAsync(uploadUrl, null);
                    Console.WriteLine("Done");
                    } catch (Exception ex) {
                        Console.WriteLine($"Failed: {ex.Message}");
                    }
                }
                currentLine++;
            }
        }
    }
}