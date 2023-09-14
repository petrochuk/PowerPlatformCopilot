using System.Text.Json.Serialization;

public class Nl2SqlResponse
{
    public object error { get; set; }
    public Query query { get; set; }
    public QueryResult QueryResult { get; set; }
    public object history { get; set; }
    public object additionalProperties { get; set; }
}

public class Query
{
    public string tSql { get; set; }
    public string explanation { get; set; }
}

public class QueryResult
{
    [JsonPropertyName("result")]
    public List<Result> Results { get; set; }
    public bool maxRowsExceeded { get; set; }
    public List<string> Links { get; set; }
}

public class Result
{
    public string roleid { get; set; }
    public string name { get; set; }
    public string fullname { get; set; }
    public string title { get; set; }
    public string address1_telephone1 { get; set; }
    public string businessunitid { get; set; }
    public object positionid { get; set; }
    public string systemuserid { get; set; }
    public DateTime modifiedon { get; set; }
    [JsonPropertyName("@recordLinks")]
    public List<string> RecordLinks { get; set; }
}
