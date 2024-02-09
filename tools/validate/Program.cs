
using System.Text.Json;
using TafutaLib;
using Tavis.UriTemplates;

var jsonQuestions = JsonDocument.Parse(File.ReadAllText("C:\\Users\\darrmi\\src\\github\\darrelmiller\\tafuta\\tools\\validate\\testcases.json"));
var questions = jsonQuestions.RootElement.EnumerateArray().Select(q => new Question
{
    Text = q.GetProperty("question").GetString() ?? String.Empty,
    Answer = q.GetProperty("answer").GetString() ?? String.Empty
});

var httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://localhost:5165")
};

var askQuestionUriTemplate = new Tavis.UriTemplates.UriTemplate("/ApiOperations/search?query={query}{&collection}");
askQuestionUriTemplate.AddParameter("collection", "apiindex2");

var failedQuestions = new List<Question>();
// Iterate through the questions and ask the API
foreach (var question in questions)
{
    askQuestionUriTemplate.SetParameter("query", question.Text);
    var url = askQuestionUriTemplate.Resolve();
    var responsePayload = await httpClient.GetStringAsync(url);
    var apiOperations = ApiOperation.ParseList(responsePayload);
    if (!apiOperations.Any(op => op.OperationKey == question.Answer))
    {
        failedQuestions.Add(question);
    }
}

Console.WriteLine($"Failed questions : {failedQuestions.Count()} out of {questions.Count()} questions.");
// list failed questions
foreach (var failedQuestion in failedQuestions)
{
    Console.WriteLine($"Question: {failedQuestion.Text}");
    Console.WriteLine($"Answer: {failedQuestion.Answer}");
}

public class Question
{
    public string Text { get; set; }
    public string Answer { get; set; }
}